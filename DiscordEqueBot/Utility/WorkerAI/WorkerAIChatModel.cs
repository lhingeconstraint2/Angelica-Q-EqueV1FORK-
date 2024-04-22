using System.Text;
using DiscordEqueBot.Services;
using LangChain.Providers;
using Newtonsoft.Json;

namespace DiscordEqueBot.Utility.WorkerAI;

public class WorkerAIChatModel : ChatModel, IPaidLargeLanguageModel, IChatModel<ChatRequest, ChatResponse, ChatSettings>

{
    public const double PricePerNeuronUsageInUsd = 0.000011;
    protected CloudflareAiWorkerProvider CloudflareAiWorkerProvider;
    protected decimal PricingPerInputTokenInUsd;
    protected decimal PricingPerOutputTokenInUsd;


    /// <inheritdoc/>
    /// to get neuronUsage go to https://ai.cloudflare.com/#pricing-calculator
    /// baseNeuronUsage = 1 input token, 1 output token, 10000 requests
    /// neuronUsageInputToken = 2 input tokens, 1 output token, 10000 requests
    /// neuronUsageOutputToken = 1 input token, 2 output tokens, 10000 requests
    public WorkerAIChatModel(
        string id,
        CloudflareAiWorkerProvider cloudflareAiWorkerProvider,
        uint contextTokenLimit,
        uint sequenceTokenLimit,
        bool isMessage = false,
        uint baseNeuronUsage = 0,
        uint neuronUsageInputToken = 0,
        uint neuronUsageOutputToken = 0
    ) : base(id)
    {
        CloudflareAiWorkerProvider = cloudflareAiWorkerProvider;
        neuronUsageInputToken = neuronUsageInputToken - baseNeuronUsage;
        neuronUsageOutputToken = neuronUsageOutputToken - baseNeuronUsage;
        PricingPerInputTokenInUsd = new decimal(PricePerNeuronUsageInUsd * neuronUsageInputToken);
        PricingPerOutputTokenInUsd = new decimal(PricePerNeuronUsageInUsd * neuronUsageOutputToken);
        ContextLength = (int) contextTokenLimit;
        IsMessage = isMessage;
    }

    public bool IsMessage { get; }


    public override async Task<ChatResponse> GenerateAsync(ChatRequest chatRequest, ChatSettings? settings = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var url = CloudflareAiWorkerProvider.GetEndpointChat(Id);
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", "Bearer " + CloudflareAiWorkerProvider.Configuration.APIKey);

        CloudflareChatSettings cloudflareChatSettings;
        if (settings is CloudflareChatSettings chatSettings)
        {
            cloudflareChatSettings = chatSettings;
        }
        else
        {
            cloudflareChatSettings = new CloudflareChatSettings();
        }


        var input = new CloudflareAiWorkerChatInput
        {
            Prompt = cloudflareChatSettings.Prompt,
            MaxTokens = cloudflareChatSettings.MaxTokens,
            Stream = false,
            Raw = cloudflareChatSettings.Raw,
            Messages = chatRequest.Messages.Select<LangChain.Providers.Message, Message>(x => x).ToArray()
        };

        var json = JsonConvert.SerializeObject(input);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonConvert.DeserializeObject<CloudflareWorkerAiChatOutput>(responseString);
        if (chatResponse == null)
        {
            throw new Exception("Failed to deserialize response");
        }


        return new ChatResponse
        {
            Messages = chatRequest.Messages
                .Append(new LangChain.Providers.Message(chatResponse.Response, MessageRole.Ai)).ToArray(),
            UsedSettings = cloudflareChatSettings
        };
    }


    public double CalculatePriceInUsd(int inputTokens, int outputTokens)
    {
        return (double) (PricingPerInputTokenInUsd * inputTokens + PricingPerOutputTokenInUsd * outputTokens);
    }
}
