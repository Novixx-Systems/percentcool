using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace percentCool.Utilities
{
    internal class Request
    {
        public Dictionary<string, string> variables = new();
        public Dictionary<string, List<string>> arrays = new();
        public List<string> vs1 = new();
        public Dictionary<string, Cookie> cookies = new();
        public int randMax = 10;
        public bool doingPercent = false;
        public string where = "";
        public string server;
        public string database;
        public string uid;
        public string password;
        public MySqlConnection connection;

        public bool skipIfStmtElse = false;
        public bool skipElseStmt = false;
        public bool skipElseStmtB = false;
    }
}
