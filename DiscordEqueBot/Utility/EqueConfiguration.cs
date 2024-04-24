namespace DiscordEqueBot.Utility;

public class EqueConfiguration
{
    public static string SectionName { get; set; } = "Eque";

    public bool IsScrappingEnabled { get; set; } = true;

    public string DiscordToken { get; set; }

    public string Template { get; set; } =
        "Your name is AIBOT. You are designed to complete the story, answer in one line.";

    public string TemplateCharName { get; set; } = "AIBOT";

    public uint MaxResponseLength { get; set; } = 128;
}
