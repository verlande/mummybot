using System;
using System.Text.RegularExpressions;

namespace mummybot.Extensions
{
    public static class StringExtensions
    {
        //Old Regex - /(https?:\/\/)?(www\.)?(discord\.(gg|li|me|io)|discordapp\.com\/invite)\/.+/
        private static readonly Regex discordInviteRegex = new Regex
            (@"(?:discord(?:\.gg|.me|app\.com\/invite)\/(?<id>([\w]{16}|(?:[\w]+-?){3})))");

        public static bool IsDiscordInvite(this string str) => discordInviteRegex.IsMatch(str);

        public static string SanitizeMentions(this string str) =>
            str.Replace("@everyone", "@everyοne", StringComparison.InvariantCultureIgnoreCase)
            .Replace("@here", "@һere", StringComparison.InvariantCultureIgnoreCase);

    }
}
