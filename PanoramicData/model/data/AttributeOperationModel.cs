using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data
{
    public class AttributeOperationModel : BindableBase
    {
        public AttributeOperationModel(AttributeModel attributeModel)
        {
            _attributeModel = attributeModel;
        }

        private AttributeModel _attributeModel = null;
        public AttributeModel AttributeModel
        {
            get
            {
                return _attributeModel;
            }
            set
            {
                this.SetProperty(ref _attributeModel, value);
            }
        }

        private QueryModel _queryModel = null;
        public QueryModel QueryModel
        {
            get
            {
                return _queryModel;
            }
            set
            {
                _queryModel = value;
                this.SetProperty(ref _queryModel, value);
            }
        }

        private AggregateFunction _aggregateFunction = AggregateFunction.None;
        public AggregateFunction AggregateFunction
        {
            get
            {
                return _aggregateFunction;
            }
            set
            {
                this.SetProperty(ref _aggregateFunction, value);
            }
        }

        private double _binSize = 1.0;
        public double BinSize
        {
            get
            {
                return _binSize;
            }
            set
            {
                this.SetProperty(ref _binSize, value);
            }
        }

        private double _minBinSize = 1.0;
        public double MinBinSize
        {
            get
            {
                return _minBinSize;
            }
            set
            {
                this.SetProperty(ref _minBinSize, value);
            }
        }

        private double _maxBinSize = 100.0;
        public double MaxBinSize
        {
            get
            {
                return _maxBinSize;
            }
            set
            {
                this.SetProperty(ref _maxBinSize, value);
            }
        }

        private bool _isBinned = false;
        public bool IsBinned
        {
            get
            {
                return _isBinned;
            }
            set
            {
                this.SetProperty(ref _isBinned, value);
            }
        }

        private bool _isGrouped = false;
        public bool IsGrouped
        {
            get
            {
                return _isGrouped;
            }
            set
            {
                this.SetProperty(ref _isGrouped, value);
            }
        }

        private SortMode _sortMode = SortMode.None;
        public SortMode SortMode
        {
            get
            {
                return _sortMode;
            }
            set
            {
                this.SetProperty(ref _sortMode, value);
            }
        }

        private ScaleFunction _scaleFunction = ScaleFunction.None;
        public ScaleFunction ScaleFunction
        {
            get
            {
                return _scaleFunction;
            }
            set
            {
                this.SetProperty(ref _scaleFunction, value);
            }
        }
    }

    public enum AggregateFunction { None, Sum, Count, Min, Max, Avg, Concat, Vis, Bin };

    public enum SortMode { Asc, Desc, None }

    public enum ScaleFunction { None, Log, Normalize, RunningTotal, RunningTotalNormalized };
}
