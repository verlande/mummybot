using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace mummybot.Services
{
    public class GuildService
    {
        private readonly DiscordSocketClient _discord;
        private mummybotDbContext _context;
        List<SocketGuild> guildsList = new List<SocketGuild>();


        public GuildService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _discord = discord;
            _context = context;
            _discord.GuildAvailable += ListGuilds;
            _discord.JoinedGuild += JoinedGuild;
        }

        public async Task ListGuilds(SocketGuild guild)
        {
            foreach (var guilds in _discord.Guilds)
            {
                guildsList.Add(guild);
            }
        }

        private async Task JoinedGuild(SocketGuild guild) 
            => await guild.DefaultChannel.SendMessageAsync("hi am so sexy x");
    }
}