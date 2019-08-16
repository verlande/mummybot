using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Modules.Tag.Controllers;
using mummybot.Models;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Services
{
    public class TagService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private IServiceProvider _services;
        private readonly mummybotDbContext _context;

        //private ModuleInfo _module;
        public static List<Tags> TagsList = new List<Tags>();

        public TagService(CommandService commands, DiscordSocketClient discord, mummybotDbContext context)
        {
            _commands = commands;
            _context = context;
            _discord = discord;
            _discord.MessageReceived += Invoke;
        }

        public void Initialize(IServiceProvider service)
        {
            _services = service;
            PopulateList();
        }

        public void PopulateList() => _context.Tags.ForEachAsync(t => TagsList.Add(t));

        private async Task Invoke(SocketMessage s)
        {
            var m = (SocketUserMessage) s;
            var context = new CommandContext(_discord, m);

            if (m.Author.IsBot) return;

            var tag = TagsList.Where(t => t.Name.Equals(m.Content) && t.Guild.Equals(context.Guild.Id)
                                                                   && t.IsCommand)
                .Select(t => t.Content).FirstOrDefault();

            if (tag != null && tag.Any())
                await s.Channel.SendMessageAsync(tag);
        }

        public TagController GetTag(mummybotDbContext context, string name,
            SocketGuild guild)
        {
            var tag = context.Tags.SingleOrDefault(t => t.Name.Equals(name) && t.Guild.Equals(guild.Id));
            //return new TagController(context, _discord, tag);
            
            return tag == null ? new TagController(null, null, null) : new TagController(context, _discord, tag);
        }

        public TagController CreateTag(mummybotDbContext context, string name, string content, SocketUser user,
            SocketGuild guild)
        {
            if (context.Tags.Any(t => t.Name.Equals(name) && t.Guild.Equals(guild.Id)))
                return new TagController(context, _discord, null);

            var tag = new Tags
            {
                Name = name,
                Content = content,
                Author = user.Id,
                Guild = guild.Id
            };

            context.Tags.Add(tag);
            TagsList.Add(tag);
            return new TagController(context, _discord, tag);
        }
    }
}