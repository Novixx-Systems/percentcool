using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace percentCool
{
    internal class Program
    {
        public static bool skipIfStmt = false;
        public static Dictionary<string, string> variables = new Dictionary<string, string>();
        public static string version = "1.01";
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static bool doingPercent = false;
        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <body>" +
            "    <p>HTTP 404 NOT Found</p>" +
            "  </body>" +
            "</html>";
        public static void Error(string errorReason)    // Error
        {
            pageData = "percentCool error: " + errorReason;
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
        public static string Format(string toFormat)
        {
            return toFormat.Replace("$", pageData).Replace("%%", "&#x25;").Replace("%", version);
        }
        public static void FormattedPrint(string toPrint)
        {
            pageData += Format(toPrint);
        }
        // Parse COOL code
        public static void ParseCOOL(string code)
        {
            bool firstPercent;
            for (int i = 0; i < code.Split(new char[] { '\n' }).Length; i++)
            {
                string line = code.Split(new char[] { '\n' })[i].Replace("\r", "").Replace("\t", " ").Trim();
                line = Regex.Replace(line, @"\s+", " ");
                if (skipIfStmt)
                {
                    if (doingPercent)
                    {
                        if (line == "stopif")
                        {
                            skipIfStmt = false;
                        }
                    }
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
                        if (line.Substring(2, 1) == "$" && isVariable(line.Substring(3).Replace(" ", "")))
                        {
                            // Print formatted string
                            string varcont;
                            variables.TryGetValue(line.Substring(3).Replace(" ", ""), out varcont);
                            FormattedPrint(varcont);
                        }
                        else
                        {
                            // Print formatted string
                            FormattedPrint(line.Substring(2));
                        }
                    }
                    else if (line.StartsWith("echo "))  // Echo
                    {
                        if (line.Substring(5, 1) == "$" && isVariable(line.Substring(6).Replace(" ", "")))
                        {
                            string varcont;
                            variables.TryGetValue(line.Substring(6).Replace(" ", ""), out varcont);
                            FormattedPrint(varcont);
                        }
                        else
                        {
                            pageData += line.Substring(5);
                        }
                    }
                    // Unlink deletes a file, use with caution!
                    else if (line.StartsWith("unlink "))
                    {
                        if (line.Substring(7, 1) == "$" && isVariable(line.Substring(8).Replace(" ", "")))
                        {
                            string varcont = null;
                            variables.TryGetValue(line.Substring(8).Replace(" ", ""), out varcont);
                            System.IO.File.Delete(System.IO.Path.Combine(Environment.CurrentDirectory, varcont));
                        }
                        else
                        {
                            System.IO.File.Delete(System.IO.Path.Combine(Environment.CurrentDirectory, line.Substring(7)));
                        }
                    }
                    else if (line.Substring(0, 1) == "$")
                    {
                        if (line.Contains("="))
                        {
                            if (isVariable(line.Substring(1).Split("=")[0].Replace(" ", "")))
                            {
                                variables.Remove(line.Substring(1).Split("=")[0].Replace(" ", ""));
                            }
                            variables.Add(line.Substring(1).Split("=")[0].Replace(" ", ""), line.Split("=")[1].TrimStart());
                        }
                    }
                    // Here comes the if statement...
                    else if (line.StartsWith("if"))
                    {
                        if (line.Contains("="))
                        {
                            string toCheck = line.Substring(3).Split("=")[0].TrimEnd();        // Just some stuff that makes
                                                                                               // it contain the first argument
                            if (line.Substring(3,1) == "$" && isVariable(line.Substring(4).Split("=")[0].Trim()))
                            {
                                string varcont;
                                variables.TryGetValue(line.Substring(4).Split("=")[0].TrimEnd(), out varcont);
                                toCheck = varcont;
                            }
                            string secondCheck = line.Substring(3).Split("=")[1].TrimStart();        // The thing to compare to
                            if (line.Split("=")[1].Trim().Substring(0, 1) == "$" && isVariable(line.Substring(4).Split("=")[1].Trim().Substring(1)))
                            {
                                string varcont;
                                variables.TryGetValue(line.Substring(4).Split("=")[1].Trim().Substring(1), out varcont);
                                secondCheck = varcont;
                            }
                            if (toCheck != secondCheck)
                            {
                                skipIfStmt = true;
                            }
                        }
                    }
                    else if (line.StartsWith("//"))
                    {
                    }
                    else if (line == "stopif")
                    {
                    }
                    else if (line.StartsWith("%>"))
                    {
                        doingPercent = false;
                    }
                    else { Error("Unknown statement " + line); }
                }
                else
                {
                    if (!firstPercent) pageData += line;
                }
            }
        }


        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            while (runServer)
            {
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

                if (System.IO.File.Exists(req.Url.AbsolutePath.Substring(1)))
                {
                    pageData = "";
                    ParseCOOL(System.IO.File.ReadAllText(req.Url.AbsolutePath.Substring(1)));
                    variables.Clear();
                }

                // Write the response info
                string disableSubmit = !runServer ? "disabled" : "";
                byte[] data = Encoding.UTF8.GetBytes(pageData);
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }


        public static void Main(string[] args)
        {
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
