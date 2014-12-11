using System;
using System.Collections.Generic;
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

namespace DataGenerator
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

        public void flightGenerator()
        {
            List<string> headers = new List<string>(new string[]
            {
                "ID",
                "From",
                "To",
                "Date Departure",
                "Time Departure",
                "Date Arrival",
                "Time Arrival",
                "Stops",
                "Airline",
                "Airline Image",
                "Price"               
            });

            List<string> citys = new List<string>(new string[]
            {
                "Boston",
                "Miami",
                "New York",
                "Providence",
                "Los Angeles",
                "Chicago",
                "Philadelphia",
                "San Francisco",
                "Seattle"               
            });

            List<string> airlines = new List<string>(new string[]
            {
                "US Airways",
                "Delta",
                "Southwest",
                "Jet Blue"            
            });

            foreach (var h in headers)
            {
                Console.Write("\"" + h + "\",");
            }
            Console.WriteLine();

            Random random = new Random();
            for (int i = 0; i < 200; i++)
            {
                List<string> citysC = citys.ToArray().ToList();
                foreach (var h in headers)
                {
                    if (h == "ID")
                    {
                        Console.Write("\"" + (i + 1) + "\",");
                    }
                    else if (h == "From")
                    {
                        int idx = random.Next(citysC.Count - 1);
                        Console.Write("\"" + citysC[idx] + "\",");
                        citysC.RemoveAt(idx);
                    }
                    else if (h == "To")
                    {
                        int idx = random.Next(citysC.Count - 1);
                        Console.Write("\"" + citysC[idx] + "\",");
                    }
                    else if (h == "Date Departure")
                    {
                        int d = random.Next(1, 27);
                        Console.Write("\"" + "1/" + d + "/2013" + "\",");

                        int ho = random.Next(1, 20);
                        Console.Write("\"" + ho + ":00" + "\",");

                        d = random.NextDouble() > 0.5 ? d + 1 : d;
                        Console.Write("\"" + "1/" + d + "/2013" + "\",");

                        ho += random.Next(1, 3);
                        Console.Write("\"" + ho + ":00" + "\",");

                    }
                    else if (h == "Stops")
                    {
                        Console.Write("\"" + random.Next(3) + "\",");
                    }
                    else if (h == "Airline")
                    {
                        int idx = random.Next(airlines.Count - 1);
                        Console.Write("\"" + airlines[idx] + "\",");

                        Console.Write("\"" + airlines[idx] + ".png\",");
                    }
                    else if (h == "Price")
                    {
                        double p = random.Next(80, 1200);
                        Console.Write("\"" + "" + p + ".00" + "\",");
                    }
                }
                Console.WriteLine();
            }
        }

        public void gradeGenerator()
        {
            List<string> first = new List<string>(new string[]
            {
                "Barbara",
                "Tony",
                "Joe",
                "Rachel",
                "Johnny",
                "Sara",
                "Hugo",
                "Isabel"             
            });

            List<string> last = new List<string>(new string[]
            {
                "Smith",
                "Dupon",
                "Meier",
                "Lopez",
                "Satiro",
                "Mueller",
                "van Dam",
                "Bolay"  
            });

            List<string> level = new List<string>(new string[]
            {
                "Freshman",
                "Sophomore",
                "Junior",
                "Senior",
                "Grad"             
            });

            List<string> headers = new List<string>(new string[]
            {
                "ID",
                "Name",
                "Email",
                "Year",
                "All Grades",
                "Average Grade"
            });

            foreach (var h in headers)
            {
                Console.Write("\"" + h + "\",");
            }
            Console.WriteLine();

            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {

                double average = 0.0;
                string email = "";
                foreach (var h in headers)
                {
                    if (h == "ID")
                    {
                        Console.Write("\"" + (i + 1) + "\",");
                    }
                    else if (h == "Name")
                    {
                        int idx1 = random.Next(first.Count - 1);
                        int idx2 = random.Next(last.Count - 1);
                        email = first[idx1][0] + "" + last[idx2][0];
                        Console.Write("\"" + first[idx1] + " " + last[idx2] +  "\",");
                    }
                    else if (h == "Email")
                    {
                        Console.Write("\"" + email.ToLower() + "@brown.edu"  + "\",");
                    }
                    else if (h == "Year")
                    {
                        int idx1 = random.Next(level.Count - 1);
                        Console.Write("\"" + level[idx1] + "\",");
                    }
                    else if (h == "All Grades")
                    {
                        double grade = 0.0;
                        string text = "";
                        double r = random.NextDouble();
                        if (r > 0.0 && r <= 0.1)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(30 + j * 10, 40 + j * 10);
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        else if (r > 0.1 && r <= 0.2)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(90 - ((6 - j) * 6), 100 - ((6 - j) * 6));
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        else if (r > 0.2 && r <= 0.3)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(40, 60);
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        else if (r > 0.3 && r <= 0.35)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(90, 100);
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        else
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(60, 100);
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        average = (int)(grade / 6);
                        Console.Write("\"" + text.Substring(0, text.Count()-3) + "\",");
                    }
                    else if (h == "Average Grade")
                    {
                        Console.Write("\"" + average + "\",");
                    }
                }
                Console.WriteLine();
            }
        }

        public void photoLibraryGenerator()
        {
            List<string> street = new List<string>(new string[]
            {
                "Governor Street",
                "Olney Street",
                "Thayer Street",
                "Meeting Street",
                "Bowen Street",
                "Keene Street",
                "Brown Street",
                "Prospect Street"             
            });

            List<string> last = new List<string>(new string[]
            {
                "Smith",
                "Dupon",
                "Meier",
                "Lopez",
                "Satiro",
                "Mueller",
                "van Dam",
                "Bolay"  
            });

            List<string> level = new List<string>(new string[]
            {
                "Freshman",
                "Sophomore",
                "Junior",
                "Senior",
                "Grad"             
            });

            List<string> headers = new List<string>(new string[]
            {
                "ID",
                "Name",
                "Email",
                "Year",
                "All Grades",
                "Average Grade"
            });

            foreach (var h in headers)
            {
                Console.Write("\"" + h + "\",");
            }
            Console.WriteLine();

            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {

                double average = 0.0;
                string email = "";
                foreach (var h in headers)
                {
                    if (h == "ID")
                    {
                        Console.Write("\"" + (i + 1) + "\",");
                    }
                    else if (h == "Name")
                    {
                        /*int idx1 = random.Next(first.Count - 1);
                        int idx2 = random.Next(last.Count - 1);
                        email = first[idx1][0] + "" + last[idx2][0];
                        Console.Write("\"" + first[idx1] + " " + last[idx2] + "\",");*/
                    }
                    else if (h == "Email")
                    {
                        Console.Write("\"" + email.ToLower() + "@brown.edu" + "\",");
                    }
                    else if (h == "Year")
                    {
                        int idx1 = random.Next(level.Count - 1);
                        Console.Write("\"" + level[idx1] + "\",");
                    }
                    else if (h == "All Grades")
                    {
                        double grade = 0.0;
                        string text = "";
                        double r = random.NextDouble();
                        if (r > 0.0 && r <= 0.1)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(30 + j * 10, 40 + j * 10);
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        else if (r > 0.1 && r <= 0.2)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(90 - ((6 - j) * 6), 100 - ((6 - j) * 6));
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        else if (r > 0.2 && r <= 0.3)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(40, 60);
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        else if (r > 0.3 && r <= 0.35)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(90, 100);
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        else
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int nr = random.Next(60, 100);
                                text += "" + nr + " / ";
                                grade += nr;
                            }
                        }
                        average = (int)(grade / 6);
                        Console.Write("\"" + text.Substring(0, text.Count() - 3) + "\",");
                    }
                    else if (h == "Average Grade")
                    {
                        Console.Write("\"" + average + "\",");
                    }
                }
                Console.WriteLine();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            gradeGenerator();
        }
    }
}
