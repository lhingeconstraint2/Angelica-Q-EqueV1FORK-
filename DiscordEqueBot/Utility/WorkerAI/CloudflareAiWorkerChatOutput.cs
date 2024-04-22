using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DiscordEqueBot.Utility.WorkerAI;

public partial class CloudflareWorkerAiChatOutput
{
    [JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
    public string Response { get; set; }
}

public partial struct CloudflareWorkerAiChatOutputUnion
{
    public CloudflareWorkerAiChatOutput CloudflareWorkerAiChatOutput;
    public string String;

    public static implicit operator
        CloudflareWorkerAiChatOutputUnion(CloudflareWorkerAiChatOutput cloudflareWorkerAiChatOutput) =>
        new CloudflareWorkerAiChatOutputUnion {CloudflareWorkerAiChatOutput = cloudflareWorkerAiChatOutput};

    public static implicit operator CloudflareWorkerAiChatOutputUnion(string String) =>
        new CloudflareWorkerAiChatOutputUnion {String = String};
}

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            CloudflareWorkerAiChatOutputUnionConverter.Singleton,
            new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}
        },
    };
}

internal class CloudflareWorkerAiChatOutputUnionConverter : JsonConverter
{
    public static readonly CloudflareWorkerAiChatOutputUnionConverter Singleton =
        new CloudflareWorkerAiChatOutputUnionConverter();

    public override bool CanConvert(Type t) => t == typeof(CloudflareWorkerAiChatOutputUnion) ||
                                               t == typeof(CloudflareWorkerAiChatOutputUnion?);

    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.String:
            case JsonToken.Date:
                var stringValue = serializer.Deserialize<string>(reader);
                return new CloudflareWorkerAiChatOutputUnion {String = stringValue};
            case JsonToken.StartObject:
                var objectValue = serializer.Deserialize<CloudflareWorkerAiChatOutput>(reader);
                return new CloudflareWorkerAiChatOutputUnion {CloudflareWorkerAiChatOutput = objectValue};
        }

        throw new Exception("Cannot unmarshal type CloudflareWorkerAiChatOutputUnion");
    }

    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    {
        var value = (CloudflareWorkerAiChatOutputUnion) untypedValue;
        if (value.String != null)
        {
            serializer.Serialize(writer, value.String);
            return;
        }

        if (value.CloudflareWorkerAiChatOutput != null)
        {
            serializer.Serialize(writer, value.CloudflareWorkerAiChatOutput);
            return;
        }

        throw new Exception("Cannot marshal type CloudflareWorkerAiChatOutputUnion");
    }
}
