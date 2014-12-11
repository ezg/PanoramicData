using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NFLScraper
{
    class Parser
    {
        public static List<Game> Games = new List<Game>();
        public static List<Team> Teams = new List<Team>();
        public static List<Player> Players = new List<Player>();
        public static List<Contract> Contracts = new List<Contract>();

        public static void Parse(string site)
        {
            Games.Clear();
            Teams.Clear();
            Contracts.Clear();
            Players.Clear();
            parseGames(site);

            Stream stream = File.Open("parser_output.txt", FileMode.Create);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, Games);
            bFormatter.Serialize(stream, Teams);
            bFormatter.Serialize(stream, Contracts);
            bFormatter.Serialize(stream, Players);
            stream.Close();
        }

        private static void parseGames(string site)
        {

            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument doc = htmlWeb.Load(site);

            HtmlNode gameTable = doc.DocumentNode.SelectNodes("//table")[0];
            var rows = gameTable.SelectNodes("tbody/tr");

            int count = 0;
            foreach (var row in rows)
            {
                Console.WriteLine("Parsing game " + count + " of " + rows.Count);

                var cells = row.SelectNodes("td");
                if (cells != null && cells[0].InnerText.Trim() != "")
                {
                    Game g = new Game();
                    Games.Add(g);

                    int w = 99;
                    Int32.TryParse(cells[0].InnerText, out w);
                    g.Week = w;


                    string pattern = "MMMM d, yyyy";
                    DateTime parsedDate = DateTime.MaxValue;
                    DateTime.TryParseExact(cells[2].InnerText + ", 2011", pattern, null, DateTimeStyles.None, out parsedDate);
                    g.Date = parsedDate;
                    if (g.Date.Month == 1 || g.Date.Month == 2)
                    {
                        g.Date = g.Date.AddYears(1);
                    }

                    var a = cells[4].SelectNodes(".//a");
                    g.Winner = parseTeam("http://www.pro-football-reference.com" + a[0].Attributes[0].Value);

                    a = cells[6].SelectNodes(".//a");
                    g.Loser = parseTeam("http://www.pro-football-reference.com" + a[0].Attributes[0].Value);

                    g.PtsWinner = Int16.Parse(cells[7].InnerText);
                    g.PtsLoser = Int16.Parse(cells[8].InnerText);

                    a = cells[3].SelectNodes(".//a");

                    parseBoxscore("http://www.pro-football-reference.com" + a[0].Attributes[0].Value, g);
                }


                count++;
            }

        }

        private static void parseBoxscore(string site, Game game)
        {
            // load
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument doc = htmlWeb.Load(site);

            HtmlNodeCollection tables = doc.DocumentNode.SelectNodes("//table");

            // table 2:  extended game stats
            var rows = tables[2].SelectNodes("tr");
            foreach (var row in rows)
            {

                var cells = row.SelectNodes("td");
                if (cells != null && cells[0].InnerText.Trim() != "")
                {
                    if (cells[0].InnerText == "Start Time")
                    {
                        game.StartTime = cells[1].InnerText;
                    }
                    else if (cells[0].InnerText == "Duration")
                    {
                        game.Duration = cells[1].InnerText;
                    }
                    else if (cells[0].InnerText == "Attendance")
                    {
                        game.Attendance = Int32.Parse(cells[1].InnerText.Replace(",", ""));
                    }
                    else if (cells[0].InnerText == "Weather")
                    {
                        game.Weather = cells[1].InnerText;
                    }
                    else if (cells[0].InnerText == "Vegas Line")
                    {
                        game.VegasLine = cells[1].InnerText;
                    }
                }
            }


            // table 7:  passing / rushing
            rows = tables[7].SelectNodes("tbody/tr");
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells != null && cells[0].InnerText.Trim() != "")
                {
                    var a = cells[0].SelectNodes(".//a");
                    Player player = parsePlayer("http://www.pro-football-reference.com" + a[0].Attributes[0].Value);
                    if (player == null)
                    {
                        continue;
                    }
                    Stat s = getStat(player, game);
                    Contract c = getContract(player, cells[1].InnerText, game);
                    s.Team = c.Team;

                    s.Passing_Cmp = parseDouble(cells[2].InnerText);
                    s.Passing_Att = parseDouble(cells[3].InnerText);
                    s.Passing_Yds = parseDouble(cells[4].InnerText);
                    s.Passing_TD = parseDouble(cells[5].InnerText);
                    s.Passing_Int = parseDouble(cells[6].InnerText);
                    s.Passing_Lng = parseDouble(cells[7].InnerText);
                    
                    s.Rushing_Att = parseDouble(cells[8].InnerText);
                    s.Rushing_Yds = parseDouble(cells[9].InnerText);
                    s.Rushing_TD = parseDouble(cells[10].InnerText);
                    s.Rushing_Lng = parseDouble(cells[11].InnerText);

                    s.Receiving_Rec = parseDouble(cells[12].InnerText);
                    s.Receiving_Yds = parseDouble(cells[13].InnerText);
                    s.Receiving_TD = parseDouble(cells[14].InnerText);
                    s.Receiving_Lng = parseDouble(cells[15].InnerText);
                }
            }

            // table 8:  Defense & Returns
            rows = tables[8].SelectNodes("tbody/tr");
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells != null && cells[0].InnerText.Trim() != "")
                {
                    var a = cells[0].SelectNodes(".//a");
                    Player player = parsePlayer("http://www.pro-football-reference.com" + a[0].Attributes[0].Value);
                    if (player == null)
                    {
                        continue;
                    }
                    Stat s = getStat(player, game);
                    Contract c = getContract(player, cells[1].InnerText, game);
                    s.Team = c.Team;

                    s.DefInt_Sk = parseDouble(cells[2].InnerText);
                    s.DefInt_Int = parseDouble(cells[3].InnerText);
                    s.DefInt_Yds = parseDouble(cells[4].InnerText);
                    s.DefInt_TD = parseDouble(cells[5].InnerText);
                    s.DefInt_Lng = parseDouble(cells[6].InnerText);

                    s.Fumbles_FR = parseDouble(cells[7].InnerText);
                    s.Fumbles_Yds = parseDouble(cells[8].InnerText);
                    s.Fumbles_TD = parseDouble(cells[9].InnerText);
                    s.Fumbles_FF = parseDouble(cells[10].InnerText);

                    s.KickRet_Rt = parseDouble(cells[11].InnerText);
                    s.KickRet_Yds = parseDouble(cells[12].InnerText);
                    s.KickRet_Y_Rt = parseDouble(cells[13].InnerText);
                    s.KickRet_TD = parseDouble(cells[14].InnerText);
                    s.KickRet_Lng = parseDouble(cells[15].InnerText);

                    s.PuntRet_Ret = parseDouble(cells[16].InnerText);
                    s.PuntRet_Yds = parseDouble(cells[17].InnerText);
                    s.PuntRet_Y_Rt = parseDouble(cells[18].InnerText);
                    s.PuntRet_TD = parseDouble(cells[19].InnerText);
                    s.PuntRet_Lng = parseDouble(cells[20].InnerText);
                }
            }

            // table 9:  Kicking & Punting
            rows = tables[9].SelectNodes("tbody/tr");
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells != null && cells[0].InnerText.Trim() != "")
                {
                    var a = cells[0].SelectNodes(".//a");
                    Player player = parsePlayer("http://www.pro-football-reference.com" + a[0].Attributes[0].Value);
                    if (player == null)
                    {
                        continue;
                    }
                    Stat s = getStat(player, game);
                    Contract c = getContract(player, cells[1].InnerText, game);
                    s.Team = c.Team;

                    s.Pat_XPM = parseDouble(cells[2].InnerText);
                    s.Pat_XPA = parseDouble(cells[3].InnerText);
                   
                    s.FG_FGM = parseDouble(cells[4].InnerText);
                    s.FG_FGA = parseDouble(cells[5].InnerText);
                   
                    s.Punting_Pnt = parseDouble(cells[6].InnerText);
                    s.Punting_Yds = parseDouble(cells[7].InnerText);
                    s.Punting_Y_P = parseDouble(cells[8].InnerText);
                    s.Punting_Lng = parseDouble(cells[9].InnerText);
                }
            }
        }

        private static double? parseDouble(string t)
        {
            if (t == null || t == "")
            {
                return null;
            }
            return Double.Parse(t);
        }

        private static Stat getStat(Player p, Game g)
        {
            foreach (var s in g.Stats)
            {
                if (s.Player == p)
                {
                    return s;
                }
            }
            Stat sn = new Stat();
            sn.Player = p;
            g.Stats.Add(sn);
            return sn;
        }

        private static Contract getContract(Player p, string teamAbbreviation, Game game)
        {
            foreach (var c in Contracts)
            {
                if (c.Team.Abbreviation == teamAbbreviation && c.Player == p)
                {
                    if (c.StartDate > game.Date)
                    {
                        c.StartDate = game.Date;
                    }
                    if (c.EndDate < game.Date)
                    {
                        c.EndDate = game.Date;
                    }
                    return c;
                }
            }
            Contract cn = new Contract();
            cn.EndDate = game.Date;
            cn.StartDate = game.Date;
            cn.Player = p;
            foreach (var t in Teams)
            {
                if (t.Abbreviation == teamAbbreviation)
                {
                    cn.Team = t;
                    break;
                }
            }
            if (cn.Team == null)
            {
            }
            Contracts.Add(cn);
            return cn;
        }

        private static Player parsePlayer(string site)
        {
            // check cache
            foreach (var player in Players)
            {
                if (player.Site == site)
                {
                    return player;
                }
            }

            // load
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument doc = htmlWeb.Load(site);

            HtmlNode h1 = doc.DocumentNode.SelectNodes("//h1")[0];
            var playerName = h1.InnerText;

            if (playerName == "File Not Found")
            {
                return null;
            }

            Player p = new Player();
            p.Name = playerName;
            p.Site = site;

            HtmlNodeCollection ps = doc.DocumentNode.SelectNodes("//p");

            p.Position = ps[3].NextSibling.NextSibling.InnerText.Trim();
            p.Height = ps[3].NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.InnerText.Replace("&nbsp;", "");
            p.Weight = ps[3].NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.InnerText.Trim();
            p.Born = ps[4].NextSibling.NextSibling.NextSibling.InnerText.Trim();           
           
            Players.Add(p);
            return p;
        }

        private static Team parseTeam(string site)
        {
            // check cache
            foreach (var team in Teams)
            {
                if (team.Site == site)
                {
                    return team;
                }
            }


            // load
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument doc = htmlWeb.Load(site);
            string[] splits = site.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries); 

            HtmlNode h1 = doc.DocumentNode.SelectNodes("//h1")[0];
            var teamName = h1.InnerText.Substring(5);
                        
            Team nt = new Team();
            nt.Name = teamName;
            nt.Abbreviation = splits[splits.Length - 2].ToUpper();
            nt.Abbreviation = nt.Abbreviation == "CLT" ? "IND" : nt.Abbreviation;
            nt.Abbreviation = nt.Abbreviation == "HTX" ? "HOU" : nt.Abbreviation;
            nt.Abbreviation = nt.Abbreviation == "CRD" ? "ARI" : nt.Abbreviation;
            nt.Abbreviation = nt.Abbreviation == "RAI" ? "OAK" : nt.Abbreviation;
            nt.Abbreviation = nt.Abbreviation == "RAM" ? "STL" : nt.Abbreviation;
            nt.Abbreviation = nt.Abbreviation == "RAV" ? "BAL" : nt.Abbreviation;
            nt.Abbreviation = nt.Abbreviation == "OTI" ? "TEN" : nt.Abbreviation;
            nt.Site = site;
            Teams.Add(nt);
            return nt;
        }
    }

    [Serializable]
    public class Game
    {
        public Game()
        {
            Stats = new List<Stat>();
        }
        public int Id { get; set; }

        public int Week { get; set; }
        public DateTime Date { get; set; }
        public Team Winner { get; set; }
        public Team Loser { get; set; }
        public int PtsWinner { get; set; }
        public int PtsLoser { get; set; }

        public string StartTime { get; set; }
        public string Duration { get; set; }
        public int Attendance { get; set; }
        public string Weather { get; set; }
        public string VegasLine { get; set; }

        public List<Stat> Stats { get; set; }
    }

    [Serializable]
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Site { get; set; }
        public string Position { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }
        public string Born { get; set; }
    }

    [Serializable]
    public class Contract
    {
        public int Id { get; set; }
        public Team Team { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Player Player { get; set; }
    }

    [Serializable]
    public class Stat
    {
        public int Id { get; set; }

        public Player Player { get; set; }
        public Team Team { get; set; }

        public double? Passing_Cmp { get; set; }
        public double? Passing_Att { get; set; }
        public double? Passing_Yds { get; set; }
        public double? Passing_TD { get; set; }
        public double? Passing_Int { get; set; }
        public double? Passing_Lng { get; set; }

        public double? Rushing_Att { get; set; }
        public double? Rushing_Yds { get; set; }
        public double? Rushing_TD { get; set; }
        public double? Rushing_Lng { get; set; }

        public double? Receiving_Rec { get; set; }
        public double? Receiving_Yds { get; set; }
        public double? Receiving_TD { get; set; }
        public double? Receiving_Lng { get; set; }

        public double? DefInt_Sk { get; set; }
        public double? DefInt_Int { get; set; }
        public double? DefInt_Yds { get; set; }
        public double? DefInt_TD { get; set; }
        public double? DefInt_Lng { get; set; }

        public double? Fumbles_FR { get; set; }
        public double? Fumbles_Yds { get; set; }
        public double? Fumbles_TD { get; set; }
        public double? Fumbles_FF { get; set; }

        public double? KickRet_Rt { get; set; }
        public double? KickRet_Yds { get; set; }
        public double? KickRet_Y_Rt { get; set; }
        public double? KickRet_TD { get; set; }
        public double? KickRet_Lng { get; set; }

        public double? PuntRet_Ret { get; set; }
        public double? PuntRet_Yds { get; set; }
        public double? PuntRet_Y_Rt { get; set; }
        public double? PuntRet_TD { get; set; }
        public double? PuntRet_Lng { get; set; }

        public double? Pat_XPM { get; set; }
        public double? Pat_XPA { get; set; }

        public double? FG_FGM { get; set; }
        public double? FG_FGA { get; set; }

        public double? Punting_Pnt { get; set; }
        public double? Punting_Yds { get; set; }
        public double? Punting_Y_P { get; set; }
        public double? Punting_Lng { get; set; }

    }

    [Serializable]
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string Site { get; set; }
    }
}
