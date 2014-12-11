using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using starPadSDK.Inq;
using starPadSDK.Inq.MSInkCompat;
using starPadSDK.Geom;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.DollarRecognizer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Input;
using BrownRecognitionCommon;


namespace ShapeRecognizer
{
    public static class Recognizer
    {
        static double             maxDist(IEnumerable<Point> pts, Pt baseP, Vec dir, out Pt left, out Pt right, out int indexL, out int indexR, out bool rightIsfarthest) {
            double farleft = 0, farright = 0;
            indexL = indexR = -1;
            int i = -1;
            left = right = baseP;
            foreach (Pt p in pts) {
                i++;
                Pt near = baseP + dir * (p - baseP).Dot(dir);
                Vec offset = p - near;
                double dist = offset.Length;
                bool onLeft = dir.Det(offset) < 0;
                if (dist > farleft && onLeft) {
                    farleft = dist;
                    indexL = i;
                    left = p;
                }
                if (dist > farright && !onLeft) {
                    farright = dist;
                    indexR = i;
                    right = p;
                }
            }
            rightIsfarthest = farright > farleft; 
            return Math.Max(farleft, farright);
        }
        static void               insertCorner(Stroq s, List<Point> pts, int prevInd, Pt prevPt, int curInd, Pt curPt)
        {
            LnSeg outSeg = new LnSeg(prevPt, s[Math.Min(s.Count-1, prevInd + 10)]);
            LnSeg inSeg = new LnSeg(s[Math.Max(0, curInd - 10)], curPt);
            for (int j = prevInd; j < curInd; j++)
                if ((s[j] - prevPt).Length > 20)
                {
                    outSeg = new LnSeg(prevPt, s[j]);
                    if (Math.Abs(outSeg.Direction.Normal().X) > 0.96)
                        outSeg = new LnSeg(prevPt, new Point(s[j].X, prevPt.Y));
                    if (Math.Abs(outSeg.Direction.Normal().Y) > 0.96)
                        outSeg = new LnSeg(prevPt, new Point(prevPt.X, s[j].Y));
                    break;
                }
            for (int j = curInd; j >= prevInd; j--)
                if ((s[j] - curPt).Length > 20)
                {
                    inSeg = new LnSeg(s[j], curPt);
                    if (Math.Abs(inSeg.Direction.Normal().X) > 0.96)
                        inSeg = new LnSeg(new Point(s[j].X, curPt.Y), curPt);
                    if (Math.Abs(inSeg.Direction.Normal().Y) > 0.96)
                        inSeg = new LnSeg(new Point(curPt.X, s[j].Y), curPt);
                    break;
                }
            var inter = outSeg.LnIntersection(inSeg);
            if (inter != null)
            {
                ;
                Console.WriteLine("out dir = " + outSeg.Direction.Normal() + " sampled = " + ((Pt)inter - prevPt).Normal() + " ang = " + Math.Acos(outSeg.Direction.Normal().X));
                pts.Add((Point)inter);
            }
        }

        static LnSeg              projectionSpan(IEnumerable<StylusPoint> points, LnSeg axis) {
            var xmin = double.MaxValue;
            var xmax = double.MinValue;
            Ln l = new Ln(axis.A, axis.B);
            Vec dir = axis.Direction.Normal();
            foreach (var p in points) {
                Pt pp = l.ProjectPoint(p);
                xmin = Math.Min(xmin, (pp - axis.A).Dot(dir));
                xmax = Math.Max(xmax, (pp - axis.A).Dot(dir));
            }
            return new LnSeg(new Pt(xmin, 0), new Pt(xmax, 0));
        }
        static IEnumerable<Point> principalAxis(IEnumerable<StylusPoint> points, out double wid, out double hgt, out TransformGroup xform) {

            // Find axis of projection with smallest span
            LnSeg  minAxisSpan = new LnSeg(new Pt(), new Pt(double.MaxValue,0));
            double ang = 0;
            for (int i = 0; i < 180; i += 5) {
                var axisSpan = projectionSpan(points, new LnSeg(points.First(), (Pt)points.First()+new RotateTransform(i).Transform(new Pt(1,0))));
                if (axisSpan.Length < minAxisSpan.Length) {
                    ang = i;
                    minAxisSpan = axisSpan;
                }
            }

            // create basis vectors and bounding box dimensions
            Vec xdir = (Vec)new RotateTransform(ang).Transform(new Point(1,0));
            Vec ydir = xdir.Perp().Normal();
            wid  = minAxisSpan.Length;
            hgt  = projectionSpan(points, new LnSeg(points.First(), (Pt)points.First() + ydir)).Length;

            // transform points into new basis
            Ln xaxis = new Ln(new Point(), xdir);
            Ln yaxis = new Ln(new Point(), ydir);
            var projectedPts = points.Select((pt) => new Point(((Vec)xaxis.ProjectPoint(pt)).Dot(xdir), ((Vec)yaxis.ProjectPoint(pt)).Dot(ydir)));
            
            // shift points so that they are all in the +x, +y quadrant
            var minx           = projectedPts.Select((pt) => pt.X).Min();
            var miny           = projectedPts.Select((pt) => pt.Y).Min();
            var shiftedOrigin  = new Point() + minx * xdir + miny * ydir;
            var transformedPts = projectedPts.Select((pt) => pt - new Vec(minx, miny));

            // create the transform from the projected points to their original coordinate system 
            xform = new TransformGroup();
              //xform.Children.Add(new ScaleTransform(wid, hgt));
                xform.Children.Add(new RotateTransform(-xdir.SignedAngle(new Vec(1,0)) * 180 / Math.PI));
                xform.Children.Add(new TranslateTransform(shiftedOrigin.X, shiftedOrigin.Y));

            return new PointCollection(transformedPts);
        }
        static void               findQuadrilateralFeatures(double wid, double hgt, IEnumerable<Point> rotatedPts, out Pt topLeft, out Pt botRight, out Pt topRight, out Pt botLeft, out double topAng, out double botAng,
                                    out int topLeftInd, out int topRightInd,out int botRightInd, out int botLeftInd)
        {
            bool rightisfarthest;
            maxDist(rotatedPts, new Point(wid, 0), (new Pt(0, hgt) - new Pt(wid, 0)).Normal(), out botRight, out topLeft, out botRightInd, out topLeftInd, out rightisfarthest);
            maxDist(rotatedPts, new Point(0, 0), (new Pt(wid, hgt) - new Pt(0, 0)).Normal(), out topRight, out botLeft, out topRightInd, out botLeftInd, out rightisfarthest);

            Vec topSide = (topRight - topLeft).Normal();
            Vec botSide = (botRight - botLeft).Normal();
            topAng = (Math.Atan2(topSide.X, topSide.Y) * 180 / Math.PI) % 90;
            botAng = (Math.Atan2(botSide.X, botSide.Y) * 180 / Math.PI) % 90;
            topAng = topAng > 45 ? Math.Abs(90 - topAng) : topAng;
            botAng = botAng > 45 ? Math.Abs(90 - botAng) : botAng;
        }

        static bool               testForRightTriangle(ref Pt pt1, ref Pt apex, ref Pt pt3) {
            var dot = (pt1 - apex).Normal().Dot((pt3 - apex).Normal());
            if (Math.Abs(dot) < 0.2) {
                var newPt3 = (Vec)(new RotateTransform(90).Transform((Pt)(pt1 - apex)));
                var flip = newPt3.Dot(pt3 - apex) < 0;
                pt3 = apex + ((Vec)(new RotateTransform(flip ? -90 : 90).Transform((Pt)(pt1 - apex)))).Normal() * (pt3 - apex).Length;
                return true;
            }
            return false;
        }
        static bool               testForIsosceles(Stroq s, out Pt pt1, out Pt apex, out Pt pt3) {
            pt1 = s.Cusps().Farthest(0, 1);
            apex = s[0];
            pt3 = s.Cusps()[1].pt;
            if (s.Cusps().Length == 2) {
                pt1 = s[s.Count / 3];
                pt3 = s[s.Count * 2 / 3];
            }
            if (Math.Abs((pt1 - apex).Length - (pt3 - apex).Length) > Math.Abs((pt1 - pt3).Length - (pt1 - apex).Length)) {
                var p = apex;
                apex = pt1;
                pt1 = p;
            }
            if (Math.Abs((pt1 - apex).Length - (pt3 - apex).Length) > Math.Abs((pt1 - pt3).Length - (pt3 - apex).Length)) {
                var p = apex;
                apex = pt3;
                pt3 = p;
            }
            double l1 = (pt1 - apex).Length;
            double l2 = (pt3 - apex).Length;
            if (Math.Abs(1.0 - l1 / l2) < 0.1) {
                double len = (l1 + l2) / 2;
                pt1 = apex + (pt1 - apex).Normal() * len;
                pt3 = apex + (pt3 - apex).Normal() * len;
                return true;
            }
            return false;
        }
        static bool               testForEllipse(Stroq s, double ovalThreshold, double wid, double hgt)
        {
            var hull = new List<Pt>(s.ConvexHull());
            double area = 0;
            for (int i = 1; i < hull.Count(); i++)
            {
                area += 0.5 * (hull[i - 1] - hull[0]).Det(hull[i] - hull[0]);
            }
            if (s.Count > 5 && Math.Abs(area) <= ovalThreshold * wid * hgt)
                return true;
            return false;
        }

        /// <summary>
        ///  tries to find a closed polygon in a set of input lines
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="figure"></param>
        /// <returns></returns>
        static List<Point>        getClosedFigure(IEnumerable<BrownLine> lines, Point[] figure, List<BrownLine> used)
        {
            foreach (var l in lines)
            {
                used.Add(l);
                double angleModifier = 1- Math.Abs((((Vec)(figure[1] - figure.First())).Normal()).Dot(((Vec)(l.Last - l.First)).Normal()));
                if (((Pt)l.First - (Pt)figure.First()).Length < lineJoinThreshold(l, angleModifier))
                {
                    if (((Pt)l.Last - (Pt)figure.Last()).Length < lineJoinThreshold(l, angleModifier))
                        return new List<Point>(figure);
                    else
                    {
                        var newFigure = new List<Point>(figure);
                        newFigure.Insert(0, l.Last);
                        var pts = getClosedFigure(lines.Where((ll) => ll != l), newFigure.ToArray(), used);
                        if (pts != null)
                            return new List<Point>(pts);
                    }
                }
                angleModifier = 1 - Math.Abs((((Vec)(figure[figure.Length - 2] - figure.Last())).Normal()).Dot(((Vec)(l.Last - l.First)).Normal()));
                if ((((Pt)l.First) - (Pt)figure.Last()).Length < lineJoinThreshold(l, angleModifier))
                {
                    if ((((Pt)l.Last) - (Pt)figure.First()).Length < lineJoinThreshold(l, angleModifier))
                        return new List<Point>(figure);
                    else
                    {
                        var newFigure = new List<Point>(figure);
                        newFigure.Add(l.Last);
                        var pts = getClosedFigure(lines.Where((ll) => ll != l), newFigure.ToArray(), used);
                        if (pts != null)
                            return new List<Point>(pts);
                    }
                }
                angleModifier = 1 - Math.Abs((((Vec)(figure[1] - figure.First())).Normal()).Dot(((Vec)(l.First - l.Last)).Normal()));
                if ((((Pt)l.Last) - (Pt)figure.First()).Length < lineJoinThreshold(l, angleModifier))
                {
                    if ((((Pt)l.First) - (Pt)figure.Last()).Length < lineJoinThreshold(l, angleModifier))
                        return new List<Point>(figure);
                    else
                    {
                        var newFigure = new List<Point>(figure);
                        newFigure.Insert(0, l.First);
                        var pts = getClosedFigure(lines.Where((ll) => ll != l), newFigure.ToArray(), used);
                        if (pts != null)
                            return new List<Point>(pts);
                    }
                }
                angleModifier = 1 - Math.Abs((((Vec)(figure[figure.Length - 2] - figure.Last())).Normal()).Dot(((Vec)(l.First - l.Last)).Normal()));
                if ((((Pt)l.Last) - (Pt)figure.Last()).Length < lineJoinThreshold(l, angleModifier))
                {
                    if ((((Pt)l.First) - (Pt)figure.First()).Length < lineJoinThreshold(l, angleModifier))
                        return new List<Point>(figure);
                    else
                    {
                        var newFigure = new List<Point>(figure);
                        newFigure.Add(l.First);
                        var pts = getClosedFigure(lines.Where((ll) => ll != l), newFigure.ToArray(), used);
                        if (pts != null)
                            return new List<Point>(pts);
                    }
                }
                used.Remove(l);
            }
            return null;
        }

        private static double lineJoinThreshold(BrownLine l, double angleModifier)
        {
            return 5 + Math.Min(40, 0.25*(l.First - l.Last).Length) * angleModifier;
        }
        /// <summary>
        /// Tries to match a shape from a set of input lines
        /// </summary>
        /// <param name="l"></param>
        public static List<Point> constructFromLines(List<BrownLine> lines)
        {
            foreach (var line in lines)
            {
                List<BrownLine> used = new List<BrownLine>(new BrownLine[] { line });
                var fig = getClosedFigure(lines.Where((l) => l != line), new Point[] { line.First, line.Last }, used);
                if (fig != null && fig.Count() > 2)
                {
                    fig.Add(fig.First());
                    lines.Clear();
                    lines.AddRange(used.ToArray());
                    return fig;
                }
            }

            return null;
        }

        /// <summary>
        /// creates a parallelogram from the specified quadrilateral features
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="topRight"></param>
        /// <param name="botLeft"></param>
        /// <param name="botRight"></param>
        /// <param name="xform"></param>
        /// <returns></returns>
        static List<Point> createParallelogram(out ShapeType shapeType, Point topLeft, Point topRight, Point botLeft, Point botRight, Transform xform)
        {
            botRight = botLeft + (topRight - topLeft);
            double wid = Math.Max(topRight.X, botRight.X);
            double hgt = Math.Max(botLeft.Y, botRight.Y);

            List<Point> output = new List<Point>();
            output.AddRange(new Point[] { topLeft, topRight, botRight, botLeft, topLeft }.Select((pt) => xform.Transform(pt)));
            
            shapeType = ShapeType.Parallelogram;
            return output;
        }
        /// <summary>
        /// Creates the best fitting triangle to an input stroq
        /// The triangle may be one of: Right, Isosceles, Scalene
        /// </summary>
        /// <param name="s"></param>
        static List<Point> createTriangle(out ShapeType shapeType, Stroq s)
        {
            shapeType = ShapeType.Triangle;

            Pt pt1, apex, pt3;
            if (testForIsosceles(s, out pt1, out apex, out pt3))
            {
                shapeType = ShapeType.IsoscelesTriangle;
            }


            // test for Right triangle
            Point pt1Save = pt1, pt3Save = pt3, apexSave = apex;
            if (testForRightTriangle(ref pt1, ref apex, ref pt3) ||
                testForRightTriangle(ref pt1, ref pt3, ref apex) ||
                testForRightTriangle(ref pt3, ref pt1, ref apex))
            {
                pt1 = pt1Save;
                pt3 = pt3Save;
                apex = apexSave;
                int rightTris = 0;
                if (testForRightTriangle(ref pt1, ref apex, ref pt3)) rightTris++;
                pt1 = pt1Save;
                pt3 = pt3Save;
                apex = apexSave;
                if (testForRightTriangle(ref pt1, ref pt3, ref apex)) rightTris++;
                pt1 = pt1Save;
                pt3 = pt3Save;
                apex = apexSave;
                if (testForRightTriangle(ref pt3, ref pt1, ref apex)) rightTris++;
                if (rightTris == 1)
                {
                    shapeType = ShapeType.RightTriangle;
                }
                else
                {
                    pt1 = pt1Save;
                    pt3 = pt3Save;
                    apex = apexSave;
                }
            }

            Vec v1 = pt1 - apex;
            Vec v2 = pt3 - pt1;
            if (v1.Det(v2) < 0) {
                Pt p = pt1;
                pt1 = pt3;
                pt3 = p;
            }
            v1 = (pt1 - apex).Normal();
            RotateTransform rot = new RotateTransform(-Math.Atan2(v1.Y, v1.X));
            TranslateTransform trans = new TranslateTransform(-pt1.X, -pt1.Y);
            var xform = new TransformGroup();
            xform.Children.Add(rot);
            xform.Children.Add(trans);
            PointCollection pc = new PointCollection(new Point[] { pt1, apex, pt3, pt1 });
            var rotatedPts = pc.Select((pt) => xform.Inverse.Transform(pt));
            double wid = 0, hgt = 0;
            foreach (var p in rotatedPts) {
                if (p.X > wid) wid = p.X;
                if (p.Y > hgt) hgt = p.Y;
            }

            List<Point> output = new List<Point>();
            output.AddRange(rotatedPts.Select((pt) => xform.Transform(pt)));
            
            return output;
        }
        /// <summary>
        /// creates an oriented trapezoid that best matches an input stroq
        /// </summary>
        /// <param name="s"></param>
        static List<Point> createTrapezoid(out ShapeType shapeType, Stroq s)
        {
            var r = s.GetBounds();

            TransformGroup xform;
            double wid, hgt;
            var rotatedPts = principalAxis(s.StylusPoints, out wid, out hgt, out xform);
            double left = 0, right = 0;
            foreach (var p in s) {
                var pp = xform.Inverse.Transform(p);
                if (pp.X > wid / 2 && pp.Y > right)
                    right = pp.Y;
                else if (pp.X < wid / 2 && pp.Y > left)
                    left = pp.Y;
            }
            bool flip = left > right; //  xform.Inverse.Transform(s.Cusps()[1].pt).X < xform.Inverse.Transform(s[0]).X;
            var pts = flip ? new Point[] { new Point(wid, hgt * 2 / 3), new Point(wid, hgt / 3), new Point(0, 0), new Point(0, hgt), new Point(wid, hgt * 2 / 3) } :
                               new Point[] { new Point(wid, hgt), new Point(wid, 0), new Point(0, hgt / 3), new Point(0, hgt * 2 / 3), new Point(wid, hgt) };

            List<Point> output = new List<Point>();
            output.AddRange(pts.Select((pt) => xform.Transform(pt)));
            shapeType = ShapeType.Trapezoid;
            return output;
        }
        /// <summary>
        /// creates the best fitting quadrilateral to an input stroq
        /// The quad may be one of : rectangle, rounded rectangle, square, diamond, parallelogram
        /// </summary>
        /// <param name="s"></param>
        static List<Point> createQuad(out ShapeType shapeType, Stroq s, double ovalThreshold)
        {
            Rect r = s.GetBounds();
            shapeType = ShapeType.Rect;

            TransformGroup xform;
            double wid, hgt;
            var rotatedPts = principalAxis(s.StylusPoints, out wid, out hgt, out xform);

            if (testForEllipse(s, ovalThreshold, wid, hgt))
                return createEllipse(out shapeType, s);

            double topAng, botAng;
            Pt topLeft, botRight, topRight, botLeft;
            int topLeftInd, topRightInd, botRightInd, botLeftInd;
            findQuadrilateralFeatures(wid, hgt, rotatedPts, out topLeft, out botRight, out topRight, out botLeft, out topAng, out botAng, out topLeftInd, out topRightInd, out botRightInd, out botLeftInd);


            if (((topLeft.Y < topRight.Y && botLeft.Y > botRight.Y) || (topLeft.Y > topRight.Y && botLeft.Y < botRight.Y)) &&
                Math.Abs((botLeft.Y-topLeft.Y) / (botRight.Y - topRight.Y) - 1) > 0.33)
                return createTrapezoid(out shapeType, s);


            // measure curvature of corners to determine if this is a paralleogram
            double straight = 0;
            if (Math.Abs(topLeftInd - topRightInd) < s.Count * .5)
                straight = s.Cusps().PtStraightness(Math.Min(topLeftInd, topRightInd), Math.Max(topLeftInd, topRightInd));
            if (Math.Abs(botLeftInd - botRightInd) < s.Count * .5)
                straight = Math.Max(straight, s.Cusps().PtStraightness(Math.Min(botLeftInd, botRightInd), Math.Max(botLeftInd, botRightInd)));
            if (topAng + botAng > 40 && straight < 0.18)
                return createParallelogram(out shapeType, topLeft, topRight, botLeft, botRight, xform);
            else {

                double ang = ((xform as TransformGroup).Children[0] as RotateTransform).Angle;
                if (Math.Abs(ang + 90) < 5 || Math.Abs(ang-270) < 5)
                    ((xform as TransformGroup).Children[0] as RotateTransform).Angle = -90;
                if (Math.Abs(ang - 90) < 5)
                    ((xform as TransformGroup).Children[0] as RotateTransform).Angle = 90;
                if (Math.Abs(ang ) < 5)
                    ((xform as TransformGroup).Children[0] as RotateTransform).Angle = 0;
                if (Math.Abs(Math.Abs(ang)-180) < 5)
                    ((xform as TransformGroup).Children[0] as RotateTransform).Angle = 180;
                bool near45 = Math.Abs(Math.Abs(ang - 90) - 45) < 15;
                if (Math.Abs(1 - (wid / hgt)) < (near45 ? 0.25 : 0.15)) {
                    wid = hgt;
                    shapeType = ShapeType.Square;
                    if (near45) {
                        shapeType = ShapeType.Diamond;
                        ((xform as TransformGroup).Children[0] as RotateTransform).Angle = ang > 90 ? 135 : 45;
                    }
                }

                int slen = s.Cusps().Length;
                if (s.Count > 15 && (s.Cusps().Length <= 3 || Math.Abs(s.Cusps()[1].curvature) < 0.35))
                {
                    shapeType = ShapeType.RoundedRect;
                }
            }

            List<Point> output = new List<Point>();
            output.AddRange(new Point[] {
                xform.Transform(new Point()), xform.Transform(new Point(wid, 0)), xform.Transform(new Point(wid, hgt)), xform.Transform(new Point(0, hgt)), xform.Transform(new Point())
            });

            return output;
        }
        /// <summary>
        /// creates the best fitting ellipse to an input stroq
        /// The ellipse may be one of: circle, oval
        /// </summary>
        /// <param name="s"></param>
        static List<Point> createEllipse(out ShapeType shapeType, Stroq s)
        {
            TransformGroup xform;
            double wid, hgt;
            var rotatedPts = principalAxis(s.StylusPoints, out wid, out hgt, out xform);

            shapeType = ShapeType.Ellipse;
            if (Math.Abs(1 - (wid / hgt)) < 0.25)
            {
                wid       = hgt;
                shapeType = ShapeType.Circle;
            }

            List<Point> output = new List<Point>();
            for (int i = 0; i < 360; i += 5)
                output.Add(xform.Transform(
                    new Point(Math.Cos(i / 180.0 * Math.PI) * (wid / 2.0) + (wid / 2.0), Math.Sin(i / 180.0 * Math.PI) * (hgt / 2.0) + (hgt / 2.0))));
            output.Add(output[0]);

            return output;
        }
        /// <summary>
        /// Adds the best fitting straight line to an input stroq
        /// </summary>
        /// <param name="l"></param>
        static List<Point> createLine(out ShapeType shapeType, Stroq s)
        { 
            shapeType = ShapeType.None;
            List<Point> pts = new List<Point>();
            pts.Add(s[0]);
            for (int i = 1; i < s.Cusps().Length; i++)
            {
                if (s.Cusps().Straightness(i-1, i) > 0.18)
                {
                    int prevInd = s.Cusps().CuspIndex(i - 1);
                    var prevPt = s.Cusps()[i - 1].pt;
                    int curInd = s.Cusps().CuspIndex(i);
                    var curPt = s.Cusps()[i].pt;
                    insertCorner(s, pts, prevInd, prevPt, curInd, curPt); // bcz: note, this is not recursive - only 1 corner will be added
                }
                pts.Add(s.Cusps()[i].pt);
            }

            List<Point> output = new List<Point>();

            output.AddRange(pts);
            shapeType = output.Count < 3 ? ShapeType.StraightLine : ShapeType.Polyline;

            return output;
        }

        /// <summary>
        /// attempts to classify an input stroke as one of a set of shape families
        /// </summary>
        /// <param name="s"></param>
        /// <returns>whether a shape was matched</returns>
        public static string           RecognizeShapeHint(Stroq s, bool fromPolylines) {

            if (s.Cusps().Straightness() < 0.18 && (s.Count < 3 || s.Count > 5))
                return "straightLine";

            List<string> strings = new List<string>(
                new string[] { 
                    ShapeResources.circle,  
                    ShapeResources.rect_1, ShapeResources.rect_2, ShapeResources.rect_3, ShapeResources.rect_4,  
                    ShapeResources.trapezoid, ShapeResources.trapezoid_2, ShapeResources.trapezoid_3, ShapeResources.trapezoid_4,
                    ShapeResources.parallelogram, 
                    ShapeResources.parallelogram_1, 
                    ShapeResources.parallelogram_2, 
                    ShapeResources.parallelogram_3, 
                    ShapeResources.parallelogram_4, 
                    ShapeResources.parallelogramR_5, 
                    ShapeResources.parallelogramR_6, 
                    ShapeResources.parallelogramR_7, 
                    ShapeResources.parallelogramR_8, 
                    ShapeResources.parallelogramR_9, 
                    ShapeResources.triangle_2,  ShapeResources.triangle_1,  ShapeResources.triangle });

            var rec = (DollarTester.DollarRec)s.Dollar("shapes", new List<string>(strings), 0.7, true);
            string recog = rec._rec;

            if ((!fromPolylines && s.Cusps().Length == 2) &&
                (recog.Contains("parallelogram") || recog.Contains("trapezoid")))
                recog = "rect";
            if (!fromPolylines && 
                s.Cusps().Length == 4 &&
                s.Cusps().Straightness(0, 1) < 0.18 &&
                s.Cusps().Straightness(1, 2) < 0.1 &&
                s.Cusps().Straightness(2, 3) < 0.18)
                recog = "triangle";
            else if (recog.Contains("trapezoid"))// || recog.Contains("parallelogram"))
                if (!fromPolylines && s.Cusps().Length == 3)
                    recog = "triangle";
            // shape isn't closed -- not a shape
            if (!recog.Contains("circle") && (s[0] - s[-1]).Length > Math.Max(s.GetBounds().Width, s.GetBounds().Height) * .25)
                return "";
            if (s.Count == 4)
                recog = "triangle";
            if (s.Count == 5)
                recog = "rect";
            return recog;
        }
        /// <summary>
        /// Tests if the Stroq matches a shape description and returns the corresponding shape's geometry
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static List<Point> Recognize(out ShapeType shapeType, Stroq s, string hint) {
            List<Point> output = new List<Point>();
            shapeType = ShapeType.None;
                 if (hint.Contains("triangle"))         output = createTriangle(out shapeType, s);
            else if (hint.Contains("rightTriangle"))    output = createTriangle(out shapeType, s);
            else if (hint.Contains("rect"))             output = createQuad(out shapeType, s, s.Cusps().Length > 3 ? 0 : 0.8);
            else if (hint.Contains("parallelogram"))    output = createQuad(out shapeType, s, 0);
            else if (hint.Contains("circle"))           output = createQuad(out shapeType, s, 0.85);
            else if (hint.Contains("trapezoid"))        output = createQuad(out shapeType, s, 0);
            else                                        output = createLine(out shapeType, s);

            return output;
        }
        public static List<Point> Recognize(out ShapeType shapeType, out List<BrownShape> usedBrownShapes, List<BrownShape> brownShapes)
        {
            List<BrownLine> lines = new List<BrownLine>();
            foreach (BrownShape bs in brownShapes)
            {
                if (bs.ShapeType == ShapeType.StraightLine || bs.ShapeType == ShapeType.Polyline)
                {
                    for (int i = 1; i < bs.ShapePoints.Length; i++)
                    {
                        lines.Add(new BrownLine(bs.ShapePoints[i - 1], bs.ShapePoints[i], bs));
                    }
                }
            }
            usedBrownShapes = new List<BrownShape>();
            var fig = constructFromLines(lines);
            if (fig != null)
            {
                foreach (BrownLine line in lines)
                {
                    if (!usedBrownShapes.Contains(line.Data as BrownShape))
                    {
                        usedBrownShapes.Add(line.Data as BrownShape);
                    }
                }
                return Recognize(out shapeType, new Stroq(fig), RecognizeShapeHint(new Stroq(fig), true));
            }
                
            shapeType = ShapeType.None;
            return new List<Point>();
        }
        /// <summary>
        /// Checks if the input stroq is a scribble gesture and returns the scribbled over elements
        /// </summary>
        /// <param name="s"></param>
        /// <param name="canvas"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        public static bool             RecognizeScribbleDelete(Stroq s, InqCanvas canvas, out SelectionObj res)
        {
            var stap = canvas != null ? new ScribbleTapCommand(canvas, false) : new ScribbleTapCommand(new StroqCollection(new Stroq[] { s }), false);
            res = stap.Fire(new Stroq[] { s }, "default");
            return !res.Empty;
        }

        public class SelectionObj
        {
            public SelectionObj()                                                    { Stroqs = new List<Stroq>(); Elements = new List<FrameworkElement>(); }
            public SelectionObj(List<Stroq> stroqs, List<FrameworkElement> elements) { Stroqs = stroqs; Elements = elements; }
            public List<Stroq>            Stroqs   { get; set; }
            public List<FrameworkElement> Elements { get; set; }
            public bool                   Empty    { get { return Stroqs.Count == 0 && Elements.Count == 0; } }
        }
        public class ScribbleTapCommand
        {
            InqCanvas _can = null;
            StroqCollection _stroqs = null;
            bool      _splitStrokes = false;
            bool      _lenient;

            object test(Stroq hull)
            {
                var hitMarks = new List<Stroq>();
                var hitRects = new List<FrameworkElement>();
                bool gotOne = false; // whether something is completely contained within the scribble
                List<Pt> hullPts = hull.Cusps().ScribblePts();
                SelectionObj sel = new SelectionObj();
                if (hullPts.Count > 4)
                {
                    double[] areas = new double[hullPts.Count - 2];
                    int[] trisUsed = new int[hullPts.Count - 2]; // initializes all counts to '0'
                    for (int i = 2; i < hullPts.Count; i++)
                        areas[i - 2] = GeomUtils.SignedArea(hullPts[i - 2], hullPts[i - 1], hullPts[i]);
                    foreach (Stroq m in _can == null ? _stroqs : _can.Stroqs)
                        if (m.GetBounds().IntersectsWith(hull.GetBounds()) &&   // if polygon bounds intersects .. and .. polygons vertices are contained
                            scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, m.Select((Pt p) => (Point)p).ToArray()))
                            hitMarks.Add(m);
                    if (_can != null)
                        foreach (var r in _can.Children)
                            if (!(r is Stroq) && r is FrameworkElement)
                            {
                                var f = r as FrameworkElement;
                                if (!double.IsNaN(f.Width) && (f is Shape || !f.RenderTransform.TransformBounds(new Rect(new Point(), new Size(f.Width, f.Height))).Contains(hull.GetBounds())))
                                    gotOne = testElement(hull, hitRects, gotOne, hullPts, areas, trisUsed, f);

                                else if (f is BrownShapeRenderer)
                                {
                                    var l = (f as BrownShapeRenderer).Polyline;
                                    for (int i = 1; i < l.Points.Count; i++)
                                    {
                                        if (scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, new Point[] { l.Points[i - 1], l.Points[i] }))
                                        {
                                            hitRects.Add(f);
                                            break;
                                        }
                                    }
                                }
                                
                                else if (f is Canvas)
                                {
                                    foreach (var mk in (f as Canvas).Children)
                                    {
                                        var mkf = mk as FrameworkElement;
                                        if (mkf.Tag == null)
                                            continue;
                                        if (mkf is Line)
                                        {
                                            Line l = mk as Line;
                                            if (scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, new Point[] { new Point(l.X1, l.Y1), new Point(l.X2, l.Y2) }))
                                                hitRects.Add(l);
                                        }
                                        else if (mkf is Polyline)
                                        {
                                            var l = mk as Polyline;
                                            for (int i = 1; i < l.Points.Count; i++)
                                            {
                                                if (scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, new Point[] { l.Points[i-1], l.Points[i] }))
                                                {
                                                    hitRects.Add(l);
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!f.RenderTransform.TransformBounds(mkf.RenderTransform.TransformBounds(
                                                new Rect(new Point(), new Size(mkf.Width, mkf.Height)))).Contains(hull.GetBounds()))
                                                gotOne = testElement(hull, hitRects, gotOne, hullPts, areas, trisUsed, mk as FrameworkElement);
                                        }
                                    }
                                }
                                else
                                    if (f is Line)
                                    {
                                        Line l = f as Line;
                                        if (scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, new Point[] { new Point(l.X1, l.Y1), new Point(l.X2, l.Y2) }))
                                            hitRects.Add(l);
                                    }
                                    else if (f is Polyline)
                                    {
                                        var l = f as Polyline;
                                        for (int i = 1; i < l.Points.Count; i++)
                                        {
                                            if (scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, new Point[] { l.Points[i - 1], l.Points[i] }))
                                            {
                                                hitRects.Add(l);
                                                break;
                                            }
                                        }
                                    }
                            }

                    // test to see whether the start and end of the scribble are deleting anything
                    bool triStart, triEnd;
                    double triPercent = analyzeScribbleIntersections(trisUsed, out triStart, out triEnd, out triPercent);

                    if (gotOne || triPercent >= TriThreshold || (hitMarks.Count > 1 && triStart && triEnd))
                        sel = new SelectionObj(hitMarks, hitRects);
                }
                return sel;
            }
            bool   testElement(Stroq hull, List<FrameworkElement> hitRects, bool gotOne, List<Pt> hullPts, double[] areas, int[] trisUsed, FrameworkElement f)
            {
                if (f is Polyline && scribbleStrokeTest(hull, ref gotOne, hullPts.ToArray(), areas, trisUsed, (f as Polyline).Points.Select((p) => (Point)p).ToArray()))
                {
                    hitRects.Add(f);
                }
                else
                {
                    var eleBounds = f.RenderTransform.TransformBounds(new Rect(new Point(), new Size(f.Width, f.Height)));
                    if (f is Shape || eleBounds.IntersectsWith(hull.GetBounds()) && !eleBounds.Contains(hull.GetBounds()))
                        if (// if stroq bounds intersects but doesn't contain element bounds ...
                        elementHitTest(hullPts, areas, trisUsed, f, ref gotOne))          //  and... element test passes
                            hitRects.Add(f);
                }
                return gotOne;
            }
            
            /// <summary>
            /// crates a ScribbleTap Command
            /// </summary>
            /// <param name="can"></param>
            /// <param name="oneStroke">true if no tap is needed</param>
            /// <param name="lenient">specifies whether scribbles with > 6 cusps that intersect something are automatically delete scribbles</param>
            public ScribbleTapCommand(InqCanvas can, bool lenient) { _can = can; _lenient = lenient; _splitStrokes = false; }
            public ScribbleTapCommand(StroqCollection stroqs, bool lenient) { _stroqs = stroqs; _lenient = lenient; _splitStrokes = false; }

            public bool         SplitStrokes {
                get { return _splitStrokes; }
                set { _splitStrokes = value; }
            }
            
            public SelectionObj Fire(Stroq[] strokes, object device) { 
                return Test1(strokes[0], "default") ? (SelectionObj)strokes[0].ScribbledOver(test) : new SelectionObj(); 
            }
            public bool         Test1(Stroq s, object device)        { 
                return !((SelectionObj)s.ScribbledOver(test)).Empty || 
                    (_lenient && s.Cusps().Length > 6 && 
                    _can.Stroqs.HitTest(s.Select<Pt, Pt>((Pt p) => p), new RectangleStylusShape(1,1)).Count > 0); 
            }
            #region Scribble Details
            public double TriThreshold = .75; // set to .4 or lower if gestures are likely
            // bcz: Hack!  avoids problems when intersection point between two lines becomes a segment because of numerical error
            void addIfNotTooSmall(ref List<Stroq> added, Stroq s)
            {
                if (s.GetBounds().MaxDim > 2)
                    added.Add(s);
            }
            Stroq stroqFromRange(Stroq s, float[] range)
            {
                List<Pt> rangePts = new List<Pt>();
                rangePts.Add(s[range[0]]);
                for (int i = (int)Math.Ceiling(range[0]); i < Math.Floor(range[1]); i++)
                    rangePts.Add(s[i]);
                rangePts.Add(s[range[1]]);
                return new Stroq(rangePts);
            }
            bool intervalContains(float start, float end, float[] scribInts)
            {
                foreach (float scribint in scribInts)
                    if (scribint > start && scribint < end)
                        return true;
                return false;
            }
            bool lineIntersectsTri(Pt a, Pt b, Pt[] hullTris, double area, int ind)
            {
                LnSeg ab = new LnSeg(a, b);
                return ab.Intersection(new LnSeg(hullTris[ind], hullTris[ind + 1])) != null ||
                    ab.Intersection(new LnSeg(hullTris[ind + 1], hullTris[ind + 2])) != null ||
                    ab.Intersection(new LnSeg(hullTris[ind], hullTris[ind + 2])) != null ||
                    pointInTri(a, hullTris, area, ind); // If line segment is inside triangle
            }
            bool pointInTri(Pt a, Pt[] hullTris, double area, int ind)
            {
                double u = GeomUtils.SignedArea(a, hullTris[ind + 1], hullTris[ind + 2]);
                double v = GeomUtils.SignedArea(hullTris[ind], a, hullTris[ind + 2]);
                double w = area - u - v;
                if (u < 0 && v < 0 && w < 0 && area < 0 && -u - v - w <= -area)
                    return true;
                if (u > 0 && v > 0 && w > 0 && area > 0 && u + v + w <= area)
                    return true;
                return false;
            }
            bool scribbleStrokeTest(Stroq hull, ref bool gotOne, Pt[] hullPts, double[] areas, int[] trisused, Point[] mpts)
            {
                List<Stroq> mstrokeList = new List<Stroq>();
                int inside = 0;
                bool intersects = hull.BackingStroke.HitTest(mpts, new RectangleStylusShape(2, 2));
                for (int i = 0; (!gotOne || inside == 0) && i < mpts.Length; i++)
                {
                    // need to do short circuit bbox test of line vs. triangle
                    // first, get bounding box of line
                    Rct lineBox = new Rct(mpts[i], new Vec());
                    bool testLineSeg = false;
                    if (i != mpts.Length - 1)
                    {
                        lineBox = lineBox.Union(mpts[i + 1]);
                        testLineSeg = true;
                    }
                    bool ptConsumed = false;
                    for (int tri = 0; tri < hullPts.Length - 2; tri++)
                    {
                        Rct triBbox = new Rct(hullPts[tri], new Vec());
                        triBbox = triBbox.Union(new Rct(hullPts[tri + 1], new Vec()));
                        triBbox = triBbox.Union(new Rct(hullPts[tri + 2], new Vec()));
                        if (lineBox.IntersectsWith(triBbox))
                        {
                            bool ptInTri = pointInTri(mpts[i], hullPts, areas[tri], tri);
                            if ((!testLineSeg && ptInTri) ||
                                            (testLineSeg &&
                                            lineIntersectsTri(mpts[i], mpts[i + 1], hullPts, areas[tri], tri)))
                            {
                                intersects = true;
                                if (ptInTri && !ptConsumed)
                                {
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
            public static Pt[] GetOutline(FrameworkElement elt, FrameworkElement parent)
            {
                if (parent == null)
                    return new Pt[0];

                elt.UpdateLayout();
                GeneralTransform trans = new MatrixTransform(Mat.Identity);

                try
                {
                    trans = elt.TransformToAncestor(parent);
                }
                catch (System.InvalidOperationException ex)
                {
                }

                Pt[] bounds = new Pt[] { new Pt(), new Pt(elt.ActualWidth, 0), new Pt(elt.ActualWidth, elt.ActualHeight), new Pt(0, elt.ActualHeight) };
                for (int i = 0; i < bounds.Length; i++)
                    bounds[i] = trans.Transform(bounds[i]);
                return bounds;
            }
            bool elementHitTest(List<Pt> hullPts, double[] areas, int[] trisused, FrameworkElement r, ref bool gotOne)
            {
                double rhullarea;
                double coverage;
                Pt[] outlinePts = GetOutline(r, _can);
                rhullarea = GeomUtils.PolygonArea(outlinePts);
                coverage = 0;
                bool intersects = false;
                for (int tri = 0; tri < hullPts.Count() - 2; tri++)
                {
                    double tcover = areas[tri] > 0 ?
                        GeomUtils.PolygonArea(GeomUtils.ClipPolygonToTriangle(outlinePts, hullPts[tri], hullPts[tri + 1], hullPts[tri + 2])) :
                        GeomUtils.PolygonArea(GeomUtils.ClipPolygonToTriangle(outlinePts, hullPts[tri], hullPts[tri + 2], hullPts[tri + 1]));
                    coverage += tcover;
                    if (tcover / Math.Abs(areas[tri]) > 0.001)
                    {
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
            double analyzeScribbleIntersections(int[] trisUsed, out bool triStart, out bool triEnd, out double triPercent)
            {
                int trisHit = 0;
                triStart = false;
                triEnd = false;

                for (int tu = 0; tu < trisUsed.Length; tu++)
                {
                    if (trisUsed[tu] != 0)
                    {
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

        static Guid IS_RECTANGLE = new Guid("4540316A-9189-41FB-90E2-56CE9F01A487");
        /// <summary>
        /// Extend a Stroq to have an IsRectangle property
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        static public bool IsRectangle(this Stroq stroke) {
            if (!stroke.Property.Exists(IS_RECTANGLE)) {
                var fd = new starPadSDK.CharRecognizer.FeaturePointDetector();
                bool rect = fd.match_rect(fd.FeaturePoints(stroke.OldStroke()));
                stroke.Property[IS_RECTANGLE] = rect;
            }
            return (bool)stroke.Property[IS_RECTANGLE];
        }
    }
}
