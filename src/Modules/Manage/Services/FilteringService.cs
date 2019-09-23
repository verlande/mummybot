using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Services;
using mummybot.Modules.Tag.Controllers;
using mummybot.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
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
        private readonly mummybotDbContext _context;
        private readonly DiscordSocketClient _discord;
        private IServiceProvider _services;

        public ConcurrentHashSet<ulong> InviteFiltering { get; }

        public FilteringService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _log = LogManager.GetCurrentClassLogger();
            _discord = discord;
            _context = context;

            InviteFiltering = new ConcurrentHashSet<ulong>(_context.Guilds.Select(x => x.GuildId));

            _discord.MessageReceived += (msg) =>
            {
                var _ = Task.Run(() =>
                {
                    var guild = (msg.Channel as ITextChannel)?.Guild;
                    var usrMsg = (IUserMessage)msg;

                    if (guild is null || usrMsg is null) return Task.CompletedTask;
                    return RunBehavior(null, guild, usrMsg);
                });
                return Task.CompletedTask;
            };
        }

        public void Initialize(IServiceProvider service)
            => _services = service;

        public async Task<bool> RunBehavior(DiscordSocketClient _, IGuild guild, IUserMessage msg)
            => !(msg.Author is IGuildUser gu) ? false : (await FilterInvites(guild, msg).ConfigureAwait(false));

        public async Task<bool> FilterInvites(IGuild guild, IUserMessage msg)
        {
            if (guild is null || msg is null) return false;

            if ((InviteFiltering.Contains(guild.Id)) && msg.Content.IsDiscordInvite())
                try
                {
                    await msg.DeleteAsync().ConfigureAwait(false);
                    return true;
                }
                catch(HttpException ex)
                {
                    _log.Warn(ex);
                    return true;
                }
            return false;
        }
    }
}
