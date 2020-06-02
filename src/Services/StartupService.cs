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
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly mummybotDbContext _context;
        private readonly IServiceProvider _provider;
        private readonly Logger _log;
        private string DefaultPrefix { get; set; }

        public StartupService(DiscordSocketClient discord, CommandService commands, IServiceProvider provider, mummybotDbContext context)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _context = context;
            _log = LogManager.GetCurrentClassLogger();

#if DEBUG
            DefaultPrefix = new ConfigService().Config["prefix"];
#else
            DefaultPrefix = Environment.GetEnvironmentVariable("PREFIX");
#endif
            var cancellationToken = new CancellationToken();
            // ReSharper disable once UnusedVariable
            var timerTask = RunPeriodically(Status, TimeSpan.FromSeconds(25), cancellationToken);

            _discord.Disconnected += Disconnected;
            _discord.Connected += Connected;
        }
        
        private async Task Connected()
        {
            // Loop through guilds added when offline
            foreach (var guilds in _discord.Guilds)
            {
                if (await _context.Guilds.AnyAsync(x => x.GuildId.Equals(guilds.Id) && x.OwnerId.Equals(guilds.OwnerId))) return;
                if (await _context.Guilds.AnyAsync(x => x.GuildId.Equals(guilds.Id) && !x.OwnerId.Equals(guilds.OwnerId)))
                {
                    var guild = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(guilds.Id));
                    guild.OwnerId = guilds.OwnerId;
                    _context.Guilds.Update(guild);
                    await _context.SaveChangesAsync();
                    _log.Info($"Updated guild owner {guilds.Name} ({guilds.Id})");

                }

                await _context.Guilds.AddAsync(new Guilds
                {
                    GuildId = guilds.Id,
                    GuildName = guilds.Name,
                    OwnerId = guilds.OwnerId,
                    Region = guilds.VoiceRegionId
                });

                await _context.SaveChangesAsync();
                _log.Info($"Inserted missing guild {guilds.Name} ({guilds.Id})");
            }

            var clientReady = new TaskCompletionSource<bool>();
            Task SetClientReady()
            {
                Task.Run(async () =>
                {
                    clientReady.TrySetResult(true);
                    try
                    {
                        foreach (var chan in (await _discord.GetDMChannelsAsync().ConfigureAwait(false)))
                            await chan.CloseAsync().ConfigureAwait(false);
                    }
                    catch {}
                });
                return Task.CompletedTask;
            }

            // Update inactive guilds
            //_context.Guilds.Where(x => !_discord.Guilds.Select(x => x.Id).Contains(x.GuildId) && x.Active/*!guildIds.Contains(x.GuildId)*/)
            //    .ToList()
            //    .Select(x => { x.Active = false; Console.WriteLine($"Set guild {x.GuildId} to inactive"); return x; })
            //    .ToList();
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
            await _commands.AddModulesAsync(this.GetType().GetTypeInfo().Assembly, _provider);
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
