﻿using PanoramicData.model.view;
using PanoramicData.model.view;
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

            _linkModels.CollectionChanged += LinkModels_CollectionChanged;
        }

        void LinkModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            bool fire = false;
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if ((item as LinkModel).ToQueryModel == this)
                    {
                        (item as LinkModel).FromQueryModel.QueryModelUpdated -= FromQueryModel_QueryModelUpdated;
                        fire = true;
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if ((item as LinkModel).ToQueryModel == this)
                    {
                        (item as LinkModel).FromQueryModel.QueryModelUpdated += FromQueryModel_QueryModelUpdated;
                        fire = true;
                    }
                }
            }
            if (fire)
            {
                FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
            }
        }

        void FromQueryModel_QueryModelUpdated(object sender, QueryModelUpdatedEventArgs e)
        {
            FireQueryModelUpdated(QueryModelUpdatedEventType.Links);
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

        private VisualizationType _visualizationType;
        public VisualizationType VisualizationType
        {
            get
            {
                return _visualizationType;
            }
            set
            {
                this.SetProperty(ref _visualizationType, value);
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

        public void ClearFilterModels(bool fireUpdate = true)
        {
            _filterModels.Clear();
            FireQueryModelUpdated(QueryModelUpdatedEventType.FilterModels);
        }

        public void AddFilterModels(List<FilterModel> filterModels, object sender)
        {
            _filterModels.AddRange(filterModels);
            FireQueryModelUpdated(QueryModelUpdatedEventType.FilterModels);
        }

        public void AddFilterModel(FilterModel filterModel, object sender)
        {
            _filterModels.Add(filterModel);
            FireQueryModelUpdated(QueryModelUpdatedEventType.FilterModels);
        }

        public void RemoveFilterModel(FilterModel filterModel, object sender)
        {
            _filterModels.Remove(filterModel);
            FireQueryModelUpdated(QueryModelUpdatedEventType.FilterModels);
        }

        public void RemoveFilterModels(List<FilterModel> filterModels, object sender)
        {
            foreach (var filterItem in filterModels)
            {
                _filterModels.Remove(filterItem);
            }
            if (filterModels.Count > 0)
            {
                FireQueryModelUpdated(QueryModelUpdatedEventType.FilterModels);
            }
        }

        public void FireQueryModelUpdated(QueryModelUpdatedEventType type)
        {
            if (type == QueryModelUpdatedEventType.Structure)
            {
                ClearFilterModels(false);
            }
            if (QueryModelUpdated != null)
            {
                QueryModelUpdated(this, new QueryModelUpdatedEventArgs(type));
            }

            if (type != QueryModelUpdatedEventType.FilterModels)
            {
                SchemaModel.QueryExecuter.ExecuteQuery(this);
            }
        }

        public string GetDataType(AttributeOperationModel attributeOperationModel, bool considerGrouping)
        {
            if (!considerGrouping)
            {
                return attributeOperationModel.AttributeModel.AttributeDataType;
            }
            else
            {
                if (attributeOperationModel.IsBinned)
                {
                    return AttributeDataTypeConstants.NVARCHAR;
                }

                List<AttributeOperationModel> allOperationModels = AttributeOperationModels;
                bool isGroupingApplied = allOperationModels.Any(aom => aom.IsBinned || aom.IsGrouped);

                if (!isGroupingApplied)
                {
                    if (allOperationModels.Count(aom => aom.AggregateFunction != AggregateFunction.None) > 0)
                    {
                        if (attributeOperationModel.AggregateFunction == AggregateFunction.Avg ||
                            attributeOperationModel.AggregateFunction == AggregateFunction.Sum ||
                            attributeOperationModel.AggregateFunction == AggregateFunction.Max ||
                            attributeOperationModel.AggregateFunction == AggregateFunction.Min)
                        {
                            if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.TIME)
                            {
                                return AttributeDataTypeConstants.TIME;
                            }
                            if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.NVARCHAR)
                            {
                                return AttributeDataTypeConstants.NVARCHAR;
                            }
                            if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.BIT)
                            {
                                return AttributeDataTypeConstants.BIT;
                            }
                            return AttributeDataTypeConstants.FLOAT;
                        }
                        else if (attributeOperationModel.AggregateFunction == AggregateFunction.Count)
                        {
                            return AttributeDataTypeConstants.INT;
                        }
                        return AttributeDataTypeConstants.NVARCHAR;
                    }
                    else
                    {
                        return attributeOperationModel.AttributeModel.AttributeDataType;
                    }
                }
                else
                {
                    if (attributeOperationModel.AggregateFunction == AggregateFunction.None &&
                        allOperationModels.Any(aom => (aom.IsBinned || aom.IsGrouped) && aom.AttributeModel == attributeOperationModel.AttributeModel))
                    {
                        return attributeOperationModel.AttributeModel.AttributeDataType;
                    }

                    if (attributeOperationModel.AggregateFunction == AggregateFunction.Avg ||
                        attributeOperationModel.AggregateFunction == AggregateFunction.Sum ||
                        attributeOperationModel.AggregateFunction == AggregateFunction.Max ||
                        attributeOperationModel.AggregateFunction == AggregateFunction.Min)
                    {
                        if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.NVARCHAR)
                        {
                            return AttributeDataTypeConstants.NVARCHAR;
                        }
                        if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.TIME)
                        {
                            return AttributeDataTypeConstants.TIME;
                        }
                        if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.BIT)
                        {
                            return AttributeDataTypeConstants.BIT;
                        }
                        return AttributeDataTypeConstants.FLOAT;
                    }
                    else if (attributeOperationModel.AggregateFunction == AggregateFunction.Count)
                    {
                        return AttributeDataTypeConstants.INT;
                    }
                    else
                    {
                        return AttributeDataTypeConstants.NVARCHAR;
                    }
                }
            }
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

    public enum QueryModelUpdatedEventType { Structure, Links, FilterModels }
    
    public enum VisualizationType { Table, Bar, Map, Plot, Line }
}
