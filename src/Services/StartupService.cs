using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NLog;

namespace mummybot.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;
        private Logger _log;

        public StartupService(DiscordSocketClient discord, CommandService commands, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _log = LogManager.GetCurrentClassLogger();
            
            var cancellationToken = new CancellationToken();
            // ReSharper disable once UnusedVariable
            var timerTask = RunPeriodically(Status, TimeSpan.FromSeconds(25), cancellationToken);

            _discord.Disconnected += Disconnected;
        }

        public async Task StartAsync()
        {
            var config = new ConfigService();
            
            await _discord.LoginAsync(TokenType.Bot, config.Config["token"]).ConfigureAwait(false);
            await _discord.StartAsync().ConfigureAwait(false);
            await _discord.SetStatusAsync(UserStatus.Online).ConfigureAwait(false);
            await _commands.AddModulesAsync(this.GetType().GetTypeInfo().Assembly, _provider);
        }

        private async void Status()
        {
            var r = new Random();

            await Task.Delay(2000);
            
            var statuses = new[]
            {
                $"Uptime: {(int)(DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalHours} hours",
                //$"Shard: {_discord.ShardId} / 1",
                $"{_discord.Guilds.Count} guilds",
                $"Latency: {_discord.Latency}ms",
                //$"{GC.GetTotalMemory(true) / 1000000} Megabytes used",
                $"{_discord.Guilds.Sum(guild => guild.MemberCount)} users"
                
            };
            await _discord.SetGameAsync($"{statuses[r.Next(statuses.Length)]} | £help").ConfigureAwait(false);
        }

        private static async Task RunPeriodically(Action action, TimeSpan interval, CancellationToken token)
        {
            while (true)
            {
                action();
                await Task.Delay(interval, token);
            }
        }

        //private async Task ScheduleAction(Action action, DateTime executionTime)
        //{
        //    await Task.Delay((int)executionTime.Subtract(DateTime.Now).TotalMilliseconds);
        //    action();
        //}

        private Task Disconnected(Exception ex)
        {
            _log.Warn(ex);
            return Task.CompletedTask;
        }
    }
}
