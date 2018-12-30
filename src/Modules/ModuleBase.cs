using Discord.Commands;
using mummybot.Services;

namespace mummybot.Modules
{
    [RequireContext(ContextType.Guild)]
    public class ModuleBase : ModuleBase<SocketCommandContext>
    {
        public mummybotDbContext Database { get; set; }
    }
}