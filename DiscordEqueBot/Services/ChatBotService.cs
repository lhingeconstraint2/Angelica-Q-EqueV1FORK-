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

    private async Task OnMessageReceived(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;


        //var response = await _chatModel.GetResponseAsync(message.Content);
        //if (response is null)
        //  return;

        //await message.Channel.SendMessageAsync(response);
    }
}