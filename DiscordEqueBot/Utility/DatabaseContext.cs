using Microsoft.EntityFrameworkCore;
using DbContext = Microsoft.EntityFrameworkCore.DbContext;
using Message = DiscordEqueBot.Models.Message;

namespace DiscordEqueBot.Utility;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
        Database.Migrate();
    }

    public Microsoft.EntityFrameworkCore.DbSet<Message> Messages { get; set; }
}
