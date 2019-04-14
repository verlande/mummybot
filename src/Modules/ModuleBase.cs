using Discord.Commands;
using NLog;

namespace mummybot.Modules
{
    [RequireContext(ContextType.Guild)]
    public class ModuleBase : ModuleBase<SocketCommandContext>
    {
        public mummybotDbContext Database { get; set; }
        public Logger _log;

        public ModuleBase() => _log = LogManager.GetCurrentClassLogger();
    }
}