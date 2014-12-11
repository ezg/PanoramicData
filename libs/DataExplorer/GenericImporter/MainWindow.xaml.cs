using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;

namespace GenericImporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Column> _columns = new List<Column>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Import_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "Excel Files 97 - 2003 (.xls)|*.xls|Excel Files (.xlsx)|*.xlsx|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = true;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                // Open the selected file to read.
                tiFilename.Text = openFileDialog1.FileName;
                CovertExcelToCsv(tiFilename.Text);
            }
        }

        private void Run_OnClick(object sender, RoutedEventArgs e)
        {
            if (tiFilename.Text != "")
            {
                CovertExcelToCsv(tiFilename.Text);
            }
        }

        private void FirstRow_OnChecked(object sender, RoutedEventArgs e)
        {
            if (tiFilename.Text != "")
            {
                CovertExcelToCsv(tiFilename.Text);
            }
        }

        private void CovertExcelToCsv(string excelFilePath, int worksheetNumber = 1)
        {
            if (!File.Exists(excelFilePath)) throw new FileNotFoundException(excelFilePath);

            // connection string
            // old excel
            //var cnnStr = String.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"Excel 8.0;IMEX=1;HDR=NO\"", excelFilePath);

            // new excel 
            var cnnStr = String.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0 xml;IMEX=1;HDR=NO;'", excelFilePath);
            var cnn = new OleDbConnection(cnnStr);

            // get schema, then data
            var dt = new DataTable();
            try
            {
                cnn.Open();
                var schemaTable = cnn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                if (schemaTable.Rows.Count < worksheetNumber) throw new ArgumentException("The worksheet number provided cannot be found in the spreadsheet");
                string worksheet = schemaTable.Rows[worksheetNumber - 1]["table_name"].ToString().Replace("'", "");
                string sql = String.Format("select * from [{0}]", worksheet);
                var da = new OleDbDataAdapter(sql, cnn);
                da.Fill(dt);
            }
            catch (Exception e)
            {
                // ???
                throw e;
            }
            finally
            {
                // free resources
                cnn.Close();
            }

            _columns.Clear();

            // write out CSV data
            bool firstRow = true;
            int count = 1;
            foreach (DataRow row in dt.Rows)
            {

                bool firstLine = true;
                foreach (DataColumn col in dt.Columns)
                {
                    if (!firstLine) { 
                        Console.Write(","); 
                    } 
                    else { 
                        firstLine = false; 
                    }
                    var data = row[col.ColumnName].ToString().Replace("\"", "\"\"");
                    Console.Write(String.Format("\"{0}\"", data));
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }

    public class Column
    {
        public string Name { get; set; }
    }
}
