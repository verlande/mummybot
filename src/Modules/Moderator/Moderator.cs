using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using mummybot.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace mummybot.Modules.Moderator
{
    public partial class Moderator : ModuleBase
    {
        [Command("Tagban"), Summary("Ban/Unban a user from creating tags in your guild")]
        public async Task TagBan(SocketGuildUser user)
        {
            if (user.IsBot) return;

            var result = await Database.Users.SingleAsync(u => u.UserId.Equals(user.Id)
                                                   && u.GuildId.Equals(user.Guild.Id));
            if (result != null && !result.TagBanned)
            {
                result.TagBanned = true;
                Database.Users.Attach(result);

                await ReplyAsync($"{user.Nickname} has been banned from creating tags");
            }
            else if (result != null && result.TagBanned)
            {
                result.TagBanned = false;
                Database.Users.Attach(result);
                await ReplyAsync($"{user.Nickname} has been unbanned from creating tags");
            }
        }

        [Command("Nick"), Summary("Sets this bots nickname"), RequireUserPermission(GuildPermission.ManageNicknames)]
        public async Task Nick([Remainder] string nickname) 
            => await Context.Guild.GetUser(Context.Client.CurrentUser.Id).ModifyAsync(x => x.Nickname = nickname);

        [Command("Setnick"), Summary("Set a users nickname") , RequireBotPermission(GuildPermission.ManageNicknames)]
        public async Task Nick(IGuildUser arg, [Remainder] string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return;
            await arg.ModifyAsync(u => u.Nickname = nickname).ConfigureAwait(false);
        }

        [Command("Kick"), RequireBotPermission(GuildPermission.KickMembers), RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user, string reason = null) 
            => await user.KickAsync(reason);

        [Command("Ban"), RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Ban (IGuildUser user, string reason = null)
        {
            await user.BanAsync(0, reason);
            await Context.Channel.SendConfirmAsync(Format.Bold(Utils.FullUserName((SocketUser)user) + " has been banned"));
        }

        [Command("Clearbot"), Summary("Clears bot messages"), RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Clearbot()
        {
            var msgs = await Context.Channel.GetMessagesAsync().FlattenAsync();
            var result = msgs.Where(x => x.Author.IsBot).Take(100);
            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(result);
        }

        [Command("Clearinv"), Summary("Clear all invite links")]
        public async Task clearInv()
        {
            var invites = Context.Guild.GetInvitesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            foreach (var inv in invites) await inv.DeleteAsync();
            await Context.Channel.SendConfirmAsync($"Deleted {invites.Count} invite links");
        }

        protected override async void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            await Database.SaveChangesAsync();
            Database.Dispose();
        }
    }
}
