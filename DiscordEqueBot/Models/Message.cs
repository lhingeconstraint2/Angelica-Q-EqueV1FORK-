using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DiscordEqueBot.Models;

[Index(nameof(Snowflake), IsUnique = true)]
public class Message
{
    public string ContextId;

    public int Id { get; set; }
    public string Snowflake { get; set; }

    [MaxLength(4096)] public string Content { get; set; }

    [MaxLength(255)] public string Author { get; set; }

    [MaxLength(255)] public string AuthorId { get; set; }

    public bool IsAi { get; set; } = false;

    [MaxLength(255)] public string Channel { get; set; }

    [MaxLength(255)] public string ChannelId { get; set; }

    [MaxLength(255)] public string? Guild { get; set; }

    [MaxLength(255)] public string? GuildId { get; set; }

    public int Likes { get; set; } = 0;
    public int replies { get; set; } = 0;

    public string? ReplyTo { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
