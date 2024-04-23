using LangChain.Providers;
using Newtonsoft.Json;

namespace DiscordEqueBot.Utility.WorkerAI;

public partial class CloudflareAiWorkerChatInput
{
    [JsonProperty("max_tokens", NullValueHandling = NullValueHandling.Ignore)]
    public long? MaxTokens { get; set; } = 100;

    [JsonProperty("prompt", NullValueHandling = NullValueHandling.Ignore)]
    public string? Prompt { get; set; }

    [JsonProperty("raw", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Raw { get; set; }

    [JsonProperty("lora", NullValueHandling = NullValueHandling.Ignore)]
    public string? Lora { get; set; }

    [JsonProperty("stream", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Stream { get; set; }

    [JsonProperty("messages", NullValueHandling = NullValueHandling.Ignore)]
    public Message[] Messages { get; set; }
}

public partial class Message
{
    [JsonProperty("content")] public string Content { get; set; }

    [JsonProperty("role")] public string Role { get; set; }

    public static implicit operator LangChain.Providers.Message(Message message)
    {
        MessageRole.TryParse(message.Role, out MessageRole role);
        return new LangChain.Providers.Message(message.Content, role);
    }

    public static implicit operator Message(LangChain.Providers.Message message)
    {
        var role = "user";
        switch (message.Role)
        {
            case MessageRole.Ai:
                role = "assistant";
                break;
            case MessageRole.System:
                role = "system";
                break;
            default:
                role = "user";
                break;
        }

        return new Message
        {
            Content = message.Content,
            Role = role
        };
    }
}
