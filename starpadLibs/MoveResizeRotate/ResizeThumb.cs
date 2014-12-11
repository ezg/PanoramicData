using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using starPadSDK.Geom;
using System.Windows.Input;
using CombinedInputAPI;

namespace DiagramDesigner
{
    public class ResizeThumb : Thumb
    {
        private double angle;
        private Point transformOrigin;
        private ContentControl designerItem;

        private Point _startDrag1 = new Point();
        private Point _current1 = new Point();
        private TouchDevice _dragDevice1 = null;

        public ResizeThumb()
        {
            //DragStarted += new DragStartedEventHandler(this.ResizeThumb_DragStarted);
            //DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(ResizeThumb_TouchDownEvent));
        }

        void ResizeThumb_TouchDownEvent(Object sender, TouchEventArgs e)
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

                    this.transformOrigin = this.designerItem.RenderTransformOrigin;
                    RotateTransform rotateTransform = this.designerItem.RenderTransform as RotateTransform;

                    if (rotateTransform != null)
                    {
                        this.angle = rotateTransform.Angle * Math.PI / 180.0;
                    }
                    else
                    {
                        this.angle = 0;
                    }

                    this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(ResizeThumb_TouchDragEvent));
                    this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(ResizeThumb_TouchUpEvent));
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

        void ResizeThumb_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                this.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(ResizeThumb_TouchDragEvent));
                this.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(ResizeThumb_TouchUpEvent));
                
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

        void ResizeThumb_TouchDragEvent(object sender, TouchEventArgs e)
        {
            this.designerItem = DataContext as ContentControl;
            if (designerItem != null)
            {
                Point curDrag = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;

                if (e.TouchDevice == _dragDevice1)
                {
                    Vec oldSize = new Vec(this.designerItem.Width, this.designerItem.Height);
                    Vec vec = curDrag - _startDrag1;
                    Mat m = Mat.Rotate(new Deg(new Rad(-this.angle)));
                    vec = m * vec;
                    Point dragDelta = new Point(vec.X, vec.Y);

                    double deltaVertical, deltaHorizontal;
                    Pt topLeftStart = new Pt(Canvas.GetLeft(this.designerItem), Canvas.GetTop(this.designerItem));

                    switch (VerticalAlignment)
                    {
                        case System.Windows.VerticalAlignment.Bottom:
                            deltaVertical = Math.Min(-dragDelta.Y, this.designerItem.ActualHeight - this.designerItem.MinHeight);
                            Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + (this.transformOrigin.Y * deltaVertical * (1 - Math.Cos(-this.angle))));
                            Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) - deltaVertical * this.transformOrigin.Y * Math.Sin(-this.angle));
                            this.designerItem.Height -= deltaVertical;
                            break;
                        case System.Windows.VerticalAlignment.Top:
                            deltaVertical = Math.Min(dragDelta.Y, this.designerItem.ActualHeight - this.designerItem.MinHeight);
                            Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + deltaVertical * Math.Cos(-this.angle) + (this.transformOrigin.Y * deltaVertical * (1 - Math.Cos(-this.angle))));
                            Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + deltaVertical * Math.Sin(-this.angle) - (this.transformOrigin.Y * deltaVertical * Math.Sin(-this.angle)));
                            this.designerItem.Height -= deltaVertical;
                            break;
                        default:
                            break;
                    }

                    switch (HorizontalAlignment)
                    {
                        case System.Windows.HorizontalAlignment.Left:
                            deltaHorizontal = Math.Min(dragDelta.X, this.designerItem.ActualWidth - this.designerItem.MinWidth);
                            Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + deltaHorizontal * Math.Sin(this.angle) - this.transformOrigin.X * deltaHorizontal * Math.Sin(this.angle));
                            Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + deltaHorizontal * Math.Cos(this.angle) + (this.transformOrigin.X * deltaHorizontal * (1 - Math.Cos(this.angle))));
                            this.designerItem.Width -= deltaHorizontal;
                            break;
                        case System.Windows.HorizontalAlignment.Right:
                            deltaHorizontal = Math.Min(-dragDelta.X, this.designerItem.ActualWidth - this.designerItem.MinWidth);
                            Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) - this.transformOrigin.X * deltaHorizontal * Math.Sin(this.angle));
                            Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + (deltaHorizontal * this.transformOrigin.X * (1 - Math.Cos(this.angle))));
                            this.designerItem.Width -= deltaHorizontal;
                            break;
                        default:
                            break;
                    }

                    _startDrag1 = curDrag;
                    _current1 = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;
                    e.Handled = true;

                    Vec newSize = new Vec(this.designerItem.Width, this.designerItem.Height);
                    // notify content if needed
                    if (designerItem is TransformationAwareContentControl)
                    {
                        ((TransformationAwareContentControl)designerItem).NotifyScale(
                            new Vec(newSize.X / oldSize.X, newSize.Y / oldSize.Y),
                                new Pt(Canvas.GetLeft(this.designerItem), Canvas.GetTop(this.designerItem)) - topLeftStart);
                        ((TransformationAwareContentControl)designerItem).NotifyInteraction();
                    }
                }
            }
        }

        /*private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            this.designerItem = DataContext as ContentControl;

            if (this.designerItem != null)
            {
                this.transformOrigin = this.designerItem.RenderTransformOrigin;
                RotateTransform rotateTransform = this.designerItem.RenderTransform as RotateTransform;

                if (rotateTransform != null)
                {
                    this.angle = rotateTransform.Angle * Math.PI / 180.0;
                }
                else
                {
                    this.angle = 0;
                }
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this.designerItem != null)
            {
                double deltaVertical, deltaHorizontal;

                switch (VerticalAlignment)
                {
                    case System.Windows.VerticalAlignment.Bottom:
                        deltaVertical = Math.Min(-dragDelta.Y, this.designerItem.ActualHeight - this.designerItem.MinHeight);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + (this.transformOrigin.Y * deltaVertical * (1 - Math.Cos(-this.angle))));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) - deltaVertical * this.transformOrigin.Y * Math.Sin(-this.angle));
                        this.designerItem.Height -= deltaVertical;
                        break;
                    case System.Windows.VerticalAlignment.Top:
                        deltaVertical = Math.Min(dragDelta.Y, this.designerItem.ActualHeight - this.designerItem.MinHeight);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + deltaVertical * Math.Cos(-this.angle) + (this.transformOrigin.Y * deltaVertical * (1 - Math.Cos(-this.angle))));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + deltaVertical * Math.Sin(-this.angle) - (this.transformOrigin.Y * deltaVertical * Math.Sin(-this.angle)));
                        this.designerItem.Height -= deltaVertical;
                        break;
                    default:
                        break;
                }

                switch (HorizontalAlignment)
                {
                    case System.Windows.HorizontalAlignment.Left:
                        deltaHorizontal = Math.Min(dragDelta.X, this.designerItem.ActualWidth - this.designerItem.MinWidth);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + deltaHorizontal * Math.Sin(this.angle) - this.transformOrigin.X * deltaHorizontal * Math.Sin(this.angle));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + deltaHorizontal * Math.Cos(this.angle) + (this.transformOrigin.X * deltaHorizontal * (1 - Math.Cos(this.angle))));
                        this.designerItem.Width -= deltaHorizontal;
                        break;
                    case System.Windows.HorizontalAlignment.Right:
                        deltaHorizontal = Math.Min(-dragDelta.X, this.designerItem.ActualWidth - this.designerItem.MinWidth);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) - this.transformOrigin.X * deltaHorizontal * Math.Sin(this.angle));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + (deltaHorizontal * this.transformOrigin.X * (1 - Math.Cos(this.angle))));
                        this.designerItem.Width -= deltaHorizontal;
                        break;
                    default:
                        break;
                }
            }

            e.Handled = true;
        }*/
    }
}
