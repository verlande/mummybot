using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Models;
using mummybot.Services;

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
                if (!await Database.Bans.AnyAsync(x => x.UserId.Equals(user.Id)))
                {
                    await Database.Bans.AddAsync(new Bans
                    {
                        UserId = user.Id,
                        Reason = reason
                    });

                    _commandHandlerService.BannedUsers.AddOrUpdate(user.Id, false, (key, old) => false);
                    _log.Info($"User {user} ({user.Id}) blacklisted");
                }

                if (await Database.Bans.AnyAsync(x => x.UserId.Equals(user.Id)))
                {
                    Database.Bans.Remove(Database.Bans.Single(x => x.UserId.Equals(user.Id)));
                    _commandHandlerService.BannedUsers.TryRemove(user.Id, out _);
                    _log.Info($"User {user} ({user.Id}) removed from blacklist");
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        protected override async void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            await Database.SaveChangesAsync();
            await Database.DisposeAsync();
        }
    }
}