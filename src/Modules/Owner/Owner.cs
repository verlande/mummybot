using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Models;
using mummybot.Extensions;
using mummybot.Modules.Owner.Services;
using Discord;
using System.Text;

namespace mummybot.Modules.Owner
{
    [Name("Owner"), RequireOwner]
    public class Owner : ModuleBase<BlacklistService>
    {
        [Command("Listguilds")]
        public async Task Listguilds()
        {
            var sb = new StringBuilder();
            var guilds = _client.Guilds.Select(x => new { x.Name, x.Users, x.Owner, x.Id });

            foreach (var guild in guilds)
                sb.AppendLine($"{guild.Name} ({guild.Id}) [{guild.Users.Count}] Owner: {guild.Owner}");

            await ReplyAsync(Format.Code(sb.ToString())).ConfigureAwait(false);
        }

        [Command("ublacklist")]
        public async Task Blacklist(SocketUser user, [Remainder] string reason = "")
        {
            try
            {
                if (!await Database.UserBlacklist.AnyAsync(x => x.UserId.Equals(user.Id)))
                {
                    await Database.UserBlacklist.AddAsync(new UserBlacklist
                    {
                        UserId = user.Id,
                        Reason = reason
                    });

                    _service.UserBlacklist.Add(user.Id);
                    await Context.Channel.SendConfirmAsync("User blacklisted").ConfigureAwait(false);
                    return;
                }

                if (await Database.UserBlacklist.AnyAsync(x => x.UserId.Equals(user.Id)))
                {
                    Database.UserBlacklist.Remove(Database.UserBlacklist.Single(x => x.UserId.Equals(user.Id)));
                    _service.UserBlacklist.TryRemove(user.Id);
                    await Context.Channel.SendConfirmAsync($"User un-blacklisted").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        [Command("gblacklist")]
        public async Task GuildBlackList(ulong guildId, [Remainder] string reason = "")
        {
            try
            {
                var guild = _client.GetGuild(guildId);
                guild?.LeaveAsync().ConfigureAwait(false);

                _service.GuildBlacklist.Add(guildId);
                await Database.GuildBlacklist.AddAsync(new GuildBlacklist
                {
                    GuildId = guildId,
                    Reason = reason
                }).ConfigureAwait(false);
                await Database.SaveChangesAsync().ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("Guild has been blacklisted").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        [Command("Sql")]
        public async Task Sql([Remainder] string sql)
        {
            try
            {
                var res = await Database.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);
                await ReplyAsync(res.ToString()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(string.Empty, ex.Message ?? ex.InnerException?.Message).ConfigureAwait(false);
                _log.Error(ex);
            }
        }

        protected override void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            Database.SaveChanges();
            Database.Dispose();
        }
    }
}