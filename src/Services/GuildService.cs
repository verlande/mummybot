using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Models;
using NLog;

namespace mummybot.Services
{
    public class GuildService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private mummybotDbContext _context;
        private Logger _log;
        public static ConcurrentDictionary<ulong, bool> GuildMsgLogging = new ConcurrentDictionary<ulong, bool>();

        public GuildService(DiscordSocketClient discord, CommandService commands, mummybotDbContext context)
        {
            _discord = discord;
            _commands = commands;
            _context = context;
            _discord.JoinedGuild += JoinedGuild;
            _discord.LeftGuild += LeftGuild;
            _discord.GuildUpdated += GuildUpdated;

            _context.Guilds.AsQueryable().ForEachAsync(x => GuildMsgLogging.TryAdd(x.GuildId, x.MessageLogging));

            _log = LogManager.GetCurrentClassLogger();
        }

        private Task JoinedGuild(SocketGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                var guildExists = await _context.Guilds.SingleAsync(x => x.GuildId == guild.Id);
                if (guildExists == null)
                {
                    await Save(new Guilds
                    {
                        GuildId = guild.Id,
                        GuildName = guild.Name,
                        OwnerId = guild.OwnerId,
                        Region = guild.VoiceRegionId
                    });
                    await _discord.GetGuild(guild.Id).DefaultChannel.SendMessageAsync("morning");
                }
                else
                {
                    guildExists.Active = true;
                    await Save(guildExists);
                }
                await _discord.GetGuild(guild.Id).DefaultChannel.SendMessageAsync("ty for adding me back");
                await SaveUsers(guild.Users.ToList());
            });
            return Task.CompletedTask;
        }

        private Task LeftGuild(SocketGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                var isActive = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(guild.Id));
                isActive.Active = false;
                _context.Guilds.Attach(isActive);
                await _context.SaveChangesAsync();
            });
            return Task.CompletedTask;
        }

        private Task GuildUpdated(SocketGuild before, SocketGuild after)
        {
            var _ = Task.Run(async () =>
            {
                var guild = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(before.Id));

                if (!before.Name.Equals(after.Name))
                    guild.GuildName = after.Name;
                if (!before.OwnerId.Equals(after.OwnerId))
                    guild.OwnerId = after.OwnerId;
                if (!before.VoiceRegionId.Equals(after.VoiceRegionId))
                    guild.Region = after.VoiceRegionId;

                _context.Attach(guild);
                await _context.SaveChangesAsync();
            });
            return Task.CompletedTask;
        }

        private async Task Save(Guilds guild)
        {
            try
            {
                _context.Guilds.Attach(guild);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.Error(ex.InnerException);
            }
        }

        private async Task SaveUsers(List<SocketGuildUser> guildUsers)
        {
            try
            {
                foreach (var users in guildUsers)
                {
                    if (await _context.Users.AnyAsync(u => u.UserId.Equals(users.Id) && u.GuildId.Equals(users.Guild.Id))) return;
                    await _context.Users.AddAsync(new Users
                    {
                        UserId = users.Id,
                        Username = Utils.FullUserName(users),
                        Nickname = users.Nickname,
                        //GuildName = users.Guild.Name,
                        GuildId = users.Guild.Id,
                        Avatar = users.GetAvatarUrl(),
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