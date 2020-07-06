using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Models;
using NLog;

namespace mummybot.Services
{
    public class StartupService
    {
        private DiscordSocketClient _discord { get; }
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private string DefaultPrefix { get; }

        public StartupService(DiscordSocketClient discord, CommandService commands, GuildService guildService, IServiceProvider provider, mummybotDbContext context)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

#if DEBUG
            DefaultPrefix = new ConfigService().Config["prefix"];
#else
            DefaultPrefix = Environment.GetEnvironmentVariable("PREFIX");
#endif
            var cancellationToken = new CancellationToken();
            // ReSharper disable once UnusedVariable
            var timerTask = RunPeriodically(Status, TimeSpan.FromSeconds(25), cancellationToken);

            _discord.Disconnected += Disconnected;
        }

        public async Task RunAsync()
        {
            var config = new ConfigService();
#if DEBUG
            await _discord.LoginAsync(TokenType.Bot, config.Config["token"]).ConfigureAwait(false);
#else
            await _discord.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN")).ConfigureAwait(false);
#endif
            await _discord.StartAsync().ConfigureAwait(false);
            await _discord.SetStatusAsync(UserStatus.Online).ConfigureAwait(false);
            await _commands.AddModulesAsync(GetType().GetTypeInfo().Assembly, _provider);
        }

        private async void Status()
        {   
            var r = new Random();
            
            await Task.Delay(2000).ConfigureAwait(false);
            
            var statuses = new[]
            {
                $"Uptime: {(int)(DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalHours} hours",
                //$"Shard: {_discord.ShardId} / 1",
                $"{_discord.Guilds.Count} guilds",
                $"Latency: {_discord.Latency}ms",
                //$"{GC.GetTotalMemory(true) / 1000000} Megabytes used",
                $"{_discord.Guilds.Sum(guild => guild.MemberCount)} users",
                $"{_commands.Commands.Count()} commands"
                
            };
            
            await _discord.SetGameAsync($"{statuses[r.Next(statuses.Length)]} | {DefaultPrefix}help").ConfigureAwait(false);
        }

        private static async Task RunPeriodically(Action action, TimeSpan interval, CancellationToken token)
        {
            while (true)
            {
                action();
                await Task.Delay(interval, token).ConfigureAwait(false);
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
