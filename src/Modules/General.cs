using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Services;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Modules
{
    [Name("General")]
    public class General : ModuleBase
    {
        private readonly DiscordSocketClient _discord;

        public General(DiscordSocketClient discord)
            => _discord = discord;
        
        [Command("Botinfo"), Summary("Displays bot information")]
        public async Task BotInfo()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            var eb = new EmbedBuilder
            {
                
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Description = $"{Format.Bold("Bot Info")}\n" +
                              $"- Author: {application.Owner.Username}#{application.Owner.Discriminator}\n" +
                              $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                              $"- Kernel: {Environment.OSVersion}\n" +
                              "- PostgreSQL Version: 10.1\n" +
                              $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                              $"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime):dd\\.hh\\:mm\\:ss}\n\n" +

                              $"{Format.Bold("Stats")}\n" +
                              $"- Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB\n" +
                              $"- Guilds Served: {Context.Client.Guilds.Count}\n" +
                              $"- Channels: {Context.Client.Guilds.Sum(g => g.Channels.Count)}\n" +
                              $"- Users: {Context.Client.Guilds.Sum(g => g.Users.Count)}\n" +
                              $"- Commands Served: {CommandHandlerService.BotReplies}\n" +
                              $"- Messages Intercepted: {CommandHandlerService.MessagesIntercepted}"
            };
            eb.WithFooter("Made with Discord.NET", "https://i.imgur.com/mCnQNtK.jpg?1");
            eb.WithColor(Utils.GetRandomColor());
            await ReplyAsync(string.Empty, embed: eb.Build());
        }

        [Command("Uptime")]
        public async Task Uptime()
        {
            await ReplyAsync($"``{DateTime.Now - Process.GetCurrentProcess().StartTime:g}``");
        }

        [Command("Guildinfo")]
        public async Task GuildInfo()
        {
            var online = Context.Guild.Users.Count(x => x.Status != UserStatus.Offline);
            //var idle = Context.Guild.Users.Count(x => x.Status == UserStatus.Idle);
            //var dnd = Context.Guild.Users.Count(x => x.Status == UserStatus.DoNotDisturb);
            var offline = Context.Guild.Users.Count(x => x.Status == UserStatus.Offline);

            var rolesb = new StringBuilder();
            var emotesb = new StringBuilder();

            foreach(var rolenames in Context.Guild.Roles)
            {
                rolesb.Append($"{rolenames}, ");
            }

            foreach(var emotes in Context.Guild.Emotes)
            {
                emotesb.Append($"{emotes} ");
            }

            var guild = Context.Guild;

            EmbedBuilder eb = new EmbedBuilder
            {
                Title = $"{guild.Name} info",
                ThumbnailUrl = guild.IconUrl,
                Color = Utils.GetRandomColor()
            };
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Owner";
                field.Value = $"<@{guild.OwnerId}>";
            });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Total users";
                field.Value = guild.MemberCount;
            });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Region";
                field.Value = guild.VoiceRegionId;
            });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Text channels";
                field.Value = guild.TextChannels.Count;
            });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Voice channels";
                field.Value = guild.VoiceChannels.Count;
            });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Created";
                field.Value = guild.CreatedAt.LocalDateTime;
            });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Statuses";
                field.Value = $"Online: {online}\nOffline: {offline}";
            });
            if (guild.Emotes.Count > 1)
            {
                eb.AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = $"Emojis ({guild.Emotes.Count})";
                    field.Value = emotesb;
                });
            }
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Roles";
                field.Value = rolesb;
            });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Verification Level";
                field.Value = guild.VerificationLevel;
            });

            await ReplyAsync(string.Empty, embed: eb.Build());
        }
        
        [Command("Avatar"), Summary("gets user avatar")]
        public async Task Avatar(SocketUser user = null)
        {
            var avatar = user ?? Context.Client.CurrentUser;
            if (user == null)
            {
                var avatarUrl = Context.User.GetAvatarUrl();
                avatarUrl = avatarUrl.Remove(avatarUrl.Length - 2, 2) + "024";
                await ReplyAsync($":camera_with_flash:**avatar for {avatar}**\n{avatarUrl}");
            }
            else
            {
                var avatarUrl = avatar.GetAvatarUrl();
                avatarUrl = avatarUrl.Remove(avatarUrl.Length - 2, 2) + "024";
                await ReplyAsync($":camera_with_flash:**avatar for {avatar}**\n{avatarUrl}");
            }
        }

        [Command("Lastnicks"), Summary("Lists 10 nickname changes of a user")]
        public async Task LastNicks(SocketGuildUser user)
        {
            var result = Database.UsersAudit.Where(u => u.UserId.Equals(user.Id)).OrderByDescending(u => u.Id).Take(10);
            if (!result.Any())
            {
                await ReplyAsync("Cannot fetch user from database");
            }
            else
            {
                var embedBuilder = new EmbedBuilder();

                await result.ForEachAsync(n => embedBuilder.AddField(field =>
                {
                    field.IsInline = false;
                    field.Name = n.Nickname;
                    field.Value = n.ChangedOn;
                }));

                embedBuilder.WithTitle($"Last {result.Count()} nicknames of {Utils.FullUserName(user)}");
                embedBuilder.WithColor(Utils.GetRandomColor());
                await ReplyAsync(string.Empty, embed: embedBuilder.Build());
            }
        }

        [Command("Newusers"), Summary("Lists 5 newest users")]
        public async Task NewUsers()
        {
            var users = Database.Users.Where(u => u.GuildId.Equals(Context.Guild.Id))
                .Select(u => new { u.Username, u.UserId, u.Joined }).Take(5).OrderByDescending(u => u.Joined);

            var eb = new EmbedBuilder().WithColor(Utils.GetRandomColor());
            
            await users.ForEachAsync(u => eb.AddField($"{u.Username} ({u.UserId})", u.Joined));

            await ReplyAsync(string.Empty, embed: eb.WithTitle("New Members").Build());
        }
    }
}