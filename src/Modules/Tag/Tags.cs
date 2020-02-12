using Discord.Commands;
using System.Threading.Tasks;
using System;
using System.Linq;
using Discord;
using Discord.WebSocket;
using mummybot.Extensions;
using mummybot.Attributes;
using mummybot.Modules.Tag.Services;

// ReSharper disable ImplicitlyCapturedClosure

namespace mummybot.Modules.Tag
{
    public class Tag
    {
        [Name("Tags"), Group("tag"), Alias("t")]
        public class TagCommands : mummybotSubmodule<TagService>
        {
            private TagService _tag { get; }
            private readonly CommandService _commands;

            public TagCommands(CommandService commands, TagService tag)
            {
                _commands = commands;
                _tag = tag;
            }

            [Command, Alias("Get"), Summary("Fetch a tag")]
            public async Task Get(string arg)
            {
                var tag = _tag.GetTag(Database, arg, Context.Guild);
                if (tag != null)
                {
                    await ReplyAsync(tag.GetContent(arg)).ConfigureAwait(false);
                    tag.AddUse();
                    tag.LastUsedBy(Context.User);
                }
            }

            [Command("Create"), Summary("Create a tag"), Cooldown(120, true)]
            public async Task CreateTag(string name, [Remainder] string content)
            {
                var blacklistCommands = _commands.Commands.Select(x => x.Name).ToArray();
                var isTagBanned = Database.Users.Any(b =>
                    b.TagBanned && b.GuildId.Equals(Context.Guild.Id) && b.UserId.Equals(Context.User.Id));

                if (isTagBanned)
                {
                    await Context.Channel.SendErrorAsync("creating tag", "You are banned from creating tags").ConfigureAwait(false);
                    return;
                }

                if (blacklistCommands.Any(b => b.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    await Context.Channel.SendErrorAsync("creating tag", "Cant use this tag name, choose another").ConfigureAwait(false);
                    return;
                }

                if (name.Length > 12 || content.Length > 255)
                {
                    await Context.Channel.SendErrorAsync("creating tag", "Tag name should be less than 12 chars" +
                        "\nTag content should be less than 255 chars").ConfigureAwait(false);
                    return;
                }

                if (_tag.GetTag(Database, name, Context.Guild).Exists())
                {
                    await Context.Channel.SendErrorAsync("creating tag", "Tag already exists, pick another name").ConfigureAwait(false);
                    return;
                }
                await _tag.CreateTag(Database, name, content, Context.User, Context.Guild);
                await Context.Channel.SendConfirmAsync($"{name} created").ConfigureAwait(false);
            }

            [Command("Delete")]
            public async Task Delete(string name) => await Context.Channel.SendConfirmAsync(_tag.GetTag(Database, name, Context.Guild)
                    .DeleteTag((SocketGuildUser)Context.User)).ConfigureAwait(false);

            [Command("List"), Summary("List all tags on this guild")]
            public async Task ListTags([Summary("List tags belonging to a user or guild")]SocketGuildUser user = null, int page = 1)
            {
                var tagList = user != null ? Database.Tags.Where(t => t.Guild.Equals(Context.Guild.Id) && t.Author.Equals(user.Id)) : Database.Tags.Where(t => t.Guild.Equals(Context.Guild.Id));

                page--;

                if (page < 0 || page > 20) return;

                const int tagsPerPage = 15;

                if (!tagList.Any())
                {
                    await ReplyAsync("This user or guild does not have any tags").ConfigureAwait(false);
                    return;
                }

                if (user == null)
                    await Context.SendPaginatedConfirmAsync(page, (currPage) =>
                        {
                            return new EmbedBuilder()
                                .WithTitle($"List of tags - {Context.Guild.Name}")
                                .WithColor(Utils.GetRandomColor())
                                .WithDescription(string.Join("\n",
                                    tagList.Skip(currPage * tagsPerPage).Take(tagsPerPage).Select(x => $"`{x.Name}`")));
                        },
                        tagList.Count(), tagsPerPage).ConfigureAwait(false);
                else
                    await Context.SendPaginatedConfirmAsync(page, (currPage) =>
                        {
                            return new EmbedBuilder()
                                .WithTitle($"{Utils.FullUserName(user)} tag list")
                                .WithColor(Utils.GetRandomColor())
                                .WithDescription(string.Join("\n",
                                    tagList.Skip(currPage * tagsPerPage).Take(tagsPerPage).Select(x => $"`{x.Name}`")));
                        },
                        tagList.Count(), tagsPerPage).ConfigureAwait(false);
            }

            [Command("Info"), Summary("Get tag info")]
            public async Task Info(string name)
            {
                var tag = _tag.GetTag(Database, name, Context.Guild);
                if (!tag.Exists())
                {
                    await Context.Channel.SendErrorAsync(string.Empty, $"{name} doesn't exist").ConfigureAwait(false);
                    return;
                }
                await ReplyAsync(string.Empty, embed: tag.TagInfoEmbed()).ConfigureAwait(false);
            }

            protected override async void AfterExecute(CommandInfo command)
            {
                base.AfterExecute(command);
                //Tags.PopulateList();
                await Database.SaveChangesAsync();
                Database.Dispose();
            }
        }
    }
}