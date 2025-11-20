// copyright (c) 2023 Novixx Systems


using MySql.Data.MySqlClient;
using Org.BouncyCastle.Ocsp;
using percentCool.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace percentCool
{
    internal static class Program
    {
        public static Random random = new();
        public static string sessionpath;
        public static bool inLoop = false;
        public static string loopThrough;
        public static int loopCount = 0;
        public static int savedLoopInt = 0;

        public static string version = "1.2.1";
        public static HttpListener listener;
        public static string url = "http://*:8000/";
        public static int pageViews = 0;
        public static ulong requestCount = 0;

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
        public static string SafeEscape(string str)
        {
            return Regex.Replace(str, @"[\x00'""\b\n\r\t\cZ\\%_]",
                delegate (Match match)
                {
                    return match.Value switch
                    {
                        // ASCII NUL (0x00) character
                        "\x00" => "\\0",
                        
                        // BACKSPACE character
                        "\b" => "\\b",
                        
                        // NEWLINE (linefeed) character
                        "\n" => "\\n",
                        
                        // CARRIAGE RETURN character
                        "\r" => "\\r",
                        
                        // TAB
                        "\t" => "\\t",
                        
                        // Ctrl-Z
                        "\u001A" => "\\Z",
                        
                        _ => "\\" + match.Value,
                    };
                });
        }
        // Function from my old programming language GOOMBAServer
        public static void InitializeSQL(string host, string db, string user, string pass, Request requ)
        {
            requ.server = host;
            requ.database = db;
            requ.uid = user;
            requ.password = pass;
            string connectionString;
            connectionString = "SERVER=" + requ.server + ";" + "DATABASE=" +
            requ.database + ";" + "UID=" + requ.uid + ";" + "PASSWORD=" + requ.password + ";";
            requ.connection = new MySqlConnection(connectionString);
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
        public static byte[] GetRequestPostData(HttpListenerRequest request)
        {
            // https://stackoverflow.com/questions/5197579/getting-form-data-from-httplistenerrequest
            if (!request.HasEntityBody)
            {
                return null;
            }
            using System.IO.Stream body = request.InputStream; // here we have data
            using var reader = new System.IO.StreamReader(body, request.ContentEncoding);
            return request.ContentEncoding.GetBytes(reader.ReadToEnd());
        }
        //open connection to database
        private static bool OpenConnection(Request requ)
        {
            try
            {
                requ.connection.Open();
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
        public static bool CloseConnection(Request requ)
        {
            try
            {
                requ.connection.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        //Query
        public static string[] Query(string cmad, Request requ)
        {

            Console.WriteLine("Query Started.");
            string query = cmad;
            for (int i = 0; i < requ.variables.Values.Count; i++)
            {
                query = query.Replace("$" + requ.variables.ElementAt(i).Key, requ.variables.ElementAt(i).Value);
            }

            if (query.ToUpper().StartsWith("SELECT"))
            {
                //Open connection
                if (OpenConnection(requ) == true)
                {
                    string[] forSelect = query.Split(' ');
                    Console.WriteLine("Open");
                    //create mysql command
                    MySqlCommand cmd = new()
                    {
                        //Assign the query using CommandText
                        CommandText = query,
                        //Assign the connection using Connection
                        Connection = requ.connection
                    };

                    //Execute query
                    var ret = cmd.ExecuteReader();
                    var i = -1;
                    requ.vs1.Clear();
                    if (forSelect[1] != "*")        // Check if its a wildcard
                    {
                        while (ret.Read())
                        {
                            i++;
                            requ.vs1.Add(ret[forSelect[1]].ToString());
                        }
                    }
                    else
                    {
                        while (ret.Read())
                        {
                            for (var f = 0; f < ret.FieldCount; f++)
                            {
                                i++;
                                requ.vs1.Add(ret[ret.GetName(f)].ToString());
                            }
                        }
                    }
                    ret.Close();

                    if (IsArray("sqlresult", requ))
                    {
                        requ.arrays.Remove("sqlresult");
                    }
                    requ.arrays.Add("sqlresult", requ.vs1);
                    requ.connection.Close();
                    return null;
                }
            }
            else
            {
                //Open connection
                if (OpenConnection(requ) == true)
                {
                    Console.WriteLine("Open");
                    //create mysql command
                    MySqlCommand cmd = new()
                    {
                        //Assign the query using CommandText
                        CommandText = query,
                        //Assign the connection using Connection
                        Connection = requ.connection
                    };

                    //Execute query
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        Error("An error occured with the SQL statement");

                    }
                    requ.connection.Close();
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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorContent);
            Console.ForegroundColor = ConsoleColor.White;
        }

        // Check if a string is a variable name
        public static bool IsVariable(string name, Request requ)
        {
            if (requ.variables.ContainsKey(name))
            {
                return true;
            }
            return false;
        }
        
        public static bool IsArray(string name, Request requ)
        {
            if (requ.arrays.ContainsKey(name))
            {
                return true;
            }
            return false;
        }

        public static void FormattedPrint(string toPrint, Request requ)
        {
            pageData += Format(toPrint, requ);
        }
        /// <summary>
        /// Formats a string using a weird formatting thing
        /// </summary>
        /// <param name="toFormat">The string to format</param>
        /// <returns></returns>
        public static string Format(string toFormat, Request requ)
        {
            bool isEscaper = false; // Bool for checking if the escape character appeared
            StringBuilder Output = new(new string(' ', 32768));
            int q = 0;
            foreach(char c in toFormat)
            {
                if (isEscaper)
                {
                    isEscaper = false;
                    Output[q] = c;
                }
                else
                {
                    if (c == '$')
                    {
                        Output = Output.Insert(q, pageData);
                    }
                    else if (c == '%')
                    {
                        Output = Output.Insert(q, version);
                    }
                    else if (c == '~')
                    {
                        Output = Output.Insert(q, random.Next(0, requ.randMax));
                    }
                    else if (c != '\\')
                    {
                        Output[q] = c;
                    }
                }
                if (c == Special.specialChars[(int)Special.SpecialCharacters.escape].ToCharArray()[0])
                {
                    isEscaper = true;
                }
                q++;
            }
            return Output.ToString().Trim();
        }
        // Parse COOL code
        public static void ParseCOOL(string code, HttpListenerRequest req, HttpListenerContext ctx, bool included, Request requ)
        {
            if (!included)
            {
                foreach (Cookie cookie in requ.cookies.Values)
                {
                    if (cookie.Expires <= DateTime.Now)
                    {
                        foreach (var item in requ.cookies.Where(kvp => kvp.Value == cookie).ToList())
                        {
                            requ.cookies.Remove(item.Key);
                            continue;
                        }
                    }
                    foreach (var item in requ.cookies.Where(kvp => kvp.Value == cookie).ToList())
                    {
                        if (ctx.Request.RemoteEndPoint.ToString() == item.Key)
                        {
                            ctx.Response.Cookies.Add(cookie);
                        }
                    }
                }
                requ.variables.Add("_TIME", DateTime.UtcNow.ToString("hh:mm:ss"));
                requ.variables.Add("_DATE", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                requ.doingPercent = false;
                byte[] reqs = GetRequestPostData(req);
                foreach (string item in req.QueryString)
                {
                    if (IsVariable("url." + item, requ))
                    {
                        requ.variables.Remove("url." + item);
                    }
                    requ.variables.Add("url." + item, HttpUtility.UrlDecode(req.QueryString[item]));
                }
                if (reqs != null)                       // Get post request into variable
                {
                    bool isMultipartFormdata = false;
                    if (req.ContentType != null)
                    {
                        if (req.ContentType.Contains("multipart/form-data;"))
                        {
                            isMultipartFormdata = true;
                        }
                    }
                    if (isMultipartFormdata)
                    {
                        string[] reqsArr = req.ContentEncoding.GetString(reqs).Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        foreach (string reqsItem in reqsArr)
                        {
                            if (reqsItem.Contains("name=\""))
                            {
                                string[] reqsItemArr = reqsItem.Split(new char[] { ';' });
                                string[] reqsItemArr2 = reqsItemArr[1].Split(new char[] { '=' });
                                string name = reqsItemArr2[1].Replace("\"", "");
                                string type = reqsArr[Array.IndexOf(reqsArr, reqsItem) + 1];
                                if (type != "")
                                {
                                    // Upload file
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("percentCool warning: File upload detected, this is still experimental and may not work for binaries");
                                    Console.ForegroundColor = ConsoleColor.White;
                                    string tempFileName = "atm_" + random.NextInt64(10101010101010, 99999999999999).ToString() + ".aks";
                                    Utils.SaveFile(req.ContentEncoding.GetString(reqs), tempFileName); // Save it
                                    if (IsVariable("post." + name, requ))
                                    {
                                        requ.variables.Remove("post." + name);
                                    }
                                    requ.variables.Add("post." + name, tempFileName);
                                }
                                else
                                {
                                    string value = reqsArr[Array.IndexOf(reqsArr, reqsItem) + 2];
                                    if (IsVariable("post." + name, requ))
                                    {
                                        requ.variables.Remove("post." + name);
                                    }
                                    requ.variables.Add("post." + name, value);
                                }
                            }
                        }
                    }
                    else
                    {
                        string[] eq = req.ContentEncoding.GetString(reqs).Split(new string[] { "&" }, StringSplitOptions.None);
                        foreach (string eqStr in eq)
                        {
                            string[] nd = eqStr.Split("=");
                            if (nd != null)
                            {
                                if (IsVariable("post." + nd[0], requ))
                                {
                                    requ.variables.Remove(nd[0]);
                                }
                                requ.variables.Add("post." + nd[0], HttpUtility.UrlDecode(nd[1]));
                            }
                        }
                    }
                }
            }
            bool firstPercent;
            int ifs = 1;
            requ.skipIfStmtElse = false;
            for (var j = 0; j < code.Split(new char[] { '\n' }).Length; j++)
            {
                i++;
                string line = code.Split(new char[] { '\n' })[j].Replace("\r", "").Replace("\t", " ").Trim();
                line = Regex.Replace(line, @"\s+", " ");
                if (inLoop)
                {
                    if (requ.doingPercent)
                    {
                        if (line == "stoploop")
                        {
                            if (loopCount >= requ.arrays[loopThrough[1..]].Count - 1)
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
                                if (IsVariable("i", requ))
                                {
                                    requ.variables.Remove("i");
                                }
                                requ.variables.Add("i", requ.arrays[loopThrough[1..]][loopCount]);
                            }
                        }
                        else
                        {
                            savedLoopInt++;
                        }
                    }
                }
                if (requ.skipElseStmt)
                {
                    if (requ.doingPercent)
                    {
                        if (line == "else")
                        {
                            requ.skipElseStmtB = true;
                        }
                        if (line == "stopif")
                        {
                            requ.skipElseStmtB = false;
                            requ.skipElseStmt = false;
                        }
                    }
                    if (requ.skipElseStmtB)
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
                    requ.doingPercent = true;            // Enter percent mode
                    firstPercent = true;
                }
                if (requ.doingPercent && !firstPercent)              // If we are in percent mode...
                {
                    if (line.StartsWith("//"))
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
                    // Include is not part of the parser
                    else if (line.StartsWith("include "))  // Include
                    {
                        if (System.IO.File.Exists(line[8..]))
                        {
                            ParseCOOL(System.IO.File.ReadAllText(line[8..]), req, ctx, true, requ);
                        }
                        else
                        {
                            Error("Cannot open " + line[8..]);
                        }
                    }
                    else if (line.StartsWith("%>"))
                    {
                        requ.doingPercent = false;
                    }
                    else if (!requ.skipIfStmtElse)
                    {
                        int t = Parser.Parse(line, ctx, requ);
                        if (t == 2)
                        {
                            Error("Unknown statement " + line);
                            return;
                        }
                        if (requ.skipIfStmtElse)
                        {
                            ifs = -1;
                        }
                    }
                }
                else
                {
                    if (!firstPercent) pageData += line + "\n";
                }
                if (requ.skipIfStmtElse)
                {
                    if (requ.doingPercent)
                    {
                        if (line.StartsWith("if "))
                        {
                            ifs += 1;
                        }
                        if (line == "else")
                        {
                            if (ifs <= 0)
                            {
                                requ.skipIfStmtElse = false; // endif
                            }
                        }
                        else if (line == "stopif")
                        {
                            ifs--;
                            if (ifs <= 0)
                            {
                                requ.skipIfStmtElse = false; // endif
                            }
                        }
                    }
                    continue;
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
                Request requ = new Request();
                requ.randMax = 0;
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
                    try
                    {
                        requ.where = req.Url.AbsolutePath[1..].Split(".")[1];
                        if (requ.where == "html" || requ.where == "cool")
                        {
                            pageData = "";
                            ParseCOOL(System.IO.File.ReadAllText(req.Url.AbsolutePath[1..]), req, ctx, false, requ);
                        }
                        else
                        {
                            data = System.IO.File.ReadAllBytes(req.Url.AbsolutePath[1..]);
                        }
                    }
                    catch { }
                }
                else
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.cool")))
                    {
                        pageData = "";
                        ParseCOOL(System.IO.File.ReadAllText(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.cool")), req, ctx, false, requ);
                        requ.where = "cool";
                    }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.html")))
                    {
                        pageData = "";
                        ParseCOOL(System.IO.File.ReadAllText(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.html")), req, ctx, false, requ);
                        requ.where = "html";
                    }
                }

                // Write the response info
                data ??= Encoding.UTF8.GetBytes(pageData);
                
                if (requ.where == "html" || requ.where == "cool")
                {
                    resp.ContentType = "text/html";
                }
                else if (requ.where == "png")
                {
                    resp.ContentType = "image/png";
                }
                else if (requ.where == "jpg" || requ.where == "jpeg")
                {
                    resp.ContentType = "image/jpeg";
                }
                else if (requ.where == "mp3")
                {
                    resp.ContentType = "audio/mpeg";
                }
                else if (requ.where == "exe" || requ.where == "zip" || requ.where == "7z" || requ.where == "rar" || requ.where == "apk" || requ.where == "dll")
                {
                    resp.ContentType = "application/octet-stream";
                }
                else
                {
                    resp.ContentType = "text/plain";
                }
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data);
                resp.Close();
            }
        }


        public static void Main()
        {
            // Check for file .cfg
            if (System.IO.File.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, "cool.cfg")))
            {
                string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(Environment.CurrentDirectory, "cool.cfg"));
                foreach (string line in lines)
                {
                    if (line.StartsWith("url="))
                    {
                        url = line[4..];
                    }
                }
            }
            Console.WriteLine("percentCool version " + version);
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

            Parser.Init();
            Utils.Init();

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
