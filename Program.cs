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

        static int i;       // For line numbers in errors, and arrays.

        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <body>" +
            "    <p>HTTP 404 NOT Found</p>" +
            "  </body>" +
            "</html>";
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
            pageData = "percentCool error: " + errorReason + " at line " + (i + 1).ToString();
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
        public static void ParseCOOL(string code, HttpListenerRequest req)
        {
            doingPercent = false;
            string reqs = GetRequestPostData(req);
            if (reqs != null)                       // Get post request into variable
            {
                string[] eq = reqs.Split("&");
                foreach (string eqStr in eq)
                {
                    string[] nd = eqStr.Split("=");
                    variables.Add(nd[0], HttpUtility.UrlDecode(nd[1]));
                }
            }
            bool firstPercent;
            for (i = 0; i < code.Split(new char[] { '\n' }).Length; i++)
            {
                string line = code.Split(new char[] { '\n' })[i].Replace("\r", "").Replace("\t", " ").Trim();
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
                            if (loopCount >= arrays[loopThrough[1..]].Count-1)
                            {
                                inLoop = false;
                                savedLoopInt = 0;
                            }
                            else
                            {
                                loopCount++;
                                i -= savedLoopInt;
                                i--;
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
                    // Set random max
                    else if (line.StartsWith("rndmax "))  // Rndmax
                    {
                        if (line.Split(" ").Length > 0)
                        {
                            randMax = int.Parse(line.Split(" ")[1]);
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
                        }
                        else if (line.Contains("="))        // Array or variable
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
                            variables.Add(line[1..].Split("=")[0].Replace(" ", ""), line.Split("=")[1].TrimStart());
endOfDefine:
                            ((Action)(() => { }))();    // Nothing
                        }
                        else
                        {
                            Error("Invalid argument for variable");
                            return;
                        }
                    }
                    // Here comes the if statement...
                    else if (line.StartsWith("if"))
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
                    else if (line.StartsWith("foreach"))
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
                    else if (line.StartsWith("sqlquery"))
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
                    else if (line.StartsWith("escape"))
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
                    else if (line.StartsWith("replace"))
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
                    else if (line.StartsWith("sqlconnect"))
                    {
                        if (line.Split(" ").Length != 5)
                        {
                            Error("Expected 4 arguments (sqlconnect)");
                            return;
                        }
                        InitializeSQL(line.Split(" ")[1], line.Split(" ")[2], line.Split(" ")[3], line.Split(" ")[4]);
                    }
                    else if (line.StartsWith("arraytovars"))    // Convert arrays to variables, an array containing "abc, a" will
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

                if (System.IO.File.Exists(req.Url.AbsolutePath[1..]))
                {
                    where = req.Url.AbsolutePath[1..].Split(".")[1];
                    if (where == "html" || where == "cool")
                    {
                        pageData = "";
                        ParseCOOL(System.IO.File.ReadAllText(req.Url.AbsolutePath[1..]), req);
                    }
                    else
                    {
                        data = System.IO.File.ReadAllBytes(req.Url.AbsolutePath[1..]);
                    }
                }
                else
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(Environment.CurrentDirectory,req.Url.AbsolutePath[1..],"index.cool")))
                    {
                        pageData = "";
                        ParseCOOL(System.IO.File.ReadAllText(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.cool")), req);
                        where = "cool";
                    }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.html")))
                    {
                        pageData = "";
                        ParseCOOL(System.IO.File.ReadAllText(System.IO.Path.Combine(Environment.CurrentDirectory, req.Url.AbsolutePath[1..], "index.html")), req);
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
