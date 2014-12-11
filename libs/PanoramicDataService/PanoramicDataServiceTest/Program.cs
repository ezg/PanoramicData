using Newtonsoft.Json;
using PanoramicDataDBConnector;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;

namespace PanoramicDataServiceTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            IEnumerable<TableInfo> infos = DatabaseManager.GetTableInfos();
            foreach (var i in infos)
            {
                Console.WriteLine(i.Name);
            }
        }

        private static void runPerfTest()
        {
            bool run = true;
            while (run)
            {
                Console.WriteLine("RUN TEST:");
                Console.WriteLine("~~~~~~~~~");
                runQueryLocal();
                Console.WriteLine("~~~~~~~~~");

                ConsoleKeyInfo key = Console.ReadKey();

                if (key.Key == ConsoleKey.Escape)
                {
                    run = false;
                }
            }
        }
        private static void runQueryLocal()
        {
            //SqlConnection connection = new SqlConnection(
            //   "Server=tcp:nog7tpedho.database.windows.net,1433;Database=panoramicdata;User ID=panoramicdata@nog7tpedho;Password=Browngfx1;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;");
            SqlConnection connection = new SqlConnection(
                @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");
            connection.Open();
            
            runQueryAndParseToString(connection, "select * from nfl.player");
            runQueryAndParseToString(connection, "select * from nfl.team");
            runQueryAndParseToString(connection, "select * from nfl.stats");

            runQueryAndParseToString(connection, "select * \n" +
                                                 "from \n" +
                                                    "nfl.stats s, \n" +
                                                    "nfl.player p, \n" +
                                                    "nfl.player_contract pc, \n" +
                                                    "nfl.game g, \n" +
                                                    "nfl.team t \n" +
                                                 "where \n" +
                                                    "s.player_id = p.id and \n" +
                                                    "s.game_id = g.id and \n" +
                                                    "pc.player_id = p.id and \n" +
                                                    "pc.team_id = t.id ");


            runQueryAndParseToString(connection, "select * \n" +
                                                 "from \n" +
                                                    "nfl.stats s, \n" +
                                                    "nfl.player p, \n" +
                                                    "nfl.player_contract pc, \n" +
                                                    "nfl.game g, \n" +
                                                    "nfl.team t \n" +
                                                 "where \n" +
                                                    "s.player_id = p.id and \n" +
                                                    "s.game_id = g.id and \n" +
                                                    "pc.player_id = p.id and \n" +
                                                    "pc.team_id = t.id and \n" + 
                                                    "t.id = 1 ");
            
        }

        private static void runQueryAndParseToString(SqlConnection connection, string query)
        {
            DateTime start = DateTime.Now;

            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            SqlDataReader myReader = cmd.ExecuteReader();

            StringBuilder ret = new StringBuilder();
            int count = 0;
            while (myReader.Read())
            {
                for (int field = 0; field < myReader.FieldCount; field++)
                {
                    ret.Append(myReader[myReader.GetName(field)].ToString() + "\t");
                }
                ret.Append("\n");

                count++;
            }
            myReader.Close();

            TimeSpan duration = DateTime.Now - start;
            Console.WriteLine(query);
            Console.WriteLine("Returned " + count + " rows in " + duration.ToString(@"mm\:ss\:fff"));
            Console.WriteLine("Buffer length: " + ret.Length);
            Console.WriteLine();
        }

        private static void runQueryOverApi()
        {
            HttpClient httpClient = new HttpClient();
            var responseMessage = httpClient.PostAsync("http://panoramicdata.azurewebsites.net/api/Data?query=asdf",
                new StringContent(JsonConvert.SerializeObject("select * form nfl.stats"), Encoding.UTF8, "application/json")).Result;
            //Console.WriteLine(JsonConvert.("select * form nfl.stats"));
            string ret = responseMessage.Content.ReadAsStringAsync().Result;

            Console.WriteLine(ret);
        }

        private static void getTableInfo()
        {
            HttpClient httpClient = new HttpClient();
            var responseMessage = httpClient.GetAsync("http://panoramicdata.azurewebsites.net/api/MetaTableInfo").Result;

            List<TableInfo> infos = JsonConvert.DeserializeObject<List<TableInfo>>(responseMessage.Content.ReadAsStringAsync().Result);

            foreach (var ti in infos)
            {
                Console.WriteLine(ti.Name);
                foreach (var fi in ti.FieldInfos)
                {
                    Console.WriteLine("\t" + fi.Name);
                }
            }
        }
    }
}
