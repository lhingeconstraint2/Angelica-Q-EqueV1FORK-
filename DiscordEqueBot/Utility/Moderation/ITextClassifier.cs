namespace DiscordEqueBot.Utility.Moderation;

public interface ITextClassifier
{
    public Task<ClassificationResult> classify(string text);
}