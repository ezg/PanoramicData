using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data
{
    public abstract class OriginModel : BindableBase
    {
        private SchemaModel _schemaModel = null;
        public SchemaModel SchemaModel
        {
            get
            {
                return _schemaModel;
            }
            set
            {
                this.SetProperty(ref _schemaModel, value);
            }
        }

        public abstract string Name
        {
            get;
        }

        public abstract List<AttributeModel> AttributeModels
        {
            get;
        }

        public abstract List<OriginModel> OriginModels
        {
            get;
        }
    }
}
