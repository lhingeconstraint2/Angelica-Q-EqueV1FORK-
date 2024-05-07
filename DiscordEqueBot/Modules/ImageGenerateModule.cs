using Discord.Interactions;
using DiscordEqueBot.Utility;

namespace DiscordEqueBot.Modules;

public class ImageGenerateModule : InteractionModuleBase<SocketInteractionContext>
{
    public ImageGenerateModule(IEqueText2Image equeText2Image)
    {
        EqueText2Image = equeText2Image;
    }

    private IEqueText2Image EqueText2Image { get; }

    [SlashCommand("ai-generate-image", "Generate an image.", runMode: RunMode.Async)]
    public async Task GenerateImage(string prompt)
    {
        try
        {
            await DeferAsync();
            var image = await EqueText2Image.GenerateImageAsync(prompt);
            // Copy to memory stream to avoid issues with the stream being disposed
            var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            await FollowupWithFileAsync(memoryStream, "image.png");
            await image.DisposeAsync();
            // Try to write the image to a file
            memoryStream.Position = 0;
            await File.WriteAllBytesAsync("image.png", memoryStream.ToArray());
        }
        catch (Exception e)
        {
            await RespondAsync($"An error occurred: {e.Message}");
        }
    }
}
