using System.Configuration;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicDataDBConnector
{
    public class DatabaseManager
    {
        private static string _lastErrorMessage = "";
        public static event EventHandler<string> ErrorMessageChanged;

        //private static string _defaultWorkTopDatabaseName = "WT_PUB_Rosemary's Web (Local Only)";
        //private static string _worktopDBConnectionString = @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=" + _defaultWorkTopDatabaseName + ";User ID=test;Password=P@55word;Connection Timeout=30;Max Pool Size=200;";
        
        public static bool Verbose { get; set; }

        private static IEnumerable<TableInfo> _tableInfos = null;
        private static IEnumerable<FieldAlias> _fieldAliases = null;

        public static IEnumerable<TableInfo> GetTableInfos()
        {
            try
            {
                if (_tableInfos == null)
                {
                    panoramicdataEntities db = new panoramicdataEntities();
                    _tableInfos = db.TableInfoes;
                }
                _tableInfos.Count();
                clearException();
                return _tableInfos;
            }
            catch (Exception e)
            {
                handleException(e);
                return null;
            }
        }

        public static IEnumerable<FieldAlias> GetFieldAliases(string[] needleParts)
        {
            try
            {
                if (_fieldAliases == null)
                {
                    panoramicdataEntities db = new panoramicdataEntities();
                    _fieldAliases = db.FieldAlias;
                }

                IEnumerable<FieldAlias> result = _fieldAliases;

                foreach (var needle in needleParts)
                {
                    result = from fa in result
                             where fa.Alias.Contains(needle)
                             select fa;
                }
                result.Count();
                clearException();
                return result;
            }
            catch (Exception e)
            {
                handleException(e);
                return new List<FieldAlias>();
            }
        }

        public static List<List<object>> ExecuteQuery(string schema, string query)
        {
            try
            {
                DateTime start = DateTime.Now;
                var connectionString = ConfigurationManager.ConnectionStrings["panoramicdata"].ConnectionString;
                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = query;
                SqlDataReader myReader = null;
                try
                {
                    myReader = cmd.ExecuteReader();
                }
                catch (Exception e)
                {
                    clearException();
                    return new List<List<object>>();
                }

                List<List<object>> ret = new List<List<object>>();
                int count = 0;
                while (myReader.Read())
                {
                    List<object> row = new List<object>();

                    for (int field = 0; field < myReader.FieldCount; field++)
                    {
                        row.Add(myReader[myReader.GetName(field)]);
                    }
                    ret.Add(row);

                    count++;
                }
                myReader.Close();

                TimeSpan duration = DateTime.Now - start;

                if (Verbose)
                {
                    Console.WriteLine(query);
                    Console.WriteLine("Returned " + count + " rows in " + duration.ToString(@"mm\:ss\:fff"));
                    Console.WriteLine();
                }
                clearException();
                return ret;
            }
            catch (Exception e)
            {
                handleException(e);
                return new List<List<object>>();
            }
        }

        public static TableInfo GetTableInfo(string schema, string name)
        {
            try
            {
                IEnumerable<TableInfo> infos = GetTableInfos();
                foreach (var ti in infos)
                {
                    if (ti.Name == name && ti.SchemaName == schema)
                    {
                        clearException();
                        return ti;
                    }
                }
                clearException();
                return null;
            }
            catch (Exception e)
            {
                handleException(e);
                return null;
            }
        }

        public static FieldInfo GetFieldInfo(string schema, string tableName, string fieldName)
        {
            try
            {
                IEnumerable<TableInfo> infos = GetTableInfos();
                foreach (var ti in infos)
                {
                    if (ti.Name == tableName && ti.SchemaName == schema)
                    {
                        foreach (var f in ti.FieldInfos)
                        {
                            if (f.Name == fieldName)
                            {
                                clearException();
                                return f;
                            }
                        }
                    }
                }
                clearException();
                return null;
            }
            catch (Exception e)
            {
                handleException(e);
                return null;
            }
        }

        private static void handleException(Exception e)
        {
            _lastErrorMessage = e.Message.Trim();

            if (ErrorMessageChanged != null)
            {
                ErrorMessageChanged(null, _lastErrorMessage);
            }
        }

        private static void clearException()
        {
            _lastErrorMessage = "";

            if (ErrorMessageChanged != null)
            {
                ErrorMessageChanged(null, _lastErrorMessage);
            }
        }
    }
}
