using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace mummybot.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        public StartupService(DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            
            CancellationToken cancellationToken = new CancellationToken();
            Task timerTask = RunPeriodically(Status, TimeSpan.FromSeconds(25), cancellationToken);
        }

        public async Task StartAsync()
        {
            var config = new ConfigService();
            
            await _discord.LoginAsync(TokenType.Bot, config.Config["token"]);
            await _discord.StartAsync();
            await _discord.SetStatusAsync(UserStatus.Online);
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async void Status()
        {
            var r = new Random();
            var users = _discord.Guilds.Sum(guild => guild.MemberCount);

            await Task.Delay(2000);
            
            var statuses = new[]
            {
                $"Uptime: {(int)(DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalHours} hours",
                //$"Shard: {_discord.ShardId} / 1",
                $"{_discord.Guilds.Count} guilds",
                $"Latency: {_discord.Latency}ms",
                //$"{GC.GetTotalMemory(true) / 1000000} Megabytes used",
                $"{users} users"
                
            };
            await _discord.SetGameAsync(statuses[r.Next(statuses.Length)] + " | £help");
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
    }
}