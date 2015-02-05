using PanoramicData.controller.data;
using PanoramicData.model.data;
using PanoramicData.model.view_new;
using PanoramicData.view.table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.view.vis.render
{
    public class TableRenderer : FilterRenderer
    {
        private SimpleDataGrid _dataGrid = new SimpleDataGrid();
        private IDisposable _filterModelDisposable = null;

        public TableRenderer()
        {
            _dataGrid.CanDrag = false;
            _dataGrid.CanReorder = true;
            _dataGrid.CanResize = true;
            _dataGrid.CanExplore = true;
            this.Content = _dataGrid;
        }


        protected override void UpdateResults()
        {
            base.UpdateResults();

            /*if (FilterModel != null)
            {
                QueryItemsProvider queryItemsProvider = new QueryItemsProvider(new FilterQueryGenerator(FilterModel));
                AsyncVirtualizingCollection<PanoramicDataRow> dataValues = new AsyncVirtualizingCollection<PanoramicDataRow>(queryItemsProvider, 100 , 1000 );
                _dataGrid.PopulateData(dataValues, null, FilterModel, false);
            }*/
        }    
    }
}
