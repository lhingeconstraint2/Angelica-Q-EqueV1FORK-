using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordEqueBot.Services;
using DiscordEqueBot.Utility;
using DiscordEqueBot.Utility.WorkerAI;
using LangChain.Extensions.DependencyInjection;
using LangChain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCore.AutoRegisterDi;

var configDiscord = new DiscordSocketConfig
{
    AlwaysDownloadUsers = true,
    GatewayIntents = GatewayIntents.MessageContent
                     | GatewayIntents.DirectMessages
                     | GatewayIntents.DirectMessageReactions
                     | GatewayIntents.DirectMessageTyping
                     | GatewayIntents.GuildMembers
                     | GatewayIntents.GuildMessages
                     | GatewayIntents.Guilds
                     | GatewayIntents.GuildMessageReactions
                     | GatewayIntents.GuildMessageTyping
};

var builder = Host.CreateApplicationBuilder(args);
var currentDirectory = Directory.GetCurrentDirectory();
var possibleAppSettingLocations = new[]
{
    Path.Combine(currentDirectory, "appsettings.json"),
    Path.Combine(currentDirectory, "..", "appsettings.json"),
    Path.Combine(currentDirectory, "..", "..", "appsettings.json"),
    Path.Combine(currentDirectory, "..", "..", "..", "appsettings.json"),
    Path.Combine(currentDirectory, "..", "..", "..", "..", "appsettings.json"),
    Path.Combine(currentDirectory, "..", "..", "..", "..", "..", "appsettings.json"),
    Path.Combine(currentDirectory, "..", "..", "..", "..", "..", "..", "appsettings.json"),
};

builder.Configuration.SetBasePath(currentDirectory);
builder.Configuration.AddEnvironmentVariables();

bool foundAppSetting = false;
foreach (var possibleAppSettingLocation in possibleAppSettingLocations)
{
    if (File.Exists(possibleAppSettingLocation))
    {
        foundAppSetting = true;
        builder.Configuration.AddJsonFile(possibleAppSettingLocation, false, true);
        break;
    }
}

if (!foundAppSetting)
{
    throw new FileNotFoundException("Could not find appsettings.json");
}

builder.Logging.AddConsole();

builder.Services.AddDbContext<DatabaseContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
builder.Services.AddSingleton(new DiscordSocketClient(configDiscord)); // Add the discord client to services
builder.Services.AddSingleton<InteractionService>(); // Add the interaction service to services
builder.Services.AddOpenAi();
builder.Services.Configure<CloudflareConfiguration>(
    builder.Configuration.GetSection(CloudflareConfiguration
        .SectionName)); // Add the CloudflareConfiguration to services
builder.Services.Configure<EqueConfiguration>(
    builder.Configuration.GetSection(EqueConfiguration.SectionName)); // Add the EqueConfiguration to services


builder.Services.AddSingleton<CloudflareAiWorkerProvider>(); // Add the CloudflareAiWorkerProvider to services
builder.Services.AddSingleton<IEmbeddingModel, EqueEmbeddingModel>(); // Add the EqueEmbeddingModel to services
builder.Services.AddSingleton<ChatModel, CloudflareAiWorkerChatModel>(provider =>
{
    var cloudflareAiWorkerProvider = provider.GetRequiredService<CloudflareAiWorkerProvider>();
    var cloudflareConfiguration = provider.GetRequiredService<IOptions<CloudflareConfiguration>>().Value;
    return new CloudflareAiWorkerChatModel(cloudflareAiWorkerProvider, cloudflareConfiguration.ChatModelId);
}); // Add the CloudflareAiWorkerChatModel to services
builder.Services.AddSingleton<IEqueText2Image, CloudflareAiWorkerText2Image>(); // Add the Text2Image

// Register any class that ends with "Service" as a service
builder.Services.RegisterAssemblyPublicNonGenericClasses()
    .Where(c => c.Name.EndsWith("Service"))
    .AsPublicImplementedInterfaces();


var host = builder.Build();


await host.RunAsync();
