using Microsoft.Practices.Prism.Mvvm;
using PanoramicData.model.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class AttributeViewModel : BindableBase
    {
        private AttributeModel _attributeModel = null;
        public AttributeModel AttributeModel
        {
            get
            {
                return _attributeModel;
            }
            set
            {
                this.SetProperty(ref _attributeModel, value);
            }
        }

        private AttributeOperationModel _attributeOperationModel = null;
        public AttributeOperationModel AttributeOperationModel
        {
            get
            {
                return _attributeOperationModel;
            }
            set
            {
                this.SetProperty(ref _attributeOperationModel, value);
            }
        }
    }
}
