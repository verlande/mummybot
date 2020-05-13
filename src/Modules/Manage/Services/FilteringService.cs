using Discord.WebSocket;
using mummybot.Services;
using mummybot.Common;
using NLog;
using System;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using mummybot.Extensions;
using Discord.Net;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Discord.Commands;

namespace mummybot.Modules.Manage.Services
{
    public class FilteringService : IEarlyBehavior, INService
    {
        private readonly Logger _log;
        private readonly DiscordSocketClient _discord;
        public ConcurrentHashSet<ulong> InviteFiltering { get; }
        public ConcurrentDictionary<ulong, string> RegexFiltering { get; }
        public ConcurrentDictionary<ulong, ulong> BotRestriction { get; }

        public int Priority => -50;
        public ModuleBehaviorType BehaviorType => ModuleBehaviorType.Blocker;

        // ReSharper disable once SuggestBaseTypeForParameter
        public FilteringService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _log = LogManager.GetCurrentClassLogger();
            _discord = discord;

            InviteFiltering = new ConcurrentHashSet<ulong>(context.Guilds.Where(x => x.FilterInvites).Select(x => x.GuildId));
            RegexFiltering = new ConcurrentDictionary<ulong, string>(context.Guilds.Where(x => x.Regex != null).ToDictionary(x => x.GuildId, x => x.Regex));
            BotRestriction = new ConcurrentDictionary<ulong, ulong>(
                context.Guilds.Where(x => x.BotChannel != 0)
                    .ToDictionary(k => k.GuildId, v => v.BotChannel));
        }

        public async Task<bool> RunBehavior(DiscordSocketClient _, IGuild guild, IUserMessage msg) 
            => msg.Author is IGuildUser gu && !gu.IsBot && !gu.GuildPermissions.Administrator &&
                    await IsBotChannel(guild, msg).ConfigureAwait(false) || 
                    await FilterInvites(guild, msg).ConfigureAwait(false) ||
                    await FilterRegex(guild, msg).ConfigureAwait(false);

        private async Task<bool> FilterInvites(IGuild guild, IMessage msg)
        {
            if (guild is null || msg is null) return false;

            if ((!InviteFiltering.Contains(guild.Id)) || !msg.Content.IsDiscordInvite()) return false;
            try
            {
                await msg.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            catch (HttpException ex)
            {
                _log.Warn(ex);
                return true;
            }
        }

        private async Task<bool> FilterRegex(IGuild guild, IMessage msg)
        {
            if (guild is null || msg is null) return false;

            if (!RegexFiltering.ContainsKey(guild.Id) ||
                !Regex.Match(msg.Content, RegexFiltering[guild.Id], RegexOptions.IgnoreCase).Success) return false;
            try
            {
                await msg.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            catch (ArgumentException ex)
            {
                _log.Warn(ex);
            }

            return false;
        }

        private async Task<bool> IsBotChannel(IGuild guild, IUserMessage usrMsg)
        {
            if (guild is null || usrMsg is null) return false;

            var argPos = 0;
            var isCommand = usrMsg.HasStringPrefix(new ConfigService().Config["prefix"], ref argPos) ||
                            usrMsg.HasMentionPrefix(_discord.CurrentUser, ref argPos);
            
            if (!isCommand || !BotRestriction.TryGetValue(guild.Id, out var channelId) ||
                usrMsg.Channel.Id == channelId) return false;
            await usrMsg.Channel.SendConfirmAsync($"My command have been restricted to <#{channelId}>").ConfigureAwait(false);
            return true;
        }
    }
}
