using Discord.Commands;
using System.Threading.Tasks;
using mummybot.Extensions;
using mummybot.Services;

namespace mummybot.Modules.Utility
{
    public partial class Utility : ModuleBase
    {
        [Command("Snipe"), Summary("Display last deleted message")]
        public async Task Snipe()
        {
            if (MessageService.snipeDict.ContainsKey(Context.Channel.Id))
            {
                var dict = MessageService.snipeDict[Context.Channel.Id];
                await Context.Channel.SendAuthorAsync(Context.Guild.GetUser(dict.AuthorId), dict.Content, dict.CreatedAt.ToString()).ConfigureAwait(false);
                return;
            }
            await Context.Channel.SendErrorAsync(string.Empty, "Nothing to snipe").ConfigureAwait(false);
        }
    }
}