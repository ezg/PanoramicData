using Microsoft.Practices.Prism.Mvvm;
using PanoramicData.controller.data;
using PanoramicData.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewResultItemModel : ExtendedBindableBase
    {
        public VisualizationViewResultItemModel()
        {
        }

        private Dictionary<AttributeViewModel, VisualizationViewResultItemValueModel> _values = new Dictionary<AttributeViewModel, VisualizationViewResultItemValueModel>();
        public Dictionary<AttributeViewModel, VisualizationViewResultItemValueModel> Values
        {
            get
            {
                return _values;
            }
            set
            {
                this.SetProperty(ref _values, value);
            }
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
