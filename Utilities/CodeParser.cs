using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace percentCool.Utilities
{
    internal class CodeParser
    {
        static int c = 0;
        static string cStr = "";
        static char currentChar => cStr[c];
        static char previousChar => (c-1 >= 0 ? cStr[c] : '\0');


        public static string[] ParseLineIntoTokens(string inp)
        {
            c = 0;
            cStr = inp;

            List<string> tokens = new List<string>();

            bool isReadingString = false;
            string temp = "";

            while (c < cStr.Length)
            {
                if(previousChar != '\\' && currentChar == '"')
                {
                    if(isReadingString) {
                        tokens.Add(temp);
                        temp = "";
                        isReadingString = false;
                    }else
                    {
                        if(temp != string.Empty) tokens.Add(temp);
                        temp = "";
                        isReadingString = true;
                    }
                }else
                {
                    if (!isReadingString && currentChar == ' ')
                    {
                        tokens.Add(temp);
                        temp = "";
                    }
                    else
                    {
                        temp += currentChar;
                    }
                }

                c++;
            }

            if(isReadingString)
            {
                Program.Error("Unterminated string!");
                return new string[0];
            }

            if(temp != string.Empty) tokens.Add(temp);

            return tokens.ToArray();
        }
    }
}
