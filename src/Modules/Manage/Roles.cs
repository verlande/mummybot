using Discord;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mummybot.Modules.Manage
{
    public partial class Manage
    {
        [RequireBotPermission(GuildPermission.ManageRoles), RequireUserPermission(GuildPermission.ManageRoles)]
        public class Roles : mummybotSubmodule//<RoleCommandService>
        {
            [Command("Role"), Summary("Add or remove a role")]
            public async Task Role(IGuildUser usr, [Remainder] IRole role)
            {
                var guser = (IGuildUser)Context.User;
                var maxRole = guser.GetRoles().Max(x => x.Position);
                if ((Context.User.Id != Context.Guild.OwnerId) && (maxRole <= role.Position || maxRole <= usr.GetRoles().Max(x => x.Position)))
                    return;
                if (usr.RoleIds.Any(x => x.Equals(role.Id)))
                {
                    await usr.RemoveRoleAsync(role).ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync($"Removed role {role.Name} for " + Format.Bold(usr.ToString()))
                        .ConfigureAwait(false);
                    return;
                }

                try
                {
                    await usr.AddRoleAsync(role).ConfigureAwait(false);

                    await Context.Channel.SendConfirmAsync($"Set role {role.Name} for " + Format.Bold(usr.ToString()))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync("", "setrole_err").ConfigureAwait(false);
                    _log.Warn(ex ?? ex.InnerException);
                }
            }

            [Command("Rolerename"), Summary("Rename a role")]
            public async Task RoleRemove(IRole role, string newName)
            {
                var guser = (IGuildUser)Context.User;Console.WriteLine("test");
                if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                {
                    await Context.Channel.SendConfirmAsync($"Can't rename {role.Name}");
                    return;
                }
                try
                {
                    if (role.Position > Context.Guild.GetUser(guser.Id).GetRoles().Max(r => r.Position))
                    {
                        await Context.Channel.SendErrorAsync("renaming role", "Role in a higher position").ConfigureAwait(false);
                        return;
                    }
                    await role.ModifyAsync(g => g.Name = newName).ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync($"Renamed {role.Name} to {newName}").ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await Context.Channel.SendErrorAsync("renaming role", string.Empty).ConfigureAwait(false);
                }
            }

            [Command("Rolemention"), Summary("Set role to metionable")]
            public async Task RoleColour([Remainder] IRole role)
            {
                if (!role.IsMentionable)
                {
                    await role.ModifyAsync(r => r.Mentionable = true).ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(role.Mention + " is now mentionable").ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendConfirmAsync(role.Mention + " is already mentionable").ConfigureAwait(false);
                }
            }
        }
    }
}