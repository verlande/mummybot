using Discord;
using Discord.WebSocket;
using mummybot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
            if (_tag != null) _tag.Uses++;
        }

        public string DeleteTag(SocketUser user)
        {
            if (_tag != null && user.Id.Equals(_tag.Author))
            {
                _context.Remove(_tag);
                return $"``{_tag.Name}`` deleted";
            }
            else if (_tag == null)
                return "Tag doesn't exist";
            else if (_tag != null && !user.Id.Equals(_tag.Author))
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

        public Embed TagInfoEmbed()
        {
            if (_tag == null) return null;
            var author = _discord.GetUser(_tag.Author);
            return new EmbedBuilder()
                .WithTitle(_tag.Name)
                .AddField("Created by", author.Mention, true)
                .AddField("Date created", _tag.Createdat, true)
                //.AddField("Invoked by prefix", _tag.IsCommand, true)
                .AddField("Uses", _tag.Uses, true)
                .AddField("Last used by", _tag.LastUsedBy == null ? "Nobody" : _discord.GetUser((ulong)_tag.LastUsedBy).Mention, true)
                .WithColor(Utils.GetRandomColor())
                .Build();
        }
    }
}
