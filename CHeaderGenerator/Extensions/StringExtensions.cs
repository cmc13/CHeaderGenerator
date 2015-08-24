using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CHeaderGenerator.Extensions
{
    static class StringExtensions
    {
        public static string CaseInsensitiveReplace(this String str, string oldValue, string newValue)
        {
            string resultString;
            try {
                resultString = Regex.Replace(str, oldValue, newValue, RegexOptions.IgnoreCase);
            } catch(ArgumentException) {
                resultString = Regex.Replace(str, Regex.Escape(oldValue), newValue, RegexOptions.IgnoreCase);
            }
            return resultString;
        }

        public static bool Contains(this String str, String check, StringComparison comp)
        {
            return str.IndexOf(check, comp) >= 0;
        }

        public static bool IsUpper(this String str)
        {
            return !str.Any(ch => Char.IsLetter(ch) && Char.IsLower(ch));
        }

        public static bool IsLower(this String str)
        {
            return !str.Any(ch => Char.IsLetter(ch) && Char.IsUpper(ch));
        }

        public static string ToUpper(this String str)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var ch in str)
            {
                if (Char.IsLetter(ch) && Char.IsLower(ch))
                    builder.Append(Char.ToUpper(ch));
                else
                    builder.Append(ch);
            }
            return builder.ToString();
        }

        public static string ToLower(this String str)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var ch in str)
            {
                if (Char.IsLetter(ch) && Char.IsUpper(ch))
                    builder.Append(Char.ToLower(ch));
                else
                    builder.Append(ch);
            }
            return builder.ToString();
        }

        public static string Trim(this String str, int count, params char[] trimChars)
        {
            string trimmedString;

            int i;
            for (i = 0; i < count && i < str.Length && trimChars.Contains(str[i]); ++i)
            { }

            trimmedString = str.Substring(i);

            for (i = trimmedString.Length; i > 0 && trimmedString.Length - i < count && trimChars.Contains(str[i]); --i)
            { }

            return trimmedString.Substring(0, i);
        }
    }
}
