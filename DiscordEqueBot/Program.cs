using Discord;
using Discord.WebSocket;

public class Program
{
    private static DiscordSocketClient _client;


    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    public static async Task Main()
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };
        _client = new DiscordSocketClient(config);

        _client.Log += Log;
        _client.Ready += Ready;
        _client.MessageUpdated += MessageUpdated;


        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    private static Task Ready()
    {
        Console.WriteLine($"Connected as -> [{_client.CurrentUser.Username}]");
        return Task.CompletedTask;
    }

    private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after,
        ISocketMessageChannel channel)
    {
        // If the message was not in the cache, downloading it will result in getting a copy of `after`.
        var message = await before.GetOrDownloadAsync();
        Console.WriteLine($"{message} -> {after.Content}");
    }
}
