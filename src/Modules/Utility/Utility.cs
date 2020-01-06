using Discord.Commands;
using Discord.WebSocket;
using System;
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

        [Command("Ping")]
        public async Task Ping() => await Context.Channel.SendConfirmAsync($"🏓 {_client.Latency.ToString()}ms").ConfigureAwait(false);

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
                await Context.Channel.SendConfirmAsync("No bans to display").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync("Error fetching ban list", ex.Message).ConfigureAwait(false);
            }
        }
    }
}