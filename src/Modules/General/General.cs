using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using mummybot.Modules.General.Common;
using mummybot.Extensions;
using mummybot.Services;
using System.Linq;
using Discord.WebSocket;
using System.Net.Http;
using Newtonsoft.Json;

namespace mummybot.Modules.General
{
    public partial class General : ModuleBase
    {
        [Command("Yomamma"), Summary("Send a \"Yomamma\" joke to a user")]
        public async Task Yomamma(IGuildUser user = null)
        {
            if (user is null)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "Yomamma <@user>").ConfigureAwait(false);
                return;
            }
            try
            {
                using var http = new HttpClient();
                var request = await http.GetStringAsync("https://api.yomomma.info/");
                var result = JsonConvert.DeserializeObject<YoMamma>(request);
                await ReplyAsync($"{user.Mention} {result.Joke}").ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                await Context.Channel.SendErrorAsync(string.Empty, ex.Message).ConfigureAwait(false);
            }
        }

        [Command("Clap"), Summary("Clap between words")]
        public async Task Clap([Remainder] string words)
            => await ReplyAsync(words.Replace(" ", ":clap:")).ConfigureAwait(false);

        [Command("Avatar"), Alias("Av"), Summary("Display user avatar")]
        public async Task Avatar(SocketGuildUser user = null) => await ReplyAsync(string.Empty, embed: new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = Utils.FullUserName(user ?? Context.User)
            },
            ImageUrl = Extensions.UserExtensions.RealAvatarUrl(user ?? Context.User, 1024).AbsoluteUri,
            Color = Utils.GetRandomColor()
        }.Build()).ConfigureAwait(false);

        [Command("Fame"), Summary("Add a message to the Hall of Fame"),
            RequireBotPermission(GuildPermission.ManageChannels), RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Fame(IMessage msg = null)
        {
            if (msg == null)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "Missing message ID").ConfigureAwait(false);
                return;
            }

            SocketGuildChannel channel = null;

            if (Context.Guild.Channels.Any(x => x.Name.Equals("⭐hall-of-fame")))
                channel = Context.Guild.Channels.Single(x => x.Name.Equals("⭐hall-of-fame"));
            else
            {
                var prompt = await PromptUserConfirmAsync(new EmbedBuilder()
                .WithDescription("Guild does not have hall-of-fame channel\nDo you wish to create one?")).ConfigureAwait(false);

                if (!prompt) return;
                
                await Context.Guild.CreateTextChannelAsync("⭐hall-of-fame").ConfigureAwait(false);
                channel = Context.Guild.Channels.Single(x => x.Name.Equals("⭐hall-of-fame"));

                await channel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions().Modify(sendMessages: PermValue.Deny));
            }

            await Context.Guild.GetTextChannel(channel.Id)
                .SendAuthorAsync((IGuildUser)msg.Author, msg.Content, $"#{msg.Channel} • {msg.CreatedAt.UtcDateTime}")
                .ConfigureAwait(false).GetAwaiter().GetResult().AddReactionAsync(new Emoji("\uD83C\uDF1F")).ConfigureAwait(false);
        }

        [Command("Hmm")]
        public async Task Hmm()
        {
            var r = new Random();
            var quote = new Quotes().QuoteList;
            await ReplyAsync(quote[r.Next(quote.Length)]).ConfigureAwait(false);
        }

        [Command("Choose"), Summary("Choose something by random")]
        public async Task Choose([Remainder] string args)
        {
            var options = args.Split(" ");
            var r = new Random();
            await ReplyAsync(options[r.Next(options.Length)]).ConfigureAwait(false);
        }

        [Command("Snipe"), Summary("Display last deleted message")]
        public async Task Snipe()
        {
            if (MessageService.SnipeDict.ContainsKey(Context.Channel.Id))
            {
                var dict = MessageService.SnipeDict[Context.Channel.Id];
                // ReSharper disable once SpecifyACultureInStringConversionExplicitly
                await Context.Channel.SendAuthorAsync(Context.Guild.GetUser(dict.AuthorId), dict.Content, $"Sent at {dict.CreatedAt}\n• Requested by {Context.User}").ConfigureAwait(false);
                return;
            }
            await Context.Channel.SendErrorAsync(string.Empty, "Nothing to snipe", $"• Requested by {Context.User}").ConfigureAwait(false);
        }
    }
}
