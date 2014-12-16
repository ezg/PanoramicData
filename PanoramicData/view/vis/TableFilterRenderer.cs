using PanoramicData.controller.data;
using PanoramicData.view.filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.view.table
{
    public class TableFilterRenderer : FilterRenderer
    {
        private SimpleDataGrid _dataGrid = new SimpleDataGrid();
        private IDisposable _filterModelDisposable = null;

        public TableFilterRenderer()
        {
            _dataGrid.CanDrag = false;
            _dataGrid.CanReorder = true;
            _dataGrid.CanResize = true;
            _dataGrid.CanExplore = true;
            _dataGrid.RowsSelected += _dataGrid_RowsSelected;
            this.Content = _dataGrid;

        }

        public override byte[] CreateImage()
        {
            return null;
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
                        FilteredItem fi = new FilteredItem(row);
                        if (FilterModel.FilteredItems.Contains(fi))
                        {
                            FilterModel.RemoveFilteredItem(fi, this);
                        }
                    }
                }
                else
                {
                    foreach (var row in rows)
                    {
                        row.IsHighligthed = true;
                        FilteredItem fi = new FilteredItem(row);
                        if (!FilterModel.FilteredItems.Contains(fi))
                        {
                            FilterModel.AddFilteredItem(fi, this);
                        }
                    }
                }
            }
        }

        protected override void Init(bool resetViewport)
        {
            if (FilterModel != null)
            {
                QueryItemsProvider queryItemsProvider = new QueryItemsProvider(new FilterQueryGenerator(FilterModel));
                AsyncVirtualizingCollection<PanoramicDataRow> dataValues = new AsyncVirtualizingCollection<PanoramicDataRow>(queryItemsProvider, 100 /*page size*/, 1000 /*timeout*/);
                _dataGrid.PopulateData(dataValues, null, FilterModel, false);
            }
        }
    }
}
