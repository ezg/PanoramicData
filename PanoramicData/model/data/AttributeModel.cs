using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data
{
    public abstract class AttributeModel : BindableBase
    {
        public AttributeModel(OriginModel originModel)
        {
            _originModel = originModel;
        }

        private OriginModel _originModel = null;
        public OriginModel OriginModel
        {
            get
            {
                return _originModel;
            }
            set
            {
                this.SetProperty(ref _originModel, value);
            }
        }

        public abstract string Name
        {
            get;
        }

        public abstract string AttributeVisualizationType
        {
            get;
        }
    }

    public class AttributeDataTypeConstants
    {
        public static string NVARCHAR = "nvarchar";
        public static string BIT = "bit";
        public static string DATE = "date";
        public static string FLOAT = "float";
        public static string GEOGRAPHY = "geography";
        public static string INT = "int";
        public static string TIME = "time";
        public static string GUID = "uniqueidentifier";
    }

    public class AttributeVisualizationTypeConstants
    {
        public static string NUMERIC = "numeric";
        public static string DATE = "date";
        public static string TIME = "time";
        public static string GEOGRAPHY = "geography";
        public static string CATEGORY = "category";
        public static string ENUM = "enum";
        public static string BOOLEAN = "boolean";
    }
}
