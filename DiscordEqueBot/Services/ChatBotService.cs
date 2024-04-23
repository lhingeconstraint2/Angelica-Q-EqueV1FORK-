using System.Text;
using Discord;
using Discord.WebSocket;
using LangChain.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordEqueBot.Services;

public class ChatBotService : IHostedService
{
    private ChatModel _chatModel;
    private IConfiguration _config;
    private DiscordSocketClient _discord;
    private IEmbeddingModel _embeddingModel;
    private ILogger<ChatBotService> _logger;

    public ChatBotService(ChatModel chatModel, ILogger<ChatBotService> logger, IEmbeddingModel embeddingModel,
        DiscordSocketClient discord,
        IConfiguration config)
    {
        _chatModel = chatModel;
        _logger = logger;
        _discord = discord;
        _embeddingModel = embeddingModel;
        _config = config;
        chatModel.PromptSent += (o, s) => _logger.LogInformation("Prompt: {Prompt}", s);
        chatModel.CompletedResponseGenerated += (o, s) => _logger.LogInformation("Response: {Response}", s);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _discord.MessageReceived += OnMessageReceived;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _discord.MessageReceived -= OnMessageReceived;
        return Task.CompletedTask;
    }


    private Task OnMessageReceived(SocketMessage message)
    {
        _ = Task.Run(async () => { await _OnMessageReceived(message); });
        return Task.CompletedTask;
    }

    private async Task _OnMessageReceived(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;
        // check if mentioned me or reply to my message
        IMessage? messageReference = null;
        if (message.Reference?.MessageId.IsSpecified == true)
        {
            messageReference = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value);
        }

        if (!message.Content.Contains(_discord.CurrentUser.Mention) &&
            messageReference?.Author.Id != _discord.CurrentUser.Id)
            return;

        var discordChat = new DiscordChat(_discord);
        discordChat.AddMessage(message);
        await discordChat.ExploreMessageReference(message.Reference);
        await discordChat.ExploreMessageHistory(message);


        var chatRequest = discordChat.ToChatRequest([
            new($"Your name is {_discord.CurrentUser.Username}#{_discord.CurrentUser.Discriminator}.",
                MessageRole.System)
        ]);

        var disposable = message.Channel.EnterTypingState();
        var chatResponse = await _chatModel.GenerateAsync(chatRequest);
        var response = chatResponse.LastMessageContent;
        if (string.IsNullOrWhiteSpace(response))
        {
            disposable.Dispose();
            return;
        }

        await message.Channel.SendMessageAsync(response);
        disposable.Dispose();
    }


    class DiscordChat
    {
        private DiscordSocketClient _discord;

        public DiscordChat(DiscordSocketClient discord)
        {
            _discord = discord;
            TokenCount = 0;
            Messages = new SortedDictionary<ulong, string>();
            MessageIsAi = new Dictionary<ulong, bool>();
        }

        protected uint TokenCount { get; set; }
        public SortedDictionary<ulong, string> Messages { get; set; }
        public Dictionary<ulong, bool> MessageIsAi { get; set; }

        public void AddMessage(IMessage message)
        {
            if (message.CleanContent.Length == 0) return;
            var content = GetAiFriendlyContent(message);
            TokenCount += CountTokens(content);
            Messages.TryAdd(message.Id, content);
            MessageIsAi[message.Id] = message.Author.Id == _discord.CurrentUser.Id;
        }

        public async Task ExploreMessageReference(MessageReference? reference, int maxToken = 100)
        {
            if (reference == null) return;
            var channel = await _discord.GetChannelAsync(reference.ChannelId);
            if (channel is not IMessageChannel textChannel) return;
            while (reference != null && TokenCount < maxToken)
            {
                var message = await textChannel.GetMessageAsync(reference.MessageId.Value);
                AddMessage(message);
                reference = message.Reference;
            }
        }

        public async Task ExploreMessageHistory(IMessage message, int maxToken = 200)
        {
            var messageHistory = message.Channel.GetMessagesAsync(message.Id, Direction.Before, 100);
            await foreach (var historyMessage in messageHistory)
            {
                foreach (var history in historyMessage)
                {
                    AddMessage(history);
                    if (TokenCount >= maxToken)
                        break;
                }

                if (TokenCount >= maxToken)
                    break;
            }
        }

        public ChatRequest ToChatRequest(Message[]? prompt = null)
        {
            var array = new List<Message>();
            if (prompt != null)
            {
                array.AddRange(prompt);
            }

            StringBuilder sb = new StringBuilder();
            foreach (var (key, value) in Messages)
            {
                var escaped = value.Replace("\n", "\\n");
                sb.AppendLine(escaped);
            }

            sb.Append($"{_discord.CurrentUser.Username}: ");
            array.Add(new Message(sb.ToString(), MessageRole.Chat));
            return new ChatRequest
            {
                Messages = array,
            };
        }

        public static String GetAiFriendlyContent(IMessage message)
        {
            return $"{message.Author.Username}: {message.CleanContent.Replace("\n", "\\n")}";
        }

        public static uint CountTokens(String message)
        {
            return (uint) message.Split(' ').Length; // naive tokenization
        }
    }
}
