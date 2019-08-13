using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Services
{
    public class MessageService
    {
        private readonly mummybotDbContext _context;
        private readonly DiscordSocketClient _discord;

        public MessageService(mummybotDbContext context, DiscordSocketClient discord)
        {
            _discord = discord;
            _context = context;
            _discord.MessageReceived += LogMessage;
            _discord.MessageDeleted += DeletedMessage;
            _discord.MessageUpdated += UpdatedMessage;
        }

        private Task LogMessage(SocketMessage m)
        {
            var _ = Task.Run(async () =>
            {
                var msg = (SocketUserMessage)m;
                if (msg.Source != MessageSource.User) return;
                var context = new SocketCommandContext(_discord, msg);

                await _context.MessageLogs.AddAsync(new MessageLogs
                {
                    Guildid = context.Guild.Id,
                    Messageid = msg.Id,
                    Authorid = msg.Author.Id,
                    Username = Utils.FullUserName(msg.Author),
                    Channelname = msg.Channel.Name,
                    Channelid = msg.Channel.Id,
                    Content = msg.Content == String.Empty ? null : msg.Content,
                    Attachments = msg.Attachments.Select(a => a.Url).FirstOrDefault(),
                    Mentionedusers = msg.MentionedUsers.Select(u => u.Username).ToArray()
                });

                await _context.SaveChangesAsync();
            });
            return Task.CompletedTask;
        }

        private Task DeletedMessage(Cacheable<IMessage, ulong> cachedmsg, ISocketMessageChannel msg)
        {
            var _ = Task.Run(async () =>
            {
                var deletedMessages = cachedmsg.GetOrDownloadAsync();
                var message = await _context.MessageLogs.SingleAsync(m => m.Messageid.Equals(deletedMessages.Result.Id));

                message.Deleted = true;
                message.Deletedat = DateTime.UtcNow;

                _context.MessageLogs.Attach(message);
                await _context.SaveChangesAsync();
            });
            return Task.CompletedTask;
        }

        private Task UpdatedMessage(Cacheable<IMessage, ulong> cachedmsg, SocketMessage msg,
            ISocketMessageChannel chlmsg)
        {
            var _ = Task.Run(async () =>
            {
                var cachedMessage = await cachedmsg.GetOrDownloadAsync();

                var message =
                    await _context.MessageLogs.SingleAsync(m => m.Messageid.Equals(cachedMessage.Id));

                message.UpdatedContent = msg.Content;
                if (msg.MentionedUsers.Any())
                    message.Mentionedusers = msg.MentionedUsers.Select(u => u.Username).ToArray();

                _context.MessageLogs.Attach(message);
                await _context.SaveChangesAsync();
            });
            return Task.CompletedTask;
        }
    }
}