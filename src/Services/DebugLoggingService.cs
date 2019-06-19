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
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");
        private readonly Logger _log;

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
            if (!File.Exists(LogFile))
                File.Create(LogFile).Dispose();
            
            var debugText =
                $"{DateTime.UtcNow:O} [{msg.Severity}] {msg.Source} : {msg.Exception?.ToString() ?? msg.Message}";
            
            //await File.AppendAllTextAsync(LogFile, debugText + "\n").ConfigureAwait(false);

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
            }
            return Task.CompletedTask;
            //await Console.Out.WriteLineAsync(debugText).ConfigureAwait(false);
        }
    }
}