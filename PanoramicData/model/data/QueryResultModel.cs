using Microsoft.Practices.Prism.Mvvm;
using PanoramicData.controller.data;
using PanoramicData.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data
{
    public class QueryResultModel : ExtendedBindableBase
    {
        private int _resultCount = -1;
        public int ResultCount
        {
            get
            {
                return _resultCount;
            }
            set
            {
                this.SetProperty(ref _resultCount, value);
            }
        }

        private AsyncVirtualizingCollection<QueryResultItemModel> _queryResultItemModels = null;
        public AsyncVirtualizingCollection<QueryResultItemModel> QueryResultItemModels
        {
            get
            {
                return _queryResultItemModels;
            }
            set
            {
                if (_queryResultItemModels != null)
                {
                    _queryResultItemModels.PropertyChanged -= _queryResultItemModels_PropertyChanged;
                }

                this.SetProperty(ref _queryResultItemModels, value);

                if (_queryResultItemModels != null)
                {
                    _queryResultItemModels.PropertyChanged += _queryResultItemModels_PropertyChanged;
                }
            }
        }

        void _queryResultItemModels_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ResultCount = _queryResultItemModels.Count;
        }
    }
}
