using System.Globalization;
using System.Reflection;
using Discord;
using Discord.Interactions;
using DiscordEqueBot.Utility;
using Microsoft.Extensions.Options;

namespace DiscordEqueBot.Modules;

public class DevModule : InteractionModuleBase<SocketInteractionContext>
{
    private EqueConfiguration _options;

    public DevModule(IOptions<EqueConfiguration> options)
    {
        _options = options.Value;
    }


    private static DateTime GetBuildDate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string BuildVersionMetadataPrefix = "+build";

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion != null)
        {
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(BuildVersionMetadataPrefix);
            if (index > 0)
            {
                value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                        out var result))
                {
                    return result;
                }
            }
        }

        return default;
    }

    private async Task info()
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
            case "reload":
                await RespondAsync("Reloading...");
                break;
            case "info":
                await info();
                break;
            default:
                await RespondAsync("Unknown command.");
                break;
        }
    }
}
