using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Models;
using mummybot.Services;
using mummybot.Extensions;

namespace mummybot.Modules.Owner
{
    [Name("Owner"), RequireOwner]
    public class Owner : ModuleBase
    {
        private readonly CommandHandlerService _commandHandlerService;

        public Owner(CommandHandlerService commandHandlerService)
        {
            _commandHandlerService = commandHandlerService;
        }

        [Command("Blacklist")]
        public async Task Blacklist(SocketUser user, [Remainder] string reason = "")
        {
            try
            {
                if (!await Database.Blacklist.AnyAsync(x => x.UserId.Equals(user.Id)))
                {
                    await Database.Blacklist.AddAsync(new Blacklist
                    {
                        UserId = user.Id,
                        Reason = reason
                    });

                    _commandHandlerService.BlacklistedUsers.AddOrUpdate(user.Id, false, (key, old) => false);
                    _log.Info($"User {user} ({user.Id}) blacklisted");
                }

                if (await Database.Blacklist.AnyAsync(x => x.UserId.Equals(user.Id)))
                {
                    Database.Blacklist.Remove(Database.Blacklist.Single(x => x.UserId.Equals(user.Id)));
                    _commandHandlerService.BlacklistedUsers.TryRemove(user.Id, out _);
                    _log.Info($"User {user} ({user.Id}) removed from blacklist");
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        [Command("Sql")]
        public async Task Sql([Remainder] string sql)
        {
            int res;
            try
            {
                res = await Database.Database.ExecuteSqlCommandAsync(sql).ConfigureAwait(false);
                await ReplyAsync(res.ToString()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(string.Empty, ex.Message ?? ex.InnerException.Message).ConfigureAwait(false);
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