using DiscordEqueBot.Utility.WorkerAI;
using LangChain.Providers;
using Microsoft.Extensions.Options;

namespace DiscordEqueBot.Services;

public class CloudflareAiWorkerProvider
    : Provider
{
    public CloudflareAiWorkerProvider(IOptions<CloudflareConfiguration> configuration) : base(CloudflareConfiguration
        .SectionName)
    {
        Configuration = configuration.Value;
        ChatSettings = new CloudflareChatSettings();
    }

    public CloudflareConfiguration Configuration { get; init; }

    public string GetEndpointChat(string model)
    {
        var url = Configuration.BaseUrlFormatted + "/ai/run/" + model;
        if (Configuration.AIGatewayName != null)
        {
            url = Configuration.GatewayUrlFormatted + Configuration.AIGatewayName + "/workers-ai/" + model;
        }

        return url;
    }
}