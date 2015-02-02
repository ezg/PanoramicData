using PanoramicData.model.data;
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
            _fetchCount = computeQueryResult().Count;
            return _fetchCount;
        }

        public IList<QueryResultItemModel> FetchRange(int startIndex, int pageCount, out int overallCount)
        {
            Console.WriteLine("page : " + startIndex + " " + pageCount);
            Thread.Sleep(10);
            
            IList<QueryResultItemModel> returnList = computeQueryResult().Skip(startIndex).Take(pageCount).ToList();
            overallCount = _fetchCount;
            return returnList;
        }

        private List<QueryResultItemModel> computeQueryResult()
        {
            if (_queryModel.GetAllAttributeOperationModel().Any())
            {
                var data = (_queryModel.SchemaModel.OriginModels[0] as SimOriginModel).Data;

                var results = data.
                    GroupBy(
                        item => getGroupByObject(item),
                        item => item,
                        (key, g) => getQueryResultItemModel(key, g)).
                        OrderBy(item => item, new ItemComparer(_queryModel));

                return results.ToList();
            }
            else
            {
                return new List<QueryResultItemModel>();
            }
        }
        
        private object getGroupByObject(Dictionary<AttributeModel, object> item)
        {
            var groupers = _queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group);
            GroupingObject groupingObject = new GroupingObject(
                groupers.Count > 0,
                _queryModel.GetAllAttributeOperationModel().Any(aom => aom.AggregateFunction != AggregateFunction.None));
            int count = 0;
            foreach (var attributeModel in item.Keys)
            {
                if (groupers.Count(avo => avo.AttributeModel == attributeModel) > 0)
                {
                    groupingObject.Add(count++, item[attributeModel]);
                }
            }
            return groupingObject;
        }

        private QueryResultItemModel getQueryResultItemModel(object key, IEnumerable<Dictionary<AttributeModel, object>> dicts)
        { 
            QueryResultItemModel item = new QueryResultItemModel();

            var attributeOperationModels = _queryModel.GetAllAttributeOperationModel();
            foreach (var attributeOperationModel in attributeOperationModels)
            {
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
                else if (attributeOperationModel.AggregateFunction == AggregateFunction.Bin)
                {
                    rawValue = "ddd";
                }
                else if (attributeOperationModel.AggregateFunction == AggregateFunction.None)
                {
                    if (_queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Any())
                    {
                        if (_queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Any(aom => aom.AttributeModel == attributeOperationModel.AttributeModel))
                        {
                            rawValue = values.First();
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

                QueryResultItemValueModel valueModel = fromRaw(attributeOperationModel, rawValue);
                if (!item.Values.ContainsKey(attributeOperationModel))
                {
                    item.Values.Add(attributeOperationModel, valueModel);
                }
            }


            return item;
        }

        private QueryResultItemValueModel fromRaw(AttributeOperationModel attributeOperationModel, object value)
        {
            QueryResultItemValueModel valueModel = new QueryResultItemValueModel();

            double d = 0.0;
            valueModel.Value = value;
            if (double.TryParse(value.ToString(), out d))
            {
                valueModel.StringValue = valueModel.Value.ToString().Contains(".") ? d.ToString("N") : valueModel.Value.ToString();
                if (attributeOperationModel.AggregateFunction == AggregateFunction.Bin)
                {
                    valueModel.StringValue = d + " - " + (d + attributeOperationModel.BinSize);
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

    public class ItemComparer : IComparer<QueryResultItemModel>
    {
        private QueryModel _queryModel = null;

        public ItemComparer(QueryModel queryModel)
        {
            _queryModel = queryModel;
        }

        public int Compare(QueryResultItemModel x, QueryResultItemModel y)
        {
            var attributeOperationModels = _queryModel.GetAllAttributeOperationModel().Where(aom => aom.SortMode != SortMode.None);
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
                    return ((double)x.Values[aom].Value).CompareTo((double)y.Values[aom].Value) * factor;
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
