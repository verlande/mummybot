using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace mummybot.Services
{
    public class Snipe
    {
        public ulong AuthorId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MessageService
    {
        private readonly DiscordSocketClient _discord;
        public static ConcurrentDictionary<ulong, Snipe> snipeDict = new ConcurrentDictionary<ulong, Snipe>();

        public MessageService(DiscordSocketClient discord)
        {
            _discord = discord;
            _discord.MessageDeleted += DeletedMessage;
        }

        private Task DeletedMessage(Cacheable<IMessage, ulong> cachedmsg, ISocketMessageChannel msg)
        {
            var _ = Task.Run(() =>
            {
                if (cachedmsg.Value.Author.IsBot) return;
                if (snipeDict.ContainsKey(cachedmsg.Value.Channel.Id))
                    snipeDict.TryRemove(cachedmsg.Value.Channel.Id, out var _);
                snipeDict.TryAdd(cachedmsg.Value.Channel.Id, new Snipe { AuthorId = cachedmsg.Value.Author.Id, Content = (cachedmsg.Value.Content == String.Empty) ? cachedmsg.Value.Attachments.First().Url : cachedmsg.Value.Content, CreatedAt = DateTime.UtcNow });
            });
            return Task.CompletedTask;
        }
    }
}