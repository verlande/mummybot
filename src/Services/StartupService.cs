using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace mummybot.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        public StartupService(DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
        }

        public async Task Start()
        {
            var config = new ConfigService();
            
            await _discord.LoginAsync(TokenType.Bot, config.Config["token"]);
            await _discord.StartAsync();
            await _discord.SetStatusAsync(UserStatus.Online);
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}