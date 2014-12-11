using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using InputFramework.WPFDevices;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;

namespace starPadSDK.AppLib
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class PieMenuControl : UserControl
    {
        public class PieMenuItem : Control
        {
            public Image image;

            public PieMenuItem(BitmapImage imageSource)
            {
                image = new Image();
                image.Source = imageSource;
            }

            public virtual void Fire() { }
        }
        
        int    _count;
        double _width;
        double _height;
        double _angle;

        Ellipse _selectedCircle;
        int     _selectedIndex;

        Path _path;
        DispatcherTimer _timer;
        Ellipse _invisibleCircle;
        
        int _frameCount;
        int _currentFrame;
        int _currentCount;

        public Point center;
        public List<PieMenuItem> _menuItems;

        public double itemRadius;
        public double menuRadius;
        public bool   isComplete;

        void   drawPieMenu()
        {
            drawCircle();

            drawMenuItems();
        }
        void   drawCircle()
        {
            double innerRadius = menuRadius - itemRadius;
            double outerRadius = menuRadius + itemRadius;

            _path = new Path();
            _path.Stroke = System.Windows.Media.Brushes.DarkGray;
            _path.Fill = System.Windows.Media.Brushes.LightGray; 

            GeometryGroup circle = new GeometryGroup();

            EllipseGeometry innerCircle = new EllipseGeometry();
            innerCircle.Center = new Point(outerRadius, outerRadius);
            innerCircle.RadiusX = innerRadius;
            innerCircle.RadiusY = innerRadius;

            EllipseGeometry outerCircle = new EllipseGeometry();
            outerCircle.Center = new Point(outerRadius, outerRadius);
            outerCircle.RadiusX = outerRadius;
            outerCircle.RadiusY = outerRadius;

            circle.Children.Add(innerCircle);
            circle.Children.Add(outerCircle);

            circle.FillRule = FillRule.EvenOdd;

            _path.Data = circle;

            canvas.Children.Add(_path);

            _invisibleCircle = new Ellipse();
            _invisibleCircle.Opacity = 0.05;
            _invisibleCircle.Fill = System.Windows.Media.Brushes.Green;
            _invisibleCircle.Stroke = System.Windows.Media.Brushes.Black;
            _invisibleCircle.Width = 2 * outerRadius;
            _invisibleCircle.Height = 2 * outerRadius;

            canvas.Children.Add(_invisibleCircle);
        }
        void   drawMenuItems()
        {
            for (int i = 0; i < _count; i++)
            {
                Image image = _menuItems[i].image;

                double radian = convertToRadian(90 + i * _angle);

                double cos = Math.Cos(radian);
                double sin = Math.Sin(radian);

                TransformGroup transGroup = new TransformGroup();
                transGroup.Children.Add(new TranslateTransform(center.X - _width / 2 + menuRadius * cos, center.Y - _height / 2 + menuRadius * sin));

                image.RenderTransform = transGroup;

                canvas.Children.Add(image);
            }
        }
        double convertToRadian(double angle)
        {
            return angle * Math.PI / 180;
        }
        void   rotatePieMenu(object sender, EventArgs e)
        {
            if (_currentCount < _count)
            {
                int rotateIndex = (_selectedIndex + _currentCount) % _count;

                Image image = _menuItems[rotateIndex].image;

                TransformGroup transGroup = new TransformGroup();
                transGroup.Children.Add(image.RenderTransform);
                transGroup.Children.Add(new RotateTransform(_angle / _frameCount, center.X, center.Y));

                image.RenderTransform = transGroup;

                if (_currentFrame >= _frameCount)
                {
                    _currentFrame = 0;
                    _currentCount++;

                    canvas.Children.Remove(image);

                    return;
                }

                _currentFrame++;

                return;
            }

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 1.0;
            animation.To = 0;
            animation.Duration = new Duration(TimeSpan.FromSeconds(1.2));

            BeginAnimation(PieMenuControl.OpacityProperty, animation);

            _timer.Stop();
        }
        int    getSelectedItemIndex(Point pos)
        {
            int index = -1;

            Vector vec = new Vector(pos.X - center.X, pos.Y - center.Y);

            if (vec.Length > (menuRadius - itemRadius))
            {
                Vector yAxis = new Vector(0, 1);

                double angleBetween = Vector.AngleBetween(yAxis, vec);

                if (angleBetween < 0)
                    angleBetween += 360;

                angleBetween += _angle / 2;

                if (angleBetween > 360)
                    angleBetween -= 360;
                
                index = (int)(angleBetween / _angle);
            }

            return index;
        }

        void pointDown(object sender, RoutedPointEventArgs e)
        {
            e.WPFPointDevice.Capture(this);
            Opacity = 1.0;
            e.Handled = true;
        }
        void pointMove(object sender, RoutedPointEventArgs e)
        {
            if (isComplete)
                return;

            canvas.Children.Remove(_selectedCircle);

            Point pos = e.GetPosition(canvas);

            int index = getSelectedItemIndex(pos);

            if (index >= 0)
            {
                double radian = convertToRadian(90 + index * _angle);

                double cos = Math.Cos(radian);
                double sin = Math.Sin(radian);

                _selectedCircle = new Ellipse();
                _selectedCircle.Stroke = System.Windows.Media.Brushes.Black;
                _selectedCircle.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });
                _selectedCircle.Width = 2 * itemRadius;
                _selectedCircle.Height = 2 * itemRadius;

                TransformGroup transGroup = new TransformGroup();
                transGroup.Children.Add(new TranslateTransform(center.X - itemRadius + menuRadius * cos, center.Y - itemRadius + menuRadius * sin));

                _selectedCircle.RenderTransform = transGroup;

                canvas.Children.Add(_selectedCircle);
            }

            e.Handled = true;  // may not need this if the contact has been captured to the PieMenu already (if so, don't Handle it here)
        }
        void pointEnd(object sender, RoutedPointEventArgs e)
        {
            Opacity = 0.15;
            if (isComplete)
                return;

            canvas.Children.Remove(_selectedCircle);

            Pt pos = e.GetPosition(canvas);

            _selectedIndex = getSelectedItemIndex(pos);

            if (_selectedIndex < 0)
                return;

            isComplete = true;

            canvas.Children.Remove(_path);
            canvas.Children.Remove(_invisibleCircle);

            _menuItems[_selectedIndex].Fire();

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(rotatePieMenu);
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 12);
            _timer.Start();

            e.Handled = true;
        }
        public PieMenuControl(List<PieMenuItem> menuItems, double width, double height)
        {
            InitializeComponent();

            _menuItems = menuItems;

            _count = menuItems.Count<PieMenuItem>();
            _width = width;
            _height = height;
            Opacity = 0.25;

            _angle = 360.0 / _count;
            itemRadius = Math.Sqrt(_width * _width + _height * _height) / 2;
            menuRadius = itemRadius / Math.Tan(convertToRadian(_angle / 2));

            center = new Point(itemRadius + menuRadius, itemRadius + menuRadius);

            _frameCount = 2;
            _currentFrame = 0;
            _currentCount = 1;

            isComplete = false;

            drawPieMenu();

            AddHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(pointDown));
            AddHandler(WPFPointDevice.PointDragEvent, new RoutedPointEventHandler(pointMove));
            AddHandler(WPFPointDevice.PointUpEvent, new RoutedPointEventHandler(pointEnd));
        }
    }
}
