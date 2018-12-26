using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Modules
{
    [Name("Admin"), Group("Admin"), Alias("A")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class AdminModule : ModuleBase
    {
        [Command("Tagban"), Summary("Ban/Unban a user from creating tags in your guild")]
        public async Task TagBan(SocketGuildUser user)
        {
            var result = await Database.Users.SingleAsync(u => u.UserId.Equals(user.Id)
                                                   && u.GuildId.Equals(user.Guild.Id));
            if (result != null && !result.TagBanned)
            {
                result.TagBanned = true;
                Database.Users.Attach(result);

                await ReplyAsync($"{user.Nickname} has been banned from using tags");
            }
            else if (result != null && result.TagBanned)
            {
                result.TagBanned = false;
                Database.Users.Attach(result);
                await ReplyAsync($"{user.Nickname} has been unbanned from using tags");
            }

            await Database.SaveChangesAsync();
        }
    }
}