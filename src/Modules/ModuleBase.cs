using Discord.Commands;
using mummybot.Services;

namespace mummybot.Modules
{
    public class ModuleBase : ModuleBase<SocketCommandContext>
    {
        public mummybotDbContext Database { get; set; }
        public TagService Tags { get; set; }
    }
}