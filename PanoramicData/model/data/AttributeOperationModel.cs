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
    }

    public enum AggregateFunction { None, Sum, Count, Min, Max, Avg, Concat, Vis, Bin };

    public enum SortMode { Asc, Desc, None }
}
