using System.Diagnostics;
using Discord;
using Discord.Interactions;
using DiscordEqueBot.Services;
using DiscordEqueBot.Utility;
using Humanizer;
using LangChain.Providers;
using Microsoft.Extensions.Logging;

namespace DiscordEqueBot.Modules;

public class ImageGenerateModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<ImageGenerateModule> _logger;

    public ImageGenerateModule(IEqueText2Image equeText2Image, ChatModel chatModel,
        TextClassifierService textClassifierService, ILogger<ImageGenerateModule> logger)
    {
        EqueText2Image = equeText2Image;
        ChatModel = chatModel;
        TextClassifierService = textClassifierService;
        _logger = logger;
    }

    private IEqueText2Image EqueText2Image { get; }
    private ChatModel ChatModel { get; }
    private TextClassifierService TextClassifierService { get; }


    [SlashCommand("ai-generate-image", "Generate an image.", runMode: RunMode.Async)]
    public async Task GenerateImage(string prompt)
    {
        var start = Stopwatch.StartNew();

        try
        {
            // is the prompt safe?
            TimeSpan timeDiff;

            var deferAsyncTask = DeferAsync();

            if (!await TextClassifierService.IsSafe(prompt))
            {
                timeDiff = start.Elapsed;
                await deferAsyncTask;
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithTitle("Racy Content Detected")
                    .WithDescription(
                        "The prompt you provided was classified as racy content. Please provide a different prompt.")
                    .WithFooter($"Took {timeDiff.Humanize()}")
                    .WithColor(Color.Red)
                    .Build());
                return;
            }

            var image = await EqueText2Image.GenerateImageAsync(prompt);
            // Copy to memory stream to avoid issues with the stream being disposed
            var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            timeDiff = start.Elapsed;
            await deferAsyncTask;
            await FollowupWithFileAsync(memoryStream, "image.png", embed: new EmbedBuilder()
                .WithFooter($"Took {timeDiff.Humanize()}")
                .WithImageUrl("attachment://image.png")
                .Build());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred");
            await FollowupAsync($"An error occurred: {e.Message}");
        }
    }
}
