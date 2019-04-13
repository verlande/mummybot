using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mummybot.Extensions;


namespace mummybot.Modules.Utility
{
    [Name("Utility")]
    public partial class Utility : ModuleBase
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _command;

        public Utility(DiscordSocketClient client, CommandService command)
        {
            _client = client;
            _command = command;
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

            await Context.Channel.SendConfirmAsync(sb.ToString(), null);
        }

        [Command("Ping")]
        public async Task Ping()
        {
            await Context.Channel.SendConfirmAsync($"🏓 {_client.Latency.ToString()}ms");
        }

        //TODO: Make this paginated
        [Command("Bans"), Summary("Returns list of bans")]
        public async Task Bans()
        {
            var banList = Context.Guild.GetBansAsync().Result;
            var sb = new StringBuilder();

            try
            {
                if (banList.Count > 0)
                { 
                    foreach (var bans in banList) sb.AppendLine($"{bans.User} - {bans.Reason}");
                await Context.Channel.SendConfirmAsync(sb.ToString(), "List of bans").ConfigureAwait(false);
                }
                else
                    await Context.Channel.SendConfirmAsync("No bans to display");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync("Error fetching ban list", ex.Message);
            }
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