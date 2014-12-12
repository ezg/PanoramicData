using GeoAPI.Geometries;
using PanoramicDataModel;
using starPadSDK.AppLib;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Geom;
using System.Collections.ObjectModel;
using System;
using System.Reactive.Linq;
using starPadSDK.Inq;
using starPadSDK.Inq.BobsCusps;
using PanoramicData.model.view;
using PanoramicData.utils.inq;
using CombinedInputAPI;
using System.Windows.Input;
using PanoramicData.controller.view;

namespace PanoramicData.view.filter
{
    public class FilterModelAttachment : UserControl, GeometryElement, StroqListener
    {
        private List<IDisposable> _sourceDisposables = new List<IDisposable>();
        private IDisposable _destinationDisposable = null;
        private double _attachmentRectHalfSize = 15;
        private Dictionary<FilterModel, IGeometry> _filterModelGeometries = new Dictionary<FilterModel, IGeometry>();
        private IGeometry _filterAttachmentGeometry = null;
        private Dictionary<FilterModel, IGeometry> _filterModelCenterGeometries = new Dictionary<FilterModel, IGeometry>();
        private Dictionary<FilterModel, IGeometry> _filterModelIconGeometries = new Dictionary<FilterModel, IGeometry>();
        //private Dictionary<FilteringType, Vec> _attachmentCenters = new Dictionary<FilteringType, Vec>(); 

        private ObservableCollection<FilterHolderViewModel> _sources = new ObservableCollection<FilterHolderViewModel>();
        public ObservableCollection<FilterHolderViewModel> Sources
        {
            get
            {
                return _sources;
            }
        }

        private FilterHolderViewModel _destination = null;
        public FilterHolderViewModel Destination
        {
            get
            {
                return _destination;
            }
            set
            {
                if (_destinationDisposable != null)
                {
                    _destinationDisposable.Dispose();
                }
                _destination = value;

                _destinationDisposable = Observable.FromEventPattern<FilterModelUpdatedEventArgs>(
                    _destination, "FilterModelUpdated")
                    //.Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            update();
                        }));
                    });
                update();
            }
        }
        private FilteringType _filteringType = FilteringType.Filter;

        public FilteringType FilteringType
        {
            get
            {
                return _filteringType;
            }
            set
            {
                _filteringType = value;
            }
        }

        public FilterModelAttachment(FilteringType filteringType)
        {
            _sources.CollectionChanged += _sources_CollectionChanged;
            _filteringType = filteringType;
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(TouchDownEvent));
        }

        public void CleanUp()
        {
            foreach (var d in _sourceDisposables)
            {
                d.Dispose();
            }
            if (_destinationDisposable != null)
            {
                _destinationDisposable.Dispose();
            }

            foreach (var source in _sources.ToArray())
            {
                Destination.RemoveIncomingFilter(source, _filteringType);
            }
        }

        private Vec updateAttachmentCenter(FilteringType filteringType)
        {
            Rct destinationRct = new Rct(_destination.Center - _destination.Dimension * 0.5, _destination.Dimension);
            var destinationGeom = destinationRct.GetLineString();

            List<Pt> midPoints = new List<Pt>();
            int sourceCount = 0;
            foreach (var s in _sources)
            {
                if (_destination.IsCombinedFilter && _destination.CombinedIncomingFilterModels.Contains(s))
                {
                    continue;
                }
                sourceCount++;
                var fromCenterToCenter = new Point[] { _destination.Center, s.Center }.GetLineString();
                var sourceRct = new Rct(s.Center - s.Dimension * 0.5, s.Dimension).GetLineString();

                var interPtSource = sourceRct.Intersection(fromCenterToCenter);
                var interPtDestination = destinationGeom.Intersection(fromCenterToCenter);

                Vec midPoint = new Vec();
                if (interPtDestination.IsEmpty || interPtSource.IsEmpty)
                {
                    midPoint = (_destination.Center.GetVec() + s.Center.GetVec()) / 2.0;
                }
                else
                {
                    midPoint = (new Vec(interPtSource.Centroid.X, interPtSource.Centroid.Y) +
                                new Vec(interPtDestination.Centroid.X, interPtDestination.Centroid.Y)) / 2.0;
                }
                midPoints.Add(midPoint.GetWindowsPoint());
            }

            if (sourceCount == 0)
            {
                if (filteringType == FilteringType.Brush)
                {
                    return new Vec(
                        destinationRct.Left + _attachmentRectHalfSize,
                        destinationRct.Bottom + _attachmentRectHalfSize - 2);
                }
                else if (filteringType == FilteringType.Filter)
                {
                    return new Vec(
                        destinationRct.Right - _attachmentRectHalfSize,
                        destinationRct.Bottom + _attachmentRectHalfSize - 2);
                }
            }

            Vec tempAttachment = midPoints.Aggregate((p1, p2) => p1 + p2).GetVec() / (double)midPoints.Count;
            Vec destinationVec = _destination.Center.GetVec();
            var inter = destinationGeom.Intersection(new Point[] { tempAttachment.GetCoord().GetPt(), destinationVec.GetCoord().GetPt() }.GetLineString());
            Vec attachmentCenter = new Vec();
            
            if (inter.IsEmpty)
            {
                Vec dirVec = tempAttachment - destinationVec;
                dirVec = dirVec.Normal() * 40000;
                dirVec += tempAttachment;
                inter = destinationGeom.Intersection(new Point[] { dirVec.GetCoord().GetPt(), destinationVec.GetCoord().GetPt() }.GetLineString());
                attachmentCenter = new Vec(inter.Centroid.X, inter.Centroid.Y);
            }
            else
            {
                attachmentCenter = new Vec(inter.Centroid.X, inter.Centroid.Y);
            }

            // left
            if (attachmentCenter.X <= destinationRct.Left)
            {
                attachmentCenter = new Vec(
                    attachmentCenter.X - _attachmentRectHalfSize + 2,
                    Math.Min(Math.Max(attachmentCenter.Y, destinationRct.Top + _attachmentRectHalfSize), destinationRct.Bottom - _attachmentRectHalfSize));
            }
            // right
            else if (attachmentCenter.X >= destinationRct.Right)
            {
                attachmentCenter = new Vec(
                    attachmentCenter.X + _attachmentRectHalfSize - 2,
                    Math.Min(Math.Max(attachmentCenter.Y, destinationRct.Top + _attachmentRectHalfSize), destinationRct.Bottom - _attachmentRectHalfSize));
            }
            // top
            else if (attachmentCenter.Y <= destinationRct.Top)
            {
                attachmentCenter = new Vec(
                    Math.Min(Math.Max(attachmentCenter.X, destinationRct.Left + _attachmentRectHalfSize), destinationRct.Right - _attachmentRectHalfSize),
                    attachmentCenter.Y - _attachmentRectHalfSize + 2);
            }
            // bottom
            else if (attachmentCenter.Y >= destinationRct.Bottom)
            {
                attachmentCenter = new Vec(
                    Math.Min(Math.Max(attachmentCenter.X, destinationRct.Left + _attachmentRectHalfSize), destinationRct.Right - _attachmentRectHalfSize),
                    attachmentCenter.Y + _attachmentRectHalfSize - 2);
            }

            return attachmentCenter;
        }

        private void drawLinesFromModelsToAttachmentCenter(FilteringType filteringyType, Vec attachmentCenter, Canvas c)
        {
            foreach (var incomingModel in _sources)
            {
                Vec incomingCenter = incomingModel.Center.GetVec();
                Rct incomingRct = new Rct(incomingModel.Center - incomingModel.Dimension * 0.5,
                    incomingModel.Dimension);
                var inter =
                    incomingRct.GetLineString()
                        .Intersection(
                            new Point[] { attachmentCenter.GetCoord().GetPt(), incomingCenter.GetCoord().GetPt() }
                                .GetLineString());
                Vec incomingStart = new Vec();

                if (inter.IsEmpty)
                {
                    incomingStart = incomingCenter;
                }
                else
                {
                    incomingStart = new Vec(inter.Centroid.X, inter.Centroid.Y);
                }
                Vec distanceVec = (attachmentCenter - incomingStart);
                if (distanceVec.Length > 0)
                {
                    Vec cutOff = distanceVec.Normalized() * (distanceVec.Length - _attachmentRectHalfSize);

                    Line l1 = new Line();
                    l1.X1 = incomingStart.X;
                    l1.Y1 = incomingStart.Y;
                    l1.X2 = incomingStart.X + cutOff.X;
                    l1.Y2 = incomingStart.Y + cutOff.Y;
                    if (_destination.GetInvertedIncomingFilterModels(_filteringType).Contains(incomingModel))
                    {
                        l1.StrokeDashArray = new DoubleCollection() { 2 };
                    }
                    l1.Stroke = Brushes.Gray;
                    l1.StrokeThickness = 2;
                    c.Children.Add(l1);


                    Vec n = distanceVec.Perp().Normalized();
                    Vec trianglePos = (distanceVec.Normalized() * (distanceVec.Length * 0.3)) + incomingStart;

                    Polygon poly = new Polygon();
                    poly.Points.Add(new Point(trianglePos.X + n.X * 8, trianglePos.Y + n.Y * 8));
                    poly.Points.Add(new Point(trianglePos.X - n.X * 8, trianglePos.Y - n.Y * 8));
                    poly.Points.Add(new Point(trianglePos.X + distanceVec.Normalized().X * 20,
                        trianglePos.Y + distanceVec.Normalized().Y * 20));
                    poly.Points.Add(new Point(trianglePos.X + n.X * 8, trianglePos.Y + n.Y * 8));
                    poly.Fill = Brushes.Gray;
                    c.Children.Add(poly);

                    _filterModelCenterGeometries.Add(incomingModel,
                        poly.Points.GetPolygon().Buffer(3));
                    _filterModelGeometries.Add(incomingModel,
                        new Point[] { incomingStart.GetCoord().GetPt(), (incomingStart + cutOff).GetCoord().GetPt() }
                            .GetLineString());



                    Vec iconPos = (distanceVec.Normalized() * (distanceVec.Length * 0.2)) + incomingStart;
                    Canvas brushCanvas = new Canvas();
                    Ellipse e = new Ellipse();
                    e.Width = 30;
                    e.Height = 30;
                    e.Fill = Brushes.White;
                    e.Stroke = Brushes.Gray;
                    e.StrokeThickness = 2;

                    if (filteringyType == FilteringType.Brush)
                    {
                        Canvas pathCanvas = new Canvas();
                        var p1 = new Path();
                        p1.Fill = Brushes.Gray;
                        p1.Data =
                            Geometry.Parse(
                                "m 0,0 c 0.426,0 0.772,-0.346 0.772,-0.773 0,-0.426 -0.346,-0.772 -0.772,-0.772 -0.427,0 -0.773,0.346 -0.773,0.772 C -0.773,-0.346 -0.427,0 0,0 m -9.319,11.674 c 0,0 7.188,0.868 7.188,-7.187 l 0,-5.26 c 0,-1.618 1.175,-1.888 2.131,-1.888 0,0 1.914,-0.245 1.871,1.87 l 0,5.246 c 0,0 0.214,7.219 7.446,7.219 l 0,2.21 -18.636,0 0,-2.21 z");
                        var tg = new TransformGroup();
                        tg.Children.Add(new ScaleTransform(1, -1));
                        tg.Children.Add(new TranslateTransform(9.3, 26));
                        p1.RenderTransform = tg;
                        pathCanvas.Children.Add(p1);

                        var p2 = new Path();
                        p2.Fill = Brushes.Gray;
                        p2.Data =
                            Geometry.Parse("m 0,0 0,-0.491 0,-4.316 18.636,0 0,3.58 0,1.227 0,5.333 L 0,5.333 0,0 z");
                        tg = new TransformGroup();
                        tg.Children.Add(new ScaleTransform(1, -1));
                        tg.Children.Add(new TranslateTransform(0, 6));
                        p2.RenderTransform = tg;
                        pathCanvas.Children.Add(p2);

                        tg = new TransformGroup();
                        tg.Children.Add(new ScaleTransform(0.7, 0.7));
                        tg.Children.Add(new TranslateTransform(9, 6));
                        pathCanvas.RenderTransform = tg;

                        brushCanvas.Children.Add(e);
                        brushCanvas.Children.Add(pathCanvas);
                        
                    }
                    else if (filteringyType == FilteringType.Filter)
                    {
                        Polygon filterIcon = new Polygon();
                        filterIcon.Points = new PointCollection();
                        filterIcon.Points.Add(new Point(0, 0));
                        filterIcon.Points.Add(new Point(15, 0));
                        filterIcon.Points.Add(new Point(10, 6));
                        filterIcon.Points.Add(new Point(10, 14));
                        filterIcon.Points.Add(new Point(5, 12));
                        filterIcon.Points.Add(new Point(5, 6));
                        filterIcon.Fill = Brushes.Gray;
                        filterIcon.Width = _attachmentRectHalfSize * 2;
                        filterIcon.Height = _attachmentRectHalfSize * 2;
                        Mat mat = Mat.Scale(1.2, 1.2) *
                                  Mat.Translate(6,8);
                        filterIcon.RenderTransform = new MatrixTransform(mat);

                        brushCanvas.Children.Add(e);
                        brushCanvas.Children.Add(filterIcon);
                    }
                    brushCanvas.RenderTransform = new TranslateTransform(iconPos.X - 15, iconPos.Y - 15);
                    c.Children.Add(brushCanvas);

                    Rect rr = new Rct(iconPos.X - 15, iconPos.Y - 15, iconPos.X + 15, iconPos.Y + 15);
                    _filterModelIconGeometries.Add(incomingModel,
                        rr.GetPolygon());
                }

            }
        }

        private void drawFilterAttachment(Vec attachmentCenter, Canvas c)
        {
            int sourceCount = _sources.Where(s => 
                !(_destination.IsCombinedFilter && _destination.CombinedIncomingFilterModels.Contains(s))).Count();

            Rectangle c1 = new Rectangle();
            c1.Width = _attachmentRectHalfSize * 2;
            c1.Height = _attachmentRectHalfSize * 2;

            c1.RenderTransform = new TranslateTransform(
                attachmentCenter.X - _attachmentRectHalfSize,
                attachmentCenter.Y - _attachmentRectHalfSize);
            c.Children.Add(c1);

            c1.Fill = Brushes.White;
            c1.Stroke = Destination.Brush;
            c1.StrokeThickness = 2;

            Polygon filterIcon = new Polygon();
            filterIcon.Points = new PointCollection();
            filterIcon.Points.Add(new Point(0, 0));
            filterIcon.Points.Add(new Point(15, 0));
            filterIcon.Points.Add(new Point(10, 6));
            filterIcon.Points.Add(new Point(10, 14));
            filterIcon.Points.Add(new Point(5, 12));
            filterIcon.Points.Add(new Point(5, 6));
            filterIcon.Fill = sourceCount > 1 ? Destination.FaintBrush : Destination.Brush;
            filterIcon.Width = _attachmentRectHalfSize * 2;
            filterIcon.Height = _attachmentRectHalfSize * 2;
            Mat mat = Mat.Scale(1.2, 1.2) *
                      Mat.Translate(attachmentCenter.X - _attachmentRectHalfSize + 6,
                      attachmentCenter.Y - _attachmentRectHalfSize + 7 + (sourceCount > 1 ? 0 : 0));
            filterIcon.RenderTransform = new MatrixTransform(mat);
            c.Children.Add(filterIcon);

            if (sourceCount > 1)
            {
                System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                if (_destination.GetFilterModelLinkType(_filteringType) == FilterModelLinkType.AND)
                {
                    label.Content = "AND";
                }
                else if (_destination.GetFilterModelLinkType(_filteringType) == FilterModelLinkType.OR)
                {
                    label.Content = "OR";
                }
                label.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                label.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                label.VerticalContentAlignment = VerticalAlignment.Center;
                label.FontSize = 9;
                label.FontWeight = FontWeights.Bold;
                label.Width = _attachmentRectHalfSize * 2;
                label.Height = _attachmentRectHalfSize * 2;
                c.UseLayoutRounding = false;
                label.RenderTransform = new TranslateTransform(attachmentCenter.X - _attachmentRectHalfSize,
                    attachmentCenter.Y - _attachmentRectHalfSize);
                c.Children.Add(label);
            }

            Vec t = (attachmentCenter - new Vec(_attachmentRectHalfSize, _attachmentRectHalfSize));
            Rct r = new Rct(new Pt(t.X, t.Y),
                new Vec(_attachmentRectHalfSize*2, _attachmentRectHalfSize*2));
            _filterAttachmentGeometry = r.GetPolygon().Buffer(3);
        }

        private void drawBrushAttachment(Vec attachmentCenter, Canvas c)
        {
            int sourceCount = _sources.Where(s =>
                !(_destination.IsCombinedFilter && _destination.CombinedIncomingFilterModels.Contains(s))).Count();

            Rectangle c1 = new Rectangle();
            c1.Width = _attachmentRectHalfSize * 2;
            c1.Height = _attachmentRectHalfSize * 2;

            c1.RenderTransform = new TranslateTransform(
                attachmentCenter.X - _attachmentRectHalfSize,
                attachmentCenter.Y - _attachmentRectHalfSize);
            c.Children.Add(c1);

            c1.Fill = Brushes.White;
            c1.Stroke = Destination.Brush;
            c1.StrokeThickness = 2;

            Canvas brushCanvas = new Canvas();
            var p1 = new Path();
            p1.Fill = sourceCount > 1 && false ? Destination.FaintBrush : Destination.Brush;
            p1.Data = Geometry.Parse("m 0,0 c 0.426,0 0.772,-0.346 0.772,-0.773 0,-0.426 -0.346,-0.772 -0.772,-0.772 -0.427,0 -0.773,0.346 -0.773,0.772 C -0.773,-0.346 -0.427,0 0,0 m -9.319,11.674 c 0,0 7.188,0.868 7.188,-7.187 l 0,-5.26 c 0,-1.618 1.175,-1.888 2.131,-1.888 0,0 1.914,-0.245 1.871,1.87 l 0,5.246 c 0,0 0.214,7.219 7.446,7.219 l 0,2.21 -18.636,0 0,-2.21 z");
            var tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(1, -1));
            tg.Children.Add(new TranslateTransform(9.3, 26));
            p1.RenderTransform = tg;
            brushCanvas.Children.Add(p1);

            var p2 = new Path();
            p2.Fill = sourceCount > 1 && false ? Destination.FaintBrush : Destination.Brush;
            p2.Data = Geometry.Parse("m 0,0 0,-0.491 0,-4.316 18.636,0 0,3.58 0,1.227 0,5.333 L 0,5.333 0,0 z");
            tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(1, -1));
            tg.Children.Add(new TranslateTransform(0, 6));
            p2.RenderTransform = tg;
            brushCanvas.Children.Add(p2);

            //BrushIcon brushIcon = new BrushIcon();
            //brushIcon.SetBrush(sourceCount > 1 && false ? Destination.FaintBrush : Destination.Brush);
            Matrix mat = Mat.Scale(0.80, 0.80) *
                      Mat.Rotate(new Deg(45), new Pt(10, 10)) * 
                      Mat.Translate(
                          attachmentCenter.X - _attachmentRectHalfSize + 7,
                          attachmentCenter.Y - _attachmentRectHalfSize + 7);
            brushCanvas.RenderTransform = new MatrixTransform(mat);
            c.Children.Add(brushCanvas);

            if (sourceCount > 1  && false)
            {
                System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                if (_destination.GetFilterModelLinkType(_filteringType) == FilterModelLinkType.AND)
                {
                    label.Content = "AND";
                }
                else if (_destination.GetFilterModelLinkType(_filteringType) == FilterModelLinkType.OR)
                {
                    label.Content = "OR";
                }
                label.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                label.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                label.VerticalContentAlignment = VerticalAlignment.Center;
                label.FontSize = 9;
                label.FontWeight = FontWeights.Bold;
                label.Width = _attachmentRectHalfSize * 2;
                label.Height = _attachmentRectHalfSize * 2;
                c.UseLayoutRounding = false;
                label.RenderTransform = new TranslateTransform(attachmentCenter.X - _attachmentRectHalfSize,
                    attachmentCenter.Y - _attachmentRectHalfSize);
                c.Children.Add(label);
            }

            Vec t = (attachmentCenter - new Vec(_attachmentRectHalfSize, _attachmentRectHalfSize));
            Rct r = new Rct(new Pt(t.X, t.Y),
                new Vec(_attachmentRectHalfSize * 2, _attachmentRectHalfSize * 2));
            _filterAttachmentGeometry = r.GetPolygon().Buffer(3);
        }

        private void update()
        {
            if (_destination != null)
            {
                Canvas c = new Canvas();

                if (_sources.Count > 0)
                {
                    _filterModelGeometries.Clear();
                    _filterModelCenterGeometries.Clear();
                    _filterModelIconGeometries.Clear();

                    Vec attachmentCenter = updateAttachmentCenter(_filteringType);
                    drawLinesFromModelsToAttachmentCenter(_filteringType, attachmentCenter, c);

                    if (_filteringType == FilteringType.Filter)
                    {
                        drawFilterAttachment(attachmentCenter, c);
                    }
                    else if (_filteringType == FilteringType.Brush)
                    {
                        drawBrushAttachment(attachmentCenter, c);
                    }
                }
                Content = c;
            }
        }

        void _sources_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (var d in _sourceDisposables)
            {
                d.Dispose();
            }
            
            foreach (var s in _sources)
            {
                IDisposable sourceDisposable = Observable.FromEventPattern<FilterModelUpdatedEventArgs>(
                ((FilterHolderViewModel)s), "FilterModelUpdated")
                .Throttle(TimeSpan.FromMilliseconds(0))
                .Subscribe((arg) =>
                {
                    Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        update();
                    }));
                });

                _sourceDisposables.Add(sourceDisposable);
            }
            update();
        }

        public GeoAPI.Geometries.IGeometry GetGeometry()
        {
            if (_sources.Count > 0)
            {
                IGeometry unionGeometry = _filterAttachmentGeometry;

                foreach (var model in _filterModelGeometries.Keys)
                {
                    unionGeometry = unionGeometry.Union(_filterModelGeometries[model].Buffer(3));
                    unionGeometry = unionGeometry.Union(_filterModelCenterGeometries[model]);
                }

                /*Polyline pl = new Polyline();
            pl.Points = new PointCollection(unionGeometry.Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
            pl.Stroke = Brushes.Green;
            pl.StrokeThickness = 1;
            _inqScene.AddNoUndo(pl);*/

                return unionGeometry;
            }
            else
            {
                return new NetTopologySuite.Geometries.Point(-40000,-40000);
            }
        }

        void TouchDownEvent(Object sender, TouchEventArgs e)
        {
            IPoint p = e.GetTouchPoint(MainViewController.Instance.InkableScene).Position.GetVec().GetCoord().GetPoint();

            foreach (var filterModel in _filterModelIconGeometries.Keys)
            {
                if (_filterModelIconGeometries[filterModel].Buffer(3).Intersects(p))
                {
                    _destination.RemoveIncomingFilter(filterModel, _filteringType);
                    _destination.AddIncomingFilter(filterModel,
                        _filteringType == FilteringType.Brush ? FilteringType.Filter : FilteringType.Brush);
                    e.Handled = true;
                    break;
                }
            }

            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;

            
            if (_filterAttachmentGeometry.Intersects(p))
            {
                toggleLinkType();
                e.Handled = true;
            }
        }

        void toggleLinkType()
        {
            if (_destination.GetFilterModelLinkType(_filteringType) == FilterModelLinkType.AND)
            {
                _destination.SetFilterModelLinkType(_filteringType, FilterModelLinkType.OR);
            }
            else if (_destination.GetFilterModelLinkType(_filteringType) == FilterModelLinkType.OR)
            {
                _destination.SetFilterModelLinkType(_filteringType, FilterModelLinkType.AND);
            }
            update();
        }

        public void CheckScribbleDelete(starPadSDK.Inq.Stroq s)
        {
            IGeometry stroqGeometry = s.GetLineString();
            foreach (var filterModel in _filterModelGeometries.Keys)
            {
                if (_filterModelGeometries[filterModel].Buffer(3).Intersects(stroqGeometry))
                {
                    _destination.RemoveIncomingFilter(filterModel, _filteringType);
                }
            }
            MainViewController.Instance.InkableScene.Remove(s);   
        }

        public void NotifyStroqAdded(starPadSDK.Inq.Stroq s)
        {
            IGeometry stroqGeometry = s.GetLineString();

            if (_filterAttachmentGeometry.Intersects(stroqGeometry))
            {
                toggleLinkType();
            }
            else
            {
                foreach (var filterModel in _filterModelGeometries.Keys)
                {
                    if (_filterModelGeometries[filterModel].Buffer(3).Intersects(stroqGeometry))
                    {
                        if (s.Cusps().Length == 2)
                        {
                            _destination.InvertIncomingFilterModel(filterModel, _filteringType);
                        }
                        else 
                        {
                            _destination.RemoveIncomingFilter(filterModel, _filteringType);
                            _destination.AddIncomingFilter(filterModel,
                                _filteringType == FilteringType.Brush ? FilteringType.Filter : FilteringType.Brush);
                        }
                        break;
                    }
                }
            }
            MainViewController.Instance.InkableScene.Remove(s);   
        }

        public void NotifyStroqRemoved(starPadSDK.Inq.Stroq s)
        {
        }

        public void NotifyStroqsRemoved(starPadSDK.Inq.StroqCollection sc)
        {
        }

        public void NotifyStroqsAdded(starPadSDK.Inq.StroqCollection sc)
        {
        }
    }
}
