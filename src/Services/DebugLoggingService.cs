using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NLog;

namespace mummybot.Services
{
    public class DebugLoggingService
    {
        private string LogDirectory { get; }
        //private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.log");
        private readonly Logger _log;

        // ReSharper disable once SuggestBaseTypeForParameter
        public DebugLoggingService(DiscordSocketClient discord, CommandService commands)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "Debug Logs");

            discord.Log += OnLog;
            commands.Log += OnLog;

            _log = LogManager.GetCurrentClassLogger();
        }

        private Task OnLog(LogMessage msg)
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
            //if (!File.Exists(LogFile))
            //    File.Create(LogFile).Dispose();

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