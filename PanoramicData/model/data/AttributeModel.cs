using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data
{
    public class AttributeModel
    {
        private AttributeModel _attributeDataModel = null;
        public AttributeModel AttributeDataModel
        {
            get
            {
                return _attributeDataModel;
            }
            set
            {
                this.SetProperty(ref _attributeDataModel, value);
            }
        }
    }
}
