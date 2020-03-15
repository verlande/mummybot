using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NLog;

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
        private readonly Logger _log;
        public static ConcurrentDictionary<ulong, Snipe> snipeDict = new ConcurrentDictionary<ulong, Snipe>();

        public MessageService(DiscordSocketClient discord)
        {
            discord.MessageDeleted += DeletedMessage;
        }

        private Task DeletedMessage(Cacheable<IMessage, ulong> cachedmsg, ISocketMessageChannel msg)
        {
            try
            {
                if (cachedmsg.Value.Author.IsBot) return Task.CompletedTask;
                if (snipeDict.ContainsKey(cachedmsg.Value.Channel.Id))
                    snipeDict.TryRemove(cachedmsg.Value.Channel.Id, out _);
                snipeDict.TryAdd(cachedmsg.Value.Channel.Id, new Snipe { AuthorId = cachedmsg.Value.Author.Id, Content = (cachedmsg.Value.Content == string.Empty) ? cachedmsg.Value.Attachments.First().Url : cachedmsg.Value.Content, CreatedAt = DateTime.UtcNow });            
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return Task.CompletedTask;
        }
    }
}