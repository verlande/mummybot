using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using mummybot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mummybot.Modules.Manage.Services
{
    public class RoleService : INService
    {
        private readonly DiscordSocketClient _discord;
        private readonly mummybotDbContext _context;
        protected readonly Logger _log = LogManager.GetLogger("logfile");

        public ConcurrentDictionary<ulong, long[]> AutoAssignRoles { get; }

        public ConcurrentDictionary<ulong, ConcurrentQueue<(SocketGuildUser, long[])>> AssignQueue { get; }
            = new ConcurrentDictionary<ulong, ConcurrentQueue<(SocketGuildUser, long[])>>();

        public RoleService(DiscordSocketClient discord, mummybotDbContext context)
        {
            _discord = discord;
            _context = context;

            AutoAssignRoles = new ConcurrentDictionary<ulong, long[]>(
                _context.Guilds
                .Where(x => x.AutoAssignRoles.Any())
                .ToDictionary(k => k.GuildId, v => v.AutoAssignRoles));

            Task.Run(async () =>
            {
                while (true)
                {
                    var queues = AssignQueue
                        .Keys
                        .Select(k =>
                        {
                            if (AssignQueue.TryGetValue(k, out var q))
                            {
                                var l = new List<(SocketGuildUser, long[])>();
                                while (q.TryDequeue(out var x))
                                    l.Add(x);
                                return l;
                            }
                            return Enumerable.Empty<(SocketGuildUser, long[])>();
                        });


                    await Task.WhenAll(queues.Select(x => Task.Run(async () =>
                    {
                        foreach (var item in x)
                        {
                            var (user, roleId) = item;
                            
                            if (user.IsBot) return;
                            
                            try
                            {
//                                var roles = new List<IRole>();
//                                foreach (var r in roleId)
//                                {
//                                    roles.Add(user.Guild.Roles.FirstOrDefault(x => x.Id == (ulong)r));
//                                }

                                var roles = roleId.Select(r => user.Guild.Roles.FirstOrDefault(x => x.Id == (ulong) r)).Cast<IRole>().ToList();

                                if (roles.Any())
                                {
                                    await user.AddRolesAsync(roles).ConfigureAwait(false);
                                    await Task.Delay(250).ConfigureAwait(false);
                                }
                                else
                                {
                                    await DisableAar(user.Guild.Id).ConfigureAwait(false);
                                }
                            }
                            catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                await DisableAar(user.Guild.Id).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _log.Warn(ex);
                            }
                        }
                    })).Append(Task.Delay(3000))).ConfigureAwait(false);
                }
            });

            _discord.UserJoined += (user) =>
            {
                if (AutoAssignRoles.TryGetValue(user.Guild.Id, out var roleId) && roleId.Any())
                {
                    var pair = (user, roleId);
                    AssignQueue.AddOrUpdate(user.Guild.Id, new ConcurrentQueue<(SocketGuildUser, long[])>(new[] { pair }),
                        (key, old) =>
                        {
                            old.Enqueue(pair);
                            return old;
                        });
                }
                return Task.CompletedTask;
            };

        }

        public async Task EnableAar(ulong guildId, long roleId)
        {
            var role = new List<long> { roleId };
            var gc = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(guildId));
            role.AddRange(gc.AutoAssignRoles);
            gc.AutoAssignRoles = role.ToArray();
            _context.Guilds.Update(gc);
            await _context.SaveChangesAsync();

            AutoAssignRoles.AddOrUpdate(guildId, role.ToArray(), delegate { return role.ToArray(); });
        }

        public async Task DisableAar(ulong guildId)
        {
            var gc = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(guildId));

            gc.AutoAssignRoles = new long[] { };
            _context.Guilds.Update(gc);
            await _context.SaveChangesAsync();

            AutoAssignRoles.TryRemove(guildId, out _);
        }
    }
}
