using Discord;
using Discord.WebSocket;
//using SixLabors.ImageSharp.Processing.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
            wrap.OnReactionAdded += (r) => { var _ = Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { var _ = Task.Run(() => reactionRemoved(r)); };
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
                try { await msg.DeleteAsync().ConfigureAwait(false); }
                catch { }
            });

            return msg;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items)
        {
            using (var provider = RandomNumberGenerator.Create())
            {
                var list = items.ToList();
                var n = list.Count;
                while (n > 1)
                {
                    var box = new byte[(n / Byte.MaxValue) + 1];
                    int boxSum;

                    do
                    {
                        provider.GetBytes(box);
                        boxSum = box.Sum(b => b);
                    }
                    while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                    var k = (boxSum % n);

                    var value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
                return list;
            }
        }

        public static IEnumerable<IRole> GetRoles(this IGuildUser user)
            => user.RoleIds.Select(r => user.Guild.GetRole(r)).Where(r => r != null);
    }
}
