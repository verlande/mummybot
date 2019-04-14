using NLog;
using NLog.Config;
using NLog.Targets;

namespace mummybot.Services
{
    public static class NLogSetup
    {
        public static void SetupLogger()
        {
            var config = new LoggingConfiguration();
            var logFile = new FileTarget("logfile")
            {
                FileName = @"Debug Logs/${shortdate}.txt",
                Layout = "${longdate} | ${level:uppercase=True} | ${message}",
                LineEnding = LineEndingMode.Default
            };

            var logConsole = new ColoredConsoleTarget("logconsole")
            {
                Layout = "${longdate} ${logger:shortName=True} | ${level:uppercase=True} | ${message}"
            };

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logFile);

            LogManager.Configuration = config;
        }
    }
}
