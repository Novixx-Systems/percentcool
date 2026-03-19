using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web;

namespace percentCool.Utilities
{
    internal class Parser
    {
        private static int error = 0;
        private static string line;
        private static HttpListenerContext ctx;
        
        private readonly static Dictionary<string, Action<Request>> keywords = new();

        public static void Init()
        {
            keywords.Clear();

            // All keywords
            //
            // NOTE: Most keywords MUST end with a space, operators
            // must NOT end with a space. Keywords that have no arguments
            // should also NOT end with a space.
            
            #region Operators
            keywords.Add("$=",           Op_DollarEquals);
            keywords.Add("$",            Op_Dollar);
            #endregion

            #region Utility Keywords
            keywords.Add("echo ", Kw_Echo);
            keywords.Add("rndmax ", Kw_Rndmax);
            keywords.Add("random", Kw_Random);
            keywords.Add("existing ", Kw_Existing);
            keywords.Add("escape ", Kw_Escape);
            keywords.Add("sanitize ", Kw_Sanitize);
            keywords.Add("replace ", Kw_Replace);
            keywords.Add("arraytovars ", Kw_Arraytovars);
            #endregion

            #region Session Keywords
            keywords.Add("sessionset ", Kw_Sessionset);
            keywords.Add("sessionget ", Kw_Sessionget);
            keywords.Add("newsession", Kw_Newsession);
            #endregion

            #region Control Flow Keywords
            keywords.Add("if ", Kw_If);
            keywords.Add("foreach ", Kw_Foreach);
            #endregion

            #region Date/Time Keywords
            keywords.Add("getdate ", Kw_Getdate);
            #endregion

            #region SQL Keywords
            keywords.Add("sqlquery ", Kw_Sqlquery);
            keywords.Add("sqlconnect ", Kw_Sqlconnect);
            #endregion

            #region Crypto Keywords
            keywords.Add("hash ", Kw_Hash);
            keywords.Add("bcrypthash ", Kw_Hash);
            keywords.Add("hashcompare ", Kw_CompareHash);
            keywords.Add("numleadingzerobits ", Kw_NumZeroBits);
            keywords.Add("md5hash ", Kw_Md5Hash);
            keywords.Add("sha1hash ", Kw_Sha1Hash);
            keywords.Add("sha256hash ", Kw_Sha256Hash);
            #endregion

            #region Mail Keywords
            keywords.Add("mail ", Kw_Mail);
            #endregion

            #region File System Keywords
            keywords.Add("writefile ", Kw_Writefile);
            keywords.Add("readfile ", Kw_Readfile);
            keywords.Add("rmfile ", Kw_Rmfile);
            keywords.Add("deletefile ", Kw_Rmfile); // Alias for rmfile
            keywords.Add("appendfile ", Kw_Appendfile);
            #endregion

            #region Web Keywords
            keywords.Add("urldecode ", Kw_Urldecode);
            keywords.Add("urlencode ", Kw_Urlencode);
            #endregion
        }

        // Urldecode and urlencode take a variable name and a value
        // and encode/decode the value and store it in the variable
        public static void Kw_Urlencode(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = args[1];
            string value = Utils.GetString(args, requ, 2);

            if (varname == string.Empty || value == string.Empty || !varname.StartsWith("$"))
            {
                return;
            }

            varname = varname[1..];

            if (requ.variables.ContainsKey(varname))
            {
                requ.variables[varname] = WebUtility.UrlEncode(value) ?? string.Empty;
            }
            else
            {
                requ.variables.Add(varname, WebUtility.UrlEncode(value) ?? string.Empty);
            }
        }

        public static void Kw_Md5Hash(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string target = Utils.GetString(args, requ, 1, true);
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(target);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                requ.variables.Add("_HASH", BitConverter.ToString(hashBytes).Replace("-", "").ToLower());
            }
        }

        public static void Kw_Sha1Hash(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string target = Utils.GetString(args, requ, 1, true);
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(target);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);
                requ.variables.Add("_HASH", BitConverter.ToString(hashBytes).Replace("-", "").ToLower());
            }
        }

        public static void Kw_Sha256Hash(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string target = Utils.GetString(args, requ, 1, true);
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(target);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                requ.variables.Add("_HASH", BitConverter.ToString(hashBytes).Replace("-", "").ToLower());
            }
        }

        public static void Kw_NumZeroBits(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = args[1];
            string value = Utils.GetString(args, requ, 2);
            if (varname == string.Empty || value == string.Empty || !varname.StartsWith("$"))
            {
                return;
            }
            // Count the number of zero bits in the value (first convert from hex to binary)
            // check if value is a valid hex string
            bool isHex = value.All(c => Uri.IsHexDigit(c));
            if (!isHex || value.Length % 2 != 0)
            {
                requ.variables["_NUMZEROBITS"] = "0";
                return;
            }
            string binary = string.Join(string.Empty, value.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
            int count = binary.TakeWhile(c => c == '0').Count();
            requ.variables["_NUMZEROBITS"] = count.ToString();
        }

        public static void Kw_Sanitize(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = args[1];
            string value = Utils.GetString(args, requ, 2);


            if (varname == string.Empty || value == string.Empty || !varname.StartsWith("$"))
            {
                return;
            }

            varname = varname[1..];

            if (requ.variables.ContainsKey(varname))
            {
                requ.variables[varname] = HttpUtility.HtmlEncode(value) ?? string.Empty;
            }
            else
            {
                requ.variables.Add(varname, HttpUtility.HtmlEncode(value));
            }
        }

        public static void Kw_Urldecode(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = args[1];
            string value = Utils.GetString(args, requ, 2);

            if (varname == string.Empty || value == string.Empty || !varname.StartsWith("$"))
            {
                return;
            }

            varname = varname[1..];

            if (requ.variables.ContainsKey(varname))
            {
                requ.variables[varname] = WebUtility.UrlDecode(value);
            }
            else
            {
                requ.variables.Add(varname, WebUtility.UrlDecode(value));
            }
        }

        private static void Kw_Random(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = "_RANDOM";

            if (varname == string.Empty)
            {
                return;
            }

            if (requ.variables.ContainsKey(varname))
            {
                requ.variables[varname] = Program.random.Next(0, requ.randMax).ToString();
            }
            else
            {
                requ.variables.Add(varname, Program.random.Next(0, requ.randMax).ToString());
            }
        }

        private static void Kw_Readfile(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, requ, 1);

            if (filename == string.Empty)
            {
                return;
            }
            if (System.IO.File.Exists(filename))
            {
                requ.variables["_FILE"] = System.IO.File.ReadAllText(filename);
            }
        }

        private static void Kw_Appendfile(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, requ, 1);
            string content = Utils.GetString(args, requ, 2);

            if (filename == string.Empty || content == string.Empty)
            {
                return;
            }

            System.IO.File.AppendAllText(filename, content);
        }

        private static void Kw_Rmfile(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, requ, 1);

            if (filename == string.Empty)
            {
                return;
            }
            if (System.IO.File.Exists(filename))
            {
                System.IO.File.Delete(filename);
            }
        }

        private static void Kw_Writefile(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, requ, 1);
            string content = Utils.GetString(args, requ, 2);

            if (filename == string.Empty || content == string.Empty)
            {
                return;
            }

            System.IO.File.WriteAllText(filename, content);
        }

        private static void Kw_Existing(Request requ)
        {
            // This is a special keyword that is used to check if a variable exists

            string varName = CodeParser.ParseLineIntoTokens(line)[1];
            if (!varName.StartsWith("$"))
            {
                return;
            }
            varName = varName.Substring(1); // Remove the $ from the name

            if (requ.variables.ContainsKey(varName))
            {
                if (requ.variables.ContainsKey("_EXISTS"))
                {
                    requ.variables["_EXISTS"] = "true";
                }
                else
                {
                    requ.variables.Add("_EXISTS", "true");
                }
            }
            else
            {
                if (requ.variables.ContainsKey("_EXISTS"))
                {
                    requ.variables["_EXISTS"] = "false";
                }
                else
                {
                    requ.variables.Add("_EXISTS", "false");
                }
            }
        }

        private static void Kw_Mail(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string to = Utils.GetString(args, requ, 1);
            string subject = Utils.GetString(args, requ, 2);
            string body = Utils.GetString(args, requ, 3);
            string from = Utils.GetString(args, requ, 4);
            string password = Utils.GetString(args, requ, 5);
            string smtp = Utils.GetString(args, requ, 6);
            int port = 25;

            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient(smtp);

            mail.From = new MailAddress(from);
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;

            SmtpServer.Port = port;
            SmtpServer.Credentials = new System.Net.NetworkCredential(from, password);

            SmtpServer.Send(mail);
        }

        public static int Parse(string lineToParse, HttpListenerContext context, Request requ)
        {
            Utils.Init();
            line = lineToParse;
            ctx = context;

            int i = 0;
            foreach (var keyword in keywords)
            {
                if (line.StartsWith(keyword.Key))
                {
                    i++;
                    keyword.Value(requ);        // We can call this since it's an action
                    if (error == 1)
                    {
                        return 0;
                    }
                    break;
                }
            }
            if (i == 0)
            {
                return 2;
            }
            return 1;
        }

        #region Operators
        public static void Op_DollarEquals(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            Utils.currentChar = 2;
            Program.FormattedPrint(Utils.GetString(args, requ, 1), requ);
        }
        public static void Op_Dollar(Request requ)
        {
            if (line.Contains("@=="))
            {
                if (Program.IsArray(line[1..].Split("@==")[0].Replace(" ", "").Replace(Special.specialChars[(int)Special.SpecialCharacters.array], ""), requ))   // If it's an array
                {
                    requ.arrays[line[1..].Split("@==")[0].Replace(" ", "").Replace(Special.specialChars[(int)Special.SpecialCharacters.array], "")].Add(line.Split("@==")[1].TrimStart()); // Insert into array
                }
                if (Program.IsVariable(line[1..].Split("@==")[0].Replace(" ", ""), requ))
                {
                    requ.variables.Remove(line[1..].Split("@==")[0].Replace(" ", ""));
                }
                requ.variables.Add(line[1..].Split("@==")[0].Replace(" ", ""), Program.Format(line.Split("@==")[1].TrimStart(), requ));
                goto endOfDefine;
            }
            else if (line.Contains("="))        // Array or variable
            {
                if (Program.IsArray(line[1..].Split("=")[0].Replace(" ", "").Replace(Special.specialChars[(int)Special.SpecialCharacters.array], ""), requ))
                {
                    requ.arrays.Remove(line[1..].Split("=")[0].Replace(" ", "").Replace(Special.specialChars[(int)Special.SpecialCharacters.array], ""));
                }
                if (Program.IsVariable(line[1..].Split("=")[0].Replace(" ", "").Replace("{", ""), requ))
                {
                    requ.variables.Remove(line[1..].Split("=")[0].Replace(" ", "").Replace("{", ""));
                }
                if (line[1..].Split("=")[1].Replace(" ", "").StartsWith(Special.specialChars[(int)Special.SpecialCharacters.array]))
                {
                    requ.arrays.Add(line[1..].Split("=")[0].Replace(" ", ""), new List<string>(line[1..].Split(Special.specialChars[(int)Special.SpecialCharacters.array])[1].Split(",")));
                    goto endOfDefine;
                }
                if (line[1..].Split("=")[1].Replace(" ", "").StartsWith("$"))           // Variable -> Variable
                {
                    if (Program.IsVariable(line[1..].Replace(" ", "").Split("=")[1][1..], requ))
                    {
                        requ.variables.Add(line[1..].Split("=")[0].Replace(" ", ""), requ.variables[line[1..].Replace(" ", "").Split("=")[1][1..]]);
                        goto endOfDefine;
                    }
                }
                requ.variables.Add(line[1..].Split("=")[0].Replace(" ", ""), line.Split("=")[1].TrimStart());
            }
            else
            {
                Program.Error("Invalid argument for variable");
                error = 1;
                return;
            }
        endOfDefine:;
        }
        #endregion
        #region Keywords
        public static void Kw_Echo(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            Utils.currentChar = 5;
            Program.pageData += Utils.GetString(args, requ, 1);
        }

        public static void Kw_Rndmax(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1)
            {
                Utils.currentChar = 7;
                try
                {
                    requ.randMax = int.Parse(Utils.GetString(args, requ, 1));
                }
                catch
                {
                    requ.randMax = Utils.defaultReturnValue;
                }
            }
            else
            {
                requ.randMax = Utils.defaultReturnValue;
            }
        }

        public static void Kw_Hash(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string target = Utils.GetString(args, requ, 1, true);

            requ.variables.Add("_HASH", BCrypt.Net.BCrypt.HashPassword(target));
        }

        public static void Kw_CompareHash(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string hash = Utils.GetString(args, requ, 1, true);
            string password = Utils.GetString(args, requ, 2, true);

            requ.variables.Add("_HASHVALID", BCrypt.Net.BCrypt.Verify(password, hash) ? "true" : "false");
        }

        public static void Kw_Sessionset(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 2)
            {
                if (ctx.Request.Cookies["session"] == null)
                {
                    Program.Error("Session not set. Use newsession to create a session");
                    error = 1;
                    return;
                }
                List<string> sessionvalues = System.IO.File.ReadLines(System.IO.Path.Combine(Program.sessionpath, ctx.Request.Cookies["session"].Value)).Where(l => l.StartsWith(args[1])).ToList();
                Utils.currentChar = 11 + args[1].Length;
                sessionvalues.Add(args[1] + ":" + Utils.GetString(args, requ, 2));
                System.IO.File.WriteAllLines(System.IO.Path.Combine(Program.sessionpath, ctx.Request.Cookies["session"].Value), sessionvalues);

            }
        }

        public static void Kw_Sessionget(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1)
            {
                if (ctx.Request.Cookies["session"] == null)
                {
                    Program.Error("Session not set. Use newsession to create a session");
                    error = 1;
                    return;
                }
                List<string> sessionvalues = System.IO.File.ReadLines(System.IO.Path.Combine(Program.sessionpath, ctx.Request.Cookies["session"].Value)).ToList();
                sessionvalues.Reverse();
                if (sessionvalues.Count > 0)
                {
                    if (Program.IsVariable("_SESSIONGET", requ))
                    {
                        requ.variables.Remove("_SESSIONGET");
                    }
                    List<string> resultList = sessionvalues.Where(r => r.StartsWith(args[1])).ToList();
                    requ.variables.Add("_SESSIONGET", resultList[0][(resultList[0].Split(":")[0].Length + 1)..]);
                }
                else
                {
                    if (Program.IsVariable("_SESSIONGET", requ))
                    {
                        requ.variables.Remove("_SESSIONGET");
                    }
                    requ.variables.Add("_SESSIONGET", "");
                }
            }
        }
        public static void Kw_Newsession(Request requ)
        {

            if (!requ.cookies.ContainsKey(ctx.Request.RemoteEndPoint.ToString()))
            {
                if (ctx.Request.Cookies["session"] != null && File.Exists(System.IO.Path.Combine(Program.sessionpath, ctx.Request.Cookies["session"].Value)))
                {
                    return;
                }
                string session = Program.NewString(32);
                while (System.IO.File.Exists(System.IO.Path.Combine(Program.sessionpath, session)))
                {
                    session = Program.NewString(32);
                }
                ctx.Response.Cookies.Clear();
                Cookie cookie = new Cookie("session", session)
                {
                    Path = "/",
                    Expires = DateTime.Now.AddDays(2)
                };
                ctx.Response.Cookies.Add(cookie);
                requ.cookies.Remove(ctx.Request.RemoteEndPoint.ToString());

                requ.cookies.Add(ctx.Request.RemoteEndPoint.ToString(), cookie);
                System.IO.File.Create(System.IO.Path.Combine(Program.sessionpath, ctx.Response.Cookies["session"].Value)).Close();
            }

            if (!Program.IsVariable("_ISSESSION", requ))
            {
                requ.variables.Add("_ISSESSION", "yes");
            }

        }

        public static void Kw_If(Request requ)
        {

            if (line.Contains("="))
            {
                string toCheck = line[3..].Split("=")[0].TrimEnd();        // Just some stuff that makes
                                                                           // it contain the first argument
                if (line.Substring(3, 1) == "$" && Program.IsVariable(line[4..].Split("=")[0].Trim(), requ))
                {
                    requ.variables.TryGetValue(line[4..].Split("=")[0].TrimEnd(), out string varcont);
                    toCheck = varcont;
                }
                string secondCheck = line[3..].Split("=")[1].TrimStart();        // The thing to compare to
                if (line.Split("=")[1].Trim() == "NULL" && !Program.IsVariable(line[4..].Split("=")[0].Trim(), requ))
                {
                    toCheck = null;
                    secondCheck = null;
                }
                if (line.Split("=")[1].Trim() == "NOTHING")
                {
                    secondCheck = "";
                }
                if (line.Split("=")[1].Trim()[..1] == "$" && Program.IsVariable(line[4..].Split("=")[1].Trim()[1..], requ))
                {
                    requ.variables.TryGetValue(line[4..].Split("=")[1].Trim()[1..], out string varcont);
                    secondCheck = varcont;
                }
                if (toCheck != secondCheck)
                {
                    requ.skipIfStmtElse = true;
                }
                else
                {
                    requ.skipElseStmt = true;
                }
            }
        }
        public static void Kw_Getdate(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            if (args.Length > 1)
            {
                if (Program.IsVariable(args[1], requ))
                {
                    requ.variables.Remove(args[1]);
                }
                requ.variables.Add(args[1], DateTime.UtcNow.ToString("yyyy-MM-dd"));
            }
        }
        public static void Kw_Foreach(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1) // Check for argument
            {
                if (args[1][..1] == "$") // Check if it's a variable
                {
                    if (Program.IsArray(args[1][1..], requ))
                    {
                        Program.loopThrough = args[1];
                        if (requ.arrays[Program.loopThrough[1..]].Count > 0)
                        {
                            Program.loopCount = 0;

                            Program.inLoop = true;

                            if (Program.IsVariable("i", requ))
                            {
                                requ.variables.Remove("i");
                            }
                            requ.variables.Add("i", requ.arrays[Program.loopThrough[1..]][Program.loopCount]);
                        }
                    }
                    else
                    {
                        Program.Error("Not an array");
                        error = 1;
                        return;
                    }
                }
                else
                {
                    Program.Error("Variable expected");
                    error = 1;
                    return;
                }
            }
        }
        public static void Kw_Sqlquery(Request requ)
        {
            if (requ.database != null) // If connected
            {
                Program.Query(line[9..], requ);
            }
            else
            {
                Program.Error("Use sqlconnect before sqlquery");
                error = 1;
                return;
            }
        }
        public static void Kw_Escape(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1 && args[1][0] == '$')
            {
                requ.variables[args[1][1..]] = Program.SafeEscape(requ.variables[args[1][1..]]);
            }
            else
            {
                Program.Error("Variable expected");
                error = 1;
                return;
            }
        }
        public static void Kw_Replace(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            if (args.Length > 3 && args[1][0] == '$')
            {
                if (Program.IsVariable(args[1][1..], requ))
                {
                    requ.variables[args[1][1..]] = requ.variables[args[1][1..]].Replace(args[2], args[3]);
                }
            }
            else
            {
                Program.Error("Variable expected");
                error = 1;
                return;
            }
        }
        public static void Kw_Sqlconnect(Request requ)  // Create a new connection
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            if (args.Length != 5)
            {
                Program.Error("Expected 4 arguments (sqlconnect)");
                error = 1;
                return;
            }
            Program.InitializeSQL(args[1], args[2], args[3], args[4], requ);
        }

        // Convert arrays to variables, an array containing "abc, a" will
        // make two variables called $a1 and $a2, $a1 contains abc and $a2 contains a
        public static void Kw_Arraytovars(Request requ)
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args[1].StartsWith("$"))
            {
                if (Program.IsArray(args[1][1..], requ))
                {
                    int thing = 0;
                    foreach (string value in requ.arrays[args[1][1..]])
                    {
                        thing++;
                        if (Program.IsVariable("a" + thing.ToString(), requ))
                        {
                            requ.variables.Remove("a" + thing.ToString());
                        }
                        requ.variables.Add("a" + thing.ToString(), value);
                    }
                }
                else
                {
                    Program.Error("Cannot find variable " + args[1][1..] + ", or not an array");
                    error = 1;
                }
            }
        }
        #endregion
    }
}
