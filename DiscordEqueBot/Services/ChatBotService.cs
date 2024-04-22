using LangChain.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordEqueBot.Services;

public class ChatBotService : IHostedService
{
    private ChatModel _chatModel;
    private IConfiguration _config;
    private IEmbeddingModel _embeddingModel;
    private ILogger<ChatBotService> _logger;

    public ChatBotService(ChatModel chatModel, ILogger<ChatBotService> logger, IEmbeddingModel embeddingModel,
        IConfiguration config)
    {
        _chatModel = chatModel;
        _logger = logger;
        _embeddingModel = embeddingModel;
        _config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
