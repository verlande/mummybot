using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using mummybot.Extensions;
using mummybot.Attributes;

namespace mummybot.Modules.Utility
{
    public partial class Utility
    {
        [Command("Userinfo"), Cooldown(5)]
        public async Task UserInfo(SocketGuildUser user = null)
        {
            user ??= (SocketGuildUser)Context.User;

            var userRoles = user.Roles.Where(x => x.Id != Context.Guild!.EveryoneRole.Id)
                .OrderByDescending(x => x.Position)
                .ToList();
            var pastNames = Database.UsersAudit.Where(x => x.UserId.Equals(user.Id)).ToList();

            var eb = new EmbedBuilder()
                .WithTitle($"{user} {(!string.IsNullOrEmpty(user.Nickname) ? $"({user.Nickname})" : "")}")
                .WithColor(Utils.GetRandomColor())
                .WithThumbnailUrl(user.GetAvatarUrl())
                .AddField("ID", user.Id, true)
                .AddField("Status", user.Status, true)
                .AddField("Joined", user.JoinedAt?.ToString("g") ?? "-", true)
                .AddField("Account Created", user.CreatedAt.ToString("g"), true)
                .AddField($"Roles ({userRoles.Count})", userRoles.Count != 0 ? string.Join("\n", userRoles.Take(10)) : "-", true)
                .AddField("Past Usernames", Format.Code(pastNames.Count != 0 ? string.Join("\n", pastNames.Select(x => x.Username).Take(5)) : "None"))
                .AddField("Past Nicknames", Format.Code(pastNames.Count != 0 ? string.Join("\n", pastNames.Select(x => x.Nickname).Take(5)) : "None"))
                .WithFooter(new EmbedFooterBuilder().WithText($"• Requested by {Context.User}"));
            await ReplyAsync(string.Empty, embed: eb.Build()).ConfigureAwait(false);
        }

        [Command("Botinfo"), Summary("Displays bot information")]
        public async Task BotInfo()
        {
            var application = await Context.Client.GetApplicationInfoAsync();

            await Context.Channel.SendConfirmAsync(string.Empty, $"{Format.Bold("Bot Info")}\n" +
                              $"- Author: {application.Owner} ({application.Owner.Id})\n" +
                              $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                              $"- Kernel: {Environment.OSVersion}\n" +
                              $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                              $"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime):dd\\.hh\\:mm\\:ss}\n" +
                              $"- Ping: {_client.Latency}ms\n" +

                    $"{Format.Bold("Stats")}\n" +
                    $"- Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB\n" +
                    $"- Used Memory: {Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)} MB\n" +
                    $"- Total Commands: {_commandService.Commands.Count()}\n" +
                    $"- Guilds Served: {Context.Client.Guilds.Count}\n" +
                    $"- Channels: {Context.Client.Guilds.Sum(g => g.TextChannels.Count)}\n" +
                    //$"- Commands Processed: {_commandHandlerService.ProcessedCommands}\n" +
                    $"- Users: {Context.Client.Guilds.Sum(g => g.Users.Count)}\n", null, "Made with Discord.NET & PostgreSQL").ConfigureAwait(false);
        }

        [Command("Uptime")]
        public async Task Uptime() => await Context.Channel.SendConfirmAsync($"``{DateTime.Now - Process.GetCurrentProcess().StartTime:g}``")
            .ConfigureAwait(false);

        [Command("Isadmin"), Summary("Check is a user is an admin")]
        public async Task IsAdmin(SocketGuildUser user)
        {
            if (user.GuildPermissions.Administrator)
                await Context.Channel.SendConfirmAsync($"{Utils.FullUserName(user)} is an admin").ConfigureAwait(false);
            else await Context.Channel.SendConfirmAsync($"{Utils.FullUserName(user)} is not an admin").ConfigureAwait(false);
        }

        [Command("Guildinfo")]
        public async Task GuildInfo()
        {
            var online = Context.Guild.Users.Count(x => x.Status != UserStatus.Offline);
            var offline = Context.Guild.Users.Count(x => x.Status == UserStatus.Offline);
            var roles = Context.Guild.Roles;

            var rolesb = new StringBuilder();

            foreach (var rolenames in roles)
                if (rolenames.Members.Count(x => x.IsBot) != 1 && roles.Count < 20)
                    rolesb.Append($"{rolenames.Mention}, ");

            var guild = Context.Guild;

            var eb = new EmbedBuilder
            {
                Title = $"{guild.Name} ({guild.Id})",
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
                field.Name = "Total bots";
                field.Value = guild.Users.Count(x => x.IsBot);
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
            if (guild.Emotes.Count > 0)
            {
                eb.AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = $"Emojis";
                    field.Value = guild.Emotes.Count;
                });
            }
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = $"Roles";
                if (roles.Count > 20)
                    field.Value = roles.Count;
                field.Value = rolesb;
            });
            eb.AddField(field =>
            {
                field.IsInline = true;
                field.Name = "Verification Level";
                field.Value = guild.VerificationLevel;
            });

            await ReplyAsync(string.Empty, embed: eb.Build()).ConfigureAwait(false);
        }

        [Command("Membercount"), Summary("Guild member count")]
        public async Task MemberCount()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Online {Context.Guild.Users.Count(x => x.Status == UserStatus.Online)}");
            sb.AppendLine($"Idle {Context.Guild.Users.Count(x => x.Status == UserStatus.Idle)}");
            sb.AppendLine($"Do Not Disturb {Context.Guild.Users.Count(x => x.Status == UserStatus.DoNotDisturb)}");
            sb.AppendLine($"Offline {Context.Guild.Users.Count(x => x.Status == UserStatus.Offline)}");
            sb.AppendLine($"Bot {Context.Guild.Users.Count(x => x.IsBot)}");

            await Context.Channel.SendConfirmAsync(sb.ToString(), $"{Context.Guild.Name} memeber count").ConfigureAwait(false);
        }
    }
}
