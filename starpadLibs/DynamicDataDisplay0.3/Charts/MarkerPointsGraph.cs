using System.Windows;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;

namespace Microsoft.Research.DynamicDataDisplay
{
    public class MarkerPointsGraph : PointsGraphBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkerPointsGraph"/> class.
        /// </summary>
        public MarkerPointsGraph() {
            ManualTranslate = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkerPointsGraph"/> class.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        public MarkerPointsGraph(IPointDataSource dataSource)
            : this() {
            DataSource = dataSource;
        }

        protected override void OnVisibleChanged(Rect newRect, Rect oldRect) {
            base.OnVisibleChanged(newRect, oldRect);
            InvalidateVisual();
        }

        public PointMarker Marker {
            get { return (PointMarker)GetValue(MarkerProperty); }
            set { SetValue(MarkerProperty, value); }
        }

        public static readonly DependencyProperty MarkerProperty =
            DependencyProperty.Register(
              "Marker",
              typeof(PointMarker),
              typeof(MarkerPointsGraph),
              new FrameworkPropertyMetadata { DefaultValue = null, AffectsRender = true }
                  );

        public bool UseVisuals = true;
        protected override void OnRenderCore(DrawingContext dc, RenderState state) {
            if (DataSource == null) return;
            if (Marker == null) return;

            if (UseVisuals) {
                OnRenderCoreVisual(dc, state);
                return;
            }

            var transform = Plotter2D.Viewport.Transform;

            Rect bounds = Rect.Empty;
            using (IPointEnumerator enumerator = DataSource.GetEnumerator(GetContext())) {
                Point point = new Point();
                while (enumerator.MoveNext()) {
                    enumerator.GetCurrent(ref point);
                    enumerator.ApplyMappings(Marker);

                    //Point screenPoint = point.Transform(state.Visible, state.Output);
                    Point screenPoint = point.DataToScreen(transform);

                    bounds = Rect.Union(bounds, point);
                    Marker.Render(dc, screenPoint);
                }
            }

            ContentBounds = bounds;
        }
        // Create a collection of child visual objects.
        private VisualCollection _children = null;
        // Provide a required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount {
            get { return _children == null ? 0 : _children.Count; }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index) {
            if (index < 0 || index >= _children.Count) {
                return null;
            }

            return _children[index];
        }
        protected void OnRenderCoreVisual(DrawingContext dc2, RenderState state) {
            if (_children == null)
                _children = new VisualCollection(this);
            var transform = Plotter2D.Viewport.Transform;
            DrawingImage dimag = new DrawingImage();
            DrawingVisual dv = new DrawingVisual();
            DrawingContext dc = dv.RenderOpen();

            Rect bounds = Rect.Empty;
            Rect screenBounds = Rect.Empty;
            dimag = new DrawingImage();
            GeometryDrawing gd = new GeometryDrawing();
            GeometryGroup gg = new GeometryGroup();
            gd.Geometry = gg;
            dimag.Drawing = gd;
            using (IPointEnumerator enumerator = DataSource.GetEnumerator(GetContext())) {
                Point point = new Point();
                while (enumerator.MoveNext()) {
                    enumerator.GetCurrent(ref point);
                    enumerator.ApplyMappings(Marker);

                    //Point screenPoint = point.Transform(state.Visible, state.Output);
                    Point screenPoint = point.DataToScreen(transform);

                    bounds = Rect.Union(bounds, point);
                    screenBounds = Rect.Union(screenBounds, screenPoint);
                    gg.Children.Add(Marker.Geometry(gd, screenPoint));
                }
            }

            dc.DrawImage(dimag, screenBounds);
            dc.Close();
            _children.Clear();
            _children.Add(dv);

            ContentBounds = bounds;
        }
    }
}
