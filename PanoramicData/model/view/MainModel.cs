using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using PanoramicData.controller.input;

namespace PanoramicData.model.view
{
    public class MainModel : BindableBase
    {
        private ObservableCollection<DatasetConfiguration> _datasetConfigurations = new ObservableCollection<DatasetConfiguration>();
        public ObservableCollection<DatasetConfiguration> DatasetConfigurations
        {
            get
            {
                return _datasetConfigurations;
            }
        }

        private string _errorMessage;

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                this.SetProperty(ref _errorMessage, value);
            }
        }
    }
}
