using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mummybot.Extensions
{
    public static class MessageChannelExtensions
    {
        public static Task<IUserMessage> EmbedAsync(this IMessageChannel ch, EmbedBuilder embed, string msg = "")
            => ch.SendMessageAsync(msg, embed: embed.Build(),
            options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string title, string error, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithColor(Color.Red).WithDescription(error)
                .WithTitle("Error " + title);

            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));

            return ch.SendMessageAsync(string.Empty, embed: eb.Build());
        }

        // ReSharper disable once MethodOverloadWithOptionalParameter
        public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string title, string text, string url = null, string footer = null, SocketGuildUser author = null)
        {
            var eb = new EmbedBuilder().WithColor(Utils.GetRandomColor()).WithDescription(text)
                .WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            if (author != null) eb.WithAuthor(efa =>
            {
                efa.IconUrl = author.GetAvatarUrl();
                efa.Name = Utils.FullUserName(author);
            });
            return ch.SendMessageAsync(string.Empty, embed: eb.Build());
        }

        public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string text, string title = null)
             => ch.SendMessageAsync(string.Empty, embed: new EmbedBuilder().WithColor(Utils.GetRandomColor()).WithDescription(text).WithTitle(title).Build());

        public static Task<IUserMessage> SendAuthorAsync(this IMessageChannel ch, IGuildUser author, string description, string footer = null)
            => ch.SendMessageAsync(string.Empty, embed: new EmbedBuilder().WithAuthor(new EmbedAuthorBuilder()
                .WithName(Utils.FullUserName((SocketUser)author)).WithIconUrl(author.GetAvatarUrl())).WithDescription(description).WithColor(Utils.GetRandomColor())
                .WithFooter(fb => fb.WithText(footer)).Build());

        private static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, string seed, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3)
        {
            var i = 0;
            return ch.SendMessageAsync($@"```css
                {string.Join("\n", items.GroupBy(item => (i++) / columns).Select(ig => string.Concat(ig.Select(el => howToPrint(el)))))}```");
        }

        public static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3) =>
            ch.SendTableAsync("", items, howToPrint, columns);
        
        private static readonly IEmote ArrowLeft = new Emoji("⬅");
        private static readonly IEmote ArrowRight = new Emoji("➡");

        public static Task SendPaginatedConfirmAsync(this ICommandContext ctx,
            int currentPage, Func<int, EmbedBuilder> pageFunc, int totalElements,
            int itemsPerPage, bool addPaginatedFooter = true)
            => ctx.SendPaginatedConfirmAsync(currentPage,
                (x) => Task.FromResult(pageFunc(x)), totalElements, itemsPerPage, addPaginatedFooter);

        private static async Task SendPaginatedConfirmAsync(this ICommandContext ctx, int currentPage, 
            Func<int, Task<EmbedBuilder>> pageFunc, int totalElements, int itemsPerPage, bool addPaginatedFooter = true)
        {
            var embed = await pageFunc(currentPage).ConfigureAwait(false);

            var lastPage = (totalElements - 1) / itemsPerPage;

            if (addPaginatedFooter)
                embed.AddPaginatedFooter(currentPage, lastPage);

            var msg = await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);

            if (lastPage == 0)
                return;

            await msg.AddReactionAsync(ArrowLeft).ConfigureAwait(false);
            await msg.AddReactionAsync(ArrowRight).ConfigureAwait(false);

            await Task.Delay(2000).ConfigureAwait(false);

            var lastPageChange = DateTime.MinValue;

            async Task ChangePage(SocketReaction r)
            {
                try
                {
                    if (r.UserId != ctx.User.Id)
                        return;
                    if (DateTime.UtcNow - lastPageChange < TimeSpan.FromSeconds(1))
                        return;
                    if (r.Emote.Name == ArrowLeft.Name)
                    {
                        if (currentPage == 0)
                            return;
                        lastPageChange = DateTime.UtcNow;
                        var toSend = await pageFunc(--currentPage).ConfigureAwait(false);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                    else if (r.Emote.Name == ArrowRight.Name)
                    {
                        if (lastPage > currentPage)
                        {
                            lastPageChange = DateTime.UtcNow;
                            var toSend = await pageFunc(++currentPage).ConfigureAwait(false);
                            if (addPaginatedFooter)
                                toSend.AddPaginatedFooter(currentPage, lastPage);
                            await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception)
                {
                    //ignored
                }
            }

            using (msg.OnReaction((DiscordSocketClient)ctx.Client, ChangePage, ChangePage))
            {
                await Task.Delay(30000).ConfigureAwait(false);
            }

            try
            {
                if (msg.Channel is ITextChannel &&
                    ((SocketGuild) ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
                    await msg.RemoveAllReactionsAsync().ConfigureAwait(false);
                else
                    await Task.WhenAll(msg.Reactions.Where(x => x.Value.IsMe)
                        .Select(x => msg.RemoveReactionAsync(x.Key, ctx.Client.CurrentUser)));
            }
            catch
            {
                // ignored
            }
        }

        private static EmbedBuilder AddPaginatedFooter(this EmbedBuilder embed, int curPage, int? lastPage) 
            => lastPage != null ? embed.WithFooter(efb => efb.WithText($"{curPage + 1} / {lastPage + 1}")) : embed.WithFooter(efb => efb.WithText(curPage.ToString()));
    }

    public sealed class ReactionEventWrapper : IDisposable
    {
        private IUserMessage Message { get; }
        public event Action<SocketReaction> OnReactionAdded = delegate { };
        public event Action<SocketReaction> OnReactionRemoved = delegate { };
        public event Action OnReactionsCleared = delegate { };

        public ReactionEventWrapper(DiscordSocketClient client, IUserMessage msg)
        {
            Message = msg ?? throw new ArgumentNullException(nameof(msg));
            _client = client;

            _client.ReactionAdded += Discord_ReactionAdded;
            _client.ReactionRemoved += Discord_ReactionRemoved;
            _client.ReactionsCleared += Discord_ReactionsCleared;
        }

        private Task Discord_ReactionsCleared(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel)
        {
            Task.Run(() =>
            {
                try
                {
                    if (msg.Id == Message.Id)
                        OnReactionsCleared?.Invoke();
                }
                catch
                {
                    // ignored
                }
            });

            return Task.CompletedTask;
        }

        private Task Discord_ReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Task.Run(() =>
            {
                try
                {
                    if (msg.Id == Message.Id)
                        OnReactionRemoved?.Invoke(reaction);
                }
                catch
                {
                    // ignored
                }
            });

            return Task.CompletedTask;
        }

        private Task Discord_ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Task.Run(() =>
            {
                try
                {
                    if (msg.Id == Message.Id)
                        OnReactionAdded?.Invoke(reaction);
                }
                catch
                {
                    // ignored
                }
            });

            return Task.CompletedTask;
        }

        private void UnsubAll()
        {
            _client.ReactionAdded -= Discord_ReactionAdded;
            _client.ReactionRemoved -= Discord_ReactionRemoved;
            _client.ReactionsCleared -= Discord_ReactionsCleared;
            OnReactionAdded = null;
            OnReactionRemoved = null;
            OnReactionsCleared = null;
        }

        private readonly DiscordSocketClient _client;

        public void Dispose() => UnsubAll();
    }
}