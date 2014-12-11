using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Input;

namespace Microsoft.Research.DynamicDataDisplay.PointMarkers {
    /// <summary>Adds Circle element at every point of graph</summary>
    public class CircleElementPointMarker : ShapeElementPointMarker {

        public EventHandler<MouseButtonEventArgs> MarkerClick;

        public override UIElement CreateMarker() {
            Ellipse result = new Ellipse();
            result.Width = Size;
            result.Height = Size;
            result.Stroke = Brush;
            result.Fill = Fill;

            result.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(result_MouseLeftButtonDown);

            if (!String.IsNullOrEmpty(ToolTipText)) {
                ToolTip tt = new ToolTip();
                tt.Content = ToolTipText;
                result.ToolTip = tt;
            }
            return marker=result;
        }
        UIElement marker;
        void result_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (MarkerClick != null) {
                MarkerClick(this, e);
            }
        }

        public Point GetPosition() {
            return new Point(
                Canvas.GetLeft(marker) + Size / 2,
                Canvas.GetTop(marker) + Size / 2);
        }
        public override void SetPosition(UIElement marker, Point screenPoint)
        {
            Canvas.SetLeft(marker, screenPoint.X - Size / 2);
            Canvas.SetTop(marker, screenPoint.Y - Size / 2);
        }
	}
}
