using Discord;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using mummybot.Modules.Manage.Services;
using System;

namespace mummybot
{
    public sealed class Program
    {
        private static void Main()
            => StartAsync().GetAwaiter().GetResult();

        private static async Task StartAsync()
        {
            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
#if DEBUG
                    LogLevel = LogSeverity.Debug,
#else
                    LogLevel = LogSeverity.Info,
#endif
                    MessageCacheSize = 500,
                    TotalShards = 1,
                    AlwaysDownloadUsers = true
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
#if DEBUG
                    LogLevel = LogSeverity.Debug,
#else
                    LogLevel = LogSeverity.Info,
#endif
                    CaseSensitiveCommands = false,
                    ThrowOnError = false
                }))
                .AddDbContext<mummybotDbContext>(options =>
                {
#if DEBUG
                    options.UseNpgsql(new ConfigService().Config["dbstring"]);
#else
                    options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"));
#endif
                }, ServiceLifetime.Transient)
                .AddSingleton<Modules.Tag.Services.TagService>()
                .AddSingleton<Modules.Manage.Services.FilteringService>()
                .AddSingleton<Modules.Runescape.Services.StatsService>()
                .AddSingleton<RoleService>()
                .AddSingleton<MessageService>()
                .AddSingleton<Services.GuildService>()
                .AddSingleton<UserService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<DebugLoggingService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<StartupService>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<DebugLoggingService>();

            await provider.GetRequiredService<StartupService>().RunAsync().ConfigureAwait(false);
            provider.GetService<CommandHandlerService>().AddServices(services);

            provider.GetRequiredService<ConfigService>();
            provider.GetRequiredService<MessageService>();
            provider.GetRequiredService<Services.GuildService>();
            provider.GetRequiredService<UserService>();
            provider.GetRequiredService<CommandService>();
            provider.GetRequiredService<CommandHandlerService>();
            provider.GetRequiredService<RoleService>();
            NLogSetup.SetupLogger();

            await Task.Delay(-1).ConfigureAwait(false);
        }
    }
}