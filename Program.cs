// copyright (c) 2022 Novixx Systems


using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Web;

namespace percentCool
{
    internal class Program
    {
        public static Random random = new();
        public static bool skipIfStmtElse = false;
        public static bool skipElseStmt = false;
        public static bool skipElseStmtB = false;
        public static string sessionpath;
        public static bool inLoop = false;
        public static string loopThrough;
        public static int loopCount = 0;
        public static int savedLoopInt = 0;
        public static Dictionary<string, string> variables = new();
        public static Dictionary<string, List<string>> arrays = new();
        public static string version = "1.01";
        public static HttpListener listener;
        public static string url = "http://*:8000/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static int randMax = 10;
        public static bool doingPercent = false;
        public static string where = "";
        public static string server;
        public static string database;
        public static string uid;
        public static string password;
        public static MySqlConnection connection;
        public static List<string> vs1 = new();
        public static Dictionary<string, Cookie> cookies = new();

        static int i;       // For line numbers in errors, and arrays.

        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <body>" +
            "    <p>HTTP 404 NOT Found</p>" +
            "  </body>" +
            "</html>";
        /// <summary>
        /// Generate a random string
        /// </summary>
        /// <param name="length">The length of the new string</param>
        /// <returns></returns>
        public static string NewString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklm";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        /// <summary>
        /// Makes a secure string for MySQL
        /// </summary>
        /// <param name="str">The string to make secure</param>
        /// <returns></returns>
        public static string safeEscape(string str)
        {
            return Regex.Replace(str, @"[\x00'""\b\n\r\t\cZ\\%_]",
                delegate (Match match)
                {
                    string v = match.Value;
                    switch (v)
                    {
                        case "\x00":            // ASCII NUL (0x00) character
                            return "\\0";
                        case "\b":              // BACKSPACE character
                            return "\\b";
                        case "\n":              // NEWLINE (linefeed) character
                            return "\\n";
                        case "\r":              // CARRIAGE RETURN character
                            return "\\r";
                        case "\t":              // TAB
                            return "\\t";
                        case "\u001A":          // Ctrl-Z
                            return "\\Z";
                        default:
                            return "\\" + v;
                    }
                });
        }
        // Function from my old programming language GOOMBAServer
        public static void InitializeSQL(string host, string db, string user, string pass)
        {
            server = host;
            database = db;
            uid = user;
            password = pass;
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
        }
        /// <summary>
        /// Check if source is a multiple of multiple
        /// </summary>
        /// <param name="source">The number to check</param>
        /// <param name="multiple">The multiple of</param>
        /// <returns></returns>
        public static bool IsMultipleOf(int source, int multiple)
        {
            return (source % multiple) == 0;
        }
        public static string GetRequestPostData(HttpListenerRequest request)
        {
            // https://stackoverflow.com/questions/5197579/getting-form-data-from-httplistenerrequest
            if (!request.HasEntityBody)
            {
                return null;
            }
            using System.IO.Stream body = request.InputStream; // here we have data
            using var reader = new System.IO.StreamReader(body, request.ContentEncoding);
            return reader.ReadToEnd();
        }
        //open connection to database
        private static bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        Console.WriteLine("SQL #0: Cannot connect to server.");
                        Error("SQL #0: Cannot connect to server.");
                        break;

                    case 1045:
                        Console.WriteLine("SQL #1045: Invalid user name and/or password.");
                        Error("SQL #1045: Incorrect username/password");
                        break;
                }
                Console.WriteLine(ex.Message);
                Error(ex.Message);
                return false;
            }
        }
        //Close connection
        public static bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        //Query
        public static string[] Query(string cmad)
        {

            Console.WriteLine("Query Started.");
            string query = cmad;
            for (int i = 0; i < variables.Values.Count; i++)
            {
                query = query.Replace("$" + variables.ElementAt(i).Key, variables.ElementAt(i).Value);
            }

            if (query.ToUpper().StartsWith("SELECT"))
            {
                //Open connection
                if (OpenConnection() == true)
                {
                    string[] forSelect = query.Split(' ');
                    Console.WriteLine("Open");
                    //create mysql command
                    MySqlCommand cmd = new()
                    {
                        //Assign the query using CommandText
                        CommandText = query,
                        //Assign the connection using Connection
                        Connection = connection
                    };

                    //Execute query
                    var ret = cmd.ExecuteReader();
                    var i = -1;
                    vs1.Clear();
                    if (forSelect[1] != "*")        // Check if its a wildcard
                    {
                        while (ret.Read())
                        {
                            i++;
                            vs1.Add(ret[forSelect[1]].ToString());
                        }
                    }
                    else
                    {
                        while (ret.Read())
                        {
                            for (var f = 0; f < ret.FieldCount; f++)
                            {
                                i++;
                                vs1.Add(ret[ret.GetName(f)].ToString());
                            }
                        }
                    }
                    ret.Close();

                    if (isArray("sqlresult"))
                    {
                        arrays.Remove("sqlresult");
                    }
                    arrays.Add("sqlresult", vs1);
                    connection.Close();
                    return null;
                }
            }
            else
            {
                //Open connection
                if (OpenConnection() == true)
                {
                    Console.WriteLine("Open");
                    //create mysql command
                    MySqlCommand cmd = new()
                    {
                        //Assign the query using CommandText
                        CommandText = query,
                        //Assign the connection using Connection
                        Connection = connection
                    };

                    //Execute query
                    cmd.ExecuteNonQuery();
                    connection.Close();
                    return null;

                }
                return null;
            }
            return null;



        }
        /// <summary>
        /// Display error message
        /// </summary>
        /// <param name="errorReason">The error message</param>
        public static void Error(string errorReason)    // Error
        {
            string errorContent = "percentCool error: " + errorReason + " at line " + (i + 1).ToString();
            pageData = errorContent;
            System.IO.File.AppendAllText("error_log", errorContent + "\n");
        }

        // Check if a string is a variable name
        public static bool isVariable(string name)
        {
            if (variables.ContainsKey(name))
            {
                return true;
            }
            return false;
        }
        public static bool isArray(string name)
        {
            if (arrays.ContainsKey(name))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Formats a string using a weird formatting thing
        /// </summary>
        /// <param name="toFormat">The string to format</param>
        /// <returns></returns>
        public static string Format(string toFormat)
        {
            return toFormat.Replace("$", pageData).Replace("%%", "&#x25;").Replace("%", version).Replace("\\rnd", random.Next(0, randMax).ToString());
        }
        public static void FormattedPrint(string toPrint)
        {
            pageData += Format(toPrint);
        }
        // Parse COOL code
        public static void ParseCOOL(string code, HttpListenerRequest req, HttpListenerContext ctx, bool included)
        {
            if (!included)
            {
                foreach (Cookie cookie in cookies.Values)
                {
                    if (cookie.Expires <= DateTime.Now)
                    {
                        foreach (var item in cookies.Where(kvp => kvp.Value == cookie).ToList())
                        {
                            cookies.Remove(item.Key);
                            continue;
                        }
                    }
                    foreach (var item in cookies.Where(kvp => kvp.Value == cookie).ToList())
                    {
                        if (ctx.Request.RemoteEndPoint.ToString() == item.Key)
                        {
                            ctx.Response.Cookies.Add(cookie);
                        }
                    }
                }
                variables.Add("_TIME", DateTime.UtcNow.ToString("hh:mm:ss"));
                variables.Add("_DATE", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                doingPercent = false;
                string reqs = GetRequestPostData(req);
                foreach (string item in req.QueryString)
                {
                    if (isVariable("url." + item))
                    {
                        variables.Remove("url." + item);
                    }
                    variables.Add("url." + item, HttpUtility.UrlDecode(req.QueryString[item]));
                }
                if (reqs != null)                       // Get post request into variable
                {
                    string[] eq = reqs.Split("&");
                    foreach (string eqStr in eq)
                    {
                        string[] nd = eqStr.Split("=");
                        if (isVariable(nd[0]))
                        {
                            variables.Remove(nd[0]);
                        }
                        variables.Add(nd[0], HttpUtility.UrlDecode(nd[1]));
                    }
                }
            }
            bool firstPercent;
            for (var j = 0; j < code.Split(new char[] { '\n' }).Length; j++)
            {
                i++;
                string line = code.Split(new char[] { '\n' })[j].Replace("\r", "").Replace("\t", " ").Trim();
                line = Regex.Replace(line, @"\s+", " ");
                if (skipIfStmtElse)
                {
                    if (doingPercent)
                    {
                        if (line == "else")
                        {
                            skipIfStmtElse = false;
                        }
                        else if (line == "stopif")
                        {
                            skipIfStmtElse = false;
                        }
                    }
                    continue;
                }
                if (inLoop)
                {
                    if (doingPercent)
                    {
                        if (line == "stoploop")
                        {
                            if (loopCount >= arrays[loopThrough[1..]].Count - 1)
                            {
                                inLoop = false;
                                savedLoopInt = 0;
                            }
                            else
                            {
                                loopCount++;
                                j -= savedLoopInt;
                                j--;
                                savedLoopInt = 0;
                                if (isVariable("i"))
                                {
                                    variables.Remove("i");
                                }
                                variables.Add("i", arrays[loopThrough[1..]][loopCount]);
                            }
                        }
                        else
                        {
                            savedLoopInt++;
                        }
                    }
                }
                if (skipElseStmt)
                {
                    if (doingPercent)
                    {
                        if (line == "else")
                        {
                            skipElseStmtB = true;
                        }
                        if (line == "stopif")
                        {
                            skipElseStmtB = false;
                            skipElseStmt = false;
                        }
                    }
                    if (skipElseStmtB)
                    {
                        continue;
                    }
                }
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                firstPercent = false;
                if (line.StartsWith("<%cool"))
                {
                    doingPercent = true;            // Enter percent mode
                    firstPercent = true;
                }
                if (doingPercent && !firstPercent)              // If we are in percent mode...
                {
                    if (line.StartsWith("$="))  // Echo (formatted)
                    {
                        if (line.Substring(2, 1) == "$" && isVariable(line[3..].Replace(" ", "")))
                        {
                            // Print formatted string
                            variables.TryGetValue(line[3..].Replace(" ", ""), out string varcont);
                            FormattedPrint(varcont);
                        }
                        else
                        {
                            // Print formatted string
                            FormattedPrint(line[2..]);
                        }
                    }
                    else if (line.StartsWith("echo "))  // Echo
                    {
                        if (line.Substring(5, 1) == "$" && isVariable(line[6..].Replace(" ", "")))
                        {
                            variables.TryGetValue(line[6..].Replace(" ", ""), out string varcont);
                            FormattedPrint(varcont);
                        }
                        else
                        {
                            pageData += line[5..];
                        }
                    }
                    // Include
                    else if (line.StartsWith("include "))  // Include
                    {
                        if (System.IO.File.Exists(line[8..]))
                        {
                            ParseCOOL(System.IO.File.ReadAllText(line[8..]), req, ctx, true);
                        }
                        else
                        {
                            Error("Cannot open " + line[8..]);
                        }
                    }
                    // Set random max
                    else if (line.StartsWith("rndmax "))  // Rndmax
                    {
                        if (line.Split(" ").Length > 1)
                        {
                            randMax = int.Parse(line.Split(" ")[1]);
                        }
                    }
                    else if (line.StartsWith("sessionset "))    // arg1 = variable name, arg2 = variable value
                    {
                        if (line.Split(" ").Length > 2)
                        {
                            try
                            {
                                List<string> sessionvalues = System.IO.File.ReadLines(System.IO.Path.Combine(sessionpath, ctx.Response.Cookies["session"].Value)).Where(l => l.StartsWith(line.Split(" ")[1])).ToList();
                                if (line[(12 + line.Split(" ")[1].Length)..].StartsWith("$") && isVariable(line[(13 + line.Split(" ")[1].Length)..]))
                                {
                                    sessionvalues.Add(line.Split(" ")[1] + ":" + variables[line[(13 + line.Split(" ")[1].Length)..]]);
                                }
                                else
                                {
                                    sessionvalues.Add(line.Split(" ")[1] + ":" + line[(12 + line.Split(" ")[1].Length)..]);
                                }
                                System.IO.File.WriteAllLines(System.IO.Path.Combine(sessionpath, ctx.Response.Cookies["session"].Value), sessionvalues);
                            }
                            catch
                            {
                                Error("Session not set. Use newsession to create a session");
                            }
                        }
                    }
                    else if (line.StartsWith("sessionget "))        // Get session
                    {
                        if (line.Split(" ").Length > 1)
                        {
                            try
                            {
                                List<string> sessionvalues = System.IO.File.ReadLines(System.IO.Path.Combine(sessionpath, ctx.Response.Cookies["session"].Value)).ToList();
                                if (isVariable("_SESSIONGET"))
                                {
                                    variables.Remove("_SESSIONGET");
                                }
                                List<string> resultList = sessionvalues.Where(r => r.StartsWith(line.Split(" ")[1])).ToList();
                                variables.Add("_SESSIONGET", resultList[0][(resultList[0].Split(":")[0].Length + 1)..]);
                            }
                            catch
                            {
                                variables.Add("_SESSIONGET", "");
                            }
                        }
                    }
                    else if (line == "newsession")
                    {
                        if (!cookies.ContainsKey(ctx.Request.RemoteEndPoint.ToString()))
                        {
                            try
                            {
                                System.IO.File.Delete(System.IO.Path.Combine(sessionpath, ctx.Response.Cookies["session"].Value));      // Try to delete old session
                            }
                            catch
                            {
                            }
                            string session = NewString(32);
                            while (System.IO.File.Exists(System.IO.Path.Combine(sessionpath, session)))
                            {
                                session = NewString(32);
                            }
                            ctx.Response.Cookies.Clear();
                            Cookie cookie = new Cookie("session", session)
                            {
                                Expires = DateTime.Now.AddDays(2)
                            };
                            ctx.Response.Cookies.Add(cookie);
                            cookies.Remove(ctx.Request.RemoteEndPoint.ToString());

                            cookies.Add(ctx.Request.RemoteEndPoint.ToString(), cookie);
                            System.IO.File.Create(System.IO.Path.Combine(sessionpath, ctx.Response.Cookies["session"].Value)).Close();
                        }

                        if (!isVariable("_ISSESSION"))
                        {
                            variables.Add("_ISSESSION", "yes");
                        }

                    }
                    // Unlink deletes a file, use with caution!
                    else if (line.StartsWith("unlink "))
                    {
                        if (line.Substring(7, 1) == "$" && isVariable(line[8..].Replace(" ", "")))
                        {
                            variables.TryGetValue(line[8..].Replace(" ", ""), out string varcont);
                            System.IO.File.Delete(System.IO.Path.Combine(Environment.CurrentDirectory, varcont));
                        }
                        else
                        {
                            System.IO.File.Delete(System.IO.Path.Combine(Environment.CurrentDirectory, line[7..]));
                        }
                    }
                    else if (line[..1] == "$")  // Starts with a variable
                    {
                        if (line.Contains("@=="))
                        {
                            if (isArray(line[1..].Split("@==")[0].Replace(" ", "").Replace("|", "")))   // If it's an array
                            {
                                arrays[line[1..].Split("@==")[0].Replace(" ", "").Replace("|", "")].Add(line.Split("@==")[1].TrimStart()); // Insert into array
                            }
                            if (isVariable(line[1..].Split("@==")[0].Replace(" ", "")))
                            {
                                variables.Remove(line[1..].Split("@==")[0].Replace(" ", ""));
                            }
                            variables.Add(line[1..].Split("@==")[0].Replace(" ", ""), Format(line.Split("@==")[1].TrimStart()));
                            goto endOfDefine;
                        }
                        if (line.Contains("="))        // Array or variable
                        {
                            if (isArray(line[1..].Split("=")[0].Replace(" ", "").Replace("|", "")))
                            {
                                arrays.Remove(line[1..].Split("=")[0].Replace(" ", "").Replace("|", ""));
                            }
                            if (isVariable(line[1..].Split("=")[0].Replace(" ", "").Replace("{", "")))
                            {
                                variables.Remove(line[1..].Split("=")[0].Replace(" ", "").Replace("{", ""));
                            }
                            if (line[1..].Split("=")[1].Replace(" ", "").StartsWith("|"))
                            {
                                arrays.Add(line[1..].Split("=")[0].Replace(" ", ""), new List<string>(line[1..].Split("|")[1].Split(",")));
                                goto endOfDefine;
                            }
                            if (line[1..].Split("=")[1].Replace(" ", "").StartsWith("$"))           // Variable -> Variable
                            {
                                if (isVariable(line[1..].Replace(" ", "").Split("=")[1][1..]))
                                {
                                    variables.Add(line[1..].Split("=")[0].Replace(" ", ""), variables[line[1..].Replace(" ", "").Split("=")[1][1..]]);
                                    goto endOfDefine;
                                }
                            }
                            variables.Add(line[1..].Split("=")[0].Replace(" ", ""), line.Split("=")[1].TrimStart());
                        }
                        else
                        {
                            Error("Invalid argument for variable");
                            return;
                        }
endOfDefine:
                        ((Action)(() => { }))();    // Nothing
                    }
                    // Here comes the if statement...
                    else if (line.StartsWith("if "))
                    {
                        if (line.Contains("="))
                        {
                            string toCheck = line[3..].Split("=")[0].TrimEnd();        // Just some stuff that makes
                                                                                       // it contain the first argument
                            if (line.Substring(3, 1) == "$" && isVariable(line[4..].Split("=")[0].Trim()))
                            {
                                variables.TryGetValue(line[4..].Split("=")[0].TrimEnd(), out string varcont);
                                toCheck = varcont;
                            }
                            string secondCheck = line[3..].Split("=")[1].TrimStart();        // The thing to compare to
                            if (line.Split("=")[1].Trim() == "NULL" && !isVariable(line[4..].Split("=")[0].Trim()))
                            {
                                toCheck = null;
                                secondCheck = null;
                            }
                            if (line.Split("=")[1].Trim() == "NOTHING")
                            {
                                secondCheck = "";
                            }
                            if (line.Split("=")[1].Trim()[..1] == "$" && isVariable(line[4..].Split("=")[1].Trim()[1..]))
                            {
                                variables.TryGetValue(line[4..].Split("=")[1].Trim()[1..], out string varcont);
                                secondCheck = varcont;
                            }
                            if (toCheck != secondCheck)
                            {
                                skipIfStmtElse = true;
                            }
                            else
                            {
                                skipElseStmt = true;
                            }
                        }
                    }
                    else if (line.StartsWith("getdate "))
                    {
                        if (line.Split(" ").Length > 1)
                        {
                            if (isVariable(line.Split(" ")[1]))
                            {
                                variables.Remove(line.Split(" ")[1]);
                            }
                            variables.Add(line.Split(" ")[1], DateTime.UtcNow.ToString("yyyy-MM-dd"));
                        }
                    }
                    else if (line.StartsWith("foreach "))
                    {
                        if (line.Split(" ").Length > 1)
                        {
                            if (line.Split(" ")[1][..1] == "$")
                            {
                                if (isArray(line.Split(" ")[1][1..]))
                                {
                                    loopThrough = line.Split(" ")[1];
                                    if (arrays[loopThrough[1..]].Count > 0)
                                    {
                                        loopCount = 0;

                                        inLoop = true;

                                        if (isVariable("i"))
                                        {
                                            variables.Remove("i");
                                        }
                                        variables.Add("i", arrays[loopThrough[1..]][loopCount]);
                                    }
                                }
                                else
                                {
                                    Error("Not an array");
                                    return;
                                }
                            }
                            else
                            {
                                Error("Variable expected");
                                return;
                            }
                        }
                    }
                    else if (line.StartsWith("sqlquery "))
                    {
                        if (database != null)
                        {
                            Query(line[9..]);
                        }
                        else
                        {
                            Error("Use sqlconnect before sqlquery");
                            return;
                        }
                    }
                    else if (line.StartsWith("escape "))
                    {
                        if (line.Split(" ").Length > 1 && line.Split(" ")[1][0] == '$')
                        {
                            variables[line.Split(" ")[1][1..]] = safeEscape(variables[line.Split(" ")[1][1..]]);
                        }
                        else
                        {
                            Error("Variable expected");
                            return;
                        }
                    }
                    else if (line.StartsWith("replace "))
                    {
                        if (line.Split(" ").Length > 3 && line.Split(" ")[1][0] == '$')
                        {
                            if (isVariable(line.Split(" ")[1][1..]))
                            {
                                variables[line.Split(" ")[1][1..]] = variables[line.Split(" ")[1][1..]].Replace(line.Split(" ")[2], line.Split(" ")[3]);
                            }
                        }
                        else
                        {
                            Error("Variable expected");
                            return;
                        }
                    }
                    else if (line.StartsWith("sqlconnect "))
                    {
                        if (line.Split(" ").Length != 5)
                        {
                            Error("Expected 4 arguments (sqlconnect)");
                            return;
                        }
                        InitializeSQL(line.Split(" ")[1], line.Split(" ")[2], line.Split(" ")[3], line.Split(" ")[4]);
                    }
                    else if (line.StartsWith("arraytovars "))    // Convert arrays to variables, an array containing "abc, a" will
                                                                 // make two variables called $a1 and $a2, $a1 contains abc and $a2 contains a
                    {
                        if (line.Split(" ")[1].StartsWith("$"))
                        {
                            if (isArray(line.Split(" ")[1][1..]))
                            {
                                int thing = 0;
                                foreach (string value in arrays[line.Split(" ")[1][1..]])
                                {
                                    thing++;
                                    if (isVariable("a" + thing.ToString()))
                                    {
                                        variables.Remove("a" + thing.ToString());
                                    }
                                    variables.Add("a" + thing.ToString(), value);
                                }
                            }
                            else
                            {
                                Error("Cannot find variable " + line.Split(" ")[1][1..] + ", or not an array");
                            }
                        }
                    }
                    else if (line.StartsWith("//"))
                    {
                    }
                    else if (line == "stopif")
                    {
                    }
                    else if (line == "stoploop")
                    {
                    }
                    else if (line == "else")
                    {
                    }
                    else if (line.StartsWith("%>"))
                    {
                        doingPercent = false;
                    }
                    else { Error("Unknown statement " + line); return; }
                }
                else
                {
                    if (!firstPercent) pageData += line + "\n";
                }
            }
        }


        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            while (runServer)
            {
                i = 0;
                byte[] data = null;
                variables.Clear();
                randMax = 0;
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();
                pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <body>" +
            "    <p>HTTP 404 NOT Found</p>" +
            "  </body>" +
            "</html>";
                if (System.IO.File.Exists(req.Url.AbsolutePath[1..]))
                {
                    where = req.Url.AbsolutePath[1..].Split(".")[1];
                    if (where == "html" || where == "cool")
                    {
                        pageData = "";
                        ParseCOOL(System.IO.File.ReadAllText(req.Url.AbsolutePath[1..]), req, ctx, false);
                    }
                    else
                    {
                        data = System.IO.File.ReadAllBytes(req.Url.AbsolutePath[1..]);
                    }
                }
                else
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.cool")))
                    {
                        pageData = "";
                        ParseCOOL(System.IO.File.ReadAllText(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.cool")), req, ctx, false);
                        where = "cool";
                    }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.html")))
                    {
                        pageData = "";
                        ParseCOOL(System.IO.File.ReadAllText(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.html")), req, ctx, false);
                        where = "html";
                    }
                }

                // Write the response info
                if (data == null)
                {
                    data = Encoding.UTF8.GetBytes(pageData);
                }
                if (where == "html" || where == "cool")
                {
                    resp.ContentType = "text/html";
                }
                else if (where == "png")
                {
                    resp.ContentType = "image/png";
                }
                else if (where == "jpg" || where == "jpeg")
                {
                    resp.ContentType = "image/jpeg";
                }
                else if (where == "mp3")
                {
                    resp.ContentType = "audio/mpeg";
                }
                else
                {
                    resp.ContentType = "text/plain";
                }
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }


        public static void Main()
        {
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, "www")))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Environment.CurrentDirectory, "www"));
            }
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, "sessions")))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Environment.CurrentDirectory, "sessions"));
            }
            sessionpath = System.IO.Path.Combine(Environment.CurrentDirectory, "sessions");
            Environment.CurrentDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "www");
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Waiting for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}
