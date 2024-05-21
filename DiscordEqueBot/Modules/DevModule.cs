using System.Globalization;
using System.Reflection;
using CacheTower;
using Discord;
using Discord.Interactions;
using DiscordEqueBot.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordEqueBot.Modules;

public class DevModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICacheStack _cacheStack;
    private readonly ILogger<DevModule> _logger;
    private readonly EqueConfiguration _options;

    public DevModule(IOptions<EqueConfiguration> options, ICacheStack cacheStack, ILogger<DevModule> logger)
    {
        _options = options.Value;
        _cacheStack = cacheStack;
        _logger = logger;
    }


    private static DateTime GetBuildDate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string buildVersionMetadataPrefix = "+build";
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion == null) return default;
        var value = attribute.InformationalVersion;
        var index = value.IndexOf(buildVersionMetadataPrefix, StringComparison.Ordinal);
        if (index <= 0) return default;
        value = value.Substring(index + buildVersionMetadataPrefix.Length);
        if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var result))
        {
            return result;
        }

        return default;
    }

    private async Task Info()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Developer Information")
            .AddField("Environment.UserName", Environment.UserName)
            .AddField("Environment.ProcessId", Environment.ProcessId)
            .WithTimestamp(GetBuildDate())
            .Build();
        await FollowupAsync(embed: embed);
    }

    private async Task CleanupCache()
    {
        await _cacheStack.CleanupAsync();
        await FollowupAsync("Cache cleaned up.");
    }

    [SlashCommand("dev", "Bot developer only.", runMode: RunMode.Async)]
    public async Task Dev(string command)
    {
        if (!_options.DevIds.Contains(Context.User.Id + ""))
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Unauthorized")
                .WithDescription("You are not authorized to use this command.")
                .WithColor(Color.Red)
                .Build());
            return;
        }

        await DeferAsync();
        try
        {
            switch (command)
            {
                case "pwd":
                    await FollowupAsync(Environment.CurrentDirectory);
                    break;
                case "exit":
                    await FollowupAsync("Exiting...");
                    Environment.Exit(0);
                    break;
                case "info":
                    await Info();
                    break;
                case "cleanup-cache":
                    await CleanupCache();
                    break;

                default:
                    await FollowupAsync("Unknown command.");
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred");
            await FollowupAsync($"An error occurred: {e.Message}");
        }
    }
}
