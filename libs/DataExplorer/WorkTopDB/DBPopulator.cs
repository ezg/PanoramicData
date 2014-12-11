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

namespace WorkTopDB
{    
    class DBPopulator
    /*
    select 
        dep.id, tot.schema_name, tot.name as to_table, frt.name as from_name, frf.name, tof.name,
        dep.relationship_name
    from 
        meta.table_dependency dep, meta.table_info frt, meta.table_info tot, meta.field_info frf,
        meta.field_info tof
    where 
        dep.from_table_id = frt.id and dep.to_table_id = tot.id and
        dep.from_field_id = frf.id and dep.to_field_id = tof.id;
    
    update meta.table_dependency set relationship_name = 'Narrative' where id = 5014 or id = 5015;
    update meta.table_dependency set relationship_name = 'Destination' where id = 5016 or id = 5017;
    update meta.table_dependency set relationship_name = 'Source' where id = 5018 or id = 5019; 
    */
    {
        public static SqlConnection connectionSource = null;

        public static SqlConnection connectionPanoramicData = null;

        public static void Populate()
        {
            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
               @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=WT_PUB_Rosemary's Web (Local Only);User ID=test;Password=P@55word;Connection Timeout=30;");
            connectionSource = new SqlConnection(stringBuilder.ToString());
            connectionSource.Open();

            stringBuilder = new SqlConnectionStringBuilder(
                @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");
            connectionPanoramicData = new SqlConnection(stringBuilder.ToString());
            connectionPanoramicData.Open();


            //parseRosemaryFile();
            //createMetaTables();
            createMetaTablesRosemary();
        }

        private static void parseRosemaryFile()
        {
            string xmlFile = @"C:\ez_projects\data\SeeAlsoLinks-2.links";

            using (FileStream stream = new FileStream(xmlFile, FileMode.Open))
            {
                XDocument document = null;
  
                document = XDocument.Load(stream);
             
                var links = document.Descendants("link");

                dropConstraintsAndTable("rms", "rms.document");
                ExecuteNonQuery(
                   "CREATE TABLE rms.document (" +
                   "id int NOT NULL," +
                   "name nvarchar(2048) NOT NULL," +
                   "CONSTRAINT PK_rms_document PRIMARY KEY (id))"
                );

                dropConstraintsAndTable("rms", "rms.link_type");
                ExecuteNonQuery(
                   "CREATE TABLE rms.link_type (" +
                   "id int NOT NULL," +
                   "name nvarchar(2048) NOT NULL," +
                   "CONSTRAINT PK_rms_link_type PRIMARY KEY (id))"
                );

                dropConstraintsAndTable("rms", "rms.link");
                ExecuteNonQuery(
                   "CREATE TABLE rms.link (" +
                   "id int NOT NULL," +
                   "source_id int NOT NULL," +
                   "destination_id int NOT NULL," +
                   "link_type_id int NOT NULL," +
                   "annotation nvarchar(2048)," +
                   "CONSTRAINT FK_rms_link_destination_document FOREIGN KEY (destination_id) REFERENCES rms.document (id) ON DELETE NO ACTION," +
                   "CONSTRAINT FK_rms_link_source_document FOREIGN KEY (source_id) REFERENCES rms.document (id) ON DELETE NO ACTION," +
                   "CONSTRAINT FK_rms_link_link_type FOREIGN KEY (link_type_id) REFERENCES rms.link_type (id) ON DELETE NO ACTION," +
                   "CONSTRAINT PK_rms_link PRIMARY KEY (id))"
                );

                int nextDocumentId = 0;
                int nextLinkId = 0;
                int nextLinkTypeId = 0;

                Dictionary<string, int> linkTypes = new Dictionary<string, int>();
                Dictionary<string, int> documents = new Dictionary<string, int>();

                foreach (var link in links)
                {
                    string source = link.Element("source").Value.Trim().Replace("'", "''");
                    string destination = link.Element("destination").Value.Trim().Replace("'", "''");
                    string annotation = link.Element("annotation").Value.Trim().Replace("'", "''");
                    string link_type = link.Element("link_type").Value.Trim().Replace("'", "''");

                    int sourceDocumentId = 0;
                    int destinationDocumentId = 0;
                    int linkTypeId = 0;
                    int linkId = 0;

                    if (documents.ContainsKey(source))
                    {
                        sourceDocumentId = documents[source];
                    }
                    else
                    {
                        sourceDocumentId = ++nextDocumentId;
                        ExecuteNonQuery("insert into rms.document (name, id) values ('" + source + "', " + sourceDocumentId + ");");
                        documents.Add(source, sourceDocumentId);
                    }

                    if (documents.ContainsKey(destination))
                    {
                        destinationDocumentId = documents[destination];
                    }
                    else
                    {
                        destinationDocumentId = ++nextDocumentId;
                        ExecuteNonQuery("insert into rms.document (name, id) values ('" + destination + "', " + destinationDocumentId + ");");
                        documents.Add(destination, destinationDocumentId);
                    }

                    if (linkTypes.ContainsKey(link_type))
                    {
                        linkTypeId = linkTypes[link_type];
                    }
                    else
                    {
                        linkTypeId = ++nextLinkTypeId;
                        ExecuteNonQuery("insert into rms.link_type (name, id) values ('" + link_type + "', " + linkTypeId + ");");
                        linkTypes.Add(link_type, linkTypeId);
                    }

                    linkId = ++nextLinkId;
                    ExecuteNonQuery("insert into rms.link (id, source_id, destination_id, link_type_id, annotation) values " + 
                        "(" + linkId + ", " +
                              sourceDocumentId + ", "+
                              destinationDocumentId + ", " +
                              linkTypeId + ", " +
                              "'" + annotation + "'" + 
                        ");");

                }
            }
        }

        private static void     createMetaTables()
        {
            // create table_info
            SqlCommand cmd = connectionPanoramicData.CreateCommand();

            cmd.CommandText =
                "delete from meta.field_alias where id >= 5000 and id < 6000;" +
                "delete from meta.field_info where id >= 5000 and id < 6000;" +
                "delete from meta.table_alias where id >= 5000 and id < 6000;" +
                "delete from meta.table_dependency where id >= 5000 and id < 6000;" +
                "delete from meta.table_info where id >= 5000 and id < 6000;";
            cmd.ExecuteNonQuery();

            Dictionary<string, int> tables = new Dictionary<string, int>();

            cmd = connectionPanoramicData.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmd.ExecuteNonQuery();

            cmd = connectionPanoramicData.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmd.ExecuteNonQuery();

            // insert records 
            int nextId = 5000;
            cmd.CommandText = "select table_name from information_schema.tables where table_schema ='rms'";
            SqlDataReader re = cmd.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', 'rms');";

                insertStatements += "insert into meta.table_alias (id, priority, alias, table_id) values (" +
                    (nextId - 1) + ", 0, '" + re.GetString(0) + "', " + (nextId - 1) + ");";

                tables.Add(re.GetString(0), nextId - 1);
            }
            re.Close();

            if (insertStatements != "")
            {
                cmd = connectionPanoramicData.CreateCommand();
                cmd.CommandText = insertStatements;
                cmd.ExecuteNonQuery();
            }


            // create field_info
            Dictionary<string, int> fields = new Dictionary<string, int>();

            // insert records 
            nextId = 5000;
            cmd = connectionPanoramicData.CreateCommand();
            cmd.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='rms'";
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
                cmd = connectionPanoramicData.CreateCommand();
                cmd.CommandText = insertStatements;
                cmd.ExecuteNonQuery();
            }
            if (updateStatements != "")
            {
                cmd = connectionPanoramicData.CreateCommand();
                cmd.CommandText = updateStatements;
                cmd.ExecuteNonQuery();
            }

            cmd = connectionPanoramicData.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NOT NULL;";
            cmd.ExecuteNonQuery();

            cmd = connectionPanoramicData.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info WITH CHECK ADD " +
                "CONSTRAINT FK_meta_table_info_pk_field_info FOREIGN KEY (pk_field_info) REFERENCES meta.field_info (id) ON DELETE NO ACTION;";
            cmd.ExecuteNonQuery();

            // get all foreign key releations for a table
            nextId = 5000;
            foreach (var table_name in tables.Keys)
            {
                cmd = connectionPanoramicData.CreateCommand();
                cmd.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn " +
                    "from sys.foreign_key_columns as fk " +
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id " +
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = 11) " +
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
                    cmd = connectionPanoramicData.CreateCommand();
                    cmd.CommandText = "insert into meta.table_dependency (id, from_table_id, to_table_id, from_field_id, to_field_id, cardinality, relationship_name) values (" +
                        nextId++ + ", " +
                        tables[table_name] + ", " +
                        tables[relation.Key] + ", " +
                        fields[table_name + "_id"] + ", " +
                        fields[relation.Key + "_" + relation.Value] + ", " +
                        "'MANY', " +
                        "'')";
                    cmd.ExecuteNonQuery();

                    cmd = connectionPanoramicData.CreateCommand();
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

        private static void createMetaTablesRosemary()
        {
            // create table_info
            SqlCommand cmdPanoramicData = connectionPanoramicData.CreateCommand();
            SqlCommand cmdSource = connectionSource.CreateCommand();

            cmdPanoramicData.CommandText =
                "delete from meta.field_alias where id >= 5000 and id < 6000;" +
                "delete from meta.field_info where id >= 5000 and id < 6000;" +
                "delete from meta.table_alias where id >= 5000 and id < 6000;" +
                "delete from meta.table_dependency where id >= 5000 and id < 6000;" +
                "delete from meta.table_info where id >= 5000 and id < 6000;";
            cmdPanoramicData.ExecuteNonQuery();


            Dictionary<string, int> tables = new Dictionary<string, int>();

            cmdPanoramicData = connectionPanoramicData.CreateCommand();
            cmdPanoramicData.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmdPanoramicData.ExecuteNonQuery();

            cmdPanoramicData = connectionPanoramicData.CreateCommand();
            cmdPanoramicData.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmdPanoramicData.ExecuteNonQuery();

            // insert records 
            int nextId = 5000;
            cmdSource.CommandText = "select table_name from information_schema.tables where table_schema ='dbo'";
            SqlDataReader re = cmdSource.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', 'dbo');";

                insertStatements += "insert into meta.table_alias (id, priority, alias, table_id) values (" +
                    (nextId - 1) + ", 0, '" + re.GetString(0) + "', " + (nextId - 1) + ");";

                tables.Add(re.GetString(0), nextId - 1);
            }
            re.Close();

            if (insertStatements != "")
            {
                cmdPanoramicData = connectionPanoramicData.CreateCommand();
                cmdPanoramicData.CommandText = insertStatements;
                cmdPanoramicData.ExecuteNonQuery();
            }


            // create field_info
            Dictionary<string, int> fields = new Dictionary<string, int>();

            // insert records 
            nextId = 5000;
            cmdSource = connectionSource.CreateCommand();
            cmdSource.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='dbo'";
            re = cmdSource.ExecuteReader();
            insertStatements = "";
            string updateStatements = "";
            while (re.Read())
            {
                string visible = (re.GetString(1) == ("ID") || re.GetString(1) == ("DoqID")) ? "0" : "1";
                insertStatements += "insert into meta.field_info (id, name, visible, data_type, table_id) values (" +
                    nextId++ + "," +
                    "'" + re.GetString(1) + "'," +
                    visible + "," +
                    "'" + re.GetString(2) + "'," +
                    "" + tables[re.GetString(0)] + "" +
                    ");";

                if (re.GetString(1) == ("ID") || re.GetString(1) == ("DoqID"))
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
                cmdPanoramicData = connectionPanoramicData.CreateCommand();
                cmdPanoramicData.CommandText = insertStatements;
                cmdPanoramicData.ExecuteNonQuery();
            }
            if (updateStatements != "")
            {
                cmdPanoramicData = connectionPanoramicData.CreateCommand();
                cmdPanoramicData.CommandText = updateStatements;
                cmdPanoramicData.ExecuteNonQuery();
            }

            cmdPanoramicData = connectionPanoramicData.CreateCommand();
            cmdPanoramicData.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NOT NULL;";
            cmdPanoramicData.ExecuteNonQuery();

            cmdPanoramicData = connectionPanoramicData.CreateCommand();
            cmdPanoramicData.CommandText = "ALTER TABLE meta.table_info WITH CHECK ADD " +
                "CONSTRAINT FK_meta_table_info_pk_field_info FOREIGN KEY (pk_field_info) REFERENCES meta.field_info (id) ON DELETE NO ACTION;";
            cmdPanoramicData.ExecuteNonQuery();

            // get all foreign key releations for a table
            nextId = 5000;
            foreach (var table_name in tables.Keys)
            {
                cmdSource = connectionSource.CreateCommand();
                cmdSource.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn, c2.name " +
                    "from sys.foreign_key_columns as fk  " +
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id  " +
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id  " +
                    "inner join sys.columns as c2 on fk.referenced_object_id = c2.object_id and fk.referenced_column_id = c2.column_id  " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = 1)  " +
                    "order by TableWithForeignKey";

                List<string[]> relations = new List<string[]>();
                re = cmdSource.ExecuteReader();

                while (re.Read())
                {
                    string[] values = new string[] { re.GetString(0), re.GetString(1), re.GetString(2) };
                    relations.Add(values);
                }
                re.Close();

                foreach (var relation in relations)
                {
                    cmdPanoramicData = connectionPanoramicData.CreateCommand();
                    cmdPanoramicData.CommandText = "insert into meta.table_dependency (id, from_table_id, to_table_id, from_field_id, to_field_id, cardinality, relationship_name) values (" +
                        nextId++ + ", " +
                        tables[table_name] + ", " +
                        tables[relation[0]] + ", " +
                        fields[table_name + "_" + relation[2]] + ", " +
                        fields[relation[0] + "_" + relation[1]] + ", " +
                        "'MANY', " +
                        "'')";
                    cmdPanoramicData.ExecuteNonQuery();

                    cmdPanoramicData = connectionPanoramicData.CreateCommand();
                    cmdPanoramicData.CommandText = "insert into meta.table_dependency (id, from_table_id, to_table_id, from_field_id, to_field_id, cardinality, relationship_name) values (" +
                        nextId++ + ", " +
                        tables[relation[0]] + ", " +
                        tables[table_name] + ", " +
                        fields[relation[0] + "_" + relation[1]] + ", " +
                        fields[table_name + "_" + relation[2]] + ", " +
                        "'ONE', " +
                        "'')";
                    cmdPanoramicData.ExecuteNonQuery();

                }
            }
        }

        private static List<List<object>> ExecuteQuery(string query, params object[] parameters)
        {
            SqlCommand cmd = connectionPanoramicData.CreateCommand();
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
            SqlCommand cmd = connectionPanoramicData.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
        }

        private static void dropConstraintsAndTable(string schema, string table)
        {
            SqlCommand cmd = connectionPanoramicData.CreateCommand();

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
                cmd = connectionPanoramicData.CreateCommand();
                cmd.CommandText = alterStatements;
                cmd.ExecuteNonQuery();
            }
            cmd = connectionPanoramicData.CreateCommand();
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
