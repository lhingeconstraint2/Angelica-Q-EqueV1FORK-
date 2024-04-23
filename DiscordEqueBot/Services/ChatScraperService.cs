using Discord;
using Discord.WebSocket;
using DiscordEqueBot.Models;
using DiscordEqueBot.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordEqueBot.Services;

public class ChatScraperService : IHostedService
{
    private static readonly Emoji[] LikeEmojis = new[]
    {
        new Emoji("👍"),
        new Emoji("\u2b50"), //star
        new Emoji("\ud83d\udd25"), //fire
    };

    private static readonly Emoji[] DislikeEmojis = new[]
    {
        new Emoji("👎"),
    };

    private readonly DatabaseContext _databaseContext;
    private readonly DiscordSocketClient _discord;
    private readonly ILogger<ChatScraperService> _logger;

    public ChatScraperService(ILogger<ChatScraperService> logger, DiscordSocketClient discord,
        DatabaseContext databaseContext)
    {
        _logger = logger;
        _discord = discord;
        _databaseContext = databaseContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _discord.MessageReceived += OnMessageReceived;
        _discord.MessageUpdated += OnMessageUpdated;
        _discord.ReactionAdded += OnReactionAdded;
        _discord.ReactionRemoved += OnReactionRemoved;
        _discord.ReactionsCleared += OnReactionsCleared;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _discord.MessageReceived -= OnMessageReceived;
        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        if (message.CleanContent.Length == 0)
            return;
        string? guildName = null;
        var messageDb = new Message
        {
            Snowflake = message.Id.ToString(),
            Author = message.Author.Username,
            AuthorId = message.Author.Id.ToString(),
            Content = message.CleanContent,
            Channel = message.Channel.Name,
            ChannelId = message.Channel.Id.ToString(),
            ContextId = message.Channel.Id.ToString(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        if (message.Channel is SocketGuildChannel guildChannel)
        {
            guildName = guildChannel.Guild.Name;
            messageDb.Guild = guildName;
            messageDb.GuildId = guildChannel.Guild.Id.ToString();
        }

        if (message.Author.Id == _discord.CurrentUser.Id)
        {
            messageDb.IsAi = true;
        }

        _logger.LogInformation("[{Timestamp:yyyy-MM-dd HH:mm:ss}] {GuildName} - {ChannelName} - {Username}: {Content}",
            DateTime.Now, guildName, message.Channel.Name, message.Author.Username, message.CleanContent);

        _databaseContext.Messages.Add(messageDb);

        await _databaseContext.SaveChangesAsync();
    }

    private async Task OnMessageUpdated(Cacheable<IMessage, ulong> cacheable, SocketMessage message,
        ISocketMessageChannel channel)
    {
        var messageDb = await _databaseContext.Messages.Where(m => m.Snowflake == message.Id.ToString())
            .FirstOrDefaultAsync();
        if (messageDb is null)
            return;

        messageDb.Content = message.CleanContent;
        messageDb.UpdatedAt = DateTime.Now;

        await _databaseContext.SaveChangesAsync();
    }


    private async Task RecalculateLikes(IMessage message)
    {
        var messageDb = await _databaseContext.Messages.Where(m => m.Snowflake == message.Id.ToString())
            .FirstOrDefaultAsync();
        if (messageDb is null)
            return;

        messageDb.Likes = 0;
        foreach (var likeEmoji in LikeEmojis)
        {
            if (message.Reactions.TryGetValue(likeEmoji, out var likeReaction))
            {
                messageDb.Likes += likeReaction.ReactionCount;
            }
        }

        foreach (var dislikeEmoji in DislikeEmojis)
        {
            if (message.Reactions.TryGetValue(dislikeEmoji, out var dislikeReaction))
            {
                messageDb.Likes -= dislikeReaction.ReactionCount;
            }
        }

        await _databaseContext.SaveChangesAsync();
    }


    private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> cacheable, SocketReaction reaction)
    {
        await RecalculateLikes(await message.GetOrDownloadAsync());
    }

    private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> cacheable, SocketReaction reaction)
    {
        await RecalculateLikes(await message.GetOrDownloadAsync());
    }

    private async Task OnReactionsCleared(Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> cacheable)
    {
        await RecalculateLikes(await message.GetOrDownloadAsync());
    }
}
