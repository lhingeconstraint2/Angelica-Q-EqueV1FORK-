using Newtonsoft.Json;

namespace DiscordEqueBot.Utility.WorkerAI;

public partial class CloudflareWorkerAiChatOutput
{
    [JsonProperty("result")] public Result Result { get; set; }

    [JsonProperty("success")] public bool Success { get; set; }

    [JsonProperty("errors")] public string[] Errors { get; set; }

    [JsonProperty("messages")] public string[] Messages { get; set; }
}

public partial class Result
{
    [JsonProperty("response")] public string Response { get; set; }
}
