using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace mummybot.Extensions
{
    public static class Extensions
    {
        public static ReactionEventWrapper OnReaction(this IUserMessage msg, DiscordSocketClient client, Func<SocketReaction, Task> reactionAdded, Func<SocketReaction, Task> reactionRemoved = null)
        {
            if (reactionRemoved == null)
                reactionRemoved = _ => Task.CompletedTask;

            var wrap = new ReactionEventWrapper(client, msg);
            wrap.OnReactionAdded += (r) => { Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { Task.Run(() => reactionRemoved(r)); };
            return wrap;
        }

        public static bool IsAuthor(this IMessage msg, IDiscordClient client)
            => msg.Author?.Id == client.CurrentUser.Id;

        public static EmbedBuilder WithErrorColor(this EmbedBuilder eb)
            => eb.WithColor(Color.DarkRed);

        public static IMessage DeleteAfter(this IUserMessage msg, int seconds)
        {
            Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000).ConfigureAwait(false);
                await msg.DeleteAsync().ConfigureAwait(false);
            });
            return msg;
        }

        public static IEnumerable<IRole> GetRoles(this IGuildUser user)
            => user.RoleIds.Select(r => user.Guild.GetRole(r)).Where(r => r != null);
    }
}
