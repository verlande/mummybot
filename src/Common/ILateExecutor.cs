using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace mummybot.Common
{
    public interface ILateExecutor
    {
        Task LateExecute(DiscordSocketClient client, IGuild guild, IUserMessage msg);
    }
}