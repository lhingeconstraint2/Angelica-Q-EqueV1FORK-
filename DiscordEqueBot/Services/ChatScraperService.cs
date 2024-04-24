using Discord;
using Discord.WebSocket;
using DiscordEqueBot.Models;
using DiscordEqueBot.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly IOptions<EqueConfiguration> _options;

    public ChatScraperService(ILogger<ChatScraperService> logger, DiscordSocketClient discord,
        IOptions<EqueConfiguration> options,
        DatabaseContext databaseContext)
    {
        _logger = logger;
        _discord = discord;
        _options = options;
        _databaseContext = databaseContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.IsScrappingEnabled)
            return Task.CompletedTask;
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
        _discord.MessageUpdated -= OnMessageUpdated;
        _discord.ReactionAdded -= OnReactionAdded;
        _discord.ReactionRemoved -= OnReactionRemoved;
        _discord.ReactionsCleared -= OnReactionsCleared;
        return Task.CompletedTask;
    }

    private async Task<Message> AddOrUpdateMessageToDB(IMessage message)
    {
        var messageDb = await _databaseContext.Messages.Where(m => m.Snowflake == message.Id.ToString())
            .FirstOrDefaultAsync();
        if (messageDb is null)
        {
            messageDb = new Message
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
            _databaseContext.Messages.Add(messageDb);
        }
        else
        {
            messageDb.Content = message.CleanContent;
            messageDb.UpdatedAt = DateTime.Now;
        }

        if (message.Channel is SocketGuildChannel guildChannel)
        {
            messageDb.Guild = guildChannel.Guild.Name;
            messageDb.GuildId = guildChannel.Guild.Id.ToString();
        }

        if (message.Author.Id == _discord.CurrentUser.Id)
        {
            messageDb.IsAi = true;
        }

        return messageDb;
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        if (message.CleanContent.Length == 0)
            return;
        string? guildName = null;

        var messageDb = await AddOrUpdateMessageToDB(message);
        guildName = messageDb.Guild;
        _logger.LogInformation("[{Timestamp:yyyy-MM-dd HH:mm:ss}] {GuildName} - {ChannelName} - {Username}: {Content}",
            DateTime.Now, guildName, message.Channel.Name, message.Author.Username, message.CleanContent);


        if (message.Reference?.MessageId.IsSpecified == true)
        {
            messageDb.ReplyTo = message.Reference.MessageId.ToString();

            var replyMessage = await _databaseContext.Messages
                                   .Where(m => m.Snowflake == message.Reference.MessageId.ToString())
                                   .FirstOrDefaultAsync() ??
                               await AddOrUpdateMessageToDB(
                                   await message.Channel.GetMessageAsync(message.Reference.MessageId.Value));

            replyMessage.replies++;
        }

        await _databaseContext.SaveChangesAsync();
        await RecalculateLikes(message);
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
        await RecalculateLikes(message);
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
