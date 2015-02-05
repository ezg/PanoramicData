using PanoramicData.model.view;
using PanoramicData.model.view_new;
using PanoramicData.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data
{
    public class QueryModel : ExtendedBindableBase
    {
        public delegate void QueryModelUpdatedHandler(object sender, QueryModelUpdatedEventArgs e);
        public event QueryModelUpdatedHandler QueryModelUpdated;

        public QueryModel(SchemaModel schemaModel)
        {
            _schemaModel = schemaModel;
            _queryResultModel = new QueryResultModel();

            foreach (var attributeFunction in Enum.GetValues(typeof(AttributeFunction)).Cast<AttributeFunction>())
            {
                _attributeFunctionOperationModels.Add(attributeFunction, new ObservableCollection<AttributeOperationModel>());
                _attributeFunctionOperationModels[attributeFunction].CollectionChanged += AttributeOperationModel_CollectionChanged;
            }
        }

        void AttributeOperationModel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    (item as AttributeOperationModel).PropertyChanged -= AttributeOperationModel_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    (item as AttributeOperationModel).QueryModel = this;
                    (item as AttributeOperationModel).PropertyChanged += AttributeOperationModel_PropertyChanged;
                }
            }
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        void AttributeOperationModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);   
        }

        private QueryResultModel _queryResultModel = null;
        public QueryResultModel QueryResultModel
        {
            get
            {
                return _queryResultModel;
            }
            set
            {
                this.SetProperty(ref _queryResultModel, value);
            }
        }

        private SchemaModel _schemaModel = null;
        public SchemaModel SchemaModel
        {
            get
            {
                return _schemaModel;
            }
            set
            {
                this.SetProperty(ref _schemaModel, value);
            }
        }

        private FilteringOperation _filteringOperation = FilteringOperation.AND;
        public FilteringOperation FilteringOperation
        {
            get
            {
                return _filteringOperation;
            }
            set
            {
                this.SetProperty(ref _filteringOperation, value);
            }
        }

        public List<AttributeOperationModel> AttributeOperationModels
        {
            get
            {
                List<AttributeOperationModel> retList = new List<AttributeOperationModel>();
                foreach (var key in _attributeFunctionOperationModels.Keys)
                {
                    retList.AddRange(_attributeFunctionOperationModels[key]);
                }
                return retList;
            }
        }

        private Dictionary<AttributeFunction, ObservableCollection<AttributeOperationModel>> _attributeFunctionOperationModels = new Dictionary<AttributeFunction, ObservableCollection<AttributeOperationModel>>();
        public void AddFunctionAttributeOperationModel(AttributeFunction attributeFunction, AttributeOperationModel attributeOperationModel)
        {
            _attributeFunctionOperationModels[attributeFunction].Add(attributeOperationModel);
        }

        public void RemoveFunctionAttributeOperationModel(AttributeFunction attributeFunction, AttributeOperationModel attributeOperationModel)
        {
            _attributeFunctionOperationModels[attributeFunction].Remove(attributeOperationModel);
        }

        public void RemoveAttributeOperationModel(AttributeOperationModel attributeOperationModel)
        {
            foreach (var key in _attributeFunctionOperationModels.Keys)
            {
                if (_attributeFunctionOperationModels[key].Contains(attributeOperationModel))
                {
                    RemoveFunctionAttributeOperationModel(key, attributeOperationModel);
                }
            }
        }

        public ObservableCollection<AttributeOperationModel> GetFunctionAttributeOperationModel(AttributeFunction attributeFunction)
        {
            return _attributeFunctionOperationModels[attributeFunction];
        }

        private ObservableCollection<LinkModel> _linkModels = new ObservableCollection<LinkModel>();
        public ObservableCollection<LinkModel> LinkModels
        {
            get
            {
                return _linkModels;
            }
        }

        private List<FilterModel> _filterModels = new List<FilterModel>();
        public List<FilterModel> FilterModels
        {
            get
            {
                return _filterModels;
            }
        }

        public void ClearFilterItems()
        {
            _filterModels.Clear();
            FireQueryModelUpdated(QueryModelUpdatedEventType.FilterItems);
        }

        public void AddFilterItems(List<FilterModel> filterItems, object sender)
        {
            _filterModels.AddRange(filterItems);
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        public void AddFilterItem(FilterModel filterItem, object sender)
        {
            _filterModels.Add(filterItem);
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        public void RemoveFilterItem(FilterModel filterItem, object sender)
        {
            _filterModels.Remove(filterItem);
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        public void RemoveFilterItems(List<FilterModel> filterItems, object sender)
        {
            foreach (var filterItem in filterItems)
            {
                _filterModels.Remove(filterItem);
            }
            if (filterItems.Count > 0)
            {
                FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
            }
        }

        public void FireQueryModelUpdated(QueryModelUpdatedEventType type)
        {
            if (QueryModelUpdated != null)
            {
                QueryModelUpdated(this, new QueryModelUpdatedEventArgs(type));
            }
            SchemaModel.QueryExecuter.ExecuteQuery(this);
        }
    }

    public class QueryModelUpdatedEventArgs : EventArgs
    {
        public QueryModelUpdatedEventType QueryModelUpdatedEventType { get; set; }

        public QueryModelUpdatedEventArgs(QueryModelUpdatedEventType type)
            : base()
        {
            QueryModelUpdatedEventType = type;
        }
    }

    public enum QueryModelUpdatedEventType { Structure, Links, FilterItems }

}
