using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Text;
using System.Threading.Tasks;
using mummybot.Extensions;
using Discord;
using mummybot.Services;

namespace mummybot.Modules.Utility
{
    [Name("Utility")]
    public partial class Utility : ModuleBase
    {
        private readonly CommandHandlerService _commandHandlerService;
        private readonly CommandService _commandService;
        
        public Utility(DiscordSocketClient client, CommandHandlerService commandHandlerService, CommandService commandService)
        {
            _commandHandlerService = commandHandlerService;
            _client = client;
            _commandService = commandService;
        }

        [Command("Ping")]
        public async Task Ping() => await Context.Channel.SendConfirmAsync($"🏓 {_client.Latency}ms").ConfigureAwait(false);

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
                    return;
                }
                await Context.Channel.SendConfirmAsync("No bans to display").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync("Error fetching ban list", ex.Message).ConfigureAwait(false);
            }
        }

        [Command("Botlist"), Summary("Returns list of bots")]
        public async Task BotList()
        {
            var sb = new StringBuilder();
            foreach (var bot in Context.Guild.Users)
                if (bot.IsBot)
                    sb.AppendLine(Format.Bold(Utils.FullUserName(bot)));
            await Context.Channel.SendConfirmAsync(sb.ToString(), "Bot List").ConfigureAwait(false);
        }
    }
}