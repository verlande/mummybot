using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Extensions;
using System.Threading.Tasks;
using mummybot.Modules.Manage.Services;
using System.Text.RegularExpressions;
using System;

namespace mummybot.Modules.Manage
{
    public partial class Manage
    {
        [Name("Manage")]
        public class ManageCommands : mummybotSubmodule<FilteringService>
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

            [Command("FilterInv"), Summary("Toggle invite link filtering"), RequireUserPermission(GuildPermission.ManageGuild | GuildPermission.Administrator)]
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

            [Command("FilterRegex"), Summary("Filter messages with regex (Advanced users)"), RequireUserPermission(GuildPermission.Administrator)]
            public async Task FilterRegex(string pattern = "")
            {
                var conf = await Database.Guilds.SingleAsync(x => x.GuildId.Equals(Context.Guild.Id)).ConfigureAwait(false);

                if (string.IsNullOrEmpty(pattern))
                {
                    if (!await PromptUserConfirmAsync(
                            new EmbedBuilder().WithDescription("Do you want to remove Regex filtering?"))
                        .ConfigureAwait(false)) return;
                    
                    _service.RegexFiltering.TryRemove(Context.Guild.Id, out _);
                    conf.Regex = null;
                    await Context.Channel.SendConfirmAsync("Regex filtering disabled").ConfigureAwait(false);
                    return;
                }

                try
                {
                    if (Regex.Match("", pattern).Success && pattern.Length < 25)
                    {
                        conf.Regex = pattern;
                        
                        if (_service.RegexFiltering.ContainsKey(Context.Guild.Id))
                            _service.RegexFiltering[Context.Guild.Id] = pattern;

                        _service.RegexFiltering.TryAdd(Context.Guild.Id, pattern);
                        Database.Update(conf);
                        await Context.Channel.SendConfirmAsync($"Regex filtering set to {Format.Code(pattern)}")
                            .ConfigureAwait(false);
                    }
                }
                catch (ArgumentException ex)
                {
                    await Context.Channel.SendErrorAsync(string.Empty, ex.Message).ConfigureAwait(false);
                    _log.Error(ex);
                }
            }

            [RequireUserPermission(GuildPermission.ManageGuild)]
            [Command("BotChannel"), Summary("Restrict bot commands to a selected channel")]
            public async Task BotChannel(ITextChannel channel = null)
            {
                var conf = await Database.Guilds.SingleAsync(x => x.GuildId.Equals(Context.Guild.Id));

                if (channel is null)
                {
                    conf.BotChannel = 0;
                    _service.BotRestriction.TryRemove(Context.Guild.Id, out _);
                    await Context.Channel.SendConfirmAsync("Disabled bot restriction").ConfigureAwait(false);
                    return;
                }
                conf.BotChannel = channel.Id;
                Database.Update(conf);
                _service.BotRestriction.TryAdd(Context.Guild.Id, channel.Id);
                await Context.Channel.SendConfirmAsync($"Restricting bot commands to <#{channel.Id}>").ConfigureAwait(false);
            }

            [Command("Botnick"), Summary("Sets this bots nickname"), RequireUserPermission(GuildPermission.ManageNicknames)]
            public async Task Nick([Remainder] string nickname)
            {
                if (nickname.Length > 32) return;
                await Context.Guild.GetUser(Context.Client.CurrentUser.Id).ModifyAsync(x => x.Nickname = nickname);
            }

            protected override void AfterExecute(CommandInfo command)
            {
                base.AfterExecute(command);
                Database.SaveChanges();
                Database.Dispose();
            }
        }
    }
}
