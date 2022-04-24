using System;
using System.Linq;

namespace PoliticalAlerts
{
    public static class Util
    {
        public static string RemoveWhitespace(this string input)
        {
            char[] chars = input.ToCharArray();
            char[] nowhitespace = new char[chars.Length];
            int count = 0;

            for (int i = 0; i < chars.Length; i++)
            {
                if (!Char.IsWhiteSpace(chars[i]))
                    nowhitespace[count++] = chars[i];
                else if (i > 0 && !Char.IsWhiteSpace(chars[i - 1]))
                    nowhitespace[count++] = ' ';
            }

            return new string(nowhitespace, 0, count);
        }
    }
}