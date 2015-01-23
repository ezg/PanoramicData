using Microsoft.Practices.Prism.Mvvm;
using PanoramicData.controller.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewResultItemModel : BindableBase
    {
        public VisualizationViewResultItemModel()
        {
        }
        
        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                this.SetProperty(ref _isSelected, value);
            }
        }
    }
}
