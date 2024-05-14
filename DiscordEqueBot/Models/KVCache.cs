using Microsoft.EntityFrameworkCore;

namespace DiscordEqueBot.Models;

[PrimaryKey(nameof(Key))]
public class KVCache
{
    public string Key { get; set; }
    public string Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public int hits { get; set; } = 0;
}