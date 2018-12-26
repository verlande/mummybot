using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using mummybot.Models;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Services
{
    public class UserService
    {
        private readonly DiscordSocketClient _discord;
        private readonly mummybotDbContext _context;

        public UserService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _discord = discord;
            _context = context;
            _discord.GuildMembersDownloaded += DownloadUsers;
            _discord.GuildMemberUpdated += UserUpdated;
            _discord.UserJoined += UserJoin;
        }

        private async Task DownloadUsers(SocketGuild guild)
        {
            var usersdb = _context.Users.AsNoTracking();
            foreach (var users in guild.Users)
            {
                if (usersdb.Any(u => u.UserId.Equals(users.Id))) return;
                
                var user = new Users
                {
                    UserId = users.Id,
                    Username = $"{users.Username}#{users.Discriminator}",
                    Nickname = users.Nickname,
                    GuildName = users.Guild.Name,
                    GuildId = users.Guild.Id,
                    Avatar = users.GetAvatarUrl(),
                    Joined = users.JoinedAt.Value.UtcDateTime
                };
                _context.Users.Add(user);
            }

            await _context.SaveChangesAsync();
        }

        private async Task UserUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            var res = await _context.Users.SingleAsync(u => u.UserId.Equals(after.Id));
            if (res != null || !after.IsBot || !before.Status.Equals(after.Status))
            {
                if (!res.Nickname.Equals(after.Nickname))
                {
                    res.Nickname = after.Nickname;
                    _context.Users.Attach(res);       
                }
                else if (!res.Username.Equals(after.Username))
                {
                    res.Username = after.Username;
                    _context.Users.Attach(res);
                }
            }

            await _context.SaveChangesAsync();

        }

        private async Task UserJoin(SocketGuildUser user)
            => await _discord.GetGuild(user.Guild.Id).DefaultChannel
                .SendMessageAsync($"**{user.Username}#{user.Discriminator} has joined {user.Guild.Name}!**");
    }
}