using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data.mssql
{
    public class MSSQLAttributeModel : AttributeModel
    {
        public MSSQLAttributeModel(OriginModel originModel, FieldInfo fieldInfo)
            : base(originModel)
        {
            _fieldInfo = fieldInfo;
        }

        private FieldInfo _fieldInfo = null;
        public FieldInfo FieldInfo
        {
            get
            {
                return _fieldInfo;
            }
            set
            {
                this.SetProperty(ref _fieldInfo, value);
            }
        }

        public override string Name
        {
            get
            {
                return _fieldInfo.Name;
            }
        }

        public override string AttributeVisualizationType
        {
            get
            {
                return _fieldInfo.VisualizationType;
            }
        }
    }
}
