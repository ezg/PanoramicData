﻿using PanoramicData.model.data;
using PanoramicData.model.data.sim;
using PanoramicData.model.view_new;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using starPadSDK.AppLib;
using System.Threading;
using System.Dynamic;

namespace PanoramicData.controller.data.sim
{
    public class SimQueryExecuter : QueryExecuter
    {
        public override void ExecuteQuery(QueryModel queryModel)
        {
            IItemsProvider<QueryResultItemModel> itemsProvider = new SimItemsProvider(queryModel);
            AsyncVirtualizingCollection<QueryResultItemModel> dataValues = new AsyncVirtualizingCollection<QueryResultItemModel>(itemsProvider, 1000, 1000);
            queryModel.QueryResultModel.QueryResultItemModels = dataValues;
        }
    }

    public class SimItemsProvider : IItemsProvider<QueryResultItemModel>
    {
        private QueryModel _queryModel = null;
        private int _fetchCount = -1;

        public SimItemsProvider(QueryModel queryModel)
        {
            _queryModel = queryModel;
        }

        public int FetchCount()
        {
            _fetchCount = QueryEngine.ComputeQueryResult(_queryModel).Count;
            return _fetchCount;
        }

        public IList<QueryResultItemModel> FetchRange(int startIndex, int pageCount, out int overallCount)
        {
            Console.WriteLine("page : " + startIndex + " " + pageCount);
            //Thread.Sleep(10);

            IList<QueryResultItemModel> returnList = QueryEngine.ComputeQueryResult(_queryModel).Skip(startIndex).Take(pageCount).ToList();

            // reset selections
            foreach (var queryResultItemModel in returnList)
            {
                FilterModel filterQueryResultItemModel = new FilterModel(queryResultItemModel);
                foreach (var fi in _queryModel.FilterModels.ToArray())
                {
                    if (fi != null)
                    {
                        if (fi.Equals(filterQueryResultItemModel))
                        {
                            queryResultItemModel.IsSelected = true;
                        }
                    }
                }
            }

            overallCount = _fetchCount;
            return returnList;
        }
    }

    public class QueryEngine
    {
        public static List<QueryResultItemModel> ComputeQueryResult(QueryModel queryModel)
        {
            if (queryModel.AttributeOperationModels.Any())
            {
                var data = getFilteredData((queryModel.SchemaModel.OriginModels[0] as SimOriginModel).Data, queryModel, true);

                var results = data.
                    GroupBy(
                        item => getGroupByObject(item, queryModel),
                        item => item,
                        (key, g) => getQueryResultItemModel(key, g, queryModel)).
                        OrderBy(item => item, new ItemComparer(queryModel));

                return results.ToList();
            }
            else
            {
                return new List<QueryResultItemModel>();
            }
        }

        private static IEnumerable<Dictionary<AttributeModel, object>> getFilteredData(IEnumerable<Dictionary<AttributeModel, object>> data, QueryModel queryModel, bool first)
        {
            var filteredData = data;
            var linkModels = queryModel.LinkModels.Where(lm => lm.ToQueryModel == queryModel && lm.LinkType == LinkType.Filter);
            if (linkModels.Count() > 0)
            {
                filteredData = getFilteredData(data, linkModels.First().FromQueryModel, false);
                foreach (var linkModel in linkModels.Skip(1))
                {
                    if (queryModel.FilteringOperation == FilteringOperation.AND)
                    {
                        filteredData = filteredData.Intersect(getFilteredData(data, linkModel.FromQueryModel, false), new DataEqualityComparer(queryModel));
                    }
                    else if (queryModel.FilteringOperation == FilteringOperation.OR)
                    {
                        filteredData = filteredData.Union(getFilteredData(data, linkModel.FromQueryModel, false), new DataEqualityComparer(queryModel));
                    }
                }
            }
            if (!first)
            {
                filteredData = filteredData.GroupBy(
                        item => getGroupByObject(item, queryModel),
                        item => item,
                        (key, g) => getPartitionedItem(key, g, queryModel)).Where(partitionedItem => passesFilterModels(partitionedItem, queryModel)).
                        SelectMany(partitionedItem => partitionedItem.Items);
            }
            return filteredData;
        }

        private static bool passesFilterModels(PartitionedItem item, QueryModel queryModel)
        {
            foreach (var filterModel in queryModel.FilterModels)
            {
                foreach (var attributeModel in filterModel.ValueComparisons.Keys)
                {
                    if (filterModel.ValueComparisons[attributeModel].Compare(fromRaw(attributeModel, item.PartitionValues[attributeModel], false, 0)))
                    {
                        return true;
                    }
                }
            }
            return queryModel.FilterModels.Count == 0 ? true : false;
        }

        private static object getGroupByObject(Dictionary<AttributeModel, object> item, QueryModel queryModel)
        {
            var groupers = queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group);
            GroupingObject groupingObject = new GroupingObject(
                groupers.Count > 0,
                queryModel.AttributeOperationModels.Any(aom => aom.AggregateFunction != AggregateFunction.None));
            int count = 0;
            foreach (var attributeModel in item.Keys)
            {
                if (groupers.Count(avo => avo.IsGrouped && avo.AttributeModel == attributeModel) > 0)
                {
                    groupingObject.Add(count++, item[attributeModel]);
                }
                else if (groupers.Count(avo => avo.IsBinned && avo.AttributeModel == attributeModel) > 0)
                {
                    AttributeOperationModel bin = groupers.Where(avo => avo.IsBinned && avo.AttributeModel == attributeModel).First();
                    double d = double.Parse(item[attributeModel].ToString());
                    groupingObject.Add(count++, Math.Floor(d / bin.BinSize) * bin.BinSize);
                }
            }
            return groupingObject;
        }

        private static PartitionedItem getPartitionedItem(object key, IEnumerable<Dictionary<AttributeModel, object>> dicts, QueryModel queryModel)
        {
            PartitionedItem partitionedItem = new PartitionedItem();
            partitionedItem.Items = dicts;

            var attributeOperationModels = queryModel.AttributeOperationModels;
            foreach (var attributeOperationModel in attributeOperationModels)
            {
                bool binned = false;
                double binSize = 0;
                IEnumerable<object> values = dicts.Select(dict => dict[attributeOperationModel.AttributeModel]);

                object rawValue = null;

                if (attributeOperationModel.AggregateFunction == AggregateFunction.Max)
                {
                    rawValue = values.Max();
                }
                else if (attributeOperationModel.AggregateFunction == AggregateFunction.Min)
                {
                    rawValue = values.Min();
                }
                else if (attributeOperationModel.AggregateFunction == AggregateFunction.Avg)
                {
                    if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.FLOAT)
                    {
                        rawValue = values.Select(v => (double)v).Average();
                    }
                    else if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.INT)
                    {
                        rawValue = values.Select(v => (int)v).Average();
                    }
                }
                else if (attributeOperationModel.AggregateFunction == AggregateFunction.Sum)
                {
                    if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.FLOAT)
                    {
                        rawValue = values.Select(v => (double)v).Sum();
                    }
                    else if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.INT)
                    {
                        rawValue = values.Select(v => (int)v).Sum();
                    }
                }
                else if (attributeOperationModel.AggregateFunction == AggregateFunction.Count)
                {
                    rawValue = values.Count();
                }
                else if (attributeOperationModel.AggregateFunction == AggregateFunction.None)
                {
                    if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Any())
                    {
                        if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Any(aom => aom.AttributeModel == attributeOperationModel.AttributeModel))
                        {
                            AttributeOperationModel grouper = queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Where(aom => aom.AttributeModel == attributeOperationModel.AttributeModel).First();
                            if (grouper.IsGrouped)
                            {
                                rawValue = values.First();
                            }
                            else if (grouper.IsBinned)
                            {
                                double d = double.Parse(values.First().ToString());
                                rawValue = Math.Floor(d / grouper.BinSize) * grouper.BinSize;
                                binned = true;
                                binSize = grouper.BinSize;
                            }
                        }
                        else
                        {
                            rawValue = "...";
                        }
                    }
                    else
                    {
                        rawValue = values.First();
                    }
                }

                if (!partitionedItem.PartitionValues.ContainsKey(attributeOperationModel))
                {
                    partitionedItem.PartitionValues.Add(attributeOperationModel, rawValue);
                    partitionedItem.IsBinned.Add(attributeOperationModel, binned);
                    partitionedItem.BinSize.Add(attributeOperationModel, binSize);
                }
            }

            return partitionedItem;
        }

        private static QueryResultItemModel getQueryResultItemModel(object key, IEnumerable<Dictionary<AttributeModel, object>> dicts, QueryModel queryModel)
        {
            QueryResultItemModel item = new QueryResultItemModel();
            PartitionedItem partitionedItem = getPartitionedItem(key, dicts, queryModel);

            var attributeOperationModels = queryModel.AttributeOperationModels;
            foreach (var attributeOperationModel in attributeOperationModels)
            {
                QueryResultItemValueModel valueModel = fromRaw(
                    attributeOperationModel, 
                    partitionedItem.PartitionValues[attributeOperationModel], 
                    partitionedItem.IsBinned[attributeOperationModel],
                    partitionedItem.BinSize[attributeOperationModel]);
                if (!item.Values.ContainsKey(attributeOperationModel))
                {
                    item.Values.Add(attributeOperationModel, valueModel);
                }
            }
            return item;
        }

        private static QueryResultItemValueModel fromRaw(AttributeOperationModel attributeOperationModel, object value, bool binned, double binSize)
        {
            QueryResultItemValueModel valueModel = new QueryResultItemValueModel();

            double d = 0.0;
            valueModel.Value = value;
            if (double.TryParse(value.ToString(), out d))
            {
                valueModel.StringValue = valueModel.Value.ToString().Contains(".") ? d.ToString("N") : valueModel.Value.ToString();
                if (binned)
                {
                    valueModel.StringValue = d + " - " + (d + binSize);
                }
                else if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.BIT)
                {
                    if (d == 1.0)
                    {
                        valueModel.StringValue = "True";
                    }
                    else if (d == 0.0)
                    {
                        valueModel.StringValue = "False";
                    }
                }
            }
            else
            {
                valueModel.StringValue = valueModel.Value.ToString();
                if (valueModel.Value is DateTime)
                {
                    valueModel.StringValue = ((DateTime)valueModel.Value).ToShortDateString();
                }
            }

            if (attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.GEOGRAPHY)
            {

                string toSplit = valueModel.StringValue;
                if (toSplit.Contains("(") && toSplit.Contains(")"))
                {
                    toSplit = toSplit.Substring(toSplit.IndexOf("("));
                    toSplit = toSplit.Substring(1, toSplit.IndexOf(")") - 1);
                }
                valueModel.ShortStringValue = valueModel.StringValue.Replace("(" + toSplit + ")", "");
            }
            else
            {
                valueModel.ShortStringValue = valueModel.StringValue.TrimTo(300);
            }

            return valueModel;
        }
    }

    public class PartitionedItem
    {
        public PartitionedItem()
        {
            PartitionValues = new Dictionary<AttributeOperationModel, object>();
            BinSize = new Dictionary<AttributeOperationModel, double>();
            IsBinned = new Dictionary<AttributeOperationModel, bool>();
        }
        public IEnumerable<Dictionary<AttributeModel, object>> Items { get; set; }
        public Dictionary<AttributeOperationModel, object> PartitionValues { get; set; }
        public Dictionary<AttributeOperationModel, double> BinSize { get; set; }
        public Dictionary<AttributeOperationModel, bool> IsBinned { get; set; }
    }

    public class DataEqualityComparer : IEqualityComparer<Dictionary<AttributeModel, object>>
    {
        private QueryModel _queryModel = null;

        public DataEqualityComparer(QueryModel queryModel)
        {
            _queryModel = queryModel;
        }

        public bool Equals(Dictionary<AttributeModel, object> x, Dictionary<AttributeModel, object> y)
        {
            return x[(_queryModel.SchemaModel.OriginModels[0] as SimOriginModel).IdAttributeModel] ==
                   y[(_queryModel.SchemaModel.OriginModels[0] as SimOriginModel).IdAttributeModel];
        }

        public int GetHashCode(Dictionary<AttributeModel, object> x)
        {
            return x[(_queryModel.SchemaModel.OriginModels[0] as SimOriginModel).IdAttributeModel].GetHashCode();
        }
    }

    public class ItemComparer : IComparer<QueryResultItemModel>
    {
        private QueryModel _queryModel = null;

        public ItemComparer(QueryModel queryModel)
        {
            _queryModel = queryModel;
        }

        public int Compare(QueryResultItemModel x, QueryResultItemModel y)
        {
            var attributeOperationModels = _queryModel.AttributeOperationModels.Where(aom => aom.SortMode != SortMode.None);
            foreach (var aom in attributeOperationModels)
            {
                int factor = aom.SortMode == SortMode.Asc ? 1 : -1;

                if (x.Values[aom].Value is string &&
                   ((string)x.Values[aom].Value).CompareTo((string)y.Values[aom].Value) != 0)
                {
                    return (x.Values[aom].Value as string).CompareTo(y.Values[aom].Value as string) * factor;
                }
                else if (x.Values[aom].Value is double &&
                         ((double)x.Values[aom].Value).CompareTo((double) y.Values[aom].Value) != 0)
                {
                    return ((double)x.Values[aom].Value).CompareTo((double)y.Values[aom].Value) * factor;
                }
                else if (x.Values[aom].Value is int &&
                     ((int)x.Values[aom].Value).CompareTo((int)y.Values[aom].Value) != 0)
                {
                    return ((int)x.Values[aom].Value).CompareTo((int)y.Values[aom].Value) * factor;
                }
                else if (x.Values[aom].Value is DateTime &&
                         ((DateTime)x.Values[aom].Value).CompareTo((DateTime)y.Values[aom].Value) != 0)
                {
                    return ((DateTime)x.Values[aom].Value).CompareTo((DateTime)y.Values[aom].Value) * factor;
                }
            }
            return 0;
        }
    }

    public class GroupingObject
    {
        private Dictionary<int, object> _dictionary = new Dictionary<int, object>();
        private bool _isAnyGrouped = false;
        private bool _isAnyAggregated = false;

        public GroupingObject(bool isAnyGrouped, bool isAnyAggregated)
        {
            _isAnyGrouped = isAnyGrouped;
            _isAnyAggregated = isAnyAggregated;
        }

        public void Add(int index, object value)
        {
            _dictionary.Add(index, value);
        }

        public override bool Equals(object obj)
        {
            if (obj is GroupingObject)
            {
                var go = obj as GroupingObject;
                if (_isAnyGrouped)
                {
                    return go._dictionary.SequenceEqual(this._dictionary);
                }
                else
                {
                    if (_isAnyAggregated)
                    {
                        return true;
                    }
                    return false;
                }
                    
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (_isAnyGrouped)
            {
                int code = 0;
                foreach (var v in _dictionary.Values)
                    code ^= v.GetHashCode();
                return code;
            }
            else
            {
                if (_isAnyAggregated)
                {
                    return 0;
                }
                return _dictionary.GetHashCode();
            }
        }
    }
}
