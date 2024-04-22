namespace DiscordEqueBot.Utility.WorkerAI;

public class CloudflareConfiguration
{
    public static string BaseUrl = "https://api.cloudflare.com/client/v4/accounts/";
    public static string GatewayUrl = "https://gateway.ai.cloudflare.com/v1/";
    public static string SectionName { get; } = "Cloudflare";

    public string APIKey { get; init; }

    public string AccountTag { get; init; }

    public string? AIGatewayName { get; init; }

    public string ChatModelId { get; init; }


    public string BaseUrlFormatted
    {
        get { return $"{BaseUrl}{AccountTag}/"; }
    }

    public string GatewayUrlFormatted
    {
        get { return $"{GatewayUrl}{AccountTag}/"; }
    }
}
