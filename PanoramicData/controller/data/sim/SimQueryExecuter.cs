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
        public override void ExecuteQuery(VisualizationViewModel visualizationViewModel)
        {
            IItemsProvider<VisualizationViewResultItemModel> itemsProvider = new SimItemsProvider(visualizationViewModel);
            AsyncVirtualizingCollection<VisualizationViewResultItemModel> dataValues = new AsyncVirtualizingCollection<VisualizationViewResultItemModel>(itemsProvider, 100, 1000);
            visualizationViewModel.VisualizationViewResultModel.VisualizationViewResultItemModels = dataValues;

        }
    }

    public class SimItemsProvider : IItemsProvider<VisualizationViewResultItemModel>
    {
        private VisualizationViewModel _visualizationViewModel = null;
        private int _fetchCount = -1;

        public SimItemsProvider(VisualizationViewModel visualizationViewModel)
        {
            _visualizationViewModel = visualizationViewModel;
        }

        public int FetchCount()
        {
            _fetchCount = (_visualizationViewModel.SchemaModel.OriginModels[0] as SimOriginModel).Data.Count;
            return _fetchCount;
        }

        public IList<VisualizationViewResultItemModel> FetchRange(int startIndex, int pageCount, out int overallCount)
        {
            Thread.Sleep(1000);
            var data = (_visualizationViewModel.SchemaModel.OriginModels[0] as SimOriginModel).Data;
            var xs = _visualizationViewModel.GetFunctionAttributeViewModel(AttributeFunction.X);

            List<VisualizationViewResultItemModel> returnList = data.Skip(startIndex).Take(pageCount).Select((dict) =>
            {
                VisualizationViewResultItemModel item = new VisualizationViewResultItemModel();
                foreach (var attributeModel in dict.Keys)
                {
                    if (xs.Count(avm => avm.AttributeModel == attributeModel) > 0)
                    {
                        var attributeValueViewModel = xs.First(avm => avm.AttributeModel == attributeModel);
                        VisualizationViewResultItemValueModel valueModel = fromRaw(attributeValueViewModel, dict[attributeModel]);
                        item.Values.Add(attributeValueViewModel, valueModel);
                    }
                }

                return item;
            }).ToList();
            overallCount = _fetchCount;
            return returnList;
        }

        private VisualizationViewResultItemValueModel fromRaw(AttributeViewModel attributeValueViewModel, object value)
        {
            VisualizationViewResultItemValueModel valueModel = new VisualizationViewResultItemValueModel();

            double d = 0.0;
            valueModel.Value = value;
            if (double.TryParse(value.ToString(), out d))
            {
                valueModel.StringValue = valueModel.Value.ToString().Contains(".") ? d.ToString("N") : valueModel.Value.ToString();
                if (attributeValueViewModel.AttributeOperationModel.AggregateFunction == AggregateFunction.Bin)
                {
                    valueModel.StringValue = d + " - " + (d + attributeValueViewModel.AttributeOperationModel.BinSize);
                }
                else if (attributeValueViewModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.BIT)
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

            if (attributeValueViewModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.GEOGRAPHY)
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
