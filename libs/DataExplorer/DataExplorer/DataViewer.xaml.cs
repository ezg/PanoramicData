using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Xml.Serialization;
using Npgsql;

namespace DataExplorer
{
    /// <summary>
    /// Interaction logic for DataViewer.xaml
    /// </summary>
    public partial class DataViewer : UserControl
    {
        public List<TableDescriptor> TableDescriptors = new List<TableDescriptor>();

        public DataViewer()
        {
            InitializeComponent();
        }

        public void SetTableDescriptors(List<TableDescriptor> tableDescriptors)
        {
            TableDescriptors = tableDescriptors;
            dataGrid.Columns.Clear();
            cmbTables.ItemsSource = TableDescriptors;
            cmbTables.DisplayMemberPath = "TableLabel";
            cmbTables.SelectedIndex = 0;
        }

        public void Execute(TableDescriptor tableDescriptor, List<Filter> filters)
        {
            string sConnectionString = "server=localhost;user=openpg;database=george;port=5432;password=openpgpwd;";
            DbConnection conn = new NpgsqlConnection(sConnectionString);
            conn.Open();
            
            // initialize the columnmapping list based on the tabledescriptor
            List<ColumnMapping> columnMappings = createColumnMapping(tableDescriptor);

            // create the datatable 
            DataTable dataTable = createDataTable(createQuery(tableDescriptor, filters), conn, columnMappings, tableDescriptor);
            dataGrid.ItemsSource = dataTable.DefaultView;
            dataGrid.Columns.Clear();

            int i = 0;
            foreach (ColumnMapping columnMapping in columnMappings)
            {
                DataGridColumn dc = createDataGridColumn(dataTable.Columns[i], columnMapping);
                if (dc != null)
                {
                    dataGrid.Columns.Add(dc);
                }
                i++;
            }
        }

        private DataGridColumn createDataGridColumn(DataColumn dataColumn, ColumnMapping columnMapping)
        {
            DataGridColumn dataGridColumn = null;

            if (columnMapping.TableFieldDescriptor != null)
            {
                if (dataColumn.DataType == typeof(DateTime))
                {
                    dataGridColumn = getDateTemplate(columnMapping.TableFieldDescriptor.FieldId);
                }
                else if (dataColumn.DataType == typeof(byte[]))
                {
                    dataGridColumn = getTextTemplate(columnMapping.TableFieldDescriptor.FieldId);
                }
                else 
                {
                    dataGridColumn = getTextTemplate(columnMapping.TableFieldDescriptor.FieldId);
                }
                dataGridColumn.Header = columnMapping.TableFieldDescriptor.FieldLabel != null ? columnMapping.TableFieldDescriptor.FieldLabel : columnMapping.TableFieldDescriptor.FieldName;
            }
            else
            {
                if (columnMapping.TableDependencyDescriptor.VisualRepresentation == VisualRepresentation.LINK)
                {
                    dataGridColumn = getLinkTemplate(columnMapping.TableDependencyDescriptor.TableDescriptor.TableId, columnMapping.TableDependencyDescriptor);
                }
                else
                {
                    dataGridColumn = getReferenceTemplate(columnMapping.TableDependencyDescriptor.TableDescriptor.TableId);
                }
                dataGridColumn.Header = columnMapping.TableDependencyDescriptor.TableDescriptor.TableLabel;
            }

            return dataGridColumn;
        }

        private DataGridColumn getLinkTemplate(string columnname, TableDependencyDescriptor tableDependencyDescriptor)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(Button));
            //factory.SetValue(Image.WidthProperty, 100d);
            //factory.SetValue(Image.HeightProperty, 100d);
            factory.AddHandler(Button.ClickEvent, new RoutedEventHandler(linkClicked));
            factory.SetValue(Button.CommandParameterProperty, columnname);

            Binding b = new Binding(columnname);
            factory.SetValue(Button.ContentProperty, b);

            b = new Binding(columnname);
            b.Converter = new TableReferenceToVisibilityConvertor();
            factory.SetValue(Button.VisibilityProperty, b);

            DataTemplate CellTemplate = new DataTemplate();
            CellTemplate.VisualTree = factory;

            col.CellTemplate = CellTemplate;
            col.IsReadOnly = true;

            return col;
        }

        private void linkClicked(object sender, RoutedEventArgs e)
        {
           // Execute((TableDescriptor)cmbTables.SelectedItem);\
            DataRowView row = ((FrameworkElement)sender).DataContext as DataRowView;
            string columnname = ((Button)sender).CommandParameter as string;

            // find tabledescriptor
            TableReference tr = (TableReference) row[columnname];
            foreach (var td in TableDescriptors)
            {
                if (td.TableName == tr.TableDependencyDescriptor.TableDescriptor.TableName)
                {
                    List<Filter> filters = new List<Filter>();
                    Filter f = new Filter();
                    filters.Add(f);
                    f.Value = tr.Id;
                    f.FieldName = tr.TableDependencyDescriptor.ToColumnName;
                    f.FilterType = FilterType.EQUALS;
                    Execute(td, filters);
                }
            }
        }

        private DataGridColumn getReferenceTemplate(string columnname)
        {
            DataGridTextColumn c = new DataGridTextColumn();
            c.Binding = new Binding(columnname);
            c.IsReadOnly = true;
            return c;
        }

        private DataGridColumn getTextTemplate(string columnname)
        {
            DataGridTextColumn c = new DataGridTextColumn();
            c.Binding = new Binding(columnname);
            c.IsReadOnly = true;
            return c;
        }

        private DataGridColumn getImageTemplate(string columnname)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(Image));
            //factory.SetValue(Image.WidthProperty, 100d);
            //factory.SetValue(Image.HeightProperty, 100d);

            Binding b = new Binding(columnname);
            b.Converter = new ByteArrayToImageConverter();
            factory.SetValue(Image.SourceProperty, b);

            DataTemplate CellTemplate = new DataTemplate();
            CellTemplate.VisualTree = factory;

            col.CellTemplate = CellTemplate;
            col.IsReadOnly = true;

            return col;
        }

        private DataGridColumn getDateTemplate(string columnname)
        {
            DataGridTextColumn c = new DataGridTextColumn();
            c.Binding = new Binding(columnname);
            c.Binding.StringFormat = "d";
            c.IsReadOnly = true;

            return c;
        }

        private DataTable createDataTable(string query, DbConnection conn, List<ColumnMapping> columnMappings, TableDescriptor tableDescriptor)
        {
            DataTable dataTable = new DataTable();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = query;
            DbDataReader re = cmd.ExecuteReader();

            bool firstRow = true;
            while (re.Read())
            {
                // create columns in the beginning
                if (firstRow)
                {
                    foreach (var columnMapping in columnMappings) 
                    {
                        DataColumn dc = null;
                        if (columnMapping.TableFieldDescriptor != null)
                        {
                            dc = new DataColumn(columnMapping.TableFieldDescriptor.FieldId, re.GetFieldType(re.GetOrdinal(columnMapping.TableFieldDescriptor.FieldId)));
                        }
                        else
                        {
                            dc = new DataColumn(columnMapping.TableDependencyDescriptor.TableDescriptor.TableId, typeof(TableReference));
                        }
                        dataTable.Columns.Add(dc);
                    }
                    firstRow = false;
                }

                dataTable.Rows.Add();
                for (int i = 0; i < columnMappings.Count; i++)
                {
                    DataRow row = dataTable.Rows[dataTable.Rows.Count - 1];

                    ColumnMapping columnMapping = columnMappings[i];
                    if (columnMapping.TableFieldDescriptor != null)
                    {
                        row[i] = re.GetValue(re.GetOrdinal(columnMapping.TableFieldDescriptor.FieldId));
                    }
                    else
                    {
                        row[i] = columnMapping.TableDependencyDescriptor.Convert(re, tableDescriptor);
                    }
                }
            }

            return dataTable;
        }

        private List<ColumnMapping> createColumnMapping(TableDescriptor tableDescriptor)
        {
            List<ColumnMapping> columnMappings = new List<ColumnMapping>();
            foreach (var tdf in tableDescriptor.Fields)
            {
                if (tdf.Visible)
                {
                    ColumnMapping cm = new ColumnMapping();
                    cm.OrderNumber = tdf.OrderNumber;
                    cm.TableFieldDescriptor = tdf;
                    columnMappings.Add(cm);
                }
            }
            foreach (var dependent in tableDescriptor.Dependencies)
            {
                if (dependent.VisualRepresentation == VisualRepresentation.EXPAND)
                {
                    foreach (var tdf in dependent.TableDescriptor.Fields)
                    {
                        if (tdf.Visible)
                        {
                            ColumnMapping cm = new ColumnMapping();
                            cm.OrderNumber = tdf.OrderNumber;
                            cm.TableFieldDescriptor = tdf;
                            columnMappings.Add(cm);
                        }
                    }
                }
                else if (dependent.VisualRepresentation == VisualRepresentation.COLLAPS)
                {
                    ColumnMapping cm = new ColumnMapping();
                    cm.OrderNumber = dependent.OrderNumber;
                    cm.TableDependencyDescriptor = dependent;
                    columnMappings.Add(cm);
                }
                else if (dependent.VisualRepresentation == VisualRepresentation.LINK)
                {
                    ColumnMapping cm = new ColumnMapping();
                    cm.OrderNumber = dependent.OrderNumber;
                    cm.TableDependencyDescriptor = dependent;
                    columnMappings.Add(cm);
                }
                else if (dependent.VisualRepresentation == VisualRepresentation.HIDE)
                {
                }
            }
            return columnMappings.OrderBy(x => x.OrderNumber).ToList();
        }

        private string createQuery(TableDescriptor descriptor, List<Filter> filters)
        {
            string query = "SELECT\n";

            // select main table fields first
            foreach (var tdf in descriptor.Fields)
            {
                query += "\t" + descriptor.TableId + "." + tdf.FieldName + " as " + tdf.FieldId + ",\n";
            }

            // select dependent fields now
            foreach (var dependent in descriptor.Dependencies)
            {
                foreach (var tdf in dependent.TableDescriptor.Fields)
                {
                    query += "\t" + dependent.TableDescriptor.TableId + "." + tdf.FieldName + " as " + tdf.FieldId + ",\n";
                }
            }
            query = query.Substring(0, query.Length - 2);

            // from clause
            query += "\nFROM ";
            query += descriptor.TableName + " as " + descriptor.TableId + "\n";
            foreach (var dependent in descriptor.Dependencies)
            {
                query += "LEFT JOIN ";
                query += dependent.TableDescriptor.TableName + " as " + dependent.TableDescriptor.TableId + " ";
                query += "ON " + dependent.TableDescriptor.TableId + "." + dependent.ToColumnName + " = " +
                descriptor.TableId + "." + dependent.FromColumnName + "\n";
            }
            query = query.Substring(0, query.Length - 1);


            // where clause if we have filters
            if (filters.Count > 0)
            {
                query += "\nWHERE ";
                foreach (var filter in filters)
                {
                    /*string id = null;
                    // find corresponding id
                    foreach (var tdf in descriptor.Fields)
                    {
                        if (tdf.FieldName == filter.FieldName)
                        {
                            id = tdf.FieldId;
                            break;
                        }
                    }
                    if (id != null)
                    {*/
                        query += "\t" + descriptor.TableId + "." + filter.FieldName + " = " + filter.Value + " and\n";
                    //}
                }
                query = query.Substring(0, query.Length - 5);
            }

            return query;
        }

        private void btnGetData_Click_1(object sender, RoutedEventArgs e)
        {
            Execute((TableDescriptor)cmbTables.SelectedItem, new List<Filter>());
        }
    }

    public class ColumnMapping
    {
        public TableFieldDescriptor TableFieldDescriptor { get; set; }
        public TableDependencyDescriptor TableDependencyDescriptor { get; set; }
        public int OrderNumber { get; set; }
    }

    public class Filter
    {
        public string FieldName { get; set; }
        public object Value { get; set; }
        public FilterType FilterType { get; set; }
    }

    public enum FilterType { EQUALS }

    public class TableReferenceToVisibilityConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TableReference tr = (TableReference)value;
            if (tr.Id == null)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            MemoryStream ms = new MemoryStream((byte[]) value);

            BitmapImage bi = new BitmapImage();
            //bi.BeginInit();
            bi.StreamSource = ms;
            //bi.EndInit();
            ms.Close();

            return bi;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
