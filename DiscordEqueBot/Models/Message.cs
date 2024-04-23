using Microsoft.EntityFrameworkCore;

namespace DiscordEqueBot.Models;

[Index(nameof(Snowflake), IsUnique = true)]
public class Message
{
    public string ContextId;

    public int Id { get; set; }
    public string Snowflake { get; set; }
    public string Content { get; set; }
    public string Author { get; set; }
    public bool IsAi { get; set; } = false;

    public string AuthorId { get; set; }
    public string Channel { get; set; }
    public string ChannelId { get; set; }
    public string? Guild { get; set; }
    public string? GuildId { get; set; }

    public int Likes { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}