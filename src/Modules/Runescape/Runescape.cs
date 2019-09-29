using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using mummybot.Extensions;
using System.Linq;
using mummybot.Attributes;
using System.Collections.Generic;

namespace mummybot.Modules.Runescape
{
    [Name("Runescape"), Summary("Runescape based commands")]
    public class RunescapeModule : ModuleBase<Services.StatsService>
    {
        //public Services.StatsService _statsService { get; set; }

        [Command("Araxxi"), Summary("Current rotation of Araxxi")]
        public async Task Araxxi()
        {
            var Rotations = new[]
            {
                "Minions",
                "Acid",
                "Darkness"
            };
            var epoch = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0,
                             DateTimeKind.Utc)).TotalMilliseconds;
            var currentRotation = Math.Floor((Math.Floor(Math.Floor((epoch / 1000) / (24 * 60 * 60))) + 3) % (4 * Rotations.Length) / 4);
            var daysLeft = 4 - (Math.Floor((epoch / 1000) / (24 * 60 * 60)) + 3) % (4 * Rotations.Length) % 4;
            var nextRotation = currentRotation + 1;

            if (nextRotation == Rotations.Length) nextRotation = 0;

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
            }

            var eb = new EmbedBuilder();
            await ReplyAsync(String.Empty, embed: eb.WithTitle("Araxxi Rotation")
                .WithThumbnailUrl("http://i.imgur.com/9m39UaE.png")
                .WithColor(Utils.GetRandomColor())
                .AddField("Top Path (Minions)", top)
                .AddField("Middle Path (Acid)", mid)
                .AddField("Bottom Path (Darkness)", bot)
                .WithFooter(new EmbedFooterBuilder().WithText($"Next path closed will be {Rotations[(int)nextRotation]} in {(daysLeft == 1 ? $"{daysLeft} Day": $"{daysLeft} Days")}"))
                .Build());
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
            var nextRoation = currentRotation + 1;

            var eb = new EmbedBuilder();
            await ReplyAsync(string.Empty, embed: eb
            .WithTitle("Vorago Roation")
            .AddField("Current Rotation", rotation[(int)currentRotation])
                .WithColor(Utils.GetRandomColor())
                .WithThumbnailUrl("http://i.imgur.com/e4WOs8J.png")
                .WithFooter(new EmbedFooterBuilder().WithText($"Next rotation {rotation[(int)nextRoation]} in {(daysNext == 1 ? $"{daysNext} Day" : $"{daysNext} Days")}"))
                .Build());
        }

        [Command("Stats"), Summary("Get RS3 player highscores"), Cooldown(30, true)]
        public async Task Stats(string player)
        {
            var res = await _service.GetPlayerStats(player).ConfigureAwait(false);
            if (res is null)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "Cannot find player").ConfigureAwait(false);
                return;
            }

            await Context.Channel.SendTableAsync(res.Skills.Values, str => $"{str}", 1).ConfigureAwait(false);
        }
    }
}