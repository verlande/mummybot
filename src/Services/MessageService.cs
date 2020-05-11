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
        private static readonly ConcurrentDictionary<ulong, Snipe> _snipeDict = new ConcurrentDictionary<ulong, Snipe>();

        public MessageService(DiscordSocketClient discord)
        {
            discord.MessageDeleted += DeletedMessage;
        }

        public static ConcurrentDictionary<ulong, Snipe> SnipeDict => _snipeDict;

        private Task DeletedMessage(Cacheable<IMessage, ulong> cachedmsg, ISocketMessageChannel msg)
        {
            //if (cachedmsg.Value.Author.IsBot || cachedmsg.Value is null) return Task.CompletedTask;
            try
            {
                if (cachedmsg.Value == null || cachedmsg.Value.Author.IsBot) return Task.CompletedTask;

                if (SnipeDict.ContainsKey(cachedmsg.Value.Channel.Id))
                    SnipeDict.TryRemove(cachedmsg.Value.Channel.Id, out _);
                SnipeDict.TryAdd(cachedmsg.Value.Channel.Id, new Snipe { AuthorId = cachedmsg.Value.Author.Id, Content = (cachedmsg.Value.Content == string.Empty) ? cachedmsg.Value.Attachments.First().Url : cachedmsg.Value.Content, CreatedAt = DateTime.UtcNow });

                return Task.CompletedTask;
            }
            catch
            {
                //ignored
            }
            return Task.CompletedTask;
        }
    }
}