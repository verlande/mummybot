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

        [Command("Newusers"), Summary("Lists 10 newest users"), Alias("nu")]
        public async Task NewUsers()
        {
            var eb = new EmbedBuilder().WithTitle("New users")
                .WithColor(Utils.GetRandomColor())
                .WithFooter($"• Requested by {Context.User}");

            var users = Context.Guild.Users.ToList().Where(u => !u.IsBot).OrderByDescending(u => u.JoinedAt).Take(10);

            foreach (var user in users) eb.AddField($"{user} ({user.Id})", user.JoinedAt.Value.ToString("g"));
            await ReplyAsync(string.Empty, embed: eb.Build()).ConfigureAwait(false);
        }
    }
}
