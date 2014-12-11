using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DataExplorer
{
    public class TableDescriptor
    {
        private static int nextTableId = 1;
        [XmlIgnore]
        public string TableId { get; set; }
        public string TableName { get; set; }
        public string TableLabel { get; set; }
        public List<TableFieldDescriptor> Fields { get; set; }
        public List<TableDependencyDescriptor> Dependencies { get; set; }

        public TableDescriptor()
        {
            Fields = new List<TableFieldDescriptor>();
            Dependencies = new List<TableDependencyDescriptor>();
            TableId = "T" + nextTableId++;
        }
    }

    public class TableFieldDescriptor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static int nextFieldId = 1;
        [XmlIgnore]
        public string FieldId { get; set; }
        
        private string _fieldName = "";
        public string FieldName
        {
            get
            {
                return _fieldName;
            }
            set
            {
                _fieldName = value;
                NotifyPropertyChanged("FieldName");
            }
        }

        private string _fieldLabel = "";
        public string FieldLabel
        {
            get
            {
                return _fieldLabel;
            }
            set
            {
                _fieldLabel = value;
                NotifyPropertyChanged("FieldLabel");
            }
        }

        private bool _visible = true;
        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
                NotifyPropertyChanged("Visible");
            }
        }
        
        public int OrderNumber { get; set; }

        public DataType DataType { get; set; }
        public IEnumerable<DataType> DataTypeValues
        {
            get
            {
                return Enum.GetValues(typeof(DataType)).Cast<DataType>();
            }
        }

        public TableFieldDescriptor()
        {
            OrderNumber = int.MaxValue;
            FieldId = "F" + nextFieldId++;
        }

        // NotifyPropertyChanged will raise the PropertyChanged event, 
        // passing the source property that is being updated.
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class TableDependencyDescriptor
    {
        public string FromColumnName { get; set; }
        public string ToColumnName { get; set; }
        public int OrderNumber { get; set; }
        // eg: $1 / $2 (where $1 is the first fieldname in ConversionFieldNames
        public string Conversion { get; set; }
        public List<string> ConversionFieldNames { get; set; }
        public TableDescriptor TableDescriptor { get; set; }
        public VisualRepresentation VisualRepresentation { get; set; }
        public IEnumerable<VisualRepresentation> VisualRepresentationValues
        {
            get
            {
                return Enum.GetValues(typeof(VisualRepresentation)).Cast<VisualRepresentation>();
            }
        }

        public DataType DataType { get; set; }
        public IEnumerable<DataType> DataTypeValues
        {
            get
            {
                return Enum.GetValues(typeof(DataType)).Cast<DataType>();
            }
        }

        public TableDependencyDescriptor()
        {
            OrderNumber = int.MaxValue;
        }

        public TableReference Convert(DbDataReader reader, TableDescriptor parent)
        {
            string text = Conversion;
            int i = 1;
            string id = null;
            foreach (var fieldName in ConversionFieldNames)
            {
                id = null;
                foreach (var tfd in TableDescriptor.Fields)
                {
                    if (tfd.FieldName == fieldName)
                    {
                        id = tfd.FieldId;
                        break;
                    }
                }
                if (id != null)
                {
                    if (text != null)
                    {
                        text = text.Replace("$" + i, reader.GetValue(reader.GetOrdinal(id)).ToString());
                    }
                }
                i++;
            }
            TableReference tr = new TableReference();
            tr.Text = text;

            id = null;
            foreach (var tfd in parent.Fields)
            {
                if (tfd.FieldName == FromColumnName)
                {
                    id = tfd.FieldId;
                    break;
                }
            }
            tr.Id = reader.GetValue(reader.GetOrdinal(id));
            if (tr.Id is System.DBNull)
            {
                tr.Id = null;
            }
            tr.TableDependencyDescriptor = this;
            return tr;
        }
    }

    public class TableReference
    {
        public string Text { get; set; }
        public object Id { get; set; }
        public TableDependencyDescriptor TableDependencyDescriptor { get; set; }

        public TableReference()
        {
            Id = null;
        }

        public override string ToString()
        {
            return Text;
        }

    }

    public enum VisualRepresentation { COLLAPS, EXPAND, LINK, HIDE }

    public enum DataType { Unspecified, Address, Number, Text, Boolean, Date }
}
