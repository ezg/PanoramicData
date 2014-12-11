using System;
using System.Collections.Generic;
using System.IO;
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

namespace VAST2011Populator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Stream stream = File.Open(@"C:\Users\ez\Downloads\MC_1_Materials_3-30-2011\MC_1_Materials_3-30-2011\Microblogs.csv", FileMode.Open);
            Dictionary<string, int> countPerDay = new Dictionary<string, int>();
            using (StreamReader sr = new StreamReader(stream))
            {
                String line = sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    List<string> entries = CSVHelper.CSVLineSplit(line);
                    string day = entries[1];
                    day = day.Substring(0, day.IndexOf(' '));
                    if (!countPerDay.ContainsKey(day))
                    {
                        countPerDay.Add(day, 0);
                    }
                    countPerDay[day] = countPerDay[day] + 1;
                }
            }
            foreach (var key in countPerDay.Keys)
            {
                Console.WriteLine(key + " = " + countPerDay[key]);
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
