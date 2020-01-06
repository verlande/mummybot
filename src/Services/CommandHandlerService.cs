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
using mummybot.Extensions;
using NLog;

namespace mummybot.Services
{
    public class CommandHandlerService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;
        public readonly ConfigService _config;
        private readonly Logger _log;

        public string DefaultPrefix { get; private set; }

        public event Func<IUserMessage, CommandInfo, Task> CommandExecuted = delegate { return Task.CompletedTask; };
        public event Func<CommandInfo, ITextChannel, string, Task> CommandErrored = delegate { return Task.CompletedTask; };
        public event Func<IUserMessage, Task> OnMessageNoTrigger = delegate { return Task.CompletedTask; };

        //userid/msg count
        public ConcurrentDictionary<ulong, uint> UserMessagesSent { get; } = new ConcurrentDictionary<ulong, uint>();

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, ConfigService config, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _config = config;

            DefaultPrefix = _config.Config["prefix"];
            _log = LogManager.GetCurrentClassLogger();

            _discord.MessageReceived += MessageReceivedHandler;
        }

        private Task LogSuccessfulExecution(IUserMessage usrMsg, ITextChannel channel)
        {
            bool normal = true;
            if (normal)
            {
                _log.Info($"" +
                        "User: {0}\n\t" +
                        "Server: {1}\n\t" +
                        "Channel: {2}\n\t" +
                        "Message: {3}",
                        usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                        (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                        (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                        usrMsg.Content // {3}
                        );
            }
            else
            {
                _log.Info("Succ | g:{0} | c: {1} | u: {2} | msg: {3}",
                    channel?.Guild.Id.ToString() ?? "-",
                    channel?.Id.ToString() ?? "-",
                    usrMsg.Author.Id,
                    usrMsg.Content);
            }
            return Task.CompletedTask;
        }

        private async Task LogErroredExecution(string erroredCmd, string errorMessage, IUserMessage usrMsg, ITextChannel channel)
        {
            await channel.SendErrorAsync($"executing {erroredCmd}", errorMessage);
            var normal = true;
            if (normal)
            {
                _log.Error($"" +
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
                            );
            }
            else
            {
                _log.Error("Err | g:{0} | c: {1} | u: {2} | msg: {3}\n\tErr: {4}",
                    channel?.Guild.Id.ToString() ?? "-",
                    channel?.Id.ToString() ?? "-",
                    usrMsg.Author.Id,
                    usrMsg.Content,
                    errorMessage);
            }
        }

        private async Task MessageReceivedHandler(SocketMessage msg)
        {
            try
            {
                if (msg.Source != MessageSource.User || !(msg is SocketUserMessage usrMsg)) return;

                UserMessagesSent.AddOrUpdate(usrMsg.Author.Id, 1, (key, old) => old += 1);

                var guild = (msg.Channel as SocketTextChannel)?.Guild;

                await TryRunCommand(guild, msg.Channel, usrMsg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Warn(ex ?? ex.InnerException);
            }
        }

        private async Task TryRunCommand(SocketGuild guild, ISocketMessageChannel channel, SocketUserMessage usrMsg)
        {
            var execTime = Environment.TickCount;

            var messageContent = usrMsg.Content;
            var isPrefixCommand = messageContent.StartsWith(DefaultPrefix, StringComparison.InvariantCultureIgnoreCase);
            var exec2 = Environment.TickCount - execTime;

            if (messageContent.StartsWith(DefaultPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var (Success, Error, Info) = await ExecuteCommandAsync(new SocketCommandContext(_discord, usrMsg), messageContent, isPrefixCommand ? 1 : DefaultPrefix.Length, _provider, MultiMatchHandling.Best).ConfigureAwait(false);

                if (Success)
                {
                    await LogSuccessfulExecution(usrMsg, channel as ITextChannel).ConfigureAwait(false);
                    return;
                }
                else if (Error != null)
                {
                    await LogErroredExecution(Info.Name, Error, usrMsg, channel as ITextChannel);
                    if (guild != null)
                        await CommandErrored(Info, channel as ITextChannel, Error).ConfigureAwait(false);
                }
            }
            else
            {
                await OnMessageNoTrigger(usrMsg).ConfigureAwait(false);
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
                    Console.WriteLine(execResult.Exception);
                }
            }

            return (true, null, cmd);
        }
        private readonly object errorLogLock = new object();
    }
}
