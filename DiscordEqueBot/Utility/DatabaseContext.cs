using Microsoft.EntityFrameworkCore;
using Message = DiscordEqueBot.Models.Message;

namespace DiscordEqueBot.Utility;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
        Database.Migrate();
    }

    public DbSet<Message> Messages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=database.db");
    }
}
