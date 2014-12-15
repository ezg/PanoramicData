using Microsoft.Practices.Prism.Mvvm;
using starPadSDK.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewModel : BindableBase
    {
        public delegate void VisualizationViewModelUpdatedHandler(object sender, VisualizationViewModelUpdatedEventArgs e);
        public event VisualizationViewModelUpdatedHandler VisualizationViewModelUpdated;

        private Dictionary<AttributeFunction, List<AttributeViewModel>> _attributeViewModels = new Dictionary<AttributeFunction, List<AttributeViewModel>>();

        private Vec _size = new Vec(180, 100);
        public Vec Size
        {
            get
            {
                return _size;
            }
            set
            {
                this.SetProperty(ref _size, value);
            }
        }

        private Pt _postion;
        public Pt Position
        {
            get
            {
                return _postion;
            }
            set
            {
                this.SetProperty(ref _postion, value);
            }
        }

        private VisualizationType _visualizationType;
        public VisualizationType VisualizationType
        {
            get
            {
                return _visualizationType;
            }
            set
            {
                this.SetProperty(ref _visualizationType, value);
            }
        }

        private bool _isTemporary;
        public bool IsTemporary
        {
            get
            {
                return _isTemporary;
            }
            set
            {
                this.SetProperty(ref _isTemporary, value);
            }
        }

        protected void fireTableModelUpdated(VisualizationViewModelUpdatedEventType type)
        {
            if (VisualizationViewModelUpdated != null)
            {
                VisualizationViewModelUpdated(this, new VisualizationViewModelUpdatedEventArgs(type));
            }
        }
    }

    public class VisualizationViewModelUpdatedEventArgs : EventArgs
    {
        public VisualizationViewModelUpdatedEventType VisualizationViewModelUpdatedEventType { get; set; }

        public VisualizationViewModelUpdatedEventArgs(VisualizationViewModelUpdatedEventType type)
            : base()
        {
            VisualizationViewModelUpdatedEventType = type;
        }
    }

    public enum VisualizationViewModelUpdatedEventType { View, Links }

    public enum VisualizationType { Table, Histogram, Map, Plot, Pie, Slider, Pivot, Line, OneD, Frozen, Test }
}
