using System.Collections.Generic;
using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using mummybot.Extensions;
using System;

namespace mummybot.Modules.General
{
    public partial class General
    {
        private enum Regional
        {
            A, B, C, D, E, F
        }

        private static string EnumToEmoji(Regional regional) =>
            regional switch
            {
                Regional.A => ":regional_indicator_a:",
                Regional.B => ":regional_indicator_b:",
                Regional.C => ":regional_indicator_c:",
                Regional.D => ":regional_indicator_d:",
                Regional.E => ":regional_indicator_e:",
                Regional.F => ":regional_indicator_f:",
                _ => null
            };

        private static Emoji EnumToUnicode(Regional regional) =>
            regional switch
            {
                Regional.A => new Emoji("\U0001F1E6"),
                Regional.B => new Emoji("\U0001F1E7"),
                Regional.C => new Emoji("\U0001F1E8"),
                Regional.D => new Emoji("\U0001F1E9"),
                Regional.E => new Emoji("\U0001F1EA"),
                Regional.F => new Emoji("\U0001F1EB"),
                _ => null
            };

        [Command("Poll"), Summary("<question|answer|answer>")]
        public async Task Poll([Remainder] string arg)
        {
            var answers = arg.Split("|").Skip(1).ToArray();
            if (answers.Length > 6) { await Context.Channel.SendErrorAsync(string.Empty, "Max 6 answers").ConfigureAwait(false); return; }
            var question = arg.Split("|")[0];

            var regional = new List<Regional>();
            
            for (var i = 0; i <= answers.Length - 1; i++)
                regional.Add((Regional)i);
            
            var answer = new StringBuilder();
            var num = 0;
            foreach (var s in regional)
            {
                answer.AppendLine($"{EnumToEmoji(s)} - {answers[num]}");
                num++;
            }

            var msg = await Context.Channel.SendConfirmAsync(text: answer.ToString(), title: question, footer: $"{DateTime.Now.ToLocalTime()}").ConfigureAwait(false);

            foreach (var s in regional)
            {
                await msg.AddReactionAsync(EnumToUnicode(s)).ConfigureAwait(false);
                await Task.Delay(250).ConfigureAwait(false);
            }
            await Context.Message.DeleteAsync().ConfigureAwait(false);
        }
    }
}

