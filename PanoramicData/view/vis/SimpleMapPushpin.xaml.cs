using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Maps.MapControl.WPF;
using PanoramicData.controller.data;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for SimpleMapPushpin.xaml
    /// </summary>
    public partial class SimpleMapPushpin : Pushpin
    {
        private MapFilterRenderer2 _map = null;

        private Brush _notSelectedBrush = null;
        private Brush _selectedBrush = null;
        private Color _notSelectedColor = Colors.Red;
        private Color _selectedColor = Colors.Red;
        private GradientStop _gradientStop = null;

        public PanoramicDataRow Row { get; set; }

        public FilteredItem FilteredItem { get; set; }

        public static readonly DependencyProperty PinColorProperty =
           DependencyProperty.Register("PinColor", typeof(Color),
           typeof(SimpleMapPushpin), new PropertyMetadata(Colors.Red));

        public Color PinColor
        {
            get
            {
                return (Color)GetValue(PinColorProperty);
            }
            set
            {
                SetValue(PinColorProperty, value);
            }
        }

        private string _point = null;
        public string Point
        {
            get
            {
                return _point;
            }
            set
            {
                _point = value;
                Visibility = Visibility.Collapsed;
                SetLocation();
            }
        }

        private bool _selected = false;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                _selected = value;
                Background = _selected ? _selectedBrush : _notSelectedBrush;
                PinColor = _selected ? _selectedColor : _notSelectedColor;
                if (_gradientStop != null)
                {
                    _gradientStop.Color = PinColor;
                }
                _map.setFiltredItem(FilteredItem, _selected);
            }
        }

        void SetLocation()
        {
            string toSplit = "";
            if (_point.Contains("(") && _point.Contains(")"))
            {
                toSplit = _point.Substring(_point.IndexOf("("));
                toSplit = toSplit.Substring(1, toSplit.IndexOf(")") - 1);
            }
            else
            {
                toSplit = _point;
            }

            string[] splits = toSplit.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            if (splits.Count() == 2)
            {
                Location = new Microsoft.Maps.MapControl.WPF.Location(double.Parse(splits[1]), double.Parse(splits[0]));
                Visibility = Visibility.Visible;

                List<Microsoft.Maps.MapControl.WPF.Location> locs = new List<Microsoft.Maps.MapControl.WPF.Location>();
                locs.Add(Location);
                foreach (var pp in _map.map.Children)
                {
                    if (pp is SimpleMapPushpin)
                    {
                        if (((SimpleMapPushpin)pp).Location != null)
                        {
                            locs.Add(((SimpleMapPushpin)pp).Location);
                        }
                    }
                }

                LocationRect rect = new LocationRect(locs);
                double height = rect.North - rect.South;
                double width = rect.East - rect.West;
                if (locs.Count > 1)
                {
                    //rect.East += width / 15;
                    //rect.West -= width / 15;
                    //rect.North += height / 15;
                    //rect.South -= height / 15;
                }
                //Console.WriteLine(_map.map.ZoomLevel);
                //
                //_map.map.SetView(rect);
                //_map.map.ZoomLevel = _map.map.ZoomLevel * 0.7;
            }
        }

        
        public SimpleMapPushpin()
        {
            InitializeComponent();
            this.Width = double.NaN;
            this.Height = double.NaN;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _gradientStop = (GradientStop)GetTemplateChild("gradientStop");
            _gradientStop.Color = PinColor;
        }

        public void init(string point, string content, MapFilterRenderer2 map, Color c)
        {
            if (map.FilterModel != null)
            {
                _map = map;
                Point = point;
                tb.Text = content;
                Foreground = Brushes.Black;
                this.TouchUp += MapPushpin_TouchUp;
                this.MouseDown += MapPushpin_MouseDown;

                _notSelectedColor = Color.FromArgb(50, c.R, c.G, c.B);
                _selectedColor = c;
                _notSelectedBrush = new SolidColorBrush(_notSelectedColor);
                _selectedBrush = new SolidColorBrush(_selectedColor);

                Background = _notSelectedBrush;
                PinColor = _notSelectedColor;
            }
        }

        void MapPushpin_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Selected = !this.Selected;
            e.Handled = true;
        }

        void MapPushpin_TouchUp(object sender, TouchEventArgs e)
        {

        }

    }
}
