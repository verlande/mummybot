using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace mummybot.Extensions
{
    public static class StringExtensions
    {
        public static T MapJson<T>(this string str)
    => JsonConvert.DeserializeObject<T>(str);

        private static readonly HashSet<char> lettersAndDigits = new HashSet<char>(Enumerable.Range(48, 10)
            .Concat(Enumerable.Range(65, 26))
            .Concat(Enumerable.Range(97, 26))
            .Select(x => (char)x));

        public static string StripHTML(this string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        /// <summary>
        /// Easy use of fast, efficient case-insensitive Contains check with StringComparison Member Types 
        /// CurrentCulture, CurrentCultureIgnoreCase, InvariantCulture, InvariantCultureIgnoreCase, Ordinal, OrdinalIgnoreCase
        /// </summary>    

        public static string SanitizeMentions(this string str) =>
            str.Replace("@everyone", "@everyοne", StringComparison.InvariantCultureIgnoreCase)
            .Replace("@here", "@һere", StringComparison.InvariantCultureIgnoreCase);

        public static bool ContainsNoCase(this string str, string contains, StringComparison compare)
        {
            return str.IndexOf(contains, compare) >= 0;
        }

        public static string TrimTo(this string str, int maxLength, bool hideDots = false)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), $"Argument {nameof(maxLength)} can't be negative.");
            if (maxLength == 0)
                return string.Empty;
            if (maxLength <= 3)
                return string.Concat(str.Select(c => '.'));
            if (str.Length < maxLength)
                return str;

            if (hideDots)
            {
                return string.Concat(str.Take(maxLength));
            }
            else
            {
                return string.Concat(str.Take(maxLength - 3)) + "...";
            }
        }

        public static string ToBase64(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string GetInitials(this string txt, string glue = "") =>
            string.Join(glue, txt.Split(' ').Select(x => x.FirstOrDefault()));

        public static bool IsAlphaNumeric(this string txt) =>
            txt.All(c => lettersAndDigits.Contains(c));
    }
}
