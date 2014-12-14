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
    }
}
