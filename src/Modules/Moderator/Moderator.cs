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

                await ReplyAsync($"{user} has been banned from creating tags");
            }
            else if (result != null && result.TagBanned)
            {
                result.TagBanned = false;
                await ReplyAsync($"{user} has been unbanned from creating tags");
            }
        }

        [Command("Prune"), Summary("Prune user messages"), RequireBotPermission(GuildPermission.ManageMessages), RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Prune(IGuildUser user)
        {
            var msgs = await Context.Channel.GetMessagesAsync().FlattenAsync();
            var result = msgs.Where(x => x.Author.Id.Equals(user.Id));

            try
            {
                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(result);
                await Context.Channel.SendConfirmAsync($"Successfully pruned {result.Count()} messages");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(string.Empty, ex.Message);
            }
        }

        [Command("Botnick"), Summary("Sets this bots nickname"), RequireUserPermission(GuildPermission.ManageNicknames)]
        public async Task Nick([Remainder] string nickname) => await Context.Guild.GetUser(Context.Client.CurrentUser.Id).ModifyAsync(x => x.Nickname = nickname);

        [Command("Setnick"), RequireBotPermission(GuildPermission.ManageNicknames)]
        public async Task Nick(IGuildUser arg, [Remainder] string newNick)
        {
            if (string.IsNullOrWhiteSpace(newNick)) return;

            await arg.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);
        }

        [Command("Ban"), RequireBotPermission(GuildPermission.BanMembers), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IGuildUser user, string reason = null) => await user.BanAsync(0, reason);

        [Command("Kick"), RequireBotPermission(GuildPermission.KickMembers), RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user, string reason = null) => await user.KickAsync(reason);

        [Command("Clearbots"), Summary("Clears messages from all bots"), RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Clearbot()
        {
            var msgs = await Context.Channel.GetMessagesAsync().FlattenAsync();
            var result = msgs.Where(x => x.Author.IsBot).Take(100);
            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(result);
        }

        [Command("Delinv")]
        public async Task clearInv()
        {
            var invites = Context.Guild.GetInvitesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            foreach (var inv in invites) await inv.DeleteAsync();
            await Context.Channel.SendConfirmAsync($"Deleted {invites.Count} invites");
        }

        protected override async void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            await Database.SaveChangesAsync();
            Database.Dispose();
        }
    }
}
