using PanoramicDataModel;

namespace PanoramicData.model.view
{
    public class CalculatedGroupDescriptor : PanoramicDataGroupDescriptor
    {
        public override string GetLabel()
        {
            return "Calculated Fields";
        }
    }

    public class CalculatedColumnDescriptor : PanoramicDataColumnDescriptor
    {
        private static readonly CalculatedGroupDescriptor _calculatedGroupDescriptor = new CalculatedGroupDescriptor();

        public CalculatedColumnDescriptor(TableModel tableModel, CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo)
        {
            _tableModel = tableModel;
            CalculatedColumnDescriptorInfo = calculatedColumnDescriptorInfo;
            _sortMode = SortMode.None;
            _aggregateFunction = AggregateFunction.None;
        }

        private CalculatedColumnDescriptorInfo _calculatedColumnDescriptorInfo = null;
        public CalculatedColumnDescriptorInfo CalculatedColumnDescriptorInfo
        {
            get
            {
                return _calculatedColumnDescriptorInfo;
            }
            set
            {
                if (_calculatedColumnDescriptorInfo != null)
                {
                    _calculatedColumnDescriptorInfo.PropertyChanged -= _calculatedColumnDescriptorInfo_PropertyChanged;
                }
                if (_calculatedColumnDescriptorInfo != value)
                {
                    _calculatedColumnDescriptorInfo = value;
                    OnPropertyChanged("CalculatedColumnDescriptorInfo");
                    _calculatedColumnDescriptorInfo.PropertyChanged += _calculatedColumnDescriptorInfo_PropertyChanged;
                }
            }
        }

        void _calculatedColumnDescriptorInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FirePanoramicDataColumnDescriptorUpdated(UpdatedMode.FilteredItemsChange);
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
                return "calculated.fields." + Name;
            }
        }

        public override string DataType
        {
            get
            {
                return DataTypeConstants.FLOAT;
            }
        }

        public override string VisualizationType
        {
            get
            {
                return VisualizationTypeConstants.NUMERIC;
            }
        }

        public override string Name
        {
            get
            {
                return _calculatedColumnDescriptorInfo.Name;
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
                return _calculatedGroupDescriptor;
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
            subLabel = "Calculated Field";
        }


        public override bool MatchSimple(PanoramicDataColumnDescriptor cd)
        {
            if (cd is CalculatedColumnDescriptor)
            {
                var nc = cd as CalculatedColumnDescriptor;
                return this.CalculatedColumnDescriptorInfo == nc.CalculatedColumnDescriptorInfo;
            }
            else
            {
                return false;
            }
        }
        public override bool MatchComplete(PanoramicDataColumnDescriptor cd)
        {
            if (cd is CalculatedColumnDescriptor)
            {
                var nc = cd as CalculatedColumnDescriptor;
                return
                    this.CalculatedColumnDescriptorInfo == nc.CalculatedColumnDescriptorInfo &&
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
            if (obj is CalculatedColumnDescriptor)
            {
                var nc = obj as CalculatedColumnDescriptor;
                return this.MatchComplete(nc);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code ^= this.CalculatedColumnDescriptorInfo.GetHashCode();
            return code;
        }

        public override object Clone()
        {
            PanoramicDataColumnDescriptor cd = new CalculatedColumnDescriptor(_tableModel, _calculatedColumnDescriptorInfo);
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
            PanoramicDataColumnDescriptor cd = new CalculatedColumnDescriptor(_tableModel, _calculatedColumnDescriptorInfo);
            return cd;
        }
    }
}