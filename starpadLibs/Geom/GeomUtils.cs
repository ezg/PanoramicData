using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace starPadSDK.Geom
{
    public class ContactArea
    {
        List<Pt> _pts;
        Mat _transform;
        public ContactArea(List<Pt> pts, Mat transform) { _pts = pts; _transform = transform; }
        public Rct Bounds
        {
            get
            {
                Rct bounds = Rct.Null;
                foreach (Pt p in _pts)
                    bounds = bounds.Union(_transform * p);
                return bounds;
            }
        }
        public void Translate(Vec v)
        {
            _transform = _transform * Mat.Translate(v);
        }
        public ContactArea Translated(Vec v)
        {
            return new ContactArea(_pts, _transform * Mat.Translate(v));
        }

    }
    public static class GeomUtils {
        /* Basic */
        /// <summary>
        /// Compute the signed (right-handed) area of the given triangle. The sign is positive if a,b,c are counterclockwise in mathematical space
        /// and thus clockwise on the screen.
        /// </summary>
        public static double SignedArea(Pt a, Pt b, Pt c) { return 0.5 * (b-a).Det(c-a); }
        
        /* Convex hull */
        public class LexicographicPointComparer : IComparer<Pt> {
            public int Compare(Pt a, Pt b) {
                if(a.X < b.X) return -1;
                if(a.X == b.X) {
                    if(a.Y < b.Y) return -1;
                    if(a.Y == b.Y) return 0;
                    return 1;
                }
                return 1;
            }
        }
        private static void eliminate(ref List<Pt> pts) {
            int i = 0;
            while(i < pts.Count) {
                if(2 * SignedArea(pts[i], pts[(i+1) % pts.Count], pts[(i+2) % pts.Count]) >= 0) {
                    pts.RemoveAt((i+1) % pts.Count);
                    if(i > 0) i--;
                } else i++;
            }
        }
        public static IEnumerable<Pt> ConvexHull(this IEnumerable<Pt> inPoints) {
            SortedList<Pt, Pt> pts = new SortedList<Pt, Pt>(new LexicographicPointComparer());
            foreach(Pt p in inPoints)
                if(!pts.ContainsKey(p))
                    pts.Add(p, p);
            Pt left =  pts.Values[0];
            Pt right = pts.Values[pts.Values.Count-1];
            List<Pt> lower = new List<Pt>();
            List<Pt> upper = new List<Pt>();
            lower.Add(left);
            upper.Add(left);
            foreach(Pt p in pts.Values) {
                double det = 2 * SignedArea(left, right, p);
                if(det > 0)
                    upper.Add(p);
                else if(det < 0)
                    lower.Insert(0, p);
            }
            lower.Insert(0, right);
            upper.Add(right);
            eliminate(ref lower);
            eliminate(ref upper);
            return lower.Concat(upper);
        }
        public static double PolygonArea(IEnumerable<Pt> hull) {
            double area = 0;
            Pt o = new Pt(); // compiler is unable to prove we don't need to initialize this here
            Vec a = new Vec(); // compiler is unable to prove we don't need to initialize this here
            int i = 0;
            foreach (Pt b in hull) {
                if (i == 0)
                    o = b;
                else if (i == 1)
                    a = b - o;
                else {
                    Vec v = b - o;
                    area += a.Perp().Dot(v) / 2;
                    a = v;
                }
                i++;
            }
            return area;
        }
        public static IEnumerable<Pt> ClipPolygonToTriangle(IEnumerable<Pt> hull, Pt a, Pt b, Pt c) {
            return ClipPolygonToLine(a, b, ClipPolygonToLine(b, c, ClipPolygonToLine(c, a, hull)));
        }
        public static IEnumerable<Pt> ClipPolygonToLine(Pt a, Pt b, IEnumerable<Pt> pts) {
            Vec N = (b - a).Perp();
            double D = -N.Dot((Vec)a);
            bool first = true;
            Pt S = new Pt(), F = new Pt(); // compiler is unable to prove these don't need initialization here
            foreach (Pt P in pts) {
                double v1, v2;
                v2 = N.Dot((Vec)P) + D;
                if (first) {
                    S = P;
                    F = P;
                    first = false;
                }
                else {
                    v1 = N.Dot((Vec)S) + D;
                    if (v1 * v2 < 0) {
                        yield return S + (P - S) * v1 / (v1 - v2);
                    }
                    S = P;
                }
                if (v2 >= 0) yield return P;
            }
            if (!first) {
                double v1, v2;
                v2 = N.Dot((Vec)F) + D;
                v1 = N.Dot((Vec)S) + D;
                if (v1 * v2 < 0) {
                    yield return S + (F - S) * v1 / (v1 - v2);
                }
            }
        }
        public static Pt[] ToPointList(Rct r)
        {
           return new Pt[5] { new Pt(r.Left, r.Top), new Pt(r.Right, r.Top),
                              new Pt(r.Right, r.Bottom), new Pt(r.Left, r.Bottom),
                              new Pt(r.Left, r.Top) };
        }
        public static Rct Bounds(IEnumerable<Pt> pc) {
            return Bounds(pc, Mat.Identity);
        }
        public static Rct Bounds(IEnumerable<Pt> pc, Mat xform) {
            Rct bounds = Rct.Null;
            foreach (Pt p in pc)
                bounds = bounds.Union(xform  * p);
            return bounds;
        }
    }
}
