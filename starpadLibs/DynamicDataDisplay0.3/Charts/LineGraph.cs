using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.Filters;
using Microsoft.Research.DynamicDataDisplay.Charts;
using System.Collections.Specialized;


namespace Microsoft.Research.DynamicDataDisplay
{
	/// <summary>Series of points connected by one polyline</summary>
	public class LineGraph : PointsGraphBase
	{
		/// <summary>Filters applied to points before rendering</summary>
		private readonly FilterCollection filters = new FilterCollection();

		/// <summary>
		/// Initializes a new instance of the <see cref="LineGraph"/> class.
		/// </summary>
		public LineGraph()
		{
			Legend.SetVisibleInLegend(this, true);
			ManualTranslate = true;

			filters.CollectionChanged += filters_CollectionChanged;
		}

		void filters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			filteredPoints = null;
			Update();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LineGraph"/> class.
		/// </summary>
		/// <param name="pointSource">The point source.</param>
		public LineGraph(IPointDataSource pointSource)
			: this()
		{
			DataSource = pointSource;
		}

		protected override Description CreateDefaultDescription()
		{
			return new PenDescription();
		}

		/// <summary>Provides access to filters collection</summary>
		public FilterCollection Filters
		{
			get { return filters; }
		}

		#region Pen

		/// <summary>
		/// Gets or sets the brush, using which polyline is plotted.
		/// </summary>
		/// <value>The line brush.</value>
		public Brush Stroke
		{
			get { return LinePen.Brush; }
			set
			{
				if (LinePen.Brush != value)
				{
					if (!LinePen.IsSealed)
					{
						LinePen.Brush = value;
						InvalidateVisual();
					}
					else
					{
						Pen pen = LinePen.Clone();
						pen.Brush = value;
						LinePen = pen;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the line thickness.
		/// </summary>
		/// <value>The line thickness.</value>
		public double StrokeThickness
		{
			get { return LinePen.Thickness; }
			set
			{
				if (LinePen.Thickness != value)
				{
					if (!LinePen.IsSealed)
					{
						LinePen.Thickness = value; InvalidateVisual();
					}
					else
					{
						Pen pen = LinePen.Clone();
						pen.Thickness = value;
						LinePen = pen;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the line pen.
		/// </summary>
		/// <value>The line pen.</value>
		[NotNull]
		public Pen LinePen
		{
			get { return (Pen)GetValue(LinePenProperty); }
			set { SetValue(LinePenProperty, value); }
		}

		public static readonly DependencyProperty LinePenProperty =
			DependencyProperty.Register(
			"LinePen",
			typeof(Pen),
			typeof(LineGraph),
			new FrameworkPropertyMetadata(
				new Pen(Brushes.Blue, 1),
				FrameworkPropertyMetadataOptions.AffectsRender
				),
			OnValidatePen);

		private static bool OnValidatePen(object value)
		{
			return value != null;
		}

		#endregion

		protected override void OnOutputChanged(Rect newRect, Rect oldRect)
		{
			filteredPoints = null;

			base.OnOutputChanged(newRect, oldRect);
		}

		protected override void OnDataChanged()
		{
			filteredPoints = null;

			base.OnDataChanged();
		}

		protected override void OnVisibleChanged(Rect newRect, Rect oldRect)
		{
			if (newRect != oldRect)  // bcz: changed
			{
				filteredPoints = null;
			}

			base.OnVisibleChanged(newRect, oldRect);
		}

		private FakePointList filteredPoints;
        protected override void UpdateCore() {
            if (DataSource == null) return;

            Rect output = Viewport.Output;
            var transform = GetTransform();

            if (filteredPoints == null || !(transform.DataTransform is IdentityTransform)) {
                IEnumerable<Point> points = GetPoints();

                ContentBounds = BoundsHelper.GetViewportBounds(points, transform.DataTransform);

                transform = GetTransform();
                List<Point> transformedPoints = transform.DataToScreen(points);

                // Analysis and filtering of unnecessary points
                filteredPoints = new FakePointList(FilterPoints(transformedPoints),
                    output.Left, output.Right);

                Offset = new Vector();
            }
            else {
                double left = output.Left;
                double right = output.Right;
                double shift = Offset.X;
                left -= shift;
                right -= shift;

                filteredPoints.SetXBorders(left, right);
            }
            //bcz: moved from OnRenderCore
            geometry = new StreamGeometry();
            if (filteredPoints.HasPoints) {
            using (StreamGeometryContext context = geometry.Open()) {
                Point figStart = filteredPoints.StartPoint; // note: startPoint != filteredPoints[0]
                int   figStartInd = 0;
                // bcz: hack! otherwise polylines may get clipped if the first point is way out of bounds
                context.BeginFigure(new Point(20, 20), false, false);
                for (int i = 0; i < filteredPoints.Count; i++) // bcz: added 
                    if (CoordinateTransform.IsDiscontinuity(filteredPoints[i].Y) ||
                        double.IsNaN(filteredPoints[i].Y) ||
                        i == filteredPoints.Count - 1 ||
                        !output.Contains(filteredPoints[i]))
                    {
                        List<Point> segment = new List<Point>();   // add between figStart and previous point
                        for (int j = figStartInd; j < i; j++) 
                            segment.Add(filteredPoints[j]);
                        // only add current point if it is continuous with previous points
                        if ((filteredPoints[i].Y == CoordinateTransform.NumToPosInfinityDiscontinuity || 
                            filteredPoints[i].Y == CoordinateTransform.NumToNegInfinityDiscontinuity) &&
                            !double.IsNaN(filteredPoints[i].Y))
                            // NOTE: clamp() function should be fixed to be a clip() function
                            segment.Add(clip(segment[segment.Count-1], CoordinateTransform.DiscontinuityToPoint( filteredPoints[i], true), output));
                        else if (!double.IsNaN(filteredPoints[i].Y) && !CoordinateTransform.IsDiscontinuity(filteredPoints[i].Y) && !output.Contains(filteredPoints[i]))
                            if (segment.Count > 0)
                            {
                                int index = System.Math.Min(i + 1, filteredPoints.Count - 1); // use first point outside bounds (shd also check for NAN & discont...)
                                segment.Add(clip(segment[segment.Count - 1], filteredPoints[index], output));
                            }

                        if (segment.Count == 0) {
                            figStartInd = i + 1;
                            figStart = filteredPoints[i];
                            continue;
                        }
                        if (double.IsNaN(figStart.Y) || CoordinateTransform.IsDiscontinuity(figStart.Y))
                            figStart = segment[0];
                        
                        if ( CoordinateTransform.IsDiscontinuity(figStart.Y) )
                            // NOTE: clamp() function should be fixed to be a clip() function
                            figStart = clip(segment[0], CoordinateTransform.DiscontinuityToPoint(figStart, false), output);
                        else
                            // NOTE: clamp() function should be fixed to be a clip() function
                            figStart = clip(segment[0], figStart, output);

                        context.LineTo(figStart, false, false);
                        context.PolyLineTo(segment, true, true);

                        // figure out where to start the next segment
                        while (i < filteredPoints.Count && 
                                 (double.IsNaN(filteredPoints[i].Y) ||
                                  CoordinateTransform.IsDiscontinuity(filteredPoints[i].Y) || 
                                  !output.Contains(filteredPoints[System.Math.Min(i+1, filteredPoints.Count-1)])))
                            i++;
                        if (i >= filteredPoints.Count - 1)
                            break;
                        figStartInd = i+1;
                        figStart = filteredPoints[i];
                    }
            }
            } // if (filteredPoints.hasPoints)
            geometry.Freeze();
        }
        //bcz: Incomplete -- should be a clip() function -- this needs to compute the intersection point between the line from the 
        // previous (or next) point to the clamped point and the bounds.  
        Point clamp(Point p, Rect bounds) {
            return new Point(p.X, System.Math.Min(bounds.Bottom, System.Math.Max(bounds.Top, p.Y)));
        }
        bool is_between(double val, double v1, double v2)
        {
            if (v1 <= val)
                return v1 < val && val < v2 || val == v1 || val == v2;
            else
                return v2 < val && val < v1 || val == v1 || val == v2;
        }
        // this clip function is special cased for the case where one point is inside bounds and one point is outside bounds.
        // p1 = point inside bounds
        // p2 = point outside bounds
        Point clip(Point p1, Point p2, Rect bounds)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;

            Point result = clamp(p2, bounds);

            if (result.Y == bounds.Top || result.Y == bounds.Bottom)
            {
                double d = result.Y - p2.Y;
                if (dy != 0)
                    result.X += d * dx / dy;
            }
            else
            {
                double d = result.X - p2.X;
                if (dx != 0)
                    result.Y += d * dy / dx;
            }

            return result;
        }
        Point clip2(Point p1, Point p2, Rect bounds)
        {
            Point result = new Point(p2.X, p2.Y);

            double dy = p2.Y - p1.Y;
            double dx = p2.X - p1.X;
            if (dy != 0)
            {
                if (is_between(bounds.Top, p1.Y, p2.Y))
                {
                    // intersect with top of bounds
                    double x = (bounds.Top - p1.Y) * dx / dy + p1.X;
                    if (bounds.Left <= x && x <= bounds.Right)
                    {
                        result.X = x;
                        result.Y = bounds.Top;
                    }
                }
                else if (is_between(bounds.Bottom, p1.Y, p2.Y))
                {
                    // intersect with bottom of bounds
                    double x = (bounds.Bottom - p1.Y) * dx / dy + p1.X;
                    if (bounds.Left <= x && x <= bounds.Right)
                    {
                        result.X = x;
                        result.Y = bounds.Bottom;
                    }
                }
            }
            if (dx != 0)
            {
                if (is_between(bounds.Left, p1.X, p2.X))
                {
                    // intersect with left of bounds
                    double y = (bounds.Left - p1.X) * dy / dx + p1.Y;
                    if (bounds.Top <= y && y <= bounds.Bottom)
                    {
                        result.X = bounds.Left;
                        result.Y = y;
                    }
                }
                else if (is_between(bounds.Right, p1.X, p2.X))
                {
                    // intersect with right of bounds
                    double y = (bounds.Right - p1.X) * dy / dx + p1.Y;
                    if (bounds.Top <= y && y <= bounds.Bottom)
                    {
                        result.X = bounds.Right;
                        result.Y = y;
                    }
                }
            }
            return result;
        }
        StreamGeometry geometry;// bcz: added

		protected override void OnRenderCore(DrawingContext dc, RenderState state)
		{
			if (DataSource == null) return;

			if (filteredPoints.HasPoints)
			{

				const Brush brush = null;
				Pen pen = LinePen;


				bool isTranslated = IsTranslated;
				if (isTranslated)
				{
					dc.PushTransform(new TranslateTransform(Offset.X, Offset.Y));
				}
				dc.DrawGeometry(brush, pen, geometry);
				if (isTranslated)
				{
					dc.Pop();
				}

#if __DEBUG
				FormattedText text = new FormattedText(filteredPoints.Count.ToString(),
					CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
					new Typeface("Arial"), 12, Brushes.Black);
				dc.DrawText(text, Viewport.Output.GetCenter());
#endif
			}
		}

		private bool filteringEnabled = true;
		public bool FilteringEnabled
		{
			get { return filteringEnabled; }
			set
			{
				if (filteringEnabled != value)
				{
					filteringEnabled = value;
					filteredPoints = null;
					Update();
				}
			}
		}

		private List<Point> FilterPoints(List<Point> points)
		{
			if (!filteringEnabled)
				return points;

			var filteredPoints = filters.Filter(points, Viewport.Output);

			return filteredPoints;
		}
	}
}
