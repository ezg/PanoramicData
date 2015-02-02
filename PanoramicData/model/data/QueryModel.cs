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
                _attributeFunctionOperationModels[attributeFunction].CollectionChanged += QueryModel_CollectionChanged;
            }
        }

        void QueryModel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    (item as AttributeOperationModel).PropertyChanged -= QueryModel_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    (item as AttributeOperationModel).QueryModel = this;
                    (item as AttributeOperationModel).PropertyChanged += QueryModel_PropertyChanged;
                }
            }
        }

        void QueryModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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

        private Dictionary<AttributeFunction, ObservableCollection<AttributeOperationModel>> _attributeFunctionOperationModels = new Dictionary<AttributeFunction, ObservableCollection<AttributeOperationModel>>();
        public void AddFunctionAttributeOperationModel(AttributeFunction attributeFunction, AttributeOperationModel attributeOperationModel)
        {
            _attributeFunctionOperationModels[attributeFunction].Add(attributeOperationModel);
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        public void RemoveFunctionAttributeOperationModel(AttributeFunction attributeFunction, AttributeOperationModel attributeOperationModel)
        {
            _attributeFunctionOperationModels[attributeFunction].Remove(attributeOperationModel);
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        public void RemoveAttributeOperationModel(AttributeOperationModel attributeOperationModel)
        {
            foreach (var key in _attributeFunctionOperationModels.Keys)
            {
                if (_attributeFunctionOperationModels[key].Contains(attributeOperationModel))
                {
                    RemoveFunctionAttributeOperationModel(key, attributeOperationModel);
                    FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
                }
            }
        }

        public ObservableCollection<AttributeOperationModel> GetFunctionAttributeOperationModel(AttributeFunction attributeFunction)
        {
            return _attributeFunctionOperationModels[attributeFunction];
        }

        public List<AttributeOperationModel> GetAllAttributeOperationModel()
        {
            List<AttributeOperationModel> retList = new List<AttributeOperationModel>();
            foreach (var key in _attributeFunctionOperationModels.Keys)
            {
                retList.AddRange(_attributeFunctionOperationModels[key]);
            }
            return retList;
        }

        private List<FilterItem> _filterItems = new List<FilterItem>();
        public List<FilterItem> FilterItems
        {
            get
            {
                return _filterItems;
            }
        }

        public void ClearFilterItems()
        {
            _filterItems.Clear();
            FireQueryModelUpdated(QueryModelUpdatedEventType.FilterItems);
        }

        public void AddFilterItems(List<FilterItem> filterItems, object sender)
        {
            _filterItems.AddRange(filterItems);
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        public void AddFilterItem(FilterItem filterItem, object sender)
        {
            _filterItems.Add(filterItem);
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        public void RemoveFilterItem(FilterItem filterItem, object sender)
        {
            _filterItems.Remove(filterItem);
            FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        public void RemoveFilterItems(List<FilterItem> filterItems, object sender)
        {
            foreach (var filterItem in filterItems)
            {
                _filterItems.Remove(filterItem);
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
