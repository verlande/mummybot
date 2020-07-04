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
                context.GuildBlacklist.Select(x => x.GuildId));

            UserBlacklist = new ConcurrentHashSet<ulong>(
				context.UserBlacklist.Select(x => x.UserId));

            _discord.Ready += async () =>
            {
                var guilds = _discord.Guilds.Select(x => x.Id).ToList();
                foreach (var guild in guilds.Where(guild => GuildBlacklist.Contains(guild)))
                {
                    await _discord.GetGuild(guild).LeaveAsync().ConfigureAwait(false);
                    _log.Info($"Dropped guild {guild}");
                }
            };

            _discord.JoinedGuild += async guild =>
            {
                if (!GuildBlacklist.Contains(guild.Id)) return;
                await guild.LeaveAsync().ConfigureAwait(false);
                _log.Info($"Blocked guild {guild.Name} ({guild.Id})");
            };
        }

        public Task<bool> RunBehavior(DiscordSocketClient _, IGuild guild, IUserMessage msg)
        {
            if (!UserBlacklist.Contains(msg.Author.Id)) return Task.FromResult(false);
            var argPos = 0;
            return Task.FromResult(msg.HasStringPrefix(CommandHandlerService.DefaultPrefix, ref argPos) ||
                                   msg.HasMentionPrefix(_discord.CurrentUser, ref argPos));
        }
    }
}
