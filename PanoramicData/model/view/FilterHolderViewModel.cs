using System.Collections.Generic;
using PanoramicDataModel;
using starPadSDK.Geom;
using System.Windows;
using starPadSDK.Inq;
using PanoramicData.view.filter;

namespace PanoramicData.model.view
{
    public class FilterHolderViewModel : FilterModel
    {
        public FilterHolderViewModel()
        {
            Dimension = new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT);
        }

        private Point _center = new Point();
        public Point Center
        {
            get
            {
                return _center;
            }
            set
            {
                if (_center != value)
                {
                    _center = value;
                    fireFilterUpdated(UpdatedMode.UI);
                }
            }
        }

        private Vec _dimension = new Vec();
        public Vec Dimension
        {
            get
            {
                return _dimension;
            }
            set
            {
                if (_dimension != value)
                {
                    _dimension = value;
                    fireFilterUpdated(UpdatedMode.UI);
                }
            }
        }

        private bool _noChrome = false;
        public bool NoChrome
        {
            get
            {
                return _noChrome;
            }
            set
            {
                this.SetProperty(ref _noChrome, value);
            }
        }

        public Stroq Stroq { get; set; }

        public static FilterHolderViewModel CreateDefault(PanoramicDataColumnDescriptor columnDescriptor1, 
            PanoramicDataColumnDescriptor columnDescriptor2, TableModel tableModel, FilterRendererType type)
        {
            FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
            filterHolderViewModel.TableModel = tableModel;

            filterHolderViewModel.FilterRendererType = type;

            PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor1.Clone();
            PanoramicDataColumnDescriptor y = (PanoramicDataColumnDescriptor)columnDescriptor2.Clone();
            //PanoramicDataColumnDescriptor s = (PanoramicDataColumnDescriptor)theModel.ColumnDescriptors[playerPath][0].Clone();

            //filterHolderViewModel.AddColumnDescriptor(s);
            filterHolderViewModel.AddColumnDescriptor(y);
            if (!filterHolderViewModel.ColumnDescriptors.Contains(y))
            {
                filterHolderViewModel.AddColumnDescriptor(x);
            }

            filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);
            filterHolderViewModel.AddOptionColumnDescriptor(Option.Y, y);
            //filterHolderViewModel.AddSeriesColumnDescriptor(s);

            //filterHolderViewModel.Label = x.Name + ", " + y.Name;

            return filterHolderViewModel;
        }

        public static FilterHolderViewModel CreateTable(TableModel tableModel)
        {
            FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
            filterHolderViewModel.TableModel = tableModel;

            filterHolderViewModel.FilterRendererType = FilterRendererType.Table;

            foreach (List<DatabaseColumnDescriptor> cds in tableModel.ColumnDescriptors.Values)
            {
                foreach (var cd in cds)
                {
                    filterHolderViewModel.AddOptionColumnDescriptor(Option.X, (DatabaseColumnDescriptor)cd.Clone());
                }
            }
            return filterHolderViewModel;
        }

        public static FilterHolderViewModel CreateDefaultPie(PanoramicDataColumnDescriptor columnDescriptor,
            TableModel tableModel)
        {
            FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
            filterHolderViewModel.TableModel = tableModel;

            filterHolderViewModel.FilterRendererType = FilterRendererType.Pie;

            PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
            PanoramicDataColumnDescriptor g = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
            g.IsGrouped = true;
            PanoramicDataColumnDescriptor y = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
            y.AggregateFunction = AggregateFunction.Count;

            filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);
            filterHolderViewModel.AddOptionColumnDescriptor(Option.ColorBy, g);
            filterHolderViewModel.AddOptionColumnDescriptor(Option.Y, y);

            return filterHolderViewModel;
        }

        public static FilterHolderViewModel CreateCopy(FilterModel filterModel, bool asTable = false, bool copyLinks = true)
        {
            FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
            filterHolderViewModel.TableModel = filterModel.TableModel;
            filterHolderViewModel.FilterRendererType = filterModel.FilterRendererType;

            foreach (var cd in filterModel.ColumnDescriptors)
            {
                List<Option> opts = filterModel.GetOptionsForColumnDescriptors(cd);
                foreach (var option in opts)
                {
                    PanoramicDataColumnDescriptor clone = (PanoramicDataColumnDescriptor) cd.Clone();
                    clone.ScaleFunction = cd.ScaleFunction;
                    if (cd.FilterStroqs != null)
                    {
                        clone.FilterStroqs = cd.FilterStroqs.Clone();
                    }
                    if (asTable && option == Option.Y)
                    {
                        filterHolderViewModel.AddOptionColumnDescriptor(Option.X, clone);
                    }
                    else
                    {
                        if (asTable && option == Option.ColorBy)
                        {
                            filterHolderViewModel.AddOptionColumnDescriptor(Option.X, clone);
                        }
                        filterHolderViewModel.AddOptionColumnDescriptor(option, clone);
                    }
                }
            }

            if (copyLinks)
            {
                foreach (var model in filterModel.GetIncomingFilterModels(FilteringType.Brush))
                {
                    filterHolderViewModel.AddIncomingFilter(model, FilteringType.Brush, true);
                }

                foreach (var model in filterModel.GetIncomingFilterModels(FilteringType.Filter))
                {
                    filterHolderViewModel.AddIncomingFilter(model, FilteringType.Filter, true);
                }

                foreach (var model in filterModel.GetOutgoingFilterModels(FilteringType.Brush))
                {
                    model.AddIncomingFilter(filterHolderViewModel, FilteringType.Brush, true);
                }

                foreach (var model in filterModel.GetOutgoingFilterModels(FilteringType.Filter))
                {
                    model.AddIncomingFilter(filterHolderViewModel, FilteringType.Filter, true);
                }

                foreach (var fi in filterModel.FilteredItems)
                {
                    //filterHolderViewModel.AddFilteredItem(fi, filterHolderViewModel);
                }

                foreach (var fi in filterModel.EmbeddedFilteredItems)
                {
                    filterHolderViewModel.AddEmbeddedFilteredItem(fi);
                }
            }

            return filterHolderViewModel;
        }

        public static FilterHolderViewModel CreateDefault(PanoramicDataColumnDescriptor columnDescriptor, TableModel tableModel)
        {
            FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
            filterHolderViewModel.TableModel = tableModel;
            //filterHolderViewModel.Label = columnDescriptor.Name;

            if (columnDescriptor.VisualizationType == VisualizationTypeConstants.ENUM)
            {
                filterHolderViewModel.FilterRendererType = FilterRendererType.Pie;

                PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                PanoramicDataColumnDescriptor g = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                g.IsGrouped = true;
                PanoramicDataColumnDescriptor y = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                y.AggregateFunction = AggregateFunction.Count;

                filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.ColorBy, g);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.Y, y);
            }
            else if (columnDescriptor.VisualizationType == VisualizationTypeConstants.NUMERIC)
            {
                filterHolderViewModel.FilterRendererType = FilterRendererType.Histogram;

                PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                x.AggregateFunction = AggregateFunction.Bin;

                PanoramicDataColumnDescriptor bin = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                bin.IsBinned = true;

                PanoramicDataColumnDescriptor y = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                y.AggregateFunction = AggregateFunction.Count;

                filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.GroupBy, bin);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.Y, y);
            }
            else if (columnDescriptor.VisualizationType == VisualizationTypeConstants.GEOGRAPHY)
            {
                filterHolderViewModel.FilterRendererType = FilterRendererType.Map;

                PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                x.AggregateFunction = AggregateFunction.Count;

                PanoramicDataColumnDescriptor y = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                y.IsGrouped = true;

                filterHolderViewModel.AddOptionColumnDescriptor(Option.Location, y);
                filterHolderViewModel.AddOptionColumnDescriptor(Option.Label, x);
                //filterHolderViewModel.AddOptionColumnDescriptor(Option.Y, y);
                //filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);
            }
            else 
            {
                filterHolderViewModel.FilterRendererType = FilterRendererType.Table;

                PanoramicDataColumnDescriptor x = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                filterHolderViewModel.AddOptionColumnDescriptor(Option.X, x);
                PanoramicDataColumnDescriptor g = (PanoramicDataColumnDescriptor)columnDescriptor.Clone();
                g.IsGrouped = true;
                //filterHolderViewModel.AddOptionColumnDescriptor(Option.GroupBy, g);
            }

            return filterHolderViewModel;
        }
    }
}
