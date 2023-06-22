using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

        // I literally tried A LOT of different ways to do this, and then I found this on StackOverflow lmfaoooooooo
        public static void SaveFile(string stringBuffer, string fname)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(stringBuffer);
            List<byte> bytes = new List<byte>(buffer);
            string[] splitString = stringBuffer.Split('\n');
            int lengthOfFourLines = splitString[0].Length + splitString[1].Length + splitString[2].Length + splitString[3].Length + 4;
            bytes.RemoveRange(0, lengthOfFourLines);
            int lengthOfLastLine = splitString[^2].Length + 2;
            bytes.RemoveRange(bytes.Count - lengthOfLastLine, lengthOfLastLine);
            buffer = bytes.ToArray();
            FileStream file = File.Create(fname);
            file.Write(buffer);
            file.Close();
        }
        //TODO add comment what this is used for
        public static string ReplaceWord(this string text, string word, string bywhat)
        {
            static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '$';
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
        public static string GetString(string[] args, int arg = 0, bool noSpace = true)
        {
            string returnValue;
            try
            {
                //returnValue = Parser.line[currentChar..];
                returnValue = args[arg];
            }
            catch
            {
                returnValue = "";
            }
            foreach (string var in Program.variables.Keys)
            {
                returnValue = returnValue.ReplaceWord("$" + var, (noSpace ? "" : " ") + Program.variables[var]);
            }
            return returnValue;
        }
    }
}
