FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DiscordEqueBot/DiscordEqueBot.csproj", "DiscordEqueBot/"]
RUN dotnet restore "DiscordEqueBot/DiscordEqueBot.csproj"
COPY . .
WORKDIR "/src/DiscordEqueBot"
RUN dotnet build "DiscordEqueBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiscordEqueBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscordEqueBot.dll"]
