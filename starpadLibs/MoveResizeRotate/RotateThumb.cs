using CombinedInputAPI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner
{
    public class RotateThumb : Thumb
    {
        private Point centerPoint;
        private Vector startVector;
        private double initialAngle;
        private Canvas designerCanvas;
        private ContentControl designerItem;
        private RotateTransform rotateTransform;

        private Point _startDrag1 = new Point();
        private Point _current1 = new Point();
        private TouchDevice _dragDevice1 = null;

        public RotateThumb()
        {
            //DragDelta += new DragDeltaEventHandler(this.RotateThumb_DragDelta);
            //DragStarted += new DragStartedEventHandler(this.RotateThumb_DragStarted);

            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(RotateThumb_TouchDownEvent));
        }

        void RotateThumb_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;

            this.designerItem = DataContext as ContentControl;
            if (designerItem != null)
            {
                if (_dragDevice1 == null)
                {
                    e.Handled = true;
                    e.TouchDevice.Capture(this);
                    _startDrag1 = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;
                    _current1 = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;

                    this.designerCanvas = VisualTreeHelper.GetParent(this.designerItem) as Canvas;

                    if (this.designerCanvas != null)
                    {
                        this.centerPoint = this.designerItem.TranslatePoint(
                            new Point(this.designerItem.Width * this.designerItem.RenderTransformOrigin.X,
                                      this.designerItem.Height * this.designerItem.RenderTransformOrigin.Y),
                                      this.designerCanvas);

                        Point startPoint = e.GetTouchPoint(this.designerCanvas).Position;
                        this.startVector = Point.Subtract(startPoint, this.centerPoint);

                        this.rotateTransform = this.designerItem.RenderTransform as RotateTransform;
                        if (this.rotateTransform == null)
                        {
                            this.designerItem.RenderTransform = new RotateTransform(0);
                            this.initialAngle = 0;
                        }
                        else
                        {
                            this.initialAngle = this.rotateTransform.Angle;
                        }
                    }

                    this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(RotateThumb_TouchMoveEvent));
                    this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(RotateThumb_TouchUpEvent));
                    _dragDevice1 = e.TouchDevice;

                    // notify content if needed
                    if (designerItem is TransformationAwareContentControl)
                    {
                        ((TransformationAwareContentControl)designerItem).PreTransformation();
                        ((TransformationAwareContentControl)designerItem).NotifyInteraction();
                    }
                }
            }
        }

        void RotateThumb_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                this.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(RotateThumb_TouchMoveEvent));
                this.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(RotateThumb_TouchUpEvent));

                ContentControl designerItem = DataContext as ContentControl;
                if (designerItem != null)
                {
                    // notify content if needed
                    if (designerItem is TransformationAwareContentControl)
                    {
                        ((TransformationAwareContentControl)designerItem).PostTransformation();
                        ((TransformationAwareContentControl)designerItem).NotifyInteraction();
                    }
                }
            }

        }

        void RotateThumb_TouchMoveEvent(object sender, TouchEventArgs e)
        {
            this.designerItem = DataContext as ContentControl;
            if (designerItem != null && this.designerCanvas != null)
            {
                Point curDrag = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;

                if (e.TouchDevice == _dragDevice1)
                {
                    Vector vec = curDrag - _startDrag1;
                    Point dragDelta = new Point(vec.X, vec.Y);

                    Point currentPoint = e.GetTouchPoint(this.designerCanvas).Position;
                    Vector deltaVector = Point.Subtract(currentPoint, this.centerPoint);

                    double angle = Vector.AngleBetween(this.startVector, deltaVector);

                    RotateTransform rotateTransform = this.designerItem.RenderTransform as RotateTransform;
                    double currentAngle = rotateTransform.Angle;
                    rotateTransform.Angle = this.initialAngle + Math.Round(angle, 0);
                    this.designerItem.InvalidateMeasure();
                    
                    _startDrag1 = curDrag;
                    _current1 = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;
                    e.Handled = true;

                    // notify content if needed
                    if (designerItem is TransformationAwareContentControl)
                    {
                        ((TransformationAwareContentControl)designerItem).NotifyRotate(rotateTransform.Angle - currentAngle);
                        ((TransformationAwareContentControl)designerItem).NotifyInteraction();
                    }
                }
            }
        }

        /*private void RotateThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            this.designerItem = DataContext as ContentControl;

            if (this.designerItem != null)
            {
                this.designerCanvas = VisualTreeHelper.GetParent(this.designerItem) as Canvas;

                if (this.designerCanvas != null)
                {
                    this.centerPoint = this.designerItem.TranslatePoint(
                        new Point(this.designerItem.Width * this.designerItem.RenderTransformOrigin.X,
                                  this.designerItem.Height * this.designerItem.RenderTransformOrigin.Y),
                                  this.designerCanvas);

                    Point startPoint = Mouse.GetTouchPoint(this.designerCanvas);
                    this.startVector = Point.Subtract(startPoint, this.centerPoint);

                    this.rotateTransform = this.designerItem.RenderTransform as RotateTransform;
                    if (this.rotateTransform == null)
                    {
                        this.designerItem.RenderTransform = new RotateTransform(0);
                        this.initialAngle = 0;
                    }
                    else
                    {
                        this.initialAngle = this.rotateTransform.Angle;
                    }
                }
            }
        }

        private void RotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this.designerItem != null && this.designerCanvas != null)
            {
                Point currentPoint = Mouse.GetTouchPoint(this.designerCanvas);
                Vector deltaVector = Point.Subtract(currentPoint, this.centerPoint);

                double angle = Vector.AngleBetween(this.startVector, deltaVector);

                RotateTransform rotateTransform = this.designerItem.RenderTransform as RotateTransform;
                rotateTransform.Angle = this.initialAngle + Math.Round(angle, 0);
                this.designerItem.InvalidateMeasure();
            }
        }*/
    }
}
