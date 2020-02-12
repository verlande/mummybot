using System;
using System.Text.RegularExpressions;
// ReSharper disable StringLiteralTypo

namespace mummybot.Extensions
{
    public static class StringExtensions
    {
        //Old Regex - /(https?:\/\/)?(www\.)?(discord\.(gg|li|me|io)|discordapp\.com\/invite)\/.+/
        private static readonly Regex DiscordInviteRegex = new Regex
            (@"(?:discord(?:\.gg|.me|app\.com\/invite)\/(?<id>([\w]{16}|(?:[\w]+-?){3})))");

        private static readonly Regex DiscordAttachmentRegex = new Regex
            (@"(https?:\/\/)?(cdn.discordapp\.com\/attachments)\/.+");

        public static bool IsDiscordInvite(this string str) => DiscordInviteRegex.IsMatch(str);

        public static bool IsDiscordAttachment(this string str) => DiscordAttachmentRegex.IsMatch(str);

        public static string SanitizeMentions(this string str) =>
            str.Replace("@everyone", "@everyοne", StringComparison.InvariantCultureIgnoreCase)
            .Replace("@here", "@һere", StringComparison.InvariantCultureIgnoreCase);

    }
}
