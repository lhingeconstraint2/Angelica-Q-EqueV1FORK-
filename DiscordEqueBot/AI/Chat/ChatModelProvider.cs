using DiscordEqueBot.Utility.AzureAI;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordEqueBot.AI.Chat;

public class ChatModelProvider
{
    public AzureAIConfiguration.ModelConfig? CurrentModel = null;

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
        var res = ChangeChatModel(null);
        if (res is null)
        {
            throw new Exception("No chat model found");
        }

        return res;
    }

    public ChatModel? ChangeChatModel(string? model)
    {
        var selectedModel = Configuration.Value.GetSelectedModel(model);
        if (selectedModel is null)
        {
            return null;
        }

        var provider = new OpenAiProvider(
            selectedModel.Key,
            selectedModel.Endpoint
        );
        var llm = new OpenAiLatestSmartChatModel(provider);
        this.ChatModel = llm;
        Logger.LogInformation("ChatModelProvider: Chat model is:" + selectedModel.GetDisplayName());
        this.CurrentModel = selectedModel;
        return llm;
    }
}