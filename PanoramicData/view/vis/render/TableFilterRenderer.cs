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
            _dataGrid.RowsSelected += _dataGrid_RowsSelected;
            this.Content = _dataGrid;

        }

        void _dataGrid_RowsSelected(object sender, List<PanoramicDataRow> rows)
        {
            if (rows != null)
            {
                if (rows.Any(r => r.IsHighligthed))
                {
                    foreach (var row in rows)
                    {
                        row.IsHighligthed = false;
                        FilterItem fi = new FilterItem(row);
                        if (VisualizationViewModel.QueryModel.FilterItems.Contains(fi))
                        {
                            VisualizationViewModel.QueryModel.RemoveFilterItem(fi, this);
                        }
                    }
                }
                else
                {
                    foreach (var row in rows)
                    {
                        row.IsHighligthed = true;
                        FilterItem fi = new FilterItem(row);
                        if (!VisualizationViewModel.QueryModel.FilterItems.Contains(fi))
                        {
                            VisualizationViewModel.QueryModel.AddFilterItem(fi, this);
                        }
                    }
                }
            }
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
