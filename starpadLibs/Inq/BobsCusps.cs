using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Ink;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq.MSInkCompat;

namespace starPadSDK.Inq {
    namespace BobsCusps {
        static public class CuspAccessor {
            static Guid CUSP_GUID = new Guid("3a9be4f5-37c1-44ab-865f-43d6e253f185");
            static public Cusps Cusps(this Stroq stroke) {
                if (!stroke.Property.Exists(CUSP_GUID))
                    stroke.Property[CUSP_GUID] = new BobsCusps.Cusps(stroke);
                if (((Cusps)stroke.Property[CUSP_GUID])[((Cusps)stroke.Property[CUSP_GUID]).Length - 1].index != stroke.Count - 1)
                    stroke.Property[CUSP_GUID] = new BobsCusps.Cusps(stroke);
                return (Cusps)stroke.Property[CUSP_GUID]; 
            }
        }
        public class Cusps {
            FeaturePointDetector.CuspSet _cusps = null;
            public Cusps(Stroq s) { _cusps = FeaturePointDetector.FeaturePoints(s); }
            public int        Length { get { return _cusps.cusps.Count; } }
            public LnSeg      inSeg(int i) { return new LnSeg(_cusps.cusps[i-1].pt, _cusps.cusps[i].pt); }
            public LnSeg      outSeg(int i) { return new LnSeg(_cusps.cusps[i].pt, _cusps.cusps[i+1].pt); }
            public double     Distance { get { return _cusps.dist; } }
            public IList<int> SelfIntersects { get { return _cusps.intersects; } }
            public FeaturePointDetector.CuspRec this[int i] { get { return i >= 0 ? _cusps.cusps[i] : _cusps.cusps[_cusps.cusps.Count+i]; } }
            public int        CuspIndex(int i) { return FeaturePointDetector.convertIndex(_cusps.cusps[i].index, _cusps.skipped); }
            public double     Straightness() { return _cusps.straight; }
            public double     Straightness(int a, int b) {
                return V2D.Straightness(_cusps.pts, _cusps.cusps[a].index, _cusps.cusps[b< 0 ? _cusps.cusps.Count()+b:b].index);
            }
            public double PtStraightness(int a, int b) { return V2D.Straightness(_cusps.s, a, b); }
            public Pt Farthest(int a, int b)
            {
                bool left;
                int ind;
                V2D.MaxDist(_cusps.pts, _cusps.cusps[a].pt, (_cusps.cusps[a].pt - _cusps.cusps[b].pt).Normal(), out left, out ind);
                return ind != -1 ? _cusps.pts[ind] : _cusps.pts[a];
            }
            public Pt Farthest(Pt b)
            {
                bool left;
                int ind;
                V2D.MaxDist(_cusps.pts, _cusps.cusps[0].pt, (_cusps.cusps[0].pt - b).Normal(), out left, out ind);
                return ind != -1 ? _cusps.pts[ind] : _cusps.cusps[0].pt;
            }
            public Pt Farthest(Pt a, Pt b)
            {
                bool left;
                int ind;
                V2D.MaxDist(_cusps.pts, a, (a - b).Normal(), out left, out ind);
                return ind != -1 ? _cusps.pts[ind] : _cusps.cusps[0].pt;
            }
            public List<Pt> ScribblePts() {
                List<Pt> hullPts = new List<Pt>();
                hullPts.Add(this[0].pt);
                int numPos = 0;
                int numNeg = 0;
                for (int i = 1; i < Length - 1; i++) {
                    double ang = outSeg(i).Direction.SignedAngle(-inSeg(i).Direction);
                    if (ang > 0)
                        numPos++;
                    else numNeg++;
                    if (true)//Math.Abs(ang) < Math.PI / 2)  //bcz:  Sigh ... this makes us miss a lot of deletions on the Surface
                        hullPts.Add(this[i].pt);
                }
                hullPts.Add(this[-1].pt);
                if (numPos == 0 || numNeg == 0)
                    hullPts.Clear();
                return hullPts;
            }
        }
        /// <summary>
        /// This class exists purely to provide some utility geometric functions for the feature point detector. Anything that becomes useful to something outside
        /// of this file should be (cleaned up and) moved into a geometric utilities file in the main Points folder. As a result, comments will be sparser
        /// in this class than usual.
        /// </summary>
        internal static class V2D {
            public static double Straightness(IList<Pt> pts, out bool left) { return Straightness(pts, 0, pts.Count, out left); }
            public static double Straightness(IList<Pt> pts) { return Straightness(pts, 0, pts.Count); }

            public static double Straightness(IList<Pt> pts, int start, int end, out bool left) {
                return Straightness(pts, start, end, (pts[start] - pts[Math.Min(pts.Count-1, end)]).Length, out left);
            }
            public static double Straightness(IList<Pt> pts, int start, int end) { bool left; return Straightness(pts, start, end, out left); }
            public static double Straightness(IList<Pt> pts, int start, int end, double dist) { bool left; return Straightness(pts, start, end, dist, out left); }
            public static double Straightness(IList<Pt> pts, int start, int end, double dist, out bool left) {
                return Straightness(pts, pts[Math.Min(pts.Count-1, end)] - pts[start], start, end, dist, out left);
            }
            /// <summary>
            /// Return the width of a set of points along a given direction divided by the distance passed in. Also return whether the furthest point from
            /// the start point is to the left of the start point.
            /// </summary>
            /// <param name="pts">The set of points to subset with start and end to find the set of points actually used.</param>
            /// <param name="dir">The direction along which to take the length.</param>
            /// <param name="start">The index of the first point from pts to consider.</param>
            /// <param name="end">Two past the index of the last point to consider.</param>
            /// <param name="dist">The distance to scale the computed distance by.</param>
            /// <param name="left">Whether the furthest point from the start point is to its left.</param>
            public static double Straightness(IList<Pt> pts, Vec dir, int start, int end, double dist, out bool left) {
                return MaxDist(pts, dir.Normalized(), out left, start, end)/dist;
            }
            public static double Straightness(IList<Pt> pts, Pt baseP, Vec dir, int start, int end, double dist) {
                bool left;
                return Straightness(pts, dir, start, end, dist, out left);
            }
            public static double MaxDist(IList<Pt> pts, Vec dir, out bool left, out int index, int start, int end) {
                return MaxDist(pts, pts[start], dir, out left, out index, start, end);
            }
            public static double MaxDist(IEnumerable<Pt> pts, Pt baseP, Vec dir, out bool left, out int index, int start, int end) {
                double dist = MaxDist(pts.Skip(start+1).Take(end-start-2), baseP, dir, out left, out index);
                index += start+1;
                return dist;
            }
            /// <summary>
            /// Consider the projection of a set of points onto a line from a given point in a given direction.
            /// Return the maximum distance of a point in that projected set from the given point, or, if the given point is in the middle of the projected
            /// set and the projection line is not vertical, the width along the line of the projected set. Also return the index of the projected point furthest from
            /// the given point, and whether that point is to the left of the given point.
            /// </summary>
            /// <param name="pts">The set of points to find the furthest or width of.</param>
            /// <param name="baseP">The given origin point.</param>
            /// <param name="dir">The direction to take the distance along. Should be unit length.</param>
            /// <param name="left">Was the furthest point to the left?</param>
            /// <param name="index">The index of the furthest point.</param>
            public static double MaxDist(IEnumerable<Pt> pts, Pt baseP, Vec dir, out bool left, out int index) {
                double farthest = 0, farleft = 0, farright = 0;
                left = false;
                index = -1;
                int i = -1;
                foreach(Pt p in pts) {
                    i++;
                    Pt near = baseP + dir*(p - baseP).Dot(dir);
                    Vec offset = p - near;
                    double dist = offset.Length;
                    if(dist > farleft && offset.X < 0)
                        farleft = dist;
                    if(dist > farright && offset.X > 0)
                        farright =dist;
                    if(dist > farthest) {
                        farthest= dist;
                        index = i;
                        left = offset.X < 0;
                    }
                }
                return Math.Max(farthest, farleft+farright);
            }
            public static double MaxDist(IList<Pt> pts, Vec dir, out bool left, int start, int end) { int ind; return MaxDist(pts, dir, out left, out ind, start, end); }
            public static double MaxDist(IList<Pt> pts, Vec dir, out bool left) { return MaxDist(pts, dir, out left, 0, pts.Count); }
            public static double MaxDist(IList<Pt> pts, Vec dir, out int ind) { bool left; return MaxDist(pts, dir, out left, out ind, 0, pts.Count); }
            /// <summary>
            /// Return the arc length of the piecewise-linear path following the given sequence of points.
            /// </summary>
            public static double Arclen(IEnumerable<Pt> pts) {
                return pts.ByPairs().Sum((p) => (p.First-p.Second).Length);
            }

            private class PtComparer : IComparer<Pt> {
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
            private static double signedArea(Pt a, Pt b, Pt c) {
                return 0.5 * (b-a).Det(c-a);
            }
            private static void eliminate(ref List<Pt> pts) {
                int i = 0;
                while(i < pts.Count) {
                    if(2 * signedArea(pts[i], pts[(i+1) % pts.Count], pts[(i+2) % pts.Count]) >= 0) {
                        pts.RemoveAt((i+1) % pts.Count);
                        if(i > 0) i--;
                    } else i++;
                }
            }
            public static IEnumerable<Pt> ConvexHull(IEnumerable<Pt> inPoints) {
                SortedList<Pt, Pt> pts = new SortedList<Pt, Pt>(new PtComparer());
                foreach(Pt p in inPoints) {
                    if(!pts.ContainsKey(p)) pts.Add(p, p);
                }
                Pt left = pts.Values[0];
                Pt right = pts.Values[pts.Count-1];
                List<Pt> lower = new List<Pt>();
                List<Pt> upper = new List<Pt>();
                lower.Add(left);
                upper.Add(left);
                for(int i = 0; i < pts.Count; i++) {
                    double det = 2 * signedArea(left, right, pts.Values[i]);
                    if(det > 0)
                        upper.Add(pts.Values[i]);
                    else if(det < 0)
                        lower.Insert(0, pts.Values[i]);
                }
                lower.Insert(0, right);
                upper.Add(right);
                eliminate(ref lower);
                eliminate(ref upper);
                return lower.Concat(upper);
            }
            public static List<int> SpeedCusps(Pt[] inkPts) {
                if(inkPts.Length < 2)
                    return new List<int> { 0 };
                List<double> speed = new List<double>(inkPts.Length);
                int timeSkip = 1;
                speed.Add(0);
                speed.AddRange(inkPts.ByPairs().Select((sp) => (sp.Second-sp.First).Length).ByPairs().Select((lp) => (lp.Second-lp.First)/timeSkip));
                speed.Add(speed[speed.Count-1]);

                double avg = speed.Take(speed.Count-1).Average();
                List<int> zeros = new List<int>();
                bool above = true;
                double avgCut = avg * 4/5;
                for(int i=0; i < speed.Count-1; i++) {
                    if(!above && speed[i] < avgCut && speed[i+1] >= avgCut) {
                        zeros.Add(i);
                        above  = true;
                    }
                    if(above && speed[i] > avgCut && speed[i+1] <= avgCut) {
                        above = false;
                        zeros.Add(i);
                    }
                }
                List<int> speedCusps = new List<int>();
                speedCusps.Add(0);  // always include the first point
                zeros.Add(speed.Count - 2);
                for(int i=0; i < zeros.Count; i+=2) {
                    int end = i+1 < zeros.Count ? zeros[i+1] : speed.Count;
                    int mix;
                    speed.Skip(zeros[i]).Take(end-zeros[i]).Min(Double.PositiveInfinity, out mix);
                    speedCusps.Add(mix + zeros[i]);
                }
                return speedCusps;
            }
            static public double minx(int start, int end, IEnumerable<Pt> inkPts) {
                return inkPts.Skip(start).Take(end-start).Min((p) => p.X);
            }
            static public double minx(int start, int end, IEnumerable<Pt> inkPts, out int ind) {
                double min = inkPts.Skip(start).Take(end-start).Select((p) => p.X).Min(Double.PositiveInfinity, out ind);
                ind += start;
                return min;
            }
            static public double maxx(int start, int end, IEnumerable<Pt> inkPts) {
                return inkPts.Skip(start).Take(end-start).Max((p) => p.X);
            }
            static public double maxx(int start, int end, IEnumerable<Pt> inkPts, out int ind) {
                double min = inkPts.Skip(start).Take(end-start).Select((p) => p.X).Max(Double.NegativeInfinity, out ind);
                ind += start;
                return min;
            }
            static public double miny(int start, int end, IEnumerable<Pt> inkPts) {
                return inkPts.Skip(start).Take(end-start).Min((p) => p.Y);
            }
            static public double miny(int start, int end, IEnumerable<Pt> inkPts, out int ind) {
                double min = inkPts.Skip(start).Take(end-start).Select((p) => p.Y).Min(Double.PositiveInfinity, out ind);
                ind += start;
                return min;
            }
            static public double maxy(int start, int end, IEnumerable<Pt> inkPts) {
                return inkPts.Skip(start).Take(end-start).Max((p) => p.Y);
            }
            static public double maxy(int start, int end, IEnumerable<Pt> inkPts, out int ind) {
                double min = inkPts.Skip(start).Take(end-start).Select((p) => p.Y).Max(Double.NegativeInfinity, out ind);
                ind += start;
                return min;
            }
            static public Deg angle(Pt p1, Pt p2, Vec vec) { return angle(p1 - p2, vec); }
            static public Deg angle(Vec v1, Vec v2) { return v1.UnsignedAngle(v2); }
        }
        public static class FeaturePointDetector {
            static private List<Pt> Uniquify(IList<Pt> inkPts, ref List<int> skipped, Rct bbox, int firstint) {
                double maxDim = Math.Max(bbox.Width, bbox.Height);
                List<Pt> realPts = new List<Pt>();
                int hook = firstint;
                int tailhook = inkPts.Count-1;
                int hooklimit = Math.Min(inkPts.Count/2, inkPts.Count - 5); // Math.Min(8, inkPts.Length/3);
                List<Pt> ppts = new List<Pt>();
                ppts.Add(inkPts[0]);
                double dist = 0;
                int duplicate = 0;
                for(int i =1; i <= hooklimit+duplicate/2; i++)
                    if(ppts[ppts.Count-1] == inkPts[i] || i < firstint) {
                        duplicate++;
                        continue;
                    } else {
                        dist += (inkPts[i-1]-inkPts[i]).Length;
                        if(dist > maxDim * .6)
                            break;
                        bool realHook = dist > maxDim * .4;
                        if(ppts.Count > 1) {
                            if(V2D.Straightness(ppts) > .5)
                                break;
                            Pt nextPt = inkPts[i+1];
                            int nextPtInd = i+1;
                            while(nextPt == ppts[ppts.Count-1] && nextPtInd > 0)
                                nextPt = inkPts[++nextPtInd];
                            double dirChange =  Math.Abs(DirectionChange(ppts[ppts.Count-2], ppts[ppts.Count-1], nextPt));
                            double maxDirChange = dist < 0.08 ? 1+(.75/(7-(Math.Min(6, dist)))) : 2.25;
                            if(dirChange > maxDirChange || realHook) {
                                double fwddist = 0;
                                int fwdind = i;
                                for(; fwdind<inkPts.Count-1; fwdind++) {
                                    fwddist += (inkPts[fwdind] - inkPts[fwdind+1]).Length;
                                    if(fwddist > dist)
                                        break;
                                }
                                Deg angHook = V2D.angle(inkPts[0], inkPts[i-1], inkPts[Math.Min(fwdind, inkPts.Count-1)] - inkPts[i-1]);
                                if(realHook) {
                                    if(angHook < 2.Deg() && dirChange > 1 && V2D.Straightness(inkPts, i-1, fwdind) < 0.07)
                                        hook = i-1;
                                } else
                                    if(dist < 0.01 || (angHook < (maxDim > 0.15 && dist < 0.04 ? 90.Deg() : (0.12/dist)*40.Deg()))) {
                                        hook = i-1;
                                    }
                            }
                        }
                        ppts.Add(inkPts[i]);
                        if(maxDim > 15 && ppts.Count > 6 && dist > maxDim * .6)
                            break;
                    }
                ppts.Clear();
                ppts.Add(inkPts[inkPts.Count-1]);
                dist = 0;
                for(int i = inkPts.Count-2; i >= Math.Max(hook+5, inkPts.Count/2); i--) {
                    if(ppts.Count > 0 && ppts[ppts.Count-1] == inkPts[i])
                        continue;
                    dist += (inkPts[i+1] - inkPts[i]).Length;
                    if(dist > .4*maxDim)
                        break;
                    ppts.Add(inkPts[i]);
                    if(ppts.Count > 1) {
                        if(V2D.Straightness(ppts) > .5)
                            break;
                        Pt prevPt = inkPts[i-1];
                        int prevPtInd = i-1;
                        while((prevPt == ppts[ppts.Count-1]) && prevPtInd > 0)
                            prevPt = inkPts[--prevPtInd];
                        double dampDist = Math.Max(6, Math.Min(maxDim / 2, 6));
                        double dirChange =  Math.Abs(DirectionChange(ppts[ppts.Count-2], ppts[ppts.Count-1], prevPt));
                        //double maxDirChange = dist < Math.Min(8, maxDim/2) ? Math.Max(1, (dampDist+1-(Math.Min(dampDist, dist)))) : 2.25;
                        double maxDirChange = dist < 8 ? 1+(.6/(7-(Math.Min(6, dist)))) : 2.25;
                        if(dirChange > maxDirChange) {
                            double backdist = 0;
                            int backind = i;
                            for(; backind>0; backind--) {
                                backdist += (inkPts[backind] - inkPts[backind+1]).Length;
                                if(backdist > dist)
                                    break;
                            }
                            Deg tailang = V2D.angle(inkPts[inkPts.Count-1], inkPts[i+1], inkPts[Math.Max(backind, 0)] - inkPts[i+1]);
                            if((dirChange > 2.75 && tailang < 90.Deg()) || tailang < Math.Max(25, Math.Max(15, (maxDim/2)/dist)).Deg()) {
                                tailhook = i;
                            }
                        }
                    }
                    if(maxDim > 0.15 && ppts.Count > 10) // bcz used to be 5 ... 10 for Joe .. how
                        // many points to search back to find a hook
                        break;
                }
                for(int x = 0; x < inkPts.Count; x++) {// filter out points at the same pixel location
                    if(x >= hook && x <= tailhook && 
                    (realPts.Count < 1 || realPts[realPts.Count-1] != inkPts[x]) &&
                    (realPts.Count < 2 || realPts[realPts.Count-2] != inkPts[x]) && 
                    (realPts.Count < 3 || realPts[realPts.Count-3] != inkPts[x]))
                        realPts.Add(inkPts[x]);
                    else skipped.Add(x);
                }
                if(realPts.Count == 1)
                    realPts.Add(realPts[0] + new Vec(1,1));
                return realPts;
            }
            static public bool _smoothInput = false;
            /// <summary>
            /// Flag for whether input needs to be smoothed.  Typically this is false unless a low-res
            /// digitizer like a Wacom tablet is used.
            /// </summary>
            static public bool SmoothInput { get { return _smoothInput; } set { _smoothInput = value; } }
            static public int convertIndex(int ind, IEnumerable<int> skipped) {
                int toadd = 0;
                foreach(int x in skipped)
                    if(x <= ind+toadd)
                        toadd++;
                    else
                        break;
                return ind + toadd;
            }
            static int convertIndexBack(int ind, IEnumerable<int> skipped) {
                int tosub = 0;
                foreach(int x in skipped)
                    if(x <= ind)
                        tosub++;
                return Math.Max(0, ind - tosub);
            }
            static int cuspCenter(IList<Rad> curvatures, List<int> cuspsToAvg) {
                Rad m = 0;
                int ind = 0;
                for(int a = cuspsToAvg[0]; a <= cuspsToAvg[cuspsToAvg.Count-1]; a++)
                    if(a == 0 || Math.Abs(curvatures[a]) > Math.Abs(m)) {
                        m = a == 0? 3.14.Rad() : curvatures[a];
                        ind = a;
                    }
                return ind;
            }
            public class CuspRec {
                public CuspRec(IList<Pt> pts, Rct bbox, IList<Rad> curvatures, IList<double> distances, int i) {
                    pt = pts[i];
                    if((pt.Y - bbox.Top)/bbox.Height > 0.65)
                        bot = true;
                    if((pt.Y - bbox.Top)/bbox.Height < 0.40)
                        top = true;
                    if((pt.X - bbox.Left)/bbox.Width > 0.75)
                        right = true;
                    if((pt.X - bbox.Left)/bbox.Width < 0.3)
                        left = true;
                    sign = Math.Sign(curvatures[i]);
                    curvature = curvatures[i];
                    dist = distances[i];
                    index = i;
                }
                /// <summary>
                /// The index of the point corresponding to this cusp in the points list of the cusp set containing this cusp.
                /// That is, for CuspSet cs and CuspRec cr, cs.pts[cr.index] = cr.pt
                /// </summary>
                public int index;
                /// <summary>
                /// The location of this cusp.
                /// </summary>
                public Pt pt;
                /// <summary>
                /// curvature at this cusp
                /// </summary>
                public Rad curvature;
                /// <summary>
                /// arc length from start of stroke
                /// </summary>
                public double dist;
                /// <summary>
                /// sign of curvature at cusp
                /// </summary>
                public int sign;
                /// <summary>
                /// is it near the top of the bbox?
                /// </summary>
                public bool top;
                /// <summary>
                /// is it near the left of the bbox?
                /// </summary>
                public bool left;
                /// <summary>
                /// is it near the right of the bbox?
                /// </summary>
                public bool right;
                /// <summary>
                /// is it near the bottom of the bbox?
                /// </summary>
                public bool bot;
            }
            public class CuspSet {
                public CuspSet(Stroq stroke, IList<CuspRec> inkcusps, IList<Pt> inkPts, IList<double> dists, IList<Rad> curves, IList<Rad> angs, IList<int> inters, List<int> skipPts, Rct box, Rad thresh, IList<int> indices, IList<int> speedcusps) {
                    s = stroke;
                    pts = inkPts == null ? s : inkPts;
                    distances = inkPts == null ? new double[pts.Count] : dists;
                    curvatures = inkPts == null ? new Rad[pts.Count] : curves;
                    angles = inkPts == null ? new Rad[pts.Count] : angs;
                    speedCusps  = speedcusps;
                    Rct sbbox = s.GetBounds();
                    cusps = inkcusps == null ? new CuspRec[] { new CuspRec(s,sbbox,curvatures,distances,0), 
                                                           new CuspRec(s,sbbox,curvatures,distances,s.Count-1) } : inkcusps;
                    Pt top = inkPts == null ? sbbox.TopLeft : new Pt(inkPts.Min((p)=>p.X), inkPts.Min((p)=>p.Y));
                    bbox = inkPts == null ? sbbox : new Rct(top.X, top.Y, inkPts.Max((p) => p.X), inkPts.Max((p) => p.Y));
                    l = cusps == null ? 0 : cusps.Count-1;
                    nl =  cusps == null ? 0 : (cusps.Count > 1 ? cusps.Count-2 : -1);
                    nnl =  cusps == null ? 0 : (cusps.Count > 2 ? cusps.Count-3 : -1);
                    intersects = inters;
                    skipped = skipPts;
                    threshold = thresh;
                    realcusps = indices;
                    dist  = distances[distances.Count-1];
                    straight = inkPts == null ? 0 : V2D.Straightness(inkPts);
                    last  = pts[pts.Count-1];
                }
                /// <summary>
                /// straightness of stroke
                /// </summary>
                public double straight;
                /// <summary>
                /// minimum curvature needed to be a cusp
                /// </summary>
                public Rad threshold;
                /// <summary>
                /// avg curvature between two cusps
                /// </summary>
                public Rad avgCurve(int s, int e) {
                    int inset = cusps[e].index-cusps[s].index > 6 ? 3 : 0;
                    return avgCurveSeg(cusps[s].index+inset, cusps[e].index-inset);
                }
                /// <summary>
                /// avg curvature between two point indices
                /// </summary>
                public Rad avgCurveSeg(int s, int e) {
                    e= Math.Min(e, curvatures.Count-2); // last 2 curvatures are meaningless
                    s= Math.Max(s, 2); // first 2 curvatures are meangingless
                    return e == s ? 0 : curvatures.Skip(s).Take(e-s).Average((r) => (double)r);
                }
                /// <summary>
                /// avg magnitude of curvature between two cusps
                /// </summary>
                public Rad avgCurveMag(int s, int e) {
                    int inset = cusps[e].index-cusps[s].index > 6 ? 3 : 0;
                    return avgCurveMagSeg(cusps[s].index+inset, cusps[e].index-inset);
                }
                /// <summary>
                /// avg magnitude of curvature between two point indices
                /// </summary>
                public Rad avgCurveMagSeg(int s, int e) {
                    return curvatures.Skip(s).Take(e-s).Average((c) => Math.Abs(c));
                }
                /// <summary>
                /// max curvature between two cusps
                /// </summary>
                public Rad maxCurve(int s, int e) {
                    int inset = cusps[e].index-cusps[s].index > 6 ? 3 : 0;
                    return maxCurveSeg(cusps[s].index+inset, cusps[e].index-inset);
                }
                /// <summary>
                /// max curvature between two point indices
                /// </summary>
                public Rad maxCurveSeg(int s, int e) {
                    int index; return maxCurveSeg(s, e, out index);
                }
                public Rad maxCurveSeg(int s, int e, out int index) {
                    Rad max = 0;
                    index = s;
                    for(int i = s; i < e; i++)
                        if(Math.Abs(max) < Math.Abs(curvatures[i])) {
                            index= i;
                            max = curvatures[i];
                        }
                    return max;
                }
                public Stroq s;
                /// <summary>
                /// last point of stroke
                /// </summary>
                public Pt last;
                /// <summary>
                /// total arclength
                /// </summary>
                public double dist;
                /// <summary>
                /// self-intersections from MS but in filtered pt space (pts)
                /// </summary>
                public IList<int> intersects;
                public IList<int> speedCusps = null;
                public IList<CuspRec> cusps;
                public IList<int> realcusps;
                public IList<Rad> angles;
                public IList<Rad> curvatures;
                /// <summary>
                /// points after filtering out hooks
                /// </summary>
                public IList<Pt> pts;
                /// <summary>
                /// arc length from start
                /// </summary>
                public IList<double> distances;
                /// <summary>
                /// indices of points skipped from original stroke.
                /// use FeaturePointDetector.convertIndex to map from filtered to unfiltered;
                /// FeaturePointDetector.convertIndexBack goes other way
                /// </summary>
                public List<int> skipped;
                public Rct bbox;
                /// <summary>
                /// index of last cusp
                /// </summary>
                public int l;
                /// <summary>
                /// index of next to last cusp
                /// </summary>
                public int nl;
                /// <summary>
                /// index of next to next to last cusp
                /// </summary>
                public int nnl;
            }
            public static Rad CuspThreshold = 0.1275; // bcz: .1275 when no on the SURFACE !!! 0.45 on slow sampling devices
            public static int CuspMinDist = 10; // new cusps can't within cuspMinDist of a previous cusp
            public static int Sign(CuspSet s, int c) {
                if(s.cusps[c].curvature > 2.5.Rad()) {
                    int pinter = -1;
                    for(int i = 0; i < s.intersects.Count; i++)
                        if(s.intersects[i] > s.cusps[c].curvature)
                            break;
                        else pinter = i;
                    if(pinter != -1 && pinter != s.intersects.Count-1) {
                        if(s.distances[pinter+1]-s.distances[pinter] < 10 ||
                        V2D.angle(s.pts[s.intersects[pinter]], s.cusps[c].pt, s.pts[s.intersects[pinter+1]] - s.cusps[c].pt) < 10.Deg()) {
                            return -Math.Sign(s.cusps[c].curvature);
                        }
                    }
                }
                return Math.Sign(s.cusps[c].curvature);
            }
            /// <summary>
            /// Calculates indices of feature points in the stroke.
            /// </summary>
            /// <param name="s">The stroke to process.</param>
            /// <param name="threshold">The threshold value for determining feature points.</param>
            /// <returns>An int array of the feature points.  The same as native cusp detectors.</returns>
            public static CuspSet FeaturePoints(Stroq s) {
                float[] selfIsects = s.OldSelfIntersections();
                Rad threshold = CuspThreshold;
                Rct bbox = s.GetBounds();
                List<int> skipped = new List<int>();
                int firstint = 0;
                if(s.Count < 4) {
                    return new CuspSet(s, null, null, null, null, null, new int[] { }, new List<int>(), bbox, threshold, new int[] { 0, s.Count }, new int[0]);
                } else if(s.OldPolylineCusps().Length > 9 || selfIsects.Length > 12) {
                    int[] cuspIndexes = s.OldPolylineCusps().ToArray();
                    CuspRec[] cusprecs = cuspIndexes.Select((c) => new CuspRec(s, bbox, new Rad[s.Count], new double[s.Count], c)).ToArray();
                    IList<double> Distances;
                    IList<Rad> Angles;
                    List<Pt> InkPts = Uniquify(s, ref skipped, bbox, firstint);
                    var selfints = selfIsects.Select((si) => convertIndexBack((int)si, skipped)).ToArray();
                    LocalCurvatures(InkPts, out Distances, out Angles);  // curvatures at each stroke point
                    return new CuspSet(s, cusprecs, s, Distances, new Rad[s.Count], new Rad[s.Count], selfints, new List<int>(), bbox, CuspThreshold, cuspIndexes, new int[0]);
                }
                int skipIntersects =  selfIsects.Length > 1 ? 1:-1;
                if(skipIntersects >= 0 && (s[selfIsects[skipIntersects]] - s[0]).Length < 4 && selfIsects[skipIntersects]/s.Count < 0.2) {
                    firstint = (int)(selfIsects[skipIntersects]+selfIsects[skipIntersects-1])/2;
                } else
                    skipIntersects = -1;

                bool doSpeedCusps = CuspThreshold < .12.Rad();
                List<Pt> inkPts  = Uniquify(s, ref skipped, bbox, firstint);

                IList<double> distances;
                IList<Rad> angles;
                IList<Rad> curvatures  = LocalCurvatures(inkPts, out distances, out angles);  // curvatures at each stroke point
                List<int> speedCusps = new List<int>();
                IList<CuspRec> cusps = null;
                IList<int> indices = null;
                if(doSpeedCusps) {
                    List<double> speed = new List<double>(inkPts.Count);
                    for(int i = 0; i < inkPts.Count; i++) {
                        int timeSkip = (i < inkPts.Count) ? convertIndex(i+1, skipped)-convertIndex(i, skipped):1;
                        speed[i] = (i > 0) ? ((i < inkPts.Count-1) ? (distances[i+1]-distances[i])/timeSkip : 0): 0;
                    }

                    double avg = speed.Average();
                    List<int> zeros = new List<int>();
                    bool above = true;
                    double avgCut = avg * 2/3;
                    for(int i=0; i < speed.Count-1; i++) {
                        if(!above && speed[i] < avgCut && speed[i+1] >= avgCut) {
                            zeros.Add(i);
                            above  = true;
                        }
                        if(above && speed[i] > avgCut && speed[i+1] <= avgCut) {
                            above = false;
                            zeros.Add(i);
                        }
                    }
                    speedCusps.Add(0);  // always include the first point
                    zeros.Add(speed.Count - 1);
                    for(int i=0; i < zeros.Count; i+=2) {
                        int end = i+1 < zeros.Count ? zeros[i+1] : speed.Count;
                        int mix;
                        speed.Skip(zeros[i]).Take(end-zeros[i]).Min(Double.PositiveInfinity, out mix);
                        speedCusps.Add(mix + zeros[i]);
                    }
                    indices = speedCusps.Select((c) => convertIndex(c, skipped)).ToArray();
                    cusps = speedCusps.Select((c) => new CuspRec(inkPts, bbox, curvatures, distances, c)).ToArray();
                } else {
                    List<int> cuspInds    = new List<int> { 0 }; // indices of all found cusps
                    List<int> toAvg       = new List<int> { 0 };    // cusp indices that collectively identify one broad cusp
                    int lineCount   = 0;    // how many low curvature, line-like segments we've found in a row
                    bool freshCusp   = true; // whether we're still accumulating a cusp
                    int backtrack   = 20;   // number of pixels we can backtrack to look for curvature changes depends on stroke length
                    if(distances[distances.Count-1] < 70)
                        if(distances[distances.Count-1] < 40)
                            if(distances[distances.Count-1] < 28)
                                FeaturePointDetector.CuspMinDist = 2;
                            else FeaturePointDetector.CuspMinDist = 4;
                        else FeaturePointDetector.CuspMinDist = 5;
                    else FeaturePointDetector.CuspMinDist = 9;
                    bool passedInter = false;
                    int curInter = 0;
                    float[] selfinters = selfIsects;
                    for(int i=2; i<curvatures.Count-2; i++) {
                        // get difference in current and locally previous curvatures -- this difference should be above cusp threshold for a true cusp
                        Rad avgCurv = 0;
                        int count = 0;
                        bool goBack = true, goForward = true;
                        for(int j = 1; j <= backtrack; j++) {
                            if(i-j >0 && goBack) {
                                avgCurv += j*curvatures[i-j];
                                count += j;
                                if(distances[i]-distances[i-j] > backtrack*5)
                                    goBack = false;
                            }
                            if(i+j < curvatures.Count-1 && goForward) {
                                avgCurv += j*curvatures[i+j];
                                count += j;
                                if(distances[i+j]-distances[i] > backtrack*5)
                                    goForward = false;
                            }
                        }
                        Rad CurveTolerance = 2.Rad();// *inkPts.Length/(i+0.0); // set to 2 to be more tolerant (i.e., big loopy rects) 4 to be strict (small circles)
                        int sampleDistance = CuspMinDist > 5 ? CuspMinDist/2 : CuspMinDist > 2 ? 4 : 2;
                        avgCurv /= count;
                        double speedProxy = (inkPts[i] - inkPts[i-1]).Length;
                        Rad deltaCurv = Math.Abs(curvatures[i]) < Math.Abs(avgCurv) ? 0 : Math.Abs(curvatures[i]-avgCurv)/(1-Math.Abs(avgCurv))/(1+CurveTolerance*speedProxy);
                        bool freePass = false;
                        if(Math.Abs(avgCurv) < 0.015 && Math.Abs(curvatures[i]) > threshold)
                            freePass = true;
                        Rad realthresh = threshold;

                        if(curInter < selfinters.Length && i == (int)selfinters[curInter]+1) {
                            passedInter = true;
                            curInter++;
                        }

                        realthresh = i < Math.Min(10, curvatures.Count/4) && distances[i] < 4 ? threshold *4/distances[i]: threshold;
                        // we have a cusp if ...
                        if((freePass || deltaCurv > .0045.Rad()) && Math.Abs(curvatures[i]) > realthresh &&
                    ((speedProxy > 4 && Math.Abs(curvatures[i]) > realthresh) || 
                     Math.Abs((curvatures[i]+curvatures[i-2])/2) > realthresh ||  // curvature is above threshold
                     Math.Abs((curvatures[i]+curvatures[i-1])/2) > realthresh ||  // curvature is above threshold
                     Math.Abs((curvatures[i]+curvatures[i+1])/2) > realthresh ||  // curvature is above threshold
                     Math.Abs((curvatures[i]+curvatures[i+2])/2) > realthresh)) {// sum of local curvature is above threshold (helps avoid jitter)
                            if((i - cuspInds[cuspInds.Count-1] > sampleDistance) && 
                        (deltaCurv > 0.4.Rad() || distances[i]-distances[cuspInds[cuspInds.Count-1]] > CuspMinDist) && //start a new cusp if we're far away from last
                        (!freshCusp ||  // and we're not still accumulating a cusp
                       (Math.Sign(curvatures[i]) != Math.Sign(curvatures[toAvg[0]]) && i-toAvg[toAvg.Count-1] > 3) ||
                       (Math.Abs(curvatures[i]) > Math.Max(Math.Max(Math.Abs(curvatures[i-1]), Math.Abs(curvatures[i-2])), (passedInter || (deltaCurv > 0.01.Rad() && (i - cuspInds[cuspInds.Count-1] >= 2*sampleDistance))? 1 : (realthresh > 0.3.Rad() ? 2:4))*realthresh) &&
                         ((deltaCurv > .9.Rad() && i-toAvg[toAvg.Count-1] > 3)|| 
                         // or we are averaging, but we got a big change in  direction that is far enoug away from the accumulated cusp
                         distances[i]-distances[cuspCenter(curvatures, toAvg)] > CuspMinDist))
                                )) {  // we're not averaging into a new one Or we had a Big curvature change
                                if(freshCusp)
                                    cuspInds[cuspInds.Count-1] = cuspCenter(curvatures, toAvg);
                                cuspInds.Add(i);
                                toAvg.Clear();
                                toAvg.Add(i);
                                freshCusp = true;
                                passedInter = false;
                            } else {   // average cusp index location since curvature is still changing rapidly
                                if(freshCusp) {
                                    if(Math.Abs(curvatures[i]) > Math.Abs(curvatures[(int)toAvg[toAvg.Count-1]]))
                                        toAvg.Add(i);
                                } else if(cuspInds.Count > 1 && (i-(int)cuspInds[cuspInds.Count-1] <= sampleDistance || Math.Abs(curvatures[i]) > 0.5)&& 
                            Math.Abs(curvatures[i]) > Math.Abs(curvatures[(int)cuspInds[cuspInds.Count-1]]))
                                    cuspInds[cuspInds.Count-1] = i;
                            }
                        } else if(deltaCurv < 1.Rad()) {
                            if(Math.Abs(curvatures[i]) < (CuspMinDist >= 9 ? 0.5:0.95)*realthresh) { //avg out sampling noise and see if curve is pretty straight
                                if(++lineCount >2 || (toAvg.Count > 0 && Math.Sign(curvatures[(int)toAvg[0] == 0?1:(int)toAvg[0]]) != Math.Sign(curvatures[i]))) {
                                    toAvg.Add(i);
                                    if(freshCusp)
                                        cuspInds[cuspInds.Count-1] = cuspCenter(curvatures, toAvg);
                                    freshCusp = false; // stop adding new points to this cusp
                                    toAvg.Clear();
                                    lineCount = 0;
                                }
                            } else
                                lineCount = 0;
                        }
                    }
                    toAvg.Add(inkPts.Count-1);
                    if(freshCusp && cuspInds.Count > 1)
                        if(curvatures.Count-cuspCenter(curvatures, toAvg) < 6)
                            cuspInds[cuspInds.Count-1] = inkPts.Count-1;
                        else cuspInds[cuspInds.Count-1] = cuspCenter(curvatures, toAvg);
                    if(cuspInds.Count == 1 || (inkPts.Count-1 - cuspInds[cuspInds.Count-1] > Math.Min(CuspMinDist, 3) &&
                distances[distances.Count-1]-distances[cuspInds[cuspInds.Count-1]] > CuspMinDist-1))
                        cuspInds.Add(inkPts.Count-1);
                    else cuspInds[cuspInds.Count-1] = inkPts.Count-1;

                    List<int> cuspIndsFinal = new List<int>();
                    for(int i=0; i < cuspInds.Count; i++) {// convert indices in unique point set back to indices on original point set
                        int ind= convertIndex(cuspInds[i], skipped);
                        if(i > 0 && i < cuspInds.Count-1) {
                            Pt prev = s[cuspIndsFinal[cuspIndsFinal.Count-1]];
                            Pt next = s[convertIndex(cuspInds[i+1], skipped)];
                            Pt cur  = s[ind];
                            int pInd = convertIndexBack(cuspIndsFinal[cuspIndsFinal.Count-1], skipped);
                            int nInd = cuspInds[i+1];
                            if((cur.X != prev.X || cur.Y != prev.Y) && 
                        V2D.angle(cur, prev, next - cur) > 16.Deg() ||
                        (distances[nInd]-distances[pInd]) / ((prev - cur).Length+(cur - next).Length) > 1.1)
                                cuspIndsFinal.Add(ind);
                        } else
                            cuspIndsFinal.Add(ind);
                    }
                    indices = cuspIndsFinal.ToArray();
                    cusps = indices.Select((i) => new CuspRec(inkPts, bbox, curvatures, distances, convertIndexBack(i, skipped))).ToArray();
                }
                List<float> finalSelfInters = new List<float>();
                List<float> droppedSelfInters = new List<float>();
                for(int i =0; i <selfIsects.Length; i++) {
                    if(i <= skipIntersects)
                        droppedSelfInters.Add(selfIsects[i]);
                    else if(selfIsects[i] > convertIndex(cusps[cusps.Count-1].index, skipped)) {
                        for(int j = 0; j < finalSelfInters.Count; j++)
                            if((s[finalSelfInters[j]] - s[selfIsects[i]]).Length < 0.12)
                                finalSelfInters.RemoveAt(j);
                        droppedSelfInters.Add(selfIsects[i]);
                    } else if(convertIndexBack((int)selfIsects[i], skipped) >= 0 &&
                         (i == selfIsects.Length-1 || selfIsects[i+1]-selfIsects[i] > 2 || (s[selfIsects[i+1]] - s[selfIsects[i]]).Length > 0.15)) {
                        finalSelfInters.Add(selfIsects[i]);
                        for(int j = 0; j < droppedSelfInters.Count; j++)
                            if((s[droppedSelfInters[j]] - s[selfIsects[i]]).Length < 0.15) {
                                finalSelfInters.RemoveAt(finalSelfInters.Count-1);
                                droppedSelfInters.Add(selfIsects[i]);
                                break;
                            }
                    } else droppedSelfInters.Add(selfIsects[i]);
                }
                var intersects = finalSelfInters.Select((si) => convertIndexBack((int)si, skipped)).ToArray();
                return new CuspSet(s, cusps, inkPts, distances, curvatures, angles, intersects, skipped, bbox, threshold, indices, speedCusps.ToArray());
            }
            private static Rad[] LocalCurvatures(IList<Pt> inkPts, out IList<double> distances, out IList<Rad> angles) {
                Rad[] curvatures = new Rad[inkPts.Count];
                Rad[] curvatures2 = new Rad[inkPts.Count];
                Rad[] curvatures3 = new Rad[inkPts.Count];
                angles = inkPts.ByPairs().Select((p) => DirectionChange(p.First+new Vec(1, 0), p.First, p.Second)).ToList();
                angles.Add(angles[angles.Count-1]);
                distances  = new double[inkPts.Count]; distances[0] = 0;
                for(int i=1; i<curvatures.Length-1; i++) {
                    curvatures[i] = DirectionChange(inkPts[i-1], inkPts[i], inkPts[i+1]);
                    if(i > 1 && i < curvatures.Length-2)
                        curvatures2[i] = DirectionChange(inkPts[i-2], inkPts[i], inkPts[i+2])/2;
                    else curvatures2[i] = 0;
                    if(i > 2 && i < curvatures.Length-3)
                        curvatures3[i] = DirectionChange(inkPts[i-3], inkPts[i], inkPts[i+3])/3;
                    else curvatures3[i] = 0;
                    distances[i]  = distances[i-1]+(inkPts[i-1] - inkPts[i]).Length;
                }
                if(inkPts.Count < 4)
                    return curvatures;
                curvatures3[2] = curvatures3[3]*2/3;
                curvatures3[1] = curvatures3[3]/3;
                curvatures2[1] = curvatures2[2]/2;
                int firstSign = -1;
                Rad[] smoothed = new Rad[curvatures.Length];
                for(int i = 2; i < smoothed.Length-1; i++) {
                    if(firstSign == -1 && Math.Sign(curvatures[i]) != 0 || i == 2)
                        firstSign = i;
                    double d = 1.5/(distances[i]-distances[i-1]);
                    smoothed[i] = Math.Abs(curvatures[i]) > .5.Rad() ? curvatures[i] : (curvatures[i]+curvatures2[i]+curvatures3[i])/3;
                }
                for(int i = 0; i < firstSign; i++)
                    smoothed[i] = smoothed[firstSign];
                smoothed[smoothed.Length-1] = smoothed[smoothed.Length-2];
                distances[distances.Count-1] = distances[distances.Count-2] + (inkPts[inkPts.Count-1] - inkPts[inkPts.Count-2]).Length;
                if(SmoothInput) {
                    Rad[] realSmooth = new Rad[curvatures.Length];
                    for(int i = 1; i< realSmooth.Length-1; i++)
                        realSmooth[i] = (smoothed[i-1]+smoothed[i] + smoothed[i+1])/3;
                    realSmooth[0] = smoothed[0];
                    realSmooth[realSmooth.Length-1] = smoothed[smoothed.Length-1];
                    return realSmooth;
                }
                return smoothed;
            }
            private static Rad DirectionChange(Pt a, Pt b, Pt c) {
                // Use points as vectors to calculate the angle change.
                Vec v1 = b-a;
                Vec v2 = c-b;
                if(v1.X == 0 &&  v1.Y == 0)
                    return 0;
                if(v2.X == 0 &&  v2.Y == 0)
                    return 0;
                return v1.SignedAngle(v2);
            }
        }
    }
}
