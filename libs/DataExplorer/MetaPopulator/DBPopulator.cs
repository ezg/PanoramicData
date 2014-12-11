using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
    class DBPopulator
    {
        public static SqlConnection connection = null;

        public static void Populate()
        {
            //SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
            //   @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");
            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder( 
              @"Server=MACBOOK-WIN8\SQLEXPRESS;Database=PanoramicData;Connection Timeout=30;Max Pool Size=200;Trusted_Connection=Yes;");
            

            connection = new SqlConnection(stringBuilder.ToString());
            connection.Open();


            if (true)
            {
                //
               // generalUpdates();
                
            }

            if (true)
            {
                createMetaTables();
                populateMetaTable("titanic", 1000, 2000);
                populateMetaTable("nba", 2000, 3000);
                populateMetaTable("coffee", 3000, 4000);
                populateMetaTable("census", 4000, 5000);
                populateMetaTable("tip", 5000, 6000);
                populateMetaTable("faculty", 6000, 7000);
                populateMetaTable("lahman", 7000, 8000);
                populateMetaTable("hua", 8000, 9000);

                // general updates
                generalUpdates();

                // tip schema update
                setFieldInfoVisible("id", 0, "tip", "tip");

                // census schema update
                setFieldInfoVisible("id", 0, "cenus", "cenus");
                setFieldInfoVisible("relationship", 0, "cenus", "cenus");

                setTableAlias("census", "census", "Census Data");

                // coffe schema updates 
                setFieldAlias("sales", "Sales ($)", "coffee", "coffee_sales");
                setFieldAlias("sales_date", "Sales Date", "coffee", "coffee_sales");
                setFieldAlias("profit", "Profit ($)", "coffee", "coffee_sales");
                setFieldAlias("sales_division", "Sales Division", "coffee", "coffee_sales");
                setFieldAlias("sales_state", "Sales State", "coffee", "coffee_sales");
                setFieldAlias("product_group", "Product Group", "coffee", "coffee_sales");
                setFieldAlias("product_name", "Product Name", "coffee", "coffee_sales");
                setFieldAlias("product_specification", "Product Specification", "coffee", "coffee_sales");

                setFieldInfoVisible("id", 0, "coffee", "coffee_sales");

                setTableAlias("coffee", "coffee_sales", "Coffee Sales");

                // nba schma updates 
                setFieldAlias("name", "Name", "nba", "player");
                setFieldInfoVisible("id", 0, "nba", "player");
                setFieldAlias("position", "Position", "nba", "player");
                setFieldAlias("shoots", "Shoots", "nba", "player");
                setFieldAlias("high_school", "High School", "nba", "player");
                setFieldAlias("college", "College", "nba", "player");
                setFieldAlias("draft", "Draft", "nba", "player");
                setFieldAlias("nba_debut", "NBA Debut", "nba", "player");
                setFieldAlias("hall_of_fame", "Hall of Fame", "nba", "player");
                setFieldAlias("born_in", "Born in", "nba", "player");
                setFieldAlias("birth_day", "Birthday", "nba", "player");
                setFieldAlias("height", "Height (cm)", "nba", "player");
                setFieldAlias("weight", "Weight (kg)", "nba", "player");

                setFieldInfoVisible("id", 0, "nba", "game_log");
                setFieldInfoVisible("game_id", 0, "nba", "game_log");
                setFieldInfoVisible("season_id", 0, "nba", "game_log");
                setFieldInfoVisible("player_id", 0, "nba", "game_log");
                setFieldInfoVisible("team_id", 0, "nba", "game_log");
                setFieldInfoVisible("opponent_id", 0, "nba", "game_log");

                setFieldAlias("id", "Game Log", "nba", "game_log");
                setFieldAlias("id", "Game", "nba", "game");
                setFieldAlias("id", "Season", "nba", "season");
                setFieldAlias("id", "Player", "nba", "player");
                setFieldAlias("id", "Team Played For", "nba", "team");
                setFieldAlias("id", "Opponent Team", "nba", "team");

                setFieldAlias("score_margin", "Score Margin", "nba", "game_log");
                setFieldAlias("mp", "Minutes Played", "nba", "game_log");
                setFieldAlias("fg", "Field Goals", "nba", "game_log");
                setFieldAlias("fga", "Field Goal Attempts", "nba", "game_log");
                setFieldAlias("fg_pct", "Field Goal %", "nba", "game_log");
                setFieldAlias("fg3", "3pt Field Goals", "nba", "game_log");
                setFieldAlias("fg3a", "3pt Field Goal Attempts", "nba", "game_log");
                setFieldAlias("fg3_pct", "3pt Field Goal %", "nba", "game_log");
                setFieldAlias("ft", "Free Throws", "nba", "game_log");
                setFieldAlias("fta", "Free Throw Attempts", "nba", "game_log");
                setFieldAlias("ft_pct", "Free Throw %", "nba", "game_log");
                setFieldAlias("orb", "Offensive Rebounds", "nba", "game_log");
                setFieldAlias("drb", "Defensive Rebounds", "nba", "game_log");
                setFieldAlias("trb", "Total Rebounds", "nba", "game_log");
                setFieldAlias("ast", "Assists", "nba", "game_log");
                setFieldAlias("stl", "Steals", "nba", "game_log");
                setFieldAlias("blk", "Blocks", "nba", "game_log");
                setFieldAlias("tov", "Turnovers", "nba", "game_log");
                setFieldAlias("pf", "Personal Fouls", "nba", "game_log");
                setFieldAlias("pts", "Points", "nba", "game_log");
                setFieldAlias("plus_minus", "Plus / Minus", "nba", "game_log");

                setFieldInfoVisible("id", 0, "nba", "team");
                setFieldAlias("name", "Name", "nba", "team");
                setFieldAlias("location", "Location", "nba", "team");

                setFieldInfoVisible("id", 0, "nba", "game");
                setFieldAlias("game_date", "Game Date", "nba", "game");
                setFieldAlias("playoff", "Playoff Game?", "nba", "game");

                setFieldInfoVisible("id", 0, "nba", "season");
                setFieldAlias("name", "Name", "nba", "season");

                setTableAlias("game_log", "nba", "Game Log");
                setTableAlias("team", "nba", "Team");
                setTableAlias("season", "nba", "Season");
                setTableAlias("player", "nba", "Player");

                setRelationshipName("team", "game_log", "nba", "id", "team_id", "Team Played For");
                setRelationshipName("game_log", "team", "nba", "team_id", "id", "Team Played For");

                setRelationshipName("team", "game_log", "nba", "id", "opponent_id", "Opponent Team");
                setRelationshipName("game_log", "team", "nba", "opponent_id", "id", "Opponent Team");

                setRelationshipName("game", "game_log", "nba", "id", "game_id", "Game");
                setRelationshipName("game_log", "game", "nba", "game_id", "id", "Game");

                setRelationshipName("player", "game_log", "nba", "id", "player_id", "Player");
                setRelationshipName("game_log", "player", "nba", "player_id", "id", "Player");

                setRelationshipName("season", "game_log", "nba", "id", "season_id", "Season");
                setRelationshipName("game_log", "season", "nba", "season_id", "id", "Season");

                printDependencies("nba");
            }

        }

        private static void printDependencies(string schema)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "select 	\n" +
                "    tFrom.schema_name,\n" +
                "    dep.id, \n" +
                "    tFrom.name,\n" +
                "    tTo.name,\n" +
                "    fFrom.name,\n" +
                "    fTo.name,\n" +
                "    dep.relationship_name\n" +
                "from	\n" +
                "    meta.table_dependency as dep,\n" +
                "    meta.table_info as tFrom,\n" +
                "    meta.table_info as tTo,\n" +
                "    meta.field_info as fFrom,\n" +
                "    meta.field_info as fTo\n" +
                "where \n" +
                "    dep.from_table_id = tFrom.id and \n" +
                "    dep.to_table_id = tTo.id and \n" +
                "    dep.from_field_id = fFrom.id and \n" +
                "    dep.to_field_id = fTo.id and \n" +
                "    tFrom.schema_name = '" + schema + "';";

            SqlDataReader re = cmd.ExecuteReader();
            string statements = "";
            while (re.Read())
            {
                Console.WriteLine(re.GetString(0) + ", " + re.GetInt32(1) + ", " + re.GetString(2) + ", " +
                                  re.GetString(3) + ", " + re.GetString(4) + ", " + re.GetString(5) + ", " +
                                  re.GetString(6));
            }
            re.Close();
        }

        private static void generalUpdates()
        {
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "update meta.field_info set visualization_type = 'numeric' where data_type = 'float' or data_type = 'int';" +
                "update meta.field_info set visualization_type = 'date' where data_type = 'date';" +
                "update meta.field_info set visualization_type = 'time' where data_type = 'time';" +
                "update meta.field_info set visualization_type = 'geography' where data_type = 'geography';" +
                "update meta.field_info set visualization_type = 'category' where data_type = 'nvarchar';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'location';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'embarked';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'home';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'destination';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'native_country';" +

                "update meta.field_info set visualization_type = 'geography' where name = 'University';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'Bachelors';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'Masters';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'Doctorate';" +
                "update meta.field_info set visualization_type = 'geography' where name = 'PostDoc';" +

                "update meta.field_info set data_type = 'geography' where name = 'University';" +
                "update meta.field_info set data_type = 'geography' where name = 'Bachelors';" +
                "update meta.field_info set data_type = 'geography' where name = 'Masters';" +
                "update meta.field_info set data_type = 'geography' where name = 'Doctorate';" +
                "update meta.field_info set data_type = 'geography' where name = 'PostDoc';" +

                "update meta.field_info set visualization_type = 'enum' where name = 'Rank';" +

                "update meta.field_info set visualization_type = 'enum' where name = 'sex';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'passenger_class';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'survived';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'sex';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'race';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'occupation';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'employer_type';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'education';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'smoker';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'time';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'day';" +
                "update meta.field_info set data_type = 'geography' where name = 'location';" +
                "update meta.field_info set data_type = 'geography' where name = 'embarked';" +
                "update meta.field_info set data_type = 'geography' where name = 'home';" +
                "update meta.field_info set data_type = 'geography' where name = 'destination';" +
                "update meta.field_info set data_type = 'geography' where name = 'native_country';" +
                //"update meta.field_info set data_type = 'geography' where name = 'sales_state';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'sales_division';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'product_group';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'martial_status';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'salary_over_50k';" +

                "update meta.field_info set visualization_type = 'enum' where name = 'block';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'dataGroup';" + 
                "update meta.field_info set visualization_type = 'enum' where name = 'structure';" +
                "update meta.field_info set visualization_type = 'enum' where name = 'testObject';" +

                "update meta.field_info set visualization_type = 'enum' where name = 'product_specification';";

            cmd.ExecuteNonQuery();

            runAndExecute(
                "select  " +
                "   'update meta.field_info set max_value = ' + " +
                "   '(select max(' + m.name + ') from ' + t.schema_name + '.' + t.name + ') ' + " +
                "   'where id = ' + cast(m.id as nvarchar) " +
                "from  " +
                "    meta.field_info m, meta.table_info t  " +
                "where  " +
                "    m.table_id = t.id and m.visualization_type = 'numeric';");
            runAndExecute(
                "select " +
                "    'update meta.field_info set min_value = ' + " +
                "    '(select min(' + m.name + ') from ' + t.schema_name + '.' + t.name + ') ' + " +
                "    'where id = ' + cast(m.id as nvarchar)" +
                "from " +
                "    meta.field_info m, meta.table_info t " +
                "where " +
                "    m.table_id = t.id and m.visualization_type = 'numeric';");

            runAndExecute(
                "select " +
                "    'update meta.field_info set bin_size = ' + " +
                "    '(select ceiling((max(' + m.name + ') - min(' + m.name + ')) / 10.0) from ' + t.schema_name + '.' + t.name + ') ' + " +
                "    'where id = ' + cast(m.id as nvarchar) " +
                "from  " +
                "    meta.field_info m, meta.table_info t  " +
                "where  " +
                "    m.table_id = t.id and m.visualization_type = 'numeric';");

            runAndExecute(
                "select  " +
                "    'update meta.field_info set bin_size = 0.1 ' + " +
                "    'where id = ' + cast(m.id as nvarchar) + ';' " +
                "from  " +
                "    meta.field_info m, meta.table_info t  " +
                "where  " +
                "    m.table_id = t.id and m.visualization_type = 'numeric' and " +
                "    m.max_value = 1 and m.min_value = 0; ");
        }

        private static void setRelationshipName(string fromTable, string toTable, string schemaName, string fieldFrom, string fieldTo, string relationshipName)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "update meta.table_dependency set relationship_name = '" + relationshipName + "' where id in  \n" +
                "(     \n" +
                "    select  \n" +
                "        dep.id \n" +
                "    from	 \n" +
                "        meta.table_dependency as dep, \n" +
                "        meta.table_info as tFrom, \n" +
                "        meta.table_info as tTo, \n" +
                "        meta.field_info as fFrom, \n" +
                "        meta.field_info as fTo \n" +
                "    where  \n" +
                "        dep.from_table_id = tFrom.id and \n" +
                "        dep.to_table_id = tTo.id and \n" +
                "        dep.from_field_id = fFrom.id and \n" +
                "        dep.to_field_id = fTo.id and \n" +
                "        tFrom.name = '" + fromTable + "' and  \n" +
                "        tTo.name = '" + toTable + "' and \n" +
                "        fFrom.name = '" + fieldFrom + "' and \n" +
                "        fTo.name = '" + fieldTo + "' and \n" +
                "        tFrom.schema_name = '" + schemaName + "'\n" +
                ");";
            cmd.ExecuteNonQuery();
        }

        private static void setFieldAlias(string fieldName, string alias, string schemaName, string tableName)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "update meta.field_alias set alias  = '" + alias + "' where field_id in " +
                "( " +
	            "    select  " +
		        "        fi.id " +
	            "    from  " +
		        "        meta.field_info as fi, " +
		        "        meta.table_info as ti " +
	            "    where  " +
		        "        ti.id = fi.table_id and " +
                "        ti.schema_name = '" + schemaName + "' and ti.name = '" + tableName + "' and " +
                "        fi.name = '" + fieldName + "' " +
                ");";
            cmd.ExecuteNonQuery();
        }

        private static void setTableAlias(string tableName, string schemaName, string alias)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "update meta.table_alias set alias  = '" + alias + "' where id in " +
                "( " +
                "    select  " +
                "        ti.id " +
                "    from  " +
                "        meta.table_info as ti " +
                "    where  " +
                "        ti.schema_name = '" + schemaName + "' and ti.name = '" + tableName + "');";
            cmd.ExecuteNonQuery();
        }

        private static void setFieldInfoVisible(string fieldName, int visible, string schemaName, string tableName)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "update meta.field_info set visible  = " + visible + " where id in " +
                "( " +
                "    select  " +
                "        fi.id " +
                "    from  " +
                "        meta.field_info as fi, " +
                "        meta.table_info as ti " +
                "    where  " +
                "        ti.id = fi.table_id and " +
                "        ti.schema_name = '" + schemaName + "' and ti.name = '" + tableName + "' and " +
                "        fi.name = '" + fieldName + "' " +
                ");";
            cmd.ExecuteNonQuery();
        }

        private static void runAndExecute(string query)
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

        private static void populateMetaTable(string schemaName, int idsFrom, int idsTo)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "delete from meta.field_alias where id between " + idsFrom + " and " + idsTo + ";" +
                "delete from meta.field_info where id between " + idsFrom + " and " + idsTo + ";" +
                "delete from meta.table_alias where id between " + idsFrom + " and " + idsTo + ";" +
                "delete from meta.table_dependency where id between " + idsFrom + " and " + idsTo + ";" +
                "delete from meta.table_info where id between  " + idsFrom + " and " + idsTo + ";";
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
            int nextId = idsFrom;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name from information_schema.tables where table_schema ='" + schemaName + "'";
            SqlDataReader re = cmd.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', '" + schemaName + "');\n";

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

            // get schema id
            cmd = connection.CreateCommand();
            cmd.CommandText = "select schema_id from sys.schemas where name = '" + schemaName + "'";
            re = cmd.ExecuteReader();
            re.Read();
            int schemaId = re.GetInt32(0);
            re.Close();


            Dictionary<string, int> fields = new Dictionary<string, int>();

            // insert records 
            nextId = idsFrom;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='" + schemaName + "'";
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
            nextId = idsFrom;
            foreach (var table_name in tables.Keys)
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn " +
                    "from sys.foreign_key_columns as fk " +
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id " +
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = " + schemaId + ") " +
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

        private static void createMetaTables()
        {
            SqlCommand cmd = connection.CreateCommand();

            // create table_info
            dropConstraintsAndTable("meta", "meta.table_info");
            cmd.CommandText =
                "CREATE TABLE meta.table_info (" +
                "id int NOT NULL, " +
                "name nvarchar(255) NOT NULL," +
                "schema_name nvarchar(255) NOT NULL," +
                "pk_field_info int NULL," +
                "identifier_field_id int NULL," +
                "CONSTRAINT PK_meta_table_info PRIMARY KEY (id))";
            cmd.ExecuteNonQuery();

            dropConstraintsAndTable("meta", "meta.table_alias");
            cmd.CommandText =
                "CREATE TABLE meta.table_alias (" +
                "id int NOT NULL, " +
                "priority int NOT NULL, " +
                "alias nvarchar(255) NOT NULL," +
                "table_id int NOT NULL," +
                "CONSTRAINT FK_meta_table_alias_table_info FOREIGN KEY (table_id) REFERENCES meta.table_info (id) ON DELETE CASCADE," +
                "CONSTRAINT PK_meta_table_alias PRIMARY KEY (id))";
            cmd.ExecuteNonQuery();

            // create field_info
            cmd = connection.CreateCommand();
            dropConstraintsAndTable("meta", "meta.field_info");
            cmd.CommandText =
                "CREATE TABLE meta.field_info (" +
                "id int NOT NULL, " +
                "name nvarchar(255) NOT NULL," +
                "visible bit NOT NULL," +
                "data_type nvarchar(255) NOT NULL," +
                "visualization_type nvarchar(255)," +
                "max_value float," +
                "min_value float," +
                "bin_size float," +
                "table_id int NOT NULL," +
                "CONSTRAINT FK_meta_field_info_table_info FOREIGN KEY (table_id) REFERENCES meta.table_info (id) ON DELETE CASCADE," +
                "CONSTRAINT PK_meta_field_info PRIMARY KEY (id))";
            cmd.ExecuteNonQuery();

            dropConstraintsAndTable("meta", "meta.field_alias");
            cmd.CommandText =
                "CREATE TABLE meta.field_alias (" +
                "id int NOT NULL, " +
                "priority int NOT NULL, " +
                "alias nvarchar(255) NOT NULL," +
                "field_id int NOT NULL," +
                "CONSTRAINT FK_meta_field_alias_table_info FOREIGN KEY (field_id) REFERENCES meta.field_info (id) ON DELETE CASCADE," +
                "CONSTRAINT PK_meta_field_alias PRIMARY KEY (id))";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            dropConstraintsAndTable("meta", "meta.table_dependency");
            cmd.CommandText =
                "CREATE TABLE meta.table_dependency (" +
                "id int NOT NULL, " +
                "from_table_id int NOT NULL, " +
                "to_table_id int NOT NULL, " +
                "from_field_id int NOT NULL, " +
                "to_field_id int NOT NULL, " +
                "cardinality nvarchar(255) NOT NULL," +
                "relationship_name nvarchar(255)," +

                "CONSTRAINT FK_meta_table_dependency_from_table FOREIGN KEY (from_table_id) REFERENCES meta.table_info (id) ON DELETE NO ACTION," +
                "CONSTRAINT FK_meta_table_dependency_to_table FOREIGN KEY (to_table_id) REFERENCES meta.table_info (id) ON DELETE NO ACTION," +

                "CONSTRAINT FK_meta_table_dependency_from_field FOREIGN KEY (from_field_id) REFERENCES meta.field_info (id) ON DELETE NO ACTION," +
                "CONSTRAINT FK_meta_table_dependency_to_field FOREIGN KEY (to_field_id) REFERENCES meta.field_info (id) ON DELETE NO ACTION," +

                "CONSTRAINT PK_meta_table_dependency PRIMARY KEY (id))";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NOT NULL;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info WITH CHECK ADD " +
                "CONSTRAINT FK_meta_table_info_pk_field_info FOREIGN KEY (pk_field_info) REFERENCES meta.field_info (id) ON DELETE NO ACTION;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info WITH CHECK ADD " +
                "CONSTRAINT FK_meta_table_info_identifier_field_id FOREIGN KEY (identifier_field_id) REFERENCES meta.field_info (id) ON DELETE NO ACTION;";
            cmd.ExecuteNonQuery();

            /*
            CREATE NONCLUSTERED INDEX [index_1] ON [nba].[game_log]
            (
	            [player_id] ASC,
	            [team_id] ASC,
	            [season_id] ASC,
	            [opponent_id] ASC,
	            [game_id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            GO
            */
            /*
            CREATE NONCLUSTERED INDEX [index_2] ON [nba].[player]
            (
	            [name] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            GO
            */
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
}
