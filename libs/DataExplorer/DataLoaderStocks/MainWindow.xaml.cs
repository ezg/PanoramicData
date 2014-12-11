using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Web;
using MySql.Data.MySqlClient;

namespace DataLoaderStocks
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MySqlConnection conn;

        public MainWindow()
        {
            InitializeComponent();
        }

        private List<WCompany> parseSymbolList()
        {
            List<WCompany> ret = new List<WCompany>();
            Stream stream = File.Open(@"C:\ez_projects\Sharp starPad SDK\Apps\DataExplorer\DataLoaderStocks\data\fortune500.txt", FileMode.Open);
            using (StreamReader sr = new StreamReader(stream))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.StartsWith("|-"))
                    {
                        line = line.Replace("||", "$");
                        String[] blocks = line.Split("$".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (blocks.Length > 1)
                        {
                            WCompany wc = new WCompany();
                            string tmp = blocks[0].Split("|".ToArray(), StringSplitOptions.RemoveEmptyEntries)[1];
                            wc.Symbol = tmp.Trim().Substring(0, tmp.Trim().Length - 2);
                            wc.Industry = blocks[3].Trim();
                            wc.City = blocks[4].Trim().Replace("[[", "").Replace("]]", "");
                            ret.Add(wc);
                            Console.WriteLine(wc.Symbol + " / " + wc.Industry + " / " + wc.City);
                        }
                    }
                }
            }

            return ret;
        }

        private void loadSymbolData(List<WCompany> wcs) 
        {
            string fields = "";
            for (int i = 0; i < StockInformation.YAHOO_FIELDS.Length; i = i+2)
            {
                fields += StockInformation.YAHOO_FIELDS[i];
            }

            Dictionary<int, int> counter = new Dictionary<int, int>();

            foreach (var wc in wcs)
            {
                string address = "http://finance.yahoo.com/d/quotes.csv?s=" + HttpUtility.UrlEncode(wc.Symbol) + "&f=" + HttpUtility.UrlEncode(fields);
                //Console.WriteLine(address);
                HttpWebRequest request;
                HttpWebResponse response = null;
                StreamReader reader;
                StringBuilder sbSource;

                try
                {
                    // Create and initialize the web request  
                    request = WebRequest.Create(address) as HttpWebRequest;
                    request.UserAgent = ".NET Sample";
                    request.KeepAlive = false;
                    // Set timeout to 15 seconds  
                    request.Timeout = 15 * 1000;

                    // Get response  
                    response = request.GetResponse() as HttpWebResponse;

                    if (request.HaveResponse == true && response != null)
                    {
                        reader = new StreamReader(response.GetResponseStream());
                        sbSource = new StringBuilder(reader.ReadToEnd());

                        List<string> values = CSVHelper.CSVLineSplit(sbSource.ToString());
                        Console.WriteLine(values.Count + " " + wc.Symbol);
                        if (counter.ContainsKey(values.Count))
                        {
                            counter[values.Count] = counter[values.Count] + 1;
                        }
                        else
                        {
                            counter.Add(values.Count, 1);
                        }

                        // insert 
                        MySqlCommand cmd = conn.CreateCommand();
                        string query = "INSERT INTO stock(";
                        int c = 0;
                        for (int i = 0; i < StockInformation.YAHOO_FIELDS.Length; i += 2)
                        {
                            query += StockInformation.YAHOO_FIELDS[i] + ",";
                            //Console.WriteLine(StockInformation.YAHOO_FIELDS[i] + "\t" + StockInformation.YAHOO_FIELDS[i+1] + "\t" + values[c]);
                            c++;

                        }
                        query += "location, industry) values (";
                        c = 0;
                        foreach (var v in values)
                        {
                            query += "'" + v.Replace("'", "''") + "',";
                            c++;
                        }
                        query += "'" + wc.City + "',";
                        query += "'" + wc.Industry + "')";
                        cmd.CommandText = query;
                        cmd.ExecuteNonQuery();

                        /*cmd.CommandText = "INSERT INTO meta_field (t_id, f_name, f_data_type) VALUES (" +
                            stock_t_id + "," +
                            "'industry'," +
                            (int)FieldDataType.Text + "" +
                            ")";
                        cmd.ExecuteNonQuery();*/
                    }
                }
                catch (WebException ee)
                {
                    Console.WriteLine("EE !!!!!!!!!!!!!!!!!!!! " + wc.Symbol); 
                }
            }
        }

        private void loadPriceData()
        {
            if (conn == null) 
            {
                openMySqlConnection();
            }
            MySqlCommand cmd = new MySqlCommand();
            cmd.CommandText = "select s_id, s from stock";
            cmd.Connection = conn;
            MySqlDataReader re = cmd.ExecuteReader();
            List<KeyValuePair<int, string>> symbols = new List<KeyValuePair<int, string>>();
            while (re.Read())
            {
                symbols.Add(new KeyValuePair<int, string>(re.GetInt32(0), re.GetString(1)));
            }
            re.Close();

            foreach (var key in symbols)
            {
                try
                {
                    Console.WriteLine("load prices for : " + key.Value);

                    //http://ichart.finance.yahoo.com/table.csv?s=YHOO&d=4&e=14&f=2012&g=d&a=3&b=12&c=1996&ignore=.csv
                    string address = "http://ichart.finance.yahoo.com/table.csv?s=" + HttpUtility.UrlEncode(key.Value) + "&d=4&e=14&f=2012&g=d&a=3&b=12&c=1996&ignore=.csv";
                    Console.WriteLine(address);
                    HttpWebRequest request;
                    HttpWebResponse response = null;
                    StreamReader reader;
                    StringBuilder sbSource;

                    // Create and initialize the web request  
                    request = WebRequest.Create(address) as HttpWebRequest;
                    request.UserAgent = ".NET Sample";
                    request.KeepAlive = false;
                    // Set timeout to 15 seconds  
                    request.Timeout = 15 * 1000;

                    // Get response  
                    response = request.GetResponse() as HttpWebResponse;

                    if (request.HaveResponse == true && response != null)
                    {
                        reader = new StreamReader(response.GetResponseStream());
                        sbSource = new StringBuilder(reader.ReadToEnd());

                        string query = "Insert into stock_price (s_id, date, open, high, low, close, volume, adj_close) values ";
                        string[] lines = sbSource.ToString().Split("\n".ToArray(), StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 1; i < lines.Length; i++)
                        {
                            query += "(";
                            query += key.Key + ", ";
                            string[] words = lines[i].Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                            foreach (var w in words)
                            {
                                query += "'" + w + "',";
                            }
                            query = query.Substring(0, query.Length - 1);
                            query += "),";
                        }

                        cmd.CommandText = query.Substring(0, query.Length - 1);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (WebException ee)
                {
                    Console.WriteLine("EE !!!!!!!!!!!!!!!!!!!! " + key.Value); 
                }

            }
        }

        private void openMySqlConnection()
        {
            string connStr = "server=lilienthal.metanet.ch;user=stocks1;database=stocks1;port=3306;password=brownGFX1;";
            conn = new MySqlConnection(connStr);

            Console.WriteLine("Connecting to MySQL...");
            conn.Open();
        }

        private void createTables()
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader = null;
            cmd.Connection = conn;
            cmd.CommandText = "drop table meta_table";
            cmd.ExecuteNonQuery();

            // meta table
            cmd.CommandText = 
                "CREATE TABLE meta_table (" + 
                "t_id int(11) NOT NULL AUTO_INCREMENT," +
                "t_name varchar(255) NOT NULL," +
                "PRIMARY KEY (t_id)," +
                "UNIQUE KEY t_id_UNIQUE (t_id))";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO meta_table (t_name) VALUES ('stock')";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT LAST_INSERT_ID()";
            reader = cmd.ExecuteReader();
            reader.Read();
            int stock_t_id = reader.GetInt32(0);
            reader.Close();

            cmd.CommandText = "INSERT INTO meta_table (t_name) VALUES ('stock_price')";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT LAST_INSERT_ID()";
            reader = cmd.ExecuteReader();
            reader.Read();
            int stock_price_t_id = reader.GetInt32(0);
            reader.Close();

            // meta_field
            cmd.CommandText = "drop table meta_field";
            cmd.ExecuteNonQuery();

            cmd.CommandText =
                "CREATE TABLE meta_field (" +
                "f_id int(11) NOT NULL AUTO_INCREMENT," +
                "t_id int(11) NOT NULL," +
                "f_name varchar(255) NOT NULL," +
                "f_data_type int NOT NULL," +
                "PRIMARY KEY (f_id)," +
                "UNIQUE KEY f_id_UNIQUE (f_id)," +
                "CONSTRAINT fk_t_id FOREIGN KEY (t_id) " +
                "REFERENCES meta_table (t_id))";
            cmd.ExecuteNonQuery();

            // meta_field_label
            cmd.CommandText = "drop table meta_field_label";
            cmd.ExecuteNonQuery();

            cmd.CommandText =
                "CREATE TABLE meta_field_label (" +
                "fl_id int(11) NOT NULL AUTO_INCREMENT," +
                "f_id int(11) NOT NULL," +
                "fl_name varchar(255) NOT NULL," +
                "PRIMARY KEY (fl_id)," +
                "UNIQUE KEY fl_id_UNIQUE (fl_id)," +
                "CONSTRAINT fk_f_id FOREIGN KEY (f_id) " +
                "REFERENCES meta_field (f_id))";
            cmd.ExecuteNonQuery();

            // insert fields and descriptions
            int f_id = -1;
            for (int i = 0; i < StockInformation.YAHOO_FIELDS.Length; i += 2)
            {
                cmd.CommandText = "INSERT INTO meta_field (t_id, f_name, f_data_type) VALUES (" +
                    stock_t_id + "," +
                    "'" + StockInformation.YAHOO_FIELDS[i] + "'," +
                    (int)FieldDataType.Text + "" +
                    ")";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                reader = cmd.ExecuteReader();
                reader.Read();
                f_id = reader.GetInt32(0);
                reader.Close();

                cmd.CommandText = "INSERT INTO meta_field_label (f_id, fl_name) VALUES (" +
                   f_id + "," +
                   "'" + StockInformation.YAHOO_FIELDS[i+1] + "'" +
                   ")";
                cmd.ExecuteNonQuery();
            }
            cmd.CommandText = "INSERT INTO meta_field (t_id, f_name, f_data_type) VALUES (" +
                   stock_t_id + "," +
                   "'industry'," +
                   (int)FieldDataType.Text + "" +
                   ")";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT LAST_INSERT_ID()";
            reader = cmd.ExecuteReader();
            reader.Read();
            f_id = reader.GetInt32(0);
            reader.Close();

            cmd.CommandText = "INSERT INTO meta_field_label (f_id, fl_name) VALUES (" +
               f_id + "," +
               "'Industry'" +
               ")";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO meta_field (t_id, f_name, f_data_type) VALUES (" +
                   stock_t_id + "," +
                   "'location'," +
                   (int)FieldDataType.Text + "" +
                   ")";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT LAST_INSERT_ID()";
            reader = cmd.ExecuteReader();
            reader.Read();
            f_id = reader.GetInt32(0);
            reader.Close();

            cmd.CommandText = "INSERT INTO meta_field_label (f_id, fl_name) VALUES (" +
               f_id + "," +
               "'Location'" +
               ")";
            cmd.ExecuteNonQuery();

            // stock
            cmd.CommandText = "drop table stock";
            cmd.ExecuteNonQuery();

            string createTableQuery = "CREATE TABLE stock (";
            createTableQuery += "s_id int(11) NOT NULL AUTO_INCREMENT,";
            cmd.CommandText = "select f_name from meta_field where t_id = " + stock_t_id;
            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                createTableQuery += reader.GetString(0) + " varchar(255),";
            }
            reader.Close();
            createTableQuery += "PRIMARY KEY (s_id)," +
                "UNIQUE KEY f_id_UNIQUE (s_id)" + ")";

            cmd.CommandText = createTableQuery;
            cmd.ExecuteNonQuery();

            // stock price
            string[] fields = new string[]{
                "date", "Date",
                "open", "Open",
                "high", "High",
                "low", "Low",
                "close", "Close",
                "volume", "Volume",
                "adj_close", "Adj Close",
            };

            cmd.CommandText = "drop table stock_price";
            cmd.ExecuteNonQuery();

            createTableQuery = "CREATE TABLE stock_price (";
            createTableQuery += "sp_id int(11) NOT NULL AUTO_INCREMENT," +
                "s_id int(11) NOT NULL,";
            for (int i = 0; i < fields.Length; i += 2)
            {
                createTableQuery += fields[i] + " varchar(255),";
            }
            createTableQuery += "PRIMARY KEY (sp_id)," +
                    "UNIQUE KEY f_id_UNIQUE (sp_id)," +
                    "CONSTRAINT fk_s_id FOREIGN KEY (s_id) " +
                    "REFERENCES stock (s_id))";
            cmd.CommandText = createTableQuery;
            cmd.ExecuteNonQuery();

            for (int i = 0; i < fields.Length; i += 2)
            {
                cmd.CommandText = "INSERT INTO meta_field (t_id, f_name, f_data_type) VALUES (" +
                       stock_price_t_id + "," +
                       "'" + fields[i] + "'," +
                       (int)FieldDataType.Text + "" +
                       ")";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                reader = cmd.ExecuteReader();
                reader.Read();
                f_id = reader.GetInt32(0);
                reader.Close();

                cmd.CommandText = "INSERT INTO meta_field_label (f_id, fl_name) VALUES (" +
                   f_id + "," +
                   "'" + fields[i+1] + "'" +
                   ")";
                cmd.ExecuteNonQuery();
            }

        }

        private void OpenConnection_Click(object sender, RoutedEventArgs e)
        {
            openMySqlConnection();
            createTables();

        }

        private void LoadSymbols_Click(object sender, RoutedEventArgs e)
        {
            List<WCompany> wcs = parseSymbolList();
            loadSymbolData(wcs);
        }

        private void LoadPrices_Click(object sender, RoutedEventArgs e)
        {
            loadPriceData();
        }


        private void Test_Click(object sender, RoutedEventArgs e)
        {
            string tt = "a||dbc";
            tt = tt.Replace("||", "$");
            String[] blocks = tt.Split("$".ToArray(), StringSplitOptions.RemoveEmptyEntries);


            Stream stream = File.Open(@"C:\Users\ez\AppData\Local\Temp\quotes-44 - Copy.csv", FileMode.Open);
            using (StreamReader sr = new StreamReader(stream))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
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
                    Console.WriteLine(words.Count);
                }
            }
        }
    }
}
