using LangChain.Providers;

namespace DiscordEqueBot.Utility.WorkerAI;

public class CloudflareChatSettings : ChatSettings
{
    public uint MaxTokens { get; init; } = 64;
    public string Prompt { get; init; } = string.Empty;

    public bool Raw { get; init; } = false;
    public string? LoraId { get; init; } = null;
}