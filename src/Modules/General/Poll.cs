using System;
using System.Collections.Generic;
using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using System.Linq;
using mummybot.Extensions;

namespace mummybot.Modules.General
{
    public partial class General : ModuleBase
    {
        internal enum Regional
        {
            A, B, C, D, E, F
        }

        internal string EnumToEmoji(Regional regional)
        {
            switch (regional)
            {
                case Regional.A:
                    return ":regional_indicator_a:";
                case Regional.B:
                    return ":regional_indicator_b:";
                case Regional.C:
                    return ":regional_indicator_c:";
                case Regional.D:
                    return ":regional_indicator_d:";
                case Regional.E:
                    return ":regional_indicator_e:";
                case Regional.F:
                    return ":regional_indicator_f:";
                default:
                    return null;
            }
        }

        internal Emoji EnumToUnicode(Regional regional)
        {
            switch (regional)
            {
                case Regional.A:
                    return new Emoji("\U0001F1E6");
                case Regional.B:
                    return new Emoji("\U0001F1E7");
                case Regional.C:
                    return new Emoji("\U0001F1E8");
                case Regional.D:
                    return new Emoji("\U0001F1E9");
                case Regional.E:
                    return new Emoji("\U0001F1EA");
                case Regional.F:
                    return new Emoji("\U0001F1EB");
                default:
                    return null;
            }
        }

        [Command("Poll")]
        public async Task Poll([Remainder] string arg)
        {
            var answers = arg.Split("|").Skip(1).ToArray();
            if (answers.Length > 6) { await Context.Channel.SendErrorAsync(string.Empty, "Max 6 answers"); return; }
            var question = arg.Split("|")[0];

            var regionals = new List<Regional>();
            var r = new Random();

            for (int i = 0; i <= answers.Length - 1; i++)
            {
                regionals.Add((Regional)i);
            }
            Console.WriteLine(answers.Length);
            var tr = string.Empty;
            int num = 0;
            foreach (var s in regionals)
            {
                tr = tr + EnumToEmoji(s) + $" - {answers[num]}\n";
                num += 1;
            }

            var msg = await Context.Channel.SendConfirmAsync(tr, question);

            foreach (var s in regionals)
            {
                await msg.AddReactionAsync(EnumToUnicode(s));
                await Task.Delay(250);
            }

        }
    }
}

