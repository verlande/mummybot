using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using mummybot.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
        {
            await Context.Guild.GetUser(Context.Client.CurrentUser.Id).ModifyAsync(x => x.Nickname = nickname);
        }

        [Command("Setnick"), RequireBotPermission(GuildPermission.ManageNicknames)]
        public async Task Nick(IGuildUser arg, [Remainder] string newNick)
        {
            if (string.IsNullOrWhiteSpace(newNick)) return;

            await arg.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);
        }

        [Command("Kick"), RequireBotPermission(GuildPermission.KickMembers), RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user, string reason = null) => await user.KickAsync(reason);

        [Command("Setgreeting"), Summary("Sets a greeting for new members. Use %user% to include new user's name in the message")]
        [RequireUserPermission(GuildPermission.ManageGuild | GuildPermission.Administrator)]
        public async Task SetGreeting([Remainder] string greeting)
        {
            if (greeting.Length > 100)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "Greeting message length over 100 chars");
                return;
            }
            var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
            guild.Greeting = greeting;

            await Context.Channel.SendConfirmAsync("Greeting message has been set");

            //if (greeting.Contains("%user%")) await ReplyAsync(greeting.Replace("%user%", Context.User.Mention));
        }

        [Command("Cleargreeting"), Summary("Clears greeting message")]
        [RequireUserPermission(GuildPermission.ManageGuild | GuildPermission.Administrator)]
        public async Task Cleargreeting()
        {
            var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
            if (string.IsNullOrWhiteSpace(guild.Greeting))
                await Context.Channel.SendErrorAsync(string.Empty, "Can't clear a greeting if you haven't set one");
            else
            {
                guild.Greeting = string.Empty;
                await Context.Channel.SendConfirmAsync("Cleared greeting message");
            }
        }

        [Command("Setgoodbye"), Summary("Set goodbye when a user leaves. Use %user% to include new user's name in the message")]
        [RequireUserPermission(GuildPermission.ManageGuild | GuildPermission.Administrator)]
        public async Task Setgoodbye([Remainder] string goodbye)
        {
            if (goodbye.Length > 100)
            {
                await Context.Channel.SendErrorAsync(string.Empty, "Goodbye message length over 100 chars");
                return;
            }
            var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
            guild.Goodbye = goodbye;

            await Context.Channel.SendConfirmAsync("Goodbye message has been set");
        }

        [Command("Cleargoodbye"), Summary("Clears goodbye message")]
        [RequireUserPermission(GuildPermission.ManageGuild | GuildPermission.Administrator)]
        public async Task Cleargoodbye()
        {
            var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
            if (string.IsNullOrWhiteSpace(guild.Goodbye))
                await Context.Channel.SendErrorAsync(string.Empty, "Can't clear a goodbye message if you haven't set one");
            else
            {
                guild.Goodbye = string.Empty;
                await Context.Channel.SendConfirmAsync("Cleared goodbye message");
            }
        }

        [Command("Setgreetchl"), Summary("Set channel to send greeting and goodbye messages")]
        [RequireUserPermission(GuildPermission.ManageGuild | GuildPermission.Administrator)]
        public async Task Setgreetchl(SocketTextChannel channel)
        {
            var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
            guild.GreetChl = channel.Id;
            await Context.Channel.SendConfirmAsync($"Set greeting channel to {channel.Mention}");
        }

        [Command("Clearbot"), Summary("Clears messagea from all bots"), RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Clearbot()
        {
            var msgs = await Context.Channel.GetMessagesAsync().FlattenAsync();
            var result = msgs.Where(x => x.Author.IsBot).Take(100);
            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(result);
        }

        [Command("clearinv")]
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
