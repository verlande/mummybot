using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using mummybot.Extensions;
using System.Linq;
using Discord.WebSocket;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Modules.Utility
{
    public partial class Utility
    {
        [Command("Playing"), Summary("List playing games")]
        public async Task Playing()
        {
            var sb = new StringBuilder();
            var playing = Context.Guild.Users.Where(x => x.Activity != null && !x.IsBot).GroupBy(x => new { x.Mention, x.Activity });

            foreach (var game in playing)
                sb.AppendLine($"{game.Key.Mention} | ***{game.Key.Activity.Name}***");

            await ReplyAsync(string.Empty, embed: new EmbedBuilder()
                .WithTitle($"{playing.Count()} users playing games")
                .WithThumbnailUrl("https://www.shareicon.net/data/128x128/2016/05/15/765355_multimedia_512x512.png")
                .WithDescription(sb.ToString())
                .WithColor(Utils.GetRandomColor())
                .Build());
        }

        [Command("Finddups"), Summary("Find users with matching nicknames")]
        public async Task Finddups()
        {

            var dups = Context.Guild.Users.GroupBy(x => x.Nickname).Where(x => x.Skip(1).Any()).SelectMany(x => x);

            var sb = new StringBuilder();

            foreach (var dup in dups)
                if (!string.IsNullOrWhiteSpace(dup.Nickname))
                    sb.AppendLine($"{Format.Bold(Utils.FullUserName(dup))} - {dup.Nickname}");
                else if (sb.Length == 0) sb.AppendLine("No duplicate nicknames");

            await Context.Channel.SendConfirmAsync(sb.ToString(), "Duplicate nicknames");
        }

        [Command("Lastnicks"), Summary("Lists 10 nickname changes of a user")]
        public async Task LastNicks(SocketGuildUser user)
        {
            var result = Database.UsersAudit.Where(u => u.UserId.Equals(user.Id)
                && u.GuildId.Equals(Context.Guild.Id)).OrderByDescending(u => u.Id).Take(10);

            if (!result.Any())
                await Context.Channel.SendErrorAsync(string.Empty, $"No nickname history for {Utils.FullUserName(user)}");
            else
            {
                var sb = new StringBuilder();
                await result.ForEachAsync(n => sb.AppendLine(n.Nickname));
                await Context.Channel.SendAuthorAsync(user, sb.ToString(), "User ID: " + user.Id.ToString());
            }
        }

        [Command("Newusers"), Summary("Lists 5 newest users")]
        public async Task NewUsers()
        {
            //var users = Database.Users.Where(u => u.GuildId.Equals(Context.Guild.Id))
            //    .Select(u => new { u.Username, u.UserId, u.Joined }).Take(5).OrderByDescending(u => u.Joined);

            var eb = new EmbedBuilder().WithTitle("New users")
                .WithColor(Utils.GetRandomColor());

            //await users.ForEachAsync(u => eb.AddField($"{u.Username} ({u.UserId})", u.Joined));

            var users = Context.Guild.Users.ToList().Where(u => !u.IsBot).OrderByDescending(u => u.JoinedAt).Take(5);

            foreach (var user in users) eb.AddField($"{user.Username} ({user.Id})", user.JoinedAt);
            await ReplyAsync(string.Empty, embed: eb.Build());
        }
    }
}
