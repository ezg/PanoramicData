using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace TitanicPopulator
{    
    class DBPopulator
    {
        public static SqlConnection connection = null;

        public static void Populate()
        {
            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
               @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");
            
            connection = new SqlConnection(stringBuilder.ToString());
            connection.Open();

            Stream stream = File.Open(@"C:\ez_projects\HumBub\starPad SDK\Apps\DataExplorer\TitanicPopulator\data\titanic3.csv", FileMode.Open);

            if (true)
            {
                //createTables();
                createMetaTables();
                return;
            }

            SqlCommand passengerCmd = connection.CreateCommand();

            int nextPassenger = 1;
            int currentBatchCount = 0;

            Dictionary<string, string> locationMap = new Dictionary<string, string>();

            // User, Msg, Retweet Count, Date, Hashtag list, Media list, User mentions, URLs, lat, long
            using (StreamReader sr = new StreamReader(stream))
            {
                String line = sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    List<string> entries = CSVHelper.CSVLineSplit(line);
                    int id = nextPassenger;
                    nextPassenger = nextPassenger + 1;

                    if (entries[2] == "")
                    {
                        continue;
                    }
                    passengerCmd.CommandText += string.Format(
                        "INSERT INTO titanic.passenger (" +
                               "id," +
                               "passenger_class,\n" +
                               "survived, \n" +
                               "name,\n" +
                               "sex,\n" +
                               "age,\n" +
                               "siblings_spouses,\n" +
                               "parents_children,\n" +
                               "ticket,\n" +
                               "fare,\n" +
                               "cabin,\n" +
                               "embarked,\n" +
                               "boat,\n" +
                               "body,\n" +
                               "home,\n" +
                               "destination) " +
                        "VALUES (" +
                               "@id_{0}," +
                               "@passenger_class_{0},\n" +
                               "@survived_{0},\n" +
                               "@name_{0},\n" +
                               "@sex_{0},\n" +
                               "@age_{0},\n" +
                               "@siblings_spouses_{0},\n" +
                               "@parents_children_{0},\n" +
                               "@ticket_{0},\n" +
                               "@fare_{0},\n" +
                               "@cabin_{0},\n" +
                               "@embarked_{0},\n" +
                               "@boat_{0},\n" +
                               "@body_{0},\n" +
                               "@home_{0},\n" +
                               "@destination_{0})", id);


                    addSqlParam(passengerCmd, "id", id, id);
                    addSqlParam(passengerCmd, "passenger_class", id, int.Parse(entries[0]));
                    addSqlParam(passengerCmd, "survived", id, int.Parse(entries[1]));
                    addSqlParam(passengerCmd, "name", id, entries[2]);
                    addSqlParam(passengerCmd, "sex", id, entries[3]);
                    if (entries[4] == "")
                    {
                        addSqlParam(passengerCmd, "age", id, null);
                    }
                    else
                    {
                        addSqlParam(passengerCmd, "age", id, float.Parse(entries[4]));
                    }
                    addSqlParam(passengerCmd, "siblings_spouses", id, int.Parse(entries[5]));
                    addSqlParam(passengerCmd, "parents_children", id, int.Parse(entries[6]));
                    addSqlParam(passengerCmd, "ticket", id, entries[7]);
                    if (entries[8] == "")
                    {
                        addSqlParam(passengerCmd, "fare", id, null);
                    }
                    else
                    {
                        addSqlParam(passengerCmd, "fare", id, float.Parse(entries[8]));
                    }
                    addSqlParam(passengerCmd, "cabin", id, entries[9]);

                    string town = "";
                    if (entries[10] == "Q")
                    {
                        town = "Cobh, Cork, Ireland";
                    }
                    else if (entries[10] == "C")
                    {
                        town = "Cherbourg, France";
                    }
                    else if (entries[10] == "S")
                    {
                        town = "Southampton, United Kingdom";
                    }
                    town += " (" + getLocation(town, locationMap) + ")";

                    addSqlParam(passengerCmd, "embarked", id, town);
                    addSqlParam(passengerCmd, "boat", id, entries[11]);
                    addSqlParam(passengerCmd, "body", id, entries[12]);

                    string[] towns = entries[13].Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                    if (towns.Length == 1)
                    {
                        string loc = getLocation(towns[0].Trim(), locationMap);
                        if (loc == "")
                        {
                            addSqlParam(passengerCmd, "home", id, null);
                        }
                        else
                        {
                            addSqlParam(passengerCmd, "home", id, towns[0].Trim() + " (" + loc + ")");
                        }
                        addSqlParam(passengerCmd, "destination", id, null);
                    }
                    else if (towns.Length == 2)
                    {
                        string loc = getLocation(towns[0].Trim(), locationMap);
                        if (loc == "")
                        {
                            addSqlParam(passengerCmd, "home", id, null);
                        }
                        else
                        {
                            addSqlParam(passengerCmd, "home", id, towns[0].Trim() + " (" + loc + ")");
                        }

                        loc = getLocation(towns[1].Trim(), locationMap);
                        if (loc == "")
                        {
                            addSqlParam(passengerCmd, "destination", id, null);
                        }
                        else
                        {
                            addSqlParam(passengerCmd, "destination", id, towns[1].Trim() + " (" + loc + ")");
                        }
                    }
                    else
                    {
                        addSqlParam(passengerCmd, "home", id, null);
                        addSqlParam(passengerCmd, "destination", id, null);
                    }

                    currentBatchCount++;
                    if (currentBatchCount >= 100)
                    {
                        passengerCmd.ExecuteNonQuery();
                        currentBatchCount = 0;
                        passengerCmd = connection.CreateCommand();
                    }
                }
                if (passengerCmd.CommandText != "")
                {
                    passengerCmd.ExecuteNonQuery();
                }
            }
        }

        private static string getLocation(string town, Dictionary<string, string> map)
        {
            if (map.ContainsKey(town))
            {
                return map[town];
            }
            else
            {
                string loc = MapApi.MapAPI.GeoCode(town);
                map.Add(town, loc);
                return loc;
            }
        }

        private static void createMetaTables()
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "delete from meta.field_alias where id between 6000 and 7000;" +
                "delete from meta.field_info where id between 6000 and 7000;" +
                "delete from meta.table_alias where id between 6000 and 7000;" +
                "delete from meta.table_dependency where id between 6000 and 7000;" +
                "delete from meta.table_info where id between 6000 and 7000;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmd.ExecuteNonQuery();

            Dictionary<string, int> tables = new Dictionary<string, int>();

            // insert records 
            int nextId = 6000;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name from information_schema.tables where table_schema ='titanic'";
            SqlDataReader re = cmd.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', 'titanic');\n";

                insertStatements += "insert into meta.table_alias (id, priority, alias, table_id) values (" +
                    (nextId - 1) + ", 0, '" + re.GetString(0) + "', " + (nextId - 1) + ");\n";

                tables.Add(re.GetString(0), nextId - 1);
            }
            re.Close();

            if (insertStatements != "")
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = insertStatements;
                cmd.ExecuteNonQuery();
            }


            
            Dictionary<string, int> fields = new Dictionary<string, int>();

            // insert records 
            nextId = 6000;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='titanic'";
            re = cmd.ExecuteReader();
            insertStatements = "";
            string updateStatements = "";
            while (re.Read())
            {
                string visible = re.GetString(1).EndsWith("id") ? "0" : "1";
                insertStatements += "insert into meta.field_info (id, name, visible, data_type, table_id) values (" +
                    nextId++ + "," +
                    "'" + re.GetString(1) + "'," +
                    visible + "," +
                    "'" + re.GetString(2) + "'," +
                    "" + tables[re.GetString(0)] + "" +
                    ");";

                if (re.GetString(1) == "id")
                {
                    updateStatements += "update meta.table_info set pk_field_info = " + (nextId - 1) + " where id = " + tables[re.GetString(0)] + ";";
                }

                insertStatements += "insert into meta.field_alias (id, priority, alias, field_id) values (" +
                    (nextId - 1) + ", 0, '" + re.GetString(1) + "', " + (nextId - 1) + ");";

                fields.Add(re.GetString(0) + "_" + re.GetString(1), (nextId - 1));
            }
            re.Close();

            if (insertStatements != "")
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = insertStatements;
                cmd.ExecuteNonQuery();
            }
            if (updateStatements != "")
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = updateStatements;
                cmd.ExecuteNonQuery();
            }

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NOT NULL;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info WITH CHECK ADD " +
                "CONSTRAINT FK_meta_table_info_pk_field_info FOREIGN KEY (pk_field_info) REFERENCES meta.field_info (id) ON DELETE NO ACTION;";
            cmd.ExecuteNonQuery();

            // get all foreign key releations for a table
            nextId = 6000;
            foreach (var table_name in tables.Keys)
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn " +
                    "from sys.foreign_key_columns as fk " +
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id " +
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = 12) " +
                    "order by TableWithForeignKey";
                List<KeyValuePair<string, string>> relations = new List<KeyValuePair<string, string>>();
                re = cmd.ExecuteReader();

                while (re.Read())
                {
                    relations.Add(new KeyValuePair<string, string>(re.GetString(0), re.GetString(1)));
                }
                re.Close();

                foreach (var relation in relations)
                {
                    cmd = connection.CreateCommand();
                    cmd.CommandText = "insert into meta.table_dependency (id, from_table_id, to_table_id, from_field_id, to_field_id, cardinality, relationship_name) values (" +
                        nextId++ + ", " +
                        tables[table_name] + ", " +
                        tables[relation.Key] + ", " +
                        fields[table_name + "_id"] + ", " +
                        fields[relation.Key + "_" + relation.Value] + ", " +
                        "'MANY', " +
                        "'')";
                    cmd.ExecuteNonQuery();

                    cmd = connection.CreateCommand();
                    cmd.CommandText = "insert into meta.table_dependency (id, from_table_id, to_table_id, from_field_id, to_field_id, cardinality, relationship_name) values (" +
                        nextId++ + ", " +
                        tables[relation.Key] + ", " +
                        tables[table_name] + ", " +
                        fields[relation.Key + "_" + relation.Value] + ", " +
                        fields[table_name + "_id"] + ", " +
                        "'ONE', " +
                        "'')";
                    cmd.ExecuteNonQuery();

                }
            }
        }

        private static void createTables()
        {
            dropConstraintsAndTable("titanic", "titanic.passenger");
            ExecuteNonQuery(
               "CREATE TABLE titanic.passenger (\n" +
               "id int NOT NULL,\n" +
               "name nvarchar(255) NOT NULL,\n" +
               "passenger_class int NOT NULL,\n" + 
               "survived int NOT NULL,\n" +
               "sex nvarchar(255) NOT NULL,\n" +
               "age float,\n" +
               "siblings_spouses int NOT NULL,\n" +
               "parents_children int NOT NULL,\n" +
               "ticket nvarchar(255) NOT NULL,\n" +
               "fare float,\n" +
               "cabin nvarchar(255),\n" +
               "embarked nvarchar(255),\n" +
               "boat nvarchar(255),\n" +
               "body nvarchar(255),\n" +
               "home nvarchar(255),\n" +
               "destination nvarchar(255),\n" +
               "CONSTRAINT PK_titanic_passeger PRIMARY KEY (id))"
            );
        }

        private static List<List<object>> ExecuteQuery(string query, params object[] parameters)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i+=2)
                {
                    cmd.Parameters.Add(new SqlParameter(parameters[i].ToString(), parameters[i+1]));
                }
            }
            SqlDataReader myReader = cmd.ExecuteReader();

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

            return ret;
        }

        private static void ExecuteNonQuery(string query)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
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
