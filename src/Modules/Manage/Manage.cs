using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Extensions;
using System.Threading.Tasks;
using mummybot.Modules.Manage.Services;
using System;
using mummybot.Services;

namespace mummybot.Modules.Manage
{
    public partial class Manage
    {
        [Name("Manage")]
        public class ManageCommands : mummybotSubmodule<FilteringService>
        {
            private readonly GreetingService _greeting;
            private readonly GuildService _guildService;

            public ManageCommands(GreetingService greeting, GuildService guildService)
            {
                _greeting = greeting;
                _guildService = guildService;
            }

            [Command("SetJoinMessage"), Remarks("<join message>"), Summary("Set a message whenever a user joins")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetJoinMessage([Remainder] string message = "")
            {
                if (message.Length > 255)
                {
                    await Context.Channel.SendErrorAsync(string.Empty, "Message must be lower than 255 in length")
                        .ConfigureAwait(false);
                    return;
                }

                if (!string.IsNullOrEmpty(message))
                {
                    if (await _greeting.SetUserJoinLeaveMessage(GreetingService.JoinLeave.Join, Context.Guild.Id, message).ConfigureAwait(false))
                    {
                        await Context.Channel.SendConfirmAsync("User join message has been set").ConfigureAwait(false);
                        return;
                    }

                    await Context.Channel.SendErrorAsync(string.Empty, "Something happened").ConfigureAwait(false);
                    return;
                }

                if (_guildService.AllGuildConfigs.TryGetValue(Context.Guild.Id, out var conf))
                {
                    if (conf.Greeting == "**%user% has joined**")
                    {
                        await Context.Channel.SendConfirmAsync("Set a user join message\n" +
                                                               "Using `%user%` will transform into @user")
                            .ConfigureAwait(false);
                        return;
                    }
                    
                    var prompt = await PromptUserConfirmAsync(new EmbedBuilder()
                        .WithDescription("Do you want to clear user join message?")).ConfigureAwait(false);

                    if (prompt)
                    {
                        if (await _greeting.ClearUserJoinLeaveMessage(GreetingService.JoinLeave.Join, Context.Guild.Id).ConfigureAwait(false))
                        {
                            await Context.Channel.SendConfirmAsync(string.Empty, "User join message has been cleared")
                                .ConfigureAwait(false);
                            return;
                        }

                        await Context.Channel.SendErrorAsync(string.Empty, "Something happened").ConfigureAwait(false);
                    }
                }
            }

            [Command("SetLeaveMessage"), Remarks("<leave message>"), Summary("Set a message whenever a user leaves")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetLeaveMessage([Remainder] string message = "")
            {
                if (message.Length > 255)
                {
                    await Context.Channel.SendErrorAsync(string.Empty, "Message must be lower than 255 in length")
                        .ConfigureAwait(false);
                    return;
                }

                if (!string.IsNullOrEmpty(message))
                {

                    if (await _greeting.SetUserJoinLeaveMessage(GreetingService.JoinLeave.Leave, Context.Guild.Id, message).ConfigureAwait(false))
                    {
                        await Context.Channel.SendConfirmAsync("User leave message has been set").ConfigureAwait(false);
                        return;
                    }

                    await Context.Channel.SendErrorAsync(string.Empty, "Something happened").ConfigureAwait(false);
                    return;
                }

                if (_guildService.AllGuildConfigs.TryGetValue(Context.Guild.Id, out var conf))
                {
                    if (conf.Greeting == "**%user% has left**")
                    {
                        await Context.Channel.SendConfirmAsync("Set a user leave message\n" +
                                                               "Using `%user%` will transform into @user")
                            .ConfigureAwait(false);
                        return;
                    }
                    
                    var prompt = await PromptUserConfirmAsync(new EmbedBuilder()
                        .WithDescription("Do you want to clear user leave message?")).ConfigureAwait(false);

                    if (prompt)
                    {
                        if (await _greeting.ClearUserJoinLeaveMessage(GreetingService.JoinLeave.Leave, Context.Guild.Id).ConfigureAwait(false))
                        {
                            await Context.Channel.SendConfirmAsync(string.Empty, "User leave message has been cleared")
                                .ConfigureAwait(false);
                            return;
                        }

                        await Context.Channel.SendErrorAsync(string.Empty, "Something happened").ConfigureAwait(false);
                    }
                }
            }

            [Command("SetJoinLeaveChannel"), Summary("Set channel to send join and leave message")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetJoinLeaveChannel(SocketTextChannel channel = null)
            {
                if (channel is null && _guildService.AllGuildConfigs.TryGetValue(Context.Guild.Id, out var conf))
                {
                    if (conf.GreetChl == 0)
                    {
                        await Context.Channel.SendErrorAsync(string.Empty, "Specify a channel").ConfigureAwait(false);
                        return;
                    }

                    if (await PromptUserConfirmAsync(
                        new EmbedBuilder().WithDescription("Do you want to disable join/leave messages?")
                    ).ConfigureAwait(false))
                    {

                        await _greeting.SetChannel(Context.Guild.Id, 0).ConfigureAwait(false);
                        await Context.Channel.SendConfirmAsync("Cleared and disabled join/leave messages")
                            .ConfigureAwait(false);
                        return;
                    }
                }
                
                await _greeting.SetChannel(Context.Guild.Id, channel.Id).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"Set Join/Leave channel to {channel.Mention}")
                    .ConfigureAwait(false);
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
            public async Task FilterRegex([Remainder] string pattern = "")
            {
                var conf = await Database.Guilds.SingleAsync(x => x.GuildId.Equals(Context.Guild.Id)).ConfigureAwait(false);

                if (string.IsNullOrEmpty(conf.Regex) && string.IsNullOrEmpty(pattern))
                {
                    await Context.Channel.SendErrorAsync(string.Empty, "FilterRegex <regex>\n\nUsing this command is limited to advanced users\n" +
                        $"Test your pattern on {Format.Code("https://www.regextester.com/")}").ConfigureAwait(false);
                    return;
                }

                if (string.IsNullOrEmpty(pattern) && !string.IsNullOrEmpty(conf.Regex))
                {
                    if (!await PromptUserConfirmAsync(
                            new EmbedBuilder().WithDescription("Do you want to remove Regex filtering?\n\n" + 
                            $"Current Regex {Format.Code(conf.Regex)}"))
                        .ConfigureAwait(false)) return;
                    
                    _service.RegexFiltering.TryRemove(Context.Guild.Id, out _);
                    conf.Regex = null;
                    await Context.Channel.SendConfirmAsync("Regex filtering removed and disabled").ConfigureAwait(false);
                    return;
                }

                if (!string.IsNullOrEmpty(pattern))
                    try
                    {
                        if (pattern.Length < 25)
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

                if (conf.BotChannel is 0 && channel is null)
                {
                    await Context.Channel.SendConfirmAsync("Choose a channel to restrict this bots commands to").ConfigureAwait(false);
                    return;
                }

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

            [Command("BotNick"), Summary("Sets this bots nickname"), RequireUserPermission(GuildPermission.ManageNicknames)]
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
