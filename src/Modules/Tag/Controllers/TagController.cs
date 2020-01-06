using Discord;
using Discord.WebSocket;
using mummybot.Models;
using System;

namespace mummybot.Modules.Tag.Controllers
{
    public class TagController
    {
        private readonly DiscordSocketClient _discord;
        private readonly mummybotDbContext _context;
        private readonly Tags _tag;

        public TagController(mummybotDbContext context, DiscordSocketClient discord, Tags tag)
        {
            _context = context;
            _discord = discord;
            _tag = tag;
        }

        public string GetContent(string name) 
            => _tag != null ? _tag.Content : $"Tag ``{name}`` doesn't exist";

        public void AddUse()
        {
            if (_tag != null)
            {
                _tag.Uses++;
                _tag.LastUsed = DateTime.Now;
            }
        }

        public string DeleteTag(SocketGuildUser user)
        {
            if (_tag != null && user.Id.Equals(_tag.Author) || user.GuildPermissions.Administrator)
            {
                _context.Remove(_tag);
                return $"``{_tag.Name}`` deleted";
            }
            if (_tag == null)
                return "Tag doesn't exist";
            if (_tag != null && !user.Id.Equals(_tag.Author))
                return "Tag doesn't belong to you";
            return "Can't delete tag";
        }

        public TagController LastUsedBy(SocketUser user)
        {
            _tag.LastUsedBy = user.Id;
            return new TagController(_context, _discord, _tag);
        }

        public bool Exists()
            => _tag != null;

        public Embed TagInfoEmbed() => _tag == null
                ? null
                : new EmbedBuilder()
                .WithTitle(_tag.Name)
                .AddField("Created by", _discord.GetUser(_tag.Author).Mention, true)
                .AddField("Date created", _tag.Createdat, true)
                //.AddField("Invoked by prefix", _tag.IsCommand, true)
                .AddField("Uses", _tag.Uses, true)
                .AddField("Last used by", _tag.LastUsedBy == null ? "Nobody" : _discord.GetUser((ulong)_tag.LastUsedBy).Mention, true)
                .AddField("Last used", _tag.LastUsed == null ? "N/A" : _tag.LastUsed.ToString(), true)
                .WithColor(Utils.GetRandomColor())
                .Build();
    }
}
