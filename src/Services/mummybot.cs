using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using mummybot.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using mummybot.Extensions;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using mummybot.Services;

namespace mummybot.Services
{
    public class mybot
    {
        private Logger _log;

        public DiscordSocketClient Client { get; }
        public CommandService CommandService { get; }

        private readonly DbService _db;
        public static ImmutableArray<GuildConfig> AllGuildConfigs { get; private set; }

        /* I don't know how to make this not be static
         * and keep the convenience of .WithOkColor
         * and .WithErrorColor extensions methods.
         * I don't want to pass botconfig every time I 
         * want to send a confirm or error message, so
         * I'll keep this for now */
        public static Color OkColor { get; set; }
        public static Color ErrorColor { get; set; }

        public TaskCompletionSource<bool> Ready { get; private set; } = new TaskCompletionSource<bool>();

        public IServiceProvider Services { get; private set; }

        private readonly BotConfig _botConfig;

        public int GuildCount =>
            Client.Guilds.Count;

        public event Func<GuildConfig, Task> JoinedGuild = delegate { return Task.CompletedTask; };

        public mybot(int shardId, int parentProcessId)
        {
            if (shardId < 0)
                throw new ArgumentOutOfRangeException(nameof(shardId));

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
#if GLOBAL_NADEKO
                MessageCacheSize = 0,
#else
                MessageCacheSize = 50,
#endif
                LogLevel = LogSeverity.Warning,
                ConnectionTimeout = int.MaxValue,
                ShardId = shardId,
                AlwaysDownloadUsers = false,
            });

            CommandService = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync,
            });

            using (var uow = _db.GetDbContext())
            {
                _botConfig = uow.BotConfig.GetOrCreate();
                OkColor = new Color(Convert.ToUInt32(_botConfig.OkColor, 16));
                ErrorColor = new Color(Convert.ToUInt32(_botConfig.ErrorColor, 16));
                uow.SaveChanges();
            }

            SetupShard(parentProcessId);

#if GLOBAL_NADEKO || DEBUG
            Client.Log += Client_Log;
#endif
        }

        private void StartSendingData()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var data = new ShardComMessage()
                    {
                        ConnectionState = Client.ConnectionState,
                        Guilds = Client.ConnectionState == ConnectionState.Connected ? Client.Guilds.Count : 0,
                        ShardId = Client.ShardId,
                        Time = DateTime.UtcNow,
                    };

                    await Task.Delay(7500).ConfigureAwait(false);
                }
            });
        }

        private List<ulong> GetCurrentGuildIds()
        {
            return Client.Guilds.Select(x => x.Id).ToList();
        }

        public IEnumerable<GuildConfig> GetCurrentGuildConfigs()
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.GuildConfigs.GetAllGuildConfigs(GetCurrentGuildIds()).ToImmutableArray();
            }
        }

        private void AddServices()
        {
            var startingGuildIdList = GetCurrentGuildIds();

            //this unit of work will be used for initialization of all modules too, to prevent multiple queries from running
            using (var uow = _db.GetDbContext())
            {
                var sw = Stopwatch.StartNew();

                var _bot = Client.CurrentUser;

                uow.DiscordUsers.EnsureCreated(_bot.Id, _bot.Username, _bot.Discriminator, _bot.AvatarId);

                AllGuildConfigs = uow.GuildConfigs.GetAllGuildConfigs(startingGuildIdList).ToImmutableArray();

                var s = new ServiceCollection()
                    .AddSingleton(_db)
                    .AddSingleton(Client)
                    .AddSingleton(CommandService)
                    .AddSingleton(this)
                    .AddSingleton(uow);

                

                s.LoadFrom(Assembly.GetAssembly(typeof(CommandHandlerService)));

                //initialize Services
                Services = s.BuildServiceProvider();
                var commandHandler = Services.GetService<CommandHandlerService>();
                //what the fluff
                commandHandler.AddServices(s);
                LoadTypeReaders(typeof(mybot).Assembly);

                sw.Stop();
                _log.Info($"All services loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }
        }

        private IEnumerable<object> LoadTypeReaders(Assembly assembly)
        {
            Type[] allTypes;
            try
            {
                allTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                _log.Warn(ex.LoaderExceptions[0]);
                return Enumerable.Empty<object>();
            }
            var filteredTypes = allTypes
                .Where(x => x.IsSubclassOf(typeof(TypeReader))
                    && x.BaseType.GetGenericArguments().Length > 0
                    && !x.IsAbstract);

            var toReturn = new List<object>();
            foreach (var ft in filteredTypes)
            {
                var x = (TypeReader)Activator.CreateInstance(ft, Client, CommandService);
                var baseType = ft.BaseType;
                var typeArgs = baseType.GetGenericArguments();
                try
                {
                    CommandService.AddTypeReader(typeArgs[0], x);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    throw;
                }
                toReturn.Add(x);
            }

            return toReturn;
        }

        private async Task LoginAsync(string token)
        {
            var clientReady = new TaskCompletionSource<bool>();

            Task SetClientReady()
            {
                var _ = Task.Run(async () =>
                {
                    clientReady.TrySetResult(true);
                    try
                    {
                        foreach (var chan in (await Client.GetDMChannelsAsync().ConfigureAwait(false)))
                        {
                            await chan.CloseAsync().ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    finally
                    {

                    }
                });
                return Task.CompletedTask;
            }

            //connect
            _log.Info("Shard {0} logging in ...", Client.ShardId);
            await Client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            await Client.StartAsync().ConfigureAwait(false);
            Client.Ready += SetClientReady;
            await clientReady.Task.ConfigureAwait(false);
            Client.Ready -= SetClientReady;
            Client.JoinedGuild += Client_JoinedGuild;
            Client.LeftGuild += Client_LeftGuild;
            _log.Info("Shard {0} logged in.", Client.ShardId);
        }

        private Task Client_LeftGuild(SocketGuild arg)
        {
            _log.Info("Left server: {0} [{1}]", arg?.Name, arg?.Id);
            return Task.CompletedTask;
        }

        private Task Client_JoinedGuild(SocketGuild arg)
        {
            _log.Info("Joined server: {0} [{1}]", arg?.Name, arg?.Id);
            var _ = Task.Run(async () =>
            {
                GuildConfig gc;
                using (var uow = _db.GetDbContext())
                {
                    gc = uow.GuildConfigs.ForId(arg.Id);
                }
                await JoinedGuild.Invoke(gc).ConfigureAwait(false);
            });
            return Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            var sw = Stopwatch.StartNew();

            await LoginAsync("").ConfigureAwait(false);

            _log.Info($"Shard {Client.ShardId} loading services...");
            try
            {
                AddServices();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }

            sw.Stop();
            _log.Info($"Shard {Client.ShardId} connected in {sw.Elapsed.TotalSeconds:F2}s");

            var commandHandler = Services.GetService<CommandHandlerService>();
            var CommandService = Services.GetService<CommandService>();

            // start handling messages received in commandhandler
            await commandHandler.StartHandling().ConfigureAwait(false);

            var _ = await CommandService.AddModulesAsync(this.GetType().GetTypeInfo().Assembly, Services)
                .ConfigureAwait(false);

            StartSendingData();
            Ready.TrySetResult(true);
            _log.Info($"Shard {Client.ShardId} ready.");
        }

        private Task Client_Log(LogMessage arg)
        {
            _log.Warn(arg.Source + " | " + arg.Message);
            if (arg.Exception != null)
                _log.Warn(arg.Exception);

            return Task.CompletedTask;
        }

        public async Task RunAndBlockAsync()
        {
            await RunAsync().ConfigureAwait(false);
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private void TerribleElevatedPermissionCheck()
        {
            try
            {
                var rng = new Random().Next(100000, 1000000);
                var str = rng.ToString();
                File.WriteAllText(str, str);
                File.Delete(str);
            }
            catch
            {
                _log.Error("You must run the application as an ADMINISTRATOR.");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(2);
            }
        }

        private static void SetupShard(int parentProcessId)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    var p = Process.GetProcessById(parentProcessId);
                    if (p == null)
                        return;
                    p.WaitForExit();
                }
                finally
                {
                    Environment.Exit(10);
                }
            })).Start();
        }
    }
}
