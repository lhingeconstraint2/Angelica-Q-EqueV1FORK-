using DiscordEqueBot.Utility.AzureAI;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordEqueBot.AI.Chat;

public class ChatModelProvider
{
    public ChatModelProvider(IOptions<AzureAIConfiguration> configuration, ILogger<ChatModelProvider> logger)
    {
        Configuration = configuration;
        Logger = logger;
        GetChatModel();
    }

    private IOptions<AzureAIConfiguration> Configuration { get; init; }
    private ChatModel? ChatModel { get; set; }
    private ILogger<ChatModelProvider> Logger { get; init; }


    public ChatModel GetChatModel()
    {
        if (ChatModel is not null)
        {
            return ChatModel;
        }

        var selectedModel = Configuration.Value.GetSelectedModel();
        var provider = new OpenAiProvider(
            selectedModel.Key,
            selectedModel.Endpoint
        );
        var llm = new OpenAiLatestSmartChatModel(provider);
        ChatModel = llm;
        Logger.LogInformation("ChatModelProvider: Chat model is:" + selectedModel.GetDisplayName());
        return llm;
    }
}