// copyright (c) 2023 Novixx Systems


using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Web;
using percentCool.Utilities;
using System.Globalization;
using System.IO;

namespace percentCool
{
    internal static class Program
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
        public static string version = "1.2.1";
        public static HttpListener listener;
        public static string url = "http://*:8000/";
        public static int pageViews = 0;
        public static ulong requestCount = 0;
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

                    if (IsArray("sqlresult"))
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
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        Error("An error occured with the SQL statement");

                    }
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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorContent);
            Console.ForegroundColor = ConsoleColor.White;
        }

        // Check if a string is a variable name
        public static bool IsVariable(string name)
        {
            if (variables.ContainsKey(name))
            {
                return true;
            }
            return false;
        }
        
        public static bool IsArray(string name)
        {
            if (arrays.ContainsKey(name))
            {
                return true;
            }
            return false;
        }

        public static void FormattedPrint(string toPrint)
        {
            pageData += Format(toPrint);
        }
        /// <summary>
        /// Formats a string using a weird formatting thing
        /// </summary>
        /// <param name="toFormat">The string to format</param>
        /// <returns></returns>
        public static string Format(string toFormat)
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
                        Output = Output.Insert(q, random.Next(0, randMax));
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
                byte[] reqs = GetRequestPostData(req);
                foreach (string item in req.QueryString)
                {
                    if (IsVariable("url." + item))
                    {
                        variables.Remove("url." + item);
                    }
                    variables.Add("url." + item, HttpUtility.UrlDecode(req.QueryString[item]));
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
                                    if (IsVariable("post." + name))
                                    {
                                        variables.Remove("post." + name);
                                    }
                                    variables.Add("post." + name, tempFileName);
                                }
                                else
                                {
                                    string value = reqsArr[Array.IndexOf(reqsArr, reqsItem) + 2];
                                    if (IsVariable("post." + name))
                                    {
                                        variables.Remove("post." + name);
                                    }
                                    variables.Add("post." + name, value);
                                }
                            }
                        }
                    }
                    else
                    {
                        string[] eq = req.ContentEncoding.GetString(reqs).Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        foreach (string eqStr in eq)
                        {
                            string[] nd = eqStr.Split("=");
                            if (nd != null)
                            {
                                if (IsVariable("post." + nd[0]))
                                {
                                    variables.Remove(nd[0]);
                                }
                                variables.Add("post." + nd[0], HttpUtility.UrlDecode(nd[1]));
                            }
                        }
                    }
                }
            }
            bool firstPercent;
            int ifs = 1;
            skipIfStmtElse = false;
            for (var j = 0; j < code.Split(new char[] { '\n' }).Length; j++)
            {
                i++;
                string line = code.Split(new char[] { '\n' })[j].Replace("\r", "").Replace("\t", " ").Trim();
                line = Regex.Replace(line, @"\s+", " ");
                if (skipIfStmtElse)
                {
                    if (doingPercent)
                    {
                        if (line.StartsWith("if "))
                        {
                            ifs += 1;
                        }
                        if (line == "else")
                        {
                            if (ifs <= 0)
                            {
                                skipIfStmtElse = false; // endif
                            }
                        }
                        else if (line == "stopif")
                        {
                            ifs--;
                            if (ifs <= 0)
                            {
                                skipIfStmtElse = false; // endif
                            }
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
                                if (IsVariable("i"))
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
                            ParseCOOL(System.IO.File.ReadAllText(line[8..]), req, ctx, true);
                        }
                        else
                        {
                            Error("Cannot open " + line[8..]);
                        }
                    }
                    else if (line.StartsWith("%>"))
                    {
                        doingPercent = false;
                    }
                    else
                    {
                        int t = Parser.Parse(line, ctx);
                        if (t == 2)
                        {
                            Error("Unknown statement " + line);
                            return;
                        }
                    }
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
                    try
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
                    catch { }
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
                data ??= Encoding.UTF8.GetBytes(pageData);
                
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
                else if (where == "exe" || where == "zip" || where == "7z" || where == "rar" || where == "apk" || where == "dll")
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
