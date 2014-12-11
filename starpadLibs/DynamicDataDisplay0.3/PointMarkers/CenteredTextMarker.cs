using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Research.DynamicDataDisplay.PointMarkers
{
    /// <summary>Renders specified text near the point</summary>
	public class CenteredTextMarker : PointMarker {
		public string Text {
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
        }

        public double FontSize {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public Typeface Type { get; set; }

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
			  "Text",
			  typeof(string),
			  typeof(CenteredTextMarker),
              new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
              "FontSize",
              typeof(double),
              typeof(CenteredTextMarker),
              new FrameworkPropertyMetadata(12.0));

		public override void Render(DrawingContext dc, Point screenPoint) {
            if (Type == null)
                Type = new Typeface("Arial");
			FormattedText textToDraw = new FormattedText(Text, Thread.CurrentThread.CurrentCulture,
				 FlowDirection.LeftToRight, Type, FontSize, Brushes.Black);

			double width = textToDraw.Width;
			double height = textToDraw.Height;

			const double verticalShift = -20; // px

			Rect bounds = RectExtensions.FromCenterSize(new Point(screenPoint.X, screenPoint.Y + verticalShift - height / 2),
				new Size(width, height));

			Point loc = bounds.Location;
			bounds = CoordinateUtilities.RectZoom(bounds, 1.05, 1.15);

			dc.DrawLine(new Pen(Brushes.Black, 1), Point.Add(screenPoint, new Vector(0, verticalShift)), screenPoint);
			dc.DrawRoundedRectangle(Brushes.White, new Pen(Brushes.Black, 1), bounds,5,5);
			dc.DrawText(textToDraw, loc);
        }
        public override Geometry Geometry(GeometryDrawing drawing, Point screenPoint) { // bcz: added
            return null;
        }
	}
}
