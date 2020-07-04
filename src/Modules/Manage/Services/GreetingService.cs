using System;
using Discord.WebSocket;
using mummybot.Services;
using NLog;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace mummybot.Modules.Manage.Services
{
    public class GreetingService : INService
    {
        private readonly DiscordSocketClient _discord;
        private readonly mummybotDbContext _context;
        private readonly GuildService _guildService;
        private readonly Logger _log = LogManager.GetLogger("logfile");

        public GreetingService(DiscordSocketClient discord, mummybotDbContext context, GuildService guildService)
        {
            _discord = discord;
            _context = context;
            _guildService = guildService;

            _discord.UserJoined += async user =>
            {
                await ReturnJoinLeaveMessage(JoinLeave.Join, user).ConfigureAwait(false);
            };

            _discord.UserLeft += async user =>
            {
                await ReturnJoinLeaveMessage(JoinLeave.Leave, user).ConfigureAwait(false);
            };
        }

        private async Task ReturnJoinLeaveMessage(JoinLeave type, IGuildUser user)
        {
            if (!_guildService.AllGuildConfigs.TryGetValue(user.Guild.Id, out var conf) ||
                conf.GreetChl is null && conf.GreetChl is 0) return;
            var channel = await user.Guild.GetTextChannelAsync((ulong)conf.GreetChl).ConfigureAwait(false);
            if (channel is null) return;
            switch (type)
            {
                case JoinLeave.Join:
                {
                    var joinMsg = conf.Greeting.Replace("%user%", user.Mention);

                    if (!string.IsNullOrEmpty(joinMsg))
                    {
                        await channel.SendMessageAsync(string.Empty, 
                                embed: GreetingEmbed(JoinLeave.Join, user, joinMsg))
                            .ConfigureAwait(false);
                    }

                    break;
                }
                case JoinLeave.Leave:
                {
                    var leaveMsg = conf.Goodbye.Replace("%user%", user.Mention);

                    if (!string.IsNullOrEmpty(leaveMsg))
                    {
                        await channel.SendMessageAsync(string.Empty,
                                embed: GreetingEmbed(JoinLeave.Leave, user, leaveMsg))
                            .ConfigureAwait(false);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        public async Task<bool> SetUserJoinLeaveMessage(JoinLeave type, ulong guildId, string message)
        {
            var gc = await _context.Guilds.SingleOrDefaultAsync(x => x.GuildId.Equals(guildId)).ConfigureAwait(false);

            if (gc is null) return false;

            try
            {
                _ = type switch
                {
                    JoinLeave.Join => gc.Greeting = message,
                    JoinLeave.Leave => gc.Goodbye = message,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
                };

                _guildService.AllGuildConfigs.AddOrUpdate(guildId, gc, (key, old) => gc);
                _context.Update(gc);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _log.Error(e);
                return false;
            }
        }

        public async Task<bool> ClearUserJoinLeaveMessage(JoinLeave type, ulong guildId)
        {
            try
            {
                var gc = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(guildId)).ConfigureAwait(false);
                
                _ = type switch
                {
                    JoinLeave.Join =>  gc.Greeting = string.Empty,
                    JoinLeave.Leave =>  gc.Goodbye = string.Empty,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
                };
                
                _guildService.AllGuildConfigs.AddOrUpdate(guildId, gc, (key, old) => gc);
                await _context.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                _log.Error(e);
                return false;
            }
        }

        public async Task SetChannel(ulong guildId, ulong channelId)
        {
            try
            {
                var gc = await _context.Guilds.SingleAsync(x => x.GuildId.Equals(guildId)).ConfigureAwait(false);
                gc.GreetChl = channelId;
                _guildService.AllGuildConfigs.AddOrUpdate(guildId, gc, (key, old) => gc);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }
        
        private Embed GreetingEmbed(JoinLeave type, IUser user, string msg)
        {
            var eb = new EmbedBuilder()
                .WithColor(Utils.GetRandomColor())
                .WithDescription(msg)
                .WithImageUrl(user.GetAvatarUrl());

            _ = type switch
            {
                JoinLeave.Join => eb.WithTitle("User Joined"),
                JoinLeave.Leave => eb.WithTitle("User Left"),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

            return eb.Build();
        }
        
        public enum JoinLeave { Join, Leave }
    }
}