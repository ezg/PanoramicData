using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Navigation 
{
    /// <summary>
    /// Represents a navigation methods upon one axis - mouse panning and zooming.
    /// </summary>
    public sealed class AxisNavigation : ContentGraph 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AxisNavigation"/> class.
        /// </summary>
        public AxisNavigation()
        {
            SetHorizontalOrientation();
            Content = content;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisNavigation"/> class.
        /// </summary>
        /// <param name="orientation">The orientation.</param>
        public AxisNavigation(Orientation orientation)
        {
            Orientation = orientation;
            Content = content;
        }

        private void SetHorizontalOrientation()
        {
            Grid.SetColumn(this, 1);
            Grid.SetRow(this, 2);
        }

        private void SetVerticalOrientation()
        {
            // todo should automatically search for location of axes as they can be 
            // not only from the left or bottom.
            Grid.SetColumn(this, 0);
            Grid.SetRow(this, 1);
        }

        private Orientation orientation = Orientation.Horizontal;
        /// <summary>
        /// Gets or sets the orientation of AxisNavigation.
        /// </summary>
        /// <value>The orientation.</value>
        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                if (orientation != value) {
                    orientation = value;
                    OnOrientationChanged();
                }
            }
        }

        private void OnOrientationChanged()
        {
            switch (orientation) {
                case Orientation.Horizontal:
                    SetHorizontalOrientation();
                    break;
                case Orientation.Vertical:
                    SetVerticalOrientation();
                    break;
                default:
                    break;
            }
        }

        private bool lmbPressed = false;
        private Point dragStart;
        private Point dragLastScreen; // bcz: added
        private bool axisScaling = false; // bcz: added

        public bool AxisScaling
        {   // bcz: added
            get { return axisScaling; }
            set { axisScaling = value; }
        }

        private CoordinateTransform Transform
        {
            get { return Plotter2D.Viewport.Transform; }
        }

        protected override Panel HostPanel
        {
            get { return Plotter2D.MainGrid; }
        }

        private readonly Panel content = new Canvas { Background = Brushes.Transparent };
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Color.FromRgb(255, 228, 209)).MakeTransparent(0.2);
        bool nearOrigin = false; // bcz: added
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            dragStart = e.GetPosition(Plotter2D.Viewport).ScreenToViewport(Transform); // bcz: changed
            dragLastScreen = e.GetPosition(this); // bcz: added
            nearOrigin = Math.Abs(dragStart.X / Plotter2D.Viewport.Visible.Width) < .1 && Math.Abs(dragStart.Y / Plotter2D.Viewport.Visible.Height) < .1;
            if (nearOrigin)
                dragStart = new Point(0,0);
               
            lmbPressed = true;

            content.Background = fillBrush;
            Cursor = orientation == Orientation.Horizontal ? Cursors.ScrollWE : Cursors.ScrollNS;

            CaptureMouse();
            e.Handled = true; // bcz: added
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            lmbPressed = false;

            ClearValue(CursorProperty);
            content.Background = Brushes.Transparent;

            ReleaseMouseCapture();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (lmbPressed) {
                Point mousePos = e.GetPosition(this).ScreenToViewport(Transform);

                Rect visible = Plotter2D.Viewport.Visible;
                Point dragLast = dragLastScreen.ScreenToViewport(Transform);
                double delta;
                if (nearOrigin) { // bcz: added this if condition & contents
                    delta = (mousePos - dragLast).X;
                    double relPos = (dragStart.X - visible.Left) / visible.Width;
                    double newLeft = visible.Left + relPos * delta * 5;
                    double newRight = visible.Right *(newLeft /visible.Left);
                    double newTop = visible.Top * (newLeft/visible.Left);
                    double newBot = visible.Bottom * (newLeft/visible.Left);
                    if (newRight > newLeft && newBot > newTop)
                        visible = new Rect(newLeft, newTop, newRight - newLeft, newBot-newTop);
                    dragLastScreen = e.GetPosition(this);
                } else  if (orientation == Orientation.Horizontal) {
                    if (AxisScaling) {
                        delta = (mousePos - dragLast).X;
                        double relPos = (dragStart.X - visible.Left) / visible.Width;
                        double newLeft = visible.Left + relPos * delta * 5;
                        double newRight = visible.Right - (1 - relPos) * delta * 5;
                        if (newRight > newLeft)
                            visible = new Rect(newLeft, visible.Top, newRight - newLeft, visible.Height);
                        dragLastScreen = e.GetPosition(this);
                    }
                    else {
                        delta = (mousePos - dragStart).X;
                        visible.X -= delta;
                    }
                } else if (Orientation == Orientation.Vertical) {
                    if (AxisScaling) {
                        delta = (mousePos - dragLast).Y;
                        double relPos = (dragStart.Y - visible.Top) / visible.Height;
                        double newTop = visible.Top + relPos * delta * 5;
                        double newBot = visible.Bottom - (1 - relPos) * delta * 5;
                        if (newBot > newTop)
                            visible = new Rect(visible.Left, newTop, visible.Width, newBot - newTop);
                        dragLastScreen = e.GetPosition(this);
                    }
                    else {
                        delta = (mousePos - dragStart).Y;
                        visible.Y -= delta;
                    }
                }
                Plotter2D.Viewport.Visible = visible;
            }
        }

        private const double wheelZoomSpeed = 1.2;
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            Point mousePos = e.GetPosition(this);
            int delta = -e.Delta;

            Point zoomTo = mousePos.ScreenToViewport(Transform);

            double zoomSpeed = Math.Abs(delta / Mouse.MouseWheelDeltaForOneLine);
            zoomSpeed *= wheelZoomSpeed;
            if (delta < 0) {
                zoomSpeed = 1 / zoomSpeed;
            }

            Rect visible = Plotter2D.Viewport.Visible.Zoom(zoomTo, zoomSpeed);
            Rect oldVisible = Plotter2D.Viewport.Visible;
            if (orientation == Orientation.Horizontal) {
                visible.Y = oldVisible.Y;
                visible.Height = oldVisible.Height;
            }
            else {
                visible.X = oldVisible.X;
                visible.Width = oldVisible.Width;
            }
            Plotter2D.Viewport.Visible = visible;

            e.Handled = true;
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            ClearValue(CursorProperty);
            content.Background = Brushes.Transparent;

            ReleaseMouseCapture();
        }

        private bool _axisTiedToOrigin = false; // bcz: added this variable
        public bool AxisTiedToOrigin { get { return _axisTiedToOrigin; } set { _axisTiedToOrigin = value; } }
        protected override void OnViewportPropertyChanged(ExtendedPropertyChangedEventArgs e)
        {
            if (AxisTiedToOrigin) {
                Viewport2D viewport = Plotter2D.Viewport;
                if (Orientation == Orientation.Vertical) {
                    double min = viewport.Visible.Left;
                    double max = viewport.Visible.Right;
                    if (max > 0 && min < 0) {
                        double xorigin = (0 - min) / (max - min) * viewport.Output.Width;
                        RenderTransform = new TranslateTransform(xorigin, 0);
                    }
                    if (min > 0)
                        RenderTransform = new TranslateTransform(0, 0);
                    if (max < 0)
                        RenderTransform = new TranslateTransform(viewport.Output.Width + ActualWidth, 0);
                }
                if (Orientation == Orientation.Horizontal) {
                    double min = viewport.Visible.Bottom;
                    double max = viewport.Visible.Top;
                    if (max < 0 && min > 0) {
                        double yorigin = (0 - min) / (max - min) * viewport.Output.Height;
                        RenderTransform = new TranslateTransform(0, -viewport.Output.Height + yorigin);
                    }
                    if (max > 0)
                        RenderTransform = new TranslateTransform(0, 0);
                    if (min < 0)
                        RenderTransform = new TranslateTransform(0, -viewport.Output.Height - ActualHeight);
                }
            }
        }
    }
}
