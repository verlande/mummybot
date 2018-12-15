using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using mummybot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace mummybot
{
    public static class Program
    {
        public static void Main(string[] args) =>
            StartAsync().GetAwaiter().GetResult();
        
        private static async Task StartAsync()
        {
            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    MessageCacheSize = 500
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Debug,
                    CaseSensitiveCommands = false
                }))
                .AddSingleton<DebugLoggingService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<Random>()
                .AddSingleton<StartupService>();

            var provider = services.BuildServiceProvider();

            await provider.GetRequiredService<StartupService>().Start();
            provider.GetRequiredService<DebugLoggingService>();
            provider.GetRequiredService<CommandHandlerService>();
            provider.GetRequiredService<CommandService>();
            await Task.Delay(-1);
        }
    }
}