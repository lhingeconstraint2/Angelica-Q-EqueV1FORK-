using CacheTower;
using DiscordEqueBot.AI.Chat;
using DiscordEqueBot.Models;
using DiscordEqueBot.Utility;
using DiscordEqueBot.Utility.WorkerAI;
using LangChain.Providers;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Message = LangChain.Providers.Message;

namespace DiscordEqueBot.Services;

public class TextClassifierService : IHostedService
{
    public static string[] Classification =
    [
        "safe",
        "racy"
    ];

    public TextClassifierService(ICacheStack cache, DatabaseContext databaseContext,
        ChatModelProvider chatModelProvider)
    {
        Cache = cache;
        DatabaseContext = databaseContext;
        ChatModelProvider = chatModelProvider;
    }

    private ICacheStack Cache { get; }
    private DatabaseContext DatabaseContext { get; }
    private ChatModelProvider ChatModelProvider { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DatabaseContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ClassificationResult> Classify(string prompt)
    {
        var key = $"TextClassifierService_Classify_{prompt}";
        var classificationEntry = await Cache.GetAsync<ClassificationResult>(key);
        if (classificationEntry != null && classificationEntry.Value != null)
        {
            return classificationEntry.Value;
        }

        // five shot
        var messages = new List<Message>
        {
            new Message(
                "Classify the following text as " + string.Join(" or ", Classification) + ", respond in json format.",
                MessageRole.System),
            new Message("huge tits", MessageRole.Human),
            new Message(JsonConvert.SerializeObject(new ClassificationResult("racy")), MessageRole.Ai),
            new Message("Racing cars", MessageRole.Human),
            new Message(JsonConvert.SerializeObject(new ClassificationResult("safe")), MessageRole.Ai),
            new Message("Huge assssssssssssssssses", MessageRole.Human),
            new Message(JsonConvert.SerializeObject(new ClassificationResult("racy")), MessageRole.Ai),
            new Message("Mr crabs from spongebob if he was from O block", MessageRole.Human),
            new Message(JsonConvert.SerializeObject(new ClassificationResult("safe")), MessageRole.Ai),
            new Message(prompt, MessageRole.Human)
        };
        var chatRequest = ChatRequest.ToChatRequest(messages.ToArray());
        var response = await ChatModelProvider.GetChatModel().GenerateAsync(chatRequest, new CloudflareChatSettings()
        {
            MaxTokens = 20
        });
        var result = new ClassificationResult("safe");
        try
        {
            result = JsonConvert.DeserializeObject<ClassificationResult>(response.LastMessageContent) ??
                     new ClassificationResult("safe");
            await Cache.SetAsync(key, result, TimeSpan.FromDays(1));
            await DatabaseContext.TextClassificationLogs.AddAsync(new TextClassificationLog()
            {
                Input = prompt,
                Output = response.LastMessageContent
            });
        }
        catch
        {
            // ignored
        }

        return result;
    }

    public async Task<bool> IsSafe(string prompt)
    {
        var result = await Classify(prompt);
        return result.Classification == "safe";
    }

    public class ClassificationResult
    {
        public ClassificationResult(string classification)
        {
            Classification = classification;
        }

        public string Classification { get; set; }
    }
}