using Discord;
using Discord.Commands;
using Discord.WebSocket;
using mummybot.Extensions;
using mummybot.Services;
using NLog;
using System.Threading.Tasks;
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming

namespace mummybot.Modules
{
    [RequireContext(ContextType.Guild)]
    public abstract class ModuleBase : Discord.Commands.ModuleBase<SocketCommandContext>
    {
        private string ModuleTypeName { get; }
        private string LowerModuleTypeName { get; }
        public CommandHandlerService CmdHandler { get; set; }
        // ReSharper disable once MemberCanBeProtected.Global
        public mummybotDbContext Database { get; set; }
        public DiscordSocketClient _client { get; set; }
        protected readonly Logger _log;


        protected ModuleBase(bool isTopLevelModule = true)
        {
            ModuleTypeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            LowerModuleTypeName = ModuleTypeName.ToLowerInvariant();
            _log = LogManager.GetCurrentClassLogger();
        }

        protected async Task<bool> PromptUserConfirmAsync(EmbedBuilder embed)
        {
            embed.WithColor(Utils.GetRandomColor())
                .WithFooter("Type yes/no");

            var msg = await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            try
            {
                var input = await GetUserInputAsync(Context.User.Id, Context.Channel.Id).ConfigureAwait(false);
                input = input?.ToUpperInvariant();

                return input == "YES" || input == "Y";
            }
            finally
            {
                var _ = Task.Run(() => msg.DeleteAsync());
            }
        }

        private async Task<string> GetUserInputAsync(ulong userId, ulong channelId)
        {
            var userInputTask = new TaskCompletionSource<string>();
            var dsc = Context.Client;
            try
            {
                dsc.MessageReceived += MessageReceived;

                if ((await Task.WhenAny(userInputTask.Task, Task.Delay(10000)).ConfigureAwait(false)) != userInputTask.Task)
                {
                    return null;
                }

                return await userInputTask.Task.ConfigureAwait(false);
            }
            finally
            {
                dsc.MessageReceived -= MessageReceived;
            }

            Task MessageReceived(SocketMessage arg)
            {
                var _ = Task.Run(() =>
                {
                    if (!(arg is SocketUserMessage userMsg) ||
                        !(userMsg.Channel is ITextChannel) ||
                        userMsg.Author.Id != userId ||
                        userMsg.Channel.Id != channelId)
                    {
                        return Task.CompletedTask;
                    }

                    if (userInputTask.TrySetResult(arg.Content))
                    {
                        userMsg.DeleteAfter(1);
                    }
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }
    }

    public abstract class ModuleBase<TService> : ModuleBase where TService : INService
    {
        public TService _service { get; set; }
        protected ModuleBase(bool isTopLevel = true) : base(isTopLevel) { }
    }

    public abstract class mummybotSubmodule : ModuleBase
    {
        protected mummybotSubmodule() : base(false) { }
    }

    public abstract class mummybotSubmodule<TService> : ModuleBase<TService> where TService : INService
    {
        protected mummybotSubmodule() : base(false) { }
    }
}

