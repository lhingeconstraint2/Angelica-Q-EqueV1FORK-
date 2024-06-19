namespace DiscordEqueBot.Utility.Moderation;

public class ClassificationResult
{
    public required IDictionary<string, float> Additionals;
    public bool IsRacy;
    public bool IsSafe;
}