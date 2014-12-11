using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
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
using System.Xml;
using System.Xml.Serialization;
using MySql.Data.MySqlClient;
using Npgsql;

namespace DataExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TableDescriptor descriptor = null;
        string _filename = @"C:\ez_projects\HumBub\starPad SDK\Apps\DataExplorer\DataExplorer\descriptor\hr_employee.xml";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.L && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                descriptor = Load(_filename);
                dataViewer.SetTableDescriptors(new List<TableDescriptor>(new TableDescriptor[] { descriptor }));
                listBox.ItemsSource = descriptor.Fields;
            }
            else if (e.Key == Key.T && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                foreach (var f in descriptor.Fields)
                {
                    f.Visible = !f.Visible;
                }
            }
            else if (e.Key == Key.S && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(TableDescriptor));
                TextWriter tw = new StreamWriter(_filename);
                serializer.Serialize(tw, descriptor);
                tw.Close();
            }
        }

        private TableDescriptor Load(string filename)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(TableDescriptor));
            TextReader tr = new StreamReader(filename);
            TableDescriptor descriptor = (TableDescriptor)deserializer.Deserialize(tr);
            tr.Close();
            return descriptor;
        }

        private void listBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            detailsGrid.DataContext = listBox.SelectedItem;
            detailsGrid.Visibility = Visibility.Visible;

            refGrid.DataContext = null;
            refGrid.Visibility = Visibility.Hidden;
            foreach (var reference in descriptor.Dependencies)
            {
                if (listBox.SelectedItem != null)
                {
                    if (reference.FromColumnName == ((TableFieldDescriptor)listBox.SelectedItem).FieldName)
                    {
                        refGrid.DataContext = reference;
                        refGrid.Visibility = Visibility.Visible;
                        break;
                    }
                }
            }
        }
    }

    public class BrushColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                {
                    return Brushes.Green;
                }
            }
            return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class StringListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string>)
            {
                List<string> list = value as List<string>;
                string text = "";
                foreach (var s in list)
                {
                    text += s + "; ";
                }
                return text;
            }
            else
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                string[] elements = ((string)value).Split(new char[] { ';' });
                List<string> ret = new List<string>();
                foreach (var elem in elements)
                {
                    if (elem.Trim() != "")
                    {
                        ret.Add(elem.Trim());
                    }
                }
                return ret;
            }
            else
            {
                return new List<string>();
            }
        }

    }
}
