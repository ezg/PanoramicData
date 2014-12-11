using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;

using InputFramework;
using InputFramework.WPFDevices;

/// this class displays the UI manipulation and feedback widgets for an InqScene's current Selection
namespace starPadSDK.AppLib
{
    /// <summary>
    /// Watches the Selection on the InqObjCanvas in order to display a selection boundary when objects
    /// are selected and to provide an interface for moving those objects
    /// </summary>
    public partial class SelectionFeedback
    {
        InqScene _ican = null;
        Polygon _feedbackWidget = new Polygon();
        Polygon _moveWidget = new Polygon();
        Floatie _floatie = new Floatie();
        Line _rotLine = new Line();
        Stroq _collecting = null;
        MouseEventHandler _pendingMove = null;
        RoutedPointEventHandler _pendingDrag = null;
        DateTime _downTime = DateTime.MaxValue;
        bool _moved = false;
        Mat _xform = Mat.Identity;
        Pt _selectionRelativePt;
        object _device;
        bool _useHandles = false;
        bool _allowScaleRotate = true;
        bool _immediateDrag = false;



        public bool InDragRgn(Pt where) {
            try
            {
                FrameworkElement ele = (_allowScaleRotate ? _feedbackWidget : _moveWidget);
                if (ele.ActualHeight != 0)
                    return ele.InputHitTest(where.TransformFromAtoB(_ican, ele)) != null;
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool InFloatieRgn(Pt where) {
            try
            {
                return _floatie.InputHitTest(where.TransformFromAtoB(_ican, _floatie)) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the boundary of the Selection widget based on the current SelectionObj.
        /// </summary>
        void updateBounds() {
            double zoom = (((Mat)_ican.RenderTransform.Value) * Vec.Xaxis).Length;
            // transform all selection border points into canvas coordinates (avoids scale artifacts w/ line properties)
            _feedbackWidget.Points = Selection.Outline.Points;
            Mat borderMat = Selection.Outline.RenderTransform.Value;
            Rct borderBounds = Rct.Null;
            for (int i = 0; i < _feedbackWidget.Points.Count; i++) {
                borderBounds = borderBounds.Union(_feedbackWidget.Points[i]);
                _feedbackWidget.Points[i] = borderMat * _feedbackWidget.Points[i];
            }

            Pt[] movePts = new Pt[] { new Pt(borderBounds.Left+borderBounds.Width/6, (borderBounds.Top+borderBounds.Center.Y)/2), 
                                     new Pt((borderBounds.Left+borderBounds.Center.X)/2, borderBounds.Top+borderBounds.Height/6),
                                     new Pt((borderBounds.Right+borderBounds.Center.X)/2,borderBounds.Top+borderBounds.Height/6), 
                                     new Pt(borderBounds.Right-borderBounds.Width/6, (borderBounds.Top+borderBounds.Center.Y)/2),
                                     new Pt(borderBounds.Right-borderBounds.Width/6, (borderBounds.Bottom + borderBounds.Center.Y) / 2), 
                                     new Pt((borderBounds.Right + borderBounds.Center.X)/2,borderBounds.Bottom-borderBounds.Height/6),
                                     new Pt((borderBounds.Left+borderBounds.Center.X)/2, borderBounds.Bottom-borderBounds.Height/6), 
                                     new Pt(borderBounds.Left+borderBounds.Width/6, (borderBounds.Bottom+borderBounds.Center.Y)/2),
                                     new Pt(borderBounds.Left+borderBounds.Width/6, (borderBounds.Top+borderBounds.Center.Y)/2) };
            Pt[] octPts = new Pt[] { new Pt(borderBounds.Left, (borderBounds.Top+borderBounds.Center.Y)/2), 
                                     new Pt((borderBounds.Left+borderBounds.Center.X)/2, borderBounds.Top),
                                     new Pt((borderBounds.Right + borderBounds.Center.X)/2,borderBounds.Top), 
                                     new Pt(borderBounds.Right, (borderBounds.Top+borderBounds.Center.Y)/2),
                                     new Pt(borderBounds.Right, (borderBounds.Bottom + borderBounds.Center.Y) / 2), 
                                     new Pt((borderBounds.Right + borderBounds.Center.X)/2,borderBounds.Bottom),
                                     new Pt((borderBounds.Left+borderBounds.Center.X)/2, borderBounds.Bottom), 
                                     new Pt(borderBounds.Left, (borderBounds.Bottom+borderBounds.Center.Y)/2),
                                     new Pt(borderBounds.Left, (borderBounds.Top+borderBounds.Center.Y)/2) };
            if (ImmediateDrag)
                movePts = GeomUtils.ToPointList(borderBounds);
            _moveWidget.Points = new PointCollection(movePts.Select((Pt p) => (Point)(borderMat * p)));
            _feedbackWidget.Points = new PointCollection(octPts.Select((Pt p) => (Point)(borderMat * p)));
            _feedbackWidget.StrokeThickness = _moveWidget.StrokeThickness = _rotLine.StrokeThickness = 0.5 / zoom;

            updateHandles(zoom, borderMat);
        }

        private void updateHandles(double zoom, Mat borderMat) {
            // move&scale the unit-spaced handles to bounding box of the selection outline
            Handles.RenderTransform = new MatrixTransform(Mat.Rect(GeomUtils.Bounds(Selection.Outline.Points.Select((p) => (Pt)p))) * borderMat);

            // adjust the size and location of the handles to have constant screen space size and offset from selection bounds
            Vec size = new Vec(((Mat)Handles.RenderTransform.Value * new Vec(1, 0)).Length,
                               ((Mat)Handles.RenderTransform.Value * new Vec(0, 1)).Length);
            double handleSize = TopLeft.Width / 2 / zoom;
            Mat fixHandleSize = Mat.Scale(1 / size.X / zoom, 1 / size.Y / zoom) * Mat.Translate(-handleSize / size.X, -handleSize / size.Y);
            BotLeft.RenderTransform = new MatrixTransform(fixHandleSize);
            TopLeft.RenderTransform = new MatrixTransform(fixHandleSize);
            BotRight.RenderTransform = new MatrixTransform(fixHandleSize);
            TopRight.RenderTransform = new MatrixTransform(fixHandleSize);
            RotHdl.RenderTransform = new MatrixTransform(fixHandleSize * Mat.Translate(0, -0 / size.Y));
            RotHdl.Visibility = TopLeft.Visibility = TopRight.Visibility = BotLeft.Visibility = BotRight.Visibility = _useHandles ? Visibility.Visible : Visibility.Hidden;
            //_feedbackWidget.Opacity = 0.1;

            // update floatie and rotation line
            Rct bounds = GeomUtils.Bounds(_feedbackWidget.Points.Select((p) => (Pt)p), _feedbackWidget.RenderTransform.Value);
            _floatie.RenderTransform = new MatrixTransform(1 / zoom, 0, 0, 1 / zoom,
                                                           bounds.TopLeft.X - 25 / zoom, bounds.TopLeft.Y - 25 / zoom - _floatie.ActualHeight / zoom);

            // bcz: want  WPFUtil.GetBounds(RotHdl, _ican).Center;   but it needs to call UpdateLayout to be correct, so we do it by hand instead
            Vec rotBounds = (Pt)RotHdl.RenderedGeometry.Bounds.TopLeft - (Pt)TopLeft.RenderedGeometry.Bounds.TopLeft;
            double left = (double)RotHdl.GetValue(Canvas.LeftProperty);
            double top = (double)RotHdl.GetValue(Canvas.TopProperty);
            Mat rotTransform = (((Mat)RotHdl.RenderTransform.Value) * Mat.Translate(left, top) * ((Mat)Handles.RenderTransform.Value) * ((Mat)LayoutRoot.RenderTransform.Value) * ((Mat)this.RenderTransform.Value) * ((Mat)_ican.RenderTransform.Value));
            Pt rcent = rotTransform * new Pt(RotHdl.Width / 2, RotHdl.Height / 2);
            Pt bcent = bounds.Center;
            _rotLine.X1 = rcent.X;
            _rotLine.Y1 = rcent.Y;
            _rotLine.X2 = (rcent + (rcent - bcent)).X;
            _rotLine.Y2 = (rcent + (rcent - bcent)).Y;
        }
        /// <summary>
        /// as the mouse moves with no buttons pressed, this highlights which part of the Selection widget is active
        /// and configures whether inking should be enabled.
        /// </summary>
        /// <param name="where"></param>
        /// <param name="barrelBtnPressed"></param>
        void prepareToGrab(object device, Pt where, bool barrelBtnPressed) {
            Scene.GrabSelection(device, this);
            _moveWidget.Opacity = _rotLine.Opacity = _floatie.Opacity = 1;
            if (IsHitTestVisible) {
                if (InDragRgn(where)) {
                    _ican.SetInkEnabledForDevice(device, false);
                    _feedbackWidget.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
                }
                else {
                    _ican.SetInkEnabledForDevice(device, barrelBtnPressed);
                    _feedbackWidget.Stroke = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
                }
            }
        }
        /// <summary>
        /// grabs the input focus after a MouseDown.  If Ink is enabled, then drawing begins, otherwise we start watching
        /// for crossing gestures to determine a direct manipulation mode.
        /// </summary>
        /// <param name="where"></param>
        /// <param name="stylus"></param>
        void grabInput(Pt where) {
            _moved = false;
            _xform = Mat.Identity;
            _downTime = DateTime.Now;
            _collecting = new Stroq(where);

            Opacity = 0.1;
            AddHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(crossing_Drag));
            AddHandler(WPFPointDevice.PreviewPointUpEvent,    new RoutedPointEventHandler(pointUp));
            MouseMove += new MouseEventHandler(crossing_MouseMove);
            MouseUp += new MouseButtonEventHandler(mouseUp);

            // compute location of click point in coordinate system of selection
            Matrix renderTransform = Selection.Outline.RenderTransform.Value;
            renderTransform.Invert();
            _selectionRelativePt = ((Mat)renderTransform) * where;
            _ican.RaiseStartTransformingEvent(Selection);
        }
        /// <summary>
        /// removes all input handlers installed by the Selection
        /// </summary>
        public void releaseSelection() {
            // make sure all the move callbacks are gone...
            RemoveHandler(WPFPointDevice.PreviewPointUpEvent, new RoutedPointEventHandler(pointUp));
            RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(rotHdlDrag));
            RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(rotHdlDrag));
            RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(scaleHdlDrag));
            RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(transHdlDrag));
            RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(crossing_Drag));
            RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(flickDragWait));
            this.MouseUp -= new MouseButtonEventHandler(mouseUp);
            this.MouseMove -= new MouseEventHandler(rotHdlMove);
            this.MouseMove -= new MouseEventHandler(scaleHdlMove);
            this.MouseMove -= new MouseEventHandler(transHdlMove);
            this.MouseMove -= new MouseEventHandler(crossing_MouseMove);
            this.MouseMove -= new MouseEventHandler(flickMouseWait);
            _ican.RaiseSelectionDroppedEvent(Selection);
        }
        public SelectionObj Selection { get { return _ican.Selection(_device); } }
        public object       Device    { get { return _device; } set { _device = value; } }

        // mouse movement callbacks - check for flicks across widget edges or otherwise crossing events
        void mouseUp(object sender, MouseButtonEventArgs e) {
            endInteraction();
            Mouse.Capture(null);
            e.Handled = true;
        }
        void pointUp(object sender, RoutedPointEventArgs e) {
            if (e.WPFPointDevice == _device)
                endInteraction();
        }
        void endInteraction() {
            _ican.RaiseStopTransformingEvent(Selection);
            if (!_moved)
                _ican.SetSelection(_device, new SelectionObj());
            else
                _ican.UndoRedo.Add(new XformAction(Selection, _xform, _ican));
            _ican.SetInkEnabledForDevice(_device, true);
            releaseSelection();
            Opacity = 1.0;
        }

        void flickDragWait(object sender, RoutedPointEventArgs e) {
            if (e.WPFPointDevice == _device)
            {
                if (flickWait())
                {
                    this.MouseMove -= new MouseEventHandler(flickMouseWait);
                    RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(flickDragWait));
                    AddHandler(WPFPointDevice.PreviewPointDragEvent, _pendingDrag);
                    e.Handled = true;
                }
            }
        }
        void flickMouseWait(object sender, MouseEventArgs e) {
            if (flickWait())
            {
                RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(flickDragWait));
                this.MouseMove -= new MouseEventHandler(flickMouseWait);
                this.MouseMove += _pendingMove;
                e.Handled = true;
            }
        }
        bool flickWait() {
            if (DateTime.Now.Subtract(_downTime).TotalMilliseconds > 150 && _pendingMove != null) {
                // compute location of click point in coordinate system of selection
                Matrix renderTransform = Selection.Outline.RenderTransform.Value;
                renderTransform.Invert();
                _selectionRelativePt = ((Mat)renderTransform) * _selectionRelativePt;
                return true;
            }
            return false;
        }
        void crossing_Drag(object sender, RoutedPointEventArgs e) {
            if (e.WPFPointDevice == _device)
            {
                crossingMove(e.GetPosition(_ican));
                e.Handled = true;
            }
        }
        void crossing_MouseMove(object sender, MouseEventArgs e) {
            crossingMove(e.GetPosition(_ican));
            e.Handled = true;
        }
        void crossingMove(Pt where) {
            _collecting.Add(where);
            Pt start = Selection.Outline.RenderTransform.Transform(_selectionRelativePt);
            Stroq outline = WPFUtil.PolygonOutline(Selection.Outline);
            LnSeg path = new LnSeg(start, where);
            Pt? hpt = null;
            double closest = 1, t;
            _pendingMove = null;
            _pendingDrag = null;

            // see if any widgets were crossed
            if ((hpt = path.Intersection(_moveWidget.Points.Select((Point p) => (Pt)p), out t)) != null && t < closest) { // crossed the translate boundary
                _selectionRelativePt = (Pt)hpt;
                _pendingMove = transHdlMove;
                _pendingDrag = transHdlDrag;
                closest = t;
            }
            if (AllowScaleRotate) {
                if ((hpt = path.Intersection(WPFUtil.LineSeg(_rotLine), out t)) != null && t < closest) { // crossed the rotate boundary
                    _selectionRelativePt = (Pt)hpt;
                    _pendingMove = rotHdlMove;
                    _pendingDrag = rotHdlDrag;
                    closest = t;
                }
                if ((hpt = path.Intersection(_feedbackWidget.Points.Select((Point p) => (Pt)p), out t)) != null && t < closest) { // crossed scale boundary
                    _selectionRelativePt = (Pt)hpt;
                    _pendingMove = scaleHdlMove;
                    _pendingDrag = scaleHdlDrag;
                    closest = t;
                }
            }
            if (ImmediateDrag && InDragRgn(where)) {
                _selectionRelativePt = (Pt)where;
                _pendingMove = transHdlMove;
                _pendingDrag = transHdlDrag;
                closest = t;
            }

            // perform 'flick' action on the first crossed widget
            if (_pendingMove == transHdlMove) {
                Vec dir = (path.B - path.A).Normal();
                _ican.RaisePreTransformEvent(Selection);
                if (Math.Abs(dir.X) > Math.Abs(dir.Y))
                    Selection.MoveBy(new Vec((dir.X < 0) ? -1 : 1, 0));
                else Selection.MoveBy(new Vec(0, (dir.Y < 0) ? -1 : 1));
                _ican.RaisePostTransformEvent(Selection);
            }

            if (_pendingMove != null) { // transition to flick/drag transition state
                _moved = true;
                RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(crossing_Drag));
                AddHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(flickDragWait));
                this.MouseMove -= new MouseEventHandler(crossing_MouseMove);
                this.MouseMove += new MouseEventHandler(flickMouseWait);
            }
            else _moved = false; // keep waiting...
        }

        // transformation movement callbacks - scale/rotate/translate
        void rotHdlMove(object sender, MouseEventArgs e) {
            rotHdlInteraction(e.GetPosition(_ican));
            e.Handled = true;
        }
        void rotHdlDrag(object sender, RoutedPointEventArgs e) {
            if (e.WPFPointDevice == _device)
            {
                rotHdlInteraction(e.GetPosition(_ican));
                e.Handled = true;
            }
        }
        void rotHdlInteraction(Pt cursor) {
            if (!Selection.Empty) {
                _ican.RaisePreTransformEvent(Selection);
                Pt rotCenter = Selection.ActualBounds.Center;
                Vec v1 = cursor - rotCenter; v1.Normalize();
                Vec v2 = WPFUtil.GetBounds(RotHdl, _ican).Center - rotCenter; v2.Normalize();
                Selection.XformBy(Mat.Rotate(v2.SignedAngle(v1), rotCenter));
                _xform = _xform * Mat.Rotate(v2.SignedAngle(v1), rotCenter);
                _moved = true;
                _ican.RaisePostTransformEvent(Selection);
            }
        }
        void transHdlMove(object sender, MouseEventArgs e) {
            transHdlInteraction(e.GetPosition(_ican));
            e.Handled = true;
        }
        void transHdlDrag(object sender, RoutedPointEventArgs e) {
            if (e.WPFPointDevice == _device)
            {
                transHdlInteraction(e.GetPosition(_ican));
                e.Handled = true;
            }
        }
        DateTime lastMove = DateTime.MinValue;
        void transHdlInteraction(Pt cursor) {
            if (!Selection.Empty) {
                if (DateTime.Now.Subtract(lastMove).TotalMilliseconds > 20)
                {
                    _ican.RaisePreTransformEvent(Selection);
                    // transform selection point to canvas coordinates to compute translational delta
                    Pt where = Selection.Outline.RenderTransform.Transform(_selectionRelativePt);
                    Selection.MoveBy(cursor - where);
                    _xform = _xform * Mat.Translate(cursor - where);
                    _ican.RaisePostTransformEvent(Selection);
                    lastMove = DateTime.Now;
                }
            }
        }
        void scaleHdlMove(object sender, MouseEventArgs e) {
            scaleHdlInteraction(e.GetPosition(_ican));
            e.Handled = true;
        }
        void scaleHdlDrag(object sender, RoutedPointEventArgs e) {
            if (e.WPFPointDevice == _device)
            {
                scaleHdlInteraction(e.GetPosition(_ican));
                e.Handled = true;
            }
        }
        void scaleHdlInteraction(Pt cursor) {
            if (!Selection.Empty) {
                _ican.RaisePreTransformEvent(Selection);
                _moved = true;
                Rct actualBounds = Selection.ActualBounds;
                // transform selection point to canvas coordinates to compute translational delta
                Pt movingVert = Selection.Outline.RenderTransform.Transform(_selectionRelativePt);
                Pt stationaryVert = actualBounds.Center;
                Pt newVertPos = new Ln(stationaryVert, movingVert).ProjectPoint(cursor);
                Vec Xaxis = ((Mat)Selection.Outline.RenderTransform.Value * new Vec(1, 0)).Normal();
                Vec Yaxis = ((Mat)Selection.Outline.RenderTransform.Value * new Vec(0, -1)).Normal();
                Vec scaleVec = new Vec(Xaxis.Dot(newVertPos - stationaryVert) / Xaxis.Dot(movingVert - stationaryVert),
                                       Yaxis.Dot(newVertPos - stationaryVert) / Yaxis.Dot(movingVert - stationaryVert));
                // scale selection about opposite corner
                Selection.XformBy(Mat.Scale(scaleVec, stationaryVert));
                _xform = _xform * Mat.Scale(scaleVec, stationaryVert);
                _ican.RaisePostTransformEvent(Selection);
            }
        }

        // callbacks on the manipulation handles/regions
        void HandleMouseUp(object sender, MouseButtonEventArgs e) {
            Mouse.Capture(null);
            Opacity = 1;
        }
        void HandleMouseDown(object sender, MouseButtonEventArgs e) {
            Mouse.Capture(sender as Ellipse);
            e.Handled = true;
            Opacity = 0.1;
        }
        void HandleMouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            Pt tl = WPFUtil.GetBounds(TopLeft, _ican).Center;
            Pt tr = WPFUtil.GetBounds(TopRight, _ican).Center;
            Pt bl = WPFUtil.GetBounds(BotLeft, _ican).Center;
            Pt br = WPFUtil.GetBounds(BotRight, _ican).Center;
            Dictionary<Ellipse, List<Pt>> pairs = new Dictionary<Ellipse, List<Pt>>(){
              {TopLeft,  new List<Pt>(new Pt[] {tl, br})},
              {TopRight, new List<Pt>(new Pt[] {tr, bl})},
              {BotLeft,  new List<Pt>(new Pt[] {bl, tr})},
              {BotRight, new List<Pt>(new Pt[] {br, tl})}};
            Pt stationaryVert = pairs[sender as Ellipse][1];
            Pt movingVert = pairs[sender as Ellipse][0];
            Pt newVertPos = e.GetPosition(_ican);                   // get new coordinate
            double distFromDiagonal = new Ln(stationaryVert, movingVert).Distance(newVertPos);
            if (distFromDiagonal < 30)
                newVertPos = new Ln(stationaryVert, movingVert).ProjectPoint(newVertPos);
            Vec Xaxis = ((Mat)Selection.Outline.RenderTransform.Value * new Vec(1, 0)).Normal();
            Vec Yaxis = ((Mat)Selection.Outline.RenderTransform.Value * new Vec(0, -1)).Normal();
            Vec scaleVec = new Vec(Xaxis.Dot(newVertPos - stationaryVert) / Xaxis.Dot(movingVert - stationaryVert),
                                   Yaxis.Dot(newVertPos - stationaryVert) / Yaxis.Dot(movingVert - stationaryVert));
            // scale selection about opposite corner
            Selection.XformBy(Mat.Scale(scaleVec, (tr - tl).Normal(), stationaryVert));
            _xform = _xform * Mat.Scale(scaleVec, (tr - tl).Normal(), stationaryVert);
        }

        public void MovePointDown(object sender, RoutedPointEventArgs e)
        {
            Scene.GrabSelection(e.WPFPointDevice, this);
            prepareToGrab(e.WPFPointDevice, e.GetPosition(_ican), false);
            grabInput(e.GetPosition(_ican));
            e.WPFPointDevice.Capture(this);
            e.Handled = true;
        }
        void movePointMove(object sender, RoutedPointEventArgs e) {
            prepareToGrab(e.WPFPointDevice, e.GetPosition(_ican), false);
        }
        void ican_MouseDown(object sender, MouseButtonEventArgs e) {
            if (Visibility == Visibility.Visible) {
                e.Handled = true;
                grabInput(e.GetPosition(_ican));
                Mouse.Capture(this);
            }
        }
        void ican_MouseMove(object sender, MouseEventArgs e) {
            object device = e.StylusDevice == null ? e.Device : e.StylusDevice;
            if (Visibility == Visibility.Visible &&
                e.LeftButton == MouseButtonState.Released &&
                e.RightButton == MouseButtonState.Released &&
                e.MiddleButton == MouseButtonState.Released)
                prepareToGrab(device, e.GetPosition(_ican), e.StylusDevice != null && e.StylusDevice.SwitchState(InqUtils.BarrelSwitch) == StylusButtonState.Down);
        }
        void ican_SelectionMoved(SelectionObj sel) { if (sel == Selection) updateBounds(); }
        void ican_SelectedChanged(object device, InqCanvas canvas) {
            if (device != _device)
                return;
            if (_ican.Selection(_device).Empty)
                Visibility = Visibility.Hidden;
            else {
                Opacity = 1;
                updateBounds();
                Visibility = Visibility.Visible;
            }
        }

        public void Dispose()
        {
            if (Visibility != Visibility.Visible)
            {
                _ican.SelectionMovedEvent -= new InqScene.SelectionMovedHandler(ican_SelectionMoved);
                _ican.SelectedChangedEvent -= new InqScene.SelectedChangedHandler(ican_SelectedChanged);
                _ican.MouseMove -= new MouseEventHandler(ican_MouseMove);
                _ican.MouseDown -= new MouseButtonEventHandler(ican_MouseDown);
            }
        }

        public SelectionFeedback(InqScene ican, object device) {
            this.InitializeComponent();

            _ican = ican;
            _device = device;
            LayoutRoot.Children.Insert(0, _floatie);
            LayoutRoot.Children.Insert(0, _moveWidget);
            LayoutRoot.Children.Insert(0, _feedbackWidget);
            LayoutRoot.Children.Insert(0, _rotLine);
            Visibility = Visibility.Hidden;
            _floatie.CommandPanel = new SelectionToolbar(_ican, this);
            _feedbackWidget.Fill = new SolidColorBrush(Color.FromArgb(18, 255, 200, 200));
            _feedbackWidget.Stroke = Brushes.Blue;
            _feedbackWidget.StrokeThickness = 0.5;
            _feedbackWidget.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });
            _moveWidget.Fill = new SolidColorBrush(Color.FromArgb(48, 255, 200, 200));
            _moveWidget.Stroke = Brushes.Red;
            _moveWidget.StrokeThickness = 0.5;
            _moveWidget.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });
            _rotLine.Stroke = Brushes.Blue;
            _rotLine.StrokeThickness = 0.5;
            _rotLine.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });
            this.Loaded += new RoutedEventHandler((object sender, RoutedEventArgs e) => _floatie.SetInkCanvas(_ican));
            _moveWidget.AddHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(MovePointDown));
            _moveWidget.AddHandler(WPFPointDevice.PointMoveEvent, new RoutedPointEventHandler(movePointMove));
            ican.SelectionMovedEvent += new InqScene.SelectionMovedHandler(ican_SelectionMoved);
            ican.SelectedChangedEvent += new InqScene.SelectedChangedHandler(ican_SelectedChanged);
            ican.MouseMove += new MouseEventHandler(ican_MouseMove);
            ican.MouseDown += new MouseButtonEventHandler(ican_MouseDown);
            RotHdl.PreviewMouseDown += new MouseButtonEventHandler(HandleMouseDown);
            BotRight.PreviewMouseDown += new MouseButtonEventHandler(HandleMouseDown);
            BotLeft.PreviewMouseDown += new MouseButtonEventHandler(HandleMouseDown);
            TopRight.PreviewMouseDown += new MouseButtonEventHandler(HandleMouseDown);
            TopLeft.PreviewMouseDown += new MouseButtonEventHandler(HandleMouseDown);
            RotHdl.MouseMove += new MouseEventHandler(rotHdlMove);
            BotRight.MouseMove += new MouseEventHandler(HandleMouseMove);
            BotLeft.MouseMove += new MouseEventHandler(HandleMouseMove);
            TopRight.MouseMove += new MouseEventHandler(HandleMouseMove);
            TopLeft.MouseMove += new MouseEventHandler(HandleMouseMove);
            RotHdl.MouseUp += new MouseButtonEventHandler(HandleMouseUp);
            BotRight.MouseUp += new MouseButtonEventHandler(HandleMouseUp);
            BotLeft.MouseUp += new MouseButtonEventHandler(HandleMouseUp);
            TopRight.MouseUp += new MouseButtonEventHandler(HandleMouseUp);
            TopLeft.MouseUp += new MouseButtonEventHandler(HandleMouseUp);
            RotHdl.Width = RotHdl.Height = 24;
            TopLeft.Width = TopLeft.Height = 24;
            TopRight.Width = TopRight.Height = 24;
            BotLeft.Width = BotLeft.Height = 24;
            BotRight.Width = BotRight.Height = 24;
        }
        /// <summary>
        /// Whether selections start moving immediately after clicking within the drag region or whether they wait till the border is crossed
        /// </summary>
        public bool ImmediateDrag { get { return _immediateDrag; } set { _immediateDrag = value; } }
        /// <summary>
        /// whether resize and rotate handles are used on the Selection
        /// </summary>
        public bool UseHandles { get { return _useHandles; } set { _useHandles = value; } }
        /// <summary>
        /// whether rotate/scale crossing widgets are displayed
        /// </summary>
        public bool AllowScaleRotate {
            get { return _allowScaleRotate; }
            set {
                _allowScaleRotate = value;
                LayoutRoot.Children.Remove(_feedbackWidget);
                LayoutRoot.Children.Remove(_rotLine);
                if (value) {
                    LayoutRoot.Children.Insert(0, _feedbackWidget);
                    LayoutRoot.Children.Insert(0, _rotLine);
                }
            }
        }
        /// <summary>
        /// the associated Scene
        /// </summary>
        public InqScene Scene { get { return _ican; } }

        public Floatie Floatie { get { return _floatie; } }
    }
}