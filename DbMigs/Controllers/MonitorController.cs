using DbMigs.Auth;
using DbMigs.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DbMigs.Controllers
{
    public class MonitorController : ApiController
    {
        
        [BasicAuthenticationAttribute]
        public MonitorInfo Get(string server)
        {
            MonitorInfo monitorInfo = new MonitorInfo();
            monitorInfo.Databases = new List<DB>();
            var envs = JsonConvert.DeserializeObject<Environments>(File.ReadAllText(ConfigurationManager.AppSettings["envPath"].ToString()));
            var selectedenvInfo = envs.environments.Where(e => e.server == server).Select(d => d.Databases).FirstOrDefault();
            foreach(var item in selectedenvInfo)
            {   
                string connectionString = "Data source=" + server + ";initial catalog=" + item.name + ";persist security info=True;" + "uid=" + item.username + "; password = " + item.password + ";";
                DB dbase = new DB();
                decimal optMemory, sqcacheMemory;
                List<UsedDBLog> usdDbLogs;
                ProcessDBScript(server, connectionString, dbase,out optMemory, out sqcacheMemory, out usdDbLogs);
                dbase.Name = item.name;
                monitorInfo.Databases.Add(dbase);
                monitorInfo.OptimizerMemoery = optMemory;
                monitorInfo.SQLCacheMemory = sqcacheMemory;
                monitorInfo.UsedlogSpace = usdDbLogs;
                monitorInfo.ServerName = server;
            }
            
            return monitorInfo;
        }

        public void ProcessDBScript(string server, string connectionString, DB dbase, out decimal optMemory, out decimal sqlcacheMemory, out List<UsedDBLog> usdDbLogs)
        {
            bool status = true;
            optMemory = 0;
            sqlcacheMemory = 0;
            usdDbLogs = new List<UsedDBLog>();
            var monitorQueryPath = ConfigurationManager.AppSettings["monitorQuery"].ToString();
            
            string error = string.Empty;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                try
                {
                    
                  
                    // Get All Files from folder
                    DirectoryInfo d = new DirectoryInfo(monitorQueryPath);//Assuming Test is your Folder
                    FileInfo[] Files = d.GetFiles("*.txt", SearchOption.AllDirectories); //Getting Text files
                    foreach(var file in Files)
                    {
                        if (file.Name.Contains("DBFileSize"))
                        {
                            cn.Open();
                            string script = file.OpenText().ReadToEnd();
                            string[] splitChar = { "\r\nGO\r\n" };
                            var sqlLines = script.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                            SqlCommand cmd = null;
                            foreach (var query in sqlLines)
                            {
                                cmd = new SqlCommand(query, cn)
                                {
                                    CommandTimeout = 5400
                                };
                                SqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    dbase.TotlDatabasefileSize = Convert.ToDecimal(reader["SizeInGB"]);
                                }
                            }
                            cn.Close();
                        }
                        else if (file.Name.Contains("DBSpace"))
                        {
                            cn.Open();
                            string script = file.OpenText().ReadToEnd();
                            string[] splitChar = { "\r\nGO\r\n" };
                            var sqlLines = script.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                            SqlCommand cmd = null;
                            foreach (var query in sqlLines)
                            {
                                cmd = new SqlCommand(query, cn)
                                {
                                    CommandTimeout = 5400
                                };
                                SqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    dbase.SpaceUsedbyDataObjects = Convert.ToDecimal(reader["Allocated_GB"]);
                                }
                            }
                            cn.Close();

                        }
                        else if (file.Name.Contains("OptMemory"))
                        {
                            cn.Open();
                            string script = file.OpenText().ReadToEnd();
                            string[] splitChar = { "\r\nGO\r\n" };
                            var sqlLines = script.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                            SqlCommand cmd = null;
                            foreach (var query in sqlLines)
                            {
                                cmd = new SqlCommand(query, cn)
                                {
                                    CommandTimeout = 5400
                                };
                                SqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    optMemory = Convert.ToDecimal(reader["cntr_value"]);
                                }
                            }
                            cn.Close();

                        }
                        else if (file.Name.Contains("SQLcacheMemory"))
                        {
                            cn.Open();
                            string script = file.OpenText().ReadToEnd();
                            string[] splitChar = { "\r\nGO\r\n" };
                            var sqlLines = script.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                            SqlCommand cmd = null;
                            foreach (var query in sqlLines)
                            {
                                cmd = new SqlCommand(query, cn)
                                {
                                    CommandTimeout = 5400
                                };
                                SqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    sqlcacheMemory = Convert.ToDecimal(reader["cntr_value"]);
                                }
                            }
                            cn.Close();

                        }
                        else if (file.Name.Contains("pcdbfreespace"))
                        {
                            cn.Open();
                            string script = file.OpenText().ReadToEnd();
                            string[] splitChar = { "\r\nGO\r\n" };
                            var sqlLines = script.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                            SqlCommand cmd = null;
                            foreach (var query in sqlLines)
                            {
                                cmd = new SqlCommand(query, cn)
                                {
                                    CommandTimeout = 5400
                                };
                                SqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    dbase.DatabaseFreeSpace = Convert.ToDecimal(reader["free_space_pct"]);
                                }
                            }
                            cn.Close();
                        }
                        else if (file.Name.Contains("pcdbusedspace"))
                        {
                            cn.Open();
                            string script = file.OpenText().ReadToEnd();
                            string[] splitChar = { "\r\nGO\r\n" };
                            var sqlLines = script.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                            SqlCommand cmd = null;
                            foreach (var query in sqlLines)
                            {
                                cmd = new SqlCommand(query, cn)
                                {
                                    CommandTimeout = 5400
                                };
                                SqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    dbase.DatabaseUsedSpace = Convert.ToDecimal(reader["PctFilled"]);
                                }
                            }
                            cn.Close();
                        }
                        else if (file.Name.Contains("pclogSpace"))
                        {
                            cn.Open();
                            string script = file.OpenText().ReadToEnd();
                            string[] splitChar = { "\r\nGO\r\n" };
                            var sqlLines = script.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                            SqlCommand cmd = null;
                            foreach (var query in sqlLines)
                            {
                                cmd = new SqlCommand(query, cn)
                                {
                                    CommandTimeout = 5400
                                };
                                SqlDataReader reader = cmd.ExecuteReader();
                                
                                while (reader.Read())
                                {
                                    usdDbLogs.Add(new UsedDBLog()
                                    {
                                        Database = Convert.ToString(reader["Database_Name"]),
                                        LogSize = Convert.ToDecimal(reader["Log_Size"]),
                                        LogSpace = Convert.ToDecimal(reader["Log_Space"])
                                    });

                                   
                                }
                            }
                            cn.Close();
                        }

                    }
                }
                catch (Exception ex)
                {
                    status = false;
                    error = ex.Message.Substring(0, 48);
                }
                finally
                {
                    cn.Close();
                }
            }
        }

       
    }
}