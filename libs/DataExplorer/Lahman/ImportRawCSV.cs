using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MetaPopulator
{
    class ImportRawCSV
    {
        public static SqlConnection connection = null;

        public static void Import()
        {
            string dataDir = @"C:\Users\ez\Downloads\lahman2012-csv\";

            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
               @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");
            //SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder( 
            //  @"Server=MACBOOK-WIN8\SQLEXPRESS;Database=PanoramicData;Connection Timeout=30;Max Pool Size=200;Trusted_Connection=Yes;");


            connection = new SqlConnection(stringBuilder.ToString());
            connection.Open();

            string[] files = new string[] {
                "Master.csv, master",
                "AllstarFull.csv, allstar_full",
                "HallOfFame.csv, hall_of_fame",
                "Batting.csv, batting",
                "BattingPost.csv, batting_post",
                "Pitching.csv, pitching",
                "PitchingPost.csv, pitching_post",
                "Fielding.csv, fielding",
                "FieldingOF.csv, fielding_of",
                "FieldingPost.csv, fielding_post",
                "Salaries.csv, salaries",
                "AwardsPlayers.csv, awards_players",
                "AwardsSharePlayers.csv, awards_share_players",
                "Appearances.csv, apperances",
                "SchoolsPlayers.csv, schools_players",
                "TeamsFranchises.csv, team_franchises",
                "Schools.csv, schools",
                "Teams.csv, team"
            };

            foreach (var ff in files)
            {
                Console.WriteLine(ff);
                string[] fff = ff.Split(new string[] {", "}, StringSplitOptions.RemoveEmptyEntries);
                createAndLoadTableFromCSV(dataDir + fff[0], "baseball", fff[1]);
            }
        }

        private static void createAndLoadTableFromCSV(string filenName, string schmemaName, string tableName)
        {
            Stream stream = File.OpenRead(filenName);
            List<string> columnNames = new List<string>();
            Dictionary<string, List<string>> samples = new Dictionary<string,  List<string>>();

            using (StreamReader sr = new StreamReader(stream))
            {
                // first line is header infp
                String line = sr.ReadLine();
                columnNames = CSVHelper.CSVLineSplit(line);
                for (int i = 0; i < columnNames.Count; i++)
                {
                    string columnName = columnNames[i];
                    if (Char.IsDigit(columnName.ToCharArray()[0]))
                    {
                        columnNames.RemoveAt(i); 
                        columnNames.Insert(i, "f_" + columnName);
                    }
                }

                int sampleCount = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    List<string> entries = CSVHelper.CSVLineSplit(line);
                    for (int i = 0; i < entries.Count; i++)
                    {
                        string entry = entries[i];
                        if (!samples.ContainsKey(columnNames[i]))
                        {
                            samples.Add(columnNames[i], new List<string>());
                        }
                        samples[columnNames[i]].Add(entry);
                    }

                    sampleCount++;
                    if (sampleCount > 1000)
                    {
                        break;
                    }
                }
            }
            stream.Close();
            

            // create sql query
            List<string> fields = new List<string>();
            List<string> dataTypes = new List<string>();
            fields.Add("id int");
            for (int i = 0; i < columnNames.Count; i++)
            {
                string columnName = columnNames[i];

                string dataType = "nvarchar(255)";
                if (isInt(samples[columnName]))
                {
                    dataType = "int";
                }
                else if (isDouble(samples[columnName]))
                {
                     dataType = "float";
                }
                else if (isDate(samples[columnName]))
                {
                     dataType = "date";
                }
                dataTypes.Add(dataType);
                fields.Add(columnName + " " + dataType);
            }
            
            dropConstraintsAndTable(schmemaName, schmemaName + "." + tableName);
            ExecuteNonQuery(
                "CREATE TABLE " + schmemaName + "." + tableName + " (" +
                string.Join(",\n", fields) +
                ")");

            // load data
            stream = File.OpenRead(filenName);

            using (StreamReader sr = new StreamReader(stream))
            {
                // first line is header infp
                String line = sr.ReadLine();
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "";

                int id = 1;
                while ((line = sr.ReadLine()) != null)
                {
                    cmd.CommandText +=
                        string.Format("INSERT INTO " + schmemaName + "." + tableName + " (\n" +
                             "id,\n" +
                             string.Join(",\n", columnNames) + ")\n" +
                             "VALUES (\n" +
                              "@id_{0},\n" +
                             string.Join(",\n", columnNames.Select(s => "@" + s + "_{0}")) + ")", id);

                    List<string> entries = CSVHelper.CSVLineSplit(line);
                    addSqlParam(cmd, "id", id, id);
                    for (int i = 0; i < entries.Count; i++)
                    {
                        string entry = entries[i];
                        object value = entry;
                        if (entry == "")
                        {
                            value = null;
                        }
                        else
                        {
                            if (dataTypes[i] == "int")
                            {
                                int outInt = 0;
                                if (int.TryParse(entry, out outInt))
                                {
                                    value = outInt;//int.Parse(entry);
                                }
                                else
                                {
                                    if (entry == "�")
                                    {
                                        entry = "0";
                                    }
                                    value = (int) Math.Round(double.Parse(entry), MidpointRounding.AwayFromZero);
                                }
                            }
                            else if (dataTypes[i] == "double")
                            {
                                value = double.Parse(entry);
                            }
                            else if (dataTypes[i] == "date")
                            {
                                value = DateTime.ParseExact(entry, "M/d/yyyy", null, DateTimeStyles.None);
                            }
                        }
                        addSqlParam(cmd, columnNames[i], id, value);
                    }
                    id = id + 1;

                    if (id % 20 == 0)
                    {
                        cmd.ExecuteNonQuery();
                        cmd = connection.CreateCommand();
                    }
                }
                if (cmd.CommandText != "")
                {
                    cmd.ExecuteNonQuery();
                }
            }
            stream.Close();
        }

        private static bool isInt(List<string> samples)
        {
            bool isInt = true;
            foreach (var sample in samples)
            {
                double res = 0.0;
                if (!double.TryParse(sample, out res) && sample != "")
                {
                    isInt = false;
                    break;
                }
                else
                {
                    if (!(res % 1 == 0))
                    {
                        isInt = false;
                        break;
                    }
                }
            }
            return isInt && !samples.All(s => s == "");
        }

        private static bool isDouble(List<string> samples)
        {
            bool isNumber = true;
            foreach (var sample in samples)
            {
                double res = 0.0;
                if (!double.TryParse(sample, out res) && sample != "")
                {
                    isNumber = false;
                    break;
                }
            }
            return isNumber && !samples.All(s => s == "");
        }

        private static bool isDate(List<string> samples)
        {
            bool isDate = true;
            foreach (var sample in samples)
            {
                DateTime res = DateTime.Now;
                if (!DateTime.TryParseExact(sample, "M/d/yyyy", null, DateTimeStyles.None, out res) && sample != "")
                {
                    isDate = false;
                    break;
                }
            }
            return isDate && !samples.All(s => s == "");
        }

        private static void ExecuteNonQuery(string query)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
        }
        private static void RunAndExecute(string query)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            SqlDataReader re = cmd.ExecuteReader();
            string statements = "";
            while (re.Read())
            {
                statements += re.GetString(0);
            }
            re.Close();

            if (statements != "")
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = statements;
                cmd.ExecuteNonQuery();
            }
        }

        private static void dropConstraintsAndTable(string schema, string table)
        {
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText = "SELECT " +
                            "'ALTER TABLE " + schema + ".' + OBJECT_NAME(parent_object_id) + " +
                            "' DROP CONSTRAINT ' + name " +
                            "FROM sys.foreign_keys " +
                            "WHERE referenced_object_id = object_id('" + table + "')";

            SqlDataReader re = cmd.ExecuteReader();
            string alterStatements = "";
            while (re.Read())
            {
                alterStatements += re.GetString(0) + ";";
            }
            re.Close();

            if (alterStatements != "")
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = alterStatements;
                cmd.ExecuteNonQuery();
            }
            cmd = connection.CreateCommand();
            cmd.CommandText = "drop table " + table;
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(table + " does not exist");
            }
        }

        private static void addSqlParam(SqlCommand cmd, string paramName, int id, object param, bool insertZeroInstedOfNull = false)
        {
            if (param == null)
            {
                if (insertZeroInstedOfNull)
                {
                    cmd.Parameters.Add(new SqlParameter("@" + paramName + "_" + id, float.Parse("0")));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@" + paramName + "_" + id, DBNull.Value));
                }
            }
            else
            {
                cmd.Parameters.Add(new SqlParameter("@" + paramName + "_" + id, param));
            }
        }
    }

    public class CSVHelper
    {
        public static List<string> CSVLineSplit(string line)
        {
            bool open = false;
            List<string> words = new List<string>();
            string word = "";
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ',' && !open)
                {
                    words.Add(word);
                    word = "";
                }
                else if (line[i] == '"' && !open)
                {
                    open = true;
                    //words.Add(word);
                    //word = "";
                }
                else if (line[i] == '"' && open)
                {
                    open = false;
                }
                else
                {
                    word += line[i];
                }
            }
            words.Add(word);
            return words;
        }
    }
}
