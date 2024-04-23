namespace DiscordEqueBot.Utility;

public class EqueConfiguration
{
    public static string SectionName { get; set; } = "Eque";

    public bool IsScrappingEnabled { get; set; } = true;

    public string DiscordToken { get; set; }
}
