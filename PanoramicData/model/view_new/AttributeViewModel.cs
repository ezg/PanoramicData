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

        public AttributeViewModel(AttributeModel attributeModel, AttributeOperationModel attributeOperationModel)
        {
            AttributeModel = attributeModel;
            AttributeOperationModel = attributeOperationModel;
        }

        public AttributeViewModel(AttributeViewModel attributeViewModel)
            : this(attributeViewModel.AttributeModel, attributeViewModel.AttributeOperationModel)
        {
        }

        public void FireMoved(Rct bounds, AttributeViewModelEventArgType type)
        {
            if (AttributeViewModelMoved != null)
            {
                AttributeViewModelMoved(this, new AttributeViewModelEventArgs(this, bounds, type));
            }
        }

        public void FireDropped(Rct bounds, AttributeViewModelEventArgType type)
        {
            if (AttributeViewModelDropped != null)
            {
                AttributeViewModelDropped(this, new AttributeViewModelEventArgs(this, bounds, type));
            }
        }

        private bool _isShadow = true;
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

        private bool _isDraggableByPen = true;
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

        private bool _isUpSorted = false;
        public bool IsUpSorted
        {
            get
            {
                return _isUpSorted;
            }
            set
            {
                this.SetProperty(ref _isUpSorted, value);
            }
        }

        private bool _isDownSorted = false;
        public bool IsDownSorted
        {
            get
            {
                return _isDownSorted;
            }
            set
            {
                this.SetProperty(ref _isDownSorted, value);
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


        private AttributeModel _attributeModel = null;
        public AttributeModel AttributeModel
        {
            get
            {
                return _attributeModel;
            }
            set
            {
                if (_attributeModel != null)
                {
                    _attributeModel.PropertyChanged -= _attributeModel_PropertyChanged;
                }
                this.SetProperty(ref _attributeModel, value);
                _attributeModel.PropertyChanged += _attributeModel_PropertyChanged;
                updateLabels();
            }
        }

        void _attributeModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateLabels();
        }

        private void updateLabels()
        {
            MainLabel = _attributeModel.Name; //columnDescriptor.GetLabels(out mainLabel, out subLabel);
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
        public AttributeViewModel AttributeViewModel { get; set; }
        public AttributeViewModelEventArgType Type { get; set; }
        public bool UseDefaultSize { get; set; }
        public VisualizationViewModel CreateLinkFrom { get; set; }

        public AttributeViewModelEventArgs() { }
        public AttributeViewModelEventArgs(AttributeViewModel attributeViewModel, Rct bounds, AttributeViewModelEventArgType type)
        {
            AttributeViewModel = attributeViewModel;
            Bounds = bounds;
            Type = type;
        }
    }

    public enum AttributeViewModelEventArgType
    {
        Default,
        Copy,
        Snapshot
    }
}
