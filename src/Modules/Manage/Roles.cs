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
        public class Roles : mummybotSubmodule
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

            [Command("Roleinfo"), Alias("ri")]
            public async Task RoleInfo(SocketRole role)
            {
                var eb = new EmbedBuilder
                {
                    Title = $"Role Info: {role.Name}",
                    Description = $"**Position:** {Context.Guild.Roles.Count - role.Position}/{Context.Guild.Roles.Count}\n" +
                    Format.Bold("Colour: ") + role.Color + "\n" +
                    $"**{role.Members.Count()} member** | {role.Members.Count(x => x.Status != UserStatus.Offline)} online\n" +
                    $"**Created:** {role.CreatedAt.UtcDateTime}\n" +
                    $"**Mentionable**: {role.IsMentionable}\n" +
                    $"**Permissions:** {string.Join("\n", role.Permissions.ToList())}",
                    ThumbnailUrl = $"https://www.colorhexa.com/{role.Color.ToString().Substring(1)}.png",
                    Color = role.Color
                };
                eb.WithFooter($"ID: {role.Id}");

                await ReplyAsync(string.Empty, embed: eb.Build());
            }

            [Command("Roles"), Summary("List roles of a user")]
            public async Task InRole(IGuildUser arg = null)
            {
                var channel = (ITextChannel)Context.Channel;
                var user = arg ?? (IGuildUser)Context.User;
                var roles = user.GetRoles().Except(new[] { channel.Guild.EveryoneRole }).OrderBy(r => r.Position);

                await Context.Channel.SendAuthorAsync(user, string.Join("\n", roles.Select(x => x.Mention)));
            }

            [Command("ListRoles"), Summary("Display all guild roles")]
            public async Task ListRoles()
            {
                var roles = Context.Guild.Roles;
                var sb = new StringBuilder();

                foreach (var role in roles)
                    sb.AppendLine($"``{role.Name}: {role.Id} {role.Color} MEMBERS: {role.Members.Count()}``");
                await ReplyAsync(sb.ToString());
                //await Context.Channel.SendConfirmAsync(sb.ToString(), null);
            }

            [Command("Checkperms"), Summary("View permissions of a user")]
            public async Task Perms(IGuildUser arg = null)
            {
                var sb = new StringBuilder();
                var user = arg ?? (IGuildUser)Context.User;
                var perms = user.GetPermissions((ITextChannel)Context.Channel);

                foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
                    sb.AppendLine($"{p.Name} : {p.GetValue(perms, null)}");
                await Context.Channel.SendAuthorAsync(user, sb.ToString(), $"User ID: {user.Id}");
            }
        }
    }
}