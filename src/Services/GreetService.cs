//using Discord;
//using Discord.WebSocket;
//using Microsoft.EntityFrameworkCore;
//using NLog;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace mummybot.Services
//{
//    public class GreetService : INService
//    {
//        private readonly mummybotDbContext Database;
//        public readonly DiscordSocketClient _discord;
//        private readonly Logger _log;
//        public ConcurrentDictionary<ulong, GreetSettings> GuildConfigsCache { get; }

//        public GreetService(DiscordSocketClient discord, mummybotDbContext context)
//        {
//            _discord = discord;
//            Database = context;
//            _log = LogManager.GetCurrentClassLogger();

//            GuildConfigsCache = new ConcurrentDictionary<ulong, GreetSettings>(
//                )

//            _discord.UserJoined += UserJoined;
//            _discord.UserLeft += UserLeft;
//        }

//        private Task UserLeft(IGuildUser user)
//        {
//            var _ = Task.Run(async () =>
//            {
//                try
//                {
//                    var conf = await Database.Guilds.SingleAsync(g => g.Id.Equals(user.GuildId));
//                    if (string.IsNullOrEmpty(conf.Goodbye)) return;
//                    var channel = (await user.Guild.GetTextChannelsAsync().ConfigureAwait(false)).SingleOrDefault(c => c.Id.Equals(conf.GreetChl));

//                    if (channel == null) return;

//                }
//                catch (Exception ex)
//                {

//                }
//            });
//        }

//        public GreetSettings GetOrAddSettingsForGuild(ulong guildId)
//        {
//            if (GuildConfigsCache.TryGetValue(guildId, out var settings) && settings != null) return settings;
//            GuildConfigsCache.TryAdd(guildId, settings);
//            return settings;
//        }

//        public async Task<bool> SetSettings(ulong guildId, GreetSettings settings)
//        {
//            if (settings.au)
//        }

//        public class GreetSettings
//        {
//            public int AutoDeleteGreetMessagesTimer { get; set; }
//            public int AutoDeleteByeMessagesTimer { get; set; }

//            public ulong GreetMessageChannelId { get; set; }
//            public ulong ByeMessageChannelId { get; set; }

//            public bool SendDmGreetMessage { get; set; }
//            public string DmGreetMessageText { get; set; }

//            public bool SendChannelGreetMessage { get; set; }
//            public string ChannelGreetMessageText { get; set; }

//            public bool SendChannelByeMessage { get; set; }
//            public string ChannelByeMessageText { get; set; }

//            public static GreetSettings Create(GuildConfig g) => new GreetSettings()
//            {
//                AutoDeleteByeMessagesTimer = g.AutoDeleteByeMessagesTimer,
//                AutoDeleteGreetMessagesTimer = g.AutoDeleteGreetMessagesTimer,
//                GreetMessageChannelId = g.GreetMessageChannelId,
//                ByeMessageChannelId = g.ByeMessageChannelId,
//                SendDmGreetMessage = g.SendDmGreetMessage,
//                DmGreetMessageText = g.DmGreetMessageText,
//                SendChannelGreetMessage = g.SendChannelGreetMessage,
//                ChannelGreetMessageText = g.ChannelGreetMessageText,
//                SendChannelByeMessage = g.SendChannelByeMessage,
//                ChannelByeMessageText = g.ChannelByeMessageText,
//            };
//        }

//    }
//}
