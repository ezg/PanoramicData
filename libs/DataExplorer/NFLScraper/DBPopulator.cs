using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NFLScraper
{    
    class DBPopulator
    {
        //update nfl.player set height = cast(cast(height as decimal(28,2)) as float)
        public static List<Game> Games = new List<Game>();
        public static List<Team> Teams = new List<Team>();
        public static List<Player> Players = new List<Player>();
        public static List<Contract> Contracts = new List<Contract>();
        public static SqlConnection connection = null;

        public static void Populate()
        {
            Games.Clear();
            Teams.Clear();
            Contracts.Clear();
            Players.Clear();

            Stream stream = File.Open("parser_output.txt", FileMode.Open);
            BinaryFormatter bFormatter = new BinaryFormatter();
            Games = (List<Game>) bFormatter.Deserialize(stream);
            Teams = (List<Team>) bFormatter.Deserialize(stream);
            Contracts = (List<Contract>)bFormatter.Deserialize(stream);
            Players = (List<Player>)bFormatter.Deserialize(stream);
            stream.Close();

            //SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
            //    "Server=tcp:nog7tpedho.database.windows.net,1433;Database=panoramicdata;User ID=panoramicdata@nog7tpedho;Password=Browngfx1;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;");

            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
               @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");
            
            
            connection = new SqlConnection(stringBuilder.ToString());
            connection.Open();

            /*insertPlayers();
            insertTeams();
            insertContracts();
            insertGames();
            insertStats();*/

            createMetaTables();

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

        private static void insertPlayers()
        {
            dropConstraintsAndTable("nfl", "nfl.player");

            SqlCommand cmd = connection.CreateCommand();

            // create table
            cmd.CommandText =
               "CREATE TABLE nfl.player (" +
               "id int NOT NULL," +
               "name nvarchar(255) NOT NULL," +
               "position nvarchar(255) NOT NULL," +
               "height float," +
               "weight float," +
               "birth_day date," +
               "CONSTRAINT PK_nfl_player PRIMARY KEY (id))";
            cmd.ExecuteNonQuery();

            int nextId = 1;
            int batchSize = 200;
            int currentBatchCount = 0;
            cmd = connection.CreateCommand();
            foreach (var p in Players)
            {
                p.Id = nextId++;

                int pos = p.Height.IndexOf('-');
                string[] splits = p.Height.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                float? cm = null;
                if (splits.Length > 0)
                {
                    cm = (float.Parse(splits[0]) * 12.0f + float.Parse(splits[1])) * 2.54f;
                }
                float? kg = null;
                if (p.Weight != "")
                {
                    try
                    {
                        kg = float.Parse(p.Weight.Substring(0, 3)) / 2.20462f;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("weird structure... " + p.Name);
                    }
                }

                if (p.Name.Trim() == "")
                {
                    Console.WriteLine("no name, skipped... " + p);
                    continue;
                }
                if (p.Id == 566)
                {
                    Console.WriteLine();
                }

                //"January 15, 1979 in Austin, TX"
                int index = p.Born.IndexOf("in");
                string pattern = "MMMM d, yyyy";
                DateTime parsedDate = DateTime.MaxValue;
                string dateString = p.Born.Substring(0, index == -1 ? p.Born.Length : index -1);
                if (dateString.Length > 4)
                {
                    DateTime.TryParseExact(dateString, pattern, null, DateTimeStyles.None, out parsedDate);
                }

                cmd.CommandText += string.Format("INSERT INTO nfl.player (id, name, position, height, weight, birth_day) " +
                    "VALUES (@id_{0}, @name_{0}, @position_{0}, @height_{0}, @weight_{0}, @birth_day_{0})", p.Id);

                cmd.Parameters.Add(new SqlParameter("@id_" + p.Id, p.Id));
                cmd.Parameters.Add(new SqlParameter("@name_" + p.Id, p.Name.Trim()));
                cmd.Parameters.Add(new SqlParameter("@position_" + p.Id, p.Position.Replace("&nbsp;", "").Trim()));
                if (cm == null)
                {
                    cmd.Parameters.Add(new SqlParameter("@height_" + p.Id, DBNull.Value));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@height_" + p.Id, cm));
                }
                if (kg == null)
                {
                    cmd.Parameters.Add(new SqlParameter("@weight_" + p.Id, DBNull.Value));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@weight_" + p.Id, kg));
                }
                if (parsedDate == DateTime.MaxValue)
                {
                    cmd.Parameters.Add(new SqlParameter("@birth_day_" + p.Id, DBNull.Value));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@birth_day_" + p.Id, parsedDate));
                }

                currentBatchCount++;
                if (currentBatchCount >= batchSize)
                {
                    cmd.ExecuteNonQuery();
                    currentBatchCount = 0;
                    cmd = connection.CreateCommand();
                }
            }
            
            cmd.ExecuteNonQuery();

        }


        private static void insertTeams()
        {
            dropConstraintsAndTable("nfl", "nfl.team");

            SqlCommand cmd = connection.CreateCommand();
            
            // create table
            cmd.CommandText =
               "CREATE TABLE nfl.team (" +
               "id int NOT NULL," +
               "name nvarchar(255) NOT NULL," +
               "abbreviation nvarchar(255) NOT NULL, " +
               "location geography NOT NULL " +
               "CONSTRAINT PK_nfl_team PRIMARY KEY (id))";
            cmd.ExecuteNonQuery();

            int nextId = 1;
            int batchSize = 5;
            int currentBatchCount = 0;
            cmd = connection.CreateCommand();
            foreach (var t in Teams)
            {
                t.Id = nextId++;

                string city = "";
                switch (t.Name)
                {
                    case "Green Bay Packers":
                        city = "Green Bay, Wisconsin";
                        break;
                    case "New Orleans Saints":
                        city = "New Orleans, LA";
                        break;
                    case "Detroit Lions":
                        city = "Detroit, MI";
                        break;
                    case "Tampa Bay Buccaneers":
                        city = "Tampa Bay, FL";
                        break;
                    case "Buffalo Bills":
                        city = "Buffalo, New York, NY";
                        break;
                    case "Kansas City Chiefs":
                        city = "Kansas City, Missouri";
                        break;
                    case "Chicago Bears":
                        city = "Chicago, Illinois";
                        break;
                    case "Atlanta Falcons":
                        city = "Atlanta, Georgia";
                        break;
                    case "Houston Texans":
                        city = "Houston, Texas";
                        break;
                    case "Indianapolis Colts":
                        city = "Indianapolis, Indiana";
                        break;
                    case "Cincinnati Bengals":
                        city = "Cincinnati, Ohio";
                        break;
                    case "Cleveland Browns":
                        city = "Cleveland, Ohio";
                        break;
                    case "San Diego Chargers":
                        city = "San Diego Chargers";
                        break;
                    case "Minnesota Vikings":
                        city = "Minneapolis, Minnesota";
                        break;
                    case "New York Jets":
                        city = "East Rutherford, New Jersey";
                        break;
                    case "Dallas Cowboys":
                        city = "Dallas, Texas";
                        break;
                    case "Baltimore Ravens":
                        city = "Baltimore, Maryland";
                        break;
                    case "Pittsburgh Steelers":
                        city = "Pittsburgh, Pennsylvania";
                        break;
                    case "Washington Redskins":
                        city = "Washington DC";
                        break;
                    case "New York Giants":
                        city = "New York, New York";
                        break;
                    case "San Francisco 49ers":
                        city = "San Francisco, California";
                        break;
                    case "Seattle Seahawks":
                        city = "Seattle, Washington";
                        break;
                    case "Philadelphia Eagles":
                        city = "Philadelphia, Pennsylvania";
                        break;
                    case "St. Louis Rams":
                        city = "St, Louis, Missouri";
                        break;
                    case "Arizona Cardinals":
                        city = "Glendale, Arizona ";
                        break;
                    case "Carolina Panthers":
                        city = "Charlotte, North Carolina";
                        break;
                    case "Jacksonville Jaguars":
                        city = "Jacksonville, Florida";
                        break;
                    case "Tennessee Titans":
                        city = "Nashville, Tennessee";
                        break;
                    case "New England Patriots":
                        city = "Boston, Massachusetts";
                        break;
                    case "Miami Dolphins":
                        city = "Miami, Florida";
                        break;
                    case "Oakland Raiders":
                        city = "Oakland, California";
                        break;
                    case "Denver Broncos":
                        city = "Denver, Colorado";
                        break;
                }

                HttpClient httpClient = new HttpClient();
                var responseMessage = httpClient.GetAsync(
                    "http://maps.googleapis.com/maps/api/geocode/json?address=" + city + "&sensor=true").Result;
                //Console.WriteLine(JsonConvert.("select * form nfl.stats"));
                string ret = responseMessage.Content.ReadAsStringAsync().Result;
                dynamic obj = JsonConvert.DeserializeObject(ret);
                
                var lat = obj.results.Count > 0 ? obj.results[0].geometry.location.lat : 0;
                var lng = obj.results.Count > 0 ? obj.results[0].geometry.location.lng : 0;

                //update nfl.team set location2 = LTRIM(str(location.Long, 25, 7)) + ', ' + LTRIM(str(location.Lat, 25, 7))

                cmd.CommandText += string.Format("INSERT INTO nfl.team (id, name, abbreviation, location) " +
                    "VALUES (@id_{0}, @name_{0}, @abbreviation_{0}, geography::Parse(@location_{0}))", t.Id);

                cmd.Parameters.Add(new SqlParameter("@id_" + t.Id, t.Id));
                cmd.Parameters.Add(new SqlParameter("@name_" + t.Id, t.Name.Trim()));
                cmd.Parameters.Add(new SqlParameter("@abbreviation_" + t.Id, t.Abbreviation.Trim()));
                cmd.Parameters.Add(new SqlParameter("@location_" + t.Id, "POINT(" + lng + " " + lat + ")"));
                
                currentBatchCount++;
                if (currentBatchCount >= batchSize)
                {
                    cmd.ExecuteNonQuery();
                    currentBatchCount = 0;
                    cmd = connection.CreateCommand();
                }
            }

            cmd.ExecuteNonQuery();

        }

        private static void insertContracts()
        {
            dropConstraintsAndTable("nfl", "nfl.player_contract");

            SqlCommand cmd = connection.CreateCommand();

            // create table
            cmd.CommandText =
               "CREATE TABLE nfl.player_contract (" +
               "id int NOT NULL," +
               "start_date date NOT NULL," +
               "end_date date NOT NULL," +
               "player_id int NOT NULL," +
               "team_id int NOT NULL," +
               "CONSTRAINT PK_nfl_player_contract PRIMARY KEY (id)," +
               "CONSTRAINT FK_nfl_player_contract_player FOREIGN KEY (player_id) REFERENCES nfl.player (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_nfl_player_contract_team FOREIGN KEY (team_id) REFERENCES nfl.team (id) ON DELETE CASCADE)";
            cmd.ExecuteNonQuery();

            int nextId = 1;
            int batchSize = 100;
            int currentBatchCount = 0;
            cmd = connection.CreateCommand();
            foreach (var c in Contracts)
            {
                c.Id = nextId++;

                Team team = null;
                foreach (var t in Teams)
                {
                    if (t.Abbreviation == c.Team.Abbreviation)
                    {
                        team = t;
                        break;
                    }
                }

                Player player = null;
                foreach (var p in Players)
                {
                    if (p.Name == c.Player.Name && 
                        p.Born == c.Player.Born)
                    {
                        player = p;
                        break;
                    }
                }

                if (player == null || team == null)
                {
                    Console.WriteLine("not good - contract");
                    continue;
                }


                cmd.CommandText += string.Format("INSERT INTO nfl.player_contract (id, start_date, end_date, player_id, team_id) " +
                    "VALUES (@id_{0}, @start_date_{0}, @end_date_{0}, @player_id_{0}, @team_id_{0})", c.Id);

                cmd.Parameters.Add(new SqlParameter("@id_" + c.Id, c.Id));
                cmd.Parameters.Add(new SqlParameter("@start_date_" + c.Id, c.StartDate));
                cmd.Parameters.Add(new SqlParameter("@end_date_" + c.Id, c.EndDate));
                cmd.Parameters.Add(new SqlParameter("@player_id_" + c.Id, player.Id));
                cmd.Parameters.Add(new SqlParameter("@team_id_" + c.Id, team.Id));

                currentBatchCount++;
                if (currentBatchCount >= batchSize)
                {
                    cmd.ExecuteNonQuery();
                    currentBatchCount = 0;
                    cmd = connection.CreateCommand();
                }
            }

            cmd.ExecuteNonQuery();

        }

        private static void insertGames()
        {
            SqlCommand cmd = connection.CreateCommand();

            // game_team table
            dropConstraintsAndTable("nfl", "nfl.game_team");
            dropConstraintsAndTable("nfl", "nfl.game");


            // create table
            cmd.CommandText =
               "CREATE TABLE nfl.game (" +
               "id int NOT NULL," +
               "week int NOT NULL," +
               "game_date date NOT NULL," +
               "start_time nvarchar(255)," +
               "duration nvarchar(255) NOT NULL," +
               "attendance int NOT NULL," +
               "weather nvarchar(1024)," +
               "vegas_line nvarchar(1024) NOT NULL," +
               "CONSTRAINT PK_nfl_game PRIMARY KEY (id))";
            cmd.ExecuteNonQuery();


            cmd = connection.CreateCommand();
            cmd.CommandText =
               "CREATE TABLE nfl.game_team (" +
               "id int NOT NULL," +
               "game_id int NOT NULL," +
               "team_id int NOT NULL," +
               "pts int NOT NULL," +
               "won bit NOT NULL," +
               "CONSTRAINT PK_nfl_game_team PRIMARY KEY (id)," +
               "CONSTRAINT FK_nfl_game_team_team FOREIGN KEY (team_id) REFERENCES nfl.team (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_nfl_game_team_game FOREIGN KEY (game_id) REFERENCES nfl.game (id) ON DELETE CASCADE)";
            cmd.ExecuteNonQuery();

            int nextTeamId = 1;
            int nextGameTeamId = 1;
            int batchSize = 1;
            int currentBatchCount = 0;
            cmd = connection.CreateCommand();
            foreach (var g in Games)
            {
                g.Id = nextTeamId++;

                Team teamWinner = null;
                Team teamLoser = null;
                foreach (var t in Teams)
                {
                    if (t.Abbreviation == g.Loser.Abbreviation)
                    {
                        teamLoser = t;
                    }
                    if (t.Abbreviation == g.Winner.Abbreviation)
                    {
                        teamWinner = t;
                    }
                }

                cmd.CommandText += string.Format("INSERT INTO nfl.game (" +
                    "id, week, game_date, start_time, duration, attendance, weather, vegas_line) " +
                    "VALUES (@id_{0}, @week_{0}, @game_date_{0}, " +
                            "@start_time_{0}, @duration_{0}, @attendance_{0}, @weather_{0}, @vegas_line_{0});", g.Id);

                cmd.Parameters.Add(new SqlParameter("@id_" + g.Id, g.Id));
                cmd.Parameters.Add(new SqlParameter("@week_" + g.Id, g.Week));
                cmd.Parameters.Add(new SqlParameter("@game_date_" + g.Id, g.Date));

                if (g.StartTime == null || g.StartTime == "")
                {
                    cmd.Parameters.Add(new SqlParameter("@start_time_" + g.Id, DBNull.Value));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@start_time_" + g.Id, g.StartTime));
                }
                cmd.Parameters.Add(new SqlParameter("@duration_" + g.Id, g.Duration));
                cmd.Parameters.Add(new SqlParameter("@attendance_" + g.Id, g.Attendance));
                if (g.Weather == null)
                {
                    cmd.Parameters.Add(new SqlParameter("@weather_" + g.Id, DBNull.Value));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@weather_" + g.Id, g.Weather));
                }
                cmd.Parameters.Add(new SqlParameter("@vegas_line_" + g.Id, g.VegasLine));

                // winner team / game_team
                int gameTeamId = nextGameTeamId++;
                cmd.CommandText += string.Format("INSERT INTO nfl.game_team (" +
                    "id, won, game_id, team_id, pts) " +
                    "VALUES (@game_team_id_{0}, @game_team_won_{0}, @game_team_game_id_{0}, @game_team_team_id{0}, @game_team_pts_{0});", gameTeamId);

                cmd.Parameters.Add(new SqlParameter("@game_team_id_" + gameTeamId, gameTeamId));
                cmd.Parameters.Add(new SqlParameter("@game_team_won_" + gameTeamId, true));
                cmd.Parameters.Add(new SqlParameter("@game_team_game_id_" + gameTeamId, g.Id));
                cmd.Parameters.Add(new SqlParameter("@game_team_team_id" + gameTeamId, teamWinner.Id));
                cmd.Parameters.Add(new SqlParameter("@game_team_pts_" + gameTeamId, g.PtsWinner));

                // loser team / game_team
                gameTeamId = nextGameTeamId++;
                cmd.CommandText += string.Format("INSERT INTO nfl.game_team (" +
                    "id, won, game_id, team_id, pts) " +
                    "VALUES (@game_team_id_{0}, @game_team_won_{0}, @game_team_game_id_{0}, @game_team_team_id{0}, @game_team_pts_{0});", gameTeamId);

                cmd.Parameters.Add(new SqlParameter("@game_team_id_" + gameTeamId, gameTeamId));
                cmd.Parameters.Add(new SqlParameter("@game_team_won_" + gameTeamId, false));
                cmd.Parameters.Add(new SqlParameter("@game_team_game_id_" + gameTeamId, g.Id));
                cmd.Parameters.Add(new SqlParameter("@game_team_team_id" + gameTeamId, teamLoser.Id));
                cmd.Parameters.Add(new SqlParameter("@game_team_pts_" + gameTeamId, g.PtsLoser));

                currentBatchCount++;
                if (currentBatchCount >= batchSize)
                {
                    cmd.ExecuteNonQuery();
                    currentBatchCount = 0;
                    cmd = connection.CreateCommand();
                }
            }

            if (cmd.CommandText != "")
            {
                cmd.ExecuteNonQuery();
            }

        }


        private static void insertStats()
        {
            dropConstraintsAndTable("nfl", "nfl.stats");
            SqlCommand cmd = connection.CreateCommand();

            // create table stats
            cmd.CommandText =
               "CREATE TABLE nfl.stats (" +
               "id int NOT NULL," +
               
               "game_id int NOT NULL," +
               "player_id int NOT NULL," +

               "CONSTRAINT PK_nfl_stats PRIMARY KEY (id)," +
               "CONSTRAINT FK_nfl_stats_game FOREIGN KEY (game_id) REFERENCES nfl.game (id) ON DELETE CASCADE," +
               "CONSTRAINT FK_nfl_stats_player FOREIGN KEY (player_id) REFERENCES nfl.player (id) ON DELETE CASCADE)";
            cmd.ExecuteNonQuery();


            dropConstraintsAndTable("nfl", "nfl.stats_offense");
            cmd = connection.CreateCommand();

            // create table stats_offense
            cmd.CommandText =
               "CREATE TABLE nfl.stats_offense (" +
               "id int NOT NULL," +
               "passing_cmp float," +
               "passing_att float," +
               "passing_yds float," +
               "passing_td float," +
               "passing_int float," +
               "passing_lng float," +

               "rushing_att float," +
               "rushing_yds float," +
               "rushing_td float," +
               "rushing_lng float," +

               "receiving_rec float," +
               "receiving_yds float," +
               "receiving_td float," +
               "receiving_lng float," +

               "stats_id int NOT NULL," +
              
               "CONSTRAINT PK_nfl_stats_offense PRIMARY KEY (id)," +
               "CONSTRAINT FK_nfl_stats_offense_stats FOREIGN KEY (stats_id) REFERENCES nfl.stats (id) ON DELETE CASCADE)";
            cmd.ExecuteNonQuery();


            dropConstraintsAndTable("nfl", "nfl.stats_defense");
            cmd = connection.CreateCommand();

            // create table stats_defense
            cmd.CommandText =
               "CREATE TABLE nfl.stats_defense (" +
               "id int NOT NULL," +

               "defint_sk float," +
               "defint_int float," +
               "defint_yds float," +
               "defint_td float," +
               "defint_lng float," +

               "fumbles_fr float," +
               "fumbles_yds float," +
               "fumbles_td float," +
               "fumbles_ff float," +

               "kickret_rt float," +
               "kickret_yds float," +
               "kickret_y_rt float," +
               "kickret_td float," +
               "kickret_lng float," +

               "puntret_ret float," +
               "puntret_yds float," +
               "puntret_y_rt float," +
               "puntret_td float," +
               "puntret_lng float," +

               "stats_id int NOT NULL," +

               "CONSTRAINT PK_nfl_stats_defense PRIMARY KEY (id)," + 
               "CONSTRAINT FK_nfl_stats_defense_stats FOREIGN KEY (stats_id) REFERENCES nfl.stats (id) ON DELETE CASCADE)";
            cmd.ExecuteNonQuery();

            dropConstraintsAndTable("nfl", "nfl.stats_kicking");
            cmd = connection.CreateCommand();

            // create table stats_kicking
            cmd.CommandText =
               "CREATE TABLE nfl.stats_kicking (" +
               "id int NOT NULL," +
               
               "pat_xpm float," +
               "pat_xpa float," +
               "fg_fgm float," +
               "fg_fga float," +

               "punting_pnt float," +
               "punting_yds float," +
               "punting_y_p float," +
               "punting_lng float," +

               "stats_id int NOT NULL," +

               "CONSTRAINT PK_nfl_stats_kicking PRIMARY KEY (id)," +
               "CONSTRAINT FK_nfl_stats_kicking_stats FOREIGN KEY (stats_id) REFERENCES nfl.stats (id) ON DELETE CASCADE)";
            cmd.ExecuteNonQuery();

            int nextStatsId = 1;
            int nextStatsOffenseId = 1;
            int nextStatsDefenseId = 1;
            int nextStatKickingId = 1;
            int batchSize = 50;

            foreach (var g in Games)
            {
                int currentBatchCount = 0;
                cmd = connection.CreateCommand();

                foreach (var s in g.Stats)
                {
                    s.Id = nextStatsId++;

                    // stats
                    cmd.CommandText += string.Format(
                        "INSERT INTO nfl.stats (" +
                                "id," +
                                "game_id," +
                                "player_id) " +
                        "VALUES (" +
                                "@id_{0}," +
                                
                                "@game_id_{0}," +
                                "@player_id_{0})", s.Id);

                    Player player = null;
                    foreach (var p in Players)
                    {
                        if (p.Name == s.Player.Name &&
                            p.Born == s.Player.Born)
                        {
                            player = p;
                            break;
                        }
                    }

                    if (player == null)
                    {
                        Console.WriteLine("not good - stats");
                        continue;
                    }

                    addSqlParam(cmd, "id", s.Id, s.Id);
                    addSqlParam(cmd, "game_id", s.Id, g.Id);
                    addSqlParam(cmd, "player_id", s.Id, player.Id);

                    // offense 
                    if (s.Passing_Cmp != null ||
                        s.Passing_Att != null ||
                        s.Passing_Yds != null ||
                        s.Passing_TD != null ||
                        s.Passing_Int != null ||
                        s.Passing_Lng != null ||
                        s.Rushing_Att != null ||
                        s.Rushing_Yds != null ||
                        s.Rushing_TD != null ||
                        s.Rushing_Lng != null ||
                        s.Receiving_Rec != null ||
                        s.Receiving_Yds != null ||
                        s.Receiving_TD != null ||
                        s.Receiving_Lng != null)
                    {
                        int offenseId = nextStatsOffenseId++;
                        cmd.CommandText += string.Format(
                            "INSERT INTO nfl.stats_offense (" +
                                    "id," +
                                    "passing_cmp," +
                                    "passing_att," +
                                    "passing_yds," +
                                    "passing_td," +
                                    "passing_int," +
                                    "passing_lng," +

                                    "rushing_att," +
                                    "rushing_yds," +
                                    "rushing_td," +
                                    "rushing_lng," +

                                    "receiving_rec," +
                                    "receiving_yds," +
                                    "receiving_td," +
                                    "receiving_lng," +
                                    "stats_id)" +
                            "VALUES (" +
                                    "@id_offense_{0}," +
                                    "@passing_cmp_{0}," +
                                    "@passing_att_{0}," +
                                    "@passing_yds_{0}," +
                                    "@passing_td_{0}," +
                                    "@passing_int_{0}," +
                                    "@passing_lng_{0}," +

                                    "@rushing_att_{0}," +
                                    "@rushing_yds_{0}," +
                                    "@rushing_td_{0}," +
                                    "@rushing_lng_{0}," +

                                    "@receiving_rec_{0}," +
                                    "@receiving_yds_{0}," +
                                    "@receiving_td_{0}," +
                                    "@receiving_lng_{0}," +
                                    "@stats_id_offense_{0})", offenseId);


                        addSqlParam(cmd, "id_offense", offenseId, offenseId);
                        addSqlParam(cmd, "passing_cmp", offenseId, s.Passing_Cmp, true);
                        addSqlParam(cmd, "passing_att", offenseId, s.Passing_Att, true);
                        addSqlParam(cmd, "passing_yds", offenseId, s.Passing_Yds, true);
                        addSqlParam(cmd, "passing_td", offenseId, s.Passing_TD, true);
                        addSqlParam(cmd, "passing_int", offenseId, s.Passing_Int, true);
                        addSqlParam(cmd, "passing_lng", offenseId, s.Passing_Lng, true);

                        addSqlParam(cmd, "rushing_att", offenseId, s.Rushing_Att, true);
                        addSqlParam(cmd, "rushing_yds", offenseId, s.Rushing_Yds, true);
                        addSqlParam(cmd, "rushing_td", offenseId, s.Rushing_TD, true);
                        addSqlParam(cmd, "rushing_lng", offenseId, s.Rushing_Lng, true);

                        addSqlParam(cmd, "receiving_rec", offenseId, s.Receiving_Rec, true);
                        addSqlParam(cmd, "receiving_yds", offenseId, s.Receiving_Yds, true);
                        addSqlParam(cmd, "receiving_td", offenseId, s.Receiving_TD, true);
                        addSqlParam(cmd, "receiving_lng", offenseId, s.Receiving_Lng, true);
                        addSqlParam(cmd, "stats_id_offense", offenseId, s.Id, true);
                    }


                    // defense 
                    if (s.DefInt_Sk != null ||
                        s.DefInt_Int != null ||
                        s.DefInt_Yds != null ||
                        s.DefInt_TD != null ||
                        s.DefInt_Lng != null ||
                        s.Fumbles_FR != null ||
                        s.Fumbles_Yds != null ||
                        s.Fumbles_TD != null ||
                        s.Fumbles_FF != null ||
                        s.KickRet_Rt != null ||
                        s.KickRet_Yds != null ||
                        s.KickRet_Y_Rt != null ||
                        s.KickRet_TD != null ||
                        s.KickRet_Lng != null ||
                        s.PuntRet_Ret != null ||
                        s.PuntRet_Yds != null ||
                        s.PuntRet_Y_Rt != null ||
                        s.PuntRet_TD != null ||
                        s.PuntRet_Lng != null)
                    {
                        int defenseId = nextStatsDefenseId++;
                        cmd.CommandText += string.Format(
                            "INSERT INTO nfl.stats_defense (" +
                                    "id," +
                                    "defint_sk," +
                                    "defint_int," +
                                    "defint_yds," +
                                    "defint_td," +
                                    "defint_lng," +

                                    "fumbles_fr," +
                                    "fumbles_yds," +
                                    "fumbles_td," +
                                    "fumbles_ff," +

                                    "kickret_rt," +
                                    "kickret_yds," +
                                    "kickret_y_rt," +
                                    "kickret_td," +
                                    "kickret_lng," +

                                    "puntret_ret," +
                                    "puntret_yds," +
                                    "puntret_y_rt," +
                                    "puntret_td," +
                                    "puntret_lng," +
                                    "stats_id)" +
                            "VALUES (" +
                                    "@id_defense_{0}," +
                                    "@defint_sk_{0}," +
                                    "@defint_int_{0}," +
                                    "@defint_yds_{0}," +
                                    "@defint_td_{0}," +
                                    "@defint_lng_{0}," +

                                    "@fumbles_fr_{0}," +
                                    "@fumbles_yds_{0}," +
                                    "@fumbles_td_{0}," +
                                    "@fumbles_ff_{0}," +

                                    "@kickret_rt_{0}," +
                                    "@kickret_yds_{0}," +
                                    "@kickret_y_rt_{0}," +
                                    "@kickret_td_{0}," +
                                    "@kickret_lng_{0}," +

                                    "@puntret_ret_{0}," +
                                    "@puntret_yds_{0}," +
                                    "@puntret_y_rt_{0}," +
                                    "@puntret_td_{0}," +
                                    "@puntret_lng_{0}," +
                                    "@stats_id_defense_{0})", defenseId);


                        addSqlParam(cmd, "id_defense", defenseId, defenseId);
                        addSqlParam(cmd, "defint_sk", defenseId, s.DefInt_Sk, true);
                        addSqlParam(cmd, "defint_int", defenseId, s.DefInt_Int, true);
                        addSqlParam(cmd, "defint_yds", defenseId, s.DefInt_Yds, true);
                        addSqlParam(cmd, "defint_td", defenseId, s.DefInt_TD, true);
                        addSqlParam(cmd, "defint_lng", defenseId, s.DefInt_Lng, true);

                        addSqlParam(cmd, "fumbles_fr", defenseId, s.Fumbles_FR, true);
                        addSqlParam(cmd, "fumbles_yds", defenseId, s.Fumbles_Yds, true);
                        addSqlParam(cmd, "fumbles_td", defenseId, s.Fumbles_TD, true);
                        addSqlParam(cmd, "fumbles_ff", defenseId, s.Fumbles_FF, true);

                        addSqlParam(cmd, "kickret_rt", defenseId, s.KickRet_Rt, true);
                        addSqlParam(cmd, "kickret_yds", defenseId, s.KickRet_Yds, true);
                        addSqlParam(cmd, "kickret_y_rt", defenseId, s.KickRet_Y_Rt, true);
                        addSqlParam(cmd, "kickret_td", defenseId, s.KickRet_TD, true);
                        addSqlParam(cmd, "kickret_lng", defenseId, s.KickRet_Lng, true);

                        addSqlParam(cmd, "puntret_ret", defenseId, s.PuntRet_Ret, true);
                        addSqlParam(cmd, "puntret_yds", defenseId, s.PuntRet_Yds, true);
                        addSqlParam(cmd, "puntret_y_rt", defenseId, s.PuntRet_Y_Rt, true);
                        addSqlParam(cmd, "puntret_td", defenseId, s.PuntRet_TD, true);
                        addSqlParam(cmd, "puntret_lng", defenseId, s.PuntRet_Lng, true);

                        addSqlParam(cmd, "stats_id_defense", defenseId, s.Id, true);
                    }

                    
                    // defense 
                    if (s.Pat_XPM != null ||
                        s.Pat_XPA != null ||
                        s.FG_FGM != null ||
                        s.FG_FGA != null ||
                        s.Punting_Pnt != null ||
                        s.Punting_Yds != null ||
                        s.Punting_Y_P != null ||
                        s.Punting_Lng != null)
                    {
                        int kickingId = nextStatKickingId++;
                        cmd.CommandText += string.Format(
                            "INSERT INTO nfl.stats_kicking (" +
                                    "id," +
                                    "pat_xpm," +
                                    "pat_xpa," +
                                    "fg_fgm," +
                                    "fg_fga," +

                                    "punting_pnt," +
                                    "punting_yds," +
                                    "punting_y_p," +
                                    "punting_lng," +
                                    "stats_id)" +
                            "VALUES (" +
                                    "@id_kicking_{0}," +
                                    "@pat_xpm_{0}," +
                                    "@pat_xpa_{0}," +
                                    "@fg_fgm_{0}," +
                                    "@fg_fga_{0}," +

                                    "@punting_pnt_{0}," +
                                    "@punting_yds_{0}," +
                                    "@punting_y_p_{0}," +
                                    "@punting_lng_{0}," +
                                    "@stats_id_kicking_{0})", kickingId);


                        addSqlParam(cmd, "id_kicking", kickingId, kickingId);

                        addSqlParam(cmd, "pat_xpm", kickingId, s.Pat_XPM, true);
                        addSqlParam(cmd, "pat_xpa", kickingId, s.Pat_XPA, true);
                        addSqlParam(cmd, "fg_fgm", kickingId, s.FG_FGM, true);
                        addSqlParam(cmd, "fg_fga", kickingId, s.FG_FGA, true);

                        addSqlParam(cmd, "punting_pnt", kickingId, s.Punting_Pnt, true);
                        addSqlParam(cmd, "punting_yds", kickingId, s.Punting_Yds, true);
                        addSqlParam(cmd, "punting_y_p", kickingId, s.Punting_Y_P, true);
                        addSqlParam(cmd, "punting_lng", kickingId, s.Punting_Lng, true);

                        addSqlParam(cmd, "stats_id_kicking", kickingId, s.Id, true);
                    }

                    currentBatchCount++;
                    if (currentBatchCount >= batchSize)
                    {
                        cmd.ExecuteNonQuery();
                        currentBatchCount = 0;
                        cmd = connection.CreateCommand();
                    }
                }
                if (cmd.CommandText != "")
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }


        /*
        alter table meta.field_info
        add visualization_type nvarchar(255);

        update meta.field_info set visualization_type = 'numeric' where data_type = 'float' or data_type = 'int';
        update meta.field_info set visualization_type = 'date' where data_type = 'date';
        update meta.field_info set visualization_type = 'time' where data_type = 'time';
        update meta.field_info set visualization_type = 'geography' where data_type = 'geography';
        update meta.field_info set visualization_type = 'category' where data_type = 'nvarchar';
        update meta.field_info set visualization_type = 'geography' where name = 'location';
        update meta.field_info set visualization_type = 'geography' where name = 'embarked';
        update meta.field_info set visualization_type = 'geography' where name = 'home';
        update meta.field_info set visualization_type = 'geography' where name = 'destination';
        update meta.field_info set visualization_type = 'enum' where name = 'sex';
        update meta.field_info set visualization_type = 'enum' where name = 'passenger_class';
        update meta.field_info set visualization_type = 'enum' where name = 'survived';
        update meta.field_info set visualization_type = 'enum' where name = 'sex';
        update meta.field_info set visualization_type = 'enum' where name = 'smoker';
        update meta.field_info set visualization_type = 'enum' where name = 'time';
        update meta.field_info set visualization_type = 'enum' where name = 'day';

        update meta.field_info set data_type = 'geography' where name = 'location';
        update meta.field_info set data_type = 'geography' where name = 'embarked';
        update meta.field_info set data_type = 'geography' where name = 'home';
        update meta.field_info set data_type = 'geography' where name = 'destination';
     

        update meta.field_info set data_type = 'geography' where name = 'sales_state';

        update meta.field_info set visualization_type = 'enum' where name = 'sales_division';
        update meta.field_info set visualization_type = 'enum' where name = 'product_group';
        update meta.field_info set visualization_type = 'enum' where name = 'product_specification';
        

        select * from meta.field_info where id >= 6000 and id < 7000;
         
        select 
	        'update meta.field_info set max_value = ' + 
	        '(select max(' + m.name + ') from ' + t.schema_name + '.' + t.name + ') ' +
	        'where id = ' + cast(m.id as nvarchar)
        from 
	        meta.field_info m, meta.table_info t 
        where 
	        m.table_id = t.id and m.visualization_type = 'numeric';
	
        select 
	        'update meta.field_info set min_value = ' + 
	        '(select min(' + m.name + ') from ' + t.schema_name + '.' + t.name + ') ' +
	        'where id = ' + cast(m.id as nvarchar)
        from 
	        meta.field_info m, meta.table_info t 
        where 
	        m.table_id = t.id and m.visualization_type = 'numeric';
	
        select 
	        'update meta.field_info set bin_size = ' + 
	        '(select ceiling((max(' + m.name + ') - min(' + m.name + ')) / 10.0) from ' + t.schema_name + '.' + t.name + ') ' +
	        'where id = ' + cast(m.id as nvarchar)
        from 
	        meta.field_info m, meta.table_info t 
        where 
	        m.table_id = t.id and m.visualization_type = 'numeric';
        
        select 
	        'update meta.field_info set bin_size = 0.1 ' +
	        'where id = ' + cast(m.id as nvarchar) + ';'
        from 
	        meta.field_info m, meta.table_info t 
        where 
	        m.table_id = t.id and m.visualization_type = 'numeric' and
			m.max_value = 1 and m.min_value = 0;
         
        ALTER TABLE meta.field_info ALTER COLUMN max_value float;
        ALTER TABLE meta.field_info ALTER COLUMN min_value float;
        ALTER TABLE meta.field_info ALTER COLUMN bin_size float;
        */
        private static void createMetaTables()
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "delete from meta.field_alias where id between 0 and 999;" +
                "delete from meta.field_info where id between 0 and 999;" +
                "delete from meta.table_alias where id between 0 and 999;" +
                "delete from meta.table_dependency where id between 0 and 999;" +
                "delete from meta.table_info where id between 0 and 999;";
            cmd.ExecuteNonQuery();

            if (false)
            {
                // create table_info
                dropConstraintsAndTable("meta", "meta.table_info");
                cmd.CommandText =
                   "CREATE TABLE meta.table_info (" +
                   "id int NOT NULL, " +
                   "name nvarchar(255) NOT NULL," +
                   "schema_name nvarchar(255) NOT NULL," +
                   "pk_field_info int NULL," +
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
            }

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmd.ExecuteNonQuery();

            Dictionary<string, int> tables = new Dictionary<string, int>();
            
            // insert records 
            int nextId = 1;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name from information_schema.tables where table_schema ='nfl'";
            SqlDataReader re = cmd.ExecuteReader();
            string insertStatements = "";
            while (re.Read())
            {
                insertStatements += "insert into meta.table_info (id, name, schema_name) values (" +
                    nextId++ + ", '" + re.GetString(0) + "', 'nfl');\n";

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


            // create field_info
            if (false)
            {
                cmd = connection.CreateCommand();
                dropConstraintsAndTable("meta", "meta.field_info");
                cmd.CommandText =
                   "CREATE TABLE meta.field_info (" +
                   "id int NOT NULL, " +
                   "name nvarchar(255) NOT NULL," +
                   "visible bit NOT NULL," +
                   "data_type nvarchar(255) NOT NULL," +
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
            }

            Dictionary<string, int> fields = new Dictionary<string, int>();

            // insert records 
            nextId = 1;
            cmd = connection.CreateCommand();
            cmd.CommandText = "select table_name, column_name, data_type from information_schema.columns where table_schema ='nfl'";
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

            // create table_dependency            
            if (false)
            {
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
            }

            // get all foreign key releations for a table
            nextId = 1;
            foreach (var table_name in tables.Keys)
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "" +
                    "select t.name as TableWithForeignKey, c.name as ForeignKeyColumn " + 
                    "from sys.foreign_key_columns as fk " + 
                    "inner join sys.tables as t on fk.parent_object_id = t.object_id " + 
                    "inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id " +
                    "where fk.referenced_object_id = (select object_id from sys.tables where name = '" + table_name + "' and schema_id = 5) " + 
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
