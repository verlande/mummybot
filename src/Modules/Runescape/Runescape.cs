using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using mummybot.Extensions;
using System.Linq;
using mummybot.Attributes;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace mummybot.Modules.Runescape
{
    [Name("Runescape"), Summary("Runescape based commands")]
    public class RunescapeModule : ModuleBase<Services.StatsService>
    {
        [Command("Araxxi"), Summary("Current rotation of Araxxi")]
        public async Task Araxxi()
        {
            var rotations = new[]
            {
                "Minions",
                "Acid",
                "Darkness"
            };
            var epoch = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0,
                             DateTimeKind.Utc)).TotalMilliseconds;
            var currentRotation = Math.Floor((Math.Floor(Math.Floor((epoch / 1000) / (24 * 60 * 60))) + 3) % (4 * rotations.Length) / 4);
            var daysLeft = 4 - (Math.Floor((epoch / 1000) / (24 * 60 * 60)) + 3) % (4 * rotations.Length) % 4;
            var nextRotation = currentRotation + 1;

            if ((int)nextRotation == rotations.Length) nextRotation = 0;

            var top = "OPEN";
            var mid = "OPEN";
            var bot = "OPEN";

            switch (currentRotation)
            {
                case 0:
                    top = "CLOSED";
                    break;
                case 1:
                    mid = "CLOSED";
                    break;
                case 2:
                    bot = "CLOSED";
                    break;
                default:
                    throw new InvalidOperationException();
            }

            await ReplyAsync(string.Empty, embed: new EmbedBuilder().WithTitle("Araxxi Rotation")
                .WithThumbnailUrl("http://i.imgur.com/9m39UaE.png")
                .WithColor(Utils.GetRandomColor())
                .AddField("Top Path (Minions)", top)
                .AddField("Middle Path (Acid)", mid)
                .AddField("Bottom Path (Darkness)", bot)
                .WithFooter(new EmbedFooterBuilder().WithText($"Next path closed will be {rotations[(int)nextRotation]} in {((int)daysLeft == 1 ? $"{(int)daysLeft} Day" : $"{(int)daysLeft} Days")}"))
                .Build()).ConfigureAwait(false);
        }

        [Command("Vorago"), Summary("Current rotation of Vorago")]
        public async Task Vorago()
        {
            var rotation = new[]
            {
                "Ceiling Collapse",
                "Scopulus",
                "Vitalis",
                "Green Bomb",
                "Team Split",
                "The End"
            };

            var epochNow = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0,
                                DateTimeKind.Utc)).TotalMilliseconds / 1000;
            var currentRotation = Math.Floor(((Math.Floor(Math.Floor(epochNow) / (24 * 60 * 60))) - 6) % (7 * rotation.Length) / 7);
            var daysNext = 7 - ((Math.Floor(epochNow / (24 * 60 * 60))) - 6) % (7 * rotation.Length) % 7;

            await ReplyAsync(string.Empty, embed: new EmbedBuilder()
            .WithTitle("Vorago Roation")
            .AddField("Current Rotation", rotation[(int)currentRotation])
                .WithColor(Utils.GetRandomColor())
                .WithThumbnailUrl("http://i.imgur.com/e4WOs8J.png")
                .WithFooter(new EmbedFooterBuilder().WithText($"Next rotation {rotation[(int)currentRotation + 1]} in {((int)daysNext == 1 ? $"{(int)daysNext} Day" : $"{daysNext} Days")}"))
                .Build()).ConfigureAwait(false);
        }

        [Command("Stats"), Summary("Get RS3 player highscores"), Cooldown(30, true)]
        public async Task Stats([Remainder]string player)
        {
            if (player.Length > 12) return;
            var res = await _service.GetPlayerStats(player).ConfigureAwait(false);

            if (res is null)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "No player found").ConfigureAwait(false);
                return;
            }

            var sb = new System.Text.StringBuilder();

            sb.AppendFormat("{0, -15} | {1, -5} | {2, -6} | {3, -12}\n", "Skill", "Level", "Rank", "Xp");

            foreach (var r in res.Skills.Values.ToList())
                sb.AppendFormat("{0, -15} | {1, -5} | {2, -6} | {3, -12:n0}\n", r.Name, r.Level, r.Rank, r.Experience);

            await ReplyAsync(Format.Code($"\t\t{res.Name} Highscores\n\n{sb}", "css")).ConfigureAwait(false);
        }
    }
}
