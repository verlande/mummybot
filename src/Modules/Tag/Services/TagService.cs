using Discord.WebSocket;
using mummybot.Services;
using mummybot.Modules.Tag.Controllers;
using NLog;
using System.Threading.Tasks;
using System.Linq;
using mummybot.Models;

namespace mummybot.Modules.Tag.Services
{
    public class TagService : INService
    {
        private readonly Logger _log;
        private readonly mummybotDbContext _context;
        private readonly DiscordSocketClient _discord;
        private readonly UserService _userService;

        public TagService(DiscordSocketClient discord, mummybotDbContext context, UserService userService)
        {
            _log = LogManager.GetCurrentClassLogger();
            _discord = discord;
            _context = context;
            _userService = userService;
        }


        public TagController GetTag(mummybotDbContext context, string name, SocketGuild guild)
        {
            var tag = context.Tags.SingleOrDefault(t => t.Name.Equals(name) && t.Guild.Equals(guild.Id));
            return tag == null ? new TagController(null, null, null) : new TagController(context, _discord, tag);
        }

        public async Task<TagController> CreateTag(mummybotDbContext context, string name, string content, SocketUser user, SocketGuild guild)
        {
            if (!await _userService.UserExists(user.Id, guild.Id))
                await _userService.AddUser((SocketGuildUser)user);
            
            var tag = new Tags
            {
                Name = name,
                Content = content,
                Author = user.Id,
                Guild = guild.Id
            };

            await _context.Tags.AddAsync(tag);
            await _context.SaveChangesAsync();
            _log.Info($"Inserted Tag \"{name}\" u: {user.Id} g: {guild.Id}");

            return new TagController(context, _discord, tag);
        }
    }
}
