using DbMigs.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Newtonsoft.Json;
using DbMigs.Auth;

namespace DbMigs.Controllers
{
    public class RunController : ApiController
    {
       
        //[BasicAuthenticationAttribute]
        public List<db_migration_history> Get(string action, string database, int Id, string server)
        {
            var envs = JsonConvert.DeserializeObject<Environments>(File.ReadAllText(ConfigurationManager.AppSettings["envPath"].ToString()));
            var dbScriptPath = envs.environments.Where(e => e.server == server).Select(s => s.scriptPath).FirstOrDefault();
            var dbScriptPath1 = ConfigurationManager.AppSettings["dbScriptPath1"].ToString();
            var selectedenvInfo = envs.environments.Where(e => e.server == server).Select(d => d.Databases).FirstOrDefault();
            var dbInfo = selectedenvInfo.Where(d => d.name == database).Select(d=>d).FirstOrDefault();
            
            if (action == "run")
            {
                List<string> filesExecuted = new List<string>();


                using (DbMigrationEntities db = new DbMigrationEntities())
                {
                    db.Database.Connection.ConnectionString = "server=" + server + "; database=" + database + ";" + "uid="+dbInfo.username + ";password="+dbInfo.password + "; Connect Timeout=0; Max Pool Size=5000";
                    
                    var dbMigrationHistory = db.db_migration_history.ToList();
                    Double latestVersion = Convert.ToDouble(dbMigrationHistory.OrderByDescending(c => c.installed_on).Select(v => v.version).FirstOrDefault());
                    Console.WriteLine(latestVersion);
                    var scriptExecutedNames = dbMigrationHistory.Select(c => c.script).ToList();
                    List<string> scriptColl = new List<string>();
                    foreach (var item in scriptExecutedNames)
                    {
                       
                        scriptColl.Add(item);
                    }
                    // Get All Files from folder
                    DirectoryInfo d = new DirectoryInfo(dbScriptPath);//Assuming Test is your Folder
                    FileInfo[] Files = d.GetFiles("*.sql", SearchOption.AllDirectories); //Getting Text files

                    string str = "";
                    foreach (FileInfo file in Files)
                    {
                        Console.WriteLine(file.Name);
                        if (scriptExecutedNames.Contains(file.FullName.Replace(dbScriptPath1, "")))
                        {
                            continue;
                        }
                        else
                        {
                            if (latestVersion == 0.0)
                                latestVersion = 1.0;

                            latestVersion = latestVersion + 0.1;
                            ProcessDBScript(file.FullName, "Data source=" + server +";initial catalog=" + database + ";persist security info=True;" + "uid="+dbInfo.username+"; password = "+dbInfo.password + ";" , file.FullName.Replace(dbScriptPath, ""), latestVersion.ToString(), Id);
                            filesExecuted.Add(file.FullName.Replace(dbScriptPath, "")); ;

                        }

                    }

                }

            }
            using (var context = new DbMigrationEntities())
            {
                context.Database.Connection.ConnectionString = "server="+ server +";database=" + database + ";" + "uid=" + dbInfo.username + "; password = " + dbInfo.password + "; Connect Timeout=0; Max Pool Size=5000";
                return context.db_migration_history.ToList();
            }
        }

        public void ProcessDBScript(string fileName, string connectionString, string filewithoutPath, string version, int Id)
        {
            bool status = true;
            var dbScriptPath1 = ConfigurationManager.AppSettings["dbScriptPath1"].ToString();
            string error = string.Empty;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                try
                {
                    cn.Open();
                    FileInfo file = new FileInfo(fileName);
                    string script = file.OpenText().ReadToEnd();
                    string[] splitChar = { "\r\nGO\r\n" };
                    var sqlLines = script.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                    int res = 0;
                    SqlCommand cmd = null;
                    foreach (var query in sqlLines)
                    {
                        cmd = new SqlCommand(query, cn)
                        {
                            CommandTimeout = 5400
                        };
                        res = cmd.ExecuteNonQuery();
                    }
                    cn.Close();
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

                using (var context = new DbMigrationEntities())
                {
                    context.Database.Connection.ConnectionString = connectionString;
                    var migration = new db_migration_history()
                    {
                        script = filewithoutPath.Replace(dbScriptPath1,""),
                        installed_on = DateTime.Now,
                        installed_by = "cape",
                        success = status,
                        version = version,
                        Error = error,
                        ExecutionId = Id

                    };
                    context.db_migration_history.Add(migration);

                    context.SaveChanges();
                }
            }
        }

        
    }
}
