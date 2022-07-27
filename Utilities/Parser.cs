using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace percentCool.Utilities
{
    internal class Parser
    {
        public static int error = 0;
        public static Dictionary<string, Action> keywords = new();
        public static string line;
        public static HttpListenerContext ctx;

        public static void Init()
        {
            keywords.Clear();

            // All keywords
            //
            // NOTE: Most keywords MUST end with a space, operators
            // must NOT end with a space. Keywords that have no arguments
            // should also NOT end with a space.
            //
            keywords.Add("$=", new Action(Op_DollarEquals));
            keywords.Add("$", new Action(Op_Dollar));
            keywords.Add("echo ", new Action(Kw_Echo));
            keywords.Add("rndmax ", new Action(Kw_Rndmax));
            keywords.Add("sessionset ", new Action(Kw_Sessionset));
            keywords.Add("sessionget ", new Action(Kw_Sessionget));
            keywords.Add("newsession", new Action(Kw_Newsession));
            keywords.Add("if ", new Action(Kw_If));
            keywords.Add("getdate ", new Action(Kw_Getdate));
            keywords.Add("foreach ", new Action(Kw_Foreach));
            keywords.Add("sqlquery ", new Action(Kw_Sqlquery));
            keywords.Add("escape ", new Action(Kw_Escape));
            keywords.Add("replace ", new Action(Kw_Replace));
            keywords.Add("sqlconnect ", new Action(Kw_Sqlconnect));
            keywords.Add("arraytovars ", new Action(Kw_Arraytovars));
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
            Utils.currentChar = 2;
            Program.FormattedPrint(Utils.GetString());
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
            if (line.Contains("="))        // Array or variable
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
endOfDefine:
            ((Action)(() => { }))();    // Nothing
        }
        #endregion
        #region Keywords
        public static void Kw_Echo()
        {
            Utils.currentChar = 5;
            Program.pageData += Utils.GetString();
        }

        public static void Kw_Rndmax()
        {
            if (line.Split(" ").Length > 1)
            {
                Utils.currentChar = 7;
                try
                {
                    Program.randMax = int.Parse(Utils.GetString());
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

        public static void Kw_Sessionset()
        {
            if (line.Split(" ").Length > 2)
            {
                if (ctx.Response.Cookies["session"] == null)
                {
                    Program.Error("Session not set. Use newsession to create a session");
                    error = 1;
                    return;
                }
                List<string> sessionvalues = System.IO.File.ReadLines(System.IO.Path.Combine(Program.sessionpath, ctx.Response.Cookies["session"].Value)).Where(l => l.StartsWith(line.Split(" ")[1])).ToList();
                Utils.currentChar = 11 + line.Split(" ")[1].Length;
                sessionvalues.Add(line.Split(" ")[1] + ":" + Utils.GetString());
                System.IO.File.WriteAllLines(System.IO.Path.Combine(Program.sessionpath, ctx.Response.Cookies["session"].Value), sessionvalues);

            }
        }

        public static void Kw_Sessionget()
        {
            if (line.Split(" ").Length > 1)
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
                    List<string> resultList = sessionvalues.Where(r => r.StartsWith(line.Split(" ")[1])).ToList();
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
            if (line.Split(" ").Length > 1)
            {
                if (Program.isVariable(line.Split(" ")[1]))
                {
                    Program.variables.Remove(line.Split(" ")[1]);
                }
                Program.variables.Add(line.Split(" ")[1], DateTime.UtcNow.ToString("yyyy-MM-dd"));
            }
        }
        public static void Kw_Foreach()
        {

            if (line.Split(" ").Length > 1) // Check for argument
            {
                if (line.Split(" ")[1][..1] == "$") // Check if it's a variable
                {
                    if (Program.isArray(line.Split(" ")[1][1..]))
                    {
                        Program.loopThrough = line.Split(" ")[1];
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
            if (line.Split(" ").Length > 1 && line.Split(" ")[1][0] == '$')
            {
                Program.variables[line.Split(" ")[1][1..]] = Program.safeEscape(Program.variables[line.Split(" ")[1][1..]]);
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
            if (line.Split(" ").Length > 3 && line.Split(" ")[1][0] == '$')
            {
                if (Program.isVariable(line.Split(" ")[1][1..]))
                {
                    Program.variables[line.Split(" ")[1][1..]] = Program.variables[line.Split(" ")[1][1..]].Replace(line.Split(" ")[2], line.Split(" ")[3]);
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
            if (line.Split(" ").Length != 5)
            {
                Program.Error("Expected 4 arguments (sqlconnect)");
                error = 1;
                return;
            }
            Program.InitializeSQL(line.Split(" ")[1], line.Split(" ")[2], line.Split(" ")[3], line.Split(" ")[4]);
        }

        // Convert arrays to variables, an array containing "abc, a" will
        // make two variables called $a1 and $a2, $a1 contains abc and $a2 contains a
        public static void Kw_Arraytovars()
        {

            if (line.Split(" ")[1].StartsWith("$"))
            {
                if (Program.isArray(line.Split(" ")[1][1..]))
                {
                    int thing = 0;
                    foreach (string value in Program.arrays[line.Split(" ")[1][1..]])
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
                    Program.Error("Cannot find variable " + line.Split(" ")[1][1..] + ", or not an array");
                    error = 1;
                }
            }
        }
        #endregion
    }
}
