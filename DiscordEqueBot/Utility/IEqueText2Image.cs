namespace DiscordEqueBot.Utility;

public interface IEqueText2Image
{
    public Task<Stream> GenerateImageAsync(string text);
}
