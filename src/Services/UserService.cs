using System;
using System.Linq;
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
        private readonly Logger _log;

        public UserService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _discord = discord;
            _context = context;
            _discord.GuildMembersDownloaded += DownloadUsers;
            _discord.GuildMemberUpdated += UserUpdated;
            _discord.UserJoined += UserJoin;
            _discord.UserLeft += UserLeft;
            _discord.GuildUpdated += GuildUpdated;

            _log = LogManager.GetCurrentClassLogger();
        }

        private async Task DownloadUsers(SocketGuild guild)
        {
            var usersContext = _context.Users.AsQueryable();
            var userIdList = await usersContext.Where(x => x.GuildId.Equals(guild.Id)).Select(x => x.UserId).ToListAsync();

            foreach (var users in guild.Users)
            {
                if (await usersContext.AnyAsync(u => u.UserId.Equals(users.Id) && u.GuildId.Equals(users.Guild.Id))) return;
                if (!userIdList.Contains(users.Id))
                {
                   var user = await _context.Users.SingleAsync(u => u.UserId.Equals(users.Id) && u.GuildId.Equals(guild.Id));
                    _context.Remove(user);
                }

                await _context.Users.AddAsync(new Users
                {
                    UserId = users.Id,
                    Username = Utils.FullUserName(users),
                    Nickname = users.Nickname,
                    GuildName = users.Guild.Name,
                    GuildId = users.Guild.Id,
                    Avatar = users.GetAvatarUrl(),
                    Joined = users.JoinedAt.Value.UtcDateTime
                });
            }
            await _context.SaveChangesAsync();
        }

        private Task UserUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            var _ = Task.Run(async () =>
            {
                var user = await _context.Users.SingleAsync(u => u.UserId.Equals(after.Id) && u.GuildId.Equals(after.Guild.Id));

                if (!before.Nickname.Equals(after.Nickname))
                {
                    user.Nickname = after.Nickname;
                    await _context.UsersAudit.AddAsync(new UsersAudit
                    {
                        UserId = after.Id,
                        GuildId = after.Guild.Id,
                        Nickname = after.Nickname
                    });
                }
                else if (!before.Username.Equals(after.Username))
                {
                    user.Username = after.Username;
                    await _context.UsersAudit.AddAsync(new UsersAudit
                    {
                        UserId = after.Id,
                        GuildId = after.Guild.Id,
                        Username = Utils.FullUserName(after)
                    });
                }
                else if (!before.Equals(after.GetAvatarUrl()))
                    user.Avatar = after.GetAvatarUrl();

                await _context.SaveChangesAsync();
            });
            return Task.CompletedTask;
        }

        private Task UserJoin(IGuildUser guildUser)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    await AddUser(guildUser);

                    var conf = await _context.Guilds.SingleAsync(g => g.GuildId.Equals(guildUser.GuildId));
                    var channel = (await guildUser.Guild.GetTextChannelsAsync().ConfigureAwait(false)).SingleOrDefault(c => c.Id.Equals(conf.GreetChl));

                    if (!string.IsNullOrWhiteSpace(conf.Greeting) && channel != null)//&& conf.GreetChl != null)
                    {
                        var greeting = conf.Greeting;
                        if (greeting.Contains("%user%")) greeting = greeting.Replace("%user%", guildUser.Mention);
                        await channel.SendMessageAsync(greeting).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) { _log.Error(ex); }
            });

            return Task.CompletedTask;
        }

        private Task UserLeft(IGuildUser guildUser)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    var conf = await _context.Guilds.SingleAsync(g => g.GuildId.Equals(guildUser.Guild.Id));
                    var channel = (await guildUser.Guild.GetTextChannelsAsync().ConfigureAwait(false)).SingleOrDefault(c => c.Id.Equals(conf.GreetChl));

                    if (!string.IsNullOrWhiteSpace(conf.Goodbye) && channel != null)
                    {
                        var goodbye = conf.Goodbye;
                        if (goodbye.Contains("%user%")) goodbye = goodbye.Replace("%user%", guildUser.Mention);
                        await channel.SendMessageAsync(goodbye).ConfigureAwait(false);
                    }
                    await RemoveUser(guildUser);
                }
                catch (Exception ex) { _log.Error(ex); }
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

        private async Task<bool> UserExists(ulong userId, ulong guildId)
            => await _context.Users.AnyAsync(u => u.UserId.Equals(userId) && u.GuildId.Equals(guildId));

        private async Task AddUser(IGuildUser user)
        {
            if (await UserExists(user.Id, user.GuildId)) return;
            else
            {
                try
                {
                    await _context.Users.AddAsync(new Users
                    {
                        UserId = user.Id,
                        Username = Utils.FullUserName((SocketUser)user),
                        Nickname = user.Nickname,
                        GuildName = user.Guild.Name,
                        GuildId = user.Guild.Id,
                        Avatar = user.GetAvatarUrl(),
                        Joined = user.JoinedAt.Value.UtcDateTime
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex) { _log.Error(ex); }
            }
        }

        private async Task RemoveUser(IGuildUser user)
        {
            if (await UserExists(user.Id, user.Guild.Id)) return;
            else
            {
                _context.Remove(await _context.Users.SingleAsync(u => u.UserId.Equals(user.Id) && u.GuildId.Equals(user.Guild.Id)));
                await _context.SaveChangesAsync();
            }
        }
    }
}