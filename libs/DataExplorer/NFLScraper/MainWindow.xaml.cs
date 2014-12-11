using HtmlAgilityPack;
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

namespace NFLScraper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static List<Game> Games = new List<Game>();
        public static List<Team> Teams = new List<Team>();
        public static List<Player> Players = new List<Player>();
        public static List<Contract> Contracts = new List<Contract>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Parse_Click(object sender, RoutedEventArgs e)
        {
            Parser.Parse("http://www.pro-football-reference.com/years/2011/games.htm");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DBPopulator.Populate();
        }

    }

    
}
