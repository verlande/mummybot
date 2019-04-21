using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Extensions;
using System.Threading.Tasks;

namespace mummybot.Modules.Manage
{
    public partial class Manage : ModuleBase
    {
        [Command("SetGreeting"), Summary("Sets a greeting for new members. Use %user% to include new user's name in the message")]
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

        [Command("ClearGreeting"), Summary("Clears greeting message")]
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

        [Command("SetGoodbye"), Summary("Set goodbye when a user leaves. Use %user% to include new user's name in the message")]
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

        [Command("ClearGoodbye"), Summary("Clears goodbye message")]
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

        [Command("SetGreetchl"), Summary("Set channel to send greeting and goodbye messages")]
        [RequireUserPermission(GuildPermission.ManageGuild | GuildPermission.Administrator)]
        public async Task Setgreetchl(SocketTextChannel channel)
        {
            var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
            guild.GreetChl = channel.Id;
            await Context.Channel.SendConfirmAsync($"Set greeting channel to {channel.Mention}");
        }

        [Command("Logging"), Summary("Enabled/Disable message logging")]
        public async Task Logging(string arg = null, ITextChannel chl = null)
        {
            var conf = await Database.Guilds.SingleAsync(g => g.GuildId.Equals(Context.Guild.Id));

            if (!conf.MessageLogging)
            {
                conf.MessageLogging = true;
                await Context.Channel.SendConfirmAsync("Message logging enabled");
                return;
            }

            var msg = await PromptUserConfirmAsync(new EmbedBuilder()
                .WithDescription("Disabling logged with also disable\n\n``Snipe\nUndelete\nSource``")
                .WithColor(Utils.GetRandomColor()));
            if (msg)
            {
                conf.MessageLogging = false;
                await ReplyAsync("Disabled Logging");
            }
            await Database.SaveChangesAsync();
        }
    }
}
