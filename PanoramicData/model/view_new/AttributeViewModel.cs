using Microsoft.Practices.Prism.Mvvm;
using PanoramicData.model.data;
using starPadSDK.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class AttributeViewModel : BindableBase
    {
        public static event EventHandler<AttributeViewModelEventArgs> AttributeViewModelMoved;
        public static event EventHandler<AttributeViewModelEventArgs> AttributeViewModelDropped;

        public AttributeViewModel() { }

        public AttributeViewModel(VisualizationViewModel visualizationViewModel, AttributeOperationModel attributeOperationModel)
        {
            AttributeOperationModel = attributeOperationModel;
        }

        public void FireMoved(Rct bounds, AttributeOperationModel attributeOperationModel, AttributeViewModelEventArgType type)
        {
            if (AttributeViewModelMoved != null)
            {
                AttributeViewModelMoved(this, new AttributeViewModelEventArgs(attributeOperationModel, bounds, type));
            }
        }

        public void FireDropped(Rct bounds, AttributeViewModelEventArgType type, AttributeOperationModel attributeOperationModel)
        {
            if (AttributeViewModelDropped != null)
            {
                AttributeViewModelDropped(this, new AttributeViewModelEventArgs(attributeOperationModel, bounds, type));
            }
        }

        private bool _isShadow = false;
        public bool IsShadow
        {
            get
            {
                return _isShadow;
            }
            set
            {
                this.SetProperty(ref _isShadow, value);
            }
        }

        private bool _isDraggable = true;
        public bool IsDraggable
        {
            get
            {
                return _isDraggable;
            }
            set
            {
                this.SetProperty(ref _isDraggable, value);
            }
        }
        
        private bool _isDraggableByPen = false;
        public bool IsDraggableByPen
        {
            get
            {
                return _isDraggableByPen;
            }
            set
            {
                this.SetProperty(ref _isDraggableByPen, value);
            }
        }

        private bool _isNoChrome = false;
        public bool IsNoChrome
        {
            get
            {
                return _isNoChrome;
            }
            set
            {
                this.SetProperty(ref _isNoChrome, value);
            }
        }

        private bool _isFiltered = false;
        public bool IsFiltered
        {
            get
            {
                return _isFiltered;
            }
            set
            {
                this.SetProperty(ref _isFiltered, value);
            }
        }

        private bool _isMenuEnabled = true;
        public bool IsMenuEnabled
        {
            get
            {
                return _isMenuEnabled;
            }
            set
            {
                this.SetProperty(ref _isMenuEnabled, value);
            }
        }

        private bool _isScaleFunctionEnabled = true;
        public bool IsScaleFunctionEnabled
        {
            get
            {
                return _isScaleFunctionEnabled;
            }
            set
            {
                this.SetProperty(ref _isScaleFunctionEnabled, value);
            }
        }

        private bool _isRemoveEnabled = false;
        public bool IsRemoveEnabled
        {
            get
            {
                return _isRemoveEnabled;
            }
            set
            {
                this.SetProperty(ref _isRemoveEnabled, value);
            }
        }

        private bool _isGestureEnabled = false;
        public bool IsGestureEnabled
        {
            get
            {
                return _isGestureEnabled;
            }
            set
            {
                this.SetProperty(ref _isGestureEnabled, value);
            }
        }

        private string _mainLabel = null;
        public string MainLabel
        {
            get
            {
                return _mainLabel;
            }
            set
            {
                this.SetProperty(ref _mainLabel, value);
            }
        }

        private string _sublabel = null;
        public string SubLabel
        {
            get
            {
                return _sublabel;
            }
            set
            {
                this.SetProperty(ref _sublabel, value);
            }
        }

        private AttributeOperationModel _attributeOperationModel = null;
        public AttributeOperationModel AttributeOperationModel
        {
            get
            {
                return _attributeOperationModel;
            }
            set
            {
                if (_attributeOperationModel != null)
                {
                    _attributeOperationModel.PropertyChanged -= _attributeOperationModel_PropertyChanged;
                }
                this.SetProperty(ref _attributeOperationModel, value);
                _attributeOperationModel.PropertyChanged += _attributeOperationModel_PropertyChanged;
                updateLabels();
            }
        }

        void _attributeOperationModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateLabels();
        }

        private void updateLabels()
        {
            MainLabel = _attributeOperationModel.AttributeModel.Name; //columnDescriptor.GetLabels(out mainLabel, out subLabel);

            string mainLabel = _attributeOperationModel.AttributeModel.Name;
            string subLabel = "";

            if (AttributeOperationModel.IsGrouped)
            {
                mainLabel = "[" + mainLabel + "]";
            }
            else if (AttributeOperationModel.IsBinned)
            {
                mainLabel = "[" + mainLabel + "] / [" + AttributeOperationModel.BinSize + "]";
            }
            else
            {
                mainLabel = addDetailToLabel(mainLabel);
            }

            MainLabel = mainLabel;
            SubLabel = subLabel;
        }

        private string addDetailToLabel(string name)
        {
            if (AttributeOperationModel.AggregateFunction == AggregateFunction.Avg)
            {
                name = "Avg(" + name + ")";
            }
            else if (AttributeOperationModel.AggregateFunction == AggregateFunction.Count)
            {
                name = "Count(" + name + ")";
            }
            else if (AttributeOperationModel.AggregateFunction == AggregateFunction.Max)
            {
                name = "Max(" + name + ")";
            }
            else if (AttributeOperationModel.AggregateFunction == AggregateFunction.Min)
            {
                name = "Min(" + name + ")";
            }
            else if (AttributeOperationModel.AggregateFunction == AggregateFunction.Sum)
            {
                name = "Sum(" + name + ")";
            }
            else if (AttributeOperationModel.AggregateFunction == AggregateFunction.Bin)
            {
                name = "Bin Range(" + name + ")";
            }

            if (AttributeOperationModel.ScaleFunction != ScaleFunction.None)
            {
                if (AttributeOperationModel.ScaleFunction == ScaleFunction.Log)
                {
                    name += " [Log]";
                }
                else if (AttributeOperationModel.ScaleFunction == ScaleFunction.Normalize)
                {
                    name += " [Normalize]";
                }
                else if (AttributeOperationModel.ScaleFunction == ScaleFunction.RunningTotal)
                {
                    name += " [RT]";
                }
                else if (AttributeOperationModel.ScaleFunction == ScaleFunction.RunningTotalNormalized)
                {
                    name += " [RT Norm]";
                }
            }
            return name;
        }
    }



    public interface AttributeViewModelEventHandler
    {
        void AttributeViewModelMoved(AttributeViewModel sender, AttributeViewModelEventArgs e, bool overElement);
        void AttributeViewModelDropped(AttributeViewModel sender, AttributeViewModelEventArgs e);
    }


    public class AttributeViewModelEventArgs : EventArgs
    {
        public Rct Bounds { get; set; }
        public AttributeOperationModel AttributeOperationModel { get; set; }
        public AttributeViewModelEventArgType Type { get; set; }
        public bool UseDefaultSize { get; set; }
        public VisualizationViewModel CreateLinkFrom { get; set; }

        public  AttributeViewModelEventArgs(AttributeOperationModel attributeOperationModel, Rct bounds, AttributeViewModelEventArgType type)
        {
            AttributeOperationModel = attributeOperationModel;
            Bounds = bounds;
            Type = type;
            UseDefaultSize = true;
        }
    }

    public enum AttributeViewModelEventArgType
    {
        Default,
        Copy,
        Snapshot
    }
}
