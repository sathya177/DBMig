using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DbMigs.Models
{
    public class MonitorInfo
    {
        public string ServerName { get; set; }
        public decimal OptimizerMemoery { get; set; }
        public decimal SQLCacheMemory { get; set; }
        public List<DB> Databases { get; set; }
        public List<UsedDBLog> UsedlogSpace { get; set; }
    }
    public class UsedDBLog
    {
        public string Database { get; set; }
        public decimal LogSize { get; set; }
        public decimal LogSpace { get; set; }
    }
    public class DB
    {
        public string Name { get; set; }
        public decimal TotlDatabasefileSize { get; set; }

        public decimal SpaceUsedbyDataObjects { get; set; }

       

        public decimal DatabaseFreeSpace { get; set; }

        public decimal DatabaseUsedSpace { get; set; }
    }
}