﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using mummybot.Extensions;


namespace mummybot.Modules.Utility
{
    public partial class Utility : ModuleBase
    {
        [Command("Roleinfo")]
        public async Task RoleInfo(SocketRole role)
        {
            var eb = new EmbedBuilder
            {
                Title = $"Role Info: {role.Name}",
                Description = $"**Position:** {Context.Guild.Roles.Count - role.Position}/{Context.Guild.Roles.Count}\n" +
                Format.Bold("Colour: ") + role.Color + "\n" +
                $"**{role.Members.Count()} member** | {role.Members.Count(x => x.Status != UserStatus.Offline)} online\n" +
                $"**Created:** {role.CreatedAt.UtcDateTime}\n" +
                $"**Mentionable**: {role.IsMentionable}\n" +
                $"**Permissions:** {string.Join("\n", role.Permissions.ToList())}",
                ThumbnailUrl = $"https://www.colorhexa.com/{role.Color.ToString().Substring(1)}.png",
                Color = role.Color
            };
            eb.WithFooter($"ID: {role.Id}");

            await ReplyAsync(string.Empty, embed: eb.Build());
        }

        [Command("Userinfo")]
        public async Task UserInfo(IGuildUser user)
        {
            await Context.Channel.SendAuthorAsync(user, $"{user.Username} | {user.Status}\n\n" +
            Format.Bold("Nickname: ") + user.Nickname ?? "None" +
            "\n" +
            Format.Bold("Joined: ") + user.JoinedAt.Value.DateTime +
            "\n" + Format.Bold("Created: ") + user.CreatedAt.DateTime +
            "\n" + Format.Bold($"Roles ({user.GetRoles().Count()}): ") + $"{string.Join(", ", user.GetRoles().Select(x => x.Mention))}", $"ID: {user.Id}");
        }

        [Command("Botinfo"), Summary("Displays bot information")]
        public async Task BotInfo()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            var footer = "https://i.imgur.com/mCnQNtK.jpg?1";
            //var eb = new EmbedBuilder
            //{

            //    ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
            //    Description = $"{Format.Bold("Bot Info")}\n" +
            //                  $"- Author: {application.Owner.Mention}\n" +
            //                  $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
            //                  $"- Kernel: {Environment.OSVersion}\n" +
            //                  "- PostgreSQL Version: 10.1\n" +
            //                  $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
            //                  $"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime):dd\\.hh\\:mm\\:ss}\n\n" +

            //        $"{Format.Bold("Stats")}\n" +
            //        $"- Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB\n" +
            //        $"- Guilds Served: {Context.Client.Guilds.Count}\n" +
            //        $"- Total Commands: {_command.Commands.Count()}\n" +
            //        $"- Channels: {Context.Client.Guilds.Sum(g => g.TextChannels.Count)}\n" +
            //        $"- Users: {Context.Client.Guilds.Sum(g => g.Users.Count)}\n"
            //};
            //eb.WithFooter("Made with Discord.NET", "https://i.imgur.com/mCnQNtK.jpg?1");
            //eb.WithColor(Utils.GetRandomColor());
            //await ReplyAsync(string.Empty, embed: eb.Build());

            await Context.Channel.SendConfirmAsync("", $"{Format.Bold("Bot Info")}\n" +
                              $"- Author: {application.Owner.Mention}\n" +
                              $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                              $"- Kernel: {Environment.OSVersion}\n" +
                              "- PostgreSQL Version: 10.1\n" +
                              $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                              $"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime):dd\\.hh\\:mm\\:ss}\n\n" +

                    $"{Format.Bold("Stats")}\n" +
                    $"- Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB\n" +
                    $"- Guilds Served: {Context.Client.Guilds.Count}\n" +
                    $"- Total Commands: {_command.Commands.Count()}\n" +
                    $"- Channels: {Context.Client.Guilds.Sum(g => g.TextChannels.Count)}\n" +
                    $"- Users: {Context.Client.Guilds.Sum(g => g.Users.Count)}\n", null, "Made with Discord.NET");
        }

        [Command("Uptime")]
        public async Task Uptime()
        {
            await ReplyAsync($"``{DateTime.Now - Process.GetCurrentProcess().StartTime:g}``");
        }

        [Command("Isadmin"), Summary("Check is a user is an admin")]
        public async Task IsAdmin(SocketGuildUser user)
        {
            if (user.GuildPermissions.Administrator)
                await ReplyAsync($"{Utils.FullUserName(user)} is an admin");
            else await ReplyAsync($"{Utils.FullUserName(user)} is not an admin");
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

            foreach (var rolenames in Context.Guild.Roles)
                if (rolenames.Members.Count(x => x.IsBot) != 1)
                    rolesb.Append($"{rolenames.Mention}, ");

            foreach (var emotes in Context.Guild.Emotes)
                emotesb.Append($"{emotes} ");

            var guild = Context.Guild;

            EmbedBuilder eb = new EmbedBuilder
            {
                Title = guild.Name,
                ThumbnailUrl = guild.IconUrl,
                Color = Utils.GetRandomColor()
            };
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Owner";
                field.Value = $"{guild.Owner.Mention}";
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
                field.Name = "Created At";
                field.Value = $"{guild.CreatedAt.LocalDateTime}\n{Format.Italics($"About {(DateTime.Now.Date - guild.CreatedAt.Date).Days} days ago")}";
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
                field.Name = $"Roles";
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

        [Command("Membercount"), Summary("Guild member count")]
        public async Task MemberCount() => await Context.Channel.SendConfirmAsync($"{Context.Guild.MemberCount} users in this guild");
    }
}