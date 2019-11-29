using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Extensions;
using System.Threading.Tasks;

namespace mummybot.Modules.Manage
{
    public partial class Manage
    {
        [Name("Manage")]
        public class ManageCommands : mummybotSubmodule<Services.FilteringService>
        {
            [Command("SetGreeting"), Summary("Sets a greeting for new members. Use %user% to include new user's name in the message")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetGreeting([Remainder] string greeting)
            {
                if (greeting.Length > 100)
                {
                    await Context.Channel.SendErrorAsync(string.Empty, "Greeting message length over 100 chars")
                        .ConfigureAwait(false);
                    return;
                }
                var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
                guild.Greeting = greeting;

                await Context.Channel.SendConfirmAsync("Greeting message has been set").ConfigureAwait(false);

                //if (greeting.Contains("%user%")) await ReplyAsync(greeting.Replace("%user%", Context.User.Mention));
            }

            [Command("ClearGreeting"), Summary("Clears greeting message")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Cleargreeting()
            {
                var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
                if (string.IsNullOrWhiteSpace(guild.Greeting))
                    await Context.Channel.SendErrorAsync(string.Empty, "Can't clear a greeting if you haven't set one").ConfigureAwait(false);
                else
                {
                    guild.Greeting = string.Empty;
                    await Context.Channel.SendConfirmAsync("Cleared greeting message").ConfigureAwait(false);
                }
            }

            [Command("SetGoodbye"), Summary("Set goodbye when a user leaves. Use %user% to include new user's name in the message")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Setgoodbye([Remainder] string goodbye)
            {
                if (goodbye.Length > 100)
                {
                    await Context.Channel.SendErrorAsync(string.Empty, "Goodbye message length over 100 chars")
                        .ConfigureAwait(false);
                    return;
                }
                var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
                guild.Goodbye = goodbye;

                await Context.Channel.SendConfirmAsync("Goodbye message has been set").ConfigureAwait(false);
            }

            [Command("ClearGoodbye"), Summary("Clears goodbye message")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Cleargoodbye()
            {
                var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
                if (string.IsNullOrWhiteSpace(guild.Goodbye))
                    await Context.Channel.SendErrorAsync(string.Empty, "Can't clear a goodbye message if you haven't set one")
                        .ConfigureAwait(false);
                else
                {
                    guild.Goodbye = string.Empty;
                    await Context.Channel.SendConfirmAsync("Cleared goodbye message").ConfigureAwait(false);
                }
            }

            [Command("SetGreetchl"), Summary("Set channel to send greeting and goodbye messages")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Setgreetchl(SocketTextChannel channel)
            {
                var guild = await Database.Guilds.SingleOrDefaultAsync(g => g.GuildId.Equals(Context.Guild.Id));
                guild.GreetChl = channel.Id;
                await Context.Channel.SendConfirmAsync($"Set greeting channel to {channel.Mention}").ConfigureAwait(false);
            }

            [Command("Logging"), Summary("Enabled/Disable message logging"), RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Logging(string arg = null, ITextChannel chl = null)
            {
                var conf = await Database.Guilds.SingleAsync(g => g.GuildId.Equals(Context.Guild.Id));

                if (conf.MessageLogging)
                {
                    var msg = await PromptUserConfirmAsync(new EmbedBuilder()
                        .WithDescription("Disabling message logging will disable these commands: " +
                        "\n``£Snipe\n£Undelete\n£Source``")
                        .WithColor(Utils.GetRandomColor()));
                    if (msg)
                    {
                        conf.MessageLogging = false;
                        await Context.Channel.SendConfirmAsync("Logging has been disabled").ConfigureAwait(false);
                    }
                }
            }

            [Command("FilterInv"), Summary("Toggle invite link filtering"), RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task FilterInv()
            {
                var conf = await Database.Guilds.SingleAsync(x => x.GuildId.Equals(Context.Guild.Id)).ConfigureAwait(false);

                if (_service.InviteFiltering.Contains(Context.Guild.Id))
                {
                    conf.FilterInvites = false;
                    _service.InviteFiltering.TryRemove(Context.Guild.Id);
                    await Context.Channel.SendConfirmAsync("Invite filtering disabled").ConfigureAwait(false);
                }
                else if (!_service.InviteFiltering.Contains(Context.Guild.Id))
                {
                    conf.FilterInvites = true;
                    _service.InviteFiltering.Add(Context.Guild.Id);
                    await Context.Channel.SendConfirmAsync("Invite filtering enabled").ConfigureAwait(false);
                }
            }

            protected override async void AfterExecute(CommandInfo command)
            {
                base.AfterExecute(command);
                await Database.SaveChangesAsync();
                Database.Dispose();
            }
        }
    }
}
