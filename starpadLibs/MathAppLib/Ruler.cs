using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using starPadSDK.WPFHelp;
using starPadSDK.AppLib;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using Microsoft.Research.DynamicDataDisplay.Charts;

namespace starPadSDK.AppLib
{
    public class Ruler : NumericAxisControl
    {
        private InqScene _can;
        static public double DefaultHeight = 40;
        static public double PixelsPerUnit = 25;

        /// <summary>
        /// Finds the closest Pt (in scene coordinates) in the Scene to the Ruler's edge.
        /// If no Pt is within the specified tolerance, an empty Pt is returned.
        /// </summary>
        /// <param name="dragPt"></param>
        /// <param name="scene"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        Pt   findClosestUnsnappedVertex(Pt dragPt, InqScene scene, double tolerance)
        {
            double closest = tolerance;
            double fingDist = double.MaxValue;
            Pt close = new Pt();
            foreach (FrameworkElement ele in scene.Elements)
                if (ele is PolygonBase)
                    foreach (Pt p in (ele as PolygonBase).TransformedPoints)
                        if (!RulerSnaps.Contains(p) &&
                            (RulerEdge.Distance(p) < closest || Math.Abs(RulerEdge.Distance(p) - closest) < 1e-5))
                        {
                            double newFing = (p - dragPt).Length;
                            if (Math.Abs(RulerEdge.Distance(p) - closest) < 1e-5 && fingDist > newFing)
                                continue;
                            fingDist = (p - dragPt).Length;
                            closest = RulerEdge.Distance(p);
                            close = p;
                        }
            return close;
        } /// <summary>
        /// Finds the closest Pt (in scene coordinates) in the Scene to the Ruler's edge.
        /// </summary>
        /// <param name="dragPt"></param>
        /// <param name="scene"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        Pt findClosestFeature(Pt clickPt, InqScene scene, double tolerance)
        {
            double closest = tolerance;
            Pt close = clickPt;
            foreach (FrameworkElement ele in scene.Elements)
                if (ele is PolygonBase)
                    foreach (Pt p in (ele as PolygonBase).TransformedPoints)
                        if ((clickPt - p).Length < closest) {
                            closest = (clickPt - p).Length;
                            close = p;
                        }
            return close;
        }
        void moveSnappedRuler(Pt dragPt)
        {
            Vec ruleVec = RulerDown - RulerSnaps[0];
            Vec snapVec = dragPt - RulerSnaps[0];
            XformBy(Mat.Rotate(ruleVec.SignedAngle(snapVec), RulerSnaps[0]));
        }
        void snapToVertexAndContact(Pt dragPt, Pt close)
        {
            Pt ruleP = RulerEdge.ClosestPoint(close);
            Vec ruleVec = ruleP - dragPt;
            Vec snapVec = close - dragPt;
            XformBy(Mat.Translate(close - ruleP) *  Mat.Rotate(ruleVec.SignedAngle(snapVec), close));
            if (!RulerSnaps.Contains(close))
                RulerSnaps.Add(close);
        }
        void snapToTwoVertices(Pt dragPt, Pt close)
        {
            Pt ruleP = RulerEdge.ClosestPoint(close);
            Vec ruleVec = ruleP - RulerSnaps[0];
            Vec snapVec = close - RulerSnaps[0];
            XformBy(Mat.Rotate(ruleVec.SignedAngle(snapVec), RulerSnaps[0]));
            Pt rDown  = new Pt(RulerDownLocal.X, 0);
            Pt rNow   = Xform.Inverse() * close;
            Pt rTarg  = Xform.Inverse() * dragPt;
            Pt rStart = RulerSnaps[0];
            if (rTarg.X > rDown.X)
            {
                Width += (rTarg - rDown).X;
                RulerDownLocal = new Pt(rTarg.X, RulerDownLocal.Y);
            }
        }

        public Ruler(InqScene can)
        {
            RulerSnaps = new List<Pt>();
            _can = can;
            this.Width = 250;
            this.Height = Ruler.DefaultHeight;
            Background = new SolidColorBrush(Color.FromArgb(128, Colors.Salmon.R, Colors.Salmon.G, Colors.Salmon.B));
            Range = new Range<double>(0, Width/PixelsPerUnit);

            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new TranslateTransform(50, 510));
            
            RenderTransform = tg;

            _can.AddNoUndo(this);
        }
        /// <summary>
        /// Points that the ruler has been snapped to (used while dragging the Ruler)
        /// </summary>
        public List<Pt> RulerSnaps { get; set; }
        /// <summary>
        /// the Point in the Ruler coordinate space that is being dragged
        /// </summary>
        public Pt       RulerDownLocal  { get; set; }
        public Pt       RulerDown       { get { return (Mat)RenderTransform.Value  * RulerDownLocal; } }
        /// <summary>
        /// returns the LnSeg corresponding to the edge of the Ruler
        /// </summary>
        public LnSeg RulerEdge { get { return new LnSeg(RenderTransform.Transform(new Point()), RenderTransform.Transform(new Point(Width, 0))); } }
        /// <summary>
        /// Moves the ruler by moving RulerDown to be at 'dragPt'.  
        /// freeDrag determines whether the Ruler is translated or whether it will also be
        /// rotated according to is snapped points.
        /// </summary>
        /// <param name="dragPt"></param>
        /// <param name="freeDrag"></param>
        public void Move(Pt dragPt, InqScene scene)
        {
            if (RulerSnaps.Count == 1)  // if ruler is snapped, then resize/rotate it to pass through new contact location
                moveSnappedRuler(dragPt);

            Pt close = findClosestUnsnappedVertex(dragPt, scene, 15);
            if (close != new Pt())
            {
                if (RulerSnaps.Count == 0)
                    snapToVertexAndContact(dragPt, close);
                else
                    snapToTwoVertices(dragPt, close);
            }
            else if (RulerSnaps.Count > 0)
            {
                Pt rTarg = Xform.Inverse() * dragPt;
                Pt rStart = RulerSnaps[0];
                if ((dragPt - rStart).Dot((Xform * new Vec(5, 0))) < 0)
                {
                    RenderTransform = new MatrixTransform(Mat.Rotate((Deg)180) * Xform);
                    rTarg = Xform.Inverse() * dragPt;
                    rStart = RulerSnaps[0];
                }
                Width += Math.Max(1,(rTarg - RulerDownLocal).X);
                RulerDownLocal = new Pt(rTarg.X, RulerDownLocal.Y);
            }
            else XformBy(Mat.Translate(dragPt - RulerDown));
        }
        public Mat  Xform   { get { return (Mat) RenderTransform.Value ; } }
        public void XformBy(Mat xform)
        {
            RenderTransform = new MatrixTransform((Mat)RenderTransform.Value * xform);
        }
        public void StartDrag(Pt down)
        {
            RulerSnaps = new List<Pt>();
            RulerDownLocal = down;
        }
        public void StartRubberband(Pt start, Pt dragPt, InqScene scene)
        {
            RulerSnaps = new List<Pt>();
            Pt snap = findClosestFeature(start, scene, 40);
            RulerSnaps.Add(snap);
            XformBy(Mat.Translate(snap - Xform * new Pt()));
            Width = (dragPt - start).Length;
            RulerDownLocal = new Pt((dragPt-start).Length, 0);
        }
    }
}
