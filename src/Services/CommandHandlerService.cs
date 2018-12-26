using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace mummybot.Services
{
    public class CommandHandlerService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;
        
        public CommandHandlerService(
            DiscordSocketClient discord,
            CommandService commands,
            IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceived;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public static int MessagesIntercepted;
        public static int BotReplies;
        private async Task OnMessageReceived(SocketMessage s)
        {
            var config = new ConfigService();
            var prefix = config.Config["prefix"];
            
            if (!(s is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            if (message.Source == MessageSource.User) MessagesIntercepted++;

            var argPos = 0;
            var context = new SocketCommandContext(_discord, message);

            if (!(message.HasMentionPrefix(_discord.CurrentUser, ref argPos) ||
                  message.HasStringPrefix(prefix, ref argPos))) return;
            else
                BotReplies++;
            

            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.HasValue && result.Error.Value != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}