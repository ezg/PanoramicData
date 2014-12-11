using MapApi.BingMapService;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapApi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static SqlConnection connection = null;

        public MainWindow()
        {
            InitializeComponent();
            //runLocationConversion();

            Console.WriteLine(MapApi.MapAPI.GeoCode("Paris France"));
        }

        public static void runLocationConversion()
        {
            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
                 @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");

            connection = new SqlConnection(stringBuilder.ToString());
            connection.Open();

            SqlCommand cmd = connection.CreateCommand();

            try
            {
                cmd.CommandText =
                    "ALTER TABLE acm.conference ADD location_2 nvarchar(255) NULL";
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
            }

            Dictionary<string, string> locations = new Dictionary<string, string>();

            cmd = connection.CreateCommand();
            cmd.CommandText = "select distinct(location) from acm.conference";
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string loc = reader.GetString(0);
                if (!locations.ContainsKey(loc))
                {
                    locations.Add(loc, MapAPI.GeoCode(loc));
                }
            }
            reader.Close();

            cmd = connection.CreateCommand();
            foreach (var key in locations.Keys)
            {
                cmd.CommandText += "update acm.conference set location_2 = '" + locations[key] + "' where location = '" + key + "';\n";
            } 
            cmd.ExecuteNonQuery();

            /*
            alter table acm.conference drop column location;
            exec sp_rename 'acm.conference.location_2', 'location', 'COLUMN';
            */


            /*cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info DROP " +
                "CONSTRAINT FK_meta_table_info_pk_field_info;";
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE meta.table_info ALTER COLUMN pk_field_info int NULL;";
            cmd.ExecuteNonQuery();*/
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
