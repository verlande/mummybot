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

namespace mummybot.Modules.Manage.Services
{
    public class FilteringService : INService
    {
        private readonly Logger _log;
        public ConcurrentHashSet<ulong> InviteFiltering { get; }
        public ConcurrentDictionary<ulong, string> RegexFiltering { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public FilteringService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _log = LogManager.GetCurrentClassLogger();

            InviteFiltering = new ConcurrentHashSet<ulong>(context.Guilds.Where(x => x.FilterInvites).Select(x => x.GuildId));
            RegexFiltering = new ConcurrentDictionary<ulong, string>(context.Guilds.Where(x => x.Regex != null).ToDictionary(x => x.GuildId, x => x.Regex));

            discord.MessageReceived += (msg) =>
            {
                Task.Run(() =>
                {
                    var guild = (msg.Channel as ITextChannel)?.Guild;
                    var usrMsg = (IUserMessage)msg;

                    return guild is null ? Task.CompletedTask : RunBehavior(guild, usrMsg);
                });
                return Task.CompletedTask;
            };
        }

        private async Task<bool> RunBehavior(IGuild guild, IMessage msg)
            => msg.Author is IGuildUser gu && (!gu.GuildPermissions.Administrator &&(await FilterInvites(guild, msg).ConfigureAwait(false) || await FilterRegex(guild, msg).ConfigureAwait(false)));

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
    }
}
