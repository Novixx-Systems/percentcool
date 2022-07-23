using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace percentCool
{
    internal class Program
    {
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
            "    <p>HTTP 500 Server Error</p>" +
            "  </body>" +
            "</html>";
        public static void Error(string errorReason)
        {
            pageData = "percentCOOL error: " + errorReason;
        }
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
            bool firstPercent = false;
            foreach (string line in code.Split(new char[] { '\n' }))
            {
                firstPercent = false;
                if (line.StartsWith("<%cool"))
                {
                    doingPercent = true;
                    firstPercent = true;
                }
                if (doingPercent && !firstPercent)
                {
                    if (line.StartsWith("$="))
                    {
                        if (line.Substring(2, 1) == "$" && isVariable(line.Substring(3).Replace(" ", "").Replace("\r", "")))
                        {
                            string varcont = null;
                            variables.TryGetValue(line.Substring(3).Replace(" ", "").Replace("\r", ""), out varcont);
                            FormattedPrint(varcont);
                        }
                        else
                        {
                            FormattedPrint(line.Substring(2));
                        }
                    }
                    else if (line.StartsWith("echo "))
                    {
                        if (line.Substring(5, 1) == "$" && isVariable(line.Substring(6).Replace(" ", "").Replace("\r", "")))
                        {
                            string varcont = null;
                            variables.TryGetValue(line.Substring(6).Replace(" ", "").Replace("\r", ""), out varcont);
                            FormattedPrint(varcont);
                        }
                        else
                        {
                            pageData += line.Substring(5);
                        }
                    }
                    else if (line.Substring(0, 1) == "$")
                    {
                        if (line.Contains("="))
                        {
                            if (isVariable(line.Substring(1).Split("=")[0].Replace(" ", "").Replace("\r", "")))
                            {
                                variables.Remove(line.Substring(1).Split("=")[0].Replace(" ", "").Replace("\r", ""));
                            }
                            variables.Add(line.Substring(1).Split("=")[0].Replace(" ", ""), line.Split("=")[1]);
                        }
                    }
                    else if (line.StartsWith("//"))
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
                    pageData = null;
                    ParseCOOL(System.IO.File.ReadAllText(req.Url.AbsolutePath.Substring(1)));
                    variables.Clear();
                }

                // Write the response info
                string disableSubmit = !runServer ? "disabled" : "";
                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews, disableSubmit));
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
