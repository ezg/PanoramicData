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
using System.Windows.Navigation;
using System.Windows.Shapes;
using InputFramework.WPFDevices;
using starPadSDK.Geom;

namespace starPadSDK.Inq
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ButtonGestureDialog : UserControl
    {
        private Point _startDrag = new Point();
        private WPFPointDevice _dragDevice = null;
        private bool _draggable = false;
        private FrameworkElement _dragFeedback = null;

        public delegate void InteractionStartHandler(object sender, RoutedPointEventArgs e);
        public event InteractionStartHandler InteractionStart;

        public delegate void InteractionEndHandler(object sender, RoutedPointEventArgs e);
        public event InteractionEndHandler InteractionEnd;

        public delegate void InteractionMoveHandler(object sender, RoutedPointEventArgs e);
        public event InteractionMoveHandler InteractionMove;

        public delegate void TappedHandler(object sender, RoutedPointEventArgs e);
        public event TappedHandler Tapped;

        public ButtonGestureDialog(String gestureName, bool draggable)
        {
            this.InitializeComponent();
            LabelText.Text = gestureName;
            _draggable = draggable;

            if (_draggable)
            {
                BackgroundRect.Background = new SolidColorBrush(Color.FromArgb(255, 30, 144, 255));
                this.AddHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(pointDownEvent));
            }
            else
            {
                BackgroundRect.Background = new SolidColorBrush(Color.FromArgb(255, 118, 118, 118));
                this.AddHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(pointDownEvent));
            }
        }

        public void SetDragFeedback(FrameworkElement dragFeedback)
        {
            _dragFeedback = dragFeedback;
        }

        void pointDownEvent(Object sender, RoutedPointEventArgs e)
        {
            if (_draggable)
            {
                if (e.DeviceType != InputFramework.DeviceType.MultiTouch)
                    return;

                if (_dragDevice == null)
                {
                    InteractionStart(this, e);

                    e.Handled = true;
                    e.WPFPointDevice.Capture(this);
                    _startDrag = e.GetPosition((FrameworkElement)this.Parent);

                    this.AddHandler(WPFPointDevice.PointDragEvent, new RoutedPointEventHandler(pointDragEvent));
                    this.AddHandler(WPFPointDevice.PointUpEvent, new RoutedPointEventHandler(pointUpEvent));
                    _dragDevice = e.WPFPointDevice;
                }
            }
            else
            {
                e.Handled = true;
                if (Tapped != null)
                {
                    Tapped(this, e);
                }
            }
        }

        void pointUpEvent(object sender, RoutedPointEventArgs e)
        {
            if (e.WPFPointDevice == _dragDevice)
            {
                InteractionEnd(this, e);

                e.Handled = true;
                this.RemoveHandler(WPFPointDevice.PointDragEvent, new RoutedPointEventHandler(pointDragEvent));
                this.RemoveHandler(WPFPointDevice.PointUpEvent, new RoutedPointEventHandler(pointUpEvent));
                e.WPFPointDevice.Capture(null);
                _dragDevice = null;
            }
        }

        void pointDragEvent(object sender, RoutedPointEventArgs e)
        {
            if (e.WPFPointDevice == _dragDevice)
            {
                Point curDrag = e.GetPosition((FrameworkElement)this.Parent);
                Vector dragBy = curDrag - _startDrag;

                if (_dragFeedback == null)
                {
                    this.RenderTransform = new MatrixTransform(((Mat)this.RenderTransform.Value) * Mat.Translate(dragBy));
                }
                else
                {
                    _dragFeedback.RenderTransform = new MatrixTransform(((Mat)_dragFeedback.RenderTransform.Value) * Mat.Translate(dragBy));
                }
                InteractionMove(this, e);
                _startDrag = curDrag;
                e.Handled = true;
            }
        }

    }
}
