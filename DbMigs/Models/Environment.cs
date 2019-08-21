using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DbMigs.Models
{
    public class Database
    {
        public string name { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }

    public class Environment
    {
        public int id { get; set; }
        public string server { get; set; }

        public string scriptPath { get; set; }
        public List<Database> Databases { get; set; }
    }

    public class Environments
    {
        public List<Environment> environments { get; set; }
    }
}