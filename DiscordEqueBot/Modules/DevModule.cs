using System.Globalization;
using System.Reflection;
using Discord;
using Discord.Interactions;
using DiscordEqueBot.Utility;
using Microsoft.Extensions.Options;

namespace DiscordEqueBot.Modules;

public class DevModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EqueConfiguration _options;

    public DevModule(IOptions<EqueConfiguration> options)
    {
        _options = options.Value;
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
        await RespondAsync(embed: embed);
    }

    [SlashCommand("dev", "Bot developer only commands.", runMode: RunMode.Async)]
    public async Task Dev(string command)
    {
        if (!_options.DevIds.Contains(Context.User.Id + ""))
        {
            await RespondAsync("You are not a developer.");
            return;
        }

        switch (command)
        {
            case "exit":
                await RespondAsync("Exiting...");
                Environment.Exit(0);
                break;
            case "info":
                await Info();
                break;
            default:
                await RespondAsync("Unknown command.");
                break;
        }
    }
}
