using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace mummybot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class Logging : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Services.GuildService.GuildMsgLogging[context.Guild.Id]) return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError("Command disabled as this guild has logging disabled\nUse **£Logging** to enable"));
        }
    }
}
