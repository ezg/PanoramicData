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
using System.Xml.Linq;

namespace DruckerDSPopulator
{    
    class DBPopulatorCoffee
    {
        public static SqlConnection connection = null;

        public static void Populate()
        {
            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
               @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");
            
            
            connection = new SqlConnection(stringBuilder.ToString());
            connection.Open();

            if (false)
            {
                createMetaTables();
                return;
            }

            createTables();

            Stream stream = File.Open(@"C:\ez_projects\HumBub\starPad SDK\Apps\DataExplorer\DruckerDSPopulator\CoffeeData.csv", FileMode.Open);
            int nextSales = 0;

            Dictionary<string, string> states = new Dictionary<string, string>();

            SqlCommand cmd = null;
            cmd = connection.CreateCommand();

            using (StreamReader sr = new StreamReader(stream))
            {
                String line = null;

                while ((line = sr.ReadLine()) != null)
                {
                    int id = nextSales++;
                    if (id == 1)
                    {
                        continue;
                    }

                    List<string> entries = CSVHelper.CSVLineSplit(line);

                    cmd .CommandText += string.Format(
                        "INSERT INTO coffee.coffee_sales (" +
                                "id," +
                                "sales_date,\n" +
                                "sales,\n" +
                                "profit,\n" +
                                "sales_division,\n" +
                                "sales_state,\n" +
                                "product_group,\n" +
                                "product_name,\n" +
                                "product_specification) " +
                        "VALUES (" +
                                "@id_{0}," +
                                "@sales_date_{0},\n" +
                                "@sales_{0},\n" +
                                "@profit_{0},\n" +
                                "@sales_division_{0},\n" +
                                "@sales_state_{0},\n" +
                                "@product_group_{0},\n" +
                                "@product_name_{0},\n" +
                                "@product_specification_{0})", id);

                    Console.WriteLine(DateTime.Parse(entries[0]).ToString() + " / " + entries[0]);
                    addSqlParam(cmd, "id", id, id);
                    addSqlParam(cmd, "sales_date", id, DateTime.Parse(entries[0]));
                    addSqlParam(cmd, "sales", id, float.Parse(float.Parse(entries[1]).ToString("F2")));
                    addSqlParam(cmd, "profit", id, float.Parse(float.Parse(entries[2]).ToString("F2")));
                    addSqlParam(cmd, "sales_division", id, entries[3]);
                    addSqlParam(cmd, "sales_state", id, entries[4]);
                    addSqlParam(cmd, "product_group", id, entries[5]);
                    addSqlParam(cmd, "product_name", id, entries[6]);
                    addSqlParam(cmd, "product_specification", id, entries[7]);

                    /*string loc = entries[4];

                    if (states.ContainsKey(loc))
                    {
                        loc = states[loc];
                    }
                    else
                    {
                        string state = MapApi.MapAPI.StateFromLocation(loc + " USA");
                        loc = state + " (" + MapApi.MapAPI.GeoCode(state) + ")";
                        states.Add(entries[4], loc);
                    }
                    addSqlParam(cmd, "sales_state", id, loc);
                    */
                    if (id%100 == 0)
                    {
                        cmd.ExecuteNonQuery();
                        cmd = connection.CreateCommand();
                    }
                }
            }

            cmd.ExecuteNonQuery();
        }

        private static void createTables()
        {
            dropConstraintsAndTable("coffee", "coffee.coffee_sales");
            ExecuteNonQuery(
               "CREATE TABLE coffee.coffee_sales (" +
               "id int NOT NULL," +
               "sales_date date NOT NULL," +
               "sales float NOT NULL," +
               "profit float NOT NULL," +
               "sales_division nvarchar(2048) NOT NULL," +
               "sales_state nvarchar(2048) NOT NULL," +
               "product_group nvarchar(2048) NOT NULL," +
               "product_name nvarchar(2048) NOT NULL," +
               "product_specification nvarchar(2048) NOT NULL," +
               "CONSTRAINT PK_coffee_coffee_sales PRIMARY KEY (id))"
            );
        }

        private static void createMetaTables()
        {
            // create table_info
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "delete from meta.field_alias where id >= 8000 and id < 9000;" +
                "delete from meta.field_info where id >= 8000 and id < 9000;" +
                "delete from meta.table_alias where id >= 8000 and id < 9000;" +
                "delete from meta.table_dependency where id >= 8000 and id < 9000;" +
                "delete from meta.table_info where id >= 8000 and id < 9000;";
            cmd.ExecuteNonQuery();
            
            Dictionary<string, int> tables = new Dictionary<string, int>();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmd.ExecuteNonQuery();

            // insert records 
            int nextId = 8000;
            cmd.CommandText = "select table_name from information_schema.tables where table_schema ='coffee'";
            SqlDataReader re = cmd.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', 'coffee');";

                insertStatements += "insert into meta.table_alias (id, priority, alias, table_id) values (" +
                    (nextId - 1) + ", 0, '" + re.GetString(0) + "', " + (nextId - 1) + ");";

                tables.Add(re.GetString(0), nextId - 1);
            }
            re.Close();

            if (insertStatements != "")
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = insertStatements;
                cmd.ExecuteNonQuery();
            }


            // create field_info
            Dictionary<string, int> fields = new Dictionary<string, int>();

            // insert records 
            nextId = 8000;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='coffee'";
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
            nextId = 8000;
            foreach (var table_name in tables.Keys)
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn " +
                    "from sys.foreign_key_columns as fk " +
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id " +
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = 14) " +
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

        private static List<List<object>> ExecuteQuery(string query, params object[] parameters)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i += 2)
                {
                    cmd.Parameters.Add(new SqlParameter(parameters[i].ToString(), parameters[i + 1]));
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
