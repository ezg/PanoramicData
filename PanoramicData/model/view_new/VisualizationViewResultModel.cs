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
        private AsyncVirtualizingCollection<VisualizationViewResultItemModel> _visualizationViewResultItemModels = null;
        public AsyncVirtualizingCollection<VisualizationViewResultItemModel> VisualizationViewResultItemModels
        {
            get
            {
                return _visualizationViewResultItemModels;
            }
            set
            {
                this.SetProperty(ref _visualizationViewResultItemModels, value);
            }
        }
    }
}
