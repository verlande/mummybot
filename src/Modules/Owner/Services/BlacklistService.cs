using Discord;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Common;
using mummybot.Services;
using NLog;
using System.Linq;
using System.Threading.Tasks;

namespace mummybot.Modules.Owner.Services
{
    public class BlacklistService : IEarlyBehavior, INService
    {
        private readonly Logger _log = LogManager.GetLogger("blacklist");
        private readonly DiscordSocketClient _discord;
        public int Priority => -50;
        public ModuleBehaviorType BehaviorType => ModuleBehaviorType.Blocker;

        public ConcurrentHashSet<ulong> GuildBlacklist { get; }
		public ConcurrentHashSet<ulong> UserBlacklist { get; } 

        public BlacklistService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _discord = discord;

            GuildBlacklist = new ConcurrentHashSet<ulong>(
                context.Guilds.Where(x => x.Blacklisted).Select(x => x.GuildId));
			
			UserBlacklist = new ConcurrentHashSet<ulong>(
				context.Blacklist.Select(x => x.UserId));

            _discord.JoinedGuild += async (guild) =>
            {
                if (GuildBlacklist.Contains(guild.Id))
                {
                    await guild.LeaveAsync().ConfigureAwait(false);
                    _log.Info($"Blocked guild {guild.Name} ({guild.Id})");
                }
                return;
            };
        }

        public Task<bool> RunBehavior(DiscordSocketClient _, IGuild guild, IUserMessage msg)
        {
            if (UserBlacklist.Contains(msg.Author.Id))
            {
                var argPos = 0;
                var usrMsg = (IUserMessage)msg;
                if (usrMsg.HasStringPrefix(CommandHandlerService.DefaultPrefix, ref argPos) ||
                    usrMsg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
                    return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
