using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Text;
using System.Threading.Tasks;
using mummybot.Extensions;
using Discord;
using mummybot.Services;
using System.Linq;

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

        [Command("Botlist"), Summary("Returns list of bots")]
        public async Task BotList()
        {
            var sb = new StringBuilder();
            foreach (var bot in Context.Guild.Users.Where(x => x.IsBot))
                    sb.AppendLine(Format.Bold(Utils.FullUserName(bot)) + $" {(string.IsNullOrEmpty(bot.Nickname) ? "" : $"({bot.Nickname})")}");

            await Context.Channel.SendConfirmAsync(sb.ToString(), "Bot List").ConfigureAwait(false);
        }
    }
}