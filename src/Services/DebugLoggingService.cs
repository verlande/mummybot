using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace mummybot.Services
{
    public class DebugLoggingService
    {
        private string LogDirectory { get; }
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        public DebugLoggingService(DiscordSocketClient discord, CommandService commands)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "Debug Logs");

            discord.Log += OnLog;
            commands.Log += OnLog;
        }

        private async Task OnLog(LogMessage msg)
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
            if (!File.Exists(LogFile))
                File.Create(LogFile).Dispose();
            
            var debugText =
                $"{DateTime.UtcNow:O} [{msg.Severity}] {msg.Source} : {msg.Exception?.ToString() ?? msg.Message}";

            await File.AppendAllTextAsync(LogFile, debugText + "\n").ConfigureAwait(false);

            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            await Console.Out.WriteLineAsync(debugText).ConfigureAwait(false);
        }
    }
}