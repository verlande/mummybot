using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using mummybot.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using mummybot.Services;
using System.Text;
using Discord.Net;
using System.Globalization;

namespace mummybot.Modules.Moderator
{
    public class Moderator : ModuleBase
    {
        private readonly UserService _userService;

        public Moderator(UserService userService)
        {
            _userService = userService;
        }

        [Command("Tagban"), Summary("Ban/Unban a user from creating tags in your guild"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task TagBan(SocketGuildUser user)
        {   
            if (user.IsBot || user.GuildPermissions.Administrator) return;

            if (!await _userService.UserExists(user.Id, user.Guild.Id)) await _userService.AddUser(user);

            var result = await Database.Users.SingleAsync(u => u.UserId.Equals(user.Id)
                                                   && u.GuildId.Equals(user.Guild.Id));

            if (result != null && !result.TagBanned)
            {
                result.TagBanned = true;
                await Context.Channel.SendConfirmAsync($"{user} has been banned from creating tags").ConfigureAwait(false);
            }
            else if (result != null && result.TagBanned)
            {
                result.TagBanned = false;
                await Context.Channel.SendConfirmAsync($"{user} has been unbanned from creating tags").ConfigureAwait(false);
            }
        }

        [Command("Prune"), Summary("Prune user messages upto 2 weeks old"), RequireBotPermission(GuildPermission.ManageMessages), RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Prune(IGuildUser user)
        {
            var msgs = await Context.Channel.GetMessagesAsync().FlattenAsync().ConfigureAwait(false);
            var result = msgs.Where(x => x.Author.Id.Equals(user.Id) && (DateTime.Now - x.CreatedAt).TotalDays < 14);

            try
            {
                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(result);
                await Context.Channel.SendConfirmAsync($"Successfully pruned {result.Count()} messages");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(string.Empty, ex.Message).ConfigureAwait(false);
            }
        }

        [Command("Setnick"), RequireUserPermission(GuildPermission.ManageNicknames), RequireBotPermission(GuildPermission.ManageNicknames)]
        public async Task Nick(IGuildUser arg, [Remainder] string newNick)
        {
            if (string.IsNullOrWhiteSpace(newNick) || newNick.Length > 32) return;

            await arg.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);
        }

        [Command("Ban"), RequireBotPermission(GuildPermission.BanMembers), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IGuildUser user, string reason = null) => await user.BanAsync(7, reason).ConfigureAwait(false);

        [Command("Softban"), Summary("Ban then unban removing 7 days of messages"), RequireBotPermission(GuildPermission.BanMembers), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task SoftBan(IGuildUser user, string reason = null)
        {
            await user.BanAsync(7, reason).ConfigureAwait(false);
            await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false);
        }

        [Command("Kick"), RequireBotPermission(GuildPermission.KickMembers), RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(SocketGuildUser user, string reason = null)
        {
            if (user.Hierarchy < Context.Guild.GetUser(_client.CurrentUser.Id).GetRoles().Max(r => r.Position))
            {
                await user.KickAsync(reason).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"Kicked {user}").ConfigureAwait(false);
                return;
            }
            
            await Context.Channel.SendErrorAsync("kicking user", "Cannot kick a user that has a higher hierarchy than you").ConfigureAwait(false);
        }

        [Command("Clearbots"), Summary("Clears messages from all bots"), RequireUserPermission(GuildPermission.ManageMessages), RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Clearbot()
        {
            try
            {
                var msgs = await Context.Channel.GetMessagesAsync().FlattenAsync().ConfigureAwait(false);
                var result = msgs.Where(x => x.Author.IsBot && (DateTime.Now - x.CreatedAt).TotalDays < 14);

                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(result).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"Deleted {result.Count()} bot messages").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(string.Empty, ex.Message);
            }
        }

        [Command("ListInvites"), Summary("Returns list of invites")]
        public async Task ListInvites()
        {
            var sb = new StringBuilder();
            try
            {
                var invites = Context.Guild.GetInvitesAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                if (invites.Count is 0)
                {
                    await Context.Channel.SendConfirmAsync("No invites to list").ConfigureAwait(false);
                    return;
                }

                foreach (var inv in invites)
                    sb.AppendLine($"{Format.Code(inv.Url)} by {Format.Bold($"{inv.Inviter.Username}#{inv.Inviter.Discriminator}")} at" + 
                                  $" {Format.Italics(inv.CreatedAt.Value.DateTime.ToString("g"))} used {inv.Uses} times");

                await Context.Channel.SendConfirmAsync(sb.ToString(), "Current Invite List").ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                await Context.Channel.SendErrorAsync(string.Empty, ex.Message).ConfigureAwait(false);
            }
        }

        [Command("Deleteinvites"), Summary("Delete all created invite links"), RequireBotPermission(GuildPermission.Administrator), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ClearInvite()
        {
            var invites = Context.Guild.GetInvitesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            foreach (var inv in invites) await inv.DeleteAsync().ConfigureAwait(false);
            await Context.Channel.SendConfirmAsync($"Deleted {invites.Count} invites").ConfigureAwait(false);
        }

        protected override void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            Database.SaveChanges();
            Database.Dispose();
        }
    }
}
