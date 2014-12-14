using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data
{
    public abstract class SchemaModel : BindableBase
    {
        public abstract List<OriginModel> OriginModels
        {
            get;
        }

        public abstract Dictionary<CalculatedAttributeModel, string> CalculatedAttributeModels
        {
            get;
        }

        public abstract Dictionary<NamedAttributeModel, string> NamedAttributeModels
        {
            get;
        }
    }
}
