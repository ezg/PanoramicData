using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace KnowledgeSketchSchemaParser
{
    public class DBDescriptor
    {
        public List<TableDescriptor> Tables { get; set; }
        public List<TableLinkDescriptor> Links { get; set; }
    }

    public class TableDescriptor
    {
        private static int nextTableId = 1;
        [XmlIgnore]
        public string TableId { get; set; }
        public string TableName { get; set; }
        public string TableLabel { get; set; }
        public List<TableFieldDescriptor> Fields { get; set; }

        public TableDescriptor()
        {
            Fields = new List<TableFieldDescriptor>();
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
            FieldId = "F" + nextFieldId++;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class TableLinkDescriptor
    {
        [XmlIgnore]
        public TableFieldDescriptor FromField { get; set; }
        [XmlIgnore]
        public TableFieldDescriptor ToField { get; set; }
        [XmlIgnore]
        public TableDescriptor FromTable { get; set; }
        [XmlIgnore]
        public TableDescriptor ToTable { get; set; }

        public string FromFieldName { get; set; }
        public string ToFieldName { get; set; }
        public string FromTableName { get; set; }
        public string ToTableName { get; set; }
    }

    public enum DataType { Unspecified, Address, Number, Text, Boolean, Date }
}
