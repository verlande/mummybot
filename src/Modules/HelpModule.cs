using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using mummybot.Services;

namespace mummybot.Modules
{
    [Group("Help")]
    public class HelpModule : ModuleBase
    {
        private readonly CommandService _commands;
        private readonly IServiceProvider _map;

        public HelpModule(CommandService commands, IServiceProvider map)
        {
            _commands = commands;
            _map = map;
        }

        [Command, Summary("List this bots commands")]
        public async Task Help([Summary("<module>")] string module = "")
        {
            var embed = new EmbedBuilder
            {
                Title = "mummybot Help",
                Description = $"{CommandHandlerService.DefaultPrefix}help <module>\n{_commands.Commands.Count(x => x.Module.Name != "ModuleBase" && x.Module.Name != "Owner" && x.Module.Name != "Help")} total commands",
                Color = Color.Magenta,
                Footer = new EmbedFooterBuilder()
                .WithText("All commands are case insensitive")
            };

            if (module == string.Empty)
                foreach (var mod in _commands.Modules.Where(m => m.Parent == null && m.Name != "ModuleBase" && m.Name != "Help" && m.Name != "Owner").OrderBy(x => x.Name))
                {
                    AddHelp(mod, ref embed);
                }
            else
            {
                if ("Owner".Equals(module, StringComparison.CurrentCultureIgnoreCase))
                {
                    var application = await _client.GetApplicationInfoAsync().ConfigureAwait(false);
                    if (!application.Owner.Id.Equals(Context.User.Id))
                        return;
                }
                var mod = _commands.Modules.FirstOrDefault(m =>
                    string.Equals(m.Name.Replace("Module", ""), module, StringComparison.CurrentCultureIgnoreCase));
                if (mod == null) await ReplyAsync("No module could be found").ConfigureAwait(false);

                if (mod != null)
                {
                    embed.Title = mod.Name;
                    embed.Description = $"{mod.Summary}\n" +
                                        (!string.IsNullOrEmpty(mod.Remarks) ? $"({mod.Remarks}\n)" : string.Empty);
                    AddCommands(mod, ref embed);
                }
            }

            await ReplyAsync(string.Empty, embed: embed.Build()).ConfigureAwait(false); }

        private static void AddHelp(ModuleInfo module, ref EmbedBuilder builder)
        {
            foreach (var sub in module.Submodules) AddHelp(sub, ref builder);
                builder.AddField(f =>
                {
                    f.Name = $"**{module.Name} module** ({module.Commands.Count})";

                if (module.Submodules.Count < 0)
                    f.Value = $"submodules: {string.Join(", ", module.Submodules.Select(m => m))}" + "\n" + $"commands: {string.Join(", ", module.Commands.Select(x => $"`{x.Name}`"))}";
                else
                {
                    string commands;

                    if (module.Aliases.Count > 1) commands = string.Join("\t", module.Commands.Select(x => $"``{module.Aliases[1]} {x.Name}``"));
                    else commands = "\n" + $"{string.Join("\t", module.Commands.Select(x => $"``{x.Name}``"))}";

                    f.Value = commands;
                }
            });
        }

        private void AddCommands(ModuleInfo module, ref EmbedBuilder embedBuilder)
        {
            foreach (var command in module.Commands)
            {
                command.CheckPreconditionsAsync(Context, _map).GetAwaiter().GetResult();
                AddCommand(command, ref embedBuilder);
            }
        }

        private static void AddCommand(CommandInfo command, ref EmbedBuilder embedBuilder) => 
            embedBuilder.AddField(f =>
            {
                f.Name = $"**{command.Name}**";
                f.Value = $"{command.Summary}\n" +
                    (!string.IsNullOrEmpty(command.Remarks) ? $"({command.Remarks})\n" : string.Empty) +
                    (command.Aliases.Any() ? $"**Aliases:** {string.Join(", ", command.Aliases.Select(x => $"`{x}`"))}\n" : string.Empty);
            });

        private static string GetAliases(CommandInfo command)
        {
            var output = new StringBuilder();
            if (!command.Parameters.Any()) return output.ToString();
            foreach (var param in command.Parameters)
            {
                if (param.IsOptional)
                    output.Append($"[{param.Name} = {param.DefaultValue}] ");
                else if (param.IsMultiple)
                    output.Append($"|{param.Name}| ");
                else if (param.IsRemainder)
                    output.Append($"...{param.Name} ");
                else
                    output.Append($"<{param.Name}> ");
            }
            return output.ToString();
        }
    }
}