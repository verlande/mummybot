using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using mummybot.Extensions;
using System.Linq;
using Discord.WebSocket;
using System.Text;

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
                .Build()).ConfigureAwait(false);
        }

        [Command("Finddups"), Summary("Find users with matching nicknames"), Alias("fd")]
        public async Task Finddups()
        {
            var dups = Context.Guild.Users.GroupBy(x => x.Nickname).Where(x => x.Skip(1).Any()).SelectMany(x => x).Take(10);

            var sb = new StringBuilder();

            foreach (var dup in dups)
                if (!string.IsNullOrWhiteSpace(dup.Nickname))
                    sb.AppendLine($"{Format.Bold(Utils.FullUserName(dup))} - {dup.Nickname}");
                else if (sb.Length == 0) sb.AppendLine("No duplicate nicknames");

            await Context.Channel.SendConfirmAsync(sb.ToString(), "Duplicate nicknames").ConfigureAwait(false);
        }

        [Command("Pastnicks"), Summary("Show past nicknames limited to 10"), Alias("pn")]
        public async Task LastNicks(SocketGuildUser arg)
        {
            var user = arg ?? Context.User;

            var result = Database.UsersAudit.Where(u => u.UserId.Equals(user.Id)
                && u.GuildId.Equals(Context.Guild.Id)).OrderByDescending(u => u.Id).Take(10);

            if (!result.Any())
                await Context.Channel.SendErrorAsync(string.Empty, $"No nickname history for {Utils.FullUserName(user)}").ConfigureAwait(false);
            else
            {
                var sb = new StringBuilder();
                foreach (var res in result)
                    if (!string.IsNullOrEmpty(res.Nickname))
                        sb.AppendLine($"{res.Nickname} `{res.ChangedOn}`");
                await Context.Channel.SendAuthorAsync((IGuildUser)user, sb.ToString(), $"User ID: {user.Id.ToString()}").ConfigureAwait(false);
            }
        }

        [Command("Pastusername"), Summary("Show past usernames"), Alias("pu")]
        public async Task Usernames(SocketGuildUser arg)
        {
            var user = arg ?? Context.User;

            var result = Database.UsersAudit.Where(u => u.UserId.Equals(user.Id)).Select(x => new { x.Username }).Take(10);

            if (!result.Any())
                await Context.Channel.SendErrorAsync(string.Empty, $"No username history for {Utils.FullUserName(user)}").ConfigureAwait(false);
            else
            {
                var sb = new StringBuilder();
                foreach (var res in result.Distinct())
                    if (!string.IsNullOrEmpty(res.Username)) sb.AppendLine(Format.Bold(res.Username));
                await Context.Channel.SendAuthorAsync((IGuildUser)user, sb.ToString(), $"User ID: {user.Id.ToString()}").ConfigureAwait(false);
            }
        }

        [Command("Newusers"), Summary("Lists 5 newest users"), Alias("nu")]
        public async Task NewUsers()
        {
            var eb = new EmbedBuilder().WithTitle("New users")
                .WithColor(Utils.GetRandomColor());

            var users = Context.Guild.Users.ToList().Where(u => !u.IsBot).OrderByDescending(u => u.JoinedAt).Take(10);

            foreach (var user in users) eb.AddField($"{user} ({user.Id})", user.JoinedAt);
            await ReplyAsync(string.Empty, embed: eb.Build()).ConfigureAwait(false);
        }
    }
}
