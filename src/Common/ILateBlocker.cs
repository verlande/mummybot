using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace mummybot.Common
{
    public interface ILateBlocker
    {
        Task<bool> TryBlockLate(DiscordSocketClient client, IUserMessage msg, IGuild guild,
            IMessageChannel channel, IUser user, string moduleName, string commandName);
    }
}
