using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Models;
using NLog;

namespace mummybot.Services
{
    public class GuildService : INService
    {
        private readonly DiscordSocketClient _discord;
        private readonly mummybotDbContext _context;
        private readonly Logger _log = LogManager.GetLogger("logfile");

        public ConcurrentDictionary<ulong, Guilds> AllGuildConfigs { get; }

        public GuildService(DiscordSocketClient discord, mummybotDbContext context)
        {   
            _discord = discord;
            _context = context;
            _discord.JoinedGuild += JoinedGuild;
            _discord.LeftGuild += LeftGuild;
            _discord.GuildUpdated += GuildUpdated;
            _discord.Ready += Ready;

            AllGuildConfigs = new ConcurrentDictionary<ulong, Guilds>(
                _context.Guilds.Where(x => x.Active).ToDictionary(k => k.GuildId, v => v)
            );
            _log.Info($"Loaded {AllGuildConfigs.Count} guild configs");
        }

        private Task JoinedGuild(SocketGuild guild)
        {
            Task.Run(async () =>
            {
                try
                {
                    await AddGuild(new Guilds
                    {
                        GuildId = guild.Id,
                        GuildName = guild.Name,
                        Region = guild.VoiceRegionId,
                        GreetChl = 0
                    });
                }
                catch (Exception e)
                {
                    _log.Error(e);
                }
            });
            return Task.CompletedTask;
        }

        private Task LeftGuild(SocketGuild guild)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (AllGuildConfigs.TryGetValue(guild.Id, out var conf) != null)
                    {
                        var gc = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(guild.Id))
                            .ConfigureAwait(false);
                        gc.Active = false;
                        AllGuildConfigs.TryRemove(guild.Id, out _);
                        //AllGuildConfigs.AddOrUpdate(guild.Id, gc, (key, old) => gc);
                        _context.Guilds.Update(gc);
                        await _context.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e);
                }
            });
            return Task.CompletedTask;
        }
        private Task GuildUpdated(IGuild before, IGuild after)
        {
            Task.Run(async () =>
            {
                var gc = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(before.Id));

                if (!before.Name.Equals(after.Name))
                    gc.GuildName = after.Name;
                if (!before.VoiceRegionId.Equals(after.VoiceRegionId))
                    gc.Region = after.VoiceRegionId;

                _context.Update(gc);
                AllGuildConfigs.AddOrUpdate(after.Id, gc, (key, old) => gc);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            });
            return Task.CompletedTask;
        }

        private async Task AddGuild(Guilds guild)
        {
            try
            {
                var gc = await _context.Guilds.SingleOrDefaultAsync(x => x.GuildId.Equals(guild.GuildId));

                if (gc == null)
                {
                    AllGuildConfigs.TryAdd(guild.GuildId, guild);
                    await _context.Guilds.AddAsync(guild).ConfigureAwait(false);
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                    return;
                }
                
                if (!gc.Active)
                {
                    gc.Active = true;
                    AllGuildConfigs.TryAdd(guild.GuildId, guild);
                    //AllGuildConfigs.AddOrUpdate(gc.GuildId, gc, (key, old) => gc);
                    _context.Guilds.Update(gc);
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        private async Task Ready()
        {
            foreach (var guild in _discord.Guilds)
            {
                try
                {
                    //Inactive guild active

                    if (await _context.Guilds.AnyAsync(x => x.GuildId.Equals(guild.Id) && !x.Active)
                        .ConfigureAwait(false))
                    {
                        await AddGuild(new Guilds
                        {
                            GuildId = guild.Id,
                            GuildName = guild.Name,
                            Region = guild.VoiceRegionId,
                            GreetChl = 0,

                        }).ConfigureAwait(false);
                        _log.Info($"Inactive guild updated {guild.Name} ({guild.Id})");
                    }
                
                    // Missing guild
                    if (await _context.Guilds.AnyAsync(x => x.GuildId.Equals(guild.Id))
                        .ConfigureAwait(false)) continue;

                    await AddGuild(new Guilds
                    {
                        GuildId = guild.Id,
                        GuildName = guild.Name,
                        Region = guild.VoiceRegionId,
                        GreetChl = 0,

                    }).ConfigureAwait(false);
                    _log.Info($"Missing guild inserted {guild.Name} ({guild.Id})");
                }
                catch (Exception e)
                {
                    _log.Error(e);
                }
            }
        }
    }
}