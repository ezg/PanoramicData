using Microsoft.Practices.Prism.Mvvm;
using starPadSDK.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using starPadSDK.AppLib;
using PanoramicData.utils;
using System.Collections.ObjectModel;
using PanoramicData.model.data;
using System.Windows;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewModel : ExtendedBindableBase
    {
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

        public VisualizationViewModel()
        {
        }

        public VisualizationViewModel(SchemaModel schemaModel)
        {
            _queryModel = new QueryModel(schemaModel);
            selectColor();
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
                this.SetProperty(ref _queryModel, value);
            }
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

        private SolidColorBrush _faintBrush = null;
        public SolidColorBrush FaintBrush
        {
            get
            {
                return _faintBrush;
            }
            set
            {
                this.SetProperty(ref _faintBrush, value);
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
                FaintBrush = new SolidColorBrush(Color.FromArgb(70, _color.R, _color.G, _color.B));
            }
        }

        private Vector2 _size = new Vector2(180, 100);
        public Vector2 Size
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

        private Point _postion;
        public Point Position
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
    }

    public enum VisualizationType { Table, Histogram, Map, Plot, Pie, Line, OneD, Frozen, Test }
}
