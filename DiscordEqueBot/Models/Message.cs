namespace DiscordEqueBot.Models;

public class Message
{
    public string ContextId;
    public int Id { get; set; }
    public string Content { get; set; }
    public string Author { get; set; }

    public string AuthorId { get; set; }
    public string Channel { get; set; }
    public string ChannelId { get; set; }
    public string? Guild { get; set; }
    public string? GuildId { get; set; }

    public uint Likes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
