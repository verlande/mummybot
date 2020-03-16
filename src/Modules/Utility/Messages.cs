using System;
using Discord.Commands;
using System.Threading.Tasks;
using mummybot.Extensions;
using mummybot.Services;

namespace mummybot.Modules.Utility
{
    public partial class Utility
    {
        [Command("Snipe"), Summary("Display last deleted message")]
        public async Task Snipe()
        {
            if (MessageService.SnipeDict.ContainsKey(Context.Channel.Id))
            {
                var dict = MessageService.SnipeDict[Context.Channel.Id];
                // ReSharper disable once SpecifyACultureInStringConversionExplicitly
                await Context.Channel.SendAuthorAsync(Context.Guild.GetUser(dict.AuthorId), dict.Content, $"Sent at {dict.CreatedAt}").ConfigureAwait(false);
                return;
            }
            await Context.Channel.SendErrorAsync(string.Empty, "Nothing to snipe").ConfigureAwait(false);
        }
    }
}