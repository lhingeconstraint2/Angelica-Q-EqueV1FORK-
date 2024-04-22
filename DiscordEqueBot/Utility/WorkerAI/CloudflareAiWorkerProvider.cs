using DiscordEqueBot.Utility.WorkerAI;
using LangChain.Providers;

namespace DiscordEqueBot.Services;

public class CloudflareAiWorkerProvider
    : Provider
{
    public CloudflareAiWorkerProvider(CloudflareConfiguration configuration) : base(CloudflareConfiguration.SectionName)
    {
        Configuration = configuration;
        ChatSettings = new CloudflareChatSettings();
    }

    public CloudflareConfiguration Configuration { get; init; }

    public string GetEndpointChat(string model)
    {
        var url = Configuration.BaseUrlFormatted + "/ai/run/" + Id;
        if (Configuration.AIGatewayName != null)
        {
            url = Configuration.GatewayUrlFormatted + Configuration.AIGatewayName + "/workers-ai/" + Id;
        }

        return url;
    }
}
