using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NLog;

namespace mummybot.Services
{
    public class CommandLogging
    {
        protected readonly Logger _log = LogManager.GetLogger("commandfile");

        public CommandLogging(CommandService commands)
        {
            commands.Log += OnLog;
            commands.CommandExecuted += Executed;
        }

        private async Task Executed(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            _log.Info($"Executing \"{command.Value.Name}\" for {context.User} ({context.User.Id}) in {context.Guild.Name} ({context.Guild.Id})/{context.Channel.Name}");
            if (result.IsSuccess)
                _log.Info($"Executed \"{command.Value.Name}\" for {context.User} ({context.User.Id}) in {context.Guild.Name} ({context.Guild.Id})/{context.Channel.Name}");
            if (!string.IsNullOrEmpty(result?.ErrorReason))
                _log.Error(result.ErrorReason);
                //await context.Channel.SendErrorAsync(string.Empty, result.ErrorReason).ConfigureAwait(false);
        }

        private Task OnLog(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    _log.Error(msg.Exception?.ToString() ?? msg.Message);
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    _log.Warn(msg.Message);
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    _log.Info(msg.Message);
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    //_log.Debug(msg.Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return Task.CompletedTask;
        }
    }

    public class LoggingService
    {
        protected readonly Logger _log = LogManager.GetLogger("logfile");

        // ReSharper disable once SuggestBaseTypeForParameter
        public LoggingService(DiscordSocketClient discord, CommandService commands) => discord.Log += OnLog;

        private Task OnLog(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    _log.Error(msg.Exception?.ToString() ?? msg.Message);
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    _log.Warn(msg.Message);
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    _log.Info(msg.Message);
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    _log.Debug(msg.Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return Task.CompletedTask;
        }
    }
}