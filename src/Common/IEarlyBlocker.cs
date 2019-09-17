using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace mummybot.Common
{
    public interface IEarlyBehavior
    {
        int Priority { get; }
        ModuleBehaviorType BehaviorType { get; }

        Task<bool> RunBehaviour(DiscordSocketClient client, IGuild guild, IUserMessage msg);
    }

    public enum ModuleBehaviorType
    {
        Blocker, Executor
    }
}
