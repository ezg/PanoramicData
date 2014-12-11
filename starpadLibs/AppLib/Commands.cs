using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.Serialization.Formatters.Binary;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;
using starPadSDK.CharRecognizer;
using starPadSDK.DollarRecognizer;
using InputFramework;
using InputFramework.DeviceDriver;
using InputFramework.WPFDevices;

namespace starPadSDK.AppLib {
    public class LineSelectCommand : OneStrokeGesture {
        InqScene _can;
        public LineSelectCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device) { return (s.Cusps().Length == 2 && s.Cusps().Straightness(0, 1) < 0.15); }
        public override void Fire(Stroq[] strokes, object device) {
            List<System.Windows.Point> pts = new List<System.Windows.Point>();
            foreach (Point p in strokes[0])
                pts.Add(p);
            foreach (Stroq s in _can.Stroqs) {
                if (s.GetBounds().IntersectsWith(strokes[0].GetBounds()))
                    if (s.BackingStroke.HitTest(pts.ToArray(), new RectangleStylusShape(1, 1))) {
                        Group g = _can.Groups.Find(s);
                        if (g != null)
                            _can.Selection(device).Add(new SelectionObj(g.AllStrokes(), g.AllElements(), null));
                        else
                            _can.Selection(device).Add(new SelectionObj(new Stroq[] { s }));
                        Wiggle w = _can.Wiggle(s);
                        if (w != null)
                            _can.Selection(device).Add(new SelectionObj(null, new FrameworkElement[] { w.A, w.B }, null));
                        _can.SetSelection(device, _can.Selection(device)); // notifies callbacks that the selection has changed
                    }
            }
        }
    }
    public class TapSelectCommand : OneStrokeGesture {
        InqScene _can;
        public TapSelectCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device) { return s.IsTap(); }
        public override void Fire(Stroq[] strokes, object device) {
            List<FrameworkElement> fes = new List<FrameworkElement>();
            foreach (FrameworkElement fe in _can.Elements)
                if (WPFUtil.GetBounds(fe, _can).Contains(strokes[0][0])) {
                    Group g = _can.Groups.Find(fe);
                    if (g != null)
                        _can.SetSelection(device, new SelectionObj(g.AllStrokes(), g.AllElements(), null));
                    else
                        _can.SetSelection(device, new SelectionObj(null, new FrameworkElement[] { fe }, null));
                }
        }
    }
    public class TextCommand : TwoStrokeGesture {
        InqScene _can;
        public TextCommand(InqScene can) { _can = can; }
        public override string Prompt { get {  return "Type to enter text"; } }
        public override bool Test1(Stroq s, object device) { _can.Focusable = true; _can.Focus(); return s.IsInsertion(); }
        public override bool Test2(Stroq s, Stroq prev, object device) { return false; }
        public override void Fire(Stroq[] strokes, object device) { System.Windows.Controls.TextBox tb = WPFUtil.MakeText("", strokes[0].GetBounds()); _can.AddWithUndo(tb); tb.Focus(); }
    }
    public class LassoCommand : TwoStrokeGesture {
        InqScene _can;
        public object test(Stroq lasso) {
            List<Stroq> contained = new List<Stroq>();
            List<FrameworkElement> elements = new List<FrameworkElement>();
            foreach (Stroq s in _can.Stroqs)
                if (lasso.GetBounds().Contains(s.GetBounds()))
                    contained.Add(s);
            foreach (FrameworkElement c in _can.Elements) {
                Rct crect = WPFUtil.GetBounds(c);
                if (lasso.GetBounds().IntersectsWith(crect) && !crect.Contains(lasso.GetBounds())) {
                    if (lasso.GetBounds().Contains(crect))
                        elements.Add(c);
                    else if (lasso.BackingStroke.HitTest(WPFUtil.GetOutline(c, _can).Select((Pt p) => (Point)p).ToArray(), new RectangleStylusShape(1, 1)))
                        elements.Add(c);
                }
            }
            return new SelectionObj(contained, elements, lasso.ToArray());
        }
        public LassoCommand(InqScene can) { _can = can; }
        public LassoCommand(InqScene can, bool oneStroke) { _can = can; OneStroke = oneStroke;  }
        public override string Prompt { get { return "Tap to select"; } }
        public override bool Test1(Stroq s, object device) { return s.IsLasso() && !((SelectionObj)s.Lassoed(test)).Empty; }
        public override bool Test2(Stroq s, Stroq prev, object device) { return s.IsTap(); }
        public override void Fire(Stroq[] strokes, object device) {
            _can.SetSelection(device, (SelectionObj)strokes[0].Lassoed(test));
            Group gr = _can.Groups.Create(_can.Selection(device).Strokes, _can.Selection(device).Elements);
            if (gr.Elements.Length != 0 || gr.Strokes.Length != 0 || gr.Groups.Length != 1)
                _can.Groups.Add(gr);
        }
    }
    public class ZoomOutCommand : OneStrokeGesture {
        InqScene _can;
        public ZoomOutCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device) { return s.IsDoubleHitch() == DoubleHitchTester.Dir.SW; }
        public override void Fire(Stroq[] strokes, object device) {
            FrameworkElement ancestor = _can.Parent as FrameworkElement;
            for (; (ancestor is TabItem); ancestor = ancestor.Parent as FrameworkElement)
                ;

            // save old zoom ratio to update ink drawing width
            double oldZoom = 1/(((Mat)_can.RenderTransform.Value)*new Vec(1,1)).X;

            double zoomRatio = 2 / _can.RenderTransform.Value.M11;

            // center of display window in untransformed coordinates
            Pt  center = new Pt(ancestor.ActualWidth/2, ancestor.ActualHeight/2);

            // transform center of zoom stroke to center of display window, then scale around center of display window
            _can.RenderTransform = new MatrixTransform(
                Mat.Translate(center-strokes[0][0]) * Mat.Scale(new Vec(1 / zoomRatio, 1 / zoomRatio), center) );

            _can.DefaultDrawingAttributes.Width  *= zoomRatio/oldZoom;
            _can.DefaultDrawingAttributes.Height *= zoomRatio/oldZoom;
        }
    }
    public class ZoomInCommand : OneStrokeGesture {
        InqScene _can;
        public ZoomInCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device) { return s.IsDoubleLoop(); }
        public override void Fire(Stroq[] strokes, object device) {
            FrameworkElement ancestor = _can.Parent as FrameworkElement;
            for (; (ancestor is TabItem); ancestor = ancestor.Parent as FrameworkElement)
                ;

            // save old zoom ratio to update ink drawing width
            double oldZoom = 1/(((Mat)_can.RenderTransform.Value)*new Vec(1,1)).X;

            Rct rect = strokes[0].GetBounds();    // get bounds of zoom stroke
            double aspect = rect.Width / rect.Height;
            double scrAspect = ancestor.ActualWidth / ancestor.ActualHeight;
            double zoomRatio = 1;
            if (aspect < scrAspect)
                 zoomRatio = rect.Height / ancestor.ActualHeight;
            else zoomRatio = rect.Width  / ancestor.ActualWidth;

            // center of display window in untransformed coordinates
            Pt  center = new Pt(ancestor.ActualWidth/2, ancestor.ActualHeight/2);

            // transform center of zoom stroke to center of display window, then scale around center of display window
            _can.RenderTransform = new MatrixTransform(
                Mat.Translate(center-rect.Center) * Mat.Scale(new Vec(1 / zoomRatio, 1 / zoomRatio), center) );

            _can.DefaultDrawingAttributes.Width  *= zoomRatio/oldZoom;
            _can.DefaultDrawingAttributes.Height *= zoomRatio/oldZoom;
        }
    }
    public class UndoCommand : OneStrokeGesture {
        InqScene _can;
        public UndoCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device) { return s.IsLeftRight() != 0; }
        public override void Fire(Stroq[] strokes, object device) {
            switch (strokes[0].IsLeftRight()) {
                case -1: _can.UndoRedo.Undo(); break;
                case 1: _can.UndoRedo.Redo(); break;
            }
        }
    }
    public class DollarCommand : OneStrokeGesture {
        InqScene _can;
        string[] _names = new string[0];
        public DollarCommand(InqScene can, string[] gestureNames) {
            _names = gestureNames;
            _can = can; 
        }
        public override bool Test(Stroq s, object device) { return s.Dollar(_names) != ""; }
        public override void Fire(Stroq[] strokes, object device) {
            Console.WriteLine("GOT: " + strokes[0].Dollar(_names));
            strokes[0].BackingStroke.DrawingAttributes.Color = _can.DefaultDrawingAttributes.Color;
            _can.AddWithUndo(strokes[0]);
        }
    }
    public class PasteCommand : FlickCommand {
        InqScene _can;
        public PasteCommand(InqScene can, string chars):base(chars) { _can = can; }
        public override void Fire(Stroq[] strokes, object device) {
            SelectionObj sel = _can.PasteSelection();
            if (sel != null) {
                Rct bounds = sel.Bounds;
                sel.XformBy(Mat.Translate((new Pt() - bounds.TopLeft) + strokes[0][0]));
                _can.SetSelection(device, sel);
            }
        }
    }
    public class InteractivePasteCommand : SymbolCommand
    {
        InqScene _can;
        public InteractivePasteCommand(InqScene can, string chars) : base(chars) { _can = can; }
        public override void Fire(Stroq[] strokes, object device)
        {
            SelectionObj sel = _can.PasteSelection();
            if (sel != null)
            {
                Rct bounds = sel.Bounds;
                sel.XformBy(Mat.Translate((new Pt() - bounds.TopLeft) + strokes[0][0]));
                _can.SetSelection(device, sel);
            }
            _can.SetInkEnabledForDevice(device, false);
        }
    }
    public class FlickAbortCommand : TwoStrokeGesture
    {
        InqScene _can;
        public FlickAbortCommand(InqScene can) { _can = can; }
        public override bool Test1(Stroq s, object device) { return s.IsFlick(); }
        public override bool Test2(Stroq s, Stroq prev, object device) { return s.IsTap(); }
        public override string Prompt { get { return ""; } }
        public override void Fire(Stroq[] strokes, object device)
        {
            strokes[0].BackingStroke.DrawingAttributes.Color = _can.DefaultDrawingAttributes.Color;
            _can.AddWithUndo(strokes[0]);
        }
    }
    public class SnapshotCommand : FlickCommand {
        InqScene _can;
        public SnapshotCommand(InqScene can, string chars) : base(chars) { _can = can; }
        public override void Fire(Stroq[] strokes, object device) {
            Image img = BasicSnapshot.TakeSnapshot().ConvertBitmapToWPFImage(100);
            _can.AddWithUndo(img);
            _can.SetSelection(device, new SelectionObj(null, new FrameworkElement[] { img }, null));
        }
    }
    public class PictureCommand : FlickCommand {
        InqScene _can;
        public PictureCommand(InqScene can, string chars) : base(chars) { _can = can; }
        public override void Fire(Stroq[] strokes, object device) {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                BitmapImage bmg = new BitmapImage(new Uri(ofd.FileName));
                Image img = new Image();
                img.VerticalAlignment = VerticalAlignment.Top;
                img.Width = 100;
                img.Height = 100;
                img.Source = bmg;
                _can.AddWithUndo(img);
                _can.SetSelection(device, new SelectionObj(null, new FrameworkElement[] { img }, null));
            }
        }
    }
    public class ImageCropCommand : TwoStrokeGesture{
        InqScene _can;
        /// <summary>
        /// tests if strokes pass Balanced Crop test to see if an image is contained within the crops
        /// </summary>
        /// NOTE: if an Images is contained within the crops, this will store the Image in a SelectionObj that 
        /// will be cached on the second crop stroke 'crop2' as its 'Cropped' attribute (access it using 'crop2.Cropped(test)' )
        /// <param name="crop1"></param>
        /// <param name="crop2"></param>
        /// <returns></returns>
        object test(Stroq crop1, Stroq crop2) {
            Rct bounds = crop2.GetBounds().Union(crop1.GetBounds());
            List<FrameworkElement> eles = new List<FrameworkElement>();
            foreach (FrameworkElement fe in _can.Elements)
                if (WPFUtil.GetBounds(fe).IntersectsWith(bounds))
                    eles.Add(fe);
            return new SelectionObj(null, eles, null);
        }
        public ImageCropCommand(InqScene can) { _can = can; }
        public override void   Fire(Stroq[] strokes, object device) {
            SelectionObj sobj = (SelectionObj)strokes[0].Cropped(strokes[1],test);
            Rct sbounds = strokes[0].GetBounds().Union(strokes[1].GetBounds());
            FrameworkElement cropImage = null;
            foreach (FrameworkElement e in sobj.Elements)
                if ((e is Image || (e is Canvas && (e as Canvas).Children.Count > 0 && (e as Canvas).Children[0] is Image)) ) {
                    cropImage = e;
                    break;
                }
                //System.Windows.Ink.InkAnalyzer ia = new InkAnalyzer();
                //foreach (Stroq s in sobj.Strokes)
                //    ia.AddStroke(s.BackingStroke);
                ////AnalysisHintNode node = ia.CreateAnalysisHint();
                ////node.Factoid = 
                ////node.CoerceToFactoid = true;
                ////node.Location.MakeInfinite();
                //AnalysisStatus astat = ia.Analyze();
                //string reco = ia.GetRecognizedString();
                //if (astat.Successful && reco != "Other") {
                //    ATextBox tb = new ATextBox(reco, strokes[0].GetBounds().Union(strokes[1].GetBounds()));
                //    tb.Set(this);
                //    Expr e = Text.Convert(tb.Text);
                //    Rect nombbox, inkbbox;
                //    AnImage img = new AnImage();
                //    img.Source = GDIplusWPF.Draw(e, "Arial", (float)tb.FontSize, Colors.Black, false, 96, out nombbox, out inkbbox);
                //    img.Height = 100;
                //    img.Width = 100 * inkbbox.Width / inkbbox.Height;
                //    img.RenderTransform = new TranslateTransform(tb.RenderTransform.Value.OffsetX, tb.RenderTransform.Value.OffsetY + 200);
                //    Add(img);
                //}
            Mat    old = Mat.Identity;
            Image  img = cropImage as Image;
            Canvas can = cropImage as Canvas;
            if (img != null) {
                _can.Rem(img);
                _can.SceneLayer.Children.Remove(img);
                old = (Mat)img.RenderTransform.Value;
                can = new Canvas();
                can.ClipToBounds = true;
                can.Children.Add(img);
                _can.AddWithUndo(can);
            }
            else if (can != null && can.Children[0] is Image) {
                img = can.Children[0] as Image;
                old = (Mat)img.RenderTransform.Value * (Mat)can.RenderTransform.Value;
            }
            if (can != null) {
                can.Width = sbounds.Width;
                can.Height = sbounds.Height;
                can.RenderTransform = new MatrixTransform(Mat.Translate(sbounds.TopLeft));
                img.RenderTransform = new MatrixTransform(Mat.Scale((old * new Vec(1, 0)).Length,
                                                                                                                       (old * new Vec(0, 1)).Length) * 
                                                                                                    Mat.Translate(old * new Pt() - sbounds.TopLeft));
                _can.SetSelection(device, new SelectionObj(null, new FrameworkElement[] { can }, null));
            }
        }
        public override bool   Test1(Stroq s, object device)                      { return s.IsCrop(); }
        public override bool   Test2(Stroq s, Stroq prev, object device) { return s.IsCrop() && 
                                                                                                             prev.BalancedCrops(s) && 
                                                                                                             !((SelectionObj)prev.Cropped(s,test)).Empty; }
        public override string Prompt { get { return "Draw Crop"; } }
    }
    public class CircleCommand : TwoStrokeGesture {
        InqScene _can;
        public CircleCommand(InqScene can) { _can = can; }
        public override string Prompt { get { return "Tap to make curve pusher widget"; } }
        public override bool Test1(Stroq s, object device) { return s.IsCircle(); }
        public override bool Test2(Stroq s, Stroq prev, object device) { return s.IsTap(); }
        public override void Fire(Stroq[] strokes, object device) { _can.AddWithUndo(new CurveEditing.CurveWidg(_can, strokes[0].GetBounds().TopLeft, strokes[0].GetBounds().MaxDim).Visual); }
    }
    public class WiggleCommand : OneStrokeGesture {
        InqScene _can;
        public WiggleCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device) { return s.Cusps().Length == 2 && s.Cusps().Straightness(0, 1) < 0.15; }
        public override void Fire(Stroq[] strokes, object device) { strokes[0].BackingStroke.DrawingAttributes.Color = _can.DefaultDrawingAttributes.Color; _can.AddWithUndo(new Wiggle(strokes[0])); }
    }

    public class CircleDiagramCommand : OneStrokeGesture
    {
        InqScene _can;
        public CircleDiagramCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device)
        {
            Rct r = s.GetBounds();
            if (r.Height < 75)
                return false;

            var fd = new starPadSDK.CharRecognizer.FeaturePointDetector();
            string allo = "";
            if (!fd.match_o(fd.FeaturePoints(s.OldStroke()),ref allo))
                return false;

            if (r.Width / r.Height < 0.8 || r.Height / r.Width < 0.8)
                return false;
 
            return true;
        }
        public override void Fire(Stroq[] strokes, object device)
        {
            // Create a yellow Ellipse.
            Ellipse myEllipse = new Ellipse();

            // Create a SolidColorBrush with a red color to fill the 
            // Ellipse with.
            SolidColorBrush mySolidColorBrush = new SolidColorBrush();

            // Describes the brush's color using RGB values. 
            // Each value has a range of 0-255.
            mySolidColorBrush.Color = Color.FromArgb(60, 255,255, 153);
            myEllipse.Fill = mySolidColorBrush;
            myEllipse.StrokeThickness = 1;
            myEllipse.Stroke = Brushes.Yellow;

            // Set the width and height of the Ellipse.
            double avg = 0.5 * (strokes[0].GetBounds().Width + strokes[0].GetBounds().Height);
            myEllipse.Width = avg;
            myEllipse.Height = avg;

            //position
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new TranslateTransform(strokes[0].GetBounds().TopLeft.X, strokes[0].GetBounds().TopLeft.Y));
            myEllipse.RenderTransform = tg;

            // Add the Ellipse to the StackPanel.
            _can.AddWithUndo(myEllipse);

        }
    }

    public class ScribbleTapCommand : TwoStrokeGesture {
        protected InqScene _can;
        bool    _splitStrokes = false;
        bool     _lenient;
        List<Type> _typesToIgnore = new List<Type>();
        object test(Stroq hull) {
            List<Stroq> hitMarks = new List<Stroq>();
            List<FrameworkElement> hitRects = new List<FrameworkElement>();
            bool gotOne = false; // whether something is completely contained within the scribble
            List<Pt> hullPts = hull.Cusps().ScribblePts();
            SelectionObj sel = new SelectionObj();
            if (hullPts.Count > 4) {
                double[] areas = new double[hullPts.Count - 2];
                int[] trisUsed = new int[hullPts.Count - 2]; // initializes all counts to '0'
                for (int i = 2; i < hullPts.Count; i++)
                    areas[i - 2] = GeomUtils.SignedArea(hullPts[i - 2], hullPts[i - 1], hullPts[i]);
                foreach (Stroq m in _can.Stroqs)
                    if (m.GetBounds().IntersectsWith(hull.GetBounds()) &&   // if polygon bounds intersects .. and .. polygons vertices are contained
                        scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, m.Select((Pt p) => (Point)p).ToArray()))
                        hitMarks.Add(m);
                foreach (FrameworkElement r in _can.Elements)
                    //if (r is Ruler)
                    //    continue;
                    //else if (r is PolygonBase)
                    //{
                    //    PolygonBase p = r as PolygonBase;
                    //    if (p.Bounds.IntersectsWith(hull.GetBounds()) &&   // if stroq bounds intersects ... and ... stroq test passes
                    //        scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, p.TransformedPoints.ToArray()))
                    //        hitRects.Add(p);
                    //}
                    //else
                    {
                        if (_typesToIgnore.Contains(r.GetType()))
                        {
                            continue;
                        }
                        if (r is Canvas)
                        {
                            foreach (var mk in (r as Canvas).Children)
                            {
                                if (mk is Line)
                                {
                                    Line l = mk as Line;
                                    if (scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, new Point[] { new Point(l.X1, l.Y1), new Point(l.X2, l.Y2) }))
                                    {
                                        hitRects.Add(l);
                                    }
                                }
                                else
                                {
                                    FrameworkElement mkf = mk as FrameworkElement;
                                    if (!r.RenderTransform.TransformBounds(mkf.RenderTransform.TransformBounds(
                                            new Rect(new Point(), new Size(mkf.Width, mkf.Height)))).Contains(hull.GetBounds()))
                                        gotOne = testElement(hull, hitRects, gotOne, hullPts, areas, trisUsed, mk as FrameworkElement);
                                }
                            }
                        }


                        if (r is GeometryElement)
                        {
                            GeometryElement g = r as GeometryElement;
                            if (g.GetGeometry().Intersects(hull.GetLineString()))
                            {
                                for (int tri = 0; tri < hullPts.Count() - 2; tri++)
                                {
                                    trisUsed[tri]++;
                                }
                                hitRects.Add(r);
                            }
                        }
                        else if (r is Line)
                        {
                            Line l = r as Line;
                            if (scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, new Point[] { new Point(l.X1, l.Y1), new Point(l.X2, l.Y2) }))
                            {
                                hitRects.Add(l);
                            }
                        }
                        else
                        {
                            Rct eleBounds = WPFUtil.GetBounds(r);
                            if (eleBounds.IntersectsWith(hull.GetBounds()) && !eleBounds.Contains(hull.GetBounds()) &&// if stroq bounds intersects but doesn't contain element bounds ...
                                elementHitTest(hullPts, areas, trisUsed, r, ref gotOne))          //  and... element test passes
                                hitRects.Add(r);
                        }
                    }

                // test to see whether the start and end of the scribble are deleting anything
                bool triStart, triEnd;
                double triPercent = analyzeScribbleIntersections(trisUsed, out triStart, out triEnd, out triPercent);

                if (gotOne || triPercent >= TriThreshold || (hitMarks.Count > 1 && triStart && triEnd))
                    sel = new SelectionObj(hitMarks, hitRects, null);
            }
            return sel;
        }
        private bool testElement(Stroq hull, List<FrameworkElement> hitRects, bool gotOne, List<Pt> hullPts, double[] areas, int[] trisUsed, FrameworkElement f)
        {
            var eleBounds = new Rct(f.RenderTransform.Transform(new Point()), new Vec(f.Width, f.Height));
            if (eleBounds.IntersectsWith(hull.GetBounds()) && !eleBounds.Contains(hull.GetBounds()) &&// if stroq bounds intersects but doesn't contain element bounds ...
                elementHitTest(hullPts, areas, trisUsed, f, ref gotOne))          //  and... element test passes
                hitRects.Add(f);
            return gotOne;
        }
        public bool SplitStrokes {
            get { return _splitStrokes; }
            set { _splitStrokes = value; }
        }
        public ScribbleTapCommand(InqScene can) { _can = can; }
        public ScribbleTapCommand(InqScene can, List<Type> typesToIgnore) { _can = can; _typesToIgnore = typesToIgnore; }
        /// <summary>
        /// crates a ScribbleTap Command
        /// </summary>
        /// <param name="can"></param>
        /// <param name="oneStroke">true if no tap is needed</param>
        /// <param name="lenient">specifies whether scribbles with > 6 cusps that intersect something are automatically delete scribbles</param>
        public ScribbleTapCommand(InqScene can, bool oneStroke, bool lenient, bool splitStrokes) { _can = can; OneStroke = oneStroke; _lenient = lenient; _splitStrokes = splitStrokes;  }
        public override string Prompt { get { return "Tap to delete"; } }
        public override void Fire(Stroq[] strokes, object device) {
            SelectionObj deletions = (SelectionObj)strokes[0].ScribbledOver(test);

            if (_splitStrokes) {
                Stroq[] additions = splitStrokes(strokes, deletions);
                _can.UndoRedo.Add(new ReplaceAction(new SelectionObj(additions), deletions, _can));
            } else
                _can.UndoRedo.Add(new DeleteAction(deletions, _can));

            _can.SetSelection(device, new SelectionObj());  // update the selection
        }

        public SelectionObj FireWithResult(Stroq[] strokes, object device)
        {
            SelectionObj deletions = (SelectionObj)strokes[0].ScribbledOver(test);

            if (_splitStrokes)
            {
                Stroq[] additions = splitStrokes(strokes, deletions);
                _can.UndoRedo.Add(new ReplaceAction(new SelectionObj(additions), deletions, _can));
            }
            else
                _can.UndoRedo.Add(new DeleteAction(deletions, _can));

            _can.SetSelection(device, new SelectionObj());  // update the selection
            return deletions;
        }

        public SelectionObj GetResult(Stroq[] strokes, object device)
        {
            SelectionObj deletions = (SelectionObj)strokes[0].ScribbledOver(test);
            return deletions;
        }

        public override bool Test1(Stroq s, object device) 
        { 
            var tt = ((SelectionObj)s.ScribbledOver(test)).Empty;

            return !((SelectionObj)s.ScribbledOver(test)).Empty || (_lenient && s.Cusps().Length > 6 && _can.Stroqs.HitTest(s.Select<Pt, Pt>((Pt p) => p), new RectangleStylusShape(1, 1)).Count > 0); 
        }
        public override bool Test2(Stroq s, Stroq prev, object device) { return s.IsTap(); }
        #region Scribble Details
          public double TriThreshold = .75; // set to .4 or lower if gestures are likely
        // bcz: Hack!  avoids problems when intersection point between two lines becomes a segment because of numerical error
        void addIfNotTooSmall(ref List<Stroq> added, Stroq s) {
            if (s.GetBounds().MaxDim > 2)
                added.Add(s);
        }
        Stroq[] splitStrokes(Stroq[] strokes, SelectionObj deletions) {
            List<Stroq> added = new List<Stroq>();
            List<Pt> hul = new List<Pt>(strokes[0].ConvexHull());
            PathGeometry deleteHull = WPFUtil.Geometry(strokes[0].ConvexHull());
            foreach (Stroq s in deletions.Strokes) {

                if (WPFUtil.GeometryContains(deleteHull, s))
                    continue;

                float[] scribInts = s.OldFindIntersections(new StroqCollection(new Stroq[] { strokes[0] }));
                float[] sceneInts = s.OldFindIntersections(new StroqCollection(_can.Stroqs));
                List<float[]> keepInts = new List<float[]>();
                float lastInt = 0;
                float preLastInt = 0;
                for (int i = 0; i < sceneInts.Count(); i++) {
                    if (intervalContains(lastInt, sceneInts[i], scribInts)) {
                        if (preLastInt != lastInt)
                            addIfNotTooSmall(ref added, stroqFromRange(s, new float[] { preLastInt, lastInt }));
                        preLastInt = sceneInts[i];
                    }
                    else if (preLastInt == -1)
                        preLastInt = lastInt;
                    lastInt = sceneInts[i];
                }
                if (intervalContains(lastInt, s.Count(), scribInts)) {
                    if (preLastInt != lastInt)
                        addIfNotTooSmall(ref added, stroqFromRange(s, new float[] { preLastInt, lastInt }));
                }
                else if (preLastInt != s.Count() - 1)
                    addIfNotTooSmall(ref added, stroqFromRange(s, new float[] { preLastInt, s.Count() - 1 }));
            }
            return added.ToArray();
        }
        Stroq stroqFromRange(Stroq s, float[] range) {
            List<Pt> rangePts = new List<Pt>();
            rangePts.Add(s[range[0]]);
            for (int i = (int)Math.Ceiling(range[0]); i < Math.Floor(range[1]); i++)
                rangePts.Add(s[i]);
            rangePts.Add(s[range[1]]);
            return new Stroq(rangePts);
        }
        bool intervalContains(float start, float end, float[] scribInts) {
            foreach (float scribint in scribInts)
                if (scribint > start && scribint < end)
                    return true;
            return false;
        }
        bool lineIntersectsTri(Pt a, Pt b, Pt[] hullTris, double area, int ind) {
            LnSeg ab = new LnSeg(a, b);
            return ab.Intersection(new LnSeg(hullTris[ind], hullTris[ind + 1])) != null ||
                ab.Intersection(new LnSeg(hullTris[ind + 1], hullTris[ind + 2])) != null ||
                ab.Intersection(new LnSeg(hullTris[ind], hullTris[ind + 2])) != null ||
                pointInTri(a, hullTris, area, ind); // If line segment is inside triangle
        }
        bool pointInTri(Pt a, Pt[] hullTris, double area, int ind) {
            double u = GeomUtils.SignedArea(a, hullTris[ind + 1], hullTris[ind + 2]);
            double v = GeomUtils.SignedArea(hullTris[ind], a, hullTris[ind + 2]);
            double w = area - u - v;
            if (u < 0 && v < 0 && w < 0 && area < 0 && -u - v - w <= -area)
                return true;
            if (u > 0 && v > 0 && w > 0 && area > 0 && u + v + w <= area)
                return true;
            return false;
        }
        bool scribbleStrokeTest(Stroq hull, ref bool gotOne, Pt[] hullPts, double[] areas, int[] trisused, Point[] mpts) {
            List<Stroq> mstrokeList = new List<Stroq>();
            int inside = 0;
            bool intersects = hull.BackingStroke.HitTest(mpts, new RectangleStylusShape(2, 2));
            for (int i = 0; (!gotOne || inside == 0) && i < mpts.Length; i++) {
                // need to do short circuit bbox test of line vs. triangle
                // first, get bounding box of line
                Rct lineBox = new Rct(mpts[i], new Vec());
                bool testLineSeg = false;
                if (i != mpts.Length - 1) {
                    lineBox = lineBox.Union(mpts[i + 1]);
                    testLineSeg = true;
                }
                bool ptConsumed = false;
                for (int tri = 0; tri < hullPts.Length - 2; tri++) {
                    Rct triBbox = new Rct(hullPts[tri], new Vec());
                    triBbox = triBbox.Union(new Rct(hullPts[tri + 1], new Vec()));
                    triBbox = triBbox.Union(new Rct(hullPts[tri + 2], new Vec()));
                    if (lineBox.IntersectsWith(triBbox)) {
                        bool ptInTri = pointInTri(mpts[i], hullPts, areas[tri], tri);
                        if ((!testLineSeg && ptInTri) ||
                                        (testLineSeg &&
                                        lineIntersectsTri(mpts[i], mpts[i + 1], hullPts, areas[tri], tri))) {
                            intersects = true;
                            if (ptInTri && !ptConsumed) {
                                inside++;
                                ptConsumed = true;
                            }
                            trisused[tri]++;
                        }
                    }
                }
            }
            if (inside == mpts.Length)
                gotOne = true;
            return inside > 0 || intersects;
        }
        bool elementHitTest(List<Pt> hullPts, double[] areas, int[] trisused, FrameworkElement r, ref bool gotOne) {
            double rhullarea;
            double coverage;
            Pt[] outlinePts = WPFUtil.GetOutline(r, _can);
            rhullarea = GeomUtils.PolygonArea(outlinePts);
            coverage = 0;
            bool intersects = false;
            for (int tri = 0; tri < hullPts.Count() - 2; tri++) {
                double tcover = areas[tri] > 0 ?
                    GeomUtils.PolygonArea(GeomUtils.ClipPolygonToTriangle(outlinePts, hullPts[tri], hullPts[tri + 1], hullPts[tri + 2])) :
                    GeomUtils.PolygonArea(GeomUtils.ClipPolygonToTriangle(outlinePts, hullPts[tri], hullPts[tri + 2], hullPts[tri + 1]));
                coverage += tcover;
                if (tcover / Math.Abs(areas[tri]) > 0.001) {
                    trisused[tri]++;
                    intersects = true;
                    if (gotOne)
                        break;
                }
            }
            if (coverage / rhullarea >= 0.5)
                gotOne = true;
            return intersects;
        }
        double analyzeScribbleIntersections(int[] trisUsed, out bool triStart, out bool triEnd, out double triPercent) {
            int trisHit = 0;
            triStart = false;
            triEnd = false;

            for (int tu = 0; tu < trisUsed.Length; tu++) {
                if (trisUsed[tu] != 0) {
                    trisHit++;
                    if (tu < .25 * trisUsed.Length)
                        triStart = true;
                    if (tu > .75 * trisUsed.Length)
                        triEnd = true;
                }
            }
            triPercent = trisHit / (double)(trisUsed.Length);
            return triPercent;
        }
        #endregion
    }
}
