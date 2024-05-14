using Discord.Interactions;
using DiscordEqueBot.Utility;
using LangChain.Providers;

namespace DiscordEqueBot.Modules;

public class ImageGenerateModule : InteractionModuleBase<SocketInteractionContext>
{
    public ImageGenerateModule(IEqueText2Image equeText2Image, ChatModel chatModel)
    {
        EqueText2Image = equeText2Image;
        ChatModel = chatModel;
    }

    private IEqueText2Image EqueText2Image { get; }
    private ChatModel ChatModel { get; }

    private async Task<bool> IsSafe(string prompt)
    {
        var messages = new List<Message>();
        messages.Add(new Message("Classify the following text as safe or racy, only respond with 'safe' or 'racy'.",
            MessageRole.System));
        messages.Add(new Message(prompt, MessageRole.Human));
        var chatRequest = ChatRequest.ToChatRequest(messages.ToArray());
        var response = await ChatModel.GenerateAsync(chatRequest);
        return response.LastMessageContent.Contains("safe");
    }

    [SlashCommand("ai-generate-image", "Generate an image.", runMode: RunMode.Async)]
    public async Task GenerateImage(string prompt)
    {
        try
        {
            await DeferAsync();
            // is the prompt safe?
            if (!await IsSafe(prompt))
            {
                await RespondAsync("The prompt is not safe.");
                return;
            }

            var image = await EqueText2Image.GenerateImageAsync(prompt);
            // Copy to memory stream to avoid issues with the stream being disposed
            var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            await FollowupWithFileAsync(memoryStream, "image.png");
        }
        catch (Exception e)
        {
            await RespondAsync($"An error occurred: {e.Message}");
        }
    }
}