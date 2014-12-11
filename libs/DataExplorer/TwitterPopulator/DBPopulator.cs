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

namespace TwitterPopulator
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

            Stream stream = File.Open(@"C:\ez_projects\HumBub\starPad SDK\Apps\DataExplorer\TwitterPopulator\data\test_us.csv", FileMode.Open);

            createTables();

            SqlCommand personCommand = connection.CreateCommand();
            SqlCommand messageCommand = connection.CreateCommand();
            SqlCommand hashtagCommand = connection.CreateCommand();
            SqlCommand hashtagUsageCommand = connection.CreateCommand();
            SqlCommand mentionCommand = connection.CreateCommand();
            SqlCommand urlCommand = connection.CreateCommand();
            SqlCommand urlUsageCommand = connection.CreateCommand();
            SqlCommand mediaCommand = connection.CreateCommand();
            SqlCommand mediaUsageCommand = connection.CreateCommand();

            int nextPersonId = 1;
            int nextMessageId = 1;
            int nextHashtagId = 1;
            int nextHashtagUsageId = 1;
            int nextMentionId = 1;
            int nextUrlId = 1;
            int nextUrlUsageId = 1;
            int nextMediaId = 1;
            int nextMediaUsageId = 1;

            int batchSize = 100;
            int currentBatchCount = 0;

            // User, Msg, Retweet Count, Date, Hashtag list, Media list, User mentions, URLs, lat, long
            using (StreamReader sr = new StreamReader(stream))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    List<string> entries = CSVHelper.CSVLineSplit(line);
                    if (entries.Count < 8)
                    {
                        continue;
                    }

                    // person 
                    int senderId = -1;
                    List<List<object>> val = ExecuteQuery(
                        "select id from tweet.person where name = @name",
                        "@name", entries[0].Trim());

                    if (val.Count > 0)
                    {
                        senderId = (int)val[0][0];
                    }
                    else
                    {
                        senderId = nextPersonId++;
                        personCommand.CommandText += string.Format(
                            "INSERT INTO tweet.person (id, name) " +
                            "VALUES (@id_{0}, @name_{0})", senderId);

                        personCommand.Parameters.Add(new SqlParameter("@id_" + senderId, senderId));
                        personCommand.Parameters.Add(new SqlParameter("@name_" + senderId, entries[0].Trim()));
                    }


                    // message
                    int messageId = nextMessageId++;
                    messageCommand.CommandText += string.Format(
                            "INSERT INTO tweet.message (id, date, text, retweet_count, location, person_id) " +
                            "VALUES (@id_{0}, @date_{0}, @text_{0}, @retweet_count_{0},  geography::Parse(@location_{0}), @person_id_{0})", messageId);
                    
                    string pattern = "ddd MMM dd HH:mm:ss yyyy";
                    DateTime parsedDate = DateTime.MaxValue;
                    string dateString = entries[3].Trim().Replace("EST ", "");
                    if (dateString.Length > 4)
                    {
                        DateTime.TryParseExact(dateString, pattern, null, DateTimeStyles.None, out parsedDate);
                    }
                    // User, Msg, Retweet Count, Date, Hashtag list, Media list, User mentions, URLs, lat, long


                    messageCommand.Parameters.Add(new SqlParameter("@id_" + messageId, messageId));
                    messageCommand.Parameters.Add(new SqlParameter("@date_" + messageId, parsedDate));
                    messageCommand.Parameters.Add(new SqlParameter("@text_" + messageId, entries[1].Trim()));
                    messageCommand.Parameters.Add(new SqlParameter("@retweet_count_" + messageId, int.Parse(entries[2].Trim())));
                    messageCommand.Parameters.Add(new SqlParameter("@location_" + messageId,  "POINT(" + entries[9].Trim() + " " + entries[8].Trim() + ")"));
                    messageCommand.Parameters.Add(new SqlParameter("@person_id_" + messageId, senderId));


                    // hashtag  
                    string[] hashtags = entries[4].Substring(1, entries[4].Length - 1).Split(new string[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var hashtag in hashtags)
                    {
                        int hashtagId = -1;
                        val = ExecuteQuery(
                            "select id from tweet.hashtag where lower(name) = @name",
                            "@name", hashtag);

                        if (val.Count > 0)
                        {
                            hashtagId = (int)val[0][0];
                        }
                        else
                        {
                            hashtagId = nextHashtagId++;
                            hashtagCommand.CommandText += string.Format(
                                "INSERT INTO tweet.hashtag (id, name) " +
                                "VALUES (@id_{0}, @name_{0})", hashtagId);

                            hashtagCommand.Parameters.Add(new SqlParameter("@id_" + hashtagId, hashtagId));
                            hashtagCommand.Parameters.Add(new SqlParameter("@name_" + hashtagId, hashtag.ToLower()));
                        }

                        int hashtagUsageId = nextHashtagUsageId++;
                        hashtagUsageCommand.CommandText += string.Format(
                                "INSERT INTO tweet.hashtag_usage (id, message_id, hashtag_id) " +
                                "VALUES (@id_{0}, @message_id_{0}, @hashtag_id_{0})", hashtagUsageId);

                        hashtagUsageCommand.Parameters.Add(new SqlParameter("@id_" + hashtagUsageId, hashtagUsageId));
                        hashtagUsageCommand.Parameters.Add(new SqlParameter("@message_id_" + hashtagUsageId, messageId));
                        hashtagUsageCommand.Parameters.Add(new SqlParameter("@hashtag_id_" + hashtagUsageId, hashtagId));
                    }

                    // mention  
                    string[] mentions = entries[6].Substring(1, entries[6].Length - 1).Split(new string[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var mention in mentions)
                    {
                        int mentionPersonId = -1;
                        val = ExecuteQuery(
                            "select id from tweet.person where name = @name",
                            "@name", mention);

                        if (val.Count > 0)
                        {
                            mentionPersonId = (int)val[0][0];
                        }
                        else
                        {
                            mentionPersonId = nextPersonId++;
                            personCommand.CommandText += string.Format(
                                "INSERT INTO tweet.person (id, name) " +
                                "VALUES (@id_{0}, @name_{0})", mentionPersonId);

                            personCommand.Parameters.Add(new SqlParameter("@id_" + mentionPersonId, mentionPersonId));
                            personCommand.Parameters.Add(new SqlParameter("@name_" + mentionPersonId, mention));
                        }

                        int mentionId = nextMentionId++;
                        mentionCommand.CommandText += string.Format(
                                "INSERT INTO tweet.mention (id, message_id, person_id) " +
                                "VALUES (@id_{0}, @message_id_{0}, @person_id_{0})", mentionId);

                        mentionCommand.Parameters.Add(new SqlParameter("@id_" + mentionId, mentionId));
                        mentionCommand.Parameters.Add(new SqlParameter("@message_id_" + mentionId, messageId));
                        mentionCommand.Parameters.Add(new SqlParameter("@person_id_" + mentionId, mentionPersonId));
                    }

                    // media  
                    string[] medias = entries[5].Substring(1, entries[5].Length - 1).Split(new string[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var media in medias)
                    {
                        int mediaId = -1;
                        val = ExecuteQuery(
                            "select id from tweet.media where lower(url) = @url",
                            "@url", media);

                        if (val.Count > 0)
                        {
                            mediaId = (int)val[0][0];
                        }
                        else
                        {
                            mediaId = nextMediaId++;
                            mediaCommand.CommandText += string.Format(
                                "INSERT INTO tweet.media (id, url) " +
                                "VALUES (@id_{0}, @url_{0})", mediaId);

                            mediaCommand.Parameters.Add(new SqlParameter("@id_" + mediaId, mediaId));
                            mediaCommand.Parameters.Add(new SqlParameter("@url_" + mediaId, media.ToLower()));
                        }

                        int mediaUsageId = nextMediaUsageId++;
                        mediaUsageCommand.CommandText += string.Format(
                                "INSERT INTO tweet.media_usage (id, message_id, media_id) " +
                                "VALUES (@id_{0}, @message_id_{0}, @media_id_{0})", mediaUsageId);

                        mediaUsageCommand.Parameters.Add(new SqlParameter("@id_" + mediaUsageId, mediaUsageId));
                        mediaUsageCommand.Parameters.Add(new SqlParameter("@message_id_" + mediaUsageId, messageId));
                        mediaUsageCommand.Parameters.Add(new SqlParameter("@media_id_" + mediaUsageId, mediaId));
                    }

                    // url  
                    string[] urls = entries[7].Substring(1, entries[7].Length - 1).Split(new string[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var url in urls)
                    {
                        int urlId = -1;
                        val = ExecuteQuery(
                            "select id from tweet.url where lower(url) = @url",
                            "@url", url);

                        if (val.Count > 0)
                        {
                            urlId = (int)val[0][0];
                        }
                        else
                        {
                            urlId = nextUrlId++;
                            urlCommand.CommandText += string.Format(
                                "INSERT INTO tweet.url (id, url) " +
                                "VALUES (@id_{0}, @url_{0})", urlId);

                            urlCommand.Parameters.Add(new SqlParameter("@id_" + urlId, urlId));
                            urlCommand.Parameters.Add(new SqlParameter("@url_" + urlId, url.ToLower()));
                        }

                        int urlUsgaeId = nextUrlUsageId++;
                        urlUsageCommand.CommandText += string.Format(
                                "INSERT INTO tweet.url_usage (id, message_id, url_id) " +
                                "VALUES (@id_{0}, @message_id_{0}, @url_id_{0})", urlUsgaeId);

                        urlUsageCommand.Parameters.Add(new SqlParameter("@id_" + urlUsgaeId, urlUsgaeId));
                        urlUsageCommand.Parameters.Add(new SqlParameter("@message_id_" + urlUsgaeId, messageId));
                        urlUsageCommand.Parameters.Add(new SqlParameter("@url_id_" + urlUsgaeId, urlId));
                    }

                    currentBatchCount++;
                    if (currentBatchCount >= batchSize)
                    {
                        if (personCommand.CommandText != "")
                        {
                            personCommand.ExecuteNonQuery();
                        }
                        messageCommand.ExecuteNonQuery();
                        if (hashtagCommand.CommandText != "")
                        {
                            hashtagCommand.ExecuteNonQuery();
                        }
                        if (hashtagUsageCommand.CommandText != "")
                        {
                            hashtagUsageCommand.ExecuteNonQuery();
                        }
                        if (mentionCommand.CommandText != "")
                        {
                            mentionCommand.ExecuteNonQuery();
                        }
                        if (mediaCommand.CommandText != "")
                        {
                            mediaCommand.ExecuteNonQuery();
                        }
                        if (mediaUsageCommand.CommandText != "")
                        {
                            mediaUsageCommand.ExecuteNonQuery();
                        }
                        if (urlCommand.CommandText != "")
                        {
                            urlCommand.ExecuteNonQuery();
                        }
                        if (urlUsageCommand.CommandText != "")
                        {
                            urlUsageCommand.ExecuteNonQuery();
                        }

                        currentBatchCount = 0;


                        personCommand = connection.CreateCommand();
                        messageCommand = connection.CreateCommand();
                        hashtagCommand = connection.CreateCommand();
                        hashtagUsageCommand = connection.CreateCommand();
                        mentionCommand = connection.CreateCommand();
                        mediaCommand = connection.CreateCommand();
                        mediaUsageCommand = connection.CreateCommand();
                        urlCommand = connection.CreateCommand();
                        urlUsageCommand = connection.CreateCommand();
                    }

                }
            }

            if (personCommand.CommandText != "")
            {
                personCommand.ExecuteNonQuery();
            }
            messageCommand.ExecuteNonQuery();
            if (hashtagCommand.CommandText != "")
            {
                hashtagCommand.ExecuteNonQuery();
            }
            if (hashtagUsageCommand.CommandText != "")
            {
                hashtagUsageCommand.ExecuteNonQuery();
            }
            if (mentionCommand.CommandText != "")
            {
                mentionCommand.ExecuteNonQuery();
            }
            if (mediaCommand.CommandText != "")
            {
                mediaCommand.ExecuteNonQuery();
            }
            if (mediaUsageCommand.CommandText != "")
            {
                mediaUsageCommand.ExecuteNonQuery();
            }
            if (urlCommand.CommandText != "")
            {
                urlCommand.ExecuteNonQuery();
            }
            if (urlUsageCommand.CommandText != "")
            {
                urlUsageCommand.ExecuteNonQuery();
            }


        }

        private static void createTables()
        {
            dropConstraintsAndTable("tweet", "tweet.person");
            ExecuteNonQuery(
               "CREATE TABLE tweet.person (" +
               "id int NOT NULL," +
               "name nvarchar(255) NOT NULL," +
               "CONSTRAINT PK_tweet_person PRIMARY KEY (id))"
            );

            dropConstraintsAndTable("tweet", "tweet.message");
            ExecuteNonQuery(
               "CREATE TABLE tweet.message (" +
               "id int NOT NULL," +
               "date date NOT NULL," +
               "text nvarchar(255) NOT NULL," +
               "retweet_count int NOT NULL," +
               "location geography NOT NULL," +
               "person_id int NOT NULL," +
               "CONSTRAINT PK_tweet_message PRIMARY KEY (id)," +
               "CONSTRAINT FK_tweet_message_person FOREIGN KEY (person_id) REFERENCES tweet.person (id) ON DELETE CASCADE)"
            );

            dropConstraintsAndTable("tweet", "tweet.mention");
            ExecuteNonQuery(
               "CREATE TABLE tweet.mention (" +
               "id int NOT NULL," +
               "message_id int NOT NULL," +
               "person_id int NOT NULL," +
               "CONSTRAINT PK_tweet_mention PRIMARY KEY (id)," +
               "CONSTRAINT FK_tweet_mention_message FOREIGN KEY (message_id) REFERENCES tweet.message (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_tweet_mention_person FOREIGN KEY (person_id) REFERENCES tweet.person (id) ON DELETE NO ACTION)"
            );

            dropConstraintsAndTable("tweet", "tweet.hashtag");
            ExecuteNonQuery(
               "CREATE TABLE tweet.hashtag (" +
               "id int NOT NULL," +
               "name nvarchar(255) NOT NULL," +
               "CONSTRAINT PK_tweet_hashtag PRIMARY KEY (id))");

            dropConstraintsAndTable("tweet", "tweet.hashtag_usage");
            ExecuteNonQuery(
               "CREATE TABLE tweet.hashtag_usage (" +
               "id int NOT NULL," +
               "message_id int NOT NULL," +
               "hashtag_id int NOT NULL," +
               "CONSTRAINT PK_tweet_hashtag_usage PRIMARY KEY (id)," +
               "CONSTRAINT FK_tweet_hashtag_usage_message FOREIGN KEY (message_id) REFERENCES tweet.message (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_tweet_hashtag_usage_hashtag FOREIGN KEY (hashtag_id) REFERENCES tweet.hashtag (id) ON DELETE CASCADE)"
            );

            dropConstraintsAndTable("tweet", "tweet.media");
            ExecuteNonQuery(
               "CREATE TABLE tweet.media (" +
               "id int NOT NULL," +
               "url nvarchar(255) NOT NULL," +
               "CONSTRAINT PK_tweet_media PRIMARY KEY (id))");

            dropConstraintsAndTable("tweet", "tweet.media_usage");
            ExecuteNonQuery(
               "CREATE TABLE tweet.media_usage (" +
               "id int NOT NULL," +
               "message_id int NOT NULL," +
               "media_id int NOT NULL," +
               "CONSTRAINT PK_tweet_media_usage PRIMARY KEY (id)," +
               "CONSTRAINT FK_tweet_media_usage_message FOREIGN KEY (message_id) REFERENCES tweet.message (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_tweet_media_usage_media FOREIGN KEY (media_id) REFERENCES tweet.media (id) ON DELETE CASCADE)"
            );

            dropConstraintsAndTable("tweet", "tweet.url");
            ExecuteNonQuery(
               "CREATE TABLE tweet.url (" +
               "id int NOT NULL," +
               "url nvarchar(255) NOT NULL," +
               "CONSTRAINT PK_tweet_url PRIMARY KEY (id))");

            dropConstraintsAndTable("tweet", "tweet.url_usage");
            ExecuteNonQuery(
               "CREATE TABLE tweet.url_usage (" +
               "id int NOT NULL," +
               "message_id int NOT NULL," +
               "url_id int NOT NULL," +
               "CONSTRAINT PK_tweet_url_usage PRIMARY KEY (id)," +
               "CONSTRAINT FK_tweet_url_usage_message FOREIGN KEY (message_id) REFERENCES tweet.message (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_tweet_url_usage_url FOREIGN KEY (url_id) REFERENCES tweet.url (id) ON DELETE CASCADE)"
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
