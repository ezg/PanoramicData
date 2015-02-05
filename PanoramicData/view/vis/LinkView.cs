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
using PanoramicData.model.data;
using PanoramicData.model.view_new;
using PanoramicData.utils;
using PanoramicData.view.inq;

namespace PanoramicData.view.vis
{
    public class LinkView : UserControl, IScribbable
    {
        private List<IDisposable> _sourceDisposables = new List<IDisposable>();
        private IDisposable _destinationDisposable = null;
        private double _attachmentRectHalfSize = 15;
        private Dictionary<VisualizationViewModel, IGeometry> _visualizationViewModelGeometries = new Dictionary<VisualizationViewModel, IGeometry>();
        private IGeometry _linkViewGeometry = null;
        private Dictionary<VisualizationViewModel, IGeometry> _visualizationViewModelCenterGeometries = new Dictionary<VisualizationViewModel, IGeometry>();
        private Dictionary<VisualizationViewModel, IGeometry> _visualizationViewModelIconGeometries = new Dictionary<VisualizationViewModel, IGeometry>();
        //private Dictionary<FilteringType, Vec> _attachmentCenters = new Dictionary<FilteringType, Vec>(); 
        
        public LinkView()
        {
            this.DataContextChanged += LinkView_DataContextChanged;
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(LinkView_TouchDownEvent));
        }

        void LinkView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                (e.OldValue as LinkViewModel).LinkModels.CollectionChanged -= LinkModels_CollectionChanged;
                (e.OldValue as LinkViewModel).FromVisualizationViewModels.CollectionChanged -= FromVisualizationViewModels_CollectionChanged;
                (e.OldValue as LinkViewModel).ToVisualizationViewModel.PropertyChanged -= ToVisualizationViewModel_PropertyChanged;
                (e.OldValue as LinkViewModel).ToVisualizationViewModel.QueryModel.PropertyChanged -= QueryModel_PropertyChanged;
            }
            if (e.NewValue != null)
            {
                (e.NewValue as LinkViewModel).LinkModels.CollectionChanged += LinkModels_CollectionChanged;
                (e.NewValue as LinkViewModel).FromVisualizationViewModels.CollectionChanged += FromVisualizationViewModels_CollectionChanged;
                (e.NewValue as LinkViewModel).ToVisualizationViewModel.PropertyChanged += ToVisualizationViewModel_PropertyChanged;
                (e.NewValue as LinkViewModel).ToVisualizationViewModel.QueryModel.PropertyChanged += QueryModel_PropertyChanged;
                updateRendering();
            }
        }

        void QueryModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateRendering();
        }

        void FromVisualizationViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    (item as VisualizationViewModel).PropertyChanged -= FromVisualizationViewModel_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    (item as VisualizationViewModel).PropertyChanged += FromVisualizationViewModel_PropertyChanged;
                }
            }
            updateRendering();
        }

        void ToVisualizationViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateRendering();
        }

        void FromVisualizationViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateRendering();
        }

        void LinkModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    (item as LinkModel).PropertyChanged -= LinkView_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    (item as LinkModel).PropertyChanged += LinkView_PropertyChanged;
                }
            }
            updateRendering();
        }

        void LinkView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateRendering();
        }

        private void updateRendering()
        {
            Canvas c = new Canvas();
            LinkViewModel linkViewModel = (DataContext as LinkViewModel);
            if (linkViewModel.FromVisualizationViewModels.Count > 0)
            {
                _visualizationViewModelGeometries.Clear();
                _visualizationViewModelCenterGeometries.Clear();
                _visualizationViewModelIconGeometries.Clear();

                Vec attachmentCenter = updateAttachmentCenter(LinkType.Filter, c);
                drawLinesFromModelsToAttachmentCenter(LinkType.Filter, attachmentCenter, c);
                if (linkViewModel.LinkModels.Any(lm => lm.LinkType == LinkType.Filter))
                {
                    drawFilterAttachment(attachmentCenter, c);
                }

                attachmentCenter = updateAttachmentCenter(LinkType.Brush, c);
                drawLinesFromModelsToAttachmentCenter(LinkType.Brush, attachmentCenter, c);
                if (linkViewModel.LinkModels.Any(lm => lm.LinkType == LinkType.Brush))
                {
                    drawBrushAttachment(attachmentCenter, c);
                }
            }
            Content = c;
        }

        private Vec updateAttachmentCenter(LinkType linkType, Canvas canvas)
        {
            LinkViewModel linkViewModel = (DataContext as LinkViewModel);

            Rct destinationRct = new Rct(linkViewModel.ToVisualizationViewModel.Position, 
                new Vec(linkViewModel.ToVisualizationViewModel.Size.X, linkViewModel.ToVisualizationViewModel.Size.Y));
            var destinationGeom = destinationRct.GetLineString();

            List<Pt> midPoints = new List<Pt>();
            int sourceCount = 0;
            foreach (var from in linkViewModel.FromVisualizationViewModels.Where(
                vvm => linkViewModel.LinkModels.Where(lvm => lvm.LinkType == linkType).Select(lvm => lvm.FromQueryModel).Contains(vvm.QueryModel)))
            {
                sourceCount++;
                var fromCenterToCenter = new Point[] { 
                    linkViewModel.ToVisualizationViewModel.Position + linkViewModel.ToVisualizationViewModel.Size / 2.0, 
                    from.Position + from.Size / 2.0 }.GetLineString();
                var sourceRct = new Rct(from.Position,
                    new Vec(from.Size.X, from.Size.Y)).GetLineString();
                var interPtSource = sourceRct.Intersection(fromCenterToCenter);
                var interPtDestination = destinationGeom.Intersection(fromCenterToCenter);

                Vector2 midPoint = new Vector2();
                if (interPtDestination.IsEmpty || interPtSource.IsEmpty)
                {
                    midPoint = (
                        (linkViewModel.ToVisualizationViewModel.Position + linkViewModel.ToVisualizationViewModel.Size / 2.0) + 
                        (from.Position + from.Size / 2.0)) / 2.0;
                }
                else
                {
                    midPoint = (new Vector2(interPtSource.Centroid.X, interPtSource.Centroid.Y) +
                                new Vector2(interPtDestination.Centroid.X, interPtDestination.Centroid.Y)) / 2.0;
                }
                midPoints.Add(new Pt(midPoint.X, midPoint.Y));
            }

            if (sourceCount == 0)
            {
                if (linkType == LinkType.Brush)
                {
                    return new Vec(
                        destinationRct.Left + _attachmentRectHalfSize,
                        destinationRct.Bottom + _attachmentRectHalfSize - 2);
                }
                else if (linkType == LinkType.Filter)
                {
                    return new Vec(
                        destinationRct.Right - _attachmentRectHalfSize,
                        destinationRct.Bottom + _attachmentRectHalfSize - 2);
                }
            }

            Vec tempAttachment = midPoints.Aggregate((p1, p2) => p1 + p2).GetVec() / (double)midPoints.Count;
            Vec destinationVec = new Vec(
                (linkViewModel.ToVisualizationViewModel.Position + linkViewModel.ToVisualizationViewModel.Size / 2.0).X,
                (linkViewModel.ToVisualizationViewModel.Position + linkViewModel.ToVisualizationViewModel.Size / 2.0).Y);
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

        private void drawLinesFromModelsToAttachmentCenter(LinkType linkType, Vec attachmentCenter, Canvas c)
        {
            LinkViewModel linkViewModel = (DataContext as LinkViewModel);
            foreach (var incomingModel in linkViewModel.FromVisualizationViewModels.Where(
                vvm => linkViewModel.LinkModels.Where(lvm => lvm.LinkType == linkType).Select(lvm => lvm.FromQueryModel).Contains(vvm.QueryModel)))
            {
                Vec incomingCenter = new Vector(
                    (incomingModel.Position + incomingModel.Size / 2.0).X,
                    (incomingModel.Position + incomingModel.Size / 2.0).Y);
                var incomingRct = new Rct(incomingModel.Position,
                    new Vec(incomingModel.Size.X, incomingModel.Size.Y));

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
                    if (linkViewModel.LinkModels.Where(lm => lm.IsInverted).Select(lm => lm.FromQueryModel).Contains(incomingModel.QueryModel))
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

                    _visualizationViewModelCenterGeometries.Add(incomingModel,
                        poly.Points.GetPolygon().Buffer(3));
                    _visualizationViewModelGeometries.Add(incomingModel,
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

                    if (linkType == LinkType.Brush)
                    {
                        Canvas pathCanvas = new Canvas();
                        var p1 = new Path();
                        p1.Fill = Brushes.Gray;
                        p1.Data =
                            System.Windows.Media.Geometry.Parse(
                                "m 0,0 c 0.426,0 0.772,-0.346 0.772,-0.773 0,-0.426 -0.346,-0.772 -0.772,-0.772 -0.427,0 -0.773,0.346 -0.773,0.772 C -0.773,-0.346 -0.427,0 0,0 m -9.319,11.674 c 0,0 7.188,0.868 7.188,-7.187 l 0,-5.26 c 0,-1.618 1.175,-1.888 2.131,-1.888 0,0 1.914,-0.245 1.871,1.87 l 0,5.246 c 0,0 0.214,7.219 7.446,7.219 l 0,2.21 -18.636,0 0,-2.21 z");
                        var tg = new TransformGroup();
                        tg.Children.Add(new ScaleTransform(1, -1));
                        tg.Children.Add(new TranslateTransform(9.3, 26));
                        p1.RenderTransform = tg;
                        pathCanvas.Children.Add(p1);

                        var p2 = new Path();
                        p2.Fill = Brushes.Gray;
                        p2.Data =
                            System.Windows.Media.Geometry.Parse("m 0,0 0,-0.491 0,-4.316 18.636,0 0,3.58 0,1.227 0,5.333 L 0,5.333 0,0 z");
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
                    else if (linkType == LinkType.Filter)
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
                    _visualizationViewModelIconGeometries.Add(incomingModel,
                        rr.GetPolygon());
                }

            }
        }

        private void drawFilterAttachment(Vec attachmentCenter, Canvas c)
        {
            LinkViewModel linkViewModel = (DataContext as LinkViewModel);
            int sourceCount = linkViewModel.LinkModels.Count(lvm => lvm.LinkType == LinkType.Filter);

            Rectangle c1 = new Rectangle();
            c1.Width = _attachmentRectHalfSize * 2;
            c1.Height = _attachmentRectHalfSize * 2;

            c1.RenderTransform = new TranslateTransform(
                attachmentCenter.X - _attachmentRectHalfSize,
                attachmentCenter.Y - _attachmentRectHalfSize);
            c.Children.Add(c1);

            c1.Fill = Brushes.White;
            c1.Stroke = linkViewModel.ToVisualizationViewModel.Brush;
            c1.StrokeThickness = 2;

            Polygon filterIcon = new Polygon();
            filterIcon.Points = new PointCollection();
            filterIcon.Points.Add(new Point(0, 0));
            filterIcon.Points.Add(new Point(15, 0));
            filterIcon.Points.Add(new Point(10, 6));
            filterIcon.Points.Add(new Point(10, 14));
            filterIcon.Points.Add(new Point(5, 12));
            filterIcon.Points.Add(new Point(5, 6));
            filterIcon.Fill = sourceCount > 1 ?  linkViewModel.ToVisualizationViewModel.FaintBrush : linkViewModel.ToVisualizationViewModel.Brush;
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
                if (linkViewModel.ToVisualizationViewModel.QueryModel.FilteringOperation == FilteringOperation.AND)
                {
                    label.Content = "AND";
                }
                else if (linkViewModel.ToVisualizationViewModel.QueryModel.FilteringOperation == FilteringOperation.OR)
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
            _linkViewGeometry = r.GetPolygon().Buffer(3);
        }

        private void drawBrushAttachment(Vec attachmentCenter, Canvas c)
        {
            LinkViewModel linkViewModel = (DataContext as LinkViewModel);
            int sourceCount = linkViewModel.LinkModels.Count(lvm => lvm.LinkType == LinkType.Brush);

            Rectangle c1 = new Rectangle();
            c1.Width = _attachmentRectHalfSize * 2;
            c1.Height = _attachmentRectHalfSize * 2;

            c1.RenderTransform = new TranslateTransform(
                attachmentCenter.X - _attachmentRectHalfSize,
                attachmentCenter.Y - _attachmentRectHalfSize);
            c.Children.Add(c1);

            c1.Fill = Brushes.White;
            c1.Stroke = linkViewModel.ToVisualizationViewModel.Brush;
            c1.StrokeThickness = 2;

            Canvas brushCanvas = new Canvas();
            var p1 = new Path();
            p1.Fill = linkViewModel.ToVisualizationViewModel.Brush;
            p1.Data = System.Windows.Media.Geometry.Parse("m 0,0 c 0.426,0 0.772,-0.346 0.772,-0.773 0,-0.426 -0.346,-0.772 -0.772,-0.772 -0.427,0 -0.773,0.346 -0.773,0.772 C -0.773,-0.346 -0.427,0 0,0 m -9.319,11.674 c 0,0 7.188,0.868 7.188,-7.187 l 0,-5.26 c 0,-1.618 1.175,-1.888 2.131,-1.888 0,0 1.914,-0.245 1.871,1.87 l 0,5.246 c 0,0 0.214,7.219 7.446,7.219 l 0,2.21 -18.636,0 0,-2.21 z");
            var tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(1, -1));
            tg.Children.Add(new TranslateTransform(9.3, 26));
            p1.RenderTransform = tg;
            brushCanvas.Children.Add(p1);

            var p2 = new Path();
            p2.Fill = linkViewModel.ToVisualizationViewModel.Brush;
            p2.Data = System.Windows.Media.Geometry.Parse("m 0,0 0,-0.491 0,-4.316 18.636,0 0,3.58 0,1.227 0,5.333 L 0,5.333 0,0 z");
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

            Vec t = (attachmentCenter - new Vec(_attachmentRectHalfSize, _attachmentRectHalfSize));
            Rct r = new Rct(new Pt(t.X, t.Y),
                new Vec(_attachmentRectHalfSize * 2, _attachmentRectHalfSize * 2));
            _linkViewGeometry = r.GetPolygon().Buffer(3);
        }
        
        void LinkView_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            IPoint p = e.GetTouchPoint(MainViewController.Instance.InkableScene).Position.GetVec().GetCoord().GetPoint();
            LinkViewModel linkViewModel = (DataContext as LinkViewModel);

            foreach (var visModel in _visualizationViewModelIconGeometries.Keys)
            {
                if (_visualizationViewModelIconGeometries[visModel].Buffer(3).Intersects(p))
                {
                    LinkModel linkModel = linkViewModel.LinkModels.Where(lm => lm.FromQueryModel == visModel.QueryModel).First();
                    linkModel.LinkType = linkModel.LinkType == LinkType.Brush ? LinkType.Filter : LinkType.Brush;
                  
                    e.Handled = true;
                    break;
                }
            }

            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;

            
            if (_linkViewGeometry.Intersects(p))
            {
                FilteringOperation op = linkViewModel.ToVisualizationViewModel.QueryModel.FilteringOperation;
                linkViewModel.ToVisualizationViewModel.QueryModel.FilteringOperation = op == FilteringOperation.AND ? FilteringOperation.OR : FilteringOperation.AND;
                e.Handled = true;
            }
        }
        public GeoAPI.Geometries.IGeometry Geometry
        {
            get
            {
                LinkViewModel linkViewModel = (DataContext as LinkViewModel);
                if (linkViewModel.LinkModels.Count > 0)
                {
                    IGeometry unionGeometry = _linkViewGeometry;

                    foreach (var model in _visualizationViewModelGeometries.Keys)
                    {
                        unionGeometry = unionGeometry.Union(_visualizationViewModelGeometries[model].Buffer(3));
                        unionGeometry = unionGeometry.Union(_visualizationViewModelCenterGeometries[model]);
                    }

                    /*Polyline pl = new Polyline();
                    pl.Points = new PointCollection(unionGeometry.Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
                    pl.Stroke = Brushes.Green;
                    pl.StrokeThickness = 1;
                    MainViewController.Instance.InkableScene.Add(pl);*/

                    return unionGeometry;
                }
                else
                {
                    return new NetTopologySuite.Geometries.Point(-40000, -40000);
                }
            }
        }

        public List<LinkModel> GetLinkModelsToRemove(IGeometry scribble)
        {
            LinkViewModel linkViewModel = (DataContext as LinkViewModel);
            List<LinkModel> models = new List<LinkModel>();
            if (scribble.Intersects(_linkViewGeometry.Buffer(3)))
            {
                models = linkViewModel.LinkModels.ToList();
            }
            else
            {
                foreach (var model in _visualizationViewModelGeometries.Keys)
                {
                    if (_visualizationViewModelGeometries[model].Buffer(3).Intersects(scribble))
                    {
                        models.Add(linkViewModel.LinkModels.First(lm => lm.FromQueryModel == model.QueryModel));
                    }
                }
            }
            return models;
        }
    }
}
