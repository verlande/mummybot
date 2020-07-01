using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace mummybot.Services
{
    public static class NLogSetup
    {
        public static void SetupLogger()
        {
            var config = new LoggingConfiguration();
            var commandsConfig = new LoggingConfiguration();

            var logFile = new FileTarget("logfile")
            {
#if DEBUG
                FileName = @"Debug Logs/${shortdate}.txt",
#else
                FileName = @"Logs/${shortdate}.txt",
#endif
                Layout = "${longdate} | ${level:uppercase=True} | ${message}",
                LineEnding = LineEndingMode.Default
            };

            var logConsole = new ColoredConsoleTarget("logconsole")
            {
                Layout = "${longdate} | ${level:uppercase=True} | ${logger:shortName=true} | ${message}",
            };

            var commandsFile = new FileTarget("commandsFile")
            {
                FileName = @"Debug Logs/Commands/${shortdate}.txt",
                Layout = "${message}",
                LineEnding = LineEndingMode.Default
            };

            var target = new SplitGroupTarget();
            target.Targets.Add(logFile);
            target.Targets.Add(commandsFile);

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logFile);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, commandsFile);

            LogManager.Configuration = config;
        }
    }
}
