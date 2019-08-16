using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

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
                Description = $"{_commands.Commands.Count()} total commands",
                Color = Color.Magenta,
                Footer = new EmbedFooterBuilder()
                .WithText("All commands are case insensitive")
            };

            if (module == String.Empty)
                foreach (var mod in _commands.Modules.Where(m => m.Parent == null && m.Name != "ModuleBase" && m.Name != "Help").OrderBy(x => x.Name))
                {
                    AddHelp(mod, ref embed);
                }
            else
            {
                var mod = _commands.Modules.FirstOrDefault(m =>
                    string.Equals(m.Name.Replace("Module", ""), module, StringComparison.CurrentCultureIgnoreCase));
                if (mod == null) await ReplyAsync("No module could be found");

                embed.Title = mod.Name;
                embed.Description = $"{mod.Summary}\n" +
                                    (!string.IsNullOrEmpty(mod.Remarks) ? $"({mod.Remarks}\n)" : string.Empty);
                AddCommands(mod, ref embed);
            }

            await ReplyAsync(String.Empty, embed: embed.Build());
        }

        private void AddHelp(ModuleInfo module, ref EmbedBuilder builder)
        {
            foreach (var sub in module.Submodules) AddHelp(sub, ref builder);
                builder.AddField(f =>
                {
                    f.Name = $"**{module.Name} module**";

                if (module.Submodules.Count < 0)
                    f.Value = $"submodules: {string.Join(", ", module.Submodules.Select(m => m))}" + "\n" + $"commands: {string.Join(", ", module.Commands.Select(x => $"`{x.Name}`"))}";
                else
                {
                    var commands = string.Empty;

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

        private void AddCommand(CommandInfo command, ref EmbedBuilder embedBuilder)
        {
            embedBuilder.AddField(f =>
            {
                f.Name = $"**{command.Name}**";
                f.Value = $"{command.Summary}\n" +
                    (!string.IsNullOrEmpty(command.Remarks) ? $"({command.Remarks})\n" : string.Empty) + 
                    (command.Aliases.Any() ? $"**Aliases:** {string.Join(", ", command.Aliases.Select(x => $"`{x}`"))}\n" : string.Empty);
            });
        }

        private string GetAliases(CommandInfo command)
        {
            StringBuilder output = new StringBuilder();
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
        private string GetPrefix(CommandInfo command)
        {
            //var output = GetPrefix(command.Module);
            var output = $"{command.Aliases.FirstOrDefault()} ";
            return output;
        }

        /*private string GetPrefix(ModuleInfo module)
        {
            string output = "";
            if (module.Parent != null) output = $"{GetPrefix(module.Parent)}{output}";
            if (module.Aliases.Any())
                output += string.Concat(module.Aliases.FirstOrDefault(), "");
            return output;
        }*/
    }
}