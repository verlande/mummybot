using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using mummybot.Models;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace mummybot.Services
{
    public class UserService
    {
        private readonly DiscordSocketClient _discord;
        private readonly mummybotDbContext _context;
        protected readonly Logger _log = LogManager.GetLogger("logfile");

        public UserService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _discord = discord;
            _context = context;
            _discord.GuildMemberUpdated += UserUpdated;
            _discord.GuildUpdated += GuildUpdated;
        }

        private Task UserUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            Task.Run(async () =>
            {
                if (before.IsBot) return;
                
                try
                {   
                    if (!before.Nickname.Equals(after.Nickname) && !string.IsNullOrEmpty(after.Nickname))
                        await _context.UsersAudit.AddAsync(new UsersAudit
                        {
                            UserId = after.Id,
                            Nickname = after.Nickname,
                            GuildId = after.Guild.Id
                        });

                    if (!before.Username.Equals(after.Username))
                        await _context.UsersAudit.AddAsync(new UsersAudit
                        {
                            UserId = after.Id,
                            Username = Utils.FullUserName(after),
                            GuildId = after.Guild.Id
                        });
                    
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            });
            return Task.CompletedTask;
        }


        private async Task GuildUpdated(SocketGuild before, SocketGuild after)
        {
            if (before.Name.Equals(after.Name) || before.OwnerId.Equals(after.OwnerId)) return;

            var ownerId = await _context.Guilds.SingleAsync(g => g.OwnerId.Equals(after.OwnerId));
            if (ownerId == null)
            {
                ownerId.OwnerId = after.OwnerId;
                ownerId.GuildName = after.Name;
                _context.Update(ownerId);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UserExists(ulong userId, ulong guildId) 
            => await _context.Users.AnyAsync(u => u.UserId.Equals(userId) && u.GuildId.Equals(guildId));

        public async Task AddUser(IGuildUser user)
        {
            if (await UserExists(user.Id, user.GuildId).ConfigureAwait(false)) return;
            try
            {
                await _context.Users.AddAsync(new Users
                {
                    UserId = user.Id,
                    Username = Utils.FullUserName((SocketUser)user),
                    Nickname = user.Nickname,
                    GuildId = user.Guild.Id,
                    Joined = user.JoinedAt.Value.UtcDateTime
                });
                
                await _context.SaveChangesAsync();
                //_log.Info($"Inserted User {Utils.FullUserName((SocketUser)user)} ({user.Id})");
            }
            catch (Exception ex) { _log.Error(ex); }
        }

        private async Task RemoveUser(IGuildUser user)
        {
            if (await UserExists(user.Id, user.Guild.Id).ConfigureAwait(false)) return;
            _context.Remove(await _context.Users.SingleAsync(u => u.UserId.Equals(user.Id) && u.GuildId.Equals(user.Guild.Id)));
            await _context.SaveChangesAsync();
        }
    }
}