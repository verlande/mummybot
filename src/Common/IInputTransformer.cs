using System.Threading.Tasks;
using Discord;

namespace mummybot.Common
{
    public interface IInputTransformer
    {
        Task<string> TransformInput(IGuild guild, IMessageChannel channel, IUser user, string input);
    }
}
