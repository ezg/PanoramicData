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
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using InputFramework;
using InputFramework.DeviceDriver;
using InputFramework.WPFDevices;

namespace starPadSDK.AppLib {
    public class EmptyGraphCommand :TwoStrokeGesture
    {
        InqScene _can;
        MathEditor _math;
        Rct _dummy = Rct.Null;
        List<Parser.Range> _exprs = new List<Parser.Range>();

        public override string Prompt { get { return "Draw Cross"; } }
        public Rct Dummy {
            get { return _dummy; }
            set { _dummy = value; }
        }
        public delegate void FunctionPlotHandler(IEnumerable<Parser.Range> funcs, Rct where);
        public event FunctionPlotHandler FunctionPlotEvent;
        public EmptyGraphCommand(InqScene can, MathEditor math) { _can = can; _math = math; }
        public EmptyGraphCommand(InqScene can, Rct box) { _can = can; _dummy = box; } // dummy condition for GestureBar
        public override bool Test1(Stroq s, object device)
        {
            starPadSDK.Inq.BobsCusps.Cusps crec = s.Cusps();
            foreach (Parser.Range r in _math.RecognizedMath.Ranges)
                if (r.RBounds.IntersectsWith(s.GetBounds().Inflate(20, 20)))
                    return false;
            if (crec.Straightness() < 0.14 && (s[-1]-s[0]).Length > 100 && crec.Length == 2 && crec.outSeg(0).UnsignedAngle(new LnSeg(new Pt(), new Pt(0,1))) < Math.PI/8)
                return true;
            return false;
        }
        public override bool Test2(Stroq s, Stroq prev, object device)
        {
            starPadSDK.Inq.BobsCusps.Cusps crec = s.Cusps();
            if (crec.Straightness() < 0.14 && (s[-1] - s[0]).Length > 100 && s.OldFindIntersections(new StroqCollection(new Stroq[] { prev })).Length > 0)
                return true;
            return false;
        }
        public override void Fire(Stroq[] strokes, object device) {
            if (_dummy != Rct.Null) {
                if (FunctionPlotEvent != null) {
                    Parser.Range range = new Parser.Range(new Parser.ParseResult(new CompositeExpr(WellKnownSym.power, new LetterSym('x'), 4), new System.Drawing.Rectangle()));
                    FunctionPlotEvent(new Parser.Range[] { range }, strokes[0].GetBounds().Union(strokes[1].GetBounds()));
                }
            }
            else if (FunctionPlotEvent != null)
                FunctionPlotEvent(_exprs, strokes[0].GetBounds().Union(strokes[1].GetBounds()));
        }
    }
    public class TapOnMathCommand : OneStrokeGesture {
        InqScene _can;
        MathEditor _editor;
        ContainerVisualHost _math;

        public delegate void TappedMathHandler(ContainerVisualHost math);
        public event TappedMathHandler TappedMathEvent;
        public TapOnMathCommand(InqScene can, MathEditor medit) { _can = can; _editor = medit;  }
        public override bool Test(Stroq s, object device)
        {
            if (s.GetBounds().MaxDim < 12)
                foreach (FrameworkElement ele in _editor.MathUICanvas.Children)
                    if (ele is ContainerVisualHost && WPFUtil.GetBounds(ele).Contains(s[0])) {
                        _math = ele as ContainerVisualHost;
                        return true;
                    }
            return false;
        }
        public override void Fire(Stroq[] strokes, object device) {
            if (TappedMathEvent != null)
                TappedMathEvent(_math);
        }
    }
    public class GraphingCommand : OneStrokeGesture {
        InqScene   _can;
        MathEditor _math;
        Rct        _dummy = Rct.Null;
        List<Parser.Range> _exprs = new List<Parser.Range>();

        public Rct Dummy {
            get { return _dummy; }
            set { _dummy = value; }
        }
        public delegate void FunctionPlotHandler(IEnumerable<Parser.Range> funcs, Pt where);
        public event FunctionPlotHandler FunctionPlotEvent;
        public GraphingCommand(InqScene can, MathEditor math) { _can = can; _math = math; }
        public GraphingCommand(InqScene can, Rct box) { _can = can; _dummy = box; } // dummy condition for GestureBar
        public override bool Test(Stroq s, object device)
        {
            _exprs.Clear();
            bool endsOnGraph = false;
            foreach (FrameworkElement fe in _can.Elements)
                if (fe.Tag is FunctionPlot || fe is FunctionPlot)
                    if (WPFUtil.GetBounds(fe).Contains(s[-1]))
                        endsOnGraph = true;
            if (_dummy != Rct.Null) {
                if (_dummy.Contains(s[0]))
                    if (s.Cusps().Straightness(0, -1) > .2 &&
                        (s.Cusps().Length == 2 ||
                        (s.Cusps().Length == 3 && s.Cusps().Straightness(0, 1) < 0.25 &&
                         s.Cusps().Straightness(1, 2) < 0.25)) &&
                         (s.GetBounds().Bottom - _dummy.Top / 100) / (_dummy.Height / 100) > 1.4 &&
                         s.Cusps().outSeg(0).Direction.UnsignedAngle(new Vec(0, 1)) < 25) {
                        return true;
                    }
                return false;
            }
            bool isGraphing = false;
            foreach (Parser.Range range in _math.RecognizedMath.Ranges) {
                Vec dir = s.Cusps().outSeg(0).Direction;
                starPadSDK.Inq.BobsCusps.Cusps crec = s.Cusps();
                foreach (Pt p in s)
                    if (range.RBounds.Contains(p)) {
                        _exprs.Add(range);
                        break;
                    }
                if (range.RBounds.Contains(s[0]) && (s[0].Y-range.RBounds.Top)/range.RBounds.Height > 0.5)
                    if (endsOnGraph || 
                        (crec.Straightness(0, -1) > .2 - crec.Distance/1000 - (s.GetBounds().Right-range.RBounds.Right)/range.RBounds.Width/20  && 
                        (crec.Length == 2 ||
                        (crec.Length ==3 && crec.Straightness(0,1) < 0.25 && 
                         crec.Straightness(1,2) < 0.25)) &&
                         (s.GetBounds().Bottom - range.RBounds.Top)/(range.RBounds.Height) > 1.10 && 
                         crec.outSeg(0).Direction.UnsignedAngle(new Vec(0, 1)) < 25)) {
                        isGraphing = true;
                    }
            }
            return isGraphing;
        }
        public override void Fire(Stroq[] strokes, object device) {
            if (_dummy != Rct.Null) {
                if (FunctionPlotEvent != null) {
                    Parser.Range range = new Parser.Range(new Parser.ParseResult(new CompositeExpr(WellKnownSym.power, new LetterSym('x'), 4), new System.Drawing.Rectangle()));
                    FunctionPlotEvent(new Parser.Range[] { range }, strokes[0][-1]);
                }
            } 
            else if (FunctionPlotEvent != null)
                  FunctionPlotEvent(_exprs, strokes[0][-1]);
        }
    }
    
    public class MathCropCommand : TwoStrokeGesture {
        InqScene _can;
        /// <summary>
        /// tests if strokes pass Balanced Crop test to see if an image is contained within the crops
        /// </summary>
        /// <param name="crop1"></param>
        /// <param name="crop2"></param>
        /// <returns></returns>
        object test(Stroq crop1, Stroq crop2) {
            Rct                    bounds = crop2.GetBounds().Union(crop1.GetBounds());
            List<Stroq>            stroqs = new List<Stroq>();
            List<FrameworkElement> eles   = new List<FrameworkElement>();
            foreach (Stroq s in _can.Stroqs)
                if (bounds.IntersectsWith(s.GetBounds()))
                    stroqs.Add(s);
            foreach (FrameworkElement fe in _can.Elements)
                if (WPFUtil.GetBounds(fe).IntersectsWith(bounds))
                    if (fe is Image || (fe is Canvas && (fe as Canvas).Children.Count > 0 && (fe as Canvas).Children[0] is Image))
                        return new SelectionObj();
            return new SelectionObj(stroqs, eles, null);
        }
        public override void   Fire(Stroq[] strokes, object device) {
            SelectionObj    sobj       = (SelectionObj)strokes[0].Cropped(strokes[1],test);
            StroqCollection mathStroqs = new StroqCollection(sobj.Strokes);
            MathRecognition mrec       = new MathRecognition(mathStroqs);
            if (mrec.Ranges != null && mrec.Ranges[0].Parse.expr != null) {
                _can.UndoRedo.Add(new DeleteAction(sobj, _can));
                // mrec.EnsureLoaded();
                ContainerVisualHost cvh = new ContainerVisualHost();
                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                Rct nombbox = MathExpr.ExprWPF.EWPF.DrawTop(mrec.Ranges[0].Parse.expr, 36, dc, Colors.Black, new Pt(), false);
                dc.Close();
                cvh.Children.Add(dv);
                cvh.Width = nombbox.Width;
                cvh.Height = nombbox.Height;
                cvh.RenderTransform = new TranslateTransform(mrec.Sim.Stroqs.GetBounds().Left, mrec.Sim.Stroqs.GetBounds().Top);
                _can.AddWithUndo(cvh);
            }
            mathStroqs.Clear();
        }
        public override string Prompt { get { return "Draw Crop"; } }
        public override bool   Test1(Stroq s, object device) { return s.IsCrop(); }
        public override bool   Test2(Stroq s, Stroq prev, object device) { return s.IsCrop() && s.BalancedCrops(prev) && !((SelectionObj)prev.Cropped(s,test)).Empty; }
        public MathCropCommand(InqScene can) { _can = can; }
    }
    public class WiggleCommand : OneStrokeGesture {
        InqScene _can;
        public WiggleCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device) { return s.Cusps().Length == 2 && s.Cusps().Straightness(0, 1) < 0.15; }
        public override void Fire(Stroq[] strokes, object device) { strokes[0].BackingStroke.DrawingAttributes.Color = _can.DefaultDrawingAttributes.Color; _can.AddWithUndo(new Wiggle(strokes[0])); }
    }
    public class RectangleCommand : OneStrokeGesture
    {
        InqScene _can;
        public RectangleCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device)
        {
            var fd = new starPadSDK.CharRecognizer.FeaturePointDetector();
            if (fd.match_rect(fd.FeaturePoints(s.OldStroke())))
                return true;
            return false;
        }
        public override void Fire(Stroq[] strokes, object device)
        {
            Pt a, b, c, d;
            a = strokes[0].Cusps()[0].pt;
            b = strokes[0].Cusps()[1].pt;
            c = strokes[0].Cusps()[2].pt;
            d = strokes[0].Cusps()[3].pt;

            List<double> coordY = new List<double>(new double[] { a.Y, b.Y, c.Y, d.Y });
            coordY.Sort();

            List<double> coordX = new List<double>(new double[] { a.X, b.X, c.X, d.X });
            coordX.Sort();

            a = new Pt(coordX[0], coordY[0]);
            b = new Pt(coordX[3], coordY[0]);
            c = new Pt(coordX[3], coordY[3]);
            d = new Pt(coordX[0], coordY[3]);

            PolyRectangle rect = new PolyRectangle(_can, a, b, c, d);
            rect.Fill = new SolidColorBrush(Color.FromArgb(50, 135, 206, 250));
            rect.Stroke = Brushes.Blue;
            _can.AddWithUndo(rect);
        }
    }

    public class TriangleCommand : OneStrokeGesture    {
        InqScene _can;
        public TriangleCommand(InqScene can) { _can = can; }
        public override bool Test(Stroq s, object device)
        {    
            //straightness test
            if (s.Cusps().Length != 4) return false;
            if (s.Cusps().Straightness(0,1) > 0.30) return false;
            if (s.Cusps().Straightness(1,2) > 0.30) return false;
            if (s.Cusps().Straightness(2,3) > 0.30) return false;

            starPadSDK.Inq.BobsCusps.FeaturePointDetector.CuspRec firstCusp = s.Cusps()[0];
            starPadSDK.Inq.BobsCusps.FeaturePointDetector.CuspRec lastCusp = s.Cusps()[s.Cusps().Length-1];

            //proximity check
            double dist = Math.Sqrt((firstCusp.pt.X - lastCusp.pt.X) * (firstCusp.pt.X - lastCusp.pt.X) +
                    (firstCusp.pt.Y - lastCusp.pt.Y) * (firstCusp.pt.Y - lastCusp.pt.Y));
            
            Rct r = s.GetBounds();
            double size = (r.Height+ r.Width)/2;

            if ((dist / size) > 0.2) return false;

            //size test
            double longestDimension = Math.Max(r.Width, r.Height);
            if (longestDimension < 50) return false;

            return true;
        }
        public override void Fire(Stroq[] strokes, object device)
        {
            starPadSDK.Inq.BobsCusps.FeaturePointDetector.CuspRec firstCusp = strokes[0].Cusps()[1];
            starPadSDK.Inq.BobsCusps.FeaturePointDetector.CuspRec lastCusp = strokes[0].Cusps()[2];
            Triangle t = new Triangle(_can, strokes[0][0], firstCusp.pt, lastCusp.pt);
            t.Stroke = Brushes.Green;
            t.Fill = new SolidColorBrush(Color.FromArgb(20, 125, 125, 0));
            _can.AddWithUndo(t);
        }
    }
}
