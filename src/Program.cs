﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace mummybot
{
    public sealed class Program
    {
        private static void Main(string[] args)
            => StartAsync().GetAwaiter().GetResult();

        private static async Task StartAsync()
        {
            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    MessageCacheSize = 500,
                    TotalShards = 1,
                    AlwaysDownloadUsers = true
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Debug,
                    CaseSensitiveCommands = false,
                    ThrowOnError = false
                }))
                .AddDbContext<mummybotDbContext>(options =>
                    {
                        options.UseNpgsql(new ConfigService().Config["dbstring"]
                            );
                    }, ServiceLifetime.Transient)
                .AddSingleton<Modules.Tag.Services.TagService>()
                .AddSingleton<MessageService>()
                .AddSingleton<GuildService>()
                .AddSingleton<UserService>()
                .AddSingleton<TagService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<DebugLoggingService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<StartupService>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<DebugLoggingService>();

            await provider.GetRequiredService<StartupService>().StartAsync();

            provider.GetRequiredService<ConfigService>();
            provider.GetRequiredService<MessageService>();
            provider.GetRequiredService<GuildService>();
            provider.GetRequiredService<UserService>();
            provider.GetRequiredService<CommandService>();
            provider.GetRequiredService<CommandHandlerService>();
            NLogSetup.SetupLogger();
            await Task.Delay(-1).ConfigureAwait(false);
        }
    }
}