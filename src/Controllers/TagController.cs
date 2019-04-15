using System;
using Discord.WebSocket;
using Discord;
using mummybot.Models;

namespace mummybot.Controllers
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

        public string DeleteTag()
        {
            if (_tag == null)
                return $"``{_tag.Name}`` does not exist";

            var reply = $"``{_tag.Name}`` deleted";
            _context.Tags.Remove(_tag);
            return reply;
        }

        public string GetContent(string name)
            => _tag != null ? _tag.Content : $"Tag ``{name}`` doesn't exist";

        public void AddUse()
        { 
            if (_tag != null) _tag.Uses += 1;
        }

        public bool Exists()
            => _tag != null;

        //public TagController RemovePrefix()
        //{
        //    if (_tag == null) return new TagController(null, null, null);
        //    _tag.IsCommand = true;
        //    TagService.TagsList.Add(_tag);
        //    return new TagController(_context, _discord, _tag);
        //}

        public TagController LastUsedBy(SocketUser user)
        {
            _tag.LastUsedBy = user.Id;
            return new TagController(_context, _discord, _tag);
        }

        public Embed GetEmbed()
        {
            var author = _discord.GetUser(_tag.Author);
            return new EmbedBuilder()
                .WithTitle(_tag.Name)
                .WithDescription(_tag.Content)
                .WithColor(Color.DarkGreen)
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(author?.Username ?? String.Empty)
                    .WithIconUrl(author?.GetAvatarUrl() ?? String.Empty))
                .Build();
        }

        public Embed TagInfoEmbed()
        {
            if (_tag == null) return null;
            var author = _discord.GetUser(_tag.Author);
            return new EmbedBuilder()
                .WithTitle($"Tag info: {_tag.Name}")
                .AddField("Created by", author.Mention, true)
                .AddField("Date created", _tag.Createdat, true)
                .AddField("Invoked by prefix", _tag.IsCommand, true)
                .AddField("Uses", _tag.Uses, true)
                .AddField("Last used by", _tag.LastUsedBy == null ? "Nobody" : _discord.GetUser((ulong) _tag.LastUsedBy).Mention, true)
                .WithColor(Utils.GetRandomColor())
                .Build();
        }
    }
}
