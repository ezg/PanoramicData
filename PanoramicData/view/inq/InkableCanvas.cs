using CombinedInputAPI;
using PanoramicData.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PanoramicData.view.inq
{
    public class InkableCanvas : Canvas
    {
        public delegate void InkCollectedEventHandler(object sender, InkCollectedEventArgs e);
        public event InkCollectedEventHandler InkCollectedEvent;

        private InkStroke _currentInkStroke;
        private bool _isPointerPressed;

        public InkableCanvas()
        {
            this.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        }

        /*protected override void OnStylusDown(StylusDownEventArgs e)
        {
            if (e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus)
            {
                return;
            }

            _isPointerPressed = true;

            _currentInkStroke = new InkStroke();
            _previousContactPt = e.GetStylusPoints(this)[0].ToPoint();
            _currentInkStroke.Add(new Point(_previousContactPt.X, _previousContactPt.Y));
            Children.Add(_currentInkStroke);
            e.Handled = true;
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            if (e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus)
            {
                return;
            }

            Point currentContactPt = e.GetStylusPoints(this)[0].ToPoint();
            Color strokeColor = Colors.Black;

            StylusPointCollection spc = new StylusPointCollection();

            processStylusCollection(e.GetStylusPoints(null, spc.Description), strokeColor);
            e.Handled = true;
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            if (e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus)
            {
                return;
            }

            handleUp(e.GetStylusPoints(null)[0].ToPoint());
            e.Handled = true;
        }

        private void processStylusCollection(StylusPointCollection spc, Color strokeColor)
        {
            foreach (var stylusPoint in spc)
            {
                Point current = MainViewController.Instance.MainWindow.TranslatePoint(stylusPoint.ToPoint(), this);
                _currentInkStroke.Add(new Point(current.X, current.Y));

                if (Helpers.Distance(current, _previousContactPt) > 1)
                {
                    var line = new Line
                    {
                        X1 = _previousContactPt.X,
                        Y1 = _previousContactPt.Y,
                        X2 = current.X,
                        Y2 = current.Y,
                        StrokeThickness = 3,
                        StrokeEndLineCap = PenLineCap.Round,
                        Stroke = new SolidColorBrush(strokeColor)
                    };

                    _previousContactPt = current;
                    GetDrawingCanvas().Children.Add(line);
                }
            }
        }

         */

        protected override void OnTouchDown(TouchEventArgs e)
        {
            if (!(e.Device is StylusDevice || (e.Device is MouseTouchDevice && (e.Device as MouseTouchDevice).IsStylus)))
            {
                return;
            }
            handleDown(e.GetTouchPoint(this).Position);
            e.Handled = true;
        }

        private void handleDown(Point pt)
        {
            _isPointerPressed = true;
            List<Point> pts = new List<Point>();
            pts.Add(pt);
            _currentInkStroke = new InkStroke(pts);
            addDrawingInkStroke(_currentInkStroke);
        }

        protected override void OnTouchMove(TouchEventArgs e)
        {
            if (!(e.Device is StylusDevice || (e.Device is MouseTouchDevice && (e.Device as MouseTouchDevice).IsStylus)))
            {
                return;
            }
            if (!_isPointerPressed)
            {
                return;
            }

            handleMove(e.GetTouchPoint(this).Position);
            e.Handled = true;
        }

        private void handleMove(Point pt)
        {
            _currentInkStroke.Add(new Point(pt.X, pt.Y));
        }

        protected override void OnTouchUp(TouchEventArgs e)
        {
            if (!(e.Device is StylusDevice || (e.Device is MouseTouchDevice && (e.Device as MouseTouchDevice).IsStylus)))
            {
                return;
            }
            if (!_isPointerPressed)
            {
                e.Handled = true;
                return;
            }

            handleUp(e.GetTouchPoint(this).Position);
            e.Handled = true;
        }

        private void handleUp(Point pt)
        {
            _currentInkStroke.Add(new Point(pt.X, pt.Y));
            removeDrawingInkStroke(_currentInkStroke);
            fireInkCollected(_currentInkStroke);
            _currentInkStroke = null;
            _isPointerPressed = false;

        }

        private void addDrawingInkStroke(InkStroke s)
        {
            Children.Add(s);
        }

        private void removeDrawingInkStroke(InkStroke s)
        {
            foreach (var c in Children)
            {
                if (c is InkStrokeElement && (c as InkStrokeElement).InkStroke == s)
                {
                    Children.Remove(c as InkStrokeElement);
                    break;
                }
            }
        }

        private void fireInkCollected(InkStroke s)
        {
            if (InkCollectedEvent != null)
            {
                InkCollectedEvent(this, new InkCollectedEventArgs(s));
            }
        }
        
    }

    public class InkCollectedEventArgs : RoutedEventArgs
    {
        public InkStroke InkStroke { get; private set; }

        public InkCollectedEventArgs(InkStroke s)
        {
            InkStroke = s;
        }
    }
}
