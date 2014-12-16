using Microsoft.Practices.Prism.Mvvm;
using PanoramicData.controller.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewResultModel : BindableBase
    {
        private AsyncVirtualizingCollection<PanoramicDataRow> _dataValues = null;
        public AsyncVirtualizingCollection<PanoramicDataRow> DataValues
        {
            get
            {
                return _dataValues;
            }
            set
            {
                this.SetProperty(ref _dataValues, value);
            }
        }
    }
}
