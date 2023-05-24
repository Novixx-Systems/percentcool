using Org.BouncyCastle.Asn1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace percentCool.Utilities
{
    internal class Parser
    {
        private static int error = 0;
        private static string line;
        private static HttpListenerContext ctx;
        
        private readonly static Dictionary<string, Action> keywords = new();

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
            keywords.Add("existing ", Kw_Existing);
            keywords.Add("escape ", Kw_Escape);
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
            keywords.Add("hashcompare ", Kw_CompareHash);
            #endregion

            #region Mail Keywords
            keywords.Add("mail ", Kw_Mail);
            #endregion

            #region File System Keywords
            keywords.Add("writefile ", Kw_Writefile);
            keywords.Add("readfile ", Kw_Readfile);
            keywords.Add("rmfile ", Kw_Rmfile);
            keywords.Add("deletefile ", Kw_Rmfile); // Alias for rmfile
            #endregion
        }

        private static void Kw_Readfile()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, 1);

            if (filename == string.Empty)
            {
                return;
            }
            if (System.IO.File.Exists(filename))
            {
                Program.variables["_FILE"] = System.IO.File.ReadAllText(filename);
            }
        }

        private static void Kw_Rmfile()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, 1);

            if (filename == string.Empty)
            {
                return;
            }
            if (System.IO.File.Exists(filename))
            {
                System.IO.File.Delete(filename);
            }
        }

        private static void Kw_Writefile()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, 1);
            string content = Utils.GetString(args, 2);

            if (filename == string.Empty || content == string.Empty)
            {
                return;
            }

            System.IO.File.WriteAllText(filename, content);
        }

        private static void Kw_Existing()
        {
            // This is a special keyword that is used to check if a variable exists

            string varName = CodeParser.ParseLineIntoTokens(line)[1];
            if (!varName.StartsWith("$"))
            {
                return;
            }
            varName = varName.Substring(1); // Remove the $ from the name

            if (Program.variables.ContainsKey(varName))
            {
                if (Program.variables.ContainsKey("_EXISTS"))
                {
                    Program.variables["_EXISTS"] = "true";
                }
                else
                {
                    Program.variables.Add("_EXISTS", "true");
                }
            }
            else
            {
                if (Program.variables.ContainsKey("_EXISTS"))
                {
                    Program.variables["_EXISTS"] = "false";
                }
                else
                {
                    Program.variables.Add("_EXISTS", "false");
                }
            }
        }

        private static void Kw_Mail()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string to = Utils.GetString(args, 1);
            string subject = Utils.GetString(args, 2);
            string body = Utils.GetString(args, 3);
            string from = Utils.GetString(args, 4);
            string password = Utils.GetString(args, 5);
            string smtp = Utils.GetString(args, 6);
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

        public static int Parse(string lineToParse, HttpListenerContext context)
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
                    keyword.Value();        // We can call this since it's an action
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
        public static void Op_DollarEquals()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            Utils.currentChar = 2;
            Program.FormattedPrint(Utils.GetString(args, 1));
        }
        public static void Op_Dollar()
        {
            if (line.Contains("@=="))
            {
                if (Program.isArray(line[1..].Split("@==")[0].Replace(" ", "").Replace(Special.specialChars[(int)Special.SpecialCharacters.array], "")))   // If it's an array
                {
                    Program.arrays[line[1..].Split("@==")[0].Replace(" ", "").Replace(Special.specialChars[(int)Special.SpecialCharacters.array], "")].Add(line.Split("@==")[1].TrimStart()); // Insert into array
                }
                if (Program.isVariable(line[1..].Split("@==")[0].Replace(" ", "")))
                {
                    Program.variables.Remove(line[1..].Split("@==")[0].Replace(" ", ""));
                }
                Program.variables.Add(line[1..].Split("@==")[0].Replace(" ", ""), Program.Format(line.Split("@==")[1].TrimStart()));
                goto endOfDefine;
            }
            else if (line.Contains("="))        // Array or variable
            {
                if (Program.isArray(line[1..].Split("=")[0].Replace(" ", "").Replace(Special.specialChars[(int)Special.SpecialCharacters.array], "")))
                {
                    Program.arrays.Remove(line[1..].Split("=")[0].Replace(" ", "").Replace(Special.specialChars[(int)Special.SpecialCharacters.array], ""));
                }
                if (Program.isVariable(line[1..].Split("=")[0].Replace(" ", "").Replace("{", "")))
                {
                    Program.variables.Remove(line[1..].Split("=")[0].Replace(" ", "").Replace("{", ""));
                }
                if (line[1..].Split("=")[1].Replace(" ", "").StartsWith(Special.specialChars[(int)Special.SpecialCharacters.array]))
                {
                    Program.arrays.Add(line[1..].Split("=")[0].Replace(" ", ""), new List<string>(line[1..].Split(Special.specialChars[(int)Special.SpecialCharacters.array])[1].Split(",")));
                    goto endOfDefine;
                }
                if (line[1..].Split("=")[1].Replace(" ", "").StartsWith("$"))           // Variable -> Variable
                {
                    if (Program.isVariable(line[1..].Replace(" ", "").Split("=")[1][1..]))
                    {
                        Program.variables.Add(line[1..].Split("=")[0].Replace(" ", ""), Program.variables[line[1..].Replace(" ", "").Split("=")[1][1..]]);
                        goto endOfDefine;
                    }
                }
                Program.variables.Add(line[1..].Split("=")[0].Replace(" ", ""), line.Split("=")[1].TrimStart());
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
        public static void Kw_Echo()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            Utils.currentChar = 5;
            Program.pageData += Utils.GetString(args, 1);
        }

        public static void Kw_Rndmax()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1)
            {
                Utils.currentChar = 7;
                try
                {
                    Program.randMax = int.Parse(Utils.GetString(args, 1));
                }
                catch
                {
                    Program.randMax = Utils.defaultReturnValue;
                }
            }
            else
            {
                Program.randMax = Utils.defaultReturnValue;
            }
        }

        public static void Kw_Hash()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string target = Utils.GetString(args, 1, true);

            Program.variables.Add("_HASH", BCrypt.Net.BCrypt.HashPassword(target));
        }

        public static void Kw_CompareHash()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string hash = Utils.GetString(args, 1, true);
            string password = Utils.GetString(args, 2, true);

            Program.variables.Add("_HASHVALID", BCrypt.Net.BCrypt.Verify(password, hash) ? "true" : "false");
        }

        public static void Kw_Sessionset()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 2)
            {
                if (ctx.Response.Cookies["session"] == null)
                {
                    Program.Error("Session not set. Use newsession to create a session");
                    error = 1;
                    return;
                }
                List<string> sessionvalues = System.IO.File.ReadLines(System.IO.Path.Combine(Program.sessionpath, ctx.Response.Cookies["session"].Value)).Where(l => l.StartsWith(args[1])).ToList();
                Utils.currentChar = 11 + args[1].Length;
                sessionvalues.Add(args[1] + ":" + Utils.GetString(args, 2));
                System.IO.File.WriteAllLines(System.IO.Path.Combine(Program.sessionpath, ctx.Response.Cookies["session"].Value), sessionvalues);

            }
        }

        public static void Kw_Sessionget()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1)
            {
                if (ctx.Response.Cookies["session"] == null)
                {
                    Program.Error("Session not set. Use newsession to create a session");
                    error = 1;
                    return;
                }
                List<string> sessionvalues = System.IO.File.ReadLines(System.IO.Path.Combine(Program.sessionpath, ctx.Response.Cookies["session"].Value)).ToList();
                if (sessionvalues.Count > 0)
                {
                    if (Program.isVariable("_SESSIONGET"))
                    {
                        Program.variables.Remove("_SESSIONGET");
                    }
                    List<string> resultList = sessionvalues.Where(r => r.StartsWith(args[1])).ToList();
                    Program.variables.Add("_SESSIONGET", resultList[0][(resultList[0].Split(":")[0].Length + 2)..]);
                }
                else
                {
                    if (Program.isVariable("_SESSIONGET"))
                    {
                        Program.variables.Remove("_SESSIONGET");
                    }
                    Program.variables.Add("_SESSIONGET", "");
                }
            }
        }
        public static void Kw_Newsession()
        {

            if (!Program.cookies.ContainsKey(ctx.Request.RemoteEndPoint.ToString()))
            {
                if (ctx.Response.Cookies["session"] != null)
                {
                    System.IO.File.Delete(System.IO.Path.Combine(Program.sessionpath, ctx.Response.Cookies["session"].Value));      // Try to delete old session
                }
                string session = Program.NewString(32);
                while (System.IO.File.Exists(System.IO.Path.Combine(Program.sessionpath, session)))
                {
                    session = Program.NewString(32);
                }
                ctx.Response.Cookies.Clear();
                Cookie cookie = new Cookie("session", session)
                {
                    Expires = DateTime.Now.AddDays(2)
                };
                ctx.Response.Cookies.Add(cookie);
                Program.cookies.Remove(ctx.Request.RemoteEndPoint.ToString());

                Program.cookies.Add(ctx.Request.RemoteEndPoint.ToString(), cookie);
                System.IO.File.Create(System.IO.Path.Combine(Program.sessionpath, ctx.Response.Cookies["session"].Value)).Close();
            }

            if (!Program.isVariable("_ISSESSION"))
            {
                Program.variables.Add("_ISSESSION", "yes");
            }

        }

        public static void Kw_If()
        {

            if (line.Contains("="))
            {
                string toCheck = line[3..].Split("=")[0].TrimEnd();        // Just some stuff that makes
                                                                           // it contain the first argument
                if (line.Substring(3, 1) == "$" && Program.isVariable(line[4..].Split("=")[0].Trim()))
                {
                    Program.variables.TryGetValue(line[4..].Split("=")[0].TrimEnd(), out string varcont);
                    toCheck = varcont;
                }
                string secondCheck = line[3..].Split("=")[1].TrimStart();        // The thing to compare to
                if (line.Split("=")[1].Trim() == "NULL" && !Program.isVariable(line[4..].Split("=")[0].Trim()))
                {
                    toCheck = null;
                    secondCheck = null;
                }
                if (line.Split("=")[1].Trim() == "NOTHING")
                {
                    secondCheck = "";
                }
                if (line.Split("=")[1].Trim()[..1] == "$" && Program.isVariable(line[4..].Split("=")[1].Trim()[1..]))
                {
                    Program.variables.TryGetValue(line[4..].Split("=")[1].Trim()[1..], out string varcont);
                    secondCheck = varcont;
                }
                if (toCheck != secondCheck)
                {
                    Program.skipIfStmtElse = true;
                }
                else
                {
                    Program.skipElseStmt = true;
                }
            }
        }
        public static void Kw_Getdate()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            if (args.Length > 1)
            {
                if (Program.isVariable(args[1]))
                {
                    Program.variables.Remove(args[1]);
                }
                Program.variables.Add(args[1], DateTime.UtcNow.ToString("yyyy-MM-dd"));
            }
        }
        public static void Kw_Foreach()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1) // Check for argument
            {
                if (args[1][..1] == "$") // Check if it's a variable
                {
                    if (Program.isArray(args[1][1..]))
                    {
                        Program.loopThrough = args[1];
                        if (Program.arrays[Program.loopThrough[1..]].Count > 0)
                        {
                            Program.loopCount = 0;

                            Program.inLoop = true;

                            if (Program.isVariable("i"))
                            {
                                Program.variables.Remove("i");
                            }
                            Program.variables.Add("i", Program.arrays[Program.loopThrough[1..]][Program.loopCount]);
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
        public static void Kw_Sqlquery()
        {
            if (Program.database != null) // If connected
            {
                Program.Query(line[9..]);
            }
            else
            {
                Program.Error("Use sqlconnect before sqlquery");
                error = 1;
                return;
            }
        }
        public static void Kw_Escape()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1 && args[1][0] == '$')
            {
                Program.variables[args[1][1..]] = Program.safeEscape(Program.variables[args[1][1..]]);
            }
            else
            {
                Program.Error("Variable expected");
                error = 1;
                return;
            }
        }
        public static void Kw_Replace()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            if (args.Length > 3 && args[1][0] == '$')
            {
                if (Program.isVariable(args[1][1..]))
                {
                    Program.variables[args[1][1..]] = Program.variables[args[1][1..]].Replace(args[2], args[3]);
                }
            }
            else
            {
                Program.Error("Variable expected");
                error = 1;
                return;
            }
        }
        public static void Kw_Sqlconnect()  // Create a new connection
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            if (args.Length != 5)
            {
                Program.Error("Expected 4 arguments (sqlconnect)");
                error = 1;
                return;
            }
            Program.InitializeSQL(args[1], args[2], args[3], args[4]);
        }

        // Convert arrays to variables, an array containing "abc, a" will
        // make two variables called $a1 and $a2, $a1 contains abc and $a2 contains a
        public static void Kw_Arraytovars()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args[1].StartsWith("$"))
            {
                if (Program.isArray(args[1][1..]))
                {
                    int thing = 0;
                    foreach (string value in Program.arrays[args[1][1..]])
                    {
                        thing++;
                        if (Program.isVariable("a" + thing.ToString()))
                        {
                            Program.variables.Remove("a" + thing.ToString());
                        }
                        Program.variables.Add("a" + thing.ToString(), value);
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
