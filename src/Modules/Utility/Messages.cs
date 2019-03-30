using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mummybot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Modules.Utility
{
    public partial class Utility : ModuleBase
    {
        [Command("Snipe"), Summary("Display last deleted message")]
        public async Task Snipe()
        {
            var message = await Database.MessageLogs.Where(m => m.Guildid.Equals(Context.Guild.Id) && m.Deleted && m.Channelid.Equals(Context.Channel.Id))
                .LastAsync();

            if (message == null) await Context.Channel.SendErrorAsync(string.Empty, "No message to snipe");

            var user = Context.Guild.GetUser(message.Authorid);
            string createdAt;

            if (message.Createdat.Date == DateTime.Today.Date) createdAt = $"Today at {message.Createdat.ToShortTimeString()}";
            else if (message.Createdat.Date == DateTime.Today.AddDays(-1)) createdAt = $"Yesterday at {message.Createdat.ToShortTimeString()}";
            else createdAt = $"{message.Createdat.ToShortDateString()} - {message.Createdat.ToShortTimeString()}";

            await Context.Channel.SendAuthorAsync(user, message.UpdatedContent ?? message.Content, createdAt);
        }

        [Command("delmsg"), Summary("List's deleted messages of a user")]
        public async Task DeletedMessages(SocketGuildUser user)
        {
            if (user.IsBot) return;

            var messages = Database.MessageLogs.Where(m => m.Authorid.Equals(user.Id) && m.Guildid.Equals(Context.Guild.Id) && m.Deleted).Take(5);
            var sb = new StringBuilder();

            foreach (var msg in messages)
            {
                if (msg.Content.Length > 50)
                    sb.AppendLine($"MSG ID ({msg.Messageid})\n" + msg.Content.Substring(0, msg.Content.Length / 2) + "...\n");
                else
                    sb.AppendLine($"MSG ID ({msg.Messageid})\n" + msg.Content + "\n");
            }

            await Context.Channel.SendConfirmAsync(string.Empty, sb.ToString(), footer: $"More info £source <message id>", author: user);
        }

        [Command("Source"), Summary("Fetch info about a message using message id")]
        public async Task Source([Summary("Message id")] ulong id)
        {
            var msg = await Database.MessageLogs.FirstOrDefaultAsync(m => m.Messageid.Equals(id)
            && m.Guildid.Equals(Context.Guild.Id));
            var user = Context.Guild.GetUser(msg.Authorid);

            if (msg == null) await ReplyAsync("Message doesn't exist\n" +
                                              "Messages after 2 weeks are automatically deleted");

            string jumpToMessage = null;
            if (!msg.Deleted) jumpToMessage = $"[Jump to message](https://discordapp.com/channels/{Context.Guild.Id}/{msg.Channelid}/{msg.Messageid})";

            var eb = new EmbedBuilder
            {
                Description = $"{Format.Bold("Message posted in")} {Format.Italics($"<#{msg.Channelid}>")} {jumpToMessage}",
                //Description = $"Message created by <@{msg.Authorid}>",
                Color = Utils.GetRandomColor(),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Created at {msg.Createdat}"
                }
            }.WithAuthor(new EmbedAuthorBuilder() {
                IconUrl = user.GetAvatarUrl(),
                Name = Utils.FullUserName(user)
                });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Original message";
                if (msg.Content.Length > 1024)
                    field.Value = msg.Content.Substring(0, msg.Content.Length / 2) + "...";
                else 
                    field.Value = msg.Content;
            });
            if (msg.UpdatedContent != null)
                eb.AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = "Updated message";
                    field.Value = msg.UpdatedContent;
                });
            if (msg.Attachments != null)
                eb.AddField("Attachment", msg.Attachments, true);
            if (msg.Deleted)
            {
                eb.AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = "Deleted at";
                    field.Value = $"{msg.Deletedat}\n Deleted after {msg.Deletedat.Value.Subtract(msg.Createdat)}";
                });
            }

            await ReplyAsync(string.Empty, embed: eb.Build());
        }
    }
}