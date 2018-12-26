using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace mummybot
{
    public class Program
    {
        public static void Main(string[] args)
            => StartAsync().GetAwaiter().GetResult();

        private static async Task StartAsync()
        {
            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    MessageCacheSize = 500,
                    TotalShards = 1
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Debug,
                    CaseSensitiveCommands = false
                }))
                .AddDbContext<mummybotDbContext>(options =>
                    {
                        options.UseNpgsql(new ConfigService().Config["dbstring"]
                            );
                    }, ServiceLifetime.Transient)
                .AddSingleton<GuildService>()
                .AddSingleton<UserService>()
                .AddSingleton<TagService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<DebugLoggingService>()
                .AddSingleton<StartupService>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<DebugLoggingService>();
            
            await provider.GetRequiredService<StartupService>().Start();
            
            provider.GetRequiredService<GuildService>();
            provider.GetRequiredService<UserService>();
            provider.GetRequiredService<CommandService>();
            provider.GetRequiredService<CommandHandlerService>();
            
            await Task.Delay(-1);
        }
    }
}