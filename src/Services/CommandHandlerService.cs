using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Discord.Net;
using System.IO;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using mummybot.Common;
using mummybot.Extensions;
using NLog;

namespace mummybot.Services
{
    public class CommandHandlerService : INService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly mummybotDbContext _context;
        private IServiceProvider _services;
        protected readonly Logger _log = LogManager.GetLogger("logfile");
        protected readonly Logger _blog = LogManager.GetLogger("blockfile");
        private IEnumerable<IEarlyBehavior> _earlyBehaviors;
        private IEnumerable<ILateBlocker> _lateBlockers;
        private IEnumerable<IInputTransformer> _inputTransformers;
        private IEnumerable<ILateExecutor> _lateExecutors;

        public static string DefaultPrefix { get; set; }

        public event Func<IUserMessage, CommandInfo, Task> CommandExecuted = delegate { return Task.CompletedTask; };
        public event Func<CommandInfo, ITextChannel, string, Task> CommandErrored = delegate { return Task.CompletedTask; };
        public event Func<IUserMessage, Task> OnMessageNoTrigger = delegate { return Task.CompletedTask; };

        //userid/msg count
        private ConcurrentDictionary<ulong, uint> UserMessagesSent { get; } = new ConcurrentDictionary<ulong, uint>();
        public ConcurrentHashSet<ulong> UsersOnShortCooldown { get; } = new ConcurrentHashSet<ulong>();
        private readonly Timer _clearUsersOnShortCooldown;
        public const int GlobalCommandsCooldown = 750;
        private uint processedCommands = 0;

        public uint ProcessedCommands
        {
            get => processedCommands;
            set => processedCommands = value;
        }

        public CommandHandlerService() { }

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, mummybotDbContext context, ConfigService config, IServiceProvider services)
        {
            _discord = discord;
            _commands = commands;
            _context = context;
            _services = services;

#if DEBUG
            DefaultPrefix = config.Config["prefix"];
#else
            DefaultPrefix = Environment.GetEnvironmentVariable("PREFIX");
#endif
            _clearUsersOnShortCooldown = new Timer(_ =>
            {
                UsersOnShortCooldown.Clear();
            }, null, GlobalCommandsCooldown, GlobalCommandsCooldown);


            _discord.MessageReceived += MessageReceivedHandler;
        }

        public void AddServices(IServiceCollection services)
        {
            _earlyBehaviors = services.Where(x =>
                    x.ImplementationType?.GetInterfaces().Contains(typeof(IEarlyBehavior)) ?? false)
                .Select(x => _services.GetService(x.ImplementationType) as IEarlyBehavior)
                .ToArray();
            
            _lateBlockers = services.Where(x =>
                    x.ImplementationType?.GetInterfaces().Contains(typeof(ILateBlocker)) ?? false)
                .Select(x => _services.GetService(x.ImplementationType) as ILateBlocker);
                        
            _inputTransformers = services.Where(x => x.ImplementationType?.GetInterfaces().Contains(typeof(IInputTransformer)) ?? false)
                .Select(x => _services.GetService(x.ImplementationType) as IInputTransformer)
                .ToArray();
            
            _lateExecutors = services.Where(x => x.ImplementationType?.GetInterfaces().Contains(typeof(ILateExecutor)) ?? false)
                .Select(x => _services.GetService(x.ImplementationType) as ILateExecutor)
                .ToArray();
        }

        public Task StartHandling()
        {
            _discord.MessageReceived += (msg) => { var _ = Task.Run(() => MessageReceivedHandler(msg)); return Task.CompletedTask; };
            return Task.CompletedTask;
        }

        private Task LogSuccessfulExecution(IMessage usrMsg, IGuildChannel channel, params int[] execPoints)
        {
            /*_log.Info($"" +
                "User: {0}\n\t" +
                "Server: {1}\n\t" +
                "Channel: {2}\n\t" +
                "Message: {3}",
                usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                usrMsg.Content // {3}
                );*/
            
            
            
            //_log.Info("Executed | g:{0} | c: {1} | u: {2} | msg: {3}",
            //    channel?.Guild.Id.ToString() ?? "-",
            //    channel?.Id.ToString() ?? "-",
            //    usrMsg.Author.Id,
            //    usrMsg.Content);
            
            ProcessedCommands += 1;
            return Task.CompletedTask;
        }

        private async Task LogErroredExecution(string errorMessage, string erroredCmd, IMessage usrMsg, ITextChannel channel, params int[] execPoints)
        {
            await channel.SendErrorAsync($"executing {erroredCmd}", errorMessage);
            /*_log.Error($"" +
                            "User: {0}\n\t" +
                            "Server: {1}\n\t" +
                            "Channel: {2}\n\t" +
                            "Message: {3}\n\t" +
                            "Error: {4}",
                            usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                            (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                            (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                            usrMsg.Content,// {3}
                            errorMessage
                            //exec.Result.ErrorReason // {4}
                            );*/
            //_log.Error("Err | g:{0} | c: {1} | u: {2} | msg: {3}\n\tErr: {4}",
            //    channel?.Guild.Id.ToString() ?? "-",
            //    channel?.Id.ToString() ?? "-",
            //    usrMsg.Author.Id,
            //    usrMsg.Content,
            //    errorMessage);
        }

        private async Task MessageReceivedHandler(SocketMessage msg)
        {
            try
            {
                if (msg.Author.IsBot || !(msg is SocketUserMessage usrMsg)) return;

                UserMessagesSent.AddOrUpdate(usrMsg.Author.Id, 1, (key, old) => ++old);

                var channel = msg.Channel as ISocketMessageChannel;
                var guild = (msg.Channel as SocketTextChannel)?.Guild;

                await TryRunCommand(guild, msg.Channel, usrMsg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                if (ex.InnerException != null)
                    _log.Error(ex.InnerException);
            }
        }
        
        private async Task TryRunCommand(SocketGuild guild, ISocketMessageChannel channel, SocketUserMessage usrMsg)
        {
            var execTime = Environment.TickCount;

            foreach (var beh in _earlyBehaviors)
            {
                if (await beh.RunBehavior(_discord, guild, usrMsg).ConfigureAwait(false))
                {
                    if (beh.BehaviorType == ModuleBehaviorType.Blocker)
                    {
                        _blog.Info("Blocked User: [{0}] Message: [{1}] Service: [{2}]", usrMsg.Author,
                            usrMsg.Content, beh.GetType().Name);
                        return;
                    }
                    else if (beh.BehaviorType == ModuleBehaviorType.Executor)
                    {
                        _blog.Info("User [{0}] executed [{1}] in [{2}]", usrMsg.Author, usrMsg.Content,
                            beh.GetType().Name);
                    }

                    return;
                }
            }

            var exec2 = Environment.TickCount - execTime;
            var messageContent = usrMsg.Content;

            if (_inputTransformers != null)
                foreach (var exec in _inputTransformers)
                {
                    string newContent;
                    if ((newContent = await exec.TransformInput(guild, usrMsg.Channel, usrMsg.Author, messageContent)
                            .ConfigureAwait(false)) != messageContent.ToLowerInvariant())
                    {
                        messageContent = newContent;
                        break;
                    }
                }

            var argPos = 0;

            var isPrefixCommand = usrMsg.HasStringPrefix(DefaultPrefix, ref argPos);
	        var isMentionCommand = usrMsg.HasMentionPrefix(_discord.CurrentUser, ref argPos);
	    
            if (isPrefixCommand ||isMentionCommand)
	    {
                var (Success, Error, Info) = await ExecuteCommandAsync(new SocketCommandContext(_discord, usrMsg), messageContent, isMentionCommand ? argPos : DefaultPrefix.Length, _services, MultiMatchHandling.Best).ConfigureAwait(false);
                execTime = Environment.TickCount - execTime;

                if (Success)
                {
                    await LogSuccessfulExecution(usrMsg, channel as ITextChannel, exec2, execTime).ConfigureAwait(false);
                    await CommandExecuted(usrMsg, Info).ConfigureAwait(false);
                    return;
                }
                else if (Error != null)
                {
                    await LogErroredExecution(Error, Info.Name, usrMsg, channel as ITextChannel, exec2, execTime).ConfigureAwait(false);
                    if (guild != null)
                        await CommandErrored(Info, channel as ITextChannel, Error).ConfigureAwait(false);
                }
            }
            else
            {
                await OnMessageNoTrigger(usrMsg).ConfigureAwait(false);
            }

            foreach (var exec in _lateExecutors)
            {
                await exec.LateExecute(_discord, guild, usrMsg).ConfigureAwait(false);
            }
        }

        public Task<(bool Success, string Error, CommandInfo Info)> ExecuteCommandAsync(SocketCommandContext context, string input, int argPos, IServiceProvider serviceProvider, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
            => ExecuteCommand(context, input.Substring(argPos), serviceProvider, multiMatchHandling);

        public async Task<(bool Success, string Error, CommandInfo Info)> ExecuteCommand(SocketCommandContext context, string input, IServiceProvider services, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
        {
            var searchResult = _commands.Search(context, input);
            if (!searchResult.IsSuccess)
                return (false, null, null);

            var commands = searchResult.Commands;
            var preconditionResults = new Dictionary<CommandMatch, PreconditionResult>();

            foreach (var match in commands)
            {
                preconditionResults[match] = await match.Command.CheckPreconditionsAsync(context, services).ConfigureAwait(false);
            }

            var successfulPreconditions = preconditionResults
                .Where(x => x.Value.IsSuccess)
                .ToArray();

            if (successfulPreconditions.Length == 0)
            {
                //All preconditions failed, return the one from the highest priority command
                var bestCandidate = preconditionResults
                    .OrderByDescending(x => x.Key.Command.Priority)
                    .FirstOrDefault(x => !x.Value.IsSuccess);
                return (false, bestCandidate.Value.ErrorReason, commands[0].Command);
            }

            var parseResultsDict = new Dictionary<CommandMatch, ParseResult>();
            foreach (var pair in successfulPreconditions)
            {
                var parseResult = await pair.Key.ParseAsync(context, searchResult, pair.Value, services).ConfigureAwait(false);

                if (parseResult.Error == CommandError.MultipleMatches)
                {
                    IReadOnlyList<TypeReaderValue> argList, paramList;
                    switch (multiMatchHandling)
                    {
                        case MultiMatchHandling.Best:
                            argList = parseResult.ArgValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
                            paramList = parseResult.ParamValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
                            parseResult = ParseResult.FromSuccess(argList, paramList);
                            break;
                    }
                }

                parseResultsDict[pair.Key] = parseResult;
            }
            // Calculates the 'score' of a command given a parse result
            float CalculateScore(CommandMatch match, ParseResult parseResult)
            {
                float argValuesScore = 0, paramValuesScore = 0;

                if (match.Command.Parameters.Count > 0)
                {
                    var argValuesSum = parseResult.ArgValues?.Sum(x => x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;
                    var paramValuesSum = parseResult.ParamValues?.Sum(x => x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;

                    argValuesScore = argValuesSum / match.Command.Parameters.Count;
                    paramValuesScore = paramValuesSum / match.Command.Parameters.Count;
                }

                var totalArgsScore = (argValuesScore + paramValuesScore) / 2;
                return match.Command.Priority + totalArgsScore * 0.99f;
            }

            //Order the parse results by their score so that we choose the most likely result to execute
            var parseResults = parseResultsDict
                .OrderByDescending(x => CalculateScore(x.Key, x.Value));

            var successfulParses = parseResults
                .Where(x => x.Value.IsSuccess)
                .ToArray();

            if (successfulParses.Length == 0)
            {
                //All parses failed, return the one from the highest priority command, using score as a tie breaker
                var bestMatch = parseResults
                    .FirstOrDefault(x => !x.Value.IsSuccess);
                return (false, bestMatch.Value.ErrorReason, commands[0].Command);
            }

            var cmd = successfulParses[0].Key.Command;

            // Bot will ignore commands which are ran more often than what specified by
            // GlobalCommandsCooldown constant (miliseconds)
            if (!UsersOnShortCooldown.Add(context.Message.Author.Id))
                return (false, null, cmd);
            //return SearchResult.FromError(CommandError.Exception, "You are on a global cooldown.");

            var commandName = cmd.Aliases.First();
            foreach (var exec in _lateBlockers)
            {
                if (await exec.TryBlockLate(_discord, context.Message, context.Guild, context.Channel, context.User, cmd.Module.Name, commandName).ConfigureAwait(false))
                {
                    _log.Info("Late blocking User [{0}] Command: [{1}] in [{2}]", context.User, commandName, exec.GetType().Name);
                    return (false, null, cmd);
                }
            }

            //If we get this far, at least one parse was successful. Execute the most likely overload.
            var chosenOverload = successfulParses[0];
            var execResult = (ExecuteResult)await chosenOverload.Key.ExecuteAsync(context, chosenOverload.Value, services).ConfigureAwait(false);

            if (execResult.Exception != null && (!(execResult.Exception is HttpException he) || he.DiscordCode != 50013))
            {
                lock (errorLogLock)
                {
                    var now = DateTime.Now;
                    File.AppendAllText($"./command_errors_{now:yyyy-MM-dd}.txt",
                        $"[{now:HH:mm-yyyy-MM-dd}]" + Environment.NewLine
                        + execResult.Exception.ToString() + Environment.NewLine
                        + "------" + Environment.NewLine);
                    _log.Warn(execResult.Exception);
                }
            }

            return (true, null, cmd);
        }

        private readonly object errorLogLock = new object();
    }
}
