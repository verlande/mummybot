using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using mummybot.Modules.General.Common;
using mummybot.Extensions;
using mummybot.Attributes;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace mummybot.Modules.General
{
    public partial class General
    {
        [Command("Bible"), Remarks("£bible exodus 21:15"), Cooldown(10, true)]
        public async Task Bible(string passage = null, string chapverse = null)
        {
            const string bibleUrl = "https://labs.bible.org/api/?passage=";
            const string bibleIcon = "https://cdn.iconscout.com/icon/premium/png-256-thumb/bible-1720260-1460821.png";
            const string bibleGate = "https://www.biblegateway.com/passage/?search=";

            try
            {
                using var http = new HttpClient();
                string res;
                if (passage != null)
                    res = await http.GetStringAsync($"{bibleUrl}{passage} {chapverse}&type=json").ConfigureAwait(false);
                else
                    res = await http.GetStringAsync($"{bibleUrl}random&type=json").ConfigureAwait(false);

                var verse = JsonConvert.DeserializeObject<Bible[]>(res);

                var eb = new EmbedBuilder();
                eb.WithAuthor(eab =>
                    eab.WithName($"{verse[0].Bookname} {verse[0].Chapter}:{verse[0].Verse}")
                        .WithUrl($"{bibleGate}{verse[0].Bookname}+{verse[0].Chapter}:{verse[0].Verse}&version=ISV"))
                        .WithThumbnailUrl(bibleIcon)
                        .WithColor(Utils.GetRandomColor());
                        eb.WithDescription(verse[0].Text);

                await ReplyAsync(string.Empty, embed: eb.Build()).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "Not found").ConfigureAwait(false);
            }
        }

        [Command("Xkcd"), Summary("Xkcd comics"), Cooldown(10, true)]
        public async Task Xkcd(string comicnum = null)
        {
            var num = Convert.ToInt32(comicnum);
            if (num < 0) return;

            const string xkcdUrl = "https://xkcd.com";

            try
            {
                using var http = new HttpClient();
                string res;
                if (comicnum == null)
                    res = await http.GetStringAsync($"{xkcdUrl}/info.0.json").ConfigureAwait(false);
                else
                    res = await http.GetStringAsync($"{xkcdUrl}/{num}/info.0.json").ConfigureAwait(false);

                var comic = JsonConvert.DeserializeObject<Xkcd>(res);

                var eb = new EmbedBuilder();

                void Action(EmbedAuthorBuilder eab) => eab.WithName(comic.Title)
                    .WithUrl($"{xkcdUrl}/{comic.Num}")
                    .WithIconUrl("http://xkcd.com/s/919f27.ico");

                eb.WithAuthor(Action)
                    .WithColor(Utils.GetRandomColor())
                    .WithImageUrl(comic.ImageLink)
                    .AddField(
                        efb => efb.WithName("Comic number").WithValue(comic.Num.ToString()).WithIsInline(true))
                    .AddField(efb =>
                        efb.WithName("Date").WithValue($"{comic.Day}/{comic.Month}/{comic.Year}").WithIsInline(true))
                    .AddField(efb => efb.WithName("Title").WithValue(comic.Title).WithIsInline(false));

                await ReplyAsync(string.Empty, embed: eb.Build()).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "Comic not found").ConfigureAwait(false);
            }
        }

        [Command("Urban"), Cooldown(30, true), Summary("Search Urban Dictionary")]
        public async Task Urban([Remainder]string word)
        {
            var page = 1;
            page--;
            const int urbanPerPage = 1;

            using var http = new HttpClient();
            var res = JObject.Parse(await http.GetStringAsync("http://api.urbandictionary.com/v0/define?term=" + word).ConfigureAwait(false));

            var jArr = (JArray)res["list"];
            var urbanList = jArr.ToObject<IList<List>>().ToArray();

            if (urbanList.Length == 0)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "Word not found").ConfigureAwait(false);
                return;
            }

            if (page < 0 || page > 20) return;

            await Context.SendPaginatedConfirmAsync(page, (currPage) => new EmbedBuilder()
                .WithTitle($"Urban Dictionary - {word}")
                .WithUrl(urbanList[currPage].Permalink.ToString())
                .WithColor(Utils.GetRandomColor())
                .WithThumbnailUrl("https://storage.googleapis.com/burbcommunity-morethanthecurve/2013/09/urban-dictionary-logo.gif")
                .WithDescription(string.Join("\n", urbanList.Skip(currPage * urbanPerPage).Take(urbanPerPage).Select(x => x.Definition)))
                .AddField("Example", string.Join("\n", string.Join("\n", urbanList.Skip(currPage * urbanPerPage).Take(urbanPerPage).Select(x => x.Example))))
                .AddField("Thumbs", $"👍{urbanList[currPage].ThumbsUp}\t👎{urbanList[currPage].ThumbsDown}"),
                urbanList.Length, urbanPerPage).ConfigureAwait(false);
        }
    }
}
