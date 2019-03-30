using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using mummybot.Modules.General.Common;
using mummybot.Extensions;
using System.Linq;

namespace mummybot.Modules.General
{
    public partial class General : ModuleBase
    {
        [Command("Clap"), Summary("Clap between words")]
        public async Task Clap([Remainder] string words)
            => await ReplyAsync(words.Replace(" ", ":clap:"));

        [Command("Timezone")]
        public async Task Timezone(int page = 1)
        {
            page--;
            if (page < 0 || page > 20) return;

            var timezones = TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(x => x.BaseUtcOffset)
                .ToArray();
            var timezonesPerPage = 20;

            await Context.SendPaginatedConfirmAsync(page, (currPage) => new EmbedBuilder()
            .WithTitle("Timezones Available")
            .WithDescription(string.Join("\n", timezones.Skip(currPage * timezonesPerPage).Take(timezonesPerPage).Select(x => $"`{x.Id, -25}` {(x.BaseUtcOffset < TimeSpan.Zero ? "-" : "+")}{x.BaseUtcOffset:hhmm}"))),
            timezones.Length, timezonesPerPage).ConfigureAwait(false);
        }

        [Command("Hmm")]
        public async Task Hmm()
        {
            var r = new Random();
            var quote = new Quotes().QuoteList;
            await ReplyAsync(quote[r.Next(quote.Length)]);
        }

        [Command("Choose"), Summary("Choose something by random")]
        public async Task Choose([Remainder] string args)
        {
            var options = args.Split(" ");
            var r = new Random();
            await ReplyAsync(options[r.Next(options.Length)]);
        }
    }
}