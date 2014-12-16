using PanoramicData.model.view;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewModelFactory
    {
        public static VisualizationViewModel CreateDefault(AttributeViewModel attributeViewModel)
        {
            VisualizationViewModel visualizationViewModel = new VisualizationViewModel();

            if (attributeViewModel.AttributeModel.AttributeVisualizationType == AttributeVisualizationTypeConstants.ENUM)
            {
                visualizationViewModel.VisualizationType = VisualizationType.Pie;
                /*
                PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                PanoramicDataColumnDescriptor g = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                g.IsGrouped = true;
                PanoramicDataColumnDescriptor y = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                y.AggregateFunction = AggregateFunction.Count;

                filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.ColorBy, g);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.Y, y);*/
            }
            else if (attributeViewModel.AttributeModel.AttributeVisualizationType == AttributeVisualizationTypeConstants.NUMERIC)
            {
                /*visualizationViewModel.VisualizationType = VisualizationType.Histogram;

                PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                x.AggregateFunction = AggregateFunction.Bin;

                PanoramicDataColumnDescriptor bin = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                bin.IsBinned = true;

                PanoramicDataColumnDescriptor y = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                y.AggregateFunction = AggregateFunction.Count;

                filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.GroupBy, bin);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.Y, y);*/
            }
            else if (attributeViewModel.AttributeModel.AttributeVisualizationType == AttributeVisualizationTypeConstants.GEOGRAPHY)
            {
                /*visualizationViewModel.VisualizationType = VisualizationType.Map;

                PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                x.AggregateFunction = AggregateFunction.Count;

                PanoramicDataColumnDescriptor y = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                y.IsGrouped = true;

                filterHolderViewModel.AddOptionColumnDescriptor(Option.Location, y);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.Label, x);
                //filterHolderViewModel.AddOptionColumnDescriptor(Option.Y, y);
                //filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);*/
            }
            else
            {
                visualizationViewModel.VisualizationType = VisualizationType.Table;
                visualizationViewModel.AddFunctionAttributeViewModel(AttributeFunction.X, new AttributeViewModel(attributeViewModel.AttributeModel, new data.AttributeOperationModel()));
            }

            return visualizationViewModel;
        }
    }
}
