﻿using Microsoft.Practices.Prism.Mvvm;
using starPadSDK.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using starPadSDK.AppLib;
using PanoramicData.utils;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewModel : ExtendedBindableBase
    {
        public delegate void VisualizationViewModelUpdatedHandler(object sender, VisualizationViewModelUpdatedEventArgs e);
        public event VisualizationViewModelUpdatedHandler VisualizationViewModelUpdated;

        private static int _nextColorId = 0;
        public static Color[] COLORS = new Color[] {
            Color.FromRgb(26, 188, 156),
            Color.FromRgb(52, 152, 219),
            Color.FromRgb(52, 73, 94),
            Color.FromRgb(142, 68, 173),
            Color.FromRgb(241, 196, 15),
            Color.FromRgb(231, 76, 60),
            Color.FromRgb(149, 165, 166),
            Color.FromRgb(211, 84, 0),
            Color.FromRgb(189, 195, 199),
            Color.FromRgb(46, 204, 113),
            Color.FromRgb(155, 89, 182),
            Color.FromRgb(22, 160, 133),
            Color.FromRgb(41, 128, 185),
            Color.FromRgb(44, 62, 80),
            Color.FromRgb(230, 126, 34),
            Color.FromRgb(39, 174, 96),
            Color.FromRgb(243, 156, 18),
            Color.FromRgb(192, 57, 43),
            Color.FromRgb(127, 140, 141)
        };

        private Dictionary<AttributeFunction, List<AttributeViewModel>> _attributeFunctionViewModels = new Dictionary<AttributeFunction, List<AttributeViewModel>>();

        public VisualizationViewModel()
        {
            selectColor();
            _visualizationViewResultModel = new VisualizationViewResultModel();
        }

        private void selectColor()
        {
            if (_nextColorId >= COLORS.Count() - 1)
            {
                _nextColorId = 0;
            }
            Color = COLORS[_nextColorId++];
        }

        private SolidColorBrush _brush = null;
        public SolidColorBrush Brush
        {
            get
            {
                return _brush;
            }
            set
            {
                this.SetProperty(ref _brush, value);
            }
        }

        private Color _color = Color.FromArgb(0xff, 0x00, 0x00, 0x00);
        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                this.SetProperty(ref _color, value);
                Brush = new SolidColorBrush(_color);
            }
        }

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

        private VisualizationViewResultModel _visualizationViewResultModel;
        public VisualizationViewResultModel VisualizationViewResultModel
        {
            get
            {
                return _visualizationViewResultModel;
            }
        }

        public void AddFunctionAttributeViewModel(AttributeFunction attributeFunction, AttributeViewModel attributeViewModel)
        {
            if (!_attributeFunctionViewModels.ContainsKey(attributeFunction))
            {
                _attributeFunctionViewModels.Add(attributeFunction, new List<AttributeViewModel>());
            }

            if (!_attributeFunctionViewModels[attributeFunction].Contains(attributeViewModel))
            {
                _attributeFunctionViewModels[attributeFunction].Add(attributeViewModel);
                fireVisualizatinViewModelUpdated(VisualizationViewModelUpdatedEventType.Structure);
            }
        }

        private List<FilterItem> _filterItems = new List<FilterItem>();
        public List<FilterItem> FilterItems
        {
            get
            {
                return _filterItems;
            }
        }

        public void ClearFilterItems()
        {
            _filterItems.Clear();
            fireVisualizatinViewModelUpdated(VisualizationViewModelUpdatedEventType.FilterItems);
        }

        public void AddFilterItems(List<FilterItem> filterItems, object sender)
        {
            _filterItems.AddRange(filterItems);
            fireVisualizatinViewModelUpdated(VisualizationViewModelUpdatedEventType.Structure);
        }

        public void AddFilterItem(FilterItem filterItem, object sender)
        {
            _filterItems.Add(filterItem);
            fireVisualizatinViewModelUpdated(VisualizationViewModelUpdatedEventType.Structure);
        }

        public void RemoveFilterItem(FilterItem filterItem, object sender)
        {
            _filterItems.Remove(filterItem);
            fireVisualizatinViewModelUpdated(VisualizationViewModelUpdatedEventType.Structure);
        }

        public void RemoveFilterItems(List<FilterItem> filterItems, object sender)
        {
            foreach (var filterItem in filterItems)
            {
                _filterItems.Remove(filterItem);
            }
            if (filterItems.Count > 0)
            {
                fireVisualizatinViewModelUpdated(VisualizationViewModelUpdatedEventType.Structure);
            }
        }

        protected void fireVisualizatinViewModelUpdated(VisualizationViewModelUpdatedEventType type)
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

    public enum VisualizationViewModelUpdatedEventType { Structure, Rendering, Links, FilterItems }

    public enum VisualizationType { Table, Histogram, Map, Plot, Pie, Line, OneD, Frozen, Test }
}
