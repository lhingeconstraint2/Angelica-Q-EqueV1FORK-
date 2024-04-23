using Discord;
using Discord.WebSocket;
using DiscordEqueBot.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordEqueBot.Services;

public class DiscordStartupService : IHostedService
{
    private readonly IConfiguration _config;
    private readonly DiscordSocketClient _discord;
    private readonly ILogger<DiscordStartupService> _logger;

    public DiscordStartupService(DiscordSocketClient discord, IConfiguration config,
        ILogger<DiscordStartupService> logger)
    {
        _discord = discord;
        _config = config;
        _logger = logger;

        _discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        if (string.IsNullOrEmpty(_config["DiscordToken"]))
            throw new InvalidOperationException("Discord token is not set in configuration.");
        _discord.Ready += OnReady;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _discord.LoginAsync(TokenType.Bot, _config["DiscordToken"]);
        await _discord.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discord.LogoutAsync();
        await _discord.StopAsync();
    }

    private async Task OnReady()
    {
        _logger.LogInformation("Logged in as {Username}#{Discriminator}", _discord.CurrentUser.Username,
            _discord.CurrentUser.Discriminator);
        _logger.LogInformation("Connected to {GuildCount} guilds", _discord.Guilds.Count);
    }
}