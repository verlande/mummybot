using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace mummybot.Extensions
{
    public static class ReactionExtensions
    {
        public static ReactionEventWrapper OnReaction(this IUserMessage msg, DiscordSocketClient client,
            Func<SocketReaction, Task> reactionAdded, Func<SocketReaction, Task> reactionRemoved = null)
        {
            if (reactionRemoved == null) reactionRemoved = _ => Task.CompletedTask;
            var wrap = new ReactionEventWrapper(client, msg);
            wrap.OnReactionAdded += (r) => { Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { Task.Run(() => reactionRemoved(r)); };
            return wrap;
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
                        if (msg.Id == Message.Id) OnReactionsCleared?.Invoke();
                    }
                    catch
                    {
                        // ignored
                    }
                });
                return Task.CompletedTask;
            }

            private Task Discord_ReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
                SocketReaction reaction)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (msg.Id == Message.Id) OnReactionRemoved?.Invoke(reaction);
                    }
                    catch
                    {
                        // ignored
                    }
                });
                return Task.CompletedTask;
            }

            private Task Discord_ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
                SocketReaction reaction)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (msg.Id == Message.Id) OnReactionAdded?.Invoke(reaction);
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
}