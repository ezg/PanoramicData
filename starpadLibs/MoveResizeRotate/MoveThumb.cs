using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System;
using System.Windows.Input;
using CombinedInputAPI;

namespace DiagramDesigner
{
    public class MoveThumb : Thumb
    {
        private Point _startDrag1 = new Point();
        private Point _current1 = new Point();
        private TouchDevice _dragDevice1 = null;

        public MoveThumb()
        {
            //DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(MoveThumb_TouchDownEvent));
        }

        void MoveThumb_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;

            ContentControl designerItem = DataContext as ContentControl;
            if (designerItem != null)
            {
                if (_dragDevice1 == null)
                {
                    e.Handled = true;
                    e.TouchDevice.Capture(this);
                    _startDrag1 = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;
                    _current1 = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;

                    this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(MoveThumb_TouchMoveEvent));
                    this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(MoveThumb_TouchUpEvent));
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

        void MoveThumb_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                this.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(MoveThumb_TouchMoveEvent));
                this.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(MoveThumb_TouchUpEvent));

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

        void MoveThumb_TouchMoveEvent(object sender, TouchEventArgs e)
        {
            ContentControl designerItem = DataContext as ContentControl;
            if (designerItem != null)
            {
                Point curDrag = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;

                if (e.TouchDevice == _dragDevice1)
                {
                    Vector vec = curDrag - _startDrag1;
                    Point dragDelta = new Point(vec.X, vec.Y);

                    Canvas.SetLeft(designerItem, Canvas.GetLeft(designerItem) + dragDelta.X);
                    Canvas.SetTop(designerItem, Canvas.GetTop(designerItem) + dragDelta.Y);

                    _startDrag1 = curDrag;
                    _current1 = e.GetTouchPoint((FrameworkElement)designerItem.Parent).Position;
                    e.Handled = true;

                    // notify content if needed
                    if (designerItem is TransformationAwareContentControl)
                    {
                        ((TransformationAwareContentControl)designerItem).NotifyMove(dragDelta);
                        ((TransformationAwareContentControl)designerItem).NotifyInteraction();
                    }
                }
            }
        }

        /*private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ContentControl designerItem = DataContext as ContentControl;

            if (designerItem != null)
            {
                Point dragDelta = new Point(e.HorizontalChange, e.VerticalChange);

                RotateTransform rotateTransform = designerItem.RenderTransform as RotateTransform;
                if (rotateTransform != null)
                {
                    dragDelta = rotateTransform.Transform(dragDelta);
                }

                Canvas.SetLeft(designerItem, Canvas.GetLeft(designerItem) + dragDelta.X);
                Canvas.SetTop(designerItem, Canvas.GetTop(designerItem) + dragDelta.Y);
            }
        }*/
    }
}
