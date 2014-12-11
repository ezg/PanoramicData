using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ink;

namespace starPadSDK.CharRecognizer
{
	/// <summary>
	/// Functions to treat points as 2D vectors.
	/// </summary>
	public class V2D
	{
		public static PointF Normalize(PointF v)
		{
			float l = Length(v);
			if(l == 0)
			{
                return v;
				//throw new System.DivideByZeroException("Attempt to normalize a zero-length vector!");
			}
            v.X/=l;
			v.Y/=l;

			return v;
        }
        public static double Straightness(Point[] pts, out bool left) { return Straightness(pts, 0, pts.Length, out left); }
        public static double Straightness(Point[] pts) { return Straightness(pts, 0, pts.Length); }
        
        public static double Straightness(Point[] pts, int start, int end, out bool left) { return Straightness(pts, start, end, Dist(pts[start], pts[Math.Min(pts.Length-1,end)]), out left); 
        }
        public static double Straightness(Point[] pts, int start, int end) { bool left;  return Straightness(pts, start, end, out left); }
        public static double Straightness(Point[] pts, int start, int end, double dist) { bool left; return Straightness(pts, start, end, dist, out left); }
        public static double Straightness(Point[] pts, int start, int end, double dist, out bool left) {
            return Straightness(pts,V2D.Sub(pts[Math.Min(pts.Length-1,end)], pts[start]), start, end, dist, out left);
        }
        public static double Straightness(Point[] pts, Point dir, int start, int end, double dist, out bool left) {
            return MaxDist(pts, V2D.Normalize(dir), out left, start, end)/dist;
        }
        public static double Straightness(Point[] pts, Point baseP, Point dir, int start, int end, double dist) {
            bool left;
            return Straightness(pts, dir, start, end, dist, out left);
        }
        public static double MaxDist(Point[] pts, PointF dir, out bool left, out int index, int start, int end) {
            return MaxDist(pts, pts[start], dir, out left, out index, start, end);
        }
        public static double MaxDist(Point[] pts, Point baseP, PointF dir, out bool left, out int index, int start, int end) {
            double farthest =0, farleft = 0, farright = 0;
            left = false;
            index = start;
            for (int i = start+1; i < end-1; i++) {
                PointF near = V2D.Add(baseP, V2D.Mul(dir, V2D.Dot(V2D.Sub(pts[i],baseP), dir)));
                PointF offset = V2D.Sub(pts[i], near);
                double dist = V2D.Length(offset);
                if (dist > farleft && offset.X < 0)
                    farleft = dist;
                if (dist > farright && offset.X > 0)
                    farright =dist;
                if (dist > farthest) {
                    farthest= dist;
                    index = i;
                    left = offset.X < 0;
                }
            }
            return Math.Max(farthest, farleft+farright);
        }
        public static double MaxDist(Point[] pts, PointF dir, out bool left, int start, int end) { int ind;  return MaxDist(pts, dir, out left, out ind, start, end); }
        public static double MaxDist(Point[] pts, PointF dir, out bool left) { return MaxDist(pts, dir, out left, 0, pts.Length); }
        public static double MaxDist(Point[] pts, PointF dir, out int ind) { bool left;  return MaxDist(pts, dir, out left, out ind, 0, pts.Length); }
        public static float Length(PointF v)
		{
			return (float)Math.Sqrt(v.X*v.X + v.Y*v.Y);
		}

		public static float Dot(PointF v1, PointF v2)
		{
			return v1.X*v2.X + v1.Y*v2.Y;
		}

        public static float Dot(Point v1, PointF v2) {
            return v1.X*v2.X + v1.Y*v2.Y;
        }
		public static float Angle(PointF v1, PointF v2)
		{
			v1 = Normalize(v1);
			v2 = Normalize(v2);
            float dot = Dot(v1, v2) ;
			// Make sure we don't have rounding error
			dot = (float)(Math.Max(-1.0,Math.Min(1.0,dot)));
            return (float)Math.Acos(dot)* ((v1.X * v2.Y - v1.Y * v2.X) > 0 ? 1 :-1);
		}

		public static PointF Normalize(Point v)
		{
			return Normalize(new PointF(v.X,v.Y));
		}

		public static double Length(Point v)
		{
			return Math.Sqrt(v.X*v.X + v.Y*v.Y);
		}
        public static Point Add(Point v1, Point v2) { return new Point(v1.X+v2.X, v1.Y+v2.Y); }
        public static Point Mul(Point v1, float mul) { return new Point((int)(v1.X*mul), (int)(v1.Y*mul)); }
        public static Point Mul(PointF v1, float mul) { return new Point((int)(v1.X*mul), (int)(v1.Y*mul)); }
        public static Point Sub(Point v1, Point v2) { return new Point(v1.X-v2.X, v1.Y-v2.Y); }
        public static PointF Sub(PointF v1, PointF v2) { return new PointF(v1.X-v2.X, v1.Y-v2.Y); }
		public static double Dist(Point p1, Point p2) {
			return Math.Sqrt((p1.X-p2.X)*(p1.X-p2.X) + (p1.Y-p2.Y)*(p1.Y-p2.Y));
		}
		public static Point  Interp(Point p1, Point p2, double interp) {
			Point vec = new Point(p2.X-p1.X, p2.Y-p1.Y);
			return new Point((int)(vec.X *interp + p1.X), (int)(vec.Y * interp + p1.Y));
		}
		public static double Angle(Point v1, Point v2)
		{
			PointF v1F = Normalize(v1);
			PointF v2F = Normalize(v2);
            double dot = Dot(v1F, v2F) ;
			// Make sure we don't have rounding error
			dot = (Math.Max(-1.0,Math.Min(1.0,dot)));
            return Math.Acos(dot)* ((v1F.X * v2F.Y - v1F.Y * v2F.X) > 0 ? 1 :-1);
        }
        class PointComparer : IComparer {
            public int Compare(object x, object y) {
                if (!(x is Point) || !(y is Point)) return -1;
                Point a = (Point)x;
                Point b = (Point)y;
                if (a.X < b.X) return -1;
                if (a.X == b.X) {
                    if (a.Y < b.Y) return -1;
                    if (a.Y == b.Y) return 0;
                    return 1;
                }
                return 1;
            }

        }
        public static double Det(Point veca, Point vecb) { return veca.X*vecb.Y - veca.Y*vecb.X; }
        public static double Det(Point a, Point apex, Point b) { return Det(V2D.Sub(a,apex),V2D.Sub(b,apex)); }
        private static double signedArea(Point a, Point b, Point c) {
            return 0.5 * Det(new Point(b.X-a.X, b.Y-a.Y), new Point(c.X-a.X, c.Y-a.Y));
        }
        protected static void eliminate(ref ArrayList pts) {
            int i = 0;
            while (i < pts.Count) {
                if (2 * signedArea((Point)pts[i], (Point)pts[(i+1) % pts.Count], (Point)pts[(i+2) % pts.Count]) >= 0) {
                    pts.RemoveAt((i+1) % pts.Count);
                    if (i > 0) i--;
                } else i++;
            }
        }
        public static Point[] ConvexHull(Point[] inPoints) {
            SortedList pts = new SortedList(new PointComparer(), inPoints.Length);
            for (int i = 0; i < inPoints.Length; i++) {
                if (!pts.ContainsKey(inPoints[i])) {
                    pts.Add(inPoints[i], inPoints[i]);
                }
            }
            Point left = (Point)pts.GetByIndex(0);
            Point right = (Point)pts.GetByIndex(pts.Count-1);
            ArrayList lower = new ArrayList();
            ArrayList upper = new ArrayList();
            lower.Add(left);
            upper.Add(left);
            for (int i = 0; i < pts.Count; i++) {
                double det = 2 * signedArea(left, right, (Point)pts.GetByIndex(i));
                if (det > 0)
                    upper.Add(pts.GetByIndex(i));
                else if (det < 0)
                    lower.Insert(0, pts.GetByIndex(i));
            }
            lower.Insert(0, right);
            upper.Add(right);
            eliminate(ref lower);
            eliminate(ref upper);
            Point[] res = new Point[lower.Count + upper.Count];
            lower.CopyTo(res, 0);
            upper.CopyTo(res, lower.Count);
            return res;
        }

        public static Point[] StrokeHull(Stroke s) {
            Point[] pts = s.GetPoints();
            int len = pts.Length;
            Point[] ret = new Point[2 * len+1];
            for (int i = 0; i < len; i++) 
                ret[i] = new Point(pts[i].X,pts[i].Y-1);
            for (int i = len; i < 2*len; i++)
                ret[i] = new Point(pts[2 * len - i-1].X, pts[2 * len - i-1].Y + 1);
            ret[2 * len] = pts[0];
            return ret;
        }
    }
}
