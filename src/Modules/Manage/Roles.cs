using Discord;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Extensions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mummybot.Modules.Manage
{
    public partial class Manage
    {
        [Summary("Role management")]
        public class Roles : mummybotSubmodule
        {
            [Command("Role"), Summary("Add or remove a role"), Remarks("<user> <role>"),
                RequireBotPermission(GuildPermission.ManageRoles), RequireUserPermission(GuildPermission.ManageRoles)]
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
                    await Context.Channel.SendErrorAsync(string.Empty, "setrole_err").ConfigureAwait(false);
                    _log.Warn(ex);
                }
            }

            [Command("Rolerename"), Summary("Rename a role"), Remarks("<role> <rolename>"),
                RequireBotPermission(GuildPermission.ManageRoles), RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task RoleRemove(IRole role, [Remainder] string newName)
            {
                var guser = (IGuildUser)Context.User;
                if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                {
                    await Context.Channel.SendErrorAsync(string.Empty, $"Can't rename {role.Name}").ConfigureAwait(false);
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
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync("renaming role", string.Empty).ConfigureAwait(false);
                    _log.Warn(ex ?? ex.InnerException);
                }
            }

            [Command("Rolemention"), Summary("Set role to mentionable"), Remarks("<role>"),
                RequireBotPermission(GuildPermission.ManageRoles), RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task RoleColour([Remainder] IRole role)
            {
                if (!role.IsMentionable)
                {
                    await role.ModifyAsync(r => r.Mentionable = true).ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(role.Mention + " is now mentionable").ConfigureAwait(false);
                    return;
                }
                await role.ModifyAsync(r => r.Mentionable = false).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"Removed {role.Mention} as a mentioned role").ConfigureAwait(false);
            }

            [Command("Roleinfo"), Alias("ri"), Remarks("<role>")]
            public async Task RoleInfo(SocketRole role)
            {
                await ReplyAsync(string.Empty, embed: new EmbedBuilder
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
                }.WithFooter($"Role ID: {role.Id}").Build()).ConfigureAwait(false);
            }

            //[Command("Userroles"), Summary("List roles of a user"), Remarks("<user>")]
            //public async Task InRole(IGuildUser arg = null)
            //{
            //    var channel = (ITextChannel)Context.Channel;
            //    var user = arg ?? (IGuildUser)Context.User;
            //    var roles = user.GetRoles().Except(new[] { channel.Guild.EveryoneRole }).OrderBy(r => r.Position);

            //    await Context.Channel.SendAuthorAsync(user, string.Join("\n", roles.Select(x => x.Mention)), $"User ID: {user.Id}").ConfigureAwait(false);
            //}

            [Command("Roles"), Summary("Display all user or guild roles"), Remarks("[@user]")]
            public async Task ListRoles(IGuildUser user =  null)
            {
                var page = 1;
                page--;
                var currPage = 0;
                const int rolesPerPage = 25;
                var eb = new EmbedBuilder().WithColor(Utils.GetRandomColor());

                var roles = user == null ? eb.WithTitle($"{Context.Guild.Name} role list").WithDescription(string.Join("\n", Context.Guild.Roles.Skip(currPage * rolesPerPage).Take(rolesPerPage).Select(x => x.Mention)))
                    : eb.WithTitle($"{user} role list").WithDescription(string.Join("\n", user.GetRoles().Skip(currPage * rolesPerPage).Take(rolesPerPage).Select(x => x.Mention)));

                await Context.SendPaginatedConfirmAsync(page, (currPage) => eb,
                    Context.Guild.Roles.Count, rolesPerPage).ConfigureAwait(false);
            }

            [Command("Checkperms"), Summary("View permissions of a user"), Remarks("<user>")]
            public async Task Perms(IGuildUser arg = null)
            {
                var sb = new StringBuilder();
                var user = arg ?? (IGuildUser)Context.User;
                var perms = user.GetPermissions((ITextChannel)Context.Channel);

                foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
                    sb.AppendLine($"{p.Name} : {p.GetValue(perms, null)}");
                await Context.Channel.SendAuthorAsync(user, sb.ToString(), $"User ID: {user.Id}").ConfigureAwait(false);
            }
        }
    }
}