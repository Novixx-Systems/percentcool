using System;
using System.Text;
using System.Text.RegularExpressions;

namespace percentCool.Utilities
{
    internal static class Utils
    {
        public static int currentChar;
        public static int defaultReturnValue = 0;
        public static void Init()
        {
            currentChar = 0;
        }
        public static string ReplaceWord(this string text, string word, string bywhat)
        {
            static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
            StringBuilder sb = null;
            int p = 0, j = 0;
            while (j < text.Length && (j = text.IndexOf(word, j, StringComparison.Ordinal)) >= 0)
                if ((j == 0 || !IsWordChar(text[j - 1])) &&
                    (j + word.Length == text.Length || !IsWordChar(text[j + word.Length])))
                {
                    sb ??= new StringBuilder();
                    sb.Append(text, p, j - p);
                    sb.Append(bywhat);
                    j += word.Length;
                    p = j;
                }
                else j++;
            if (sb == null) return text;
            sb.Append(text, p, text.Length - p);
            return sb.ToString();
        }
        public static string GetString()
        {
            string returnValue;
            try
            {
                returnValue = Parser.line[currentChar..];
            }
            catch
            {
                returnValue = "";
            }
            foreach (string var in Program.variables.Keys)
            {
                returnValue = returnValue.ReplaceWord("$" + var, Program.variables[var]);
            }
            return returnValue;
        }
    }
}
