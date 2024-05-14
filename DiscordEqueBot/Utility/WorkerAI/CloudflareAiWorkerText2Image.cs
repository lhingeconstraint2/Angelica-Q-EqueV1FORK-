using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DiscordEqueBot.Utility.WorkerAI;

public class CloudflareAiWorkerText2Image : IEqueText2Image
{
    public CloudflareAiWorkerText2Image(IOptions<CloudflareConfiguration> configuration)
    {
        Configuration = configuration.Value;
    }

    private CloudflareConfiguration Configuration { get; }

    public async Task<Stream> GenerateImageAsync(string text)
    {
        var url = Configuration.BaseUrlFormatted + "/ai/run/" + Configuration.Text2ImageModelId;
        var data = new Text2ImageRequest
        {
            prompt = text
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {Configuration.APIKey}");
        request.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

        HttpClient client = new HttpClient();
        var response = await client.SendAsync(request);
        var responseStream = await response.Content.ReadAsStreamAsync();
        return responseStream;
    }

    public class Text2ImageRequest
    {
        public string prompt { get; set; }
    }
}