using System;
namespace mummybot.Extensions
{
    public static class StringExtensions
    {
        public static string SanitizeMentions(this string str) =>
            str.Replace("@everyone", "@everyοne", StringComparison.InvariantCultureIgnoreCase)
            .Replace("@here", "@һere", StringComparison.InvariantCultureIgnoreCase);

    }
}
