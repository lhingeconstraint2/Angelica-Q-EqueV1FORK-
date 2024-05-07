using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using DiscordEqueBot.Utility;
using DiscordEqueBot.Utility.WorkerAI;
using LangChain.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Message = LangChain.Providers.Message;

namespace DiscordEqueBot.Services;

public class ChatBotService : IHostedService
{
    private ChatModel _chatModel;
    private IConfiguration _config;
    private DiscordSocketClient _discord;
    private IEmbeddingModel _embeddingModel;
    private ILogger<ChatBotService> _logger;
    private IOptions<EqueConfiguration> _options;

    public ChatBotService(ChatModel chatModel, ILogger<ChatBotService> logger, IEmbeddingModel embeddingModel,
        DiscordSocketClient discord,
        IOptions<EqueConfiguration> options,
        IConfiguration config)
    {
        _chatModel = chatModel;
        _logger = logger;
        _discord = discord;
        _embeddingModel = embeddingModel;
        _config = config;
        _options = options;
        CloudflareChatSettings.MaxTokenDefault = _options.Value.MaxResponseLength;
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
        _ = Task.Run(async () =>
        {
            IDisposable? disposable = null;
            try
            {
                if (message.Author.IsBot)
                    return;
                // check if mentioned me or reply to my message
                IMessage? messageReference = null;
                if (message.Reference?.MessageId.IsSpecified == true)
                {
                    messageReference = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value);
                }

                bool isMentionedMe =
                    message.MentionedUsers.FirstOrDefault(u => u.Id == _discord.CurrentUser.Id) != null;
                bool isReplyToMe = messageReference?.Author.Id == _discord.CurrentUser.Id;

                if (!isMentionedMe && !isReplyToMe)
                    return;

                disposable = message.Channel.EnterTypingState();
                await _OnMessageReceived(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing message");
            }
            finally
            {
                disposable?.Dispose();
            }
        });
        return Task.CompletedTask;
    }

    private async Task _OnMessageReceived(SocketMessage message)
    {
        var discordChat = new DiscordChat(_discord);
        await discordChat.AddMessage(message);

        // Reference or History
        if (message.Reference?.MessageId.IsSpecified == true)
        {
            await discordChat.ExploreMessageReference(message.Reference);
        }
        else
        {
            await discordChat.ExploreMessageHistory(message);
        }


        var aiName = _discord.CurrentUser.Username;
        var sampleConversation = _options.Value.Template;
        sampleConversation = sampleConversation.Replace(_options.Value.TemplateCharName, aiName);

        uint maxWords = (uint) (CloudflareChatSettings.MaxTokenDefault * 0.8);
        var chatRequest = discordChat.ToChatRequest([
            new(sampleConversation, MessageRole.System),
            new Message($"Keep OOC out of the chat, max words limit up to {maxWords} words.", MessageRole.System)
        ]);

        var chatResponse = await _chatModel.GenerateAsync(chatRequest);
        var response = chatResponse.LastMessageContent;
        response = response.Replace(aiName + ": ", "");
        response = response.Replace(_discord.CurrentUser.Username + "#" + _discord.CurrentUser.Discriminator + ": ",
            "");
        // replace text that surrounded by * * or italic
        response = Regex.Replace(response, @"\*.*?\*", "");
        if (string.IsNullOrWhiteSpace(response))
        {
            return;
        }

        IUserMessage messageRespond;
        if (message is SocketUserMessage userMessage)
        {
            messageRespond = await userMessage.ReplyAsync(response);
        }
        else
        {
            messageRespond = await message.Channel.SendMessageAsync(response);
        }

        await messageRespond.AddReactionAsync(new Emoji("👍"));
        await messageRespond.AddReactionAsync(new Emoji("👎"));
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

        public async Task AddMessage(IMessage message)
        {
            if (message.CleanContent.Length == 0) return;
            var content = await GetAiFriendlyContent(message);
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
                await AddMessage(message);
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
                    await AddMessage(history);
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
                var escaped = value.Trim().Replace("\n", "\\n");
                sb.AppendLine(escaped);
            }

            sb.Append($"{_discord.CurrentUser.Username}: ");
            array.Add(new Message(sb.ToString(), MessageRole.Chat));
            return new ChatRequest
            {
                Messages = array,
            };
        }


        public static async Task<String> GetAiFriendlyContent(IMessage message)
        {
            StringBuilder sb = new StringBuilder();
            if (message.Reference?.MessageId.IsSpecified == true)
            {
                var replyMessage = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value);
                if (replyMessage is not null)
                {
                    var replyContent = replyMessage.CleanContent.Replace("\n", "\\n");
                    if (!string.IsNullOrWhiteSpace(replyContent))
                    {
                        // sb.AppendLine($"> {replyMessage.Author.Username}: {replyContent}");
                    }
                }
            }

            sb.AppendLine($"{message.Author.Username}: {message.CleanContent.Replace("\n", "\\n")}");
            return sb.ToString();
        }


        public static uint CountTokens(String message)
        {
            return (uint) message.Split(' ').Length; // naive tokenization
        }
    }
}
