using System;
using System.Reactive.Linq;
using System.Windows;
using PanoramicDataModel;

namespace PanoramicData.model.view
{
    public class CustomFieldsGroupDescriptor : PanoramicDataGroupDescriptor
    {
        public override string GetLabel()
        {
            return "Custom Groups";
        }
    }

    public class NamedFilterModelColumnDescriptor : PanoramicDataColumnDescriptor
    {
        private static readonly CustomFieldsGroupDescriptor _panoramicDataGroupDescriptor = new CustomFieldsGroupDescriptor();
        private IDisposable _filterModelDisposable = null;

        public NamedFilterModelColumnDescriptor(TableModel tableModel, FilterModel filterModel)
        {
            _tableModel = tableModel;
            FilterModel = filterModel;
            _sortMode = SortMode.None;
            _aggregateFunction = AggregateFunction.None;
            _binSize = 1.0;
        }

        private FilterModel _filterModel = null;
        public FilterModel FilterModel
        {
            get
            {
                return _filterModel;
            }
            set
            {
                if (_filterModel != value)
                {
                    _filterModel = value;
                    OnPropertyChanged("FilterModel");

                    if (_filterModelDisposable != null)
                    {
                        _filterModelDisposable.Dispose();
                    }
                    _filterModelDisposable = Observable.FromEventPattern<FilterModelUpdatedEventArgs>((FilterModel)_filterModel, "FilterModelUpdated")
                        .Throttle(TimeSpan.FromMilliseconds(50))
                        .Subscribe((arg) =>
                        {
                            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                if (arg.EventArgs.Mode != UpdatedMode.UI)
                                {
                                    FirePanoramicDataColumnDescriptorUpdated(UpdatedMode.FilteredItemsChange);
                                }
                            }));
                        });
                }
            }
        }

        private TableModel _tableModel = null;
        public TableModel TableModel
        {
            get
            {
                return _tableModel;
            }
            set
            {
                if (_tableModel != value)
                {
                    _tableModel = value;
                    OnPropertyChanged("TableModel");
                }
            }
        }

        protected override string ExternalQualifier
        {
            get
            {
                return "custom.fields." + Name;
            }
        }

        public override string DataType
        {
            get
            {
                return DataTypeConstants.BIT;
            }
        }

        public override string VisualizationType
        {
            get
            {
                return VisualizationTypeConstants.ENUM;
            }
        }

        public override string Name
        {
            get
            {
                return TableModel.NamedFilterModels[_filterModel];
            }
        }

        public override double? MaxValue
        {
            get
            {
                return 1.0;
            }
        }

        public override double? MinValue
        {
            get
            {
                return 0.0;
            }
        }

        public override bool IsPrimaryKey
        {
            get
            {
                return false;
            }
            set{}
        }

        public override PanoramicDataGroupDescriptor PanoramicDataGroupDescriptor
        {
            get
            {
                return _panoramicDataGroupDescriptor;
            }
        }

        public override string GetSimpleLabel()
        {
            string mainLabel = this.Name;
            mainLabel = AddDetailToLabel(mainLabel);
            return mainLabel;
        }

        public override void GetLabels(out string mainLabel, out string subLabel, bool addDetails = true)
        {
            mainLabel = this.Name;

            if (addDetails)
            {
                if (this.IsGrouped)
                {
                    mainLabel = "[" + mainLabel + "]";
                }
                else if (this.IsTiled)
                {
                    mainLabel = "[" + mainLabel + "] / " + this.NumberOfTiles;
                }
                else if (this.IsBinned)
                {
                    mainLabel = "[" + mainLabel + "] / [" + this.BinSize + "]";
                }
                else
                {
                    mainLabel = AddDetailToLabel(mainLabel);
                }
            }
            subLabel = "Custom Field";
        }

        public override bool MatchSimple(PanoramicDataColumnDescriptor cd)
        {
            if (cd is NamedFilterModelColumnDescriptor)
            {
                var nc = cd as NamedFilterModelColumnDescriptor;
                return this.FilterModel == nc.FilterModel;
            }
            else
            {
                return false;
            }
        }
        public override bool MatchComplete(PanoramicDataColumnDescriptor cd)
        {
            if (cd is NamedFilterModelColumnDescriptor)
            {
                var nc = cd as NamedFilterModelColumnDescriptor;
                return
                    this.FilterModel == nc.FilterModel &&
                    this.AggregateFunction == nc.AggregateFunction &&
                    this.SortMode == nc.SortMode &&
                    this.IsGrouped == nc.IsGrouped &&
                    this.IsBinned == nc.IsBinned &&
                    this.BinSize == nc.BinSize &&
                    this.IsTiled == nc.IsTiled;
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is NamedFilterModelColumnDescriptor)
            {
                var nc = obj as NamedFilterModelColumnDescriptor;
                return this.MatchComplete(nc);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code ^= this.FilterModel.GetHashCode();
            return code;
        }

        public override object Clone()
        {
            PanoramicDataColumnDescriptor cd = new NamedFilterModelColumnDescriptor(_tableModel, _filterModel);
            cd.IsGrouped = this.IsGrouped;
            cd.IsBinned = this.IsBinned;
            cd.IsTiled = this.IsTiled;
            cd.BinSize = this.BinSize;
            cd.BinLowerBound = this.BinLowerBound;
            cd.BinUpperBound = this.BinUpperBound;
            cd.NumberOfTiles = this.NumberOfTiles;
            cd.SortMode = this.SortMode;
            cd.AggregateFunction = this.AggregateFunction;
            cd.Order = this.Order;

            return cd;
        }

        public override PanoramicDataColumnDescriptor SimpleClone()
        {
            PanoramicDataColumnDescriptor cd = new NamedFilterModelColumnDescriptor(_tableModel, _filterModel);
            return cd;
        }
    }
}