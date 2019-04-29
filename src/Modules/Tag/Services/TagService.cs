﻿using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Services;
using mummybot.Modules.Tag.Controllers;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace mummybot.Modules.Tag.Services
{
    public class TagService : INService
    {
        private readonly Logger _log;
        private readonly mummybotDbContext _context;
        private readonly DiscordSocketClient _discord;
        private IServiceProvider _services;

        public TagService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _log = LogManager.GetCurrentClassLogger();
            _discord = discord;
            _context = context;
        }

        public void Initialize(IServiceProvider service)
            => _services = service;

        public TagController GetTag(mummybotDbContext context, string name, SocketGuild guild)
        {
            var tag = context.Tags.SingleOrDefault(t => t.Name.Equals(name) && t.Guild.Equals(guild.Id));
            return tag == null ? new TagController(null, null, null) : new TagController(context, _discord, tag);
        }

        public async Task<TagController> CreateTag(mummybotDbContext context, string name, string content, SocketUser user, SocketGuild guild)
        {
            var tag = new Models.Tags
            {
                Name = name,
                Content = content,
                Author = user.Id,
                Guild = guild.Id
            };

            await context.AddAsync(tag);
            return new TagController(context, _discord, tag);
        }
    }
}
