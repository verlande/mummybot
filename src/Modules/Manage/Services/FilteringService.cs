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

namespace mummybot.Modules.Manage.Services
{
    public class FilteringService : INService
    {
        private readonly Logger _log;
        private IServiceProvider _services;

        public ConcurrentHashSet<ulong> InviteFiltering { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public FilteringService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _log = LogManager.GetCurrentClassLogger();

            InviteFiltering = new ConcurrentHashSet<ulong>(context.Guilds.Select(x => x.GuildId));

            discord.MessageReceived += (msg) =>
            {
                var _ = Task.Run(() =>
                {
                    var guild = (msg.Channel as ITextChannel)?.Guild;
                    var usrMsg = (IUserMessage)msg;

                    return guild is null ? Task.CompletedTask : RunBehavior(null, guild, usrMsg);
                });
                return Task.CompletedTask;
            };
        }

        public void Initialize(IServiceProvider service)
            => _services = service;

        private async Task<bool> RunBehavior(DiscordSocketClient _, IGuild guild, IMessage msg)
            => msg.Author is IGuildUser gu && !gu.GuildPermissions.Administrator && !gu.IsBot && await FilterInvites(guild, msg).ConfigureAwait(false);

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
    }
}
