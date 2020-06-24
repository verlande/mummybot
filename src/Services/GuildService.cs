using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly Logger _log;

        private ConcurrentDictionary<ulong, Guilds> AllGuildConfigs { get; set; }

        public GuildService(DiscordSocketClient discord, mummybotDbContext context)
        {   
            _discord = discord;
            _context = context;
            _discord.JoinedGuild += JoinedGuild;
            _discord.LeftGuild += LeftGuild;
            _discord.UserJoined += UserJoined;
            _discord.UserLeft += UserLeft;

            _log = LogManager.GetCurrentClassLogger();

            AllGuildConfigs = new ConcurrentDictionary<ulong, Guilds>(
                _context.Guilds.ToDictionary(k => k.GuildId, v => v)
            );
        }

        private Task JoinedGuild(SocketGuild guild)
        {
            Task.Run(async () =>
            {
                if (!AllGuildConfigs.ContainsKey(guild.Id))
                {
                    await AddGuild(new Guilds
                    {
                        GuildId = guild.Id,
                        GuildName = guild.Name,
                        OwnerId = guild.OwnerId,
                        Region = guild.VoiceRegionId
                    }).ConfigureAwait(false);
                }
                try
                {
                    await guild.DefaultChannel.SendMessageAsync(
                            $"Thanks for inviting me, use `{CommandHandlerService.DefaultPrefix}help` for my commands")
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            });
            
            return Task.CompletedTask;
        }

        private Task LeftGuild(SocketGuild guild)
        {
            Task.Run(async () =>
            {
                AllGuildConfigs.TryRemove(guild.Id, out _);
                var isActive = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(guild.Id));
                isActive.Active = false;
                _context.Guilds.Update(isActive);
                await _context.SaveChangesAsync();

            });
            return Task.CompletedTask;
        }

        private Task UserJoined(IGuildUser user)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (AllGuildConfigs.TryGetValue(user.GuildId, out var cfg))
                    {
                        var channel = await user.Guild.GetTextChannelAsync(cfg.BotChannel).ConfigureAwait(false);
                        if (channel != null)
                        {
                            var greeting = cfg.Greeting.Replace("%user%", user.Mention);
                            await channel.SendMessageAsync(greeting).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            });
            return Task.CompletedTask;
        }

        private Task UserLeft(IGuildUser user)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (AllGuildConfigs.TryGetValue(user.GuildId, out var cfg))
                    {
                        var channel = await user.Guild.GetTextChannelAsync(cfg.BotChannel).ConfigureAwait(false);
                        if (channel != null)
                        {
                            var goodbye = cfg.Goodbye.Replace("%user%", user.Mention);
                            await channel.SendMessageAsync(goodbye).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            });
            return Task.CompletedTask;
        }

        private Task GuildUpdated(IGuild before, IGuild after)
        {
            Task.Run(async () =>
            {
                var guild = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(before.Id));

                if (!before.Name.Equals(after.Name))
                    guild.GuildName = after.Name;
                if (!before.OwnerId.Equals(after.OwnerId))
                    guild.OwnerId = after.OwnerId;
                if (!before.VoiceRegionId.Equals(after.VoiceRegionId))
                    guild.Region = after.VoiceRegionId;

                _context.Update(guild);
                await _context.SaveChangesAsync();
            });
            return Task.CompletedTask;
        }

        private async Task AddGuild(Guilds guild)
        {
            var exists = await _context.Guilds.AnyAsync(x => x.GuildId == guild.GuildId);
            try
            {
                if (exists)
                {
                    var gc = await _context.Guilds.SingleAsync(x => x.GuildId == guild.GuildId);
                    gc.Active = true;
                    _context.Guilds.Update(guild);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    AllGuildConfigs.TryAdd(guild.GuildId, guild);
                    _context.Guilds.Update(guild);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.InnerException);
            }
        }

        private async Task SaveUsers(IEnumerable<SocketGuildUser> guildUsers)
        {
            try
            {
                foreach (var users in guildUsers)
                {
                    if (await _context.Users.AnyAsync(u =>
                        u.UserId.Equals(users.Id) && u.GuildId.Equals(users.Guild.Id))) return;
                    await _context.Users.AddAsync(new Users
                    {
                        UserId = users.Id,
                        Username = Utils.FullUserName(users),
                        Nickname = users.Nickname,
                        GuildId = users.Guild.Id,
                        Joined = users.JoinedAt.Value.UtcDateTime
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.Error(ex.InnerException);
            }
        }
    }
}