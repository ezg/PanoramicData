using System;
using System.Collections.Generic;
using System.Data.Common;
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
using System.Xml.Serialization;
using Npgsql;

namespace KnowledgeSketchSchemaParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static DbConnection conn = null;
        private MainWindowViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();

            this.viewModel = new MainWindowViewModel();
            this.DataContext = viewModel;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string sConnectionString = "server=localhost;user=postgres;database=booktown;port=5432;password=browngfx1;";
            conn = new NpgsqlConnection(sConnectionString);
            conn.Open();

            List<TableDescriptor> descriptors = new List<TableDescriptor>();

            // author Table
            TableDescriptor descriptor = createTableDescriptor("authors", "Author");
            descriptors.Add(descriptor);

            // books Table
            descriptor = createTableDescriptor("books", "Book");
            descriptors.Add(descriptor);

            // subjects Table
            descriptor = createTableDescriptor("subjects", "Subject");
            descriptors.Add(descriptor);

            List<TableLinkDescriptor> links = generateLinks(descriptors);

            DBDescriptor dbd = new DBDescriptor();
            dbd.Links = links;
            dbd.Tables = descriptors;

            XmlSerializer serializer = new XmlSerializer(typeof(DBDescriptor));
            TextWriter tw = new StreamWriter("test.xml");
            serializer.Serialize(tw, dbd);

        }

        static void saveToFile(TableDescriptor descriptor, string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TableDescriptor));
            TextWriter tw = new StreamWriter(filename);
            serializer.Serialize(tw, descriptor);
            tw.Close();
        }

        static TableDescriptor createTableDescriptor(string tablename, string tablelabel)
        {
            TableDescriptor descriptor = new TableDescriptor();
            descriptor.TableName = tablename;
            descriptor.TableLabel = tablelabel;
            generateFields(descriptor);
            generateDescription(descriptor);
            return descriptor;
        }

        static void generateFields(TableDescriptor descriptor)
        {
            string query = "select a.attname from pg_attribute a, pg_class c where a.attrelid = c.relfilenode and c.relname = '" + descriptor.TableName + "' and attnum > 0 and attstattarget != 0";

            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = query;
            DbDataReader re = cmd.ExecuteReader();

            while (re.Read())
            {
                TableFieldDescriptor tfd = new TableFieldDescriptor();
                tfd.FieldName = re.GetString(0);
                descriptor.Fields.Add(tfd);
            }

            re.Close();
        }

        static void generateDescription(TableDescriptor descriptor)
        {
            string query =
               "select " +
               "    a.attname as field_name, d.description as header_name " +
               "from " +
               "    pg_description d, pg_class c, pg_attribute a " +
               "where " +
               "    d.objoid = c.relfilenode and " +
               "    a.attrelid = c.relfilenode and " +
               "    c.relname = '" + descriptor.TableName + "' and " +
               "    d.objsubid = a.attnum";

            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = query;
            DbDataReader re = cmd.ExecuteReader();

            while (re.Read())
            {
                foreach (var tfd in descriptor.Fields)
                {
                    if (tfd.FieldName == re.GetString(0))
                    {
                        tfd.FieldLabel = re.GetString(1);
                        break;
                    }
                }
            }

            re.Close();
        }

        static List<TableLinkDescriptor> generateLinks(List<TableDescriptor> descriptors)
        {
            List<TableLinkDescriptor> links = new List<TableLinkDescriptor>();

            foreach (var descriptor in descriptors)
            {
                string query =
                    "select " +
                    "    tot.relname as to_table, " +
                    "    fratt.attname as from_column_name, " +
                    "    toatt.attname as to_column_name " +
                    "from " +
                    "    pg_constraint as cot, " +
                    "    pg_class as tot, " +
                    "    pg_class as frt, " +
                    "    pg_attribute as fratt, " +
                    "    pg_attribute as toatt " +
                    "where " +
                    "    frt.oid = cot.conrelid and " +
                    "    tot.oid = cot.confrelid and " +
                    "    cot.conkey[1] = fratt.attnum and " +
                    "    cot.conrelid = fratt.attrelid and " +
                    "    cot.confkey[1] = toatt.attnum and " +
                    "    cot.confrelid = toatt.attrelid and " +
                    "    frt.relname = '" + descriptor.TableName + "'";

                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = query;
                DbDataReader re = cmd.ExecuteReader();

                while (re.Read())
                {
                    bool found = false;
                    TableDescriptor toTable = null;
                    foreach (var d in descriptors)
                    {
                        if (d.TableName == re.GetString(0)) 
                        {
                            found = true;
                            toTable = d;
                            break;
                        }
                    }

                    if (found)
                    {
                        TableLinkDescriptor link = new TableLinkDescriptor();
                        links.Add(link);
                        link.ToTable = toTable;
                        link.ToTableName = toTable.TableName;
                        link.FromTable = descriptor;
                        link.FromTableName = descriptor.TableName;

                        foreach (var tfd in link.ToTable.Fields)
                        {
                            if (tfd.FieldName == re.GetString(2))
                            {
                                link.ToField = tfd;
                                link.ToFieldName = tfd.FieldName;
                                break;
                            }
                        }

                        foreach (var tfd in link.FromTable.Fields)
                        {
                            if (tfd.FieldName == re.GetString(1))
                            {
                                link.FromField = tfd;
                                link.FromFieldName = tfd.FieldName;
                                break;
                            }
                        }
                    }
                }
                re.Close();
            }

            return links;
        }
    }
}
