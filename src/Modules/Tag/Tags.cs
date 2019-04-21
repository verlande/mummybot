using Discord.Commands;
using System.Threading.Tasks;
using System;
using System.Linq;
using Discord;
using Discord.WebSocket;
using mummybot.Services;
using mummybot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Modules.Tag
{
    [Name("Tags"), Group("tag"), Alias("t")]
    public class TagModule : ModuleBase
    {
        public TagService Tags { get; set; }
        private readonly CommandService _commands;

        public TagModule(CommandService commands)
            => _commands = commands;

        [Command, Summary("Fetches a tag")]
        public async Task GetTag(string name)
        {
            var tag = Tags.GetTag(Database, name, Context.Guild);
            if (tag != null)
            {
                await ReplyAsync(tag.GetContent(name));
                tag.LastUsedBy(Context.User);
                tag.AddUse();
            }
        }

        [Command("Create"), Summary("Creates a tag")]
        public async Task CreateTag([Summary("Max length = 12")] string name, [Remainder, Summary("Max length = 255")]
            string content)
        {
            var blacklistCommands = _commands.Commands.Select(c => c.Name).ToList();
            var isTagBanned = Database.Users.Any(b =>
                b.TagBanned && b.GuildId.Equals(Context.Guild.Id) && b.UserId.Equals(Context.User.Id));

            if (Tags.GetTag(Database, name, Context.Guild).Exists())
            {
                await ReplyAsync("Tag exists choose another");
                return;
            }

            if (name.Length > 12 || content.Length > 255)
            {
                await ReplyAsync("Tag name or content length exceeded");
                return;
            }

            if (isTagBanned)
            {
                await ReplyAsync("You're banned from creating tags on this guild");
                return;
            }

            if (blacklistCommands.Any(b => b.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                await ReplyAsync("Blacklisted tag name");
                return;
            }

            Tags.CreateTag(Database, name, content, Context.User, Context.Guild);
            await ReplyAsync($"Tag ``{name}`` created");
        }

        [Command("Delete"), Summary("Owners or admins can delete a tag")]
        public async Task Delete(string name)
        {
            var userRole = (SocketGuildUser)Context.User;
            if (userRole.GuildPermissions.Administrator || await Database.Tags.AnyAsync(t => t.Name.Equals(name) && t.Author.Equals(Context.User.Id)))
                await ReplyAsync(Tags.GetTag(Database, name, Context.Guild).DeleteTag());
        }

        [Command("Info"), Summary("Get tag info")]
        public async Task Info(string name)
        {
            var tag = Tags.GetTag(Database, name, Context.Guild);
            if (!tag.Exists())
            {
                await ReplyAsync($"``{name}`` doesn't exist");
                return;
            }

            await ReplyAsync(String.Empty, embed: tag.TagInfoEmbed());
        }

        //TODO: Make this paginated
        [Command("List"), Summary("List all tags on this guild")]
        public async Task ListTags([Summary("List tags belonging to a user")]SocketGuildUser user = null, int page = 1)
        {
            var tagList = Database.Tags.Where(t => t.Guild.Equals(Context.Guild.Id));

            page--;

            if (page < 0 || page > 20) return;

            var tagsPerPage = 15;

            if (!tagList.Any()) await ReplyAsync("This guild does not have any tags");

            if (user == null)
            {
                await Context.SendPaginatedConfirmAsync(page, (currPage) => new EmbedBuilder()
                    .WithTitle($"List of tags - {Context.Guild.Name}")
                    .WithColor(Utils.GetRandomColor())
                    .WithDescription(string.Join("\n", tagList.Skip(currPage * tagsPerPage).Take(tagsPerPage).Select(x => $"`{x.Name}`"))),
                    tagList.Count(), tagsPerPage).ConfigureAwait(false);
            }
            else
            {
                await Context.SendPaginatedConfirmAsync(page, (currPage) => new EmbedBuilder()
                    .WithTitle($"{Utils.FullUserName(user)} tag list")
                    .WithColor(Utils.GetRandomColor())
                    .WithDescription(string.Join("\n", tagList.Skip(currPage * tagsPerPage).Take(tagsPerPage).Select(x => $"`{x.Name}`"))),
                    tagList.Count(), tagsPerPage).ConfigureAwait(false);
            }

            // if (user == null)
            // {
            //     var tagList = Database.Tags.Where(t => t.Guild.Equals(Context.Guild.Id))
            //         .Select(t => t.Name);
            //     if (!tagList.Any()) await Context.Channel.SendConfirmAsync("No tags to display from this guild");
            //     else await Context.Channel.SendConfirmAsync(string.Join("\n", tagList), "List of tags");
            // }
            // else
            // {
            //     var tagList = Database.Tags.Where(t => t.Guild.Equals(Context.Guild.Id)
            //                                            && t.Author.Equals(user.Id)).Select(t => t.Name);
            //     if (!tagList.Any()) await ReplyAsync($"{user.Nickname} doesn't have any tags to list");
            //     else
            //     {
            //         await ReplyAsync(String.Empty, embed: new EmbedBuilder()
            //             .WithAuthor(user.Nickname, user.GetAvatarUrl())
            //             .WithColor(Utils.GetRandomColor())
            //             .WithDescription(string.Join("\n", tagList)).Build());
            //     }
            // }
        }

        //[Command("Prefix"), Summary("Sets a tag to invoke without a prefix")]
        //[RequireUserPermission(GuildPermission.Administrator)]
        //public async Task RemovePrefix(string name)
        //{
        //    var result = Tags.GetTag(Database, name, Context.Guild).RemovePrefix();
        //    if (result == null)
        //    {
        //        await ReplyAsync($"Can't do that");
        //        return;
        //    }

        //    await ReplyAsync("Done");
        //}

        protected override async void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            Tags.PopulateList();
            await Database.SaveChangesAsync();
            Database.Dispose();
        }
    }
}