using PanoramicData.model.view_new;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public SimItemsProvider(VisualizationViewModel visualizationViewModel)
        {
            _visualizationViewModel = visualizationViewModel;
        }

        public int FetchCount()
        {
            return 0;
        }

        public IList<VisualizationViewResultItemModel> FetchRange(int startIndex, int pageCount, out int overallCount)
        {
            overallCount = 0;
            return null;
        }
    }
}
