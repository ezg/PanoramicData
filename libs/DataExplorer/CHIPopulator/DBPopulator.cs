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

namespace CHIPopulator
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

            if (true)
            {
                //createMetaTables();
                createMetaTablesTest();
                //createMetaTablesNBA();
                return;
            }

            createTables();

            string MetadataDirectory = @"C:\ez_projects\fulfillment-w-affils-june2011\chi";
            string[] xmlFiles = Directory.GetFiles(MetadataDirectory, "*.xml");
            Console.WriteLine("# of Files: " + xmlFiles.Length);

            int confId = 0;
            int nextConfId = 0;
            int articleId = 0;
            int nextAuthorId = 0;
            int nextKeywordId = 0;
            int nextKeywordUsageId = 0;
            int nextOrganisationId = 0;
            int nextAffiliationId = 0;
            int nextAuthorshipId = 0;
            foreach (string xmlFile in xmlFiles)
            {
                using (FileStream stream = new FileStream(Path.Combine(MetadataDirectory, xmlFile), FileMode.Open))
                {
                    XDocument document = null;
                    try
                    {
                        document = XDocument.Load(stream);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                    var content = document.Descendants("content").Single();
                    var articles = content.Descendants("article_rec");
                    var rec = document.Descendants("proceeding_rec").First();
                    string confName = rec.Element("acronym").Value;
                    Console.WriteLine(confName);

                    var loc = document.Descendants("conference_loc").First();
                    string confCity = loc.Element("city").Value;
                    string confState = loc.Element("state").Value;
                    string confCountry = loc.Element("country").Value;
                    List<string> confLocs = new List<string>();
                    confLocs.Add(confCity);
                    confLocs.Add(confState);
                    confLocs.Add(confCountry);

                    string confLoc = string.Join(", ", confLocs.Where(s => s.Trim() != ""));

                    Console.WriteLine(confLoc);
                    Console.WriteLine("---------------------------");

                    confId = nextConfId++;

                    SqlCommand cmd = connection.CreateCommand();
                    cmd = connection.CreateCommand();
                    cmd.CommandText = string.Format(
                            "INSERT INTO acm.conference (id, name, location) " +
                            "VALUES (@id_{0}, @name_{0}, @location_{0})", confId);

                    cmd.Parameters.Add(new SqlParameter("@id_" + confId, confId));
                    cmd.Parameters.Add(new SqlParameter("@name_" + confId, confName));
                    cmd.Parameters.Add(new SqlParameter("@location_" + confId, confLoc));

                    cmd.ExecuteNonQuery();


                    // Create a temporary article object used for importing
                    // Be sure to decode text fields as they might contain encoded characters

                    foreach (var article in articles)
                    {
                        string title = article.Element("title").Value;
                        if (article.Element("subtitle") != null && !string.IsNullOrWhiteSpace(article.Element("subtitle").Value))
                            title += ": " + article.Element("subtitle").Value;
                        title = System.Net.WebUtility.HtmlDecode(title);

                        string url = article.Element("url") != null ? article.Element("url").Value : null;
                        DateTime publicationDate = DateTime.Now;
                        try
                        {
                            publicationDate = DateTime.Parse(article.Element("article_publication_date").Value);
                        }
                        catch (Exception e)
                        {
                        }

                        ACMArticle articleObj = new ACMArticle(title, url, publicationDate);

                        // Author
                        var authorsNode = article.Element("authors");
                        foreach (var authorObj in authorsNode.Elements("au"))
                        {
                            string name;

                            string middleName = authorObj.Element("middle_name").Value;
                            name = string.IsNullOrWhiteSpace(middleName) ?
                                authorObj.Element("first_name").Value + " " + authorObj.Element("last_name").Value :
                                authorObj.Element("first_name").Value + " " + middleName + " " + authorObj.Element("last_name").Value;

                            var surname = authorObj.Element("surname");
                            if (surname != null && string.IsNullOrWhiteSpace(surname.Value))
                                name += " " + surname.Value;

                            string affiliation = "";
                            if (authorObj.Element("affiliation") != null)
                            {
                                affiliation = System.Net.WebUtility.HtmlDecode(authorObj.Element("affiliation").Value);
                            }


                            articleObj.Authors.Add(new ACMAuthor(System.Net.WebUtility.HtmlDecode(name), 
                                System.Net.WebUtility.HtmlDecode(affiliation)));
                        }

                        // Kewords
                        var keywordNode = article.Element("keywords");
                        if (keywordNode != null)
                        {
                            foreach (var keywordObj in keywordNode.Elements("kw"))
                                articleObj.Keywords.Add(keywordObj.Value);
                        }

                        articleObj.DebugConsole();

                        // insert into db
                        articleId++;
                        Console.WriteLine(articleId);
                        cmd = connection.CreateCommand();
                        cmd.CommandText = string.Format(
                                "INSERT INTO acm.publication (id, title, url, publication_date, conference_id) " +
                                "VALUES (@id_{0}, @title_{0}, @url_{0}, @publication_date_{0}, @conference_id_{0})", articleId);

                        cmd.Parameters.Add(new SqlParameter("@id_" + articleId, articleId));
                        cmd.Parameters.Add(new SqlParameter("@title_" + articleId, articleObj.Title));
                        cmd.Parameters.Add(new SqlParameter("@url_" + articleId, articleObj.Url.ToString()));
                        cmd.Parameters.Add(new SqlParameter("@publication_date_" + articleId, articleObj.PublicationDate));
                        cmd.Parameters.Add(new SqlParameter("@conference_id_" + articleId, confId));

                        cmd.ExecuteNonQuery();

                        foreach (var author in articleObj.Authors)
                        {
                            int authorId = -1;
                            List<List<object>> val = ExecuteQuery(
                                "select id from acm.person where name = @name",
                                "@name", author.Name.Trim());

                            if (val.Count > 0)
                            {
                                authorId = (int)val[0][0];
                            }
                            else
                            {
                                authorId = nextAuthorId++;

                                cmd = connection.CreateCommand();
                                cmd.CommandText = string.Format(
                                        "INSERT INTO acm.person (id, name) " +
                                        "VALUES (@id_{0}, @name_{0})", authorId);

                                cmd.Parameters.Add(new SqlParameter("@id_" + authorId, authorId));
                                cmd.Parameters.Add(new SqlParameter("@name_" + authorId, author.Name.Trim()));

                                cmd.ExecuteNonQuery();
                            }

                            int organisationId = -1;
                            val = ExecuteQuery(
                                "select id from acm.organisation where name = @name",
                                "@name", author.Affiliation.Trim());

                            if (val.Count > 0)
                            {
                                organisationId = (int)val[0][0];
                            }
                            else
                            {
                                organisationId = nextOrganisationId++;

                                cmd = connection.CreateCommand();
                                cmd.CommandText = string.Format(
                                        "INSERT INTO acm.organisation (id, name) " +
                                        "VALUES (@id_{0}, @name_{0})", organisationId);

                                cmd.Parameters.Add(new SqlParameter("@id_" + organisationId, organisationId));
                                cmd.Parameters.Add(new SqlParameter("@name_" + organisationId, author.Affiliation.Trim()));

                                cmd.ExecuteNonQuery();
                            }


                            int authorShipId = nextAuthorshipId++;
                            cmd = connection.CreateCommand();
                            cmd.CommandText = string.Format(
                                    "INSERT INTO acm.authorship (id, person_id, publication_id) " +
                                    "VALUES (@id_{0}, @person_id{0}, @publication_id{0})", authorShipId);

                            cmd.Parameters.Add(new SqlParameter("@id_" + authorShipId, authorShipId));
                            cmd.Parameters.Add(new SqlParameter("@person_id" + authorShipId, authorId));
                            cmd.Parameters.Add(new SqlParameter("@publication_id" + authorShipId, articleId));

                            cmd.ExecuteNonQuery();


                            int affiliationId = nextAffiliationId++;

                            cmd = connection.CreateCommand();
                            cmd.CommandText = string.Format(
                                    "INSERT INTO acm.affiliation (id, authorship_id, organisation_id) " +
                                    "VALUES (@id_{0}, @authorship_id{0}, @organisation_id{0})", affiliationId);

                            cmd.Parameters.Add(new SqlParameter("@id_" + affiliationId, affiliationId));
                            cmd.Parameters.Add(new SqlParameter("@authorship_id" + authorShipId, authorShipId));
                            cmd.Parameters.Add(new SqlParameter("@organisation_id" + affiliationId, organisationId));

                            cmd.ExecuteNonQuery();
                        }

                        foreach (var keyword in articleObj.Keywords)
                        {
                            int keywordId = -1;
                            List<List<object>> val = ExecuteQuery(
                                "select id from acm.keyword where name = @name",
                                "@name", keyword.Trim());

                            if (val.Count > 0)
                            {
                                keywordId = (int)val[0][0];
                            }
                            else
                            {
                                keywordId = nextKeywordId++;

                                cmd = connection.CreateCommand();
                                cmd.CommandText = string.Format(
                                        "INSERT INTO acm.keyword (id, name) " +
                                        "VALUES (@id_{0}, @name_{0})", keywordId);

                                cmd.Parameters.Add(new SqlParameter("@id_" + keywordId, keywordId));
                                cmd.Parameters.Add(new SqlParameter("@name_" + keywordId, keyword.Trim()));

                                cmd.ExecuteNonQuery();
                            }

                            int keywordUsageId = -1;
                            val = ExecuteQuery(
                                "select id from acm.keyword_usage where keyword_id = @keyword_id and publication_id = @publication_id",
                                "@keyword_id", keywordId, "@publication_id", articleId);

                            if (val.Count > 0)
                            {
                                keywordUsageId = (int)val[0][0];
                            }
                            else
                            {
                                keywordUsageId = nextKeywordUsageId++;

                                cmd = connection.CreateCommand();
                                cmd.CommandText = string.Format(
                                        "INSERT INTO acm.keyword_usage (id, keyword_id, publication_id) " +
                                        "VALUES (@id_{0}, @keyword_id_{0}, @publication_id_{0})", keywordUsageId);

                                cmd.Parameters.Add(new SqlParameter("@id_" + keywordUsageId, keywordUsageId));
                                cmd.Parameters.Add(new SqlParameter("@keyword_id_" + keywordUsageId, keywordId));
                                cmd.Parameters.Add(new SqlParameter("@publication_id_" + keywordUsageId, articleId));

                                cmd.ExecuteNonQuery();
                            }
                        }

                    }
                }
            }
        }

        private static void createTables()
        {
            dropConstraintsAndTable("acm", "acm.conference");
            ExecuteNonQuery(
               "CREATE TABLE acm.conference (" +
               "id int NOT NULL," +
               "name nvarchar(2048) NOT NULL," +
               "location nvarchar(2048) NOT NULL," +
               "CONSTRAINT PK_acm_conference PRIMARY KEY (id))"
            );

            dropConstraintsAndTable("acm", "acm.person");
            ExecuteNonQuery(
               "CREATE TABLE acm.person (" +
               "id int NOT NULL," +
               "name nvarchar(2048) NOT NULL," +
               "CONSTRAINT PK_acm_author PRIMARY KEY (id))"
            );

            dropConstraintsAndTable("acm", "acm.publication");
            ExecuteNonQuery(
               "CREATE TABLE acm.publication (" +
               "id int NOT NULL," +
               "title nvarchar(2048) NOT NULL," +
               "url nvarchar(2048) NOT NULL," +
               "publication_date date NOT NULL," +
               "conference_id int NOT NULL," +
               "CONSTRAINT FK_acm_publication_conference FOREIGN KEY (conference_id) REFERENCES acm.conference (id) ON DELETE CASCADE," +
               "CONSTRAINT PK_acm_publication PRIMARY KEY (id))"
            );

            dropConstraintsAndTable("acm", "acm.keyword");
            ExecuteNonQuery(
               "CREATE TABLE acm.keyword (" +
               "id int NOT NULL," +
               "name nvarchar(2048) NOT NULL," +
               "CONSTRAINT PK_acm_keyword PRIMARY KEY (id))");

            dropConstraintsAndTable("acm", "acm.keyword_usage");
            ExecuteNonQuery(
               "CREATE TABLE acm.keyword_usage (" +
               "id int NOT NULL," +
               "publication_id int NOT NULL," +
               "keyword_id int NOT NULL," +
               "CONSTRAINT PK_acm_keyword_usage PRIMARY KEY (id)," +
               "CONSTRAINT FK_acm_keyword_usage_publication FOREIGN KEY (publication_id) REFERENCES acm.publication (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_acm_keyword_usage_keyword FOREIGN KEY (keyword_id) REFERENCES acm.keyword (id) ON DELETE CASCADE)"
            );

            dropConstraintsAndTable("acm", "acm.organisation");
            ExecuteNonQuery(
               "CREATE TABLE acm.organisation (" +
               "id int NOT NULL," +
               "name nvarchar(2048) NOT NULL," +
               "CONSTRAINT PK_acm_organisation PRIMARY KEY (id))");

            dropConstraintsAndTable("acm", "acm.authorship");
            ExecuteNonQuery(
               "CREATE TABLE acm.authorship (" +
               "id int NOT NULL," +
               "person_id int NOT NULL," +
               "publication_id int NOT NULL," +
               "CONSTRAINT PK_acm_authorship PRIMARY KEY (id)," +
               "CONSTRAINT FK_acm_authorship_person FOREIGN KEY (person_id) REFERENCES acm.person (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_acm_authorship_publication FOREIGN KEY (publication_id) REFERENCES acm.publication (id) ON DELETE CASCADE)"
            );

            dropConstraintsAndTable("acm", "acm.affiliation");
            ExecuteNonQuery(
               "CREATE TABLE acm.affiliation (" +
               "id int NOT NULL," +
               "authorship_id int NOT NULL," +
               "organisation_id int NOT NULL," +
               "CONSTRAINT PK_acm_affiliation PRIMARY KEY (id)," +
               "CONSTRAINT FK_acm_affiliation_authorship FOREIGN KEY (authorship_id) REFERENCES acm.authorship (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_acm_affiliation_organisation FOREIGN KEY (organisation_id) REFERENCES acm.organisation (id) ON DELETE CASCADE)"
            );
        }


        private static void createMetaTables()
        {
            // create table_info
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "delete from meta.field_alias where id >= 1000 and id < 2000;" +
                "delete from meta.field_info where id >= 1000 and id < 2000;" +
                "delete from meta.table_alias where id >= 1000 and id < 2000;" +
                "delete from meta.table_dependency where id >= 1000 and id < 2000;" +
                "delete from meta.table_info where id >= 1000 and id < 2000;";
            
            Dictionary<string, int> tables = new Dictionary<string, int>();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmd.ExecuteNonQuery();

            // insert records 
            int nextId = 1000;
            cmd.CommandText = "select table_name from information_schema.tables where table_schema ='acm'";
            SqlDataReader re = cmd.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', 'acm');";

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
            nextId = 1000;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='acm'";
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
            nextId = 1000;
            foreach (var table_name in tables.Keys)
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn " +
                    "from sys.foreign_key_columns as fk " +
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id " +
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = 8) " +
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


        private static void createMetaTablesTest()
        {
            // create table_info
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "delete from meta.field_alias where id >= 2000 and id < 3000;" +
                "delete from meta.field_info where id >= 2000 and id < 3000;" +
                "delete from meta.table_alias where id >= 2000 and id < 3000;" +
                "delete from meta.table_dependency where id >= 2000 and id < 3000;" +
                "delete from meta.table_info where id >= 2000 and id < 3000;";

            Dictionary<string, int> tables = new Dictionary<string, int>();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmd.ExecuteNonQuery();

            // insert records 
            int nextId = 2000;
            cmd.CommandText = "select table_name from information_schema.tables where table_schema ='test'";
            SqlDataReader re = cmd.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', 'test');";

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
            nextId = 2000;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='test'";
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
            nextId = 2000;
            foreach (var table_name in tables.Keys)
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn " +
                    "from sys.foreign_key_columns as fk " +
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id " +
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = 9) " +
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


        private static void createMetaTablesNBA()
        {
            // create table_info
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "delete from meta.field_alias where id >= 3000 and id < 4000;" +
                "delete from meta.field_info where id >= 3000 and id < 4000;" +
                "delete from meta.table_alias where id >= 3000 and id < 4000;" +
                "delete from meta.table_dependency where id >= 3000 and id < 4000;" +
                "delete from meta.table_info where id >= 3000 and id < 4000;";

            Dictionary<string, int> tables = new Dictionary<string, int>();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmd.ExecuteNonQuery();

            // insert records 
            int nextId = 3000;
            cmd.CommandText = "select table_name from information_schema.tables where table_schema ='nba'";
            SqlDataReader re = cmd.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', 'nba');";

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
            nextId = 3000;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='nba'";
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
            nextId = 3000;
            foreach (var table_name in tables.Keys)
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn " +
                    "from sys.foreign_key_columns as fk " +
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id " +
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = 10) " +
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
    public class ACMAuthor
    {
        public string Name { get; set; }
        public string Affiliation { get; set; }

        public ACMAuthor(string name, string affiliation)
        {
            Name = name;
            Affiliation = affiliation;
        }
    }

    public class ACMArticle
    {
        public ACMArticle(string title, string url, DateTime publicationDate)
        {
            Title = title;
            Url = url != null ? new Uri(url, UriKind.Absolute) : null;
            PublicationDate = publicationDate;
            Authors = new List<ACMAuthor>();
            Keywords = new List<string>();
        }

        public string Title { get; private set; }
        public Uri Url { get; private set; }
        public DateTime PublicationDate { get; private set; }
        public List<ACMAuthor> Authors { get; private set; }
        public List<string> Keywords { get; private set; }

        public string PdfUrl
        {
            get
            {
                if (Url == null)
                    return null;

                UriBuilder uriBuilder = new UriBuilder(Url);
                uriBuilder.Host = "dl.acm.org";
                uriBuilder.Query += "&type=pdf";

                return uriBuilder.ToString();
            }
        }

        public void DebugConsole()
        {
            Console.WriteLine("Title: " + Title);
            Console.WriteLine("Authors: " + String.Join(", ", Authors));
            Console.WriteLine("Publication Date: " + PublicationDate.ToShortDateString());
            Console.WriteLine("Keywords: " + String.Join(", ", Keywords));
            Console.WriteLine("URL: " + Url);
            Console.WriteLine();
        }

    }
}
