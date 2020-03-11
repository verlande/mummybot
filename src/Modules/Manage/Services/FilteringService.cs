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
        private IServiceProvider _services;
        private readonly mummybotDbContext _context;

        public ConcurrentHashSet<ulong> InviteFiltering { get; }
        public ConcurrentDictionary<ulong, string> RegexFiltering { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public FilteringService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _log = LogManager.GetCurrentClassLogger();
            _context = context;

            InviteFiltering = new ConcurrentHashSet<ulong>(context.Guilds.Where(x => x.FilterInvites).Select(x => x.GuildId));
            RegexFiltering = new ConcurrentDictionary<ulong, string>(_context.Guilds.Where(x => x.Regex != null).ToDictionary(x => x.GuildId, x => x.Regex));

            discord.MessageReceived += (msg) =>
            {
                var _ = Task.Run(() =>
                {
                    var guild = (msg.Channel as ITextChannel)?.Guild;
                    //if ((IUserMessage)msg == null) return Task.CompletedTask;
                    var usrMsg = (IUserMessage)msg;

                    return guild is null ? Task.CompletedTask : RunBehavior(null, guild, usrMsg);
                });
                return Task.CompletedTask;
            };
        }

        public void Initialize(IServiceProvider service)
            => _services = service;

        private async Task<bool> RunBehavior(DiscordSocketClient _, IGuild guild, IMessage msg)
            => !(msg.Author is IGuildUser gu) //it's never filtered outside of guilds, and never block administrators
                ? false
                : !gu.GuildPermissions.Administrator && (await FilterInvites(guild, msg).ConfigureAwait(false) || await FilterRegex(guild, msg).ConfigureAwait(false));

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

            if (RegexFiltering.ContainsKey(guild.Id))
            {
                if (Regex.Match(msg.Content, RegexFiltering[guild.Id], RegexOptions.IgnoreCase).Success)
                    try
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                        return true;
                    }
                    catch (ArgumentException ex)
                    {
                        _log.Warn(ex);
                        return true;
                    }
            }
            return false;
        }
    }
}
