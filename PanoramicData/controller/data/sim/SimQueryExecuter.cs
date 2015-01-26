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
            _fetchCount = (_queryModel.SchemaModel.OriginModels[0] as SimOriginModel).Data.Count;
            return _fetchCount;
        }

        public IList<QueryResultItemModel> FetchRange(int startIndex, int pageCount, out int overallCount)
        {
            Console.WriteLine("page : " + startIndex + " " + pageCount);
            Thread.Sleep(100);
            var data = (_queryModel.SchemaModel.OriginModels[0] as SimOriginModel).Data;
            var xs = _queryModel.GetFunctionAttributeOperationModel(AttributeFunction.X);

            List<QueryResultItemModel> returnList = data.Skip(startIndex).Take(pageCount).Select((dict) =>
            {
                QueryResultItemModel item = new QueryResultItemModel();
                foreach (var attributeModel in dict.Keys)
                {
                    if (xs.Count(avm => avm.AttributeModel == attributeModel) > 0)
                    {
                        var attributeValueViewModel = xs.First(avm => avm.AttributeModel == attributeModel);
                        QueryResultItemValueModel valueModel = fromRaw(attributeValueViewModel, dict[attributeModel]);
                        item.Values.Add(attributeValueViewModel, valueModel);
                    }
                }

                return item;
            }).ToList();
            overallCount = _fetchCount;
            return returnList;
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
}
