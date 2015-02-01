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
            var data = (_queryModel.SchemaModel.OriginModels[0] as SimOriginModel).Data;

            var results = data.OrderBy(item => item, new ItemComparer(_queryModel)).
                GroupBy(
                    item => getGroupByObject(item), 
                    item => item,
                    (key, g) => getQueryResultItemModel(key, g));

            return results.ToList();
            
        }
        
        private object getGroupByObject(Dictionary<AttributeModel, object> item)
        {
            var groupers = _queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group);
            GroupingObject groupingObject = new GroupingObject(groupers.Count == 0);
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

            // groupers first
            var attributeOperationModels = _queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group);
            foreach (var attributeOperationModel in attributeOperationModels)
            {
                IEnumerable<object> values = dicts.Select(dict => dict[attributeOperationModel.AttributeModel]);
                QueryResultItemValueModel valueModel = fromRaw(attributeOperationModel, values.First());
                if (!item.Values.ContainsKey(attributeOperationModel))
                {
                    item.Values.Add(attributeOperationModel, valueModel);
                }
            }

            // x values
            attributeOperationModels = _queryModel.GetFunctionAttributeOperationModel(AttributeFunction.X);
            foreach (var attributeOperationModel in attributeOperationModels)
            {
                IEnumerable<object> values = dicts.Select(dict => dict[attributeOperationModel.AttributeModel]);
                QueryResultItemValueModel valueModel = fromRaw(attributeOperationModel, values.First());
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

    public class ItemComparer : IComparer<Dictionary<AttributeModel, object>>
    {
        private QueryModel _queryModel = null;

        public ItemComparer(QueryModel queryModel)
        {
            _queryModel = queryModel;
        }

        public int Compare(Dictionary<AttributeModel, object> x, Dictionary<AttributeModel, object> y)
        {
            return string.Compare(y.LastName, x.LastName);
        }
    }

    public class GroupingObject
    {
        private Dictionary<int, object> _dictionary = new Dictionary<int, object>();
        private bool _isNotGrouped = false;

        public GroupingObject(bool isNotGrouped)
        {
            _isNotGrouped = isNotGrouped;
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
                if (_isNotGrouped)
                    return false;
                else
                    return go._dictionary.SequenceEqual(this._dictionary);
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (_isNotGrouped)
            {
                return _dictionary.GetHashCode();
            }
            else
            {
                int code = 0;
                foreach (var v in _dictionary.Values)
                    code ^= v.GetHashCode();
                return code;
            }
        }
    }
}
