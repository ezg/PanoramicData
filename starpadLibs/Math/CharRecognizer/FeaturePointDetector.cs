using System;
using Microsoft.Ink;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using starPadSDK.UnicodeNs;

namespace starPadSDK.CharRecognizer
{
    /// <summary>
    /// </summary>
    public class FeaturePointDetector
    {
        public Point[] Uniquify(Point[] inkPts, ref ArrayList skipped, Rectangle bbox, int firstint)
        {
            if (InkPixel == 0)
                InkPixel = 100;
            int maxDim = (int)(Math.Max(bbox.Width, bbox.Height) / InkPixel);
            List<Point> realPts = new List<Point>();
            int hook = firstint;
            int tailhook = inkPts.Length - 1;
            int hooklimit = Math.Min(inkPts.Length / 2, inkPts.Length - 5); // Math.Min(8, inkPts.Length/3);
            List<Point> ppts = new List<Point>();
            ppts.Add(inkPts[0]);
            double dist = 0;
            int duplicate = 0;
            for (int i = 1; i <= hooklimit + duplicate / 2; i++)
                if (ppts[ppts.Count - 1] == inkPts[i] || i < firstint)
                {
                    duplicate++;
                    continue;
                }
                else
                {
                    dist += V2D.Dist(inkPts[i - 1], inkPts[i]) / InkPixel;
                    if (dist > maxDim * .6)
                        break;
                    bool realHook = dist > maxDim * .4;
                    if (ppts.Count > 1)
                    {
                        if (V2D.Straightness(ppts.ToArray()) > .5)
                            break;
                        Point nextPt = inkPts[i + 1];
                        int nextPtInd = i + 1;
                        while (nextPt == ppts[ppts.Count - 1] && nextPtInd > 0)
                            nextPt = inkPts[++nextPtInd];
                        double dirChange = Math.Abs(DirectionChange(ppts[ppts.Count - 2], ppts[ppts.Count - 1], nextPt));
                        double maxDirChange = dist < 8 ? 1 + (.75 / (7 - (Math.Min(6, dist)))) : 2.25;
                        if (dirChange > maxDirChange || realHook)
                        {
                            double fwddist = 0;
                            int fwdind = i;
                            for (; fwdind < inkPts.Length - 1; fwdind++)
                            {
                                fwddist += V2D.Dist(inkPts[fwdind], inkPts[fwdind + 1]) / InkPixel;
                                if (fwddist > dist)
                                    break;
                            }
                            double angHook = angle(inkPts[0], inkPts[i - 1], V2D.Sub(inkPts[Math.Min(fwdind, inkPts.Length - 1)], inkPts[i - 1]));
                            if (realHook)
                            {
                                if (angHook < 2 && dirChange > 1 && V2D.Straightness(inkPts, i - 1, fwdind) < 0.07)
                                    hook = i - 1;
                            }
                            else
                                if (dist < 1 || (angHook < (maxDim > 15 && dist < 4 ? 90 : (12 / dist) * 40)))
                                {
                                    hook = i - 1;
                                }
                        }
                    }
                    ppts.Add(inkPts[i]);
                    if (maxDim > 15 && ppts.Count > 6 && dist > maxDim * .6)
                        break;
                }
            ppts.Clear();
            ppts.Add(inkPts[inkPts.Length - 1]);
            dist = 0;
            for (int i = inkPts.Length - 2; i >= Math.Max(hook + 5, inkPts.Length / 2); i--)
            {
                if (ppts.Count > 0 && ppts[ppts.Count - 1] == inkPts[i])
                    continue;
                dist += V2D.Dist(inkPts[i + 1], inkPts[i]) / InkPixel;
                if (dist > .5 * maxDim)
                    break;
                ppts.Add(inkPts[i]);
                if (ppts.Count > 1)
                {
                    double tailstr = V2D.Straightness(ppts.ToArray());
                    if (tailstr > .5)
                        break;
                    Point prevPt = inkPts[i - 1];
                    int prevPtInd = i - 1;
                    while ((prevPt == ppts[ppts.Count - 1]) && prevPtInd > 0)
                        prevPt = inkPts[--prevPtInd];
                    int dampDist = Math.Max(6, Math.Min(maxDim / 2, 6));
                    double dirChange = Math.Abs(DirectionChange(ppts[ppts.Count - 2], ppts[ppts.Count - 1], prevPt));
                    //double maxDirChange = dist < Math.Min(8, maxDim/2) ? Math.Max(1, (dampDist+1-(Math.Min(dampDist, dist)))) : 2.25;
                    double maxDirChange = dist < 8 ? 1 + (.6 / (7 - (Math.Min(6, dist)))) : 2.25;
                    if (dirChange > maxDirChange)
                    {
                        double backdist = 0;
                        int backind = i;
                        for (; backind > 0; backind--)
                        {
                            backdist += V2D.Dist(inkPts[backind], inkPts[backind + 1]) / InkPixel;
                            if (backdist > dist)
                                break;
                        }
                        double tailang = angle(inkPts[inkPts.Length - 1], inkPts[i + 1], V2D.Sub(inkPts[Math.Max(backind, 0)], inkPts[i + 1]));
                        double maxtailang = 80 - Math.Max(0, tailstr - 0.2) / 0.1 * 15;
                        if ((dirChange > 2.75 && tailang < maxtailang) || tailang < Math.Max(25, Math.Max(15, InkPixel * (maxDim / 2) / dist)))
                        {
                            tailhook = i;
                        }
                    }
                }
                if (maxDim > 15 && ppts.Count > 10) // bcz used to be 5 ... 10 for Joe .. how
                    // many points to search back to find a hook
                    break;
            }
            for (int x = 0; x < inkPts.Length; x++)
            {// filter out points at the same pixel location
                if (x >= hook && x <= tailhook &&
                    (realPts.Count < 1 || (Point)realPts[realPts.Count - 1] != inkPts[x]) &&
                   (realPts.Count < 2 || (Point)realPts[realPts.Count - 2] != inkPts[x]) &&
                   (realPts.Count < 3 || (Point)realPts[realPts.Count - 3] != inkPts[x]))
                    realPts.Add(inkPts[x]);
                else skipped.Add(x);
            }
            if (realPts.Count == 1)
                realPts.Add(new Point(((Point)realPts[0]).X + 1, ((Point)realPts[0]).Y + 1));
            return realPts.ToArray();
        }
        /// <summary>
        /// Flag for whether input needs to be smoothed.  Typically this is false unless a low-res
        /// digitizer like a Wacom tablet is used.
        /// </summary>
        public bool SmoothInput { get; set; }
        int convertIndex(int ind, ArrayList skipped)
        {
            int toadd = 0;
            foreach (int x in skipped)
                if (x <= ind + toadd)
                    toadd++;
                else
                    break;
            return ind + toadd;
        }
        int convertIndexBack(int ind, ArrayList skipped)
        {
            int tosub = 0;
            foreach (int x in skipped)
                if (x <= ind)
                    tosub++;
            return Math.Max(0, ind - tosub);
        }
        int cuspCenter(double[] curvatures, ArrayList cuspsToAvg)
        {
            double m = 0;
            int ind = 0;
            for (int a = (int)cuspsToAvg[0]; a <= (int)cuspsToAvg[cuspsToAvg.Count - 1]; a++)
                if (a == 0 || Math.Abs(curvatures[a]) > Math.Abs(m))
                {
                    m = a == 0 ? 3.14 : curvatures[a];
                    ind = a;
                }
            return ind;
        }
        public class CuspRec
        {
            public CuspRec(Point[] pts, Rectangle bbox, double[] curvatures, double[] distances, int i)
            {
                pt = pts[i];
                if ((pt.Y - bbox.Top + 0.0) / bbox.Height > 0.65)
                    bot = true;
                if ((pt.Y - bbox.Top + 0.0) / bbox.Height < 0.40)
                    top = true;
                if ((pt.X - bbox.Left + 0.0) / bbox.Width > 0.75)
                    right = true;
                if ((pt.X - bbox.Left + 0.0) / bbox.Width < 0.3)
                    left = true;
                sign = Math.Sign(curvatures[i]);
                curvature = curvatures[i];
                dist = distances[i];
                index = i;
            }
            public int index;
            public Point pt;
            /// <summary>
            /// curvature at this cusp
            /// </summary>
            public double curvature;
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
        public class CuspSet
        {
            public CuspSet(Stroke stroke, CuspRec[] inkcusps, Point[] inkPts, double[] dists, double[] curves, double[] angs, int[] inters, ArrayList skipPts, Rectangle box, double thresh, int[] indices, int[] speedcusps)
            {
                s = stroke;
                pts = inkPts == null ? s.GetPoints() : inkPts;
                distances = inkPts == null ? new double[pts.Length] : dists;
                curvatures = inkPts == null ? new double[pts.Length] : curves;
                angles = inkPts == null ? new double[pts.Length] : angs;
                speedCusps = speedcusps;
                cusps = inkcusps == null ? new CuspRec[] { new CuspRec(s.GetPoints(),s.GetBoundingBox(),curvatures,distances,0), 
                                                           new CuspRec(s.GetPoints(),s.GetBoundingBox(),curvatures,distances,s.GetPoints().Length-1) } : inkcusps;
                Point top = inkPts == null ? s.GetBoundingBox().Location : new Point(minx(0, inkPts.Length, inkPts), miny(0, inkPts.Length, inkPts));
                bbox = inkPts == null ? s.GetBoundingBox() : new Rectangle(top.X, top.Y, maxx(0, inkPts.Length, inkPts) - top.X, maxy(0, inkPts.Length, inkPts) - top.Y);
                l = cusps == null ? 0 : cusps.Length - 1;
                nl = cusps == null ? 0 : (cusps.Length > 1 ? cusps.Length - 2 : -1);
                nnl = cusps == null ? 0 : (cusps.Length > 2 ? cusps.Length - 3 : -1);
                intersects = inters;
                skipped = skipPts;
                threshold = thresh;
                realcusps = indices;
                dist = distances[distances.Length - 1];
                straight = inkPts == null ? 0 : V2D.Straightness(inkPts);
                last = pts[pts.Length - 1];
            }
            /// <summary>
            /// straightness of stroke
            /// </summary>
            public double straight;
            /// <summary>
            /// minimum curvature needed to be a cusp
            /// </summary>
            public double threshold;
            /// <summary>
            /// avg curvature between two cusps
            /// </summary>
            public double avgCurve(int s, int e)
            {
                int inset = cusps[e].index - cusps[s].index > 6 ? 3 : 0;
                return avgCurveSeg(cusps[s].index + inset, cusps[e].index - inset);
            }
            /// <summary>
            /// avg curvature between two point indices
            /// </summary>
            public double avgCurveSeg(int s, int e)
            {
                double st_curve = 0;
                int st_count = 0;
                e = Math.Min(e, curvatures.Length - 2); // last 2 curvatures are meaningless
                s = Math.Max(s, 2); // first 2 curvatures are meangingless
                for (int i = s; i < e; i++)
                {
                    st_curve += curvatures[i];
                    st_count++;
                }
                st_curve = st_count > 0 ? st_curve / st_count : 0;
                return st_curve;
            }
            /// <summary>
            /// avg magnitude of curvature between two cusps
            /// </summary>
            public double avgCurveMag(int s, int e)
            {
                int inset = cusps[e].index - cusps[s].index > 6 ? 3 : 0;
                return avgCurveMagSeg(cusps[s].index + inset, cusps[e].index - inset);
            }
            /// <summary>
            /// avg magnitude of curvature between two point indices
            /// </summary>
            public double avgCurveMagSeg(int s, int e)
            {
                double st_curve = 0;
                int st_count = 0;
                for (int i = s; i < e; i++)
                {
                    st_curve += Math.Abs(curvatures[i]);
                    st_count++;
                }
                st_curve /= st_count;
                return st_curve;
            }
            /// <summary>
            /// max curvature between two cusps
            /// </summary>
            public double maxCurve(int s, int e, out int index)
            {
                int inset = cusps[e].index - cusps[s].index > 6 ? 3 : 0;
                return maxCurveSeg(cusps[s].index + inset, cusps[e].index - inset, out index);
            }
            /// <summary>
            /// max curvature between two point indices
            /// </summary>
            public double maxCurveSeg(int s, int e)
            {
                int index; return maxCurveSeg(s, e, out index);
            }
            public double maxCurveSeg(int s, int e, out int index)
            {
                double max = 0;
                index = s;
                for (int i = s; i < e; i++)
                    if (Math.Abs(max) < Math.Abs(curvatures[i]))
                    {
                        index = i;
                        max = curvatures[i];
                    }
                return max;
            }
            public Stroke s;
            /// <summary>
            /// last point of stroke
            /// </summary>
            public Point last;
            /// <summary>
            /// total arclength
            /// </summary>
            public double dist;
            /// <summary>
            /// self-intersections from MS but in filtered pt space (pts)
            /// </summary>
            public int[] intersects;
            public int[] speedCusps = null;
            public CuspRec[] cusps;
            public int[] realcusps;
            public double[] angles;
            public double[] curvatures;
            /// <summary>
            /// points after filtering out hooks
            /// </summary>
            public Point[] pts;
            /// <summary>
            /// arc length from start
            /// </summary>
            public double[] distances;
            /// <summary>
            /// indices of points skipped from original stroke.
            /// use FeaturePointDetector.convertIndex to map from filtered to unfiltered;
            /// FeaturePointDetector.convertIndexBack goes other way
            /// </summary>
            public ArrayList skipped;
            public Rectangle bbox;
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
        public float InkPixel = 0;
        public double CuspThreshold = 0.1275;
        public Guid CuspGuid = new Guid("{78C144BC-FBFD-433d-BD57-25026FEF15D6}");
        public Strokes Ignorable = null; // hash from stroke id to ignoreable flag
        public int CuspMinDist = 10; // new cusps can't within cuspMinDist of a previous cusp
        /// <summary>
        /// Calculates indices of feature points in the stroke.
        /// </summary>
        /// <param name="s">The stroke to process.</param>
        /// <param name="threshold">The threshold value for determining feature points.</param>
        /// <returns>An int array of the feature points.  The same as native cusp detectors.</returns>
        public CuspSet FeaturePoints(Stroke s)
        {
            Point[] strokePts = s.GetPoints();
            float[] selfIsects = s.SelfIntersections;
            double threshold = CuspThreshold;
            if (strokePts.Length < 5)
            {
                return new CuspSet(s, null, null, null, null, null, new int[] { }, new ArrayList(), s.GetBoundingBox(), threshold, new int[] { 0, strokePts.Length }, new int[0]);
            }
            else if (s.PolylineCusps.Length > 9 || selfIsects.Length > 12)
            {
                CuspRec[] cusprecs = new CuspRec[s.PolylineCusps.Length];
                for (int i = 0; i < cusprecs.Length; i++)
                    cusprecs[i] = new CuspRec(s.GetPoints(), s.GetBoundingBox(), new double[s.GetPoints().Length],
                        new double[s.GetPoints().Length], s.PolylineCusps[i]);
                int[] cuspIndexes = new int[cusprecs.Length];
                for (int i = 0; i < cusprecs.Length; i++)
                    cuspIndexes[i] = (int)s.PolylineCusps[i];
                return new CuspSet(s, cusprecs, s.GetPoints(), new double[s.GetPoints().Length], new double[s.GetPoints().Length], new double[s.GetPoints().Length], new int[] { }, new ArrayList(), s.GetBoundingBox(), CuspThreshold, cuspIndexes, new int[0]);
            }
            ArrayList skipped = new ArrayList();
            int skipIntersects = selfIsects.Length > 1 ? 1 : -1;
            int firstint = 0;
            Rectangle bbox = s.GetBoundingBox();
            if (skipIntersects >= 0 && V2D.Dist(getPt(selfIsects[skipIntersects], s.GetPoints()), s.GetPoints()[0]) < InkPixel * 4 &&
                selfIsects[skipIntersects] / (float)s.GetPoints().Length < 0.2)
            {
                firstint = (int)(selfIsects[skipIntersects] + selfIsects[skipIntersects - 1]) / 2;
            }
            else
                skipIntersects = -1;

            bool doSpeedCusps = CuspThreshold < .12;
            Point[] inkPts = Uniquify(strokePts, ref skipped, s.GetBoundingBox(), firstint);
            Point[] scrPts = new Point[inkPts.Length];
            for (int i = 0; i < scrPts.Length; i++)
                scrPts[i] = new Point((int)Math.Round(inkPts[i].X / (float)InkPixel), (int)Math.Round(inkPts[i].Y / (float)InkPixel));

            double[] distances;
            double[] angles;
            double[] curvatures = LocalCurvatures(inkPts, scrPts, out distances, out angles);  // curvatures at each stroke point
            List<int> speedCusps = new List<int>();
            CuspRec[] cusps = null;
            int[] indices = null;
            if (doSpeedCusps)
            {
                double[] speed = new double[inkPts.Length];
                Point[] p = new Point[inkPts.Length];
                for (int i = 0; i < inkPts.Length; i++)
                {
                    int timeSkip = (i < inkPts.Length) ? convertIndex(i + 1, skipped) - convertIndex(i, skipped) : 1;
                    speed[i] = (i > 0) ? ((i < inkPts.Length - 1) ? (distances[i + 1] - distances[i]) / timeSkip : 0) : 0;
                    p[i] = new Point(0, (int)speed[i]);
                }

                double avg = 0;
                for (int z = 0; z < speed.Length; z++) avg += speed[z];
                avg /= speed.Length;
                List<int> zeros = new List<int>();
                bool above = true;
                double avgCut = avg * 2 / 3;
                for (int i = 0; i < p.Length - 1; i++)
                {
                    if (!above && speed[i] < avgCut && speed[i + 1] >= avgCut)
                    {
                        zeros.Add(i);
                        above = true;
                    }
                    if (above && speed[i] > avgCut && speed[i + 1] <= avgCut)
                    {
                        above = false;
                        zeros.Add(i);
                    }
                }
                speedCusps.Add(0);  // always include the first point
                zeros.Add(speed.Length - 1);
                for (int i = 0; i < zeros.Count; i += 2)
                {
                    int my;
                    FeaturePointDetector.miny(zeros[i], i + 1 < zeros.Count ? zeros[i + 1] : p.Length, p, out my);
                    speedCusps.Add(my);
                }
                indices = speedCusps.ToArray();
                cusps = new CuspRec[speedCusps.Count];
                for (int i = 0; i < speedCusps.Count; i++)
                {
                    indices[i] = convertIndex(speedCusps[i], skipped);
                    cusps[i] = new CuspRec(inkPts, bbox, curvatures, distances, speedCusps[i]);
                }
            }
            else
            {
                ArrayList cuspInds = new ArrayList(); cuspInds.Add(0); // indices of all found cusps
                ArrayList toAvg = new ArrayList(); toAvg.Add(0);    // cusp indices that collectively identify one broad cusp
                int lineCount = 0;    // how many low curvature, line-like segments we've found in a row
                bool freshCusp = true; // whether we're still accumulating a cusp
                int backtrack = 20;   // number of pixels we can backtrack to look for curvature changes depends on stroke length
                if (distances[distances.Length - 1] / (int)InkPixel < 70)
                    if (distances[distances.Length - 1] / (int)InkPixel < 40)
                        if (distances[distances.Length - 1] / (int)InkPixel < 28)
                            CuspMinDist = 2;
                        else CuspMinDist = 4;
                    else CuspMinDist = 5;
                else CuspMinDist = 9;
                bool passedInter = false;
                int curInter = 0;
                float[] selfinters = selfIsects;
                for (int i = 2; i < curvatures.Length - 2; i++)
                {
                    // get difference in current and locally previous curvatures -- this difference should be above cusp threshold for a true cusp
                    double avgCurv = 0;
                    int count = 0;
                    bool goBack = true, goForward = true;
                    for (int j = 1; j <= backtrack; j++)
                    {
                        if (i - j > 0 && goBack)
                        {
                            avgCurv += j * curvatures[i - j];
                            count += j;
                            if ((distances[i] - distances[i - j]) / InkPixel > backtrack * 5)
                                goBack = false;
                        }
                        if (i + j < curvatures.Length - 1 && goForward)
                        {
                            avgCurv += j * curvatures[i + j];
                            count += j;
                            if ((distances[i + j] - distances[i]) / InkPixel > backtrack * 5)
                                goForward = false;
                        }
                    }
                    double CurveTolerance = 2;// *inkPts.Length/(i+0.0); // set to 2 to be more tolerant (i.e., big loopy rects) 4 to be strict (small circles)
                    int sampleDistance = CuspMinDist > 5 ? CuspMinDist / 2 : CuspMinDist > 2 ? 4 : 2;
                    avgCurv /= count;
                    double speedProxy = V2D.Dist(scrPts[i], scrPts[i - 1]) > 0 ? V2D.Dist(scrPts[i + 1], scrPts[i - 1]) / 2 : 0;
                    double deltaCurv = Math.Abs(curvatures[i]) < Math.Abs(avgCurv) ? 0 : Math.Abs(curvatures[i] - avgCurv) / (1 - Math.Abs(avgCurv)) / (1 + CurveTolerance * speedProxy);
                    bool freePass = false;
                    if (Math.Abs(avgCurv) < 0.015 && Math.Abs(curvatures[i]) > threshold)
                        freePass = true;
                    double realthresh = threshold;

                    if (curInter < selfinters.Length && i == (int)selfinters[curInter] + 1)
                    {
                        passedInter = true;
                        curInter++;
                    }

                    realthresh = i < Math.Min(10, curvatures.Length / 4) && distances[i] < 400 ? threshold * 400 / distances[i] : threshold;
                    // we have a cusp if ...
                    if ((freePass || deltaCurv > .0045) && Math.Abs(curvatures[i]) > realthresh &&
                        //distances[i] > InkPixel *3 &&
                        ((speedProxy > 4 && Math.Abs(curvatures[i]) > realthresh) ||
                         Math.Abs((curvatures[i] + curvatures[i - 2]) / 2) > realthresh ||  // curvature is above threshold
                         Math.Abs((curvatures[i] + curvatures[i - 1]) / 2) > realthresh ||  // curvature is above threshold
                         Math.Abs((curvatures[i] + curvatures[i + 2]) / 2) > realthresh ||  // curvature is above threshold
                         Math.Abs((curvatures[i] + curvatures[i + 2]) / 2) > realthresh))
                    {// sum of local curvature is above threshold (helps avoid jitter)
                        if ((i - (int)cuspInds[cuspInds.Count - 1] > sampleDistance) &&
                            (deltaCurv > 0.4 || (distances[i] - distances[(int)cuspInds[cuspInds.Count - 1]]) / InkPixel > CuspMinDist) && //start a new cusp if we're far away from last
                            (!freshCusp ||  // and we're not still accumulating a cusp
                           (Math.Sign(curvatures[i]) != Math.Sign(curvatures[(int)toAvg[0]]) && i - (int)toAvg[toAvg.Count - 1] > 3) ||
                           (Math.Abs(curvatures[i]) > Math.Max(Math.Max(Math.Abs(curvatures[i - 1]), Math.Abs(curvatures[i - 2])), (passedInter || (deltaCurv > 0.01 && (i - (int)cuspInds[cuspInds.Count - 1] >= 2 * sampleDistance)) ? 1 : (realthresh > 0.3 ? 2 : 4)) * realthresh) &&
                             ((deltaCurv > .9 && i - (int)toAvg[toAvg.Count - 1] > 3) ||
                            // or we are averaging, but we got a big change in  direction that is far enoug away from the accumulated cusp
                             (distances[i] - distances[cuspCenter(curvatures, toAvg)]) / InkPixel > CuspMinDist))
                            ))
                        {  // we're not averaging into a new one Or we had a Big curvature change
                            if (freshCusp)
                                cuspInds[cuspInds.Count - 1] = cuspCenter(curvatures, toAvg);
                            cuspInds.Add(i);
                            toAvg.Clear();
                            toAvg.Add(i);
                            freshCusp = true;
                            passedInter = false;
                        }
                        else
                        {   // average cusp index location since curvature is still changing rapidly
                            if (freshCusp)
                            {
                                if (Math.Abs(curvatures[i]) > Math.Abs(curvatures[(int)toAvg[toAvg.Count - 1]]))
                                    toAvg.Add(i);
                            }
                            else if (cuspInds.Count > 1 && (i - (int)cuspInds[cuspInds.Count - 1] <= sampleDistance || Math.Abs(curvatures[i]) > 0.5) &&
                             Math.Abs(curvatures[i]) > Math.Abs(curvatures[(int)cuspInds[cuspInds.Count - 1]]))
                                cuspInds[cuspInds.Count - 1] = i;
                        }
                    }
                    else if (deltaCurv < 1)
                    {
                        if (Math.Abs(curvatures[i]) < (CuspMinDist >= 9 ? 0.5 : 0.95) * realthresh)
                        { //avg out sampling noise and see if curve is pretty straight
                            if (++lineCount > 2 || (toAvg.Count > 0 && Math.Sign(curvatures[(int)toAvg[0] == 0 ? 1 : (int)toAvg[0]]) != Math.Sign(curvatures[i])))
                            {
                                toAvg.Add(i);
                                if (freshCusp)
                                    cuspInds[cuspInds.Count - 1] = cuspCenter(curvatures, toAvg);
                                freshCusp = false; // stop adding new points to this cusp
                                toAvg.Clear();
                                lineCount = 0;
                            }
                        }
                        else
                            lineCount = 0;
                    }
                }
                toAvg.Add(scrPts.Length - 1);
                if (freshCusp && cuspInds.Count > 1)
                    if (curvatures.Length - cuspCenter(curvatures, toAvg) < 6)
                        cuspInds[cuspInds.Count - 1] = scrPts.Length - 1;
                    else cuspInds[cuspInds.Count - 1] = cuspCenter(curvatures, toAvg);
                if (cuspInds.Count == 1 || (scrPts.Length - 1 - (int)cuspInds[cuspInds.Count - 1] > Math.Min(CuspMinDist, 3) &&
                    (distances[distances.Length - 1] - distances[(int)cuspInds[cuspInds.Count - 1]]) / InkPixel > CuspMinDist - 1))
                    cuspInds.Add(scrPts.Length - 1);
                else cuspInds[(int)cuspInds.Count - 1] = scrPts.Length - 1;

                ArrayList cuspIndsFinal = new ArrayList();
                for (int i = 0; i < cuspInds.Count; i++)
                {// convert indices in unique point set back to indices on original point set
                    int ind = convertIndex((int)cuspInds[i], skipped);
                    if (i > 0 && i < cuspInds.Count - 1)
                    {
                        Point prev = strokePts[(int)cuspIndsFinal[cuspIndsFinal.Count - 1]];
                        Point next = strokePts[convertIndex((int)cuspInds[i + 1], skipped)];
                        Point cur = strokePts[ind];
                        int pInd = convertIndexBack((int)cuspIndsFinal[cuspIndsFinal.Count - 1], skipped);
                        int nInd = (int)cuspInds[i + 1];
                        if ((cur.X != prev.X || cur.Y != prev.Y) &&
                            angle(cur, prev, V2D.Sub(next, cur)) > 16 ||
                            (distances[nInd] - distances[pInd]) / (V2D.Dist(prev, cur) + V2D.Dist(cur, next)) > 1.1)
                            cuspIndsFinal.Add(ind);
                    }
                    else
                        cuspIndsFinal.Add(ind);
                }
                indices = (int[])cuspIndsFinal.ToArray(typeof(int));
                cusps = new CuspRec[indices.Length];
                for (int i = 0; i < indices.Length; i++)
                    cusps[i] = new CuspRec(inkPts, bbox, curvatures, distances, convertIndexBack(indices[i], skipped));
            }
            ArrayList inters = new ArrayList();
            List<float> finalSelfInters = new List<float>();
            List<float> droppedSelfInters = new List<float>();
            for (int i = 0; i < selfIsects.Length; i++)
            {
                if (i <= skipIntersects)
                    droppedSelfInters.Add(selfIsects[i]);
                else if (selfIsects[i] > convertIndex(cusps[cusps.Length - 1].index, skipped))
                {
                    for (int j = 0; j < finalSelfInters.Count; j++)
                        if (V2D.Dist(getPt(finalSelfInters[j], s.GetPoints()), getPt(selfIsects[i], s.GetPoints())) < 12)
                            finalSelfInters.RemoveAt(j);
                    droppedSelfInters.Add(selfIsects[i]);
                }
                else if (convertIndexBack((int)selfIsects[i], skipped) >= 0 &&
                       (i == selfIsects.Length - 1 ||
                       selfIsects[i + 1] - selfIsects[i] > 2 ||
                       V2D.Dist(getPt(selfIsects[i + 1], s.GetPoints()), getPt(selfIsects[i], s.GetPoints())) > 15))
                {
                    finalSelfInters.Add(selfIsects[i]);
                    for (int j = 0; j < droppedSelfInters.Count; j++)
                        if (V2D.Dist(getPt(droppedSelfInters[j], s.GetPoints()), getPt(selfIsects[i], s.GetPoints())) < 15)
                        {
                            finalSelfInters.RemoveAt(finalSelfInters.Count - 1);
                            droppedSelfInters.Add(selfIsects[i]);
                            break;
                        }
                }
                else droppedSelfInters.Add(selfIsects[i]);
            }
            for (int i = 0; i < finalSelfInters.Count; i++)
            {
                int nextInt = convertIndexBack((int)finalSelfInters[i], skipped);
                if (inters.Count == 0 || (int)inters[inters.Count - 1] != nextInt)
                    inters.Add(nextInt);
            }
            int[] intersects = (int[])inters.ToArray(typeof(int));
            return new CuspSet(s, cusps, inkPts, distances, curvatures, angles, intersects, skipped, bbox, threshold, indices, speedCusps.ToArray());
        }
        private static string[] _words = null;
        private static HashSet<string> _wordList = new HashSet<string>(new string[] { "dx", "dy", "dz" });
        public static string[] Words { get { if (_words == null) InitWords(); return _words; } }
        static FeaturePointDetector()
        {
            InitWords();
        }
        public static void AddWords(IEnumerable<string> wl)
        {
            foreach (string s in wl)
                _wordList.Add(s);
            InitWords();
        }
        public SortedList 
            ClearRecogs(Strokes clearStrokes)
        {
            SortedList toUpdate = new SortedList();
            if (clearStrokes == null)
                return toUpdate;

            Strokes strokes = clearStrokes.Ink.CreateStrokes();
            strokes.Add(clearStrokes);
            strokes.Remove(Ignorable);
            foreach (Stroke s in strokes)
            {
                if (s == null || s.Deleted)// s.ExtendedProperties.Contains(IgnoreGuid))
                    continue;
                Recognition oldRec = (Recognition)Recogs[s.Id];
                if (oldRec != null)
                {
                    foreach (Stroke os in oldRec.strokes)
                    {
                        if (!os.Deleted)
                        {
                            if (Recogs.Contains(os.Id))
                                Recogs.Remove(os.Id);
                            if (!toUpdate.Contains(os.Id))
                                toUpdate.Add(os.Id, os);
                        }
                        Recogs.Remove(os.Id);
                    }
                }
                else
                    if (!toUpdate.Contains(s.Id))
                        toUpdate.Add(s.Id, s);
            }
            return toUpdate;
        }
        public SortedList Reset(Ink ink)
        {
            Recogs = new Hashtable();
            SortedList toUpdate = new SortedList();
            foreach (Stroke s in ink.Strokes)
            {
                toUpdate.Add(s.Id, s);
                if (Classification(s) == null)
                    FullClassify(s);
            }
            return toUpdate;
        }
        public SortedList Reset(Strokes strokes)
        {
            SortedList toUpdate = ClearRecogs(strokes);
            foreach (Stroke s in toUpdate.GetValueList())
            {
                if (s.ExtendedProperties.Contains(IgnoreGuid))
                    Ignorable.Add(s);
                else if (Classification(s) == null)
                    FullClassify(s);
            }
            return toUpdate;
        }
        private static void InitWords()
        {
            _words = _wordList.OrderBy((string s) => s).ToArray();
        }

        public Strokes filter(Strokes strokes)
        {
            strokes.Remove(Ignorable);
            return strokes;
        }
        /* <summary>Recognizes a given Stroke which has previously been classified</summary> */
        public Recognition Classification(Stroke s)
        {
            if (s == null || s.Deleted || Recogs[s.Id] == null) return null;
            return (Recognition)Recogs[s.Id];
        }
        public void UnClassify(Stroke s)
        {
            Recognition r = Classification(s);
            if (r != null)
                foreach (Stroke sr in r.strokes)
                    Recogs.Remove(sr.Id);
        }
        public Recognition FullClassify(Stroke stroke) { return FullClassify(stroke, false); }
        public Recognition FullClassify(Stroke stroke, bool singleOnly)
        {
            Recognition r = Classify(stroke, singleOnly);
            FullClassify(stroke, r);
            return r;
        }
        public void FullClassify(Stroke stroke, Recognition r)
        {
            if (r != null)
            {
                foreach (Stroke s in r.strokes)
                    if (!s.Deleted)
                    {
                        UnClassify(s);
                        Recogs.Add(s.Id, r);
                    }
            }
        }
        public Recognition Classify(Stroke s) { return Classify(s, false); }
        public Recognition Classify(Stroke s, bool singleOnly)
        {
            string allograph;
            Recognition r = Classify(s, singleOnly, out allograph);
            return r;
        }
        public Recognition Classify(Stroke s, bool singleOnly, out string allograph)
        {
            FeaturePointDetector.CuspSet cset = FeaturePoints(s);
            s.ExtendedProperties.Add(CuspGuid, cset.realcusps);
            Recognition recog = Recognize(cset, singleOnly);
            if (recog != null)
                allograph = recog.allograph;
            else allograph = "";
            return recog;
        }
        public Recognition Recognize(CuspSet cset, bool singleOnly)
        {
            string allograph = "";
            int midpt = (cset.s.GetBoundingBox().Top + cset.s.GetBoundingBox().Bottom) / 2;
            int baseline = cset.s.GetBoundingBox().Bottom;
            List<string> msalts = new List<string>();
            List<int> msxhgts = new List<int>();
            List<int> msbaselines = new List<int>();
            string topMSAlt = "";
            int topMSxhgt = midpt;
            int topMSbaseline = baseline;
            RecognitionAlternate topMSWord = null;
            Strokes csetStks = cset.s.Ink.CreateStrokes(new int[] { cset.s.Id });

            using (RecognizerContext myRecoContext = new RecognizerContext())
            {
                myRecoContext.Factoid = Microsoft.Ink.Factoid.OneChar;
                RecognitionStatus status;
                myRecoContext.Strokes = csetStks;
                RecognitionResult recoResult = null;
                try { recoResult = myRecoContext.Recognize(out status); }
                catch (Exception) { status = RecognitionStatus.ProcessFailed; }
                if (status == RecognitionStatus.NoError)
                {
                    if (recoResult.TopString.Length != 1)
                    {
                        topMSWord = recoResult.TopAlternate;
                    }
                    foreach (RecognitionAlternate a in recoResult.GetAlternatesFromSelection())
                        if (a.ToString().Length == 1)
                        {
                            int xhgt = a.Midline.BeginPoint.Y;
                            int baseL = a.Baseline.BeginPoint.Y;
                            string small = "acemnorsuvwxz.,-~αεικνοπστυω°()μ" + Unicode.M.MINUS_SIGN + Unicode.D.DOT_OPERATOR;
                            if (small.Contains(a.ToString()))
                            {
                                xhgt = cset.s.GetBoundingBox().Top;
                                baseL = cset.s.GetBoundingBox().Bottom;
                            }
                            if (topMSAlt == "")
                            {
                                topMSAlt = a.ToString();
                                topMSxhgt = xhgt;
                                topMSbaseline = baseL;
                            }
                            msalts.Add(a.ToString());
                            msxhgts.Add(xhgt);
                            msbaselines.Add(baseL);
                        }
                }
            }
            if (match_dot(cset))
            {
                allograph = ".";
            }
            else if (cset.cusps.Length < 3 && (cset.intersects.Length < 2 || (cset.distances[cset.intersects[1]] - cset.distances[cset.intersects[0]] < InkPixel * 5)))
            {
                #region 2cusps
                double upang = angle(cset.last, cset.pts[0], new PointF(0, 1));
                double downang = angle(cset.last, cset.pts[0], new PointF(0, -1));
                double ang = Math.Min(upang, downang);
                Point vec = V2D.Sub(cset.last, cset.cusps[0].pt);
                PointF dir = V2D.Normalize(vec);
                bool left = true;
                double farthest = V2D.MaxDist(cset.pts, dir, out left);
                int farInd = cset.pts.Length - 1;
                double nearCenter = double.MaxValue;
                int centIndex = -1;
                for (int i = (int)(cset.pts.Length * .9); i < cset.pts.Length - 1; i++)
                {
                    bool tleft;
                    double far = V2D.MaxDist(cset.pts, V2D.Normalize(V2D.Sub(cset.pts[i], cset.pts[0])), out tleft);
                    if (far < farthest)
                    {
                        farthest = far;
                        farInd = i;
                    }
                }
                for (int i = 0; i < cset.pts.Length; i++)
                {
                    if ((centIndex == -1 || Math.Abs(cset.pts[i].Y - (cset.bbox.Top + cset.bbox.Bottom) / 2) < nearCenter))
                    {
                        nearCenter = Math.Abs(cset.pts[i].Y - (cset.bbox.Top + cset.bbox.Bottom) / 2);
                        centIndex = i;
                    }
                }
                double curveStraightness = V2D.Straightness(cset.pts, cset.pts[0], V2D.Sub(cset.pts[farInd], cset.pts[0]), 0, cset.pts.Length, cset.dist);
                curveStraightness *= Math.Sqrt(Math.Max(1, cset.dist / 100.0 / InkPixel));
                bool topDown = cset.pts[0].Y < cset.last.Y;
                double openratio = V2D.Dist(cset.cusps[0].pt, cset.cusps[1].pt) / cset.dist;
                double aspect = farthest / V2D.Length(vec);
                bool stleft, enleft;
                double startcurve = V2D.Straightness(cset.pts, (int)(cset.pts.Length * .1), centIndex, out stleft);
                double endcurve = V2D.Straightness(cset.pts, centIndex, cset.pts.Length, out enleft);
                if (openratio < 0.2)
                {
                    allograph = "0";
                }
                else if (openratio < 0.6 && ang > 65 && aspect > 0.5)
                    allograph = "uv";
                int topind; miny(0, cset.pts.Length, cset.pts, out topind);
                if (!topDown && cset.bbox.Height > cset.bbox.Width && V2D.Straightness(cset.pts, 0, topind) < .12 &&
                    (cset.dist - cset.distances[topind]) / cset.bbox.Height > .4 &&
                   (cset.dist - cset.distances[topind]) / V2D.Dist(cset.last, cset.pts[topind]) > .75 &&
                    cset.curvatures[topind] > 0)
                    allograph = "P";
                else if (allograph == "" && ang < 60)
                {
                    if (stleft && !enleft && (startcurve > 0.2 || endcurve > 0.2))
                    {
                        if ((cset.bbox.Bottom - (topDown ? cset.last.Y : cset.pts[0].Y) + 0.0) / cset.bbox.Height > 0.3)
                            allograph = Math.Sign(cset.curvatures[cset.pts.Length / 2]) == -1 && !topDown ? "partial" : "6";
                        else if (topDown)
                        {
                            int botind, rightind;
                            maxy(cset.pts.Length / 2, cset.pts.Length, cset.pts, out botind);
                            maxx(botind, cset.pts.Length, cset.pts, out rightind);
                            if (botind < cset.pts.Length - 1 && Math.Abs(cset.angles[botind + 1]) > 2 &&
                                (cset.last.X - cset.pts[rightind].X + 0.0) / cset.bbox.Width < -0.1)
                            {
                                for (int i = botind; i < cset.angles.Length; i++)
                                    if (cset.angles[i - 1] >= 0 && cset.angles[i] < 0 &&
                                        (cset.bbox.Right - cset.pts[i].X + 0.0) / cset.bbox.Width > 0.1)
                                    {
                                        allograph = "6";
                                        break;
                                    }
                            }
                        }
                    }
                    if (!stleft && !enleft && (startcurve > 0.2 || endcurve > 0.2))
                    {
                        if ((cset.bbox.Bottom - (topDown ? cset.last.Y : cset.pts[0].Y) + 0.0) / cset.bbox.Height > 0.3)
                            allograph = topDown ? "2p" : (cset.cusps[cset.l].left ? "0" : "partial");
                        else
                        {
                            int botind;
                            maxy(cset.pts.Length / 2, cset.pts.Length, cset.pts, out botind);
                            if (botind < cset.pts.Length - 1 && Math.Abs(cset.angles[botind + 1]) < 1)
                            {
                                for (int i = botind; i < cset.angles.Length; i++)
                                    if (cset.angles[i - 1] < 0 && cset.angles[i] > 1.57)
                                    {
                                        allograph = topDown ? "2p" : "partial";
                                        break;
                                    }
                            }
                        }
                    }
                }
                if (allograph == "" && (ang < 25 || (ang < (cset.straight > 0.2 ? 55 : 45) && aspect > (left ? 0.10 : 0.06))))
                {
                    if (curveStraightness > 0.05 && stleft && !enleft && ((startcurve > 0.06 && endcurve > 0.03) || (startcurve > 0.03 && endcurve > 0.06)) &&
                        (startcurve > 0.1 || (cset.cusps[0].pt.Y < cset.last.Y ? cset.cusps[0].right : cset.cusps[1].right)))
                    {
                        if (cset.cusps[0].pt.Y < cset.last.Y && cset.bbox.Height / InkPixel < 20)
                            allograph = curveStraightness > 0.08 ? "," : "1";
                        else
                        {
                            bool l;
                            int maxind, minind;
                            V2D.MaxDist(cset.pts, V2D.Normalize(V2D.Sub(cset.pts[cset.pts.Length / 2], cset.pts[0])), out l, out minind, 0, cset.pts.Length / 2);
                            V2D.MaxDist(cset.pts, V2D.Normalize(V2D.Sub(cset.last, cset.pts[cset.pts.Length / 2])), out l, out maxind, cset.pts.Length / 2, cset.pts.Length);
                            if (cset.curvatures[minind] < 0 && cset.curvatures[maxind] > 0 && cset.last.X < cset.pts[minind].X)
                                allograph = "INT" + (cset.cusps[0].pt.Y < cset.last.Y ? "top" : "bot");
                            else if (cset.curvatures[minind] < 0 && (cset.curvatures[maxind] < 0 || (cset.pts[centIndex].X - cset.bbox.Left + 0.0) / cset.bbox.Width < 0.5))
                                allograph = "(";
                            else allograph = ")";
                        }
                    }
                    if (curveStraightness > 0.07 && allograph == "" && aspect > (left ? 0.04 + .035 * Math.Min(1, InkPixel * 50 / cset.bbox.Height) : 0.04) && farthest > InkPixel * 2)
                    {
                        if (left)
                        {
                            if (aspect > 0.4 || cset.bbox.Width / (float)cset.bbox.Height > .5)
                            {
                                if ((topDown && startcurve > 0) || (!topDown && startcurve < 0))
                                {
                                    int maxDist;
                                    double md = V2D.MaxDist(cset.pts, V2D.Normalize(V2D.Sub(cset.last, cset.pts[0])), out left, out maxDist, 3, cset.pts.Length);
                                    double cang = angle(cset.pts[0], cset.pts[maxDist], V2D.Sub(cset.last, cset.pts[maxDist]));
                                    double vertang = angle(cset.last, cset.pts[0], new PointF(1, 0));
                                    if (md / V2D.Dist(cset.last, cset.pts[0]) > 0.285 || cang < 86)
                                        allograph = stleft ? "c(" : "\\";
                                    else if (vertang < 60)
                                        allograph = "\\";
                                    else
                                    {
                                        int leftInd, topInd;
                                        int mi_x = minx(0, cset.pts.Length, cset.pts, out leftInd);
                                        int top_x = maxx(0, leftInd, cset.pts, out topInd);
                                        if (!stleft)
                                            allograph = "\\";
                                        else
                                            if (cset.distances[leftInd] / cset.dist < 0.1 || angle(cset.pts[topInd], cset.pts[leftInd], new PointF(0, -1)) < 15)
                                                allograph = "(";
                                            else allograph = "(c";
                                    }
                                }
                            }
                            else if (stleft)
                            {
                                if (aspect > (0.4 + 0.08) / 2)
                                {
                                    double slantAng = angle(cset.last, cset.pts[0], new PointF(0, -1));
                                    if (slantAng > 25 && cset.last.Y < cset.pts[0].Y)
                                        allograph = "r";
                                    else allograph = "(c";
                                }
                                else
                                {
                                    allograph = (curveStraightness < 0.085 && aspect < 0.14) ? "1(" : "(1";
                                }
                            }
                        }
                        else
                        {
                            if (ang > 40)
                                allograph = ",";
                            else if (aspect < 0.2)
                                allograph = ((!stleft || startcurve < 0.05 || endcurve > 0.15) && (!enleft || startcurve > 0.1) && curveStraightness > 0.07) ? ")1" : "1";
                            else allograph = ((startcurve < 0.05 || !stleft) && !enleft && endcurve > 0.05) ? ")" : "1";
                        }
                    }
                    else if (allograph == "" && aspect < 0.1)
                    {
                        if (ang <= (cset.pts[0].X < cset.last.X ? 20 : 25) && curveStraightness < 0.1)
                            allograph = "1";
                        else if (ang > 25 && curveStraightness < 0.1)
                        {
                            if (cset.pts[0].X < cset.last.X)
                                allograph = "\\";
                            else allograph = "/";
                        }
                        else allograph = ang > 16 && topDown ? "\\" : left ? "1()" : "1)(";
                    }
                }
                if (allograph == "" && cset.dist / Math.Sqrt(cset.bbox.Width * cset.bbox.Width + cset.bbox.Height * cset.bbox.Height) < 1.5)
                {
                    double slope = Math.Abs(Math.Abs(ang) - 90);

                    if (slope < 30)
                    {//45
                        double totCrvP = 0, totCrvN = 0;
                        for (int i = 3; i < cset.pts.Length - 3; i++)
                        {
                            totCrvP += cset.curvatures[i] > 0 ? cset.curvatures[i] : 0;
                            totCrvN += cset.curvatures[i] < 0 ? cset.curvatures[i] : 0;
                        }
                        if (totCrvP / (cset.pts.Length - 6) < 0.06 || totCrvN / (cset.pts.Length - 6) < 0.06)
                        {
                            //if (Math.Abs(Math.Abs(ang) - 90) < 50)
                            //    allograph = "\\";
                            //else  
                            allograph = "-";
                        }
                        else allograph = "~";
                    }
                    else if ((cset.cusps[0].pt.X < cset.cusps[1].pt.X && cset.cusps[0].pt.Y > cset.cusps[1].pt.Y) ||
                          (cset.cusps[1].pt.X < cset.cusps[0].pt.X && cset.cusps[1].pt.Y > cset.cusps[0].pt.Y))
                    {
                        bool lefty;
                        double curve = V2D.Straightness(cset.pts, out lefty);
                        if (cset.cusps[0].pt.Y < cset.last.Y && (cset.bbox.Height < InkPixel * 10 || curve > 0.2))
                            allograph = ",";
                        else if (aspect > 0.06 && Math.Abs(cset.angles[cset.angles.Length / 2] - Math.PI / 2) < Math.PI / 6 &&
                            V2D.Straightness(cset.pts, 0, cset.pts.Length / 4) > 0.07 &&
                            V2D.Straightness(cset.pts, cset.pts.Length * 3 / 4, cset.pts.Length) > 0.07)
                            //cset.avgCurveMag(0, 1) > 0.025 &&  (Math.Sign(cset.avgCurveSeg(0, cset.pts.Length/4)) != Math.Sign(cset.avgCurveSeg(cset.pts.Length*3/4, cset.pts.Length))))
                            allograph = "INT" + (cset.cusps[0].pt.Y < cset.last.Y ? "top" : "bot");
                        else if (curve > 0.13 && lefty)
                            allograph = ang < 15 ? "1(" : "r";
                        else allograph = "/";
                    }
                    else allograph = ang < 15 ? "1" : "\\";
                }
                #endregion
            }
            else if (cset.cusps.Length < 10)
            {
                #region 3cusps
                if ((msalts.Contains("k") || (!cset.cusps[0].top && msalts.Contains("h"))) && match_k(cset, ref allograph)) allograph += "";
                else if (msalts.Contains("d") && match_delta(cset)) allograph = "δ";
                else if (topMSAlt != "g" && msalts.Contains("5") && match_5(cset)) allograph = "5s";
                else if (msalts.Contains("a") && match_a(cset)) allograph = "a";
                else if (msalts.Contains("9") && match_9(cset, ref midpt)) allograph = "9";
                else if (msalts.Contains("s") && topMSAlt != "g" && match_s(cset, ref allograph)) allograph += "";
                else if (msalts.Contains("g") && match_g(cset, ref midpt, ref allograph)) allograph += "";
                else if (msalts.Contains("q") && match_q(cset, ref midpt, ref allograph)) allograph += "";
                else if (msalts.Contains("d") && match_d(cset, ref midpt)) allograph = "d";
                else if (msalts.Contains("b") && match_b(cset, ref midpt)) allograph = "b";
                else if (match_arrow(cset, ref allograph)) ;
                else if (msalts.Contains("h") && match_h(cset, ref midpt, ref allograph)) allograph += "";
                else if (match_caret(cset)) allograph = "^";
                else if (msalts.Contains("n") && match_n(cset, ref allograph)) allograph += "";
                else if (msalts.Contains("w") && match_w(cset, ref allograph)) allograph += "";
                else if ((msalts.Contains("w") || msalts.Contains("v")) && match_omega(cset, ref allograph)) allograph += "";
                else if ((msalts.Contains("m") || msalts.Contains("M")) && match_m(cset, ref allograph)) allograph += "";
                else if (msalts.Contains("3") && match_3(cset)) allograph = "3";
                else if (msalts.Contains("p") && match_p(cset, ref allograph)) allograph = allograph + "";
                else if ((msalts.Contains("2") || msalts.Contains("z")) && match_2z(cset, ref allograph)) allograph = allograph + "";
                else if (msalts.Contains("z") && match_z(cset)) allograph = "z";
                else if (msalts.Contains("1") && match_1(cset, ref allograph)) ;
                else if (match_bint(cset, ref allograph)) { allograph += ""; }
                else if (msalts.Contains("v") && match_v(cset)) allograph = "v";
                else if (msalts.Contains("u") && match_uv(cset)) allograph = "uv";
                else if (msalts.Contains("u") && match_u(cset)) allograph = "u";
                else if (msalts.Contains("8") && match_8(cset, ref midpt)) allograph = "8";
                else if ((msalts.Contains("e") || msalts.Contains("l")) && match_el(cset, ref allograph)) allograph += "";
                else if (msalts.Contains("d") && match_dcursive(cset)) allograph = "d";
                else if (msalts.Contains("O") && match_theta(cset)) allograph = "θ";
                else if (msalts.Contains("o") && match_sigma(cset)) allograph = "sigma";
                else if ((msalts.Contains("o") || msalts.Contains("O")) && match_o(cset, ref allograph)) allograph = allograph + "";
                else if (match_rect(cset)) allograph = "box";
                else if ((msalts.Contains("r") || msalts.Contains("n")) && match_r(cset)) allograph = "r";
                else if (match_Omega(cset)) allograph = "Omega"; //"\u03A9"
                else if (msalts.Contains("7") && match_7(cset, ref allograph)) { }//also checks for "not7"
                else if (msalts.Contains("L") && match_L(cset)) allograph = "L";
                else if (msalts.Contains("6") && match_b6(cset)) allograph = "b6";
                else if (msalts.Contains("6") && match_6(cset)) allograph = ("oO0".Contains(topMSAlt) ? "0" : "6");
                else if (msalts.Contains("2") && match_2(cset, ref midpt, ref allograph)) allograph = allograph + "";
                else if (match_d2(cset)) allograph = "d2";
                else if (match_lb(cset)) allograph = "[";
                else if (match_rb(cset)) allograph = "]";
                else if ((msalts.Contains("c") || msalts.Contains("C")) && match_c(cset, ref allograph)) allograph += "";
                else if (match_infinity(cset) || match_2infty(cset)) allograph = "∞";
                else if (match_E_loopy(cset)) allograph = "Escript";
                else if (msalts.Contains(">") && match_gt(cset)) allograph = ">";
                else if ((msalts.Contains("z") || msalts.Contains("y") || msalts.Contains("4")) && match_y(cset, ref allograph, ref midpt)) allograph = allograph + "";
                else if ((!msalts.Contains("b") && msalts.Contains("5")) && match_b(cset, ref midpt)) allograph = "b";
                else if (msalts.Contains("<") && match_lt(cset)) allograph = "<";
                else if (msalts.Contains("N") && match_N(cset)) allograph = "N";
                else if (match_alphax(cset)) allograph = "alphax";
                else if (match_alpha(cset)) allograph = "alpha";
                else if (match_beta(cset, ref baseline, ref midpt, ref allograph)) allograph += "";
                else if (match_partial(cset)) allograph = "partial";
                else if (match_partialTop(cset)) allograph = "2partial";
                else if (match_sqrt(cset)) allograph = "sqrt";
                else if (match_gamma(cset)) allograph = "gamma";
                else if (match_Sigma(cset)) allograph = "Sigma";
                else if (match_nu(cset)) allograph = "nu";
                else if (match_mu(cset)) allograph = "mu";
                else if (match_tilde(cset)) allograph = "~";
                else if (topMSAlt != "{" && match_lp(cset, ref allograph)) allograph += "";
                else if (match_rp(cset)) allograph = ")";
                else if (match_superset(cset)) allograph = "superset";
                else if (match_backwardsL(cset)) allograph = "-L";
                else if (match_tint(cset)) allograph = "INTtop";
                else if (match_not(cset)) allograph = "not"; // "\u00AC"
                else if (match_vectorArrow(cset)) allograph = "vectorArrow"; // "\u20D1"
                else if ((V2D.Straightness(cset.pts) < 0.1 || msalts.Contains("-") || msalts.Contains("~")) && match_mi(cset)) allograph = "-";
                else if (!msalts.Contains("b") && !msalts.Contains("L") && match_fcusp(cset)) allograph = cset.pts[0].Y < cset.last.Y ? (cset.curvatures[cset.curvatures.Length * 7 / 8] > 0 ? "fbase" : "") : "r";
                else if (match_Delta(cset, ref allograph)) ;
                #endregion
            }
            else if (cset.cusps.Length < 20 && match_arrow(cset, ref allograph)) ;

            bool msftDifferent = !allograph.Contains(topMSAlt);
            if (msftDifferent)
            {
                Console.Write("ME = " + allograph);
                if (allograph != "")
                    Console.WriteLine(" THEM = " + topMSAlt);
            }
            if (allograph == "")
            {
                #region blank_allograph
                using (RecognizerContext rc = new RecognizerContext())
                {
                    RecognitionStatus stat;
                    RecognitionResult rr;
                    rc.Strokes = csetStks;
                    rr = rc.Recognize(out stat);
                    if (topMSWord != null && rr.TopString.Length > 1)
                    {
                        topMSWord = rr.TopAlternate;
                    }
                    else topMSWord = null;
                }
                using (RecognizerContext myRecoContext = new RecognizerContext())
                {
                    msalts.Clear();
                    msxhgts.Clear();
                    msbaselines.Clear();
                    WordList wl = new WordList();
                    foreach (string word in _words)
                        wl.Add(word);
                    myRecoContext.WordList = wl;
                    myRecoContext.Factoid = Microsoft.Ink.Factoid.WordList + "|" + Microsoft.Ink.Factoid.OneChar;
                    RecognitionStatus status;
                    RecognitionResult recoResult;
                    myRecoContext.Strokes = csetStks;
                    recoResult = myRecoContext.Recognize(out status);

                    if (recoResult.TopString.Length == 1)
                    {
                        using (RecognizerContext myRecoContext2 = new RecognizerContext())
                        {
                            myRecoContext2.Factoid = Microsoft.Ink.Factoid.OneChar;
                            myRecoContext2.Strokes = csetStks;
                            recoResult = myRecoContext2.Recognize(out status);
                            foreach (RecognitionAlternate a in recoResult.GetAlternatesFromSelection())
                                if (a.ToString().Length == 1)
                                {
                                    int xhgt = a.Midline.BeginPoint.Y;
                                    int baseL = a.Baseline.BeginPoint.Y;
                                    string small = "acemnorsuvwxz.,-~αεικνοπστυω°()" + Unicode.M.MINUS_SIGN + Unicode.D.DOT_OPERATOR;
                                    if (small.Contains(a.ToString()))
                                    {
                                        xhgt = cset.s.GetBoundingBox().Top;
                                        baseL = cset.s.GetBoundingBox().Bottom;
                                    }
                                    if (topMSAlt == "")
                                    {
                                        topMSAlt = a.ToString();
                                        topMSxhgt = xhgt;
                                        topMSbaseline = baseL;
                                    }
                                    msalts.Add(a.ToString());
                                    msxhgts.Add(xhgt);
                                    msbaselines.Add(baseL);
                                }
                        }
                    }
                    else
                    {
                        foreach (RecognitionAlternate a in recoResult.GetAlternatesFromSelection())
                        {
                            if (!a.ToString().Contains("i") && !a.ToString().Contains("j") && !a.ToString().Contains("t") && Array.BinarySearch(_words, a.ToString().ToLower()) >= 0)
                            {
                                msalts.Add(a.ToString().ToLower());
                                msxhgts.Add(a.Midline.BeginPoint.Y);
                                msbaselines.Add(a.Baseline.BeginPoint.Y);
                            }
                        }
                        // if MS really thought it was a character, then override the best word choice
                        // if MS really thought it was one of our list words (possibly with different case), then take that
                        // if MS really thought it was another word, not in our list, and even if asked to look for a char only (or a char or our list),
                        //  check for a word in our list with just word factoid (produces different result than oring with single char!) then take that
                        //  word if found, otherwise the orginal word
                        if (recoResult.TopString.Length == 1 ||
                            (Array.BinarySearch(_words, recoResult.TopString.ToLower()) >= 0 &&
                            !recoResult.TopString.Contains("i") && !recoResult.TopString.Contains("j") && !recoResult.TopString.Contains("t"))
                            )
                        {
                            allograph = recoResult.TopString.ToLower();
                            baseline = recoResult.TopAlternate.Baseline.BeginPoint.Y;
                            midpt = recoResult.TopAlternate.Midline.BeginPoint.Y;
                        }
                        else if (topMSWord != null && recoResult.TopString.Length > 1)
                        {
                            using (RecognizerContext myRecoContext2 = new RecognizerContext())
                            {
                                myRecoContext2.WordList = wl;
                                myRecoContext2.Factoid = Microsoft.Ink.Factoid.WordList;
                                myRecoContext2.Strokes = csetStks;
                                recoResult = myRecoContext2.Recognize(out status);
                                if (Array.BinarySearch(_words, recoResult.TopString.ToLower()) >= 0)
                                {
                                    allograph = recoResult.TopString.ToLower();
                                    baseline = recoResult.TopAlternate.Baseline.BeginPoint.Y;
                                    midpt = recoResult.TopAlternate.Midline.BeginPoint.Y;
                                }
                                else
                                    allograph = "__MS word__";
                            }
                        }
                    }
                }
                #endregion
            }
            bool msftRecoged = false;
            if (allograph == "")
            {
                for (int i = 0; i < msalts.Count; i++)
                {
                    string a = msalts[i];
                    if (a == "." || a == "'" || a == "`" || a == "|" || a == "@" ||
                        a == "j" || a == "J" || a == "i" || a == "t" || a == "~" || a == "^" || a == "&" || a == "M" ||
                        a == "I" || a == "l")
                        continue;
                    else
                    {
                        allograph = (a == "P" || a == "U" || a == "C" || a == "O" || a == "S" || a == "V" || a == "W" || a == "Z") ? char.ToLower(a[0]).ToString() : a;
                        if (allograph == "o")
                            allograph = "0";
                        if (allograph == "(" && cset.straight < 0.2)
                            allograph = "(1";
                        if (allograph == ")" && cset.straight < 0.2)
                            allograph = ")1";
                        midpt = msxhgts[i];
                        baseline = msbaselines[i];
                        break;
                    }
                }
                if (allograph == "" && topMSAlt != "")
                {
                    allograph = topMSAlt;
                    midpt = topMSxhgt;
                    baseline = topMSbaseline;
                }
                if (allograph != "") msftRecoged = true;
            }
            if (msftDifferent && msftRecoged)
                Console.WriteLine(" THEM = " + allograph);
            if (allograph == "__MS word__") return new Recognition(csetStks, topMSWord);
            Recognition r = allograph == "" ? null : new Recognition(cset.s, allograph, baseline, midpt, msftRecoged);
            if (r != null) r.Different = msftDifferent;
            Strokes stks = cset.s.Ink.CreateStrokes();
            Point[] hull = null;
            int extender = (int)(Math.Min(cset.bbox.Width / 2, 15) * InkPixel);

            if (allograph.Contains("1") || allograph == "~" || allograph == "r" || allograph == "vr" || allograph == "-" || allograph == "^" || cset.bbox.Width / (float)cset.bbox.Height > Math.Min(3, 3 * cset.bbox.Height / InkPixel / 10) || allograph == "2")
            {
                hull = new Point[] { new Point(cset.bbox.Left-extender,  cset.bbox.Top-extender), 
                                     new Point(cset.bbox.Right+extender, cset.bbox.Top-extender),
                                     new Point(cset.bbox.Right+extender, cset.bbox.Bottom+extender), 
                                     new Point(cset.bbox.Left-extender,  cset.bbox.Bottom+extender) };
                stks = filter(cset.s.Ink.HitTest(hull, 1));
                if (stks.Count > 3)
                {
                    SortedList nearest = new SortedList();
                    for (int i = 0; i < stks.Count; i++)
                        if (stks[i].Id == cset.s.Id)
                            continue;
                        else
                        {
                            float near1, near2;
                            cset.s.NearestPoint(stks[i].GetPoint(0), out near1);
                            cset.s.NearestPoint(stks[i].GetPoint(stks[i].GetPoints().Length - 1), out near2);
                            float ind = Math.Min(near1, near2);
                            while (nearest.Contains(ind))
                                ind++;
                            nearest.Add(ind, stks[i]);
                        }
                    stks = stks.Ink.CreateStrokes(new int[] { ((Stroke)nearest.GetByIndex(0)).Id, ((Stroke)nearest.GetByIndex(1)).Id, cset.s.Id });
                }
            }
            if (!singleOnly && match_pi(cset, allograph, ref stks))
            {
                allograph = "pi";
                r = new Recognition(stks, allograph, stks.GetBoundingBox().Bottom, stks.GetBoundingBox().Top, false);
            }
            else if (!singleOnly && match_ellipsis(cset, allograph, ref stks))
            {
                allograph = "⋯";
                r = new Recognition(stks, allograph, stks.GetBoundingBox().Bottom, stks.GetBoundingBox().Top, false);
            }
            else if (!singleOnly && match3_surfaceIntegral(cset, allograph, ref stks))
            {
                allograph = "surfaceIntegral";
                r = new Recognition(stks, allograph, stks.GetBoundingBox().Bottom, stks.GetBoundingBox().Top, false);
            }
            else if (!singleOnly && match_H(cset, allograph, ref stks, ref midpt, ref allograph))
            {
                r = new Recognition(stks, allograph, stks.GetBoundingBox().Bottom, midpt, false);
            }
            else if (!singleOnly && match_K(cset, ref stks, ref midpt))
            {
                allograph = "k";
                r = new Recognition(stks, allograph, stks.GetBoundingBox().Bottom, midpt, false);
            }
            else if (!singleOnly && match_plmi(cset, ref allograph, ref stks))
            {
                r = new Recognition(stks, allograph, stks.GetBoundingBox().Bottom, stks.GetBoundingBox().Top, false);
            }
            else if (!singleOnly && match3_bbRk(cset, ref allograph, ref stks, ref midpt))
            {
                r = new Recognition(stks, allograph, stks.GetBoundingBox().Bottom, midpt, false);
            }
            else if (!singleOnly)
            {
                stks = cset.s.Ink.CreateStrokes();
                // first find any intersecting strokes               
                bool intersection = false;
                Strokes nearby = cset.s.Ink.HitTest(cset.s.GetBoundingBox(), 1);
                foreach (Stroke s in filter(nearby))
                    if (cset.s.Id != s.Id)
                    { // cset.s.Id - s.Id > 0 ) {
                        Strokes tst = cset.s.Ink.CreateStrokes(new int[] { s.Id });
                        if (cset.s.FindIntersections(tst).Length > 0)
                        {
                            stks.Add(tst[0]);
                            intersection = true;
                        }
                    }
                stks.Remove(cset.s);
                if (stks.Count == 0 && (allograph == "." || (cset.bbox.Width < InkPixel * 10 && cset.bbox.Height < InkPixel * 12)) && !intersection)
                {
                    int window = 10;
                    //int window = 20;//CJ: for ellipsis
                    foreach (Stroke p in filter(cset.s.Ink.Strokes))
                        if (p.Id == cset.s.Id - 1)
                        {
                            window = Math.Max(window, (int)(p.GetBoundingBox().Height / InkPixel));
                            break;
                        }
                    int lookUp = (int)(InkPixel * window * 2.5);
                    hull = new Point[] {     new Point((int)(cset.bbox.Left-InkPixel*window),  cset.bbox.Bottom-lookUp),
                                             new Point((int)(cset.bbox.Right+InkPixel*window), cset.bbox.Bottom-lookUp),
                                             new Point((int)(cset.bbox.Right+InkPixel*window), (int)(cset.bbox.Bottom+InkPixel*window*2.5)), 
                                             new Point((int)(cset.bbox.Left-InkPixel*window),  (int)(cset.bbox.Bottom+InkPixel*window*2.5)) };
                    stks = filter(cset.s.Ink.HitTest(hull, 1));
                    stks.Remove(cset.s);
                    intersection = false;
                    Strokes lineLike = stks.Ink.CreateStrokes();
                    float ldist = 0;
                    float aldist = 0;
                    Strokes aboveStrs = stks.Ink.CreateStrokes();
                    Strokes aboveLines = stks.Ink.CreateStrokes();
                    SortedList dists = new SortedList();
                    if (stks.Count > 0)
                    {
                        foreach (Stroke s in stks)
                        {
                            Recognition rcl = Classification(s);
                            Point nearest = getPt(s.NearestPoint(cset.bbox.Location), s.GetPoints());
                            int dist = Math.Abs(nearest.X - cset.bbox.X);
                            if (nearest.Y < cset.bbox.Top && allograph != "-" && allograph != "~")
                            { // dots dont grab anything upward except !'s and ellipses
                                if (rcl == null && s.GetBoundingBox().Width / (float)s.GetBoundingBox().Height < 0.4)
                                    aboveStrs.Add(s);
                                else if (rcl != null && !"\\()1/.,:⋯⋮⋰⋱".Contains(rcl.alt.Character.ToString()))
                                    aboveStrs.Add(s);
                                if (rcl != null && rcl.strokes.Count == 1 && ("\\()uL1l/.,:⋯⋮⋰⋱" + Unicode.M.MINUS_SIGN + Unicode.D.DIVISION_SLASH).Contains(rcl.alt.Character.ToString()))
                                {
                                    aboveLines.Add(s);
                                    aldist = dist;
                                }
                            }
                            else if (rcl != null && rcl.strokes.Count == 1 && ("\\()uL1l/.,:⋯⋮⋰⋱" + Unicode.M.MINUS_SIGN + Unicode.D.DIVISION_SLASH).Contains(rcl.alt.Character.ToString()))
                            {
                                lineLike.Add(s);
                                ldist = dist;
                            }
                            else
                                dist = Math.Abs((cset.bbox.X - (s.GetBoundingBox().Left + s.GetBoundingBox().Right) / 2));
                            while (dists.Contains(dist))
                                dist++;
                            dists.Add(dist, rcl);
                        }
                        stks.Remove(aboveStrs);
                        if (lineLike.Count > 0 && (ldist / InkPixel < 7 || ldist * .8 < (int)dists.GetKeyList()[0]))
                        {
                            stks.Clear();
                            stks.Add(lineLike);
                        }
                        else if (aboveLines.Count > 0 && (aldist / InkPixel < 7 || aldist * .8 < (int)dists.GetKeyList()[0]))
                        {
                            stks.Clear();
                            stks.Add(aboveLines);
                        }
                    }
                }
                if (allograph != "sin" && allograph != "cos")
                    if ((stks.Count == 0 && (allograph == "" || cset.bbox.Width / (float)cset.bbox.Height > Math.Min(3, 3 * cset.bbox.Height / InkPixel / 10) ||
                        "3by<c0.(>7)L/\\}-~12z".IndexOf(allograph[0]) > -1 || allograph == "superset" || cset.bbox.Width / (float)cset.bbox.Height > 3))
                        || (stks.Count == 1 && allograph != "" && (">7)".IndexOf(allograph[0]) > -1 || allograph == "superset")))
                    {
                        int xtend = (int)(Math.Max(3, (int)((Math.Max(cset.bbox.Width, cset.bbox.Height) / InkPixel / 25.0 * 7))) * InkPixel);
                        extender = (int)(Math.Max((int)(cset.bbox.Width * 1.2), 20 * InkPixel));
                        if (stks.Count == 0 || stks[0].Id > cset.s.Id)
                        {
                            intersection = false;
                            // try leftward/rightward for "5 (b+-) F G 4 Y K"
                            // try downward T "
                            //now check upward for '=' 
                            if (cset.s.GetPoints().Length > 2 && cset.straight > 0.2)
                            {
                                stks = filter(cset.s.Ink.HitTest(cset.s.GetPoints(), 1));
                                stks.Remove(cset.s);
                            }
                            if (stks.Count == 0)
                            {
                                hull = new Point[] { new Point(cset.bbox.Left-xtend,cset.bbox.Bottom+extender), new Point(cset.bbox.Right+xtend,cset.bbox.Bottom+extender),
                                         new Point(cset.bbox.Right+xtend, cset.bbox.Top-extender), new Point(cset.bbox.Left-xtend, cset.bbox.Top-extender) };
                                stks = filter(cset.s.Ink.HitTest(hull, 1));
                                stks.Remove(cset.s);
                            }
                        }
                    }

                if (stks.Count == 0)
                {
                    intersection = false;
                    hull = new Point[] { new Point(cset.bbox.Left,cset.bbox.Top), new Point((int)(cset.bbox.Left-InkPixel*2),cset.bbox.Top),
                                             new Point((int)(cset.bbox.Left-InkPixel*2), cset.bbox.Bottom), new Point(cset.bbox.Left, cset.bbox.Bottom) };
                    stks = filter(cset.s.Ink.HitTest(hull, 1));
                    stks.Remove(cset.s);
                }
                if (stks.Count > 0)
                {
                    Stroke prev = null;
                    double dist = 100000;
                    if (stks.Count > 1)
                    {
                        Strokes earlier = stks.Ink.CreateStrokes();
                        foreach (Stroke s in stks)
                            if (s.Id < cset.s.Id)
                                earlier.Add(s);
                        if (earlier.Count == 1)
                        {

                            prev = earlier[0];
                        }
                        if (prev == null)
                        {
                            if (earlier.Count > 0)
                                stks = earlier;
                            Point center = V2D.Mul(V2D.Add(cset.s.GetPoint(0), cset.s.GetPoint(cset.s.GetPoints().Length - 1)), 0.5f);
                            double centerFactor = (cset.bbox.Width / (float)cset.bbox.Height) > 2 ? cset.bbox.Width / 3 : 0;
                            foreach (Stroke p in stks)
                            {
                                float test, test2;
                                Point p1 = getPt(p.NearestPoint(center, out test), p.GetPoints());
                                Point p2 = getPt(p.NearestPoint(cset.s.GetPoint(0), out test2), p.GetPoints());
                                if (p.GetBoundingBox().Right < cset.bbox.Left || p.GetBoundingBox().Left > cset.bbox.Right) { test *= 2f; test2 *= 2f; }
                                if (test < dist || test2 + centerFactor < dist)
                                {
                                    dist = Math.Min(test, test2 + centerFactor);
                                    prev = p;
                                }
                            }
                        }
                    }
                    else
                        prev = stks[0];
                    stks = cset.s.Ink.CreateStrokes(new int[] { prev.Id });
                }
                Stroke other = stks.Count > 0 ? stks[0] : null;
                Recognition precog = other != null ? Classification(other) : null;
                Strokes contained = stks.Count > 0 ? cset.s.Ink.HitTest(stks.GetBoundingBox(), 50) : stks.Ink.CreateStrokes();
                contained.Remove(stks);
                if (precog != null)
                    contained.Remove(precog.strokes);
                contained.Remove(cset.s);


                CuspSet cset2 = null;
                if (other != null)
                    cset2 = FeaturePoints(other);
                if ((contained.Count == 0 || cset.s.Id == other.Id + 1) && other != null && other.Id != cset.s.Id && (other.Id < cset.s.Id || Classification(other) != null))
                {
                    msftRecoged = false;
                    if (Classification(other) == null)
                        precog = Recognize(cset2, false);
                    bool dontstomponuser = precog != null && precog.levelsetby == 0 ? true : false;
                    baseline = Math.Max(cset.s.GetBoundingBox().Bottom, cset2.s.GetBoundingBox().Bottom);
                    midpt = Math.Min(cset.s.GetBoundingBox().Top, cset2.s.GetBoundingBox().Top);
                    bool unistroke = precog != null && precog.strokes.Count == 1;
                    string pletter = precog != null ? (precog.allograph == Unicode.M.MINUS_SIGN.ToString() ? "-" : precog.allograph) : "";
                    string newmatch = "";
                    bool isDot = cset.dist / InkPixel < 10 && Math.Max(cset.bbox.Width, cset.bbox.Height) / cset2.dist < 0.05;

                    if (precog == null || dontstomponuser) newmatch = "";
                    if (pletter == null) /* do nothing; the previous stroke was an MS word */;
                    else if ((cset2 != null) && cset2.cusps.Length < 10)
                    { // Don't turn "Expand" (two-stroke cursive) into 'x'!)
                        if (isDot)
                        {     // if 2nd stroke is truly a dot, then don't let any of the characters that want non-dots to even see this input             
                            if (unistroke && !intersection && match2_i(cset, cset2, allograph, pletter, ref newmatch))
                            {
                                if (newmatch == "j")
                                {
                                    midpt = cset2.s.GetBoundingBox().Top;
                                    baseline = (cset2.bbox.Top + cset2.bbox.Bottom) / 2;
                                }
                                else midpt = Math.Max(cset.bbox.Top, cset2.bbox.Top);
                            }
                            else if (unistroke && !intersection && match2_excl(cset, cset2, allograph, pletter, ref midpt, ref newmatch)) newmatch = "!";
                            else if (unistroke && !intersection && match2_j(cset, cset2, allograph, pletter, ref newmatch, ref baseline)) { midpt = cset2.s.GetBoundingBox().Top; newmatch = "j"; }
                            else if (!intersection && match2_sin(cset, cset2, allograph, pletter)) newmatch = "sin";
                            //else if (unistroke && !intersection && pletter == "." && allograph == ".") newmatch = ":";
                            else if (unistroke && !intersection && match2_cdots(cset, cset2, allograph, pletter)) newmatch = "..";
                            else if (unistroke && !intersection && match2_vdots(cset, cset2, allograph, pletter)) newmatch = ":";
                            else if (unistroke && !intersection && match2_ddots_up_right(cset, cset2, allograph, pletter)) newmatch = "⋰";
                            else if (unistroke && !intersection && match2_ddots_down_right(cset, cset2, allograph, pletter)) newmatch = "⋱";
                            else if (!intersection && match3_cdots(cset, cset2, allograph, pletter)) newmatch = "⋯";
                            else if (!intersection && match3_vdots(cset, cset2, allograph, pletter)) newmatch = "⋮";
                            else if (!intersection && match3_ddots_up_right(cset, cset2, allograph, pletter)) newmatch = "⋰";
                            else if (!intersection && match3_ddots_down_right(cset, cset2, allograph, pletter)) newmatch = "⋱";
                            else if (!intersection && match2_circledDot(cset, cset2, allograph, pletter)) newmatch = "circledDot";
                            else if (unistroke && !intersection && match2_eq(cset, cset2, allograph, pletter)) newmatch = "=";
                        }
                        else if (!intersection && match3_identicalTo(cset, cset2, allograph, pletter, ref newmatch)) { } //also checks for "almostEqTo"
                        else if (!intersection && match3_approximatelyEqualTo(cset, cset2, allograph, pletter, ref newmatch)) { }// also checks for "asymptoticallyEqTo"
                        else if (unistroke && !intersection && match2_eq(cset, cset2, allograph, pletter)) newmatch = "=";
                        else if (!intersection && match3_Xi(cset, cset2, allograph, pletter, ref newmatch)) { }
                        else if (!intersection && match2_lessThanOrEqualTo(cset, cset2, allograph, pletter, ref newmatch)) { }
                        //"match2_lessThanOrEqualTo" also checks for GREATER-THAN OR EQUAL TO, SUPERSET OF OR EQUAL TO, & SUBSET OF OR EQUAL TO
                        else if (intersection && match2_pathIntegral(cset, cset2, allograph, pletter)) newmatch = "pathIntegral";
                        else if (match2_E(cset, cset2, allograph, pletter, ref midpt)) newmatch = "E";
                        else if (match2_exists(cset, cset2, allograph, pletter, ref midpt)) newmatch = "exists";
                        else if (match2_F(cset, cset2, allograph, pletter, precog, ref midpt)) newmatch = "F";
                        else if (!unistroke && match2_I(cset, cset2, allograph, pletter, ref midpt)) newmatch = "I";
                        else if (match2_5(cset, cset2, allograph, pletter, ref midpt, ref stks)) newmatch = "5";
                        else if (match2_forall(cset, cset2, allograph, pletter, ref midpt)) newmatch = "forall";
                        else if (unistroke && !intersection && match2_i(cset, cset2, allograph, pletter, ref newmatch))
                        {
                            if (newmatch == "j")
                            {
                                midpt = cset2.s.GetBoundingBox().Top;
                                baseline = (cset2.bbox.Top + cset2.bbox.Bottom) / 2;
                            }
                            else midpt = Math.Max(cset.bbox.Top, cset2.bbox.Top);
                            midpt = Math.Max(cset.bbox.Top, cset2.bbox.Top);
                        }
                        else if (unistroke && !intersection && match2_excl(cset, cset2, allograph, pletter, ref midpt, ref newmatch)) newmatch = "!";
                        else if (unistroke && !intersection && match2_j(cset, cset2, allograph, pletter, ref newmatch, ref baseline)) { midpt = cset2.s.GetBoundingBox().Top; newmatch = "j"; }
                        else if (unistroke && match2_G(cset, cset2, allograph, pletter, ref midpt)) newmatch = "G";
                        else if (match2_bbC(cset, cset2, allograph, pletter)) newmatch = "bbC"; //"\u2102"
                        else if (unistroke && match2_lambda(cset, cset2, allograph, pletter, ref midpt)) newmatch = "lambda";
                        else if (match2_circledSlash(cset, cset2, allograph, pletter, ref newmatch)) { } //also checks for circledBackslash
                        else if (match3_circledTimes(cset, cset2, allograph, pletter)) newmatch = "circledTimes";
                        else if (match2_bbZ(cset, cset2, allograph, pletter)) newmatch = "bbZ"; //"\u2124"
                        else if (intersection && match2_x(cset, cset2, allograph, pletter, ref stks)) newmatch = "x";
                        else if (unistroke && match2_T(cset, cset2, allograph, pletter, ref midpt, ref stks, ref newmatch)) newmatch += "";
                        else if (match2_Perp(cset, cset2, allograph, pletter, ref midpt, ref stks, ref newmatch)) newmatch += "";
                        else if (unistroke && match2_Sigma(cset, cset2, allograph, pletter, ref midpt, ref stks)) newmatch += "Sigma";
                        else if (unistroke && match2_t(cset, cset2, allograph, pletter, ref midpt, ref newmatch)) newmatch += "";
                        else if (unistroke && match2_memberof(cset, cset2, allograph, pletter, ref midpt)) newmatch = "memberof";
                        else if (unistroke && match2_l1arrow(cset, cset2, allograph, pletter, ref midpt)) newmatch = "larrow-2"; // + sometimes grabbed this wrongly if this line was after it
                        else if (unistroke && match2_A(cset, cset2, allograph, pletter, ref midpt)) newmatch = "A";
                        else if (unistroke && match2_f(cset, cset2, allograph, pletter, ref midpt)) newmatch = "f";
                        else if ((unistroke || pletter == "perp") && intersection && match2_pl(cset, cset2, allograph, pletter, ref stks, ref newmatch)) newmatch += "";
                        else if (match3_mapsTo(cset, cset2, allograph, pletter, ref newmatch)) { } //also checks for assertion
                        else if (intersection && match2_Psi(cset, cset2, allograph, pletter, ref midpt)) newmatch = "Psi";
                        else if (unistroke && match2_4(cset, cset2, allograph, pletter, ref midpt)) newmatch = "4";
                        else if (unistroke && match2_y(cset, cset2, allograph, pletter, ref midpt)) newmatch = "y4";
                        else if (unistroke && match2_Y(cset, cset2, allograph, pletter, ref midpt)) newmatch = "Y";
                        else if (unistroke && match2_2(cset, cset2, allograph, pletter, ref midpt)) newmatch = "2";
                        else if (intersection && match2_7(cset, cset2, allograph, pletter, ref midpt)) newmatch = "7";
                        else if (unistroke && match2_Estart(cset, cset2, allograph, pletter)) newmatch = "t";
                        else if (intersection && match2_z(cset, cset2, allograph, pletter)) newmatch = "zed";
                        else if (unistroke && match2_k(cset, cset2, allograph, pletter, intersection, ref midpt, ref newmatch)) newmatch += "";
                        else if (unistroke && match2_xx(cset, cset2, allograph, pletter)) newmatch = "xx";
                        else if (unistroke && match2_8(cset, cset2, allograph, pletter, ref midpt)) newmatch = "8";
                        else if (match2_phi(cset, cset2, allograph, pletter, intersection, ref newmatch)) { midpt = cset.bbox.Bottom; }//also checks for circled vertical bar
                        else if (intersection && match2_Q(cset, cset2, allograph, pletter, ref midpt)) newmatch = "Q";
                        else if (match3_bbQ(cset, cset2, allograph, pletter)) newmatch = "bbQ"; //"\u211A"
                        else if (match2_bbN(cset, cset2, allograph, pletter)) newmatch = "bbN"; //"\u2115"
                        else if (unistroke && match2_Rk(cset, cset2, allograph, pletter, intersection, ref midpt, ref newmatch)) newmatch += "";
                        else if (unistroke && match2_P(cset, cset2, allograph, pletter, intersection, ref midpt)) newmatch = "P";
                        else if (unistroke && match2_D(cset, cset2, allograph, pletter, intersection, ref midpt)) newmatch = "D";
                        else if (unistroke && match2_b(cset, cset2, allograph, pletter, ref midpt)) newmatch = "b";
                        else if (unistroke && match2_B(cset, cset2, allograph, pletter, intersection, ref newmatch, ref midpt)) newmatch += "";
                        else if (unistroke && match2_a(cset, cset2, allograph, pletter, ref midpt)) newmatch = "ad";
                        else if (intersection && match2_d(cset, cset2, allograph, pletter, ref midpt)) newmatch = "da";
                        else if (!intersection && match2_sin(cset, cset2, allograph, pletter)) newmatch = "sin";
                        else if (unistroke && match2_theta(cset, cset2, allograph, pletter, ref midpt, ref newmatch)) { }
                        //"match2_theta" also checks for GREEK CAPITAL LETTER THETA and CIRCLED MINUS
                        else if (match3_circledPlus(cset, cset2, allograph, pletter)) newmatch = "circledPlus";
                        else if (!intersection && unistroke && match2_circledDot(cset, cset2, allograph, pletter)) newmatch = "circledDot";
                        else if (intersection && match2_angle(cset, cset2, allograph, pletter, ref newmatch)) { } //checks for measured and spherical angles
                        else if (intersection && match3_notequal(cset, cset2, allograph, precog, ref stks, ref midpt)) newmatch = "≠";
                        else if (match3_r2arrow(cset, cset2, allograph, precog, ref stks, ref midpt)) newmatch = "rdoublearrow";
                        else if (unistroke && match2_r1arrow(cset, cset2, allograph, pletter, ref midpt)) newmatch = "rarrow-2";
                        else if (unistroke && !intersection && match2_cdots(cset, cset2, allograph, pletter)) newmatch = "..";
                        else if (unistroke && !intersection && match2_vdots(cset, cset2, allograph, pletter)) newmatch = ":";
                        else if (unistroke && !intersection && match2_ddots_up_right(cset, cset2, allograph, pletter)) newmatch = "⋰";
                        else if (unistroke && !intersection && match2_ddots_down_right(cset, cset2, allograph, pletter)) newmatch = "⋱";
                        else if (!intersection && match3_cdots(cset, cset2, allograph, pletter)) newmatch = "⋯";
                        else if (!intersection && match3_vdots(cset, cset2, allograph, pletter)) newmatch = "⋮";
                        else if (!intersection && match3_ddots_up_right(cset, cset2, allograph, pletter)) newmatch = "⋰";
                        else if (!intersection && match3_ddots_down_right(cset, cset2, allograph, pletter)) newmatch = "⋱";
                    }


                    //else if(match_bar(cset, precog, ref allograph, ref stks))
                    //    r = new Recognition(stks, allograph, stks.GetBoundingBox().Bottom, stks.GetBoundingBox().Top, false);
                    if (newmatch == "" &&
                        (intersection || allograph == "." || (cset.bbox.Height < cset2.bbox.Height && (allograph.Contains("1") || cset2.cusps.Length > 5))))
                    {
                        using (RecognizerContext myRecoContext = new RecognizerContext())
                        {
                            WordList wl = new WordList();
                            foreach (string word in _words) wl.Add(word);
                            myRecoContext.WordList = wl;
                            myRecoContext.Factoid = Microsoft.Ink.Factoid.WordList;
                            RecognitionStatus status;
                            RecognitionResult recoResult;
                            Strokes tmpstrokes = csetStks;
                            if (precog == null)
                                precog = new Recognition(cset2.s, ".", cset.bbox.Top, false);
                            foreach (Stroke s in precog.strokes)
                                if (!s.Deleted)
                                    tmpstrokes.Add(s);
                            tmpstrokes.Add(cset2.s);
                            myRecoContext.Strokes = tmpstrokes;
                            recoResult = myRecoContext.Recognize(out status);
                            string topString = recoResult.TopString;
                            int raw = -1;
                            int numPossStrokes = 1;
                            string lower = char.ToLower(recoResult.TopString[0]).ToString() + recoResult.TopString.Substring(1);
                            for (int i = 0; i < lower.Length; i++)
                                if (lower[i] == 'i' || lower[i] == 'j' || lower[i] == 't' || lower[i] == 'x')
                                    numPossStrokes++;
                            if (numPossStrokes < myRecoContext.Strokes.Count)
                                lower = topString = "";
                            if (lower.Length == 1)
                            {
                                if (cset.bbox.Width / (float)cset2.bbox.Width > 2.75 || ((cset2.bbox.Right - cset.bbox.Left + 0.0) / cset2.bbox.Width > 1.75))
                                    topString = lower = "";
                                else if (cset.s.FindIntersections(cset2.s.Ink.CreateStrokes(new int[] { cset2.s.Id })).Length < 2 ||
                                    !"+XYTFJNZztfy".Contains(lower))
                                    lower = recoResult.TopString;
                                else topString = lower = "";
                            }
                            else if ((cset.bbox.Width / (float)cset.bbox.Height > 3.5 ||
                                     cset2.bbox.Width / (float)cset2.bbox.Height > 3.5) && recoResult.TopString.ToLower() == "dy")
                                topString = lower = "";
                            if (recoResult.TopConfidence == RecognitionConfidence.Poor)
                                topString = lower = "";
                            double boundRatio = Math.Max(cset2.bbox.Width, cset2.bbox.Height) / (float)Math.Max(cset.bbox.Width, cset.bbox.Height);
                            if ((raw = Array.BinarySearch(_words, topString)) >= 0 ||
                                Array.BinarySearch(_words, lower) >= 0 || (boundRatio < 2 && boundRatio > 0.5 && cset2.s.Id < cset.s.Id &&
                                intersection && lower.Length > 0 && "ABQ+4xXYTFJKNPRZzfty".Contains(lower)))
                            {
                                newmatch = raw >= 0 ? topString : lower;
                                midpt = recoResult.TopAlternate.Midline.BeginPoint.Y;
                                baseline = recoResult.TopAlternate.Baseline.BeginPoint.Y;
                                msftRecoged = true;
                            }
                        }
                    }
                    if (newmatch != "")
                    {
                        if (newmatch == "x" || newmatch == "T" || newmatch == "5" || newmatch == "perp" || newmatch == "rdoublearrow")
                        {
                        }
                        else if ((newmatch == "I" || newmatch == "E" || newmatch == "F" || newmatch == "aproxEqTo"
                          || newmatch == "rdoublearrow" || newmatch == "bbQ" || newmatch == "circledPlus" ||
                          newmatch == "⋰" || newmatch == "⋱" || newmatch == "⋯" || newmatch == "⋮" || newmatch == "⋰" ||
                          newmatch == "⋱" || newmatch == "≠" || newmatch == "≡" || newmatch == "bbR") && precog != null)
                        {
                            stks = csetStks;
                            stks.Add(precog.strokes);
                            baseline = stks.GetBoundingBox().Bottom;
                            midpt = (stks.GetBoundingBox().Top + baseline) / 2;
                        }
                        else
                        {
                            stks = csetStks;
                            stks.Add(precog.strokes);
                        }
                        r = new Recognition(stks, newmatch, baseline, midpt, msftRecoged);
                    }
                }
            }
            return r;
        }
        public static int miny(int start, int end, Point[] inkPts)
        {
            int ind;
            return miny(start, end, inkPts, out ind);
        }
        public static int miny(int start, int end, Point[] inkPts, out int ind)
        {
            int min = int.MaxValue;
            ind = -1;
            for (int i = start; i < end; i++)
                if (inkPts[i].Y < min)
                {
                    min = inkPts[i].Y;
                    ind = i;
                }
            return min;
        }
        public static int maxy(int start, int end, Point[] inkPts)
        {
            int ind;
            return maxy(start, end, inkPts, out ind);
        }
        public static int maxy(int start, int end, Point[] inkPts, out int ind)
        {
            int max = int.MinValue;
            ind = -1;
            for (int i = start; i < end; i++)
                if (inkPts[i].Y > max)
                {
                    max = inkPts[i].Y;
                    ind = i;
                }
            return max;
        }
        public static int maxlocaly(int start, int end, Point[] inkPts)
        {
            int ind;
            return maxlocaly(start, end, inkPts, out ind);
        }
        public static int maxlocaly(int start, int end, Point[] inkPts, out int ind)
        {
            int max = int.MinValue;
            ind = -1;
            for (int i = start; i < end; i++)
                if (inkPts[i].Y > max && (i == 0 || inkPts[i - 1].Y < inkPts[i].Y) && (i == end - 1 || inkPts[i].Y < inkPts[i + 1].Y))
                {
                    max = inkPts[i].Y;
                    ind = i;
                }
            return max;
        }
        public static int minx(int start, int end, Point[] inkPts)
        {
            int ind;
            return minx(start, end, inkPts, out ind);
        }
        public static int minx(int start, int end, Point[] inkPts, out int ind)
        {
            int min = int.MaxValue;
            ind = -1;
            for (int i = start; i < end; i++)
                if (inkPts[i].X < min)
                {
                    ind = i;
                    min = inkPts[i].X;
                }
            return min;
        }
        public static int maxx(int start, int end, Point[] inkPts)
        {
            int ind;
            return maxx(start, end, inkPts, out ind);
        }
        public static int maxx(int start, int end, Point[] inkPts, out int ind)
        {
            int max = int.MinValue;
            ind = -1;
            for (int i = start; i < end; i++)
                if (inkPts[i].X > max)
                {
                    max = inkPts[i].X;
                    ind = i;
                }
            return max;
        }
        bool match_a9gq_start(CuspSet s, bool lenient, out int maxc, out int startCusp)
        {
            maxc = -1;
            int apex = 0;
            startCusp = 0;
            if (s.cusps.Length > 3)
            {
                double retraceAng = angle(s.cusps[1].pt, s.pts[Math.Max(0, s.cusps[1].index - 2)], V2D.Sub(s.cusps[1].pt, s.pts[s.cusps[1].index + 2]));
                retraceAng = Math.Min(retraceAng, angle(s.cusps[1].pt, s.cusps[0].pt, V2D.Sub(s.cusps[1].pt, s.pts[s.cusps[1].index + 2])));
                if (s.cusps[1].dist / V2D.Dist(s.cusps[1].pt, s.cusps[0].pt) < 1.5 && s.cusps[0].pt.X < s.cusps[1].pt.X && s.cusps[0].pt.Y > s.cusps[1].pt.Y && retraceAng < 90)
                    apex = 1;
                if (s.cusps.Length > 3)
                {
                    double retAng = angle(s.cusps[2].pt, s.pts[s.cusps[2].index - 2], V2D.Sub(s.cusps[2].pt, s.pts[s.cusps[2].index + 2]));
                    if (s.cusps.Length > 3 && s.cusps[2].top && s.cusps[1].pt.Y < s.cusps[2].pt.Y && retAng < 15 &&
                    V2D.Dist(s.cusps[2].pt, s.cusps[1].pt) / (s.cusps[2].dist - s.cusps[1].dist) > 0.95)
                    {
                        retraceAng = retAng;
                        apex = 2;
                    }
                }
                if (apex > 0 && retraceAng > 35 && s.cusps[apex].dist / s.dist > .2)
                {
                    PointF apIn = V2D.Normalize(V2D.Sub(s.cusps[apex - 1].pt, s.cusps[apex].pt));
                    PointF apOut = V2D.Normalize(V2D.Sub(s.cusps[apex + 1].pt, s.cusps[apex].pt));
                    if (apOut.X > apIn.X)
                        return false;
                }
            }
            startCusp = apex;
            if (startCusp == 0)
                lenient = true;
            double mi_y = miny(s.cusps[startCusp].index, s.cusps[startCusp + 1].index, s.pts);
            PointF startdir = V2D.Normalize(V2D.Sub(s.pts[s.cusps[startCusp].index + 2], s.cusps[startCusp].pt));
            double angS = angle(startdir, new Point(-1, -2));
            if (angS > 90 && startdir.Y > 1)
                return false;
            if (s.cusps.Length < 3 || (mi_y - s.bbox.Top + 0.0) / s.bbox.Height > 0.2)
                return false;
            double near = .65 * Math.Max(s.bbox.Width, s.bbox.Height);
            Point realstart = V2D.Dist(s.pts[0], s.s.GetPoint(0)) / InkPixel < 1 || angle(s.s.GetPoint(0), s.pts[0], V2D.Sub(s.pts[s.cusps[1].index / 2], s.pts[0])) < 20 ? s.pts[0] : s.s.GetPoint(0);
            for (int i = startCusp + 1; i < s.cusps.Length - 1; i++)
            {
                double disttostart = V2D.Dist(s.cusps[i].pt, s.cusps[startCusp].pt);
                if (startCusp == 0)
                    disttostart = Math.Max(disttostart, V2D.Dist(s.cusps[i].pt, realstart));
                if (startCusp == 0 && V2D.Dist(s.cusps[i].pt, realstart) < disttostart)
                    disttostart = V2D.Dist(s.cusps[i].pt, realstart);
                if (disttostart / (s.cusps[i].dist - s.cusps[startCusp].dist) < .9 &&
                    (Math.Sign(s.cusps[i].curvature) != -1 ||
                     Math.Abs(s.cusps[i].curvature) > 2.5 ||
                    (Math.Abs(s.cusps[i].curvature) > .5 &&
                     s.intersects.Length > 0 &&
                     s.cusps[i].index > s.intersects[0])) && near > disttostart)
                {
                    maxc = i;
                    near = disttostart;
                }
                else if (Math.Abs(s.cusps[i].curvature) > 2.2)
                    break;
            }
            if (maxc == -1 || Math.Sign(s.avgCurve(startCusp, maxc)) != -1)
                return false;
            int botind;
            int ma_y = maxy(s.cusps[startCusp].index, s.cusps[maxc].index, s.pts, out botind);
            if (s.pts[botind].Y < s.cusps[maxc].pt.Y)
            {
                double maxcAng = angle(s.cusps[maxc].pt, s.pts[(int)(s.cusps[maxc].index * .9)],
                    V2D.Sub(s.cusps[maxc].pt, s.pts[(int)(s.cusps[maxc].index * 1.1)]));
                if (maxcAng > 80)
                    return false;
            }
            int top_y = miny(s.cusps[startCusp].index, botind, s.pts);
            if ((top_y - mi_y + 0.0) / (maxy(0, s.cusps[maxc].index, s.pts) - mi_y) > 0.35)
                return false;
            if ((s.cusps[maxc].dist - s.cusps[startCusp].dist) / V2D.Dist(s.cusps[startCusp].pt, s.cusps[maxc].pt) < 1.5)
                return false;
            if (Math.Abs(s.cusps[maxc].curvature) < .15)
                return false;
            int loopstartind = s.cusps[startCusp].index;
            double loopy = 0.45;
            if (maxc > startCusp + 1)
            {
                if (s.avgCurve(startCusp + 1, maxc) > 0.02)
                    return false;
                double l1straight = V2D.Straightness(s.pts, s.cusps[startCusp].index, s.cusps[startCusp + 1].index);
                double l2straight = V2D.Straightness(s.pts, s.cusps[startCusp + 1].index, s.cusps[maxc].index);
                double lobeAng = angle(s.cusps[startCusp].pt, s.cusps[startCusp + 1].pt, V2D.Sub(s.cusps[maxc].pt, s.cusps[startCusp + 1].pt));
                if (l1straight < 0.075 && l2straight < 0.075)
                {
                    if (lobeAng > 25)
                        return false;
                }
                else
                {
                    double diffSize = Math.Max(s.cusps[startCusp + 1].dist - s.cusps[startCusp].dist, s.cusps[maxc].dist - s.cusps[startCusp + 1].dist) /
                                        V2D.Dist(s.cusps[startCusp + 1].pt, s.cusps[startCusp].pt) - 1;
                    if (lobeAng < 15 && diffSize < .03 && angle(s.cusps[startCusp + 1].pt, s.cusps[startCusp].pt, new PointF(0, 1)) < 15)
                        return false;
                }
                //if (l1straight > 0.12)
                loopy += l1straight;
            }
            if ((s.cusps[startCusp + 1].dist - s.cusps[startCusp].dist) / s.bbox.Height > 0.5 && s.bbox.Height / InkPixel > 10 &&
                angle(s.cusps[startCusp + 1].pt, s.cusps[startCusp].pt, new PointF(0, 1)) < 15 &&
                 V2D.Straightness(s.pts, s.cusps[startCusp].index, s.cusps[startCusp + 1].index) < 0.15)
                return false;
            Point loopstart = s.pts[loopstartind];
            double closeloopdist = Math.Min(Math.Abs(realstart.X - s.cusps[maxc].pt.X), Math.Abs(loopstart.X - s.cusps[maxc].pt.X));
            double closeloopdistY = Math.Min(Math.Abs(realstart.Y - s.cusps[maxc].pt.Y), Math.Abs(loopstart.Y - s.cusps[maxc].pt.Y));
            double lobewidth = maxx(s.cusps[startCusp].index, s.cusps[maxc].index, s.pts) - minx(s.cusps[startCusp].index, s.cusps[maxc].index, s.pts);
            if (closeloopdist / lobewidth > 0.55)
                return false;
            if (s.cusps[startCusp].pt.X > s.cusps[maxc].pt.X)
                return closeloopdistY / Math.Max(s.bbox.Width, s.bbox.Height) < loopy * Math.Min(1, Math.Abs(-1.57 - s.angles[s.cusps[maxc].index - 2]) / 1.57);
            if (closeloopdist / (s.cusps[maxc].dist - s.cusps[startCusp].dist) > (lenient ? 0.30 : .18))
                return false;
            if (closeloopdistY / (s.cusps[maxc].dist - s.cusps[startCusp].dist) > (lenient ? 0.4 : 0.30))
                return false;
            return true;
        }
        bool match_9gq(CuspSet s, bool lenient, out double en_curve)
        {
            en_curve = 0;
            int maxc, startc;
            if (!match_a9gq_start(s, lenient, out maxc, out startc))
                return false;
            double minlobe = maxy(s.cusps[0].index, s.cusps[maxc].index, s.pts);
            double mintail = maxy(s.cusps[maxc].index, s.pts.Length, s.pts);
            if ((mintail - minlobe) / s.bbox.Height < .25)
                return false;
            int inset = Math.Abs(s.curvatures[s.curvatures.Length - 1]) > .3 || Math.Abs(s.curvatures[s.curvatures.Length - 2]) > .4 ||
                Math.Abs(s.curvatures[s.curvatures.Length - 3]) > .50 ? 6 : 3;
            int endtail = s.pts.Length - inset;
            int starttail = (Math.Max(s.pts.Length - 10, s.cusps[maxc].index) + s.pts.Length - inset) / 2;
            if (s.intersects.Length / 2 * 2 == s.intersects.Length && s.intersects.Length >= 2 && s.intersects[s.intersects.Length - 2] > s.cusps[maxc].index)
            {
                endtail = s.intersects[s.intersects.Length - 1];
                starttail = s.intersects[s.intersects.Length - 2];
            }
            en_curve = s.avgCurveSeg(starttail, endtail);
            return true;
        }
        bool match_a(CuspSet s)
        {
            int maxc, startc;
            if (s.cusps.Length > 7)
                return false;
            if (!match_a9gq_start(s, false, out maxc, out startc))
                return false;
            if (s.cusps.Length - maxc > 2)
            {
                if (s.cusps[s.l].top || s.cusps[s.nl].curvature > 0.05)
                    return false;
            }
            int mlobeindex, mintailind;
            double minlobe = maxy(s.cusps[0].index, s.cusps[maxc].index, s.pts, out mlobeindex);
            double mintail = maxy(s.cusps[maxc].index, s.pts.Length, s.pts, out mintailind);
            bool left;
            double straightness = V2D.Straightness(s.pts, mlobeindex, s.cusps[maxc].index);
            double wavy = (s.cusps[maxc].dist - s.distances[mlobeindex]) / V2D.Dist(s.cusps[maxc].pt, s.pts[mlobeindex]);
            if (straightness > 0.2 && wavy > 1.35)
                return false;
            double tailAng = angle(s.pts[mintailind], s.cusps[maxc].pt, new PointF(0, 1));
            double tailAngRel = angle(s.pts[mintailind], s.cusps[maxc].pt, V2D.Sub(s.pts[mintailind], s.last));
            if ((s.bbox.Bottom - s.pts[mintailind].Y + 0.0) / s.bbox.Height > 0.25 && tailAng > 60)
                return false;
            if ((s.dist - s.distances[mintailind]) / s.bbox.Height > 0.1 && tailAngRel < 10)
                return false;
            if (s.pts[mintailind].X < s.cusps[maxc].pt.X && V2D.Dist(s.pts[0], s.cusps[maxc].pt) / s.bbox.Height > 0.45 + 1 / (s.bbox.Height / InkPixel))
                return false;
            if (tailAng < 15 || s.pts[mintailind].X < s.cusps[maxc].pt.X)
                if (((mintail - minlobe) / s.bbox.Height > Math.Max(0.28 + wavy / 50, Math.Min(0.5, .28 + (tailAng - 10) / 180.0 * Math.PI / 3))) || (mintail - minlobe) / s.bbox.Height < -0.6)
                    return false;
            if ((mintail - minlobe) / s.bbox.Height > 0.225 && V2D.Straightness(s.pts, s.cusps[maxc].index, s.pts.Length) > 0.5)
                return false;
            int maxtailind;
            double taildist = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.cusps[maxc].pt)), out left, out maxtailind, s.cusps[maxc].index, s.pts.Length);
            double tailCurve = taildist / V2D.Dist(s.last, s.cusps[maxc].pt);
            double taildistang = angle(s.pts[maxtailind], V2D.Mul(V2D.Add(s.cusps[maxc].pt, s.last), 0.5f), new Point(-1, 1));
            left = taildistang < 110;
            if (s.intersects.Length > 0 && tailAng > 25 && s.last.X < s.cusps[maxc].pt.X && V2D.Dist(s.cusps[maxc].pt, s.pts[0]) / s.bbox.Height > 0.25)
                return false;
            if (V2D.Dist(s.cusps[maxc].pt, s.s.GetPoint(0)) / Math.Max(s.bbox.Width, s.bbox.Height) > 0.65)
                return false;
            if ((tailCurve > (0.1 + (s.pts[mintailind].X < s.cusps[maxc].pt.X ? 0 : tailAng / 100)) && !left) || tailAng > 83)//(tailCurve > 0.6 && left))
                return false;
            return true;
        }
        public bool match_9(CuspSet s, ref int xhgt)
        {
            int maxc, startc;
            if (!match_a9gq_start(s, false, out maxc, out startc))
                return false;
            if (startc > 0)
            {
                int tailx = minx(0, s.cusps[startc].index, s.pts);
                int lobex = minx(s.cusps[startc].index, s.cusps[maxc].index, s.pts);
                if (tailx < lobex)
                    return false;
            }
            int tailcusp = s.l - maxc > 1 ? maxc + 1 : -1;
            double en_curve;
            if (!match_9gq(s, false, out en_curve))
                return false;
            if ((s.dist - s.cusps[maxc].dist) / s.bbox.Height > 1.2 && V2D.Straightness(s.pts, s.cusps[maxc].index, s.pts.Length) > 0.1)
                return false;
            int mixind, mix = minx(0, s.pts.Length, s.pts, out mixind);
            //if (V2D.Straightness(s.pts, 0, mixind) < 0.15 && V2D.Dist(s.pts[0], s.cusps[maxc].pt)/s.bbox.Height > 0.25)
            //    return false;
            bool left;
            if (V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.s.GetPoint(s.s.GetPoints().Length - 1), s.cusps[maxc].pt)), out left, s.cusps[maxc].index, s.pts.Length) / V2D.Dist(s.s.GetPoint(s.s.GetPoints().Length - 1), s.cusps[maxc].pt) >
                (left ? 0.15 : 0.11))
                return false;
            xhgt = (s.bbox.Top + s.bbox.Bottom) / 2;
            return true;
        }
        bool match_g(CuspSet s, ref int baseline, ref string allograph)
        {
            int maxc, startc;
            if (!match_a9gq_start(s, true, out maxc, out startc))
                return false;
            bool lenient = true;
            if (maxc == 3 && Math.Abs(s.cusps[1].curvature) > 0.25 && Math.Abs(s.cusps[2].curvature) > 0.25)
                lenient = false;
            double en_curve;
            bool left;
            if (!match_9gq(s, lenient, out en_curve))
                return false;
            if (Math.Abs(en_curve) < 0.065)
            {
                if ((s.dist - s.cusps[maxc].dist) / s.bbox.Height < 1)
                {
                    Point rlast = s.s.GetPoint(s.s.GetPoints().Length - 1);
                    if (V2D.Dist(rlast, s.last) / s.bbox.Height < .1 || V2D.Det(V2D.Sub(rlast, s.last), V2D.Sub(s.pts[s.pts.Length - 2], s.last)) < 0)
                        if (V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.cusps[maxc].pt)), out left, s.cusps[maxc].index, s.pts.Length) / s.bbox.Height < 0.09 || left)
                            return false;
                }
            }
            if (Math.Sign(s.avgCurve(maxc, s.l)) != 1)
                return false;
            int botind, botloopind;
            maxy(0, s.cusps[maxc].index + 1, s.pts, out botind);
            maxy(s.cusps[maxc].index, s.pts.Length, s.pts, out botloopind);
            if ((s.distances[botloopind] - s.cusps[maxc].dist) / V2D.Dist(s.pts[botloopind], s.cusps[maxc].pt) > 1.2)
            {
                for (int i = maxc + 1; i < s.l; i++)
                    if (s.cusps[i].index > botloopind)
                        break;
                    else if (s.cusps[i].curvature < 0)
                        return false;
            }
            if (botind < s.pts.Length - 4 && Math.Abs(s.angles[botloopind]) > Math.PI / 2)
                return false;
            int int2 = s.intersects.Length > 1 ? s.intersects[s.intersects.Length - 1] : -1;
            int int1 = s.intersects.Length > 1 ? s.intersects[s.intersects.Length - 2] : -1;
            if (s.intersects.Length > 0 && V2D.Dist(s.last, s.pts[0]) / s.bbox.Height < 0.3 &&
                ((int2 > botind && s.angles[s.cusps[maxc].index - 3] < 0) || int2 < 0 ||
                (s.pts[int2].X < s.cusps[maxc].pt.X && int1 < s.cusps[maxc].index)))
                return false;
            double tailang = angle(s.last, s.pts[s.pts.Length - 5], new PointF(-1, 0));
            if (botind == s.cusps[maxc].index && tailang < 20)
                return false;
            if ((int1 < botloopind && int2 > botloopind) || (s.pts[botloopind].Y - s.last.Y + 0.0) / s.bbox.Height > 0.3)
                allograph = "g";
            else allograph = "g9";
            return true;
        }
        bool match_q(CuspSet s, ref int baseline, ref string allograph)
        {
            int maxc, startc;
            if (!match_a9gq_start(s, true, out maxc, out startc))
                return false;
            int tailcusp = s.l - maxc > 1 ? maxc + 1 : -1;
            double en_curve;
            if (!match_9gq(s, true, out en_curve) || (Math.Abs(en_curve) < 0.065 &&
                (tailcusp != -1 && V2D.Dist(s.cusps[maxc].pt, s.cusps[s.l].pt) / (s.dist - s.cusps[maxc].dist) > 0.95)))
                return false;
            if (maxc > 1)
            {
                int leftind; minx(0, s.cusps[maxc].index, s.pts, out leftind);
                int botleftind; minx(s.cusps[maxc].index, s.pts.Length, s.pts, out botleftind);
                if (leftind != -1 && botleftind != -1 && V2D.Straightness(s.pts, 0, leftind) < 0.15 &&
                    V2D.Straightness(s.pts, leftind, s.cusps[maxc].index) < 0.15 &&
                    V2D.Straightness(s.pts, s.cusps[maxc].index, botleftind) < 0.15 &&
                    (((s.dist - s.distances[botleftind]) / (s.pts[botleftind].Y - s.last.Y) < 1.2) && V2D.Straightness(s.pts, botleftind, s.pts.Length) < 0.15))
                    return false;
            }
            if (s.intersects.Length == 2)
            {
                int maxlobeind, max = maxx(s.intersects[0], s.intersects[1], s.pts, out maxlobeind);
                double lobeang = angle(s.pts[maxlobeind], s.pts[s.intersects[0]], new PointF(1, 0));
                if (maxlobeind != s.intersects[1] && maxlobeind != s.intersects[0])
                {
                    if (s.angles[s.intersects[s.intersects.Length - 1]] > -Math.PI / 4 && lobeang < 39)
                        return false;
                }
                else if (s.angles[s.cusps[maxc].index + 1] > -Math.PI / 4)
                    return false;
            }
            else if (s.intersects.Length == 0)
                if (s.angles[s.cusps[maxc].index + 1] > -Math.PI / 4)
                    return false;
            if (s.l - maxc > 1)
            {
                double ang = angle(s.cusps[maxc].pt, s.cusps[maxc + 1].pt, new Point(0, -1));
                if (ang > 37.5)
                    return false;
            }
            int botlobeind; maxy(0, s.pts.Length, s.pts, out botlobeind);
            for (int c = maxc + 1; c < s.cusps.Length - 1; c++)
                if (s.cusps[c].index < botlobeind && s.cusps[c].curvature > 0)
                    return false;
            bool left;
            double taildist = (s.dist - s.cusps[maxc].dist) / (s.bbox.Bottom - s.cusps[maxc].pt.Y);
            if (V2D.Straightness(s.s.GetPoints(), convertIndex(s.cusps[maxc].index, s.skipped), s.s.GetPoints().Length, out left) < 0.1 || (!left && taildist < 1.33))
                if (V2D.Dist(s.last, s.cusps[maxc].pt) / s.bbox.Height < 0.3)
                    return false;
            baseline = (s.bbox.Top + s.bbox.Bottom) / 2;
            if ((s.dist - s.distances[botlobeind]) / s.bbox.Height < 0.4 && (s.cusps[s.nl].index <= botlobeind || s.cusps[s.nl].curvature > 0))
                allograph = "q9";
            else allograph = "q";
            return true;
        }
        static public double angle(Point p1, Point p2, Point vec) { return angle(V2D.Sub(p1, p2), V2D.Normalize(vec)); }
        static public double angle(Point p1, Point p2, PointF vec) { return angle(V2D.Sub(p1, p2), vec); }
        static public double angle(Point v1, Point v2) { return angle(V2D.Normalize(v1), V2D.Normalize(v2)); }
        static public double angle(PointF v1, Point v2) { return angle(v1, V2D.Normalize(v2)); }
        static public double angle(Point v1, PointF v2) { return angle(V2D.Normalize(v1), v2); }
        static public double angle(PointF v1, PointF v2) { return Math.Acos(V2D.Dot(v1, v2)) * 180 / Math.PI; }
        bool match_straight_stem_base(CuspSet s, bool lenient, out int botCusp)
        {
            botCusp = 1;
            int topCusp = 0;
            if (s.cusps.Length < 3)
                return false;
            Point p1 = s.pts[0];
            Point p2 = s.cusps[1].pt;
            if (s.cusps[botCusp].top && s.cusps.Length > 2 && s.cusps[botCusp - 1].pt.Y > s.cusps[botCusp].pt.Y)
            {
                topCusp = 1;
                botCusp = 2;
                int dir = -1;
                if (s.cusps[1].pt.Y < s.cusps[0].pt.Y)
                { // && s.s.GetPoint(0).Y < s.cusps[0].pt.Y) {
                    if (s.s.GetPoint(0).Y < s.cusps[0].pt.Y)
                    {
                        p1 = s.s.GetPoint(0);
                        p2 = s.pts[0];
                    }
                    else
                        dir = 1;
                    botCusp = 0;
                }
                double leadRatio = V2D.Dist(p1, p2) / s.dist;
                double leadAng = Math.Abs(angle(p1, p2, new PointF(0, dir)));
                if (botCusp != 0 && leadAng > (leadRatio < .2 ? 45 : 25))
                    return false;
                if (dir == 1)
                    return true;
            }
            for (; botCusp < s.cusps.Length - 1; botCusp++)
            {
                if (s.cusps[botCusp + 1].curvature > 0.1)
                    break;
                int my = miny(s.cusps[botCusp].index, s.cusps[botCusp + 1].index, s.pts);
                if ((my - s.cusps[botCusp].pt.Y) / (float)s.bbox.Height < 0.2)
                    break;
            }
            if (botCusp > 0 && Math.Abs(s.cusps[botCusp].curvature) < 0.2)
                return false;
            int startind = -1;
            double ang = angle(s.cusps[botCusp].pt, p1, new PointF(0, 1));
            int angMax = (lenient ? 60 : 45);
            if (botCusp > 0 && s.cusps[botCusp].curvature > 0.3)
                angMax /= 2;
            if (ang < angMax && s.cusps[botCusp].index < 6)
                return true;
            if (botCusp > 0 && (ang > angMax || (ang > 32 && Math.Abs(s.avgCurve(botCusp - 1, botCusp)) > 0.08)))
                return false;
            double dratio = V2D.Straightness(startind == -1 ? s.s.GetPoints() : s.pts, startind == -1 ? 0 : startind,
                startind == -1 ?
                convertIndex(s.cusps[botCusp].index, s.skipped) : s.cusps[botCusp].index);
            if (ang < 10 && dratio < 0.13)
                return true;
            if (V2D.Straightness(s.pts, s.cusps[topCusp].index, s.cusps[botCusp].index) > 0.22)
                return false;
            //if ((Math.Abs(s.maxCurve(botCusp-1,botCusp)) > 0.04 && Math.Abs(s.avgCurve(botCusp-1,botCusp)) > 0.04 && dratio > (lenient ? .15 :.13)) ||
            //    (!lenient && dratio > .13))
            //    return false;
            return true;
        }
        bool match_db_base(CuspSet s, ref double minlobe, out int botCusp)
        {
            if (!match_straight_stem_base(s, false, out botCusp) || botCusp < 1 || !s.cusps[botCusp - 1].top)
                return false;
            if (V2D.Dist(s.cusps[botCusp - 1].pt, s.cusps[botCusp].pt) / s.bbox.Height < 0.30)
                return false;
            minlobe = miny(s.cusps[botCusp].index, s.pts.Length - 1, s.pts);
            if ((minlobe - s.pts[botCusp - 1].Y) / s.bbox.Height < .12)
                return false;
            return true;
        }

        bool match_d(CuspSet s, ref int xhgt)
        {
            double minlobe = 0;
            int botCusp;
            if (s.cusps[s.l].top)
                return false;
            if (!match_db_base(s, ref minlobe, out botCusp))
                return false;
            double lobehgt = (minlobe - s.pts[botCusp - 1].Y) / s.bbox.Height;
            if (lobehgt < .3)
                return false;
            if ((s.last.X - s.cusps[botCusp].pt.X + 0.0) / s.bbox.Height > lobehgt * 2 / 3)
            {
                double ang = angle(s.last, s.pts[s.cusps[s.l].index - 4], new PointF(1, 0));
                if (ang > 45 || Math.Sign(s.avgCurve(s.nl, s.l)) == 1)
                    return false;
            }
            if (s.cusps[botCusp].curvature < 0)
            {
                if (s.intersects.Length == 0)
                    return false;
                for (int i = 0; i < s.intersects.Length - 1; i++)
                    if (s.intersects[i] > s.cusps[botCusp].index)
                    {
                        int rightlobe = maxx(s.cusps[botCusp].index, s.intersects[i], s.pts);
                        int leftlobe = minx(s.intersects[i], s.pts.Length, s.pts);
                        if ((rightlobe - getPt(s.intersects[0], s.pts).X + 0.0) / (s.pts[botCusp].X - leftlobe) > 1)
                            return false;
                        break;
                    }
            }
            int minind, miyind, maxxind;
            double mi_x = minx(s.cusps[botCusp].index, s.cusps[s.l].index, s.pts, out minind);
            double ma_x = maxx(s.cusps[botCusp].index, s.cusps[s.l].index, s.pts, out maxxind);
            if (minind > s.cusps[s.l].index - 3)
                return false;
            int tailstart = -1;
            for (int i = minind; i < s.pts.Length - 1; i++)
                if (s.pts[i].X < s.cusps[botCusp].pt.X && s.pts[i + 1].X > s.cusps[botCusp].pt.X)
                {
                    tailstart = i;
                    break;
                }
            double mi_y = miny(s.cusps[botCusp].index, maxxind, s.pts, out miyind);
            if (tailstart > 0 && s.cusps[botCusp].pt.X - mi_x / ma_x - s.cusps[botCusp].pt.X > (V2D.Straightness(s.pts, tailstart, s.pts.Length) > 0.12 ? 1 : 1.4))
                return false;
            if (miyind == -1) miyind = maxxind;
            if (tailstart > 0 && (s.last.Y - s.cusps[botCusp].pt.Y) / (float)(s.pts[miyind].Y - s.cusps[botCusp].pt.Y) > .8 &&
                angle(s.last, s.pts[tailstart], new PointF(0, -1)) < 50)
                return false;
            for (int i = minind + 3; i < s.cusps[s.l].index - 3; i++)
                if (s.pts[i].X > s.pts[i + 1].X)
                    return false;
            if (Math.Sign(s.avgCurve(botCusp, s.l)) == 1 || Math.Abs(s.avgCurve(botCusp, s.l)) < 0.06)
                return false;
            for (int c = botCusp + 1; c < s.cusps.Length - 1; c++)
                if (Math.Sign(s.cusps[c].curvature) == 1 ||
                    Math.Abs(s.cusps[c].curvature) > 1.8)
                    return false;
            xhgt = (s.bbox.Top + s.bbox.Bottom) / 2;
            return true;
        }
        bool match_b(CuspSet s, ref int xhgt)
        {
            double minlobe = 0;
            if (s.intersects.Length > 1 && s.bbox.Width / (float)s.bbox.Height > 1.5 + (s.last.Y - s.bbox.Bottom + 0.0) / s.bbox.Height)
                return false;
            int botCusp;
            if (s.cusps[s.l].top)
                return false;
            if (!match_db_base(s, ref minlobe, out botCusp))
                return false;
            int rightlobe;
            double mi_x = minx(s.cusps[botCusp].index, s.cusps[s.l].index, s.pts);
            double ma_x = maxx(s.cusps[botCusp].index, s.cusps[s.l].index, s.pts, out rightlobe);
            if ((s.dist - s.cusps[botCusp].dist) / Math.Abs(s.cusps[s.l].pt.X - s.cusps[botCusp].pt.X + 0.0) < 1.2)
                return false;
            int toplobeind; double toplob = miny(s.cusps[botCusp].index, s.pts.Length, s.pts, out toplobeind);
            if ((toplob - s.bbox.Top + 0.0) / s.bbox.Height < 0.25 && -s.cusps[botCusp].curvature < s.curvatures[toplobeind])
                return false;
            if (V2D.Dist(s.cusps[botCusp].pt, s.last) / (s.dist - s.cusps[botCusp].dist) > 0.15 &&
                (s.last.X - s.cusps[botCusp].pt.X + 0.0) / (ma_x - mi_x) > 0.2)
            {
                bool ok = false, gotBot = false;
                for (int i = rightlobe; i < s.pts.Length && s.curvatures[i] > 0; i++)
                    if (s.angles[i - 1] <= 0 && s.angles[i] >= 0)
                        gotBot = true;
                    else if (gotBot && s.angles[i] > 1)
                        ok = true;
                if (!ok)
                {
                    if (s.cusps.Length > 3)
                    {
                        double ang2 = angle(s.last, s.pts[s.pts.Length - 5], V2D.Sub(s.cusps[botCusp].pt, s.pts[0]));
                        double angtail = angle(s.last, s.pts[s.cusps[s.l].index - 5], new Point(-2, -1));
                        if (ang2 < 25 || angtail > 100)
                            return false;
                    }
                    else
                    {
                        PointF dir = V2D.Normalize(V2D.Sub(s.cusps[s.l].pt, s.pts[s.cusps[s.l].index - 4]));
                        double ang = angle(dir, new PointF(-1, 0));
                        double ang2 = angle(s.cusps[s.l].pt, s.cusps[botCusp].pt, new Point(0, -1));
                        if (((ang2 > 45 && ang > 30) && dir.Y > 0) || Math.Sign(s.curvatures[s.cusps[s.l].index - 5]) == -1)
                            return false;
                    }
                    ok = true;
                }
            }
            if (s.avgCurveSeg(s.cusps[botCusp].index + 2, s.cusps[botCusp + 1].index - 2) < 0 && s.cusps[botCusp + 1].curvature > 1)
                return false;
            if ((s.cusps[botCusp].pt.X - mi_x) > (ma_x - s.cusps[botCusp].pt.X) * .75)
                return false;
            if (Math.Sign(s.avgCurve(botCusp, s.l)) == -1 || Math.Abs(s.avgCurve(botCusp, s.l)) < 0.06)
                return false;
            for (int c = botCusp + 1; c < s.cusps.Length - 1; c++)
                if (Math.Sign(s.cusps[c].curvature) == -1)
                    return false;
            xhgt = (s.bbox.Top + s.bbox.Bottom) / 2;
            return true;
        }
        bool match_y(CuspSet s, ref string allograph, ref int baseline)
        {
            int topcusp = 2;
            if (s.cusps.Length >= 4)
            {
                for (int i = 3; i < s.cusps.Length - 1; i++)
                    if (s.cusps[i].pt.Y < s.cusps[topcusp].pt.Y)
                        topcusp = i;
                if (s.cusps[topcusp].pt.Y > s.cusps[1].pt.Y)
                    topcusp = 1;
            }
            else
            {
                topcusp = 1;
            }
            if ((!s.cusps[0].top && (s.s.GetPoint(0).Y - s.bbox.Top + 0.0) / s.bbox.Height > 0.4) || s.cusps[topcusp].bot)
                return false;
            if (topcusp == s.cusps.Length - 1)
                return false;
            int maxlobeind;
            int maxlobe = maxy(topcusp > 2 ? s.cusps[1].index : 0, s.cusps[topcusp].index, s.pts, out maxlobeind);
            if (maxlobeind < 2 || (s.curvatures[maxlobeind] > 0 && (s.intersects.Length < 2 || s.intersects[0] > maxlobeind)))
                return false;
            bool left;
            int lobeind;
            double disty = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.s.GetPoint(0), s.cusps[topcusp].pt)), out left, out lobeind, 0, s.cusps[topcusp].index);
            if (s.cusps[s.l].bot && (disty / s.bbox.Height < 0.12 || (s.pts[lobeind].Y < s.cusps[topcusp].pt.Y && s.curvatures[lobeind] > 0)))
                return false;
            if ((s.bbox.Bottom - maxlobe + 0.0) / s.bbox.Height < 0.25)
                return false;
            PointF dir = V2D.Normalize(V2D.Sub(s.pts[0], s.cusps[topcusp].pt));
            if (dir.Y < 0)
                dir = new PointF(dir.X, -dir.Y);
            double ang = angle(dir, new PointF(0, 1));
            if (ang < 15 || dir.X > 0)
                return false;
            if (Math.Abs(s.cusps[topcusp].curvature) < 0.2)
                return false;
            if ((topcusp + 1 < s.cusps.Length - 1 && Math.Sign(s.cusps[topcusp + 1].curvature) == -1))
                return false;
            if (miny(s.cusps[topcusp].index + 5, s.pts.Length - 3, s.pts) < s.cusps[topcusp].pt.Y)
                return false;
            if (topcusp - 1 > 0 && Math.Abs(s.cusps[topcusp - 1].curvature) > 0.5 &&
                s.cusps[topcusp - 1].dist / s.dist < 0.2 &&
                s.intersects.Length > 0 && s.intersects[s.intersects.Length - 1] > 2 * s.pts.Length / 3 &&
                s.angles[(s.intersects[s.intersects.Length - 1] + s.pts.Length - 1) / 2] < 0 &&
                s.angles[(s.intersects[s.intersects.Length - 1] + s.pts.Length - 1) / 2] > -2.5)
                return false;
            int tailloop = 0;
            double a2 = angle(s.cusps[topcusp].pt, s.last, new PointF(0, -1));
            for (int i = 0; i < s.intersects.Length; i++)
                if (s.intersects[i] > s.cusps[topcusp].index)
                    tailloop++;
            int botind; maxy(s.cusps[topcusp].index, s.pts.Length, s.pts, out botind);
            if (s.intersects.Length > 0 &&
                (s.bbox.Bottom - s.last.Y + 0.0) / s.bbox.Height < 0.1 &&
                V2D.Straightness(s.pts, s.cusps[topcusp].index, s.pts.Length) < 0.15 && s.intersects.Length == 2 &&
                s.curvatures[(s.intersects[0] + s.intersects[1]) / 2] < 0)
            { // stem is very straight
                Point inter = getPt(s.intersects[0], s.pts);
                int miyindex, miy = miny(s.intersects[0], s.intersects[1], s.pts, out miyindex);
                double str = V2D.Straightness(s.pts, V2D.Sub(s.pts[s.intersects[1]], s.pts[s.intersects[1] + 1]), s.intersects[0], s.intersects[1],
                    V2D.Dist(s.pts[s.intersects[0]], s.pts[miyindex]), out left);
                if (str > 0.3)
                {
                    allograph = "varphi";
                }
            }
            double loopstr = V2D.Straightness(s.pts, 0, s.cusps[topcusp].index);
            if (loopstr < 0.4 &&
                angle(s.pts[0], s.cusps[topcusp].pt, V2D.Sub(s.pts[botind], s.last)) < 20)
                return false;
            if (loopstr < 0.15 && s.bbox.Height / (float)s.bbox.Width < 1)
                return false;
            if (allograph == "")
            {
                double chanceof4 = Math.Max(0.08, (s.cusps[topcusp].pt.Y - s.pts[0].Y + 0.0) / (maxlobe - s.bbox.Top) * .25);
                double downstr = V2D.Straightness(s.pts, 0, s.cusps[1].index);
                double upstr = V2D.Straightness(s.pts, s.cusps[topcusp - 1].index, s.cusps[topcusp].index);
                double crossstr = V2D.Straightness(s.pts, s.cusps[1].index, s.cusps[2].index);
                double maxstemang = 15;
                if (angle(s.cusps[2].pt, s.cusps[1].pt, new PointF(1, 0)) < 20 && (s.cusps[2].dist - s.cusps[1].dist) / s.bbox.Width > 0.2)
                    maxstemang = 30;
                if (s.intersects.Length > 0 && s.intersects[0] > s.pts.Length / 10 && s.intersects[0] < s.cusps[topcusp].index)
                {
                    chanceof4 = 0.2;
                    maxstemang = 30;
                }
                if (a2 < maxstemang && (s.dist - s.cusps[topcusp].dist) / V2D.Dist(s.cusps[topcusp].pt, s.pts[botind]) < 1.125 &&
                    V2D.Straightness(s.pts, s.cusps[topcusp].index, s.pts.Length) < 0.10 &&
                    (maxy(0, s.cusps[topcusp].index, s.pts) - s.bbox.Top + 0.0) / s.bbox.Height > 0.25 &&
                    (downstr < chanceof4 || upstr < chanceof4 || crossstr < chanceof4))
                    allograph = "4y"; // stem is straight, vertical, longer than top lobe, and some part of upper lobe is straight
                else if ((s.bbox.Bottom - s.last.Y + 0.0) / s.bbox.Height < 0.1 && V2D.Straightness(s.pts, s.cusps[topcusp].index, s.pts.Length) < 0.1)
                {
                    if (s.intersects.Length > 0)
                    { // stem is very straight
                        Point inter = getPt(s.intersects[s.intersects.Length - 2], s.pts);
                        int mix = minx(0, s.intersects[s.intersects.Length - 2], s.pts);
                        int max = maxx(s.intersects[s.intersects.Length - 2], s.intersects[s.intersects.Length - 1], s.pts);
                        if ((max - inter.X + 0.0) / (max - mix) > 0.1)
                            allograph = "varphi";
                        else allograph = "y44";
                    }
                    allograph = "y44";
                }
                else if (s.curvatures[(maxlobeind + s.cusps[topcusp].index) / 2] > 0 && s.cusps[1].curvature < -1 &&
                   s.intersects.Length > 0 && (s.angles[s.pts.Length - 4] < 0 || s.angles[s.pts.Length - 4] > 2.5))
                    allograph = "2y";
                else allograph = "y";
            }
            return true;
        }
        public bool match_delta(CuspSet s)
        {
            if (s.pts.Length < 5)
                return false;
            //get the starting angle for delta, check the angle
            double startAng = angle(s.pts[0], s.pts[4], new PointF(0, -1));

            if (startAng < 25)
                return false;

            //if starting point is above the end point, not a delta
            int startPointY = s.pts[0].Y;
            int topPointY = s.pts[s.pts.Length - 1].Y;

            if (topPointY > startPointY)
                return false;

            //check if the first cusp is to the top
            if (!s.cusps[s.l].top)
                return false;

            //check for the proportion of delta's hook length to the bbox
            int hookLength = startPointY - topPointY;

            if (hookLength < s.bbox.Height * .3)
                return false;

            //check finishing part's straightness
            Point endPoint = s.pts[s.pts.Length - 1];
            int topIndex;
            int topY = miny(0, s.pts.Length, s.pts, out topIndex);
            double straightness;
            if (endPoint.Y > topY)
            {
                straightness = V2D.Straightness(s.pts, topIndex, s.pts.Length - 1);
            }
            else
            {
                return false;
            }

            if (straightness > 0.2)
                return false;

            //check curvature between the rightMost point and the bottom point
            int bottomIndex;
            int bottomY = maxy(0, s.pts.Length, s.pts, out bottomIndex);

            int rightMostIndex;
            int rightMostBetween = maxx(bottomIndex, topIndex, s.pts, out rightMostIndex);

            double curvature = s.avgCurveSeg(rightMostIndex, topIndex);

            if (curvature < 0)
                return false;

            //passes all the tests, this is letter delta
            return true;
        }


        bool match_partial(CuspSet s)
        {
            if (s.cusps.Length < 3 || s.cusps[0].left)
                return false;
            int botind, rightind;
            int boty = maxy(0, s.pts.Length, s.pts, out botind);
            int rmx = maxx(botind, s.pts.Length, s.pts, out rightind);
            if (rightind == -1)
                return false;
            Point rightlobe = s.pts[rightind];

            double endAng = angle(s.pts[0], s.pts[4], new PointF(0, -1));
            if (endAng < 25)
                return false;
            Point stemvec = V2D.Sub(s.last, rightlobe);

            if (stemvec.X == 0 && stemvec.Y == 0)
                return false;
            double ang = angle(s.last, rightlobe, new Point(-1, -1));

            if (ang > 45)
                return false;
            int toplobeind;
            int loopmax = miny(0, botind, s.pts, out toplobeind);
            if ((loopmax - s.last.Y + 0.0) / s.bbox.Height < .2)
                return false;

            if ((boty - loopmax) < .05 * s.bbox.Height)
                return false;
            int leftind;
            int leftlobe = minx(0, botind, s.pts, out leftind);


            if (toplobeind > leftind)
                return false;
            int loopendind;
            int loopclosest = maxx(0, leftind, s.pts, out loopendind);

            if (leftind - loopendind < 3)
                return false;

            if ((s.intersects.Length < 2 || s.intersects[1] < loopendind) && (leftind <= toplobeind || loopendind > toplobeind))
                return false;
            for (int c = 1; c < s.cusps.Length - 1; c++)
                if (Math.Sign(s.cusps[c].curvature) != -1 || Math.Abs(s.cusps[c].curvature) > 1.5)
                    return false;
            return true;
        }
        bool match_partialTop(CuspSet s)
        {
            if (s.cusps.Length < 3)
                return false;
            int botind, rightind;
            int boty = maxy(0, s.pts.Length, s.pts, out botind);
            int rmx = maxx(0, botind, s.pts, out rightind);
            if (rightind == -1)
                return false;
            Point rightlobe = s.pts[rightind];
            double endAng = angle(s.last, s.pts[s.pts.Length - 3], new PointF(0, -1));
            if (endAng < 25)
                return false;
            Point stemvec = V2D.Sub(s.pts[0], rightlobe);
            if (stemvec.X == 0 && stemvec.Y == 0)
                return false;
            double ang = angle(s.pts[0], rightlobe, new Point(-1, -1));
            if (ang > 45)
                return false;
            if (s.intersects.Length > 0)
            {
                double extends = (s.dist - s.distances[s.intersects[s.intersects.Length - 1]]) / s.bbox.Height;
                if (extends > 0.35)
                    return false;
            }
            int toplobeind;
            int loopmax = miny(botind, s.pts.Length, s.pts, out toplobeind);
            if ((loopmax - s.pts[0].Y + 0.0) / s.bbox.Height < .2 ||
                (s.pts[botind].Y - loopmax + 0.0) / s.bbox.Height < .2)
                return false;
            if ((boty - loopmax) < .05 * s.bbox.Height)
                return false;
            int leftind;
            int leftlobe = minx(botind, s.pts.Length, s.pts, out leftind);
            if (s.curvatures[botind] < 0)
                return false;
            if (toplobeind < leftind)
                return false;
            int loopendind;
            int loopclosest = maxx(leftind, s.pts.Length, s.pts, out loopendind);
            if (loopendind - leftind < 3)
                return false;
            if (s.intersects.Length >= 2)
            {
                double vang = angle(s.last, s.pts[s.intersects[s.intersects.Length - 1]], new PointF(0, -1));
                if ((s.dist - s.distances[s.intersects[s.intersects.Length - 1]]) / s.bbox.Height > .1 && vang < 60)
                    return false;
            }
            if ((s.intersects.Length < 2 || s.intersects[1] > loopendind) && (leftind >= toplobeind || loopendind < toplobeind))
                return false;
            if ((s.last.X - s.bbox.Left + 0.0) / s.bbox.Width < 0.2)
                return false;
            for (int c = 1; c < s.cusps.Length - 1; c++)
                if (Math.Sign(s.cusps[c].curvature) != 1 || Math.Abs(s.cusps[c].curvature) > 1.5)
                    return false;
            return true;
        }
        public bool match_6(CuspSet s)
        {
            if (s.cusps.Length < 3 || !s.cusps[0].top)
                return false;
            int botind, leftind;
            int boty = maxy(0, Math.Min(s.intersects.Length > 2 && s.intersects[1] > s.pts.Length / 8 ? s.intersects[1] : s.pts.Length - 1, 3 * s.pts.Length / 4), s.pts, out botind);
            int lmx = minx(0, botind, s.pts, out leftind);
            if (leftind == -1)
                return false;
            Point leftlobe = s.pts[leftind];
            double endAng = angle(s.cusps[s.l].pt, s.pts[s.cusps[s.l].index - 3], new PointF(0, -1));
            if (endAng < 25)
                return false;

            //added by abrindam to stop 6 from grabbing loopy E's
            //intent is to reject any 6's that end with the tail
            //pointing right

            double endAngV2 = s.angles[s.angles.Length - 1] * (180 / Math.PI);

            if (Math.Abs(endAngV2) > 90)
            {
                //return false;
            }




            Point stemvec = V2D.Sub(s.pts[0], leftlobe);
            if (stemvec.X == 0 && stemvec.Y == 0)
                return false;
            double ang = angle(s.pts[0], leftlobe, new Point(1, -1));
            if (ang > 30 && V2D.Straightness(s.pts, 0, botind) < 0.15)
                return false;
            int toplobeind;
            int loopmax = miny(botind, s.pts.Length - 1, s.pts, out toplobeind);
            if ((loopmax - s.pts[0].Y) < .25 * s.bbox.Height)
                return false;
            if ((boty - loopmax) < .05 * s.bbox.Height)
                return false;
            if (s.cusps[s.l].top)
                return false;
            int rightind;
            int rightlobe = maxx(botind, s.pts.Length - 1, s.pts, out rightind);
            if (toplobeind < rightind)
                return false;
            int loopendind;
            int loopclosest = minx(rightind, s.pts.Length - 1, s.pts, out loopendind);
            if (loopendind - rightind < 3)
                return false;
            if ((s.intersects.Length < 2 || s.intersects[1] > loopendind) && (rightind >= toplobeind || loopendind < toplobeind))
                return false;
            for (int c = 1; c < s.cusps.Length - 1; c++)
                if (Math.Sign(s.cusps[c].curvature) != -1 || Math.Abs(s.cusps[c].curvature) > 2)
                    return false;
            return true;
        }
        bool match_d2(CuspSet s)
        {
            if (s.cusps.Length < 3 || !s.cusps[0].top || s.bbox.Width / (s.bbox.Height + 0.0) > 0.9)
                return false;
            if (s.cusps[1].top)
                return false;
            int botind; maxy(0, s.pts.Length, s.pts, out botind);
            if (V2D.Straightness(s.pts, s.pts[0], V2D.Sub(s.pts[botind], s.pts[0]), 0, s.pts.Length, V2D.Dist(s.pts[botind], s.pts[0])) < 0.12)
                return false;
            int rightind;
            int ymx = maxy(0, Math.Min(s.intersects.Length == 2 ? s.intersects[1] : s.pts.Length - 1, 3 * s.pts.Length / 4), s.pts, out botind);
            int rmx = maxx(0, botind, s.pts, out rightind);
            if (rightind == -1)
                return false;
            if (V2D.Straightness(s.pts, 0, rightind < s.pts.Length * .1 ? botind : rightind) < .2)
            {
                double ang = angle(s.pts[0], s.pts[rightind < s.pts.Length * .1 ? botind : rightind], new PointF(0, -1));
                if (ang > 20)
                    return false;
                if (V2D.Straightness(s.pts, 0, botind) > 0.3)
                    return false;
            }
            else
                return false;
            int rightlobeind2, leftlobeind2;
            int leftlobe = minx(botind, s.pts.Length - 1, s.pts, out leftlobeind2);
            int rightlobe2 = maxx(leftlobeind2, s.pts.Length - 1, s.pts, out rightlobeind2);
            if (rightlobe2 - rmx > rmx - leftlobe || (rightlobe2 - leftlobe + 0.0) / s.bbox.Width < 0.2)
                return false;
            int loopmax = miny(botind, s.pts.Length - 1, s.pts);
            if ((loopmax - s.pts[0].Y) < .25 * s.bbox.Height)
                return false;
            if (ymx - loopmax < .15 * s.bbox.Height)
                return false;
            if (s.cusps.Length > 3 && (s.cusps[s.nl].pt.X - s.cusps[s.l].pt.X) > 0)
                return false;
            for (int c = 1; c < s.cusps.Length - 1; c++)
                if (Math.Sign(s.cusps[c].curvature) != 1 && (s.cusps[c + 1].dist - s.cusps[c - 1].dist) / V2D.Dist(s.cusps[c - 1].pt, s.cusps[c + 1].pt) > 1.1)
                    return false;
            return true;
        }
        bool match_b6(CuspSet s)
        {
            if (s.cusps.Length < 3 || !s.cusps[0].top)
                return false;
            int botind, leftind;
            int boty = maxy(0, Math.Min(s.intersects.Length > 1 ? s.intersects[s.intersects.Length - 1] : s.pts.Length - 1, 3 * s.pts.Length / 4), s.pts, out botind);
            bool left;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[0], s.pts[botind])), out left, out leftind, 0, botind);
            if (leftind == -1)
                return false;
            if (leftind < 0.1 * s.pts.Length)
                leftind = botind;
            Point leftlobe = s.pts[leftind];
            double ang = angle(s.pts[0], leftlobe, new PointF(0, -1));
            if (ang > 30)
                return false;
            int toplobeind, loopleftind;
            int loopmax = miny(botind, s.pts.Length - 1, s.pts, out toplobeind);
            int loopleft = minx(toplobeind, s.pts.Length, s.pts, out loopleftind);
            double endAng = angle(s.pts[loopleftind], s.pts[toplobeind], new PointF(0, 1));
            if ((s.pts[loopleftind].X - s.pts[leftind].X + 0.0) / s.bbox.Width > Math.Max(3 * InkPixel / s.bbox.Width, 0.15) && (s.last.Y - s.pts[toplobeind].Y + 0.0) / s.bbox.Height > 0.15 &&
                endAng < 60)
                return false;
            double tailAng = angle(s.last, s.pts[loopleftind], new PointF(1, 0));
            double tailLength = (s.last.X - s.pts[loopleftind].X + 0.0) / s.bbox.Width;
            if (tailAng < 75 && tailLength > .2 && V2D.Straightness(s.pts, 0, botind) > 0.09)
                return false;
            if ((loopmax - s.pts[0].Y + 0.0) / s.bbox.Height < .25)
                return false;
            if ((boty - loopmax) < .05 * s.bbox.Height)
                return false;
            if (s.cusps[s.l].top)
                return false;
            int rightind;
            int rightlobe = maxx(botind, s.pts.Length, s.pts, out rightind);
            double stemStr = V2D.Straightness(s.pts, 0, leftind, out left);
            if (stemStr > .14 / (2 * ang / 15f) && left)
                return false;
            else if ((s.last.X - s.pts[leftind].X + 0.0) / s.bbox.Width > 0.15 && (ang < 10 || s.pts[0].X > leftlobe.X))
            {
                if (s.intersects.Length > 0)
                {
                    if (s.angles[s.intersects[0]] < -2)
                        return false;
                }
                else
                {
                    double closest = double.MaxValue;
                    int closeInd = -1;
                    for (int i = leftind; i < rightind; i++)
                        if (V2D.Dist(s.last, s.pts[i]) < closest)
                        {
                            closest = V2D.Dist(s.last, s.pts[i]);
                            closeInd = i;
                        }
                    if (closest / s.bbox.Width < 0.25 && Math.Abs(s.angles[closeInd]) > 2)
                        return false;
                }
            }
            int loopendind;
            int loopclosest = minx(rightind, s.pts.Length, s.pts, out loopendind);
            if ((s.intersects.Length < 2 || s.intersects[1] > loopendind) && (rightind >= toplobeind || loopendind < toplobeind))
                return false;
            for (int c = 1; c < s.cusps.Length - 1; c++)
                if ((Math.Sign(s.cusps[c].curvature) != -1 && Math.Abs(s.cusps[c].curvature) < 2.6) ||
                    (Math.Sign(s.cusps[c].curvature) == -1 && Math.Abs(s.cusps[c].curvature) > 2.6))
                    return false;
            return true;
        }
        bool match_hnr_base(CuspSet s, bool lenient, out int botCusp)
        {
            if (!match_straight_stem_base(s, lenient, out botCusp))
                return false;
            Point p1 = botCusp > 0 ? s.cusps[botCusp - 1].pt : s.s.GetPoint(0);
            if (p1.Y > s.cusps[botCusp].pt.Y)
                return false;
            //if (V2D.Dist(p1, s.cusps[botCusp].pt)/(s.bbox.Bottom-s.cusps[botCusp].pt.Y) < 0.5)
            //    return false;
            if ((s.bbox.Bottom - s.cusps[botCusp].pt.Y + 0.0 - 2 * InkPixel) / s.bbox.Height > 0.375)
                return false;
            if (V2D.Dot(V2D.Sub(s.last, s.cusps[botCusp].pt), new PointF(1, 0)) < 0)
                return false;
            if (V2D.Dist(s.cusps[botCusp].pt, s.cusps[s.l].pt) / (s.dist - s.cusps[botCusp].dist) < 0.15)
                return false;
            //if (Math.Sign(s.avgCurve(botCusp, s.l)) != 1)
            //    return false;
            if (s.cusps.Length > botCusp + 2 && Math.Sign(s.cusps[botCusp + 1].curvature) == -1)
                return false;
            return true;
        }
        bool match_h(CuspSet s, ref int xhgt, ref string allograph)
        {
            allograph = "";
            string allographwillbe = "h";
            int botCusp;
            if (!match_hnr_base(s, false, out botCusp) || botCusp < 1 || !s.cusps[botCusp - 1].top || s.cusps.Length > 5)
                return false;
            int rightstem = maxx(s.cusps[botCusp].index / 2, s.cusps[botCusp].index, s.pts);
            if ((rightstem - s.bbox.Left + 0.0) / s.bbox.Width > 0.5)
                return false;
            if (angle(s.pts[botCusp - 1], s.cusps[botCusp].pt, V2D.Sub(s.pts[s.cusps[botCusp].index + 3], s.cusps[botCusp].pt)) > 30 &&
                s.cusps[botCusp].curvature > 0)
                return false;
            int topind;
            double minlobe = miny(s.cusps[botCusp].index, s.cusps[s.l].index, s.pts, out topind);
            if (s.curvatures[topind] < 0) //|| s.curvatures[topind] > Math.Abs(s.cusps[1].curvature))
                return false;
            if (s.cusps.Length > 4)
            {
                //if (Math.Sign(s.cusps[botCusp+2].curvature) == 1)
                //    return false;
                if (s.cusps[botCusp + 2].index > topind + 1 &&
                    miny(s.cusps[botCusp + 2].index, s.cusps[s.l].index, s.pts) < miny(s.cusps[botCusp + 1].index, s.cusps[botCusp + 2].index, s.pts))
                    return false;
            }
            if (s.curvatures[topind] > 1.5 && s.cusps[botCusp].curvature > -1)
                allographwillbe = "hu";
            double angt = angle(s.cusps[botCusp - 1].pt, s.cusps[botCusp].pt, new PointF(0, -1));
            if (angt > 37.5)
                return false;
            double ang = angle(s.last, s.cusps[botCusp].pt, new PointF(1, 0));
            double maxAng = 45;
            double thresh = Math.Max(10, maxAng / 2 / (V2D.Dist(s.last, s.cusps[botCusp].pt) / s.bbox.Height));
            if (ang > thresh)
                return false;
            double ang2 = angle(s.last, s.pts[topind], new Point(1, 2));
            if (ang2 > 45)
                return false;
            if (Math.Abs(s.avgCurveMag(botCusp, s.l)) < 0.05)
                return false;
            if (s.cusps.Length - botCusp == 4 && Math.Sign(s.cusps[s.nl].curvature) == -1 && (s.dist - s.cusps[s.nl].dist) / s.dist > 0.2)
                return false;
            double angstem = angle(s.pts[0], s.pts[topind], V2D.Sub(s.pts[0], s.pts[s.cusps[botCusp].index / 3]));
            double loberatio = (s.bbox.Bottom - minlobe + 0.0 + 4 * InkPixel) / (s.bbox.Height + 4 * InkPixel);
            if (loberatio > .7 / Math.Max(1, 1.215 * s.bbox.Width / s.bbox.Height) ||
                loberatio < .15 * Math.Max(1, 2.0 * s.bbox.Width / s.bbox.Height) ||
                (loberatio > .64 && angstem < 35))
                return false;
            int topInd;
            if (s.intersects.Length > 1)
            {
                PointF stemDir = V2D.Normalize(V2D.Sub(s.pts[s.intersects[0]], s.cusps[botCusp].pt));
                topInd = s.intersects[1];
                bool left;
                int index;
                double maxdim = V2D.MaxDist(s.pts, stemDir, out left, out index, s.intersects[1], s.pts.Length);
                if (maxdim / s.bbox.Width < 0.5)
                    return false;
                double mindim = V2D.MaxDist(s.pts, stemDir, out left, out index, s.cusps[botCusp].index, s.intersects[1]);
                if (mindim / s.bbox.Width > 0.2 && maxdim / s.bbox.Width / (mindim / s.bbox.Width) < 2)
                    return false;
            }
            xhgt = (int)minlobe;
            allograph = allographwillbe;
            return true;
        }
        public bool match_n(CuspSet s, ref string allograph)
        {
            if ((s.bbox.Width + 0.0) / s.bbox.Height > 3)
                return false;
            int botCusp;
            if (!match_hnr_base(s, true, out botCusp) || s.cusps.Length > 6 || s.cusps[s.l].top)
                return false;
            if (botCusp == 0 && (Math.Abs(s.cusps[1].curvature) > 1 || (s.cusps.Length > 3 && Math.Abs(s.cusps[1].curvature + s.cusps[2].curvature) > .75)))
                return false;
            if (s.cusps[0].right)
                return false;
            int minlobeind; miny(s.cusps[botCusp].index, s.pts.Length, s.pts, out minlobeind);
            /*
            for (int i = botCusp; i < s.cusps.Length; i++)
                if (minlobeind == -1 || s.cusps[i].pt.Y < s.pts[minlobeind].Y)
                    minlobeind = s.cusps[i].index;
                else if (s.pts[minlobeind].Y < s.cusps[i].pt.Y)
                    break;
            */
            int minsqueezeind; double minsqueeze = minx(s.cusps[botCusp + 1].index, s.pts.Length, s.pts, out minsqueezeind);
            if (minsqueezeind != -1)
            {
                int maxlobebulge; double maxbulge = maxx(s.cusps[botCusp].index, minsqueezeind, s.pts, out maxlobebulge);
                if (maxlobebulge != -1 && (s.pts[maxlobebulge].X - minsqueeze) / s.bbox.Width > 0.25 && (s.last.X - minsqueeze + 0.0) / s.bbox.Width > 0.25)
                    return false; // squished in the middle like a 'k' or 'R'
            }
            double minlobe = s.pts[minlobeind].Y;// miny(s.cusps[botCusp].index, s.cusps[s.cusps.Length-1].index, s.pts, out minlobeind);
            double loberatio = (s.bbox.Bottom - minlobe + 0.0) / s.bbox.Height;
            if (loberatio < .6 / Math.Max(1, 1.25 * s.bbox.Width / s.bbox.Height))
                return false;
            if (botCusp > 0)
            {
                if (Math.Abs(s.cusps[botCusp].curvature) < 0.4)
                    return false;
                if (s.cusps.Length > 3 && Math.Abs(s.cusps[botCusp].curvature) * 1.2 < Math.Abs(s.cusps[botCusp + 1].curvature) &&
                     Math.Abs(s.cusps[botCusp].curvature) < 1 && (s.cusps[s.nl].pt.X - s.cusps[0].pt.X) > .24 * s.bbox.Width) // from match_u
                    return false;
            }
            int bottailind, bottail = maxy(minlobeind, s.pts.Length, s.pts, out bottailind);
            if ((s.last.X - s.cusps[botCusp].pt.X + 0.0) / (maxx(s.cusps[botCusp].index, s.pts.Length, s.pts) - Math.Max(s.pts[0].X, s.cusps[botCusp].pt.X) + 0.0) < 0.8 &&
                Math.Abs(s.angles[s.angles.Length - 1]) < Math.PI / 2)
                return false;
            if ((bottail - s.bbox.Top) / (float)s.bbox.Height < 0.65)
                return false;
            int endcusp = -1;
            for (int i = botCusp + 1; i < s.cusps.Length; i++)
                if (s.cusps[i].index < minlobeind && (s.cusps[i].curvature < 0 || (s.intersects.Length > 0 && s.intersects[s.intersects.Length - 1] > s.cusps[i].index && s.intersects[s.intersects.Length - 2] < s.cusps[i].index)))
                    return false;
                else if (s.cusps[i].index > minlobeind)
                {
                    if (!s.cusps[i].top && (endcusp == -1 || s.cusps[endcusp].pt.Y < s.cusps[i].pt.Y))
                    {
                        if (endcusp != -1 && (s.cusps[endcusp].curvature < 0 ||
                            (s.intersects.Length > 0 && s.intersects[s.intersects.Length - 1] > s.cusps[endcusp].index && s.intersects[s.intersects.Length - 2] < s.cusps[endcusp].index)))
                            return false;
                        endcusp = i;
                    }
                    else if (endcusp != -1 && s.cusps[i].pt.Y < s.cusps[endcusp].pt.Y)
                        break;
                    else if (s.cusps[i].curvature < 0 && !s.cusps[i].top && bottailind - s.cusps[i].index > s.pts.Length / 10)
                        return false;
                }
            if ((s.dist - s.distances[bottailind]) / s.bbox.Height > 0.15 && V2D.Straightness(s.pts, bottailind, s.pts.Length) > 0.25)
                return false;
            for (int i = endcusp + 1; i < s.cusps.Length - 1; i++)
                if (s.cusps[i].curvature > 0 || s.cusps[i].pt.Y < s.pts[minlobeind].Y)
                    return false;

            if ((s.bbox.Bottom - s.cusps[botCusp].pt.Y + 0.0 - 2 * InkPixel) / s.bbox.Height > 0.4 * (s.bbox.Height / (float)s.bbox.Width))
                return false;
            bool left;
            if (botCusp > 0 && s.cusps[botCusp + 1].index < s.pts.Length - 5 &&
                angle(s.pts[0], s.cusps[botCusp].pt, V2D.Sub(s.pts[s.cusps[botCusp + 1].index + 5], s.cusps[botCusp].pt)) > 25 &&
                (V2D.Straightness(s.pts, s.cusps[botCusp].index, s.cusps[botCusp + 1].index, out left) < 0.05 || !left))
                allograph = "un";
            else allograph = "n";
            return true;
        }
        public bool match_r(CuspSet s)
        {
            if ((s.bbox.Width + 0.0) / s.bbox.Height < 0.33)
                return false;
            int botCusp;
            if (s.cusps[0].bot && V2D.Straightness(s.pts, 0, s.cusps[1].index) < 0.18 && angle(s.cusps[1].pt, s.pts[0], new PointF(0, -1)) < 40)
                botCusp = 0;
            else if (!match_hnr_base(s, true, out botCusp))
                return false;
            if (s.intersects.Length == 2)
            {
                double tailratio = (s.distances[s.intersects[1]] - s.distances[s.intersects[0]]) / s.bbox.Height / 2;
                int botind;
                maxy(s.intersects[0], s.intersects[1], s.pts, out botind);
                if (botind < 0) return false;
                double stemang = angle(s.pts[0], s.pts[botind], new PointF(0, -1));
                double endang = angle(s.last, s.pts[botind], new PointF(0, -1));
                if (botind != -1 && (endang / stemang < 3 || s.angles[s.angles.Length - 2] < 2.6))
                {
                    double inS = V2D.Straightness(s.pts, s.intersects[0], botind);
                    double ouS = V2D.Straightness(s.pts, botind, s.intersects[1]);
                    if (tailratio > .25 && inS + ouS > .20)
                        if ((s.last.X - s.pts[s.intersects[1]].X + 0.0) / (s.pts[s.intersects[1]].Y - s.last.Y) < 1.5)
                            return false;
                    double rAng = angle(s.last, s.pts[botind], new PointF(0, -1));
                    if (rAng < 25 && Math.Abs(s.angles[s.angles.Length - 1]) < 2.6)
                        return false;
                }
            }
            if (V2D.Straightness(s.pts, s.cusps[botCusp + 1].index, s.pts.Length) > 0.22)
                for (int c = botCusp + 1; c < s.cusps.Length - 1; c++)
                    if (s.cusps[c].curvature < 0 && (s.intersects.Length < 2 || !(s.intersects[0] < s.cusps[c].index && s.intersects[1] > s.cusps[c].index)) &&
                        (botCusp > 0 || (s.bbox.Bottom - s.cusps[c].pt.Y + 0.0) / s.bbox.Height > 0.4 || (s.cusps[c].pt.Y - s.last.Y + 0.0) / s.bbox.Height > .5))
                        return false;
            if ((s.last.X - s.cusps[botCusp].pt.X + 0.0) / s.bbox.Height > 2.4)
                return false;
            if (botCusp > 0 && Math.Abs(s.cusps[botCusp].curvature) < 1.2 && s.cusps[botCusp].dist / s.bbox.Height < 0.2)
                return false;
            if ((s.bbox.Right - s.last.X + 0.0) / s.bbox.Width > .15)
                return false;
            if (botCusp > 0 && V2D.Dist(s.cusps[botCusp - 1].pt, s.last) / s.bbox.Width < 0.5)
                return false;
            double stemStr = V2D.Straightness(s.pts, s.cusps[botCusp + 1].index, s.pts.Length);
            double radStr = V2D.Straightness(s.pts, s.cusps[botCusp].index, s.cusps[botCusp + 1].index);
            if ((s.dist - s.cusps[botCusp + 1].dist) / s.bbox.Width > 1.25 &&
                 ((stemStr < 0.05 && radStr < 0.07) || (stemStr + radStr) < 0.13))
                return false;
            int minlobeind; double minlobe = miny(s.cusps[botCusp].index, s.cusps[s.l].index, s.pts, out minlobeind);
            bool left; int rightlobeind;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.pts[minlobeind])), out left, out rightlobeind, minlobeind, s.pts.Length);
            foreach (CuspRec c in s.cusps)
                if (c.index > minlobeind)
                {
                    if (c.index < rightlobeind)
                        rightlobeind = c.index;
                    break;
                }
                else if (c.index > s.cusps[botCusp + 1].index && c.bot)
                    return false;
            if (rightlobeind > minlobeind + 2 && angle(s.pts[rightlobeind], s.pts[minlobeind], new PointF(1, 0)) > 60)
                return false;
            int maxxind, rightlobe = maxx(minlobeind, s.pts.Length, s.pts, out maxxind);
            if (maxxind < s.pts.Length - 5)
            {
                int botloopind; maxy(maxxind, s.pts.Length, s.pts, out botloopind);
                if ((s.dist - s.distances[maxxind]) / V2D.Dist(s.last, s.pts[maxxind]) > 1.5 &&
                    (botloopind > s.pts.Length - 4 ||
                     s.curvatures[botloopind] > 0 || (s.distances[botloopind] - s.distances[maxxind]) / V2D.Dist(s.pts[botloopind], s.pts[maxxind]) > 1.5 ||
                     V2D.Straightness(s.pts, botloopind, s.pts.Length) > 0.2))
                    return false;
            }
            double tdist = V2D.Dist(new Point(s.bbox.Right, s.bbox.Bottom), s.pts[minlobeind]) * 2;
            if (s.dist - s.distances[minlobeind] > tdist)
                return false;
            if (botCusp > 0 && (minlobe - s.cusps[botCusp - 1].pt.Y) / s.bbox.Height > .45)
                return false;
            if (s.cusps.Length > botCusp + 2 && s.cusps[botCusp + 1].curvature < 0 && (s.intersects.Length == 0 || s.intersects[0] > s.cusps[botCusp + 1].index))
                return false;
            if (botCusp < s.nl && botCusp > 0 && Math.Abs(s.cusps[botCusp].curvature) < Math.Abs(s.cusps[botCusp + 1].curvature) && s.cusps[s.l].top)
                return false;
            double ang = botCusp > 0 ? angle(s.cusps[botCusp].pt, s.cusps[botCusp - 1].pt, new PointF(0, 1)) : 0;
            double curvy = (s.dist - s.cusps[botCusp].dist) / V2D.Dist(s.last, s.cusps[botCusp].pt) - 1;
            if (curvy < 0.05 || (curvy < 0.075 && ((s.angles[s.angles.Length - 1] > 0 && s.angles[s.angles.Length - 1] < 2.50) || ang > 30)))
                return false;
            if ((s.bbox.Bottom - s.last.Y + 0.0) / s.bbox.Height < 0.35)
                return false;
            return true;
        }
        bool closedLoop(CuspSet s, out double opening)
        {
            opening = double.MaxValue;
            int nearIndex, nearX = s.curvatures[s.curvatures.Length / 2] < 0 ? maxx(0, s.pts.Length / 4, s.pts, out nearIndex) : minx(0, s.pts.Length / 4, s.pts, out nearIndex);
            Point startPt = s.pts[nearIndex];
            int nearInd = -1;
            int rightlobe = maxx(0, convertIndex((int)(.6 * s.pts.Length), s.skipped), s.s.GetPoints());
            for (int i = convertIndex((int)(.6 * s.pts.Length), s.skipped); i < s.s.GetPoints().Length; i++)
            {
                rightlobe = Math.Max(rightlobe, s.s.GetPoint(i).X);
                double dist = V2D.Dist(startPt, s.s.GetPoint(i));
                double closeAng = angle(startPt, s.s.GetPoint(i), new PointF(-1, 0));
                if (dist / (rightlobe - s.bbox.Left) < .37)
                    return true;
                if (dist / (rightlobe - s.bbox.Left) < .47)
                    if (closeAng < 20)
                        return true;
                if (dist < opening)
                {
                    nearInd = i;
                    opening = dist;
                }
            }
            int mx = maxx(0, nearInd, s.s.GetPoints());
            if ((s.s.GetPoint(s.s.GetPoints().Length - 1).X - mx + 0.0) / (mx - s.bbox.Left) > 0.1)
            {
                opening = double.MaxValue;
                return false;
            }
            Point stopPt = s.s.GetPoint(s.s.GetPoints().Length - 1);
            for (int i = 0; i < .1 * s.s.GetPoints().Length; i++)
            {
                double dist = V2D.Dist(stopPt, s.s.GetPoint(i));
                if (dist / s.bbox.Width < .47)
                    return true;
                if (dist < opening)
                    opening = dist;
            }
            return false;
        }
        public bool match_uv(CuspSet s)
        {
            if ((s.bbox.Width + 0.0) / s.bbox.Height > 2)
                return false;

            double opening;
            if (closedLoop(s, out opening))
                return false;
            double ang = angle(s.last, s.pts[0], new PointF(1, 0));
            if (ang > 55)
                return false;
            if (s.pts[0].X < s.last.X && Math.Sign(s.avgCurve(0, s.l)) == 1)
                return false;
            double closeAng = angle(s.s.GetPoint(0), s.pts[2], V2D.Sub(s.pts[s.pts.Length - 3], s.last));
            if (opening < Math.Max(InkPixel * 2, .34 * s.bbox.Width) || (opening / s.bbox.Width < 0.4 && closeAng < 100))
                return false;
            double openRatio = opening / s.bbox.Width;
            if (openRatio < 0.45 || (closeAng < 100 && openRatio < .6))
                return false;
            if (s.cusps.Length > 4)
                return false;
            int botind;
            int ma_y = maxy(0, s.pts.Length, s.pts, out botind);
            int maxlobeind;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.pts[0])), out maxlobeind);
            double lobeAng = angle(V2D.Normalize(V2D.Sub(s.pts[maxlobeind], V2D.Mul(V2D.Add(s.last, s.pts[0]), .5f))), new PointF(0, 1));
            if (lobeAng + ang > 65)
                return false;
            bool left;
            if (botind < 2 || botind > s.pts.Length - 3)
                return false;
            double startStraight = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[botind], s.pts[0])), out left, 0, botind) / s.bbox.Height;
            int endtopind; miny(botind, s.pts.Length, s.pts, out endtopind);
            double stopStraight = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[botind], s.pts[endtopind])), out left, endtopind, s.pts.Length) / s.bbox.Height;
            if (stopStraight > 0.15 && stopStraight / startStraight > 2.5)
                return false;
            if (s.intersects.Length > 0 && s.intersects[0] > 2 && (s.dist - s.distances[s.intersects[0]]) / s.dist > 0.2)
                return false;
            if (s.pts[botind].X > s.cusps[s.l].pt.X ||
                s.pts[botind].Y < s.cusps[0].pt.Y ||
                s.pts[botind].Y < s.cusps[s.l].pt.Y ||
                (s.cusps[0].pt.Y - s.bbox.Top + 0.0) / s.bbox.Height > .7 || (s.cusps[s.l].pt.Y - s.bbox.Top + 0.0) / s.bbox.Height > .7 ||
                Math.Abs(s.avgCurve(0, s.l)) < 0.03)
                return false;
            return true;
        }
        public bool match_u(CuspSet s)
        {
            if (s.cusps.Length > 7)
                return false;
            if (s.cusps.Length > 4 && (s.cusps[s.l].dist - s.cusps[3].dist) / V2D.Dist(s.cusps[s.l].pt, s.cusps[3].pt) > 1.25)
                return false;
            if ((s.bbox.Width + 0.0) / s.bbox.Height > 2.4)
                return false;
            if (s.cusps.Length < 3)
                return false;
            int toprightcusp = 2;
            for (int i = 3; i < s.l; i++)
                if (s.cusps[toprightcusp].pt.Y > s.cusps[i].pt.Y)
                    toprightcusp = i;
            int midind = s.cusps[1].index;
            if (s.cusps.Length == 3)
            {
                maxy(0, midind, s.pts, out midind);
                toprightcusp = 1;
            }
            if (midind == -1 || Math.Abs(s.curvatures[midind]) > 1 || (s.cusps[s.nl].pt.X - s.bbox.Left) < .24 * s.bbox.Width)
                return false;
            Point midpt = s.pts[midind];
            if (midpt.X > s.cusps[toprightcusp].pt.X ||
                midpt.Y < s.cusps[0].pt.Y ||
                midpt.Y < s.cusps[toprightcusp].pt.Y ||
                !s.cusps[0].top || !s.cusps[toprightcusp].top)
                return false;
            if (Math.Abs(s.cusps[toprightcusp].curvature) < 0.6 ||
                 s.cusps[s.l].pt.X < midpt.X ||
                 s.cusps[s.l].top ||
                 (s.cusps[s.l].pt.Y - midpt.Y + 0.0) / s.bbox.Height > 0.35)
                return false;
            double endang = angle(s.last, s.cusps[toprightcusp].pt, new Point(1, 2));
            if ((s.last.Y - s.pts[midind].Y + 0.0) / s.bbox.Height >= 0.3) // from 'w' rejection test
                return false;
            double enddist = (s.dist - s.cusps[toprightcusp].dist) / s.bbox.Height;
            if (enddist > 2)
                return false;
            if (angle(s.last, s.cusps[toprightcusp + 1].pt, new Point(1, 1)) > 60 || V2D.Straightness(s.pts, s.cusps[toprightcusp + 1].index, s.pts.Length) > 0.2) // if the tail is pretty straight, don't worry about any wiggles
                for (int c = toprightcusp + 1; c < s.cusps.Length - 1; c++)
                    if (s.cusps[c].curvature > 0.0)
                        return false;
            double topangs = angle(s.pts[0], s.cusps[toprightcusp].pt, new PointF(-1, 0));
            if (topangs > 50)
                return false;
            double ang = angle(s.last, s.cusps[toprightcusp].pt, new PointF(0, 1));
            if (ang > 60 || (s.last.X < s.cusps[toprightcusp].pt.X && ang > 20))
                return false;
            return true;
        }

        bool match_Omega(CuspSet s)
        {
            //Unicode Character 'GREEK CAPITAL LETTER OMEGA' (U+03A9) - "Ω"

            //Construct Point A and check to see that it's within the designated region
            if (s.cusps[0].top || !s.cusps[0].left)
                return false;
            Point A = s.pts[0];
            int b1xind = 0;

            //Construct Point E and check to see that it's within the designated region
            if (s.cusps[s.l].top || !s.cusps[s.l].right)
                return false;
            Point E = s.last;
            int b4xind = s.cusps[s.l].index;

            //Construct Point C (top of character)
            int t1yind, t1y = miny(0, s.pts.Length, s.pts, out t1yind);
            Point C = s.pts[t1yind];

            //Find and construct B, the point furthest away from Line Segment AC in first half
            bool bLeft; int b2xind;
            V2D.MaxDist(s.pts, A, V2D.Normalize(V2D.Sub(C, A)), out bLeft, out b2xind, 0, t1yind);
            Point B = s.pts[b2xind];
            if (bLeft == true)
                return false;

            //Verify that Line Segment AB's straightness & orientation is within limits
            if (V2D.Straightness(s.pts, b1xind, b2xind) > 0.22)
                return false;

            if (angle(B, A, new PointF(1, 0)) > 30)
                return false;

            //Verify that Arc BC's curvature is within limits
            if (V2D.Straightness(s.pts, b2xind, t1yind) < 0.15)
                return false;

            //Verify that Angle ABC is within limits
            if (angle(A, B, C) < 80)
                return false;

            //Construct D, the point furthest away from Line Segment CE in second half
            bool dLeft; int b3xind;
            V2D.MaxDist(s.pts, C, V2D.Normalize(V2D.Sub(E, C)), out dLeft, out b3xind, t1yind, b4xind);
            Point D = s.pts[b3xind];
            if (dLeft == false)
                return false;

            //Verify that Line Segment DE's straightness & orientation is within limits
            if (V2D.Straightness(s.pts, b3xind, b4xind) > 0.22)
                return false;

            if (angle(E, D, new PointF(1, 0)) > 30)
                return false;

            //Verify that Arc CD's curvature is within limits
            if (V2D.Straightness(s.pts, t1yind, b3xind) < 0.15)
                return false;

            //Verify that Angle CDE is within limits
            if (angle(C, D, E) < 80)
                return false;

            return true;
        }

        bool match_vectorArrow(CuspSet s)
        {
            //Unicode Character Vector Arrow (U+21C0)

            //Construct Point A (left part of character)
            int b1xind, b1x = minx(0, s.pts.Length, s.pts, out b1xind);
            Point A = s.pts[b1xind];

            //Construct Point C (top of character)
            int t1yind, t1y = miny(0, s.pts.Length, s.pts, out t1yind);
            Point C = s.pts[t1yind];

            //Find and construct B, the point furthest away from Line Segment AC
            bool bLeft; int b2xind;
            V2D.MaxDist(s.pts, A, V2D.Normalize(V2D.Sub(C, A)), out bLeft, out b2xind, 0, s.pts.Length);
            Point B = s.pts[b2xind];
            if (bLeft == true)
                return false;

            //Verify that Line Segment AB's straightness & orientation is within limits
            if (V2D.Straightness(s.pts, b1xind, b2xind) > 0.12)
                return false;
            if (angle(B, A, new PointF(1, 0)) > 15)
                return false;

            //Verify that Line Segment BC's straightness & orientation is within limits
            if (V2D.Straightness(s.pts, b2xind, t1yind) > 0.30)
                return false;
            if (angle(C, B, new PointF(1, 0)) < 120)
                return false;

            //Check that Line Segment BC's length is appropriate
            double lengthRatio = (s.distances[t1yind] - s.distances[b2xind]) / (float)s.dist;
            if (lengthRatio < 0.10 || lengthRatio > 0.4)
                return false;

            return true;
        }

        bool match_not(CuspSet s)
        {
            //Unicode Character 'NOT SIGN' (U+00AC)

            //Construct Point A (left part of character)
            int t1yind, t1y = minx(0, s.pts.Length, s.pts, out t1yind);
            Point A = s.pts[t1yind];


            //Construct Point C (bottom of character)
            int b1xind, b1x = maxy(0, s.pts.Length, s.pts, out b1xind);
            Point C = s.pts[b1xind];

            //Find and construct B, the point furthest away from Line Segment AC
            bool bLeft; int t2yind;
            V2D.MaxDist(s.pts, A, V2D.Normalize(V2D.Sub(C, A)), out bLeft, out t2yind, 0, s.pts.Length);
            Point B = s.pts[t2yind];
            if (bLeft == true)
                return false;

            if (s.distances[t1yind] / s.bbox.Height > 0.1 && angle(s.pts[t2yind], s.pts[t1yind], V2D.Sub(s.pts[0], s.pts[t1yind])) > 15)
                return false;

            //Verify that Line Segment AB's straightness & orientation is within limits
            if (V2D.Straightness(s.pts, t1yind, t2yind) > 0.12)
                return false;
            if (angle(B, A, new PointF(1, 0)) > 15)
                return false;

            //Verify that Line Segment BC's straightness & orientation is within limits
            if (V2D.Straightness(s.pts, t2yind, b1xind) > 0.20)
                return false;
            double angleBC = angle(C, B, new PointF(1, 0));
            if (angleBC < 70 || angleBC > 110)
                return false;

            //Check that Line Segment BC's length is appropriate
            double lengthRatio = (s.distances[b1xind] - s.distances[t2yind]) / (float)s.dist;
            if (lengthRatio < 0.10 || lengthRatio > 0.5)
                return false;

            return true;
        }

        bool match_w(CuspSet s, ref string allograph)
        {
            int start = Math.Min(0, s.pts.Length / 8);
            int b1yind, b1y = maxy(start, s.pts.Length / 2, s.pts, out b1yind);
            if (s.cusps.Length < 4)
                return false;
            if (b1yind < 1 || (s.distances[b1yind] / s.bbox.Height) < 0.1)
                return false;
            int b2yind, b2y = maxlocaly(s.pts.Length / 2, s.pts.Length, s.pts, out b2yind);
            if (b2yind == -1)
                return false;
            int t3yind = s.pts.Length - 1;
            int t2yind, t2y = miny(b1yind, b2yind, s.pts, out t2yind);
            if (t2yind == -1)
                return false;
            int t1yind = start;
            if (t1yind == -1)
                return false;
            if (s.pts[t1yind].X > s.pts[t2yind].X || s.pts[t2yind].X > s.pts[t3yind].X)
                return false;
            if (s.pts[b1yind].X > s.pts[b2yind].X || angle(s.pts[b2yind], s.pts[b1yind], new PointF(1, 0)) > 50)
                return false;
            if (Math.Abs(s.pts[t1yind].Y - s.pts[b1yind].Y + 0.0) / s.bbox.Height < 0.2)
                return false;
            if ((s.pts[b2yind].Y - s.last.Y + 0.0) / s.bbox.Height < 0.26)
                return false;
            if (angle(s.pts[(3 * t1yind + b1yind) / 4], s.pts[b1yind], V2D.Sub(s.pts[t2yind], s.pts[b1yind])) < 7.5 &&
                (s.distances[b1yind] - (s.distances[t2yind] - s.distances[b1yind])) / s.distances[b1yind] < 0.25)
                return false;
            if (V2D.Straightness(s.pts, t1yind, b1yind) < 0.21 &&
                V2D.Straightness(s.pts, b1yind, t2yind) < 0.21 &&
                V2D.Straightness(s.pts, t2yind, b2yind) < 0.21 &&
                V2D.Straightness(s.pts, b2yind, t3yind) < 0.21)
            {
                allograph = "w";
                return true;
            }
            return false;
        }
        bool match_omega(CuspSet s, ref string allograph)
        {
            if (s.cusps[0].bot || s.cusps[s.cusps.Length - 1].bot)
                return false;
            int b1yind, b1y = maxy(s.pts.Length / 8, s.pts.Length / 2, s.pts, out b1yind);
            if (b1yind < 1 || (s.distances[b1yind] / s.bbox.Height) < 0.1 || s.curvatures[b1yind] > 0)
                return false;
            int b2yind, b2y = maxlocaly(s.pts.Length / 2, s.pts.Length, s.pts, out b2yind);
            int t3yind = s.pts.Length - 1;
            int t2yind;
            if (b2yind == -1)
            {
                bool left;
                V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.pts[b1yind])), out left, out t2yind, b1yind, s.pts.Length);
                if (!left || (s.pts[b1yind].Y - s.pts[t2yind].Y + 0.0) / s.bbox.Height < 0.1)
                    return false;
                b2yind = (t3yind + t2yind) / 2;
            }
            else
                miny(b1yind, b2yind, s.pts, out t2yind);
            int t1yind, t1y = miny(0, b1yind, s.pts, out t1yind);
            if (t1yind == -1)
                return false;
            if ((s.pts[b1yind].Y - s.pts[t2yind].Y + 0.0) / s.bbox.Height < 0.1)
                return false;
            if (s.pts[t1yind].X > s.pts[t2yind].X || s.pts[t2yind].X > s.pts[t3yind].X)
                return false;
            if (s.pts[b1yind].X > s.pts[b2yind].X)
                return false;
            if (angle(s.pts[(3 * t1yind + b1yind) / 4], s.pts[b1yind], V2D.Sub(s.pts[t2yind], s.pts[b1yind])) < 7.5 &&
                (s.distances[b1yind] - (s.distances[t2yind] - s.distances[b1yind])) / s.distances[b1yind] < 0.25)
                return false;
            if (V2D.Straightness(s.pts, t1yind, b1yind) > 0.075 ||
                V2D.Straightness(s.pts, b1yind, t2yind) > 0.075 ||
                V2D.Straightness(s.pts, t2yind, b2yind) > 0.12 ||
                V2D.Straightness(s.pts, b2yind, t3yind) > 0.12)
            {
                double tailhgt = (s.pts[b2yind].Y - Math.Max(s.pts[t1yind].Y, s.pts[t3yind].Y) + 0.0) / s.bbox.Height;
                double taillen = V2D.Dist(s.last, s.pts[t2yind]) / s.bbox.Width;
                double tailtop = miny(b2yind, s.pts.Length, s.pts);
                if ((tailtop - s.bbox.Top + 0.0) / s.bbox.Height < .4 && tailhgt < 0.3 && taillen > 0.2 && V2D.Dist(s.last, s.pts[t2yind]) / (s.pts[t2yind].X - s.pts[0].X) < 5 && V2D.Straightness(s.pts, t2yind, t3yind) < 0.75)
                {
                    if (V2D.Dist(s.pts[b1yind], s.pts[t2yind]) / s.bbox.Height < 0.5)
                        return false;
                    if (s.cusps[1].curvature < (Math.Max(s.curvatures[t2yind - 1], Math.Max(s.curvatures[t2yind + 1], s.curvatures[t2yind])) < Math.Abs(s.cusps[1].curvature) ? -.75 : -1))
                        return false;
                    if (s.distances[t1yind] / s.bbox.Width < .1)
                        if (angle(s.pts[(t1yind + b1yind) / 2], s.pts[b1yind], V2D.Sub(s.pts[(t2yind + b1yind) / 2], s.pts[b1yind])) < 32)
                            return false;
                    if (s.curvatures[b1yind] < -2)
                        return false;
                    allograph = "vr";
                    return true;
                }
                int realt3yind; miny(b2yind, t3yind, s.pts, out realt3yind);
                if (realt3yind != -1 && V2D.Dist(s.last, s.pts[realt3yind]) / s.bbox.Height > 0.2 &&
                    angle(s.last, s.pts[realt3yind], new PointF(0, 1)) < 60) // has a final tail like an 'm'
                    return false;
                if ((s.pts[b2yind].Y - s.pts[t2yind].Y + 0.0) / s.bbox.Height < 0.075)
                    return false;
                if ((s.pts[b2yind].Y - s.pts[t1yind].Y + 0.0) / s.bbox.Height < 0.25)
                    return false;
                if ((s.pts[b2yind].Y - s.last.Y + 0.0) / s.bbox.Height < 0.26)
                    return false;
                if ((s.pts[b1yind].Y - s.pts[t1yind].Y + 0.0) / s.bbox.Height < 0.2)
                    return false;
                for (int i = 0; i < s.intersects.Length; i++)
                    if (s.intersects[i] > b2yind)
                        return false;
                if (s.curvatures[b2yind] > 0 && s.curvatures[b2yind] < 2)
                    return false;
                if ((s.pts[0].X - minx(0, s.pts.Length, s.pts) + 0.0) / s.bbox.Width < 0.06 ||
                    (maxx(0, s.pts.Length, s.pts) - s.last.X + 0.0) / s.bbox.Width < 0.06)
                    allograph = "w";
                else if ((s.intersects.Length == 0 && s.avgCurveSeg(b1yind + 2, t2yind - 1) > 0) ||
                     s.avgCurveSeg(b2yind + 1, t3yind - 1) > 0 ||
                    s.avgCurveSeg(t2yind + 1, b2yind - 1) > 0)
                    allograph = "w";
                else allograph = "omega";
                return true;
            }
            return false;
        }
        public bool match_z(CuspSet s)
        {
            if (s.cusps.Length < 3)
                return false;
            int maxind, minind;
            int ma_x = maxx(0, s.pts.Length / 2, s.pts, out maxind);
            int mi_x = minx(s.pts.Length / 2, s.pts.Length, s.pts, out minind);
            if (Math.Abs(maxind - s.cusps[1].index) < 3) maxind = s.cusps[1].index;
            if (Math.Abs(minind - s.cusps[2].index) < 3) minind = s.cusps[2].index;
            if (s.cusps.Length < 3 || (s.intersects.Length != 0 && (s.distances[s.intersects[s.intersects.Length - 1]] - s.distances[s.intersects[0]]) / InkPixel > 3))
                return false;
            if ((s.pts[maxind].Y - s.cusps[0].pt.Y + 0.0) / (s.pts[maxind].X - s.cusps[0].pt.X) > 0.6)
                return false;
            if ((s.last.X - s.pts[minind].X + 0.0) / s.bbox.Width < 0.33)
                return false;
            double ang = angle(s.pts[minind], s.pts[maxind], new Point(-1, 1));
            if (ang > 25)
                return false;
            if (Math.Abs(s.cusps[s.l].pt.Y - s.pts[minind].Y + 0.0) / (s.cusps[s.l].pt.X - s.pts[minind].X) > 0.5)
                return false;
            if (maxind == 0 || minind == s.pts.Length - 1)
                return false;
            double maxcurve = Math.Max(s.curvatures[maxind - 1], Math.Max(s.curvatures[maxind + 1], s.curvatures[maxind]));
            double mincurve = Math.Min(s.curvatures[minind - 1], Math.Min(s.curvatures[minind + 1], s.curvatures[minind]));
            if ((maxcurve < 0 && (s.intersects.Length < 2 || (s.intersects[1] < maxind && s.intersects[0] > maxind))) || mincurve > 0)
                return false;
            if (V2D.Straightness(s.pts, maxind, minind) > 0.2 ||
                V2D.Straightness(s.pts, minind, s.pts.Length) > .5)
                return false;
            double s1 = V2D.Straightness(s.pts, 0, maxind);
            double s2 = V2D.Straightness(s.pts, maxind, minind);
            if (s1 < 0.13 && s2 < 0.15 &&
                s.cusps[0].pt.X < s.pts[maxind].X &&
                s.pts[minind].X < s.cusps[s.l].pt.X &&
                s.cusps[0].pt.Y < s.pts[minind].Y &&
                s.pts[maxind].Y < s.last.Y)
                return true;
            return false;
        }
        bool match_alpha(CuspSet s)
        {
            if (s.cusps[0].bot || s.cusps[s.l].top)
                return false;
            if (s.intersects.Length != 2)
                return false;
            int toplobe = miny(s.intersects[0], s.intersects[1], s.pts);
            int botlobe = maxy(s.intersects[0], s.intersects[1], s.pts);
            if ((botlobe - toplobe + 0.0) / s.bbox.Height < 0.15)
                return false;
            if (s.avgCurveSeg(s.intersects[0], s.intersects[1]) < 0 || s.intersects[1] - s.intersects[0] < 2)
                return false;
            double angIn = angle(s.pts[0], s.pts[s.intersects[0]], new Point(1, -1));
            double angOut = angle(s.last, s.pts[s.intersects[1]], new Point(1, 1));
            double st1 = V2D.Straightness(s.pts, 0, s.intersects[0]);
            double st2 = V2D.Straightness(s.pts, s.intersects[1], s.pts.Length);
            if (st1 > 0.5 ||
                st2 > 0.5 ||
                (st1 > 0.1 && s.avgCurveSeg(2, s.intersects[0]) < 0))
                return false;
            if (angIn > 45 || angOut > 45)
                return false;
            return true;
        }
        bool match_alphax(CuspSet s)
        {
            if (s.cusps[0].bot || s.cusps[0].left || s.cusps[s.l].left || s.cusps[s.l].top || s.cusps[1].right || s.cusps[s.nl].right)
                return false;
            if (s.intersects.Length != 2 && s.intersects.Length != 4)
                return false;
            if (s.cusps.Length != 4)
                return false;

            if (s.intersects[s.intersects.Length - 1] - s.intersects[0] < 2) // Too small a loop
                return false;
            double angIn = angle(s.pts[0], s.pts[s.intersects[0]], new Point(1, -1));
            double angOut = angle(s.last, s.pts[s.intersects[s.intersects.Length - 1]], new Point(1, 1));
            double st1 = V2D.Straightness(s.pts, 0, s.intersects[0]);
            double st2 = V2D.Straightness(s.pts, s.intersects[s.intersects.Length - 1], s.pts.Length);
            double stVert = V2D.Straightness(s.pts, s.cusps[1].index, s.cusps[2].index);
            if (st1 > 0.5 ||
                st2 > 0.5 ||
                stVert > 0.1 ||
                (st1 > 0.1 && s.avgCurveSeg(2, s.intersects[0]) < 0))
                return false;
            if (angIn > 45 || angOut > 45)
                return false;
            return true;
        }
        public bool match_2(CuspSet s, ref int xht, ref string allograph)
        {
            if (s.cusps[0].bot || (s.last.Y - s.bbox.Top + 0.0) / s.bbox.Height < 0.25)
                return false;
            if (s.intersects.Length < 2)
                return false;
            int int1 = s.intersects[s.intersects.Length - 2];
            int int2 = s.intersects[s.intersects.Length - 1];
            if (int1 < 1)
                return false;
            int leftlobeind; minx(0, int1, s.pts, out leftlobeind);
            int posCurv, mix = maxx(leftlobeind, int1, s.pts, out posCurv);
            int toploopind; miny(leftlobeind, posCurv, s.pts, out toploopind);
            bool left;
            if (toploopind == leftlobeind)
                V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[posCurv], s.pts[leftlobeind])), out left, out  toploopind, leftlobeind, posCurv);

            for (int i = 1; i < s.cusps.Length - 1; i++)
                if (s.cusps[i].index > leftlobeind && s.cusps[i].index < int2 && s.cusps[i].curvature < 0.1)
                    return false;
            toploopind = toploopind == -1 ? posCurv : toploopind;
            if (s.cusps.Length > 2)
            {
                if ((s.cusps[1].dist - s.distances[toploopind]) / s.dist < 0.05)
                    toploopind = s.cusps[1].index;
                if ((s.cusps[2].dist - s.distances[toploopind]) / s.dist < 0.05)
                    toploopind = s.cusps[2].index;
            }
            if (posCurv > leftlobeind && s.curvatures[(posCurv + leftlobeind) / 2] < 0 && V2D.Straightness(s.pts, leftlobeind, toploopind) > 0.01 &&
                (s.curvatures[toploopind] < 0 || s.curvatures[toploopind] > 1.4))
                return false;
            if (s.intersects.Length > 2)
            {
                int maxind;
                maxx(posCurv + 1, s.intersects[0] - 3, s.pts, out maxind); // reject leftward loops at start
                if (maxind > 0 && V2D.Straightness(s.pts, maxind, s.intersects[0] - 3, out left) > 0.1 && left)
                    return false;
                if ((s.distances[s.intersects[1]] - s.distances[s.intersects[0]]) / s.bbox.Height > 0.1)
                    return false;
            }
            int toplobeind, toplobe = miny(int1, int2, s.pts, out toplobeind);
            if ((toplobe - miny(0, int1, s.pts) + 0.0) / s.bbox.Height < 0.25)
                return false;
            double attackang = angle(s.pts[0], s.pts[4], new Point(1, -1));
            if (s.cusps[0].top && s.cusps[0].right && attackang < 50 &&
                (s.pts[int1].Y - s.pts[0].Y + 0.0) / (s.bbox.Bottom - s.pts[int1].Y) < 2)
                return false;

            int rightlobeind, rx = maxx(leftlobeind, int1, s.pts, out rightlobeind);
            if (rx < s.pts[int1].X && s.angles[int1] < -Math.PI / 2 - Math.PI / 6)
                return false;
            for (int i = 0; i < s.cusps.Length; i++)
                if (s.cusps[i].index <= rightlobeind && s.cusps[i].index > posCurv && (s.cusps[i].curvature < 0 || s.avgCurve(i - 1, i) < 0))
                    return false;
            if (s.distances[leftlobeind] / s.bbox.Width > 0.15 &&
                (s.distances[int1] - s.distances[rightlobeind]) / s.bbox.Width > 0.2 &&
                (s.distances[rightlobeind] - s.distances[leftlobeind]) / s.bbox.Width > 0.2 &&
                V2D.Straightness(s.pts, leftlobeind, rightlobeind) < 0.1 && V2D.Straightness(s.pts, rightlobeind, int1) < 0.1)
                return false;

            double stratio = (s.pts[posCurv].X - s.bbox.Left + 0.0) / s.bbox.Width;
            double stangle = angle(s.pts[toploopind + 3], s.pts[toploopind], new PointF(1, 0));
            double extends = (s.dist - s.distances[int2]) / s.bbox.Height;
            if (extends < .07 || (extends < .15 && ((s.angles[int1] < 3 && s.angles[int1] > 1) || angle(s.pts[int1], s.pts[int1 - 1], V2D.Sub(s.pts[int2], s.pts[int2 - 1])) < 50)))
                return false;
            if (stangle > 50)
            {
                if (extends < .15)
                    return false;
                double straightness = V2D.Straightness(s.pts, 0, int1);
                if ((straightness < 0.15 && (extends < 0.2 || extends > 0.9 || stangle > 90)) && (stratio > 0.65 || stangle > 70))
                    return false;
            }
            if (s.avgCurveSeg(int1, int2) < 0 && int2 - int1 > 2)
                return false;
            double ang = angle(s.last, s.pts[Math.Min(s.cusps[s.l].index - 4, int2)], new Point(3, 2));
            double wavy = V2D.Dist(s.cusps[s.l].pt, s.pts[Math.Min(s.cusps[s.l].index - 4, int2)]) /
                (s.dist - s.distances[Math.Min(s.cusps[s.l].index - 4, int2)]);
            if (ang < 50 || (ang < 80 && stratio > 0.4 && wavy > 0.8))
            {
                if (extends < 0.05)
                    allograph = "2p";
                else allograph = "2";
                xht = (s.bbox.Top + s.bbox.Bottom) / 2;
                return true;
            }
            return false;
        }
        public bool match_2z(CuspSet s, ref string allograph)
        {
            if (s.cusps[0].bot || s.intersects.Length > 2)
                return false;
            if (s.intersects.Length > 1 && (s.distances[s.intersects[1]] - s.distances[s.intersects[0]]) / Math.Max(s.bbox.Width, s.bbox.Height) > 0.1)
                return false;
            int maxind, maxindreal, minind, topind;
            int ma_x = maxx(0, 2 * s.pts.Length / 3, s.pts, out maxind);
            int mi_x = minx(s.pts.Length / 2, s.pts.Length, s.pts, out minind);
            if (maxind > minind)
                ma_x = maxx(0, minind, s.pts, out maxind);
            int to_y = miny(0, s.pts.Length / 2, s.pts, out topind);
            maxindreal = convertIndex(maxind, s.skipped);
            if ((topind > 3 && Math.Sign(s.curvatures[topind]) == -1) || maxindreal < 3 || V2D.Dist(s.s.GetPoint(0), s.pts[maxind]) / s.bbox.Height < 0.1)
                return false;
            //fail if less than 3 cusps or there are intersections
            if (s.cusps.Length < 3)
                return false;
            int minretraceind; minx(0, maxind, s.pts, out minretraceind); // check for an acceptable hook at the top left of the 2
            if (minretraceind > 1 && s.distances[minretraceind] > Math.Max(InkPixel, s.bbox.Height * .05))
            {
                double retang = angle(s.pts[0], s.pts[minretraceind], new PointF(0, -1));
                double pad = (90 - retang) / 90;
                if (pad < .6) pad -= .6;
                if (V2D.Dist(s.pts[minretraceind], s.pts[0]) / (ma_x - s.pts[minretraceind].X) > 0.7 + pad)
                    return false;
            }
            if ((s.last.X - s.pts[minind].X + 0.0) / s.bbox.Width < 0.33)
                return false;
            if ((s.distances[minind] - s.distances[maxind]) / V2D.Dist(s.pts[maxind], s.pts[minind]) > 1.15)
                return false;
            double angS = angle(s.pts[minind], s.pts[maxind], new Point(-1, 0));
            if (angS < 10 || angS > 90)
                return false;
            if (angS > 75 && s.distances[maxind] / s.bbox.Height < 0.2)
                return false;
            double angtop = angle(s.pts[0], s.pts[maxind], V2D.Sub(s.pts[minind], s.pts[maxind]));
            double angtopvert = angle(s.pts[0], s.pts[maxind], new PointF(0, -1));
            if (angtop > 145)
                return false;
            if (Math.Abs(s.last.Y - s.pts[minind].Y + 0.0) / (s.last.X - s.pts[minind].X) > 0.6)
                return false;
            if (minind == s.pts.Length - 1)
                return false;
            double mincurve = Math.Min(s.curvatures[minind - 1], Math.Min(s.curvatures[minind + 1], s.curvatures[minind]));
            if (mincurve > 0)
                return false;
            double s1 = maxind < 3 ? V2D.Straightness(s.s.GetPoints(), 0, maxindreal) : V2D.Straightness(s.pts, 0, maxind);
            double s2 = V2D.Straightness(s.pts, maxind, minind);
            double curveSize = (ma_x - s.pts[0].X + 0.0) / s.bbox.Height;
            double s3 = V2D.Straightness(s.pts, minind, s.pts.Length);
            if (s3 < .5 && (s1 > Math.Min(curveSize / 2, 0.10 - (90 - angtopvert) / 180.0 / 8) || s2 > Math.Min(curveSize / 2, 0.13)) &&
                s.s.GetPoint(0).X < s.s.GetPoint(maxindreal).X &&
                s.pts[minind].X < s.cusps[s.l].pt.X &&
                s.s.GetPoint(0).Y < s.pts[minind].Y &&
                (s.last.Y - s.s.GetPoint(maxindreal).Y + 0.0) / s.bbox.Height > -0.1)
            {
                double topang = angle(s.pts[0], s.pts[(topind + maxind) / 2], new Point(-1, 1));
                allograph = s1 + Math.Max(0, s2 - .03) / 4 > Math.Min(curveSize / 2, 0.13) || s1 > s3 * 3 || topang < 35 || 90 - angtopvert > 15 ? "2z" : "z2";
                return true;
            }
            return false;

        }
        bool match_dcursive(CuspSet s)
        {
            if (s.cusps.Length > 8)
                return false;
            int leftlobeind, topind, botlobeind, topdind;
            minx(0, s.pts.Length, s.pts, out leftlobeind);
            miny(leftlobeind, s.pts.Length, s.pts, out topdind);
            if (topdind == -1)
                return false;
            if (s.cusps[s.l].top)
                return false;
            bool hasstemloop = s.intersects.Length > 0 && topdind < s.intersects[s.intersects.Length - 1] && topdind > s.intersects[0];
            int endloopind = hasstemloop ? s.intersects[s.intersects.Length - 1] : s.pts.Length - 1;
            maxy(0, topdind, s.pts, out botlobeind);
            miny(0, botlobeind, s.pts, out topind);
            if (topind == -1 || topind > endloopind)
                return false;
            if (leftlobeind > botlobeind)
                return false;
            double ang = angle(s.pts[topind + 3], s.pts[topind], new PointF(-1, 0));
            if (ang > 80)
                return false;
            for (int i = 1; i < s.cusps.Length - 1; i++)
                if (s.cusps[i].curvature > 0.25 && s.cusps[i].curvature < 1)
                    return false;
            int botind, rightind;
            int boty = maxy(topdind, s.pts.Length, s.pts, out botind);
            int rightx = maxx(botind, s.pts.Length, s.pts, out rightind);
            if (endloopind > botind && hasstemloop)
                return false;
            if ((s.cusps[0].pt.Y - s.bbox.Top + 0.0) / s.bbox.Height < 0.30)
                return false;
            if (hasstemloop && (angle(s.pts[topdind], s.pts[botind], new PointF(0, -1)) > 20 && s.avgCurveSeg(botlobeind, endloopind) > -0.03))
                return false;
            if (!hasstemloop && s.curvatures[topdind] < 0.1)
                return false;
            if ((s.pts[topind].Y - s.bbox.Top + 0.0) / s.bbox.Height < 0.25)
                return false;
            if ((miny(botlobeind, endloopind, s.pts) - s.bbox.Top + 0.0) / s.bbox.Height > 0.2)
                return false;
            if (hasstemloop && s.avgCurveSeg(endloopind, s.pts.Length) > 0.01)
                return false;
            return true;
        }
        bool match_k(CuspSet s, ref string allograph)
        {
            int startcusp = 0;
            if (!s.cusps[0].top)
            {
                if (s.intersects.Length < 2 || s.cusps.Length < 5 || s.cusps.Length > 7)
                    return false;
                startcusp = 1;
                //if (!s.cusps[0].left)
                //    return false;
            }
            int minyind, my = miny(s.cusps[startcusp + 1].index, s.pts.Length, s.pts, out minyind);
            if (!s.cusps[startcusp].top || !s.cusps[startcusp + 1].bot ||
                (s.pts[0].X - s.bbox.Left) / (float)s.bbox.Width > .65 ||
                (!s.cusps[s.nl].bot && !s.cusps[s.l].bot) ||
                s.cusps[s.l].pt.X < s.cusps[startcusp + 1].pt.X)
                return false;
            if (s.pts[minyind].Y < s.cusps[startcusp].pt.Y)
                return false;
            double kang = angle(s.pts[minyind], s.cusps[startcusp + 1].pt, V2D.Sub(s.cusps[startcusp].pt, s.cusps[startcusp + 1].pt));
            if (kang > 40)
                return false;
            if ((s.pts[minyind].Y - s.cusps[startcusp].pt.Y + 0.0) / s.bbox.Height < 0.15)
                return false;
            if (startcusp > 0 && s.avgCurve(0, startcusp) > 0.04)
                return false;

            int topLoopInd; miny(s.cusps[startcusp + 1].index, s.pts.Length, s.pts, out topLoopInd);
            if (topLoopInd == -1 || s.curvatures[topLoopInd] < 0) return false;
            int leftLoopInd = topLoopInd;
            for (int i = topLoopInd; i < s.pts.Length; i++)
                if ((s.curvatures[i] < 0 || s.curvatures[i] > 1) && s.pts[i].X < s.pts[leftLoopInd].X)
                    leftLoopInd = i;
            if (leftLoopInd == -1) return false;
            int rightLoopInd; maxx(topLoopInd, leftLoopInd, s.pts, out rightLoopInd);
            int botLoopInd; maxy(leftLoopInd, s.pts.Length, s.pts, out botLoopInd);
            if (botLoopInd == -1)
                botLoopInd = leftLoopInd;
            else if (miny(leftLoopInd, botLoopInd, s.pts) < s.pts[leftLoopInd].Y - 2 * InkPixel)
                return false;
            string allographwillbe = "";
            if (s.nnl > startcusp + 1 && s.cusps[s.nnl].pt.Y > s.cusps[s.nl].pt.Y && s.cusps[s.nnl].curvature < 0 && s.cusps[s.nl].curvature > 0 && s.last.Y > s.cusps[s.nl].pt.Y)
                return false;
            if (rightLoopInd == -1 || rightLoopInd == leftLoopInd || (s.distances[leftLoopInd] - s.distances[rightLoopInd]) / s.bbox.Height < 0.1 ||
                V2D.Straightness(s.pts, rightLoopInd, s.pts.Length) < 0.15)
            {
                int rightx = maxx(topLoopInd, s.pts.Length, s.pts);
                if ((s.last.X - s.cusps[startcusp + 1].pt.X + 0.0) / s.bbox.Width < 0.75 * (rightx - s.cusps[startcusp + 1].pt.X + 0.0) / s.bbox.Width)
                    return false;
                PointF dir = V2D.Normalize(V2D.Sub(s.pts[topLoopInd],/*new Point(s.cusps[startcusp+1].pt.X, s.pts[topLoopInd].Y),*/ s.cusps[startcusp].pt));
                double ang = angle(dir, new PointF(1, 0));
                if (ang < 37.5 || (s.cusps[0].top && V2D.Straightness(s.pts, 0, s.cusps[startcusp + 1].index) > 0.175))
                    return false;
                allographwillbe = "h";
            }
            else
            {
                double a1 = angle(s.pts[leftLoopInd], s.pts[rightLoopInd], new Point(-1, 1));
                double a2 = botLoopInd == leftLoopInd ? 180 : angle(s.pts[rightLoopInd], s.pts[leftLoopInd], V2D.Sub(s.pts[botLoopInd], s.pts[leftLoopInd]));
                double tail = angle(s.last, s.pts[botLoopInd], new Point(1, -1));
                if (((s.bbox.Bottom - s.pts[leftLoopInd].Y + 0.0) / s.bbox.Height < 0.15 && angle(s.last, s.pts[leftLoopInd], new Point(1, -1)) < 50 &&
                      (s.pts[leftLoopInd].X - s.cusps[startcusp + 1].pt.X + 0.0) / s.bbox.Width > 0.2 && a1 > 15) ||
                     a2 > 160 || a1 > 45 || (a1 > 40 && tail < 45))
                    allographwillbe = "h";
                else allographwillbe = "k";
            }
            if (s.curvatures[topLoopInd] > 2 && s.cusps[startcusp + 1].curvature > -1)
            {
                int toptail = miny(topLoopInd, s.pts.Length, s.pts);
                if ((toptail - s.bbox.Top + 0.0) / s.bbox.Height < 0.5)
                    return false;
                allographwillbe = "hu";
            }

            if (s.cusps.Length - startcusp + 1 > 3)
            {
                if (s.cusps[s.nnl].index > leftLoopInd && s.cusps[s.nl].curvature > 0 && s.cusps[s.nnl].curvature > 0)
                    return false;
            }
            // reject 2's that look like h's but have a bigger bottom loop
            double tailang = angle(s.last, s.pts[topLoopInd], new PointF(1, 0));
            if (tailang < 25 && V2D.Straightness(s.pts, topLoopInd, s.pts.Length) < .15)
                return false;
            if (allographwillbe == "h" && V2D.Straightness(s.pts, leftLoopInd, s.pts.Length) > 0.3 && (s.dist - s.distances[leftLoopInd]) / s.bbox.Width > 0.9
                && (s.last.Y - s.bbox.Top) / (float)s.bbox.Height < 0.5)
                return false;
            // reject really big lobes (probably an 'n'), or very little lobes (probably an L)
            double loberatio = (s.bbox.Bottom - s.pts[topLoopInd].Y + 0.0) / s.bbox.Height;
            if (loberatio > .635 * Math.Max(1, 1.0 * s.bbox.Height / s.bbox.Width))
            {
                int minlobeind; miny(s.cusps[startcusp + 1].index, s.pts.Length, s.pts, out minlobeind);
                int minsqueezeind; double minsqueeze = minx(topLoopInd, s.pts.Length, s.pts, out minsqueezeind);
                if (minsqueezeind != -1)
                {
                    int maxlobebulge; double maxbulge = maxx(s.cusps[startcusp + 1].index, minsqueezeind, s.pts, out maxlobebulge);
                    if (maxlobebulge == -1 || (s.pts[maxlobebulge].X - minsqueeze) / s.bbox.Width < 0.25 || (s.last.X - minsqueeze + 0.0) / s.bbox.Width < 0.25)
                        return false;
                }
                else
                    return false;
            }
            if (loberatio < .15 * Math.Max(1, 2.0 * s.bbox.Width / s.bbox.Height))
                return false;
            allograph = allographwillbe;
            return true;
        }
        public bool match_el(CuspSet s, ref string allograph)
        {
            for (int i = 1; i < s.cusps.Length - 1; i++)
                if (s.cusps[i].curvature > 0 && s.cusps[i].curvature < 3 &&
                    V2D.Straightness(s.pts, s.cusps[i - 1].index, s.cusps[i + 1].index) > 0.2)
                    return false;
            int inter1ind;
            int inter2ind;
            if (s.intersects.Length > 1)
            {
                inter1ind = s.intersects[0];
                inter2ind = s.intersects[1];
                if (Math.Abs(s.angles[inter1ind]) < Math.PI / 2)
                    return false;
            }
            else
            {
                inter1ind = 0;
                inter2ind = s.pts.Length - 1;
                int rightlobeind; maxx(0, s.pts.Length / 2, s.pts, out rightlobeind);
                int leftlobeind; minx(rightlobeind, s.pts.Length, s.pts, out leftlobeind);
                if (Math.Abs(s.angles[leftlobeind]) > 2)
                    return false;
                int botlobe = maxy(0, rightlobeind, s.pts);
                for (int i = rightlobeind + 2; i < s.pts.Length; i++)
                    if (s.pts[i].Y > botlobe)
                    {
                        inter2ind = i;
                        break;
                    }
            }
            if ((s.pts[inter1ind].Y - s.pts[0].Y + 0.0) / s.bbox.Height > 0.4)
                return false;
            int topind = inter1ind; double loopdist = 0;
            for (int i = inter1ind; i < inter2ind; i++)
                if (s.pts[i].Y < s.pts[inter1ind].Y && V2D.Dist(s.pts[i], s.pts[inter1ind]) > loopdist)
                {
                    loopdist = V2D.Dist(s.pts[i], s.pts[inter1ind]);
                    topind = i;
                }
            if (inter1ind != 0 && V2D.Straightness(s.pts, 0, inter1ind) > 0.18 && s.distances[inter1ind] / s.bbox.Height > .225)
            {
                int minretraceind; minx(0, topind, s.pts, out minretraceind);
                if (minretraceind == -1 ||
                    (s.pts[0].X - s.pts[minretraceind].X) / (float)s.bbox.Width > 0.25 ||
                    (s.distances[minretraceind] / s.bbox.Height > 0.2 && V2D.Straightness(s.pts, minretraceind, topind) > 0.3))
                    return false;
            }
            if (inter1ind != -1)
            {
                int rightind;
                maxx(inter1ind, Math.Min(s.pts.Length, topind + 3), s.pts, out rightind);
                if (rightind >= topind && s.curvatures[rightind] > 0 && s.curvatures[rightind] < 3)
                    return false;
                if (rightind == -1)
                    return false;
                if (V2D.Dist(s.pts[rightind], s.pts[inter1ind]) / s.bbox.Height < 0.2)
                    return false;
                if (V2D.Straightness(s.pts, inter1ind, rightind) > 0.4)
                    return false;
                if (rightind != topind && V2D.Straightness(s.pts, rightind, topind) > 0.25 && (s.distances[topind] - s.distances[rightind]) / s.bbox.Height > .225)
                    return false;
            }
            if (inter2ind == -1)
                return false;
            else
            {
                bool left;
                int leftbotind;
                V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.pts[inter2ind])), out left, out leftbotind, inter2ind, s.pts.Length - 1);
                V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[topind], s.last)), out left, topind, leftbotind);
                if (!left)
                    return false;
                if (V2D.Straightness(s.pts, inter2ind, leftbotind) > 0.35 && (s.distances[leftbotind] - s.distances[inter2ind]) / s.bbox.Height > .225)
                    return false;
                if (s.pts.Length - 1 != leftbotind && V2D.Straightness(s.pts, leftbotind, s.pts.Length) > 0.5 && (s.dist - s.distances[leftbotind]) / s.bbox.Height > .225)
                    return false;
            }
            if ((maxy(inter1ind, inter2ind, s.pts) - s.bbox.Top + 0.0) / s.bbox.Height < 0.25 && Math.Abs(s.angles[inter1ind]) < Math.PI / 2)
                return false;
            if (s.cusps[0].top && s.cusps[s.l].top)
                return false;
            int botind; maxy(inter2ind, s.pts.Length, s.pts, out botind);
            if (s.intersects.Length == 0 && Math.Abs(s.angles[botind]) < Math.PI / 2)
                return false;
            double loopang = angle(s.pts[inter1ind], s.pts[topind], new PointF(0, 1));
            double loopiness = (s.distances[inter2ind] - s.distances[inter1ind]) / V2D.Dist(s.pts[topind], s.pts[inter1ind]) / 2;
            double preloopstart = (s.pts[0].Y - s.bbox.Top) / (float)s.bbox.Height;
            double preloop = s.distances[inter1ind] / s.bbox.Height * preloopstart;
            double aspect = s.bbox.Width / (float)s.bbox.Height;
            double tailextension = (s.dist - s.distances[inter2ind]) / (s.distances[inter2ind] / 2);
            if (tailextension == 0 ||
                (loopang < 25 * (1 - aspect) / .33 * preloopstart + Math.Min(.3, preloop) * 70 / Math.Max(.6, tailextension) && (tailextension < 1 || preloop > 0.15 || aspect < 0.35)))
            {
                if ((s.pts[botind].Y - miny(botind, s.pts.Length, s.pts) + 0.0) / s.bbox.Height > 0.3 && (s.pts[botind].Y - s.pts[0].Y + 0.0) / s.bbox.Height > 0.3)
                    return false;
                allograph = "l";
            }
            else
            {
                bool left;
                double md = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[botind], s.last)), out left, botind, s.pts.Length);
                if (md / s.bbox.Width > 0.15 && left)
                    return false;
                allograph = "e";
            }
            return true;
        }
        bool match_gamma(CuspSet s)
        {
            if (s.cusps.Length > 4 || s.intersects.Length < 2)
                return false;
            double maxDist = 0;
            int botLobe = s.intersects[0];
            for (int i = s.intersects[0]; i < s.intersects[1]; i++)
            {
                double dist = V2D.Dist(s.pts[i], s.pts[s.intersects[0]]);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    botLobe = i;
                }
            }
            if (angle(s.pts[s.intersects[0]], s.pts[botLobe], new PointF(0, -1)) > 20)
                return false;
            if (1.0 * s.bbox.Width / s.bbox.Height > 1.4 ||
                s.intersects.Length == 0 ||
                s.cusps[0].bot)
                return false;
            if (s.cusps.Length == 3 &&
                (Math.Sign(s.avgCurve(0, 1)) == 1 || Math.Abs(s.avgCurve(0, 1)) > -0.04) &&
                Math.Sign(s.avgCurve(1, 2)) == 1 &&
                Math.Sign(s.cusps[1].curvature) == 1)
                return true;
            if (s.cusps.Length > 3 &&
                s.cusps[2].curvature < 0.4 &&
                s.avgCurve(0, 1) > -0.04 &&
                s.avgCurve(1, 2) > -0.04 &&
                Math.Sign(s.cusps[1].curvature) == 1 &&
                Math.Sign(s.cusps[2].curvature) == 1 &&
                s.cusps[0].pt.X < s.cusps[1].pt.X &&
                s.cusps[1].pt.Y > s.cusps[s.l].pt.Y)
                return true;
            return false;
        }
        bool match_m(CuspSet s, ref string allograph)
        {
            if (s.cusps.Length < 3 || s.bbox.Height < InkPixel * 4 || s.bbox.Width / (float)s.bbox.Height > 4)
                return false;
            bool hasDown = (s.cusps[1].pt.Y > s.cusps[0].pt.Y);
            int start = hasDown ? s.cusps[1].index : Math.Min(6, s.pts.Length / 8);
            int t1yind, t1y = miny(start, (s.pts.Length - start) / 2 + start, s.pts, out t1yind);
            if (t1yind == -1)
                return false;
            int t2yind, t2y = miny((s.pts.Length - start) / 2 + start, s.pts.Length, s.pts, out t2yind);
            if (t2yind == -1)
                return false;
            int b1yind = start;
            if (b1yind == -1)
                return false;
            int b2yind, b2y = maxy(t1yind, t2yind, s.pts, out b2yind);
            if (b2yind == -1 || b2yind == t1yind)
                return false;
            bool mulike = ((s.pts[hasDown ? s.cusps[1].index : 0].Y - maxy(b2yind, s.pts.Length, s.pts) + 0.0) / s.bbox.Height > 0.3 &&
                angle(s.pts[b1yind], s.pts[t1yind], V2D.Sub(s.pts[b2yind], s.pts[t1yind])) < 15);
            int b3yind, b3y = maxy(t2yind, s.pts.Length, s.pts, out b3yind);
            if ((s.pts[b3yind].Y - s.pts[b2yind].Y + 0.0) / s.bbox.Height < -0.4)
                return false;
            if ((s.pts[b1yind].Y - s.pts[t1yind].Y + 0.0) / s.bbox.Height < .2)
                return false;
            if ((s.pts[b2yind].Y - s.pts[t1yind].Y + 0.0) / s.bbox.Height < .2)
                return false;
            if ((s.pts[b3yind].Y - s.pts[t2yind].Y + 0.0) / s.bbox.Height < (mulike ? 0.2 : 0.25))
                return false;
            if ((s.pts[b2yind].Y - s.pts[t1yind].Y + 0.0) / s.bbox.Height < .1)
                return false;
            if (s.pts[b1yind].X > s.pts[b2yind].X || s.pts[b2yind].X > s.pts[b3yind].X) // bot cusps line horizontally
                return false;
            if (s.pts[t2yind].Y > s.pts[b2yind].Y || s.pts[t2yind].Y > s.pts[b3yind].Y) // top cusps above bot cusps
                return false;
            if (s.pts[t1yind].X > s.pts[t2yind].X)
                return false;
            if (s.pts[b1yind].X > s.pts[b2yind].X || s.pts[b2yind].X > s.pts[b3yind].X)
                return false;
            if (hasDown && V2D.Straightness(s.pts, 0, b1yind) > 0.15)
                return false;
            bool left;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.pts[t2yind])), out left);
            if (V2D.Straightness(s.pts, b1yind, t1yind) > 0.3 ||
                V2D.Straightness(s.pts, t1yind, b2yind) > 0.25 ||
                V2D.Straightness(s.pts, b2yind, t2yind) > 0.2 ||
                (V2D.Straightness(s.pts, b2yind, t2yind) > 0.2 && !left) ||
                V2D.Straightness(s.pts, t2yind, b3yind) > 0.35)
                return false;
            if (V2D.Dist(s.last, s.pts[b3yind]) / s.bbox.Height > 0.1 && s.last.X < (s.pts[b3yind].X + s.pts[t2yind].X) / 2)
                return false;
            double leadRatio = (s.bbox.Bottom - s.pts[0].Y + 0.0) / s.bbox.Height;
            double aspect = (s.pts[b1yind].Y - s.pts[t1yind].Y) / (float)s.bbox.Width;
            double midhgt = (s.bbox.Bottom - s.pts[b2yind].Y) / (float)s.bbox.Height;
            if (mulike)
                allograph = "mu";
            else
            {
                if (s.curvatures[t1yind] < 0 || s.curvatures[t2yind] < 0) // m's have no intersecting loops at the top
                    return false;
                bool lower = aspect < 0.75 || (hasDown && aspect < 1 && midhgt < 0.33 && (leadRatio > 0.9 || (leadRatio > 0.5 &&
                angle(s.pts[t1yind], s.pts[0], V2D.Sub(s.pts[t1yind], s.pts[b1yind])) > 25 &&
                    (angle(s.pts[0], s.pts[b1yind], V2D.Sub(s.pts[t1yind], s.pts[b1yind])) > 15 ||
                     V2D.Straightness(s.pts, b1yind, t1yind) > 0.06 ||
                     V2D.Straightness(s.pts, t1yind, b2yind) > 0.06))));
                if (!hasDown && Math.Abs(s.cusps[1].curvature) > 1 && Math.Abs(s.cusps[s.nl].curvature) > 1 &&
                    Math.Abs(s.cusps[2].curvature) < Math.Min(Math.Abs(s.cusps[1].curvature), Math.Abs(s.cusps[s.nl].curvature)) / 2)
                    allograph = "um";
                else allograph = lower ? "m" + (hasDown ? "" : "n") : "mM";
            }

            return true;
        }

        public bool match_Sigma(CuspSet s)
        {
            if (s.cusps.Length < 5 || s.bbox.Height / (float)s.bbox.Width > 3)
                return false;
            bool hasLeft = (s.cusps[1].pt.X > s.cusps[0].pt.X) || (angle(s.cusps[1].pt, s.cusps[0].pt, new PointF(0, 1)) < 60 && (s.cusps[1].pt.Y - s.bbox.Top + 0.0) / s.bbox.Height < 0.3);
            int start = hasLeft ? s.cusps[1].index : Math.Min(6, s.pts.Length / 8);
            int midind = (s.pts.Length - start) / 2 + start;
            while (s.distances[midind] / s.dist > 0.6)
                midind = (int)(.95 * midind);
            int l1xind, l1x = minx(start, midind, s.pts, out l1xind);
            if (l1xind == -1)
                return false;
            int l2xind, l2x = minx(midind, s.pts.Length, s.pts, out l2xind);
            if (l2xind == -1)
                return false;
            int r1xind = start;
            if (r1xind == -1)
                return false;
            int r2xind, r2x = maxx(l1xind, l2xind, s.pts, out r2xind);
            if (r2xind == -1)
                return false;
            if ((s.pts[0].X - r2x + 0.0) / s.bbox.Width > 0.66)
                return false;
            int r3xind = s.pts.Length - 1;
            if (s.pts[r1xind].Y > s.pts[r2xind].Y || s.pts[r2xind].Y > s.pts[r3xind].Y) // right cusps stack vertically
                return false;
            if (s.pts[l2xind].X > s.pts[r2xind].X || s.pts[l2xind].X > s.pts[r3xind].X)
                return false;
            if (s.pts[l1xind].Y > s.pts[l2xind].Y) // left cusps stack vertically
                return false;
            if (s.pts[r1xind].Y > s.pts[r2xind].Y || s.pts[r2xind].Y > s.pts[r3xind].Y)
                return false;
            if (r3xind - l2xind < 2)
                return false;
            if (hasLeft && V2D.Straightness(s.pts, 0, r1xind) > 0.15 && s.distances[r1xind] / s.dist > 0.05)
                return false;
            int endCuspInd;
            bool left;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[r3xind], s.pts[l2xind])), out left, out endCuspInd, l2xind, r3xind);
            if (r3xind > endCuspInd && V2D.Straightness(s.pts, l2xind, r3xind) > 0.25)
            {
                if (left || !hasLeft || (angle(s.last, s.pts[endCuspInd], new PointF(0, -1)) > 60 && (s.dist - s.distances[endCuspInd]) / s.bbox.Height > 0.1))
                    return false;
                if (V2D.Straightness(s.pts, endCuspInd, s.pts.Length - 1) > 0.3 || V2D.Dist(s.last, s.pts[endCuspInd]) / s.bbox.Height > 0.5)
                    return false;
                r3xind = endCuspInd;
            }
            if (V2D.Det(s.pts[r3xind], s.pts[l2xind], s.pts[r2xind]) > 0)
                return false;
            if (V2D.Straightness(s.pts, r1xind, l1xind) > 0.2 ||
                V2D.Straightness(s.pts, l1xind, r2xind) > 0.25 ||
                V2D.Straightness(s.pts, r2xind, l2xind) > 0.25 ||
                V2D.Straightness(s.pts, l2xind, r3xind) > 0.25 ||
                (V2D.Straightness(s.pts, r2xind, l2xind) + V2D.Straightness(s.pts, l2xind, r3xind) > 0.35 &&
                 V2D.Straightness(s.pts, r1xind, l1xind) > 0.06))
                return false;
            double leadRatio = (s.bbox.Right - s.pts[0].X + 0.0) / s.bbox.Width;
            if (leadRatio > 0.6)
                return false;
            return true;
        }
        bool match_sqrt(CuspSet s)
        {
            if (s.cusps.Length < 3 || !s.cusps[0].left || !s.cusps[s.l].right || s.bbox.Height < InkPixel * 5)
                return false;
            int botind; maxy(0, s.pts.Length, s.pts, out botind);
            if (botind < s.cusps[1].index && s.cusps[1].pt.Y < s.cusps[0].pt.Y)
            {
                if (V2D.Straightness(s.pts, 0, s.cusps[1].index) < 0.15 && V2D.Straightness(s.pts, s.cusps[1].index, s.pts.Length) < 0.2 &&
                    angle(s.cusps[1].pt, s.pts[0], new PointF(0, -1)) < 60 && angle(s.last, s.cusps[1].pt, new PointF(1, 0)) < 45 &&
                    (s.bbox.Bottom - s.cusps[1].pt.Y + 0.0) / InkPixel > 15)
                    return true;
            }
            else
            {
                if (botind > s.cusps[2].index + 4 && V2D.Straightness(s.pts, s.cusps[2].index, botind) > 0.15)
                    return false;
                if (Math.Abs(botind - s.cusps[1].index) < Math.Min(4, s.pts.Length / 10))
                    botind = s.cusps[1].index;
                bool left; int leftind;
                double md = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[botind], s.pts[0])), out left, out leftind, 0, botind);
                int cornerInd;
                V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.pts[botind])), out left, out cornerInd, botind, s.pts.Length);
                double tailratio = (s.last.X - s.pts[cornerInd].X + 0.0) / (s.pts[cornerInd].X - s.pts[0].X);
                if ((Math.Abs(s.curvatures[botind]) > 2 ||
                    (Math.Sign(s.curvatures[botind]) == -1 && Math.Abs(s.curvatures[botind]) > 0.3 - Math.Max(0, (tailratio - 2)) * .1 &&
                     (s.intersects.Length == 0 || s.intersects[0] > botind)) ||
                    (Math.Sign(s.curvatures[botind]) == 1 && Math.Abs(s.curvatures[botind]) > 0.3 && s.intersects.Length > 0)))
                {
                    int rightCornerInd;
                    V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.pts[cornerInd])), out left, out rightCornerInd, cornerInd, s.pts.Length);
                    if ((Math.Sign(s.curvatures[cornerInd]) == 1 && Math.Abs(s.curvatures[cornerInd]) > 0) ||
                        (s.curvatures[cornerInd] < 0 && s.intersects.Length > 1 && s.intersects[0] < cornerInd && s.intersects[1] > cornerInd) &&
                   (s.pts[cornerInd].Y - s.bbox.Top + 0.0) / s.bbox.Height < 0.5 && (s.pts[botind].X - s.bbox.Left + 0.0) / s.bbox.Width < 0.4)
                    {
                        if (rightCornerInd != s.pts.Length - 1)
                        {
                            if (V2D.Straightness(s.pts, rightCornerInd, s.pts.Length) > 0.2 && (s.dist - s.distances[rightCornerInd]) / s.bbox.Width > .2)
                                return false;
                            if (angle(s.pts[rightCornerInd], s.last, new PointF(0, -1)) > 40)
                                rightCornerInd = s.pts.Length - 1;
                        }
                        if (s.pts[0].X > s.pts[cornerInd].X)
                            return false;
                        if (V2D.Straightness(s.pts, cornerInd, rightCornerInd) > 0.25)
                            return false;
                        double ang = angle(s.pts[rightCornerInd], s.pts[cornerInd], new PointF(1, 0));
                        if (ang > 25)
                            return false;
                        if (s.cusps.Length > 4 && (s.pts[rightCornerInd].Y - s.bbox.Top + 0.0) / s.bbox.Height > 0.5) // radical shouldn't wiggle down
                            return false;
                        double tailLength = V2D.Dist(s.pts[cornerInd], s.last) / s.bbox.Height;
                        if (tailLength > 1.25 ||
                        (V2D.Straightness(s.pts, cornerInd, rightCornerInd) < 0.13 + Math.Min(0.1, tailLength - 0.45) && tailLength > 0.45))
                            return true;
                    }

                }
            }
            return false;
        }
        public bool match_3(CuspSet s)
        {
            if (s.cusps.Length < 9)
            {
                int midcusp = -1;
                double midcurve = double.MaxValue;
                for (int i = 1; i < s.cusps.Length - 1; i++)
                {
                    if (s.cusps[i].dist < s.dist / 10)
                        continue;
                    int sign = 1;
                    for (int inter = 0; inter < s.intersects.Length / 2; inter++)
                        if (s.intersects[inter * 2] < s.cusps[i].index && s.intersects[inter * 2 + 1] > s.cusps[i].index)
                        {
                            sign = -1;
                            break;
                        }
                    if ((sign * s.cusps[i].curvature < 0 || Math.Abs(s.cusps[i].curvature) > 3.1) && (i == 1 || sign * s.cusps[i].curvature < midcurve))
                    {
                        midcurve = sign * s.cusps[i].curvature;
                        midcusp = i;
                    }
                    else if (midcusp != -1 && sign * s.cusps[i].curvature > 0)
                        break;
                }
                if (midcusp < 1)
                    return false;
                int midindex = s.cusps[midcusp].index;
                for (int i = 0; i < midcusp; i++)
                    if (s.cusps[i].bot)
                        return false;
                int midx = s.cusps[midcusp].pt.X;
                int firstlobeind, secondlobeind;
                int firstlobex = maxx(s.cusps[0].index, midindex, s.pts, out firstlobeind);
                int secondlobex = maxx(midindex, s.cusps[s.l].index, s.pts, out secondlobeind);
                if (firstlobeind == midindex || secondlobeind == midindex ||
                    angle(s.pts[firstlobeind], s.pts[midindex], V2D.Sub(s.pts[secondlobeind], s.pts[midindex])) > 150)
                    return false;
                double ang = angle(s.pts[secondlobeind], s.pts[firstlobeind], new PointF(0, 1));
                if (ang > 70)
                    return false;
                if ((firstlobex - s.cusps[0].pt.X + 0.0) / s.bbox.Width < 0.1 ||
                    (secondlobex - s.cusps[s.l].pt.X + 0.0) / s.bbox.Width < 0.1)
                    return false;
                for (int i = 1; i < s.cusps.Length - 1; i++)
                {
                    if (i > midcusp && s.intersects.Length > 1 && Math.Abs(s.cusps[i].curvature) > 1)
                        for (int inter = 0; inter < s.intersects.Length / 2; inter++)
                            if (s.intersects[inter * 2] < s.cusps[i].index && s.intersects[inter * 2 + 1] > s.cusps[i].index)
                                return false;
                }
                int mi_x = int.MaxValue;
                int ma_x = s.cusps[midcusp].pt.X;
                bool gotOne = false;
                bool gotNeg = false;
                for (int a = s.cusps[1].index; a < s.cusps[midcusp].index * .9; a++)
                {
                    if (!gotNeg && s.curvatures[a] < 0 && (s.intersects.Length == 0 || s.intersects[0] > a + 1 || s.intersects[1] < a - 1))
                    {
                        gotNeg = s.curvatures[a - 1] < 0 && s.curvatures[a - 2] < 0;
                    }
                    if (!gotNeg)
                        continue;
                    if ((s.bbox.Bottom - s.pts[a].Y + 0.0) / s.bbox.Height < 0.1)
                        break;
                    if (s.curvatures[a - 1] > 0 && s.curvatures[a] > 0.125)
                        return false;
                }
                mi_x = int.MaxValue;
                ma_x = s.cusps[midcusp].pt.X;
                gotOne = false;
                int mixind = -1;
                int maxind = -1;
                bool gotPos = false;
                double ncurve = 0;
                double maxPos = 0;
                for (int a = s.cusps[midcusp].index + 1; a < s.pts.Length - 1; a++)
                {
                    if (!gotPos && s.curvatures[a] > 0)
                    {
                        gotPos = s.curvatures[a - 1] > 0 && s.curvatures[a - 2] > 0;
                        if (s.curvatures[a] > maxPos)
                            maxPos = s.curvatures[a];
                    }
                    if (!gotPos)
                        continue;
                    if ((s.bbox.Bottom - s.pts[a].Y + 0.0) / s.bbox.Height < 0.1)
                        break;
                    if (s.curvatures[a] < 0)
                        ncurve += s.curvatures[a];
                    else ncurve = 0;
                    if (mi_x > s.pts[a].X && s.curvatures[a - 1] < 0 && ncurve < -.1)
                    {
                        gotOne = true;
                        mi_x = s.pts[a].X;
                        mixind = a;
                    }
                    if (ma_x < s.pts[a].X)
                    {
                        ma_x = s.pts[a].X;
                        maxind = a;
                    }
                    if (gotOne && (ma_x - mi_x + 0.0) / s.bbox.Width > 0.05)
                        if (V2D.Straightness(s.pts, mixind, s.pts.Length) > .25 || angle(s.last, s.pts[mixind], new PointF(1, 0)) < 90)
                            return false;
                        else break;
                    if (gotOne && maxPos > Math.Abs(s.cusps[midcusp].curvature))
                        return false;
                }
                if (firstlobeind == midindex || secondlobeind == midindex ||
                    (angle(s.pts[midindex], s.pts[secondlobeind], V2D.Sub(s.pts[midindex], s.last)) < 10 &&
                     (s.dist - s.distances[secondlobeind]) / s.bbox.Width < 0.5))
                    return false;
                int boty = maxy(0, midindex - 5, s.pts);
                if (s.cusps[0].pt.Y < s.pts[midindex].Y &&
                    s.pts[midindex].Y < s.cusps[s.l].pt.Y &&
                    firstlobex > s.cusps[0].pt.X &&
                    secondlobex > s.cusps[s.l].pt.X &&
                    midx < firstlobex &&
                    midx < secondlobex)
                    return true;
            }
            return false;
        }
        bool match_beta(CuspSet s, ref int baseline, ref int midpt, ref string allograph)
        {
            if (s.cusps[0].right || !s.cusps[1].top || !s.cusps[0].bot)
                return false;
            if (V2D.Straightness(s.pts, 0, s.cusps[1].index) > 0.35)
                return false;

            int midcusp = 2;
            for (int i = 2; i < s.cusps.Length - 1; i++)
            {
                if ((s.cusps[i].curvature < 0 && s.cusps[i].curvature < s.cusps[midcusp].curvature) ||
                     (s.cusps[midcusp].curvature > 0 && s.cusps[i].curvature > s.cusps[midcusp].curvature))
                    midcusp = i;
            }
            if (midcusp >= s.cusps.Length)
                return false;
            int midindex = s.cusps[midcusp].index;
            int midx = s.cusps[midcusp].pt.X;
            int firstlobeind, secondlobeind;
            int firstlobex = maxx(s.cusps[1].index, midindex, s.pts, out firstlobeind);
            int secondlobex = maxx(midindex, s.cusps[s.l].index, s.pts, out secondlobeind);
            if (firstlobeind == -1 || secondlobeind == -1 || s.curvatures[firstlobeind] < 0 || s.curvatures[secondlobeind] < 0)
                return false;
            if (secondlobeind == -1 || firstlobeind == midindex || secondlobeind == midindex ||
                angle(s.pts[firstlobeind], s.pts[midindex], V2D.Sub(s.pts[secondlobeind], s.pts[midindex])) > 160)
                return false;
            double ang = angle(s.pts[secondlobeind], s.pts[firstlobeind], new PointF(0, 1));
            if (ang > 60)
                return false;
            if ((secondlobex - s.cusps[s.l].pt.X + 0.0) / s.bbox.Width < 0.1 ||
                s.cusps[0].right || s.cusps[s.l].right)
                return false;
            for (int c = 1; c < s.cusps.Length - 1; c++)
                if (s.cusps[c].curvature < -Math.Abs(s.curvatures[midindex]) && s.cusps[c].index > midindex * 1.1)
                    return false;
            if (firstlobeind == midindex || secondlobeind == midindex)
                return false;
            int boty = maxy(s.cusps[1].index, midindex - 5, s.pts);
            if (boty < s.pts[midindex].Y &&
                s.cusps[1].pt.Y < s.pts[midindex].Y &&
                s.pts[midindex].Y < s.cusps[s.l].pt.Y &&
                firstlobex >= s.cusps[1].pt.X &&
                secondlobex > s.cusps[s.l].pt.X &&
                midx < firstlobex &&
                midx < secondlobex &&
                !s.cusps[s.l].right)
            {
                if ((s.pts[0].Y - maxy(s.cusps[1].index, s.pts.Length, s.pts) + 0.0) / s.bbox.Height > 0.1)
                {
                    midpt = s.cusps[midcusp].pt.Y;
                    baseline = s.cusps[s.l].pt.Y;
                    allograph = "beta";
                    return true;
                }
                else
                    allograph = "B";
            }
            return false;
        }
        public bool match_p(CuspSet s, ref string allograph)
        {
            if (s.bbox.Width * 1.0 / s.bbox.Height > 1)
                return false;
            int botcusp = 0;
            for (int c = 0; c < s.cusps.Length; c++)
                if (s.cusps[c].pt.Y > s.cusps[botcusp].pt.Y)
                    botcusp = c;
            if (botcusp > 0 && V2D.Straightness(s.pts, 0, s.cusps[botcusp].index) > 0.14)
            {
                if (botcusp == 1)
                    return false;
                PointF init = V2D.Sub(s.cusps[1].pt, s.cusps[0].pt);
                if (init.Y < 0)
                {
                    if (V2D.Length(init) > V2D.Dist(s.cusps[botcusp].pt, s.cusps[1].pt))
                        return false;
                }
                else return false;
                if (Math.Abs(s.avgCurve(botcusp - 1, botcusp)) > 0.06)
                    return false;
            }
            if (!s.cusps[botcusp].bot)
                return false;
            int lobetopind;
            double lobetop = miny(s.cusps[botcusp].index, s.cusps[s.l].index, s.pts, out lobetopind);
            if ((lobetop - s.bbox.Top + 0.0) / s.bbox.Height > 0.15)
                return false;
            for (int c = botcusp + 1; c < s.l; c++)
                if (s.cusps[c].index < lobetopind && s.cusps[c].curvature < 0 && (s.curvatures[s.cusps[c].index - 1] < 0 || s.curvatures[s.cusps[c].index + 1] < 0))
                    return false;
            int lobebotind, lobeleftind;
            // Bottom lobe of P is maxy OR first Intersection or first negative curvature
            double lobebot = maxy(lobetopind, s.cusps[s.l].index, s.pts, out lobebotind);
            double lobeang = angle(s.pts[lobebotind], s.pts[lobetopind], new Point(-1, 1));
            bool left;
            double straight = V2D.Straightness(s.pts, lobetopind, lobebotind, out left);
            if (left || straight < 0.075)//|| (straight < 0.15 && lobeang > 35))
                return false;
            int firstEvent = s.pts.Length - 1;
            for (int i = 0; i < s.intersects.Length; i++)
                if (s.intersects[i] > lobetopind && s.intersects[i] < lobebotind)
                {
                    lobebotind = s.intersects[i];
                    firstEvent = s.intersects[i];
                    break;
                }
            for (int cs = 0; cs < s.cusps.Length; cs++)
                if (s.cusps[cs].index > lobetopind && s.cusps[cs].index < lobebotind && s.cusps[cs].curvature < 0)
                {
                    lobebotind = s.cusps[cs].index;
                    firstEvent = s.cusps[cs].index;
                    break;
                }
            if (lobebotind == -1)
                return false;
            int loberightind, loberight = maxx(lobetopind, lobebotind, s.pts, out loberightind);
            if (loberightind == -1)
                return false;
            if (s.angles[loberightind] < -Math.PI * 3 / 4 || s.angles[loberightind] > 0)
                return false;
            // leftmost lobe is minX OR first Intersect or first negative curvature;
            double lobeleft = minx(lobebotind, s.pts.Length, s.pts, out lobeleftind);
            lobeleftind = Math.Min(lobeleftind, firstEvent);

            // if the curve from leftmostind to the real leftmost pt is straight, update the leftmost ind
            int realleftind;
            minx(lobeleftind, s.pts.Length, s.pts, out realleftind);
            if (realleftind > lobeleftind && (s.distances[realleftind] - s.distances[lobeleftind]) / V2D.Dist(s.pts[realleftind], s.pts[lobeleftind]) > 1.3)
                return false;
            lobeleftind = realleftind;

            // now see how close we came to closing the loop of the P
            bool foundHgtMatch = false;
            for (int i = s.cusps[botcusp].index + 1; i < lobetopind; i++)
                if (s.pts[i].Y < s.pts[lobeleftind].Y && s.pts[i - 1].Y > s.pts[lobeleftind].Y)
                {
                    if ((s.pts[lobeleftind].X - s.pts[i].X + 0.0) / (s.bbox.Right - s.pts[i].X) > 0.6 && Math.Abs(s.angles[s.angles.Length - 3]) > 0.6)
                        return false;
                    foundHgtMatch = true;
                    break;
                }
            if (!foundHgtMatch)
                return false;

            // now check for any tail coming off the end of the P
            if (lobeleftind < s.pts.Length - 1)
            {
                if (V2D.Dist(s.pts[lobeleftind], s.last) / (s.dist - s.distances[lobeleftind]) < 0.7)
                    return false;
                if ((s.dist - s.distances[lobeleftind]) / s.bbox.Width > 0.15 && (
                    (s.dist - s.distances[lobeleftind]) / V2D.Dist(s.last, s.pts[lobeleftind]) > 1.4 ||
                    (s.last.X > s.pts[lobeleftind].X && angle(s.last, s.pts[lobeleftind], new Point(0, 1)) < 70)))
                    return false;
            }
            int ict = 0;
            for (int i = 0; i < s.intersects.Length; i++)
                if (s.intersects[i] > lobetopind && s.intersects[i] < lobebotind)
                    ict++;
            if (ict > 1)
                return false;

            if ((s.cusps[botcusp].pt.Y - lobebot) / s.bbox.Height < .2)
                return false;
            double avgc = s.avgCurveSeg(s.cusps[botcusp].index, lobeleftind - 3);
            if (Math.Sign(avgc) != 1 || Math.Abs(avgc) < 0.01)
                return false;

            allograph = botcusp > 0 ? "p" : "P";
            return true;
        }
        bool match_s(CuspSet s, ref string allograph)
        {
            double aspect = (s.bbox.Width / (0.0 + s.bbox.Height));
            if (aspect < 0.25)
                return false;
            if (s.intersects.Length > 0 && s.intersects[0] > 3 && s.intersects[0] < s.pts.Length - 4)
                return false;
            if (s.cusps.Length > 7 || s.cusps[0].left) //|| s.cusps[s.l].right)
                return false;
            int min_ind, max_ind;
            int mi_x = minx(0, s.pts.Length / 2, s.pts, out min_ind);
            if (min_ind < 3)
                return false;
            double inAng = angle(s.pts[min_ind], s.pts[0], new PointF(-1, 0));
            if (inAng > 65)
                return false;
            int ma_x = maxx(s.pts.Length / 2, s.pts.Length, s.pts, out max_ind);
            if (s.pts.Length == max_ind)
                return false;
            if (!s.cusps[0].right && (s.pts[(max_ind - min_ind) / 2 + min_ind].Y - s.pts[min_ind / 2].Y + 0.0) / s.bbox.Height < 0.15)
                return false;
            bool gotPos = false;
            for (int i = 1; i < s.cusps.Length - 1; i++)
            {
                if (s.cusps[i].index < min_ind)
                    continue;
                if (s.cusps[i].curvature > 0.1)
                    gotPos = true;
                else if (s.cusps[i].curvature < -0.1 && gotPos)
                    return false;
                double cuspang = angle(s.cusps[i - 1].pt, s.cusps[i].pt, V2D.Sub(s.pts[s.cusps[i].index + Math.Min(10, (s.cusps[i + 1].index - s.cusps[i].index) / 3)], s.cusps[i].pt));
                double upang = angle(s.cusps[i - 1].pt, s.cusps[i].pt, new PointF(0, 1));
                if (s.cusps[i].index <= max_ind + 2 && (s.cusps[i].curvature > .7 || cuspang < 50 || (upang < 80 && cuspang < 90)) && (s.angles[s.cusps[i].index - 3] > 0 || s.pts[s.cusps[i].index - 3].Y > s.cusps[i].pt.Y))
                    return false;
            }
            double ang = angle(s.last, s.pts[max_ind], new PointF(-1, 0));
            if (Math.Abs(ang) > 70)
                return false;
            double angstem = angle(s.pts[min_ind], s.pts[max_ind], new PointF(0, -1));
            if (Math.Abs(angstem) < 14 / (aspect > 1 ? 2 * aspect : (aspect < 0.4 ? .75 : 1.0)))
                return false;
            if (V2D.Dist(s.pts[max_ind], s.last) / s.bbox.Width < 0.25)
                return false;
            if (s.cusps[0].pt.X < s.pts[min_ind].X ||
                s.pts[min_ind].X > s.pts[max_ind].X ||
                s.pts[max_ind].X < s.cusps[s.l].pt.X ||
                s.cusps[0].pt.Y > s.pts[max_ind].Y ||
                Math.Sign(s.curvatures[min_ind]) == 1 ||
                Math.Sign(s.curvatures[max_ind]) == -1 ||
                s.pts[min_ind].Y > s.pts[max_ind].Y ||
                s.pts[min_ind].Y > s.cusps[s.l].pt.Y)
                return false;
            if (aspect < 0.48 && angstem < 20)
                allograph = "INTtop s";
            else allograph = "s";
            return true;
        }
        bool match_tint(CuspSet s)
        {
            if (!s.cusps[0].top || s.cusps[s.l].top)// || s.cusps[0].left || s.cusps[s.l].right)
                return false;
            int endTop = 0;
            for (int i = 5; i <= 3 * s.pts.Length / 4; i++)
                if (s.curvatures[i - 2] > 0 && (s.curvatures[i - 1] + s.curvatures[i - 2] + s.curvatures[i - 3]) > 0)
                {
                    endTop = i - 2;
                    break;
                }
            if (endTop > s.pts.Length / 10 && V2D.Straightness(s.pts, 0, endTop) < 0.075)
                return false;
            int rightind, leftind, topind, botind;
            int mi_x = minx(0, s.pts.Length / 2, s.pts, out leftind);
            int top = miny(0, s.pts.Length / 2, s.pts, out topind);
            int bot = maxy(s.pts.Length / 2, s.pts.Length, s.pts, out botind);
            int ma_x = maxx(s.pts.Length / 2, s.pts.Length, s.pts, out rightind);
            if (Math.Abs(s.angles[botind - 2]) > Math.PI / 2)
                return false;
            if ((s.bbox.Width + 0.0) / s.bbox.Height > 0.75)
                return false;
            if ((leftind < topind && (s.intersects.Length < 2 || s.intersects[0] > topind || s.intersects[1] < topind)) || rightind > botind)
                return false;
            double ang = angle(s.pts[rightind], s.pts[leftind], new PointF(0, 1));
            if (ang > 20)
                return false;
            return true;
        }
        bool is90(double ang)
        {
            while (ang > Math.PI / 2)
                ang = ang - Math.PI / 2;
            if (ang > Math.PI / 4)
                ang = Math.PI / 2 - ang;
            return ang < Math.PI / 8;
        }
        public bool match_rect(CuspSet s)
        {
            if (s.cusps.Length == 5)
            {
                Point v1 = V2D.Sub(s.cusps[1].pt, s.cusps[0].pt);
                Point v2 = V2D.Sub(s.cusps[2].pt, s.cusps[1].pt);
                Point v3 = V2D.Sub(s.cusps[3].pt, s.cusps[2].pt);
                Point v4 = V2D.Sub(s.cusps[4].pt, s.cusps[3].pt);
                double ang1x = Math.Acos(V2D.Normalize(v1).X);
                double ang2x = Math.Acos(V2D.Normalize(v2).X);
                double ang3x = Math.Acos(V2D.Normalize(v3).X);
                double ang4x = Math.Acos(V2D.Normalize(v4).X);
                if (Math.Abs(V2D.Angle(v1, v2)) > 2 || Math.Abs(V2D.Angle(v2, v3)) > 2 || Math.Abs(V2D.Angle(v3, v4)) > 2)
                    return false;
                if (!is90(ang1x) || !is90(ang2x) || !is90(ang3x) || !is90(ang4x))
                    return false;
                return true;
            }
            return false;
        }
        #region bcz pseudocode
        //      I would pseudo code arrow detection as:

        //                              side of arrowhead
        //                               \ \
        //              stem              \ \
        //  start -------------------------  \ retraced tip
        //                              Tip /
        //                                 /
        //                                /other side of arrowhead

        //find the end of the arrow:
        //    the farthest cusp from the starting point such that
        //    no intermediate cusp creates an acute angle with its neighbors

        //    if the the line from the start to this distant cusp isn't "smooth"
        //    then it's not a match.

        //find the retraced arrow tip:
        //    starting at the arrow tip, find the next cusp that is nearest the
        //    arrow tip.  probably can assume it's at least one cusp away and
        //    isn't the end of the stroke (otherwise it's no match)

        //find one side of the arrowhead:
        //    from the arrow tip to the retraced arrow tip, find the most
        //    distant cusp.

        //    make sure that vectors are relatively straight from the
        //        arrow tip to this distant cusp
        //        and the distant cusp to the retraced arrow tip
        //    make sure the angle between the arrow tip, distant cusp, and
        //        retraced arrow tip is relatively small.
        //    (otherwise it's no match)

        // find the other side of the arrowhead:
        //    this should just be the vector from the retraced arrow tip to the
        //       end of the stroke.

        //    make sure the vector from the retraced arrow tip to the end of the
        //      stroke is relatively straight and forms an acute angle with
        //      respect to the arrow stem.
        //    (otherwise it's no match)

        //    Also, make sure that the end of the stroke is on the opposite side
        //       of the arrow stem as the other side of the arrowhead.
        //       NOTE: this test might need to be somewhat precise since when
        //             I'm scribbling arrowheads, the distinction between the
        //             left and right arrowheads becomes subtle.


        #endregion
        public bool match_arrow(CuspSet s, ref string allograph)
        {
            Point start;
            Point stem_tip = Point.Empty;
            Point arrow_tip = Point.Empty;
            Point[] stem = null;
            double headang = 0;
            if (!match_arrow(s, out start, ref stem_tip, ref arrow_tip, ref headang, ref stem)) return false;

            Point dir = V2D.Sub(stem_tip, start);
            double angle = Math.Atan2(-dir.Y, dir.X); /* minus Y because Y points down on screen */
            angle /= Math.PI / 8; /* pieces of eight */
            if (angle < -7) allograph = "larrow-1";
            else if (angle < -5) allograph = "dlarrow-1";
            else if (angle < -3) allograph = "darrow-1";
            else if (angle < -1) allograph = "drarrow-1";
            else if (angle < 1) allograph = "rarrow-1";
            else if (angle < 3) allograph = "urarrow-1";
            else if (angle < 5) allograph = "uarrow-1";
            else if (angle < 7) allograph = "ularrow-1";
            else allograph = "larrow-1";
            return true;
        }
        public bool match_arrow(CuspSet s, out Point start, ref Point stem_tip, ref Point arrow_tip, ref double angle, ref Point[] stem)
        {
            int startCusp = 0;
            if (FeaturePointDetector.angle(s.cusps[0].pt, s.cusps[1].pt, V2D.Sub(s.last, s.cusps[1].pt)) < 30 &&
                s.cusps[1].dist / s.dist < 0.1)
                startCusp = 1;
            start = s.cusps[startCusp].pt;
            int endCusp = -1;
            if (s.cusps.Length < 5)
                return false;

            for (int i = startCusp + 1; i < s.cusps.Length - 3; i++)
                if (FeaturePointDetector.angle(s.pts[(int)(s.cusps[i].index * .7)], s.cusps[i].pt, V2D.Sub(s.cusps[i + 1].pt, s.cusps[i].pt)) < 90)
                {
                    endCusp = i;
                    break;
                }
            if (endCusp == -1)
                return false;
            Point stemPt = s.pts[(int)(s.cusps[endCusp].index * .7)];
            int leftCusp = endCusp + 1;
            for (int i = endCusp + 2; i < s.cusps.Length - 2; i++)
                if (V2D.Dist(s.cusps[i].pt, s.cusps[endCusp].pt) > V2D.Dist(s.cusps[leftCusp].pt, s.cusps[endCusp].pt))
                    leftCusp = i;
                else break;
            if (FeaturePointDetector.angle(s.cusps[leftCusp].pt, s.cusps[leftCusp + 1].pt, V2D.Sub(s.cusps[leftCusp].pt, s.cusps[endCusp].pt)) > 30)
                return false;
            int tipCusp = leftCusp + 1;
            for (int i = leftCusp + 2; i < s.cusps.Length - 1; i++)
                if (V2D.Dist(s.cusps[i].pt, s.cusps[endCusp].pt) < V2D.Dist(s.cusps[tipCusp].pt, s.cusps[endCusp].pt))
                    tipCusp = i;
                else break;
            int rightCusp = s.l;

            if (s.cusps[endCusp].dist < (s.cusps[leftCusp].dist - s.cusps[endCusp].dist) ||
                s.cusps[endCusp].dist < (s.cusps[rightCusp].dist - s.cusps[tipCusp].dist) ||
                s.cusps[endCusp].dist < (s.cusps[tipCusp].dist - s.cusps[leftCusp].dist))
                return false;


            if (Math.Sign(V2D.Det(V2D.Sub(s.cusps[leftCusp].pt, s.cusps[endCusp].pt), V2D.Sub(stemPt, s.cusps[endCusp].pt))) ==
                Math.Sign(V2D.Det(V2D.Sub(s.cusps[rightCusp].pt, s.cusps[endCusp].pt), V2D.Sub(stemPt, s.cusps[endCusp].pt))))
                return false;
            if (Math.Sign(V2D.Det(V2D.Sub(s.cusps[leftCusp].pt, s.cusps[rightCusp].pt), V2D.Sub(s.cusps[leftCusp].pt, s.cusps[endCusp].pt))) !=
                Math.Sign(V2D.Det(V2D.Sub(s.cusps[leftCusp].pt, s.cusps[endCusp].pt), V2D.Sub(s.cusps[rightCusp].pt, s.cusps[tipCusp].pt))))
                return false;
            if (FeaturePointDetector.angle(s.cusps[leftCusp].pt, s.cusps[tipCusp].pt, V2D.Sub(s.cusps[rightCusp].pt, s.cusps[tipCusp].pt)) < 10)
                return false;
            if (V2D.Dot(V2D.Sub(s.cusps[leftCusp].pt, s.cusps[startCusp].pt), V2D.Sub(s.cusps[endCusp].pt, s.cusps[startCusp].pt)) < 0)
                return false;
            if (V2D.Dot(V2D.Sub(s.cusps[leftCusp].pt, s.cusps[tipCusp].pt), V2D.Sub(s.cusps[rightCusp].pt, s.cusps[tipCusp].pt)) < 0)
                return false;
            if (V2D.Straightness(s.pts, s.cusps[endCusp].index, s.cusps[leftCusp].index) > 0.25)
                return false;
            if (V2D.Straightness(s.pts, s.cusps[leftCusp].index, s.cusps[tipCusp].index) > 0.2)
                return false;
            if (V2D.Straightness(s.pts, s.cusps[tipCusp].index, s.cusps[rightCusp].index) > 0.25)
                return false;
            Ink tmpink = new Ink();
            stem = new Point[s.cusps[endCusp].index + 1 - s.cusps[startCusp].index];
            for (int i = s.cusps[startCusp].index; i <= s.cusps[endCusp].index; i++)
                stem[i - s.cusps[startCusp].index] = s.pts[i];
            Stroke tmpstem = tmpink.CreateStroke(stem);
            float nearest = tmpstem.NearestPoint(s.cusps[leftCusp].pt);
            stem = new Point[(int)nearest + 1 - s.cusps[startCusp].index];
            for (int i = s.cusps[startCusp].index; i <= nearest; i++)
                stem[i - s.cusps[startCusp].index] = s.pts[i];
            arrow_tip = s.cusps[tipCusp].pt;
            stem_tip = s.cusps[endCusp].pt;
            angle = FeaturePointDetector.angle(s.cusps[leftCusp].pt, s.cusps[endCusp].pt, V2D.Sub(s.cusps[rightCusp].pt, s.cusps[tipCusp].pt));
            return true;
        }
        bool match_bint(CuspSet s, ref string allograph)
        {
            if (s.cusps.Length > 5)
                return false;
            if (s.cusps[s.l].bot || s.cusps[0].top || !s.cusps[s.l].right || !s.cusps[0].left)
                return false;
            int rightind, leftind, topind, botind;
            int mi_x = minx(0, s.pts.Length / 2, s.pts, out leftind);
            int bot = maxy(0, s.pts.Length / 2, s.pts, out botind);
            int top = miny(s.pts.Length / 2, s.pts.Length, s.pts, out topind);
            if (top - s.bbox.Top > 0)
                return false;
            int ma_x = maxx(s.pts.Length / 2, s.pts.Length, s.pts, out rightind);
            int mid_start_x = s.pts[s.pts.Length / 3].X;
            int mid_end_x = s.pts[2 * s.pts.Length / 3].X;
            if (s.curvatures[botind] > 0)
                return false;
            if (V2D.Straightness(s.pts, 0, s.pts.Length / 3) < 0.09)
                return false;
            if ((mid_start_x - mi_x + 0.0) / s.bbox.Width < .1)
                return false;
            if ((ma_x - mid_end_x + 0.0) / s.bbox.Width < .15)
                return false;
            if ((s.bbox.Width + 0.0) / s.bbox.Height > 1)
                return false;
            if (leftind > topind || rightind < botind)
                return false;
            double ang = angle(s.pts[topind], s.pts[botind], new PointF(0, -1));
            if (ang > 40)
                return false;
            int cornerind;
            bool left;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.cusps[1].pt)), out left, out cornerind, s.cusps[1].index, s.pts.Length);
            if (V2D.Straightness(s.pts, cornerind, s.pts.Length) < 0.18)
                allograph = "sqrt";
            else allograph = angle(s.cusps[1].pt, s.pts[0], new Point(1, -1)) < 45 ? "r" : "INTbot";
            return true;
        }
        bool match_fbase(CuspSet s, int firstCusp)
        {
            if (s.cusps.Length > 5)
                return false;
            if (s.dist / s.bbox.Height > 2)
                return false;
            int flip = 1;
            if (s.cusps[firstCusp].pt.Y > s.last.Y)
            {
                flip = -1;
            }
            else
            {
                if (Math.Sign(s.avgCurveSeg(0, s.pts.Length / 2)) == 1)
                    return false;
                for (int i = firstCusp + 1; i < s.cusps.Length - 1; i++)
                    if (s.cusps[i].dist > s.dist / 2 && s.cusps[i].curvature < 0)
                        return false;
            }
            int topind, botind, hookind = flip == -1 ? s.pts.Length - 1 : s.cusps[firstCusp].index;
            int boty = maxy(s.cusps[firstCusp].index, s.pts.Length - 1, s.pts, out botind);
            int topy = miny(s.cusps[firstCusp].index, s.pts.Length - 1, s.pts, out topind);
            if (boty - topy < s.bbox.Width)
                return false;
            if (firstCusp > 0 && s.pts[0].Y < topy)
                return false;
            bool left;
            if (flip == 1)
            {
                Point[] reversed = (Point[])s.pts.Clone();
                for (int i = 0; i < reversed.Length / 2; i++)
                {
                    Point tmp = reversed[i];
                    reversed[i] = reversed[reversed.Length - 1 - i];
                    reversed[reversed.Length - 1 - i] = tmp;
                }
                if (V2D.MaxDist(reversed, new PointF(0, 1), out left, reversed.Length / 2, reversed.Length) > 0 && left)
                    return false;
            }
            else
                if (V2D.MaxDist(s.pts, new PointF(0, -1), out left, s.pts.Length / 2, s.pts.Length) > 0 && left)
                    return false;
            if (V2D.Dist(s.pts[hookind], s.pts[topind]) / s.bbox.Height > 0.85)
                return false;
            double tailcurve = s.avgCurveSeg(s.pts.Length / 2, s.pts.Length);
            if (Math.Sign(s.curvatures[topind]) == flip || (Math.Abs(tailcurve) > 0.35 && Math.Sign(tailcurve) == -flip))
                return false;
            if (Math.Abs(s.distances[topind] - (flip == -1 ? s.dist : 0)) / V2D.Dist(s.pts[topind], flip == -1 ? s.last : s.pts[0]) > 1.4)
                return false;
            return true;
        }
        bool match_fcusp(CuspSet s)
        {
            int firstCusp = 0;
            if (s.cusps.Length > 2 && Math.Abs(s.cusps[1].curvature) > 1 && (s.bbox.Bottom - s.cusps[1].pt.Y) < 100)
            {
                double vang = angle(s.cusps[1].pt, s.cusps[0].pt, new PointF(0, 1));
                if (vang < 20)
                    firstCusp = 1;
            }
            return match_fbase(s, firstCusp);
        }
        bool match_N(CuspSet s)
        {
            int start = 0;
            if (s.cusps.Length > 4)
            {
                if (angle(s.cusps[1].pt, s.pts[s.cusps[1].index / 4], V2D.Sub(s.cusps[1].pt, s.cusps[2].pt)) < 10)
                    start = 1;
                else return false;
            }
            if (V2D.Dist(s.cusps[start].pt, s.last) / (s.dist) > 0.98)
                return false;
            double ang = angle(s.last, s.cusps[start].pt, new Point(1, -2));
            double botcurve = s.cusps[start + 1].curvature;
            if (s.intersects.Length > 1 && s.intersects[0] < s.cusps[start + 1].index && s.intersects[1] > s.cusps[start + 1].index)
                botcurve = -botcurve;
            if (Math.Abs(ang) > 30 || Math.Sign(botcurve) == -1)
                return false;
            if (s.cusps.Length - start == 3)
            {
                double totCrvP = 0;
                double totCrvN = 0;
                int numP = s.cusps[start].index + 3, numN = 0;
                int numPts = s.pts.Length - s.cusps[start].index;
                for (; numP < s.pts.Length; numP++)
                    if (s.pts[numP].Y < s.pts[numP - 1].Y + 15)
                        totCrvP += s.curvatures[numP];
                    else break;
                for (int i = s.pts.Length - 3; i > numP; i--)
                    if (s.pts[i].Y < s.pts[i - 1].Y + 15)
                    {
                        numN++;
                        totCrvN += s.curvatures[i];
                    }
                    else break;
                if (numP < s.cusps[start].index + Math.Max(3, numPts * .25) || (numN + 3) < numPts * .20)
                    return false;
                if (totCrvP / numP < 0.04 || totCrvN / numN > -0.04)
                    return false;
            }
            if (s.cusps.Length - start == 4)
            {
                if (Math.Sign(botcurve) == Math.Sign(s.cusps[start + 2].curvature))
                    return false;
                if (s.cusps[start + 1].pt.Y > s.cusps[start + 2].pt.Y)
                    return false;
                for (int c = start + 2; c < s.cusps.Length - 1; c++)
                {
                    if ((c == start + 2 && s.cusps[c].pt.X < s.cusps[c - 1].pt.X) ||
                    (Math.Sign(s.cusps[c].curvature) == -1 && s.cusps[c].pt.Y < s.last.Y))
                        return false;
                }
            }
            bool left;
            double hgtRatio = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.cusps[start].pt, new Point(s.last.X, s.cusps[start].pt.Y))), out left) / s.bbox.Width;
            if (hgtRatio < 0.9)
                return false;
            return true;
        }
        bool match_tilde(CuspSet s)
        {
            if (s.intersects.Length > 0 && s.intersects[0] > s.pts.Length / 8 && s.intersects[0] < s.pts.Length * 7 / 8)
                return false;
            if (s.bbox.Height / InkPixel < 5)
                return false;
            if (s.straight < 0.15)
                return false;
            int b1 = 0;
            int t1;
            miny(0, s.pts.Length / 2, s.pts, out t1);
            if (t1 == -1)
                return false;
            int b2;
            maxy(t1, s.pts.Length, s.pts, out b2);
            if (b2 == -1)
                return false;
            int t2 = s.pts.Length - 1;
            if ((s.pts[b2].Y - s.pts[t2].Y + 0.0) / s.bbox.Height < 0.1)
                return false;
            if (V2D.Straightness(s.pts, b1, t1) > 0.25 ||
                V2D.Straightness(s.pts, t1, b2) > 0.3 ||
                V2D.Straightness(s.pts, b2, t2) > 0.25)
                return false;
            if (s.pts[t2].X < s.pts[b2].X)
                return false;
            if (s.pts[b2].X < s.pts[t1].X)
                return false;
            if (s.pts[t1].X < s.pts[b1].X)
                return false;
            if ((s.pts[b1].Y - s.pts[t1].Y + 0.0) / s.bbox.Height < 0.1)
                return false;
            double ang = angle(s.cusps[s.l].pt, s.cusps[0].pt, new Point(2, -1));
            if (Math.Abs(ang) > 30)
                return false;
            return true;
        }
        bool match_dot(CuspSet s)
        {
            float width = s.bbox.Width / InkPixel;
            float height = s.bbox.Height / InkPixel;
            bool left;
            double str = s.straight;
            if (s.pts.Length < 5 || (width <= 4 && height <= 4) ||
                (width <= 5 && height <= 5 && str > 0.1) || (width <= 6 && height <= 6 && str > 0.2))
                return true;
            if (width / height > 4 || height / width > 4)
                return false;
            double areaCoverage = s.dist / (s.bbox.Width * s.bbox.Height);
            if (width < 10 && height < 10 && areaCoverage > 0.1)
                return true;
            if (V2D.Straightness(s.pts, V2D.Sub(s.last, s.pts[0]), 0, s.pts.Length, s.bbox.Width, out left) > .1 && width + height < 12 &&
                areaCoverage > 0.025)
                return true;
            return false;
        }
        bool match2_sin(CuspSet s, CuspSet s1, string letter, string other)
        {
            if (letter == "." || letter.Contains("1"))
                if (other == "sin")
                {
                    if (s.bbox.Bottom > s1.bbox.Bottom)
                        return false;
                    if (s.bbox.Left < s1.bbox.Left)
                        return false;
                    if (s.bbox.Height > s1.bbox.Height)
                        return false;
                    return true;
                }
            return false;
        }
        bool match2_excl(CuspSet s, CuspSet s1, string letter, string other, ref int midpt, ref string allograph)
        {
            if (s.bbox.Bottom < s1.bbox.Bottom)
            {
                CuspSet tmp = s;
                s = s1;
                s1 = tmp;
                string tl = letter;
                letter = other;
                other = tl;
            }
            if (!other.Contains("1") && !other.Contains("/") && !other.Contains("\\") && !other.Contains("(") && !other.Contains(")"))
                return false;
            if (s.bbox.Height / (float)s1.bbox.Height > 0.25 || s.bbox.Height > 7 * InkPixel || s.bbox.Width > 7 * InkPixel)
                return false;
            Rectangle dotbox = s.bbox;
            if (dotbox.Width < InkPixel * 5)
                dotbox.Inflate((int)InkPixel * 5, (int)InkPixel * 5);
            if (dotbox.Left > s.last.X + InkPixel * 5 || dotbox.Right < s.last.X - InkPixel * 5)
                return false;
            if ((s.bbox.Top - s1.bbox.Bottom + 0.0) / s1.bbox.Height < 0.1)
                return false;
            double dotang = angle(s1.last, s.pts[0], new PointF(0, -1));
            if (dotang > 60)
                return false;
            midpt = (s1.bbox.Top + s.bbox.Bottom) / 2;
            return true;
        }
        bool match2_i(CuspSet s, CuspSet s1, string letter, string other, ref string allograph)
        {
            if (s.bbox.Bottom > s1.bbox.Bottom)
            {
                CuspSet tmp = s;
                s = s1;
                s1 = tmp;
                string tl = letter;
                letter = other;
                other = tl;
            }
            if (s.bbox.Height > s1.bbox.Height)
                return false;
            if (s1.bbox.Height < InkPixel * 3)
                return false;
            if (s.bbox.Top > s1.bbox.Top)
                return false;
            if (other.Contains("INT") && s.s.Id != s1.s.Id + 1)
                return false;
            float maxhgt = InkPixel * Math.Max(15, s1.bbox.Height / 50);
            if (s.bbox.Width > InkPixel * Math.Max(10, s1.bbox.Height / 100) || s.bbox.Height > maxhgt)
                return false;
            if (s.dist > maxhgt || s.bbox.Width > InkPixel * 7 || (s.bbox.Height > InkPixel * 11 && s.bbox.Height / (float)s1.bbox.Height > 0.4) ||
                other[0] == ')' || other[0] == '0' || s.bbox.Bottom > s1.bbox.Top)
                return false;
            if (s.bbox.Width / (float)s.bbox.Height > 3 && s1.bbox.Width / (float)s1.bbox.Height < 0.15 && s.bbox.Width / InkPixel > 3 && (s.bbox.Left - s1.bbox.Right + 0.0) > InkPixel)
                return false;
            if (other[0] == '\\' || other == "uv" || other[0] == '6' || other[0] == '(' || other[0] == 'L' || other[0] == 'c')
            {
                allograph = "i";
                return true;
            }
            int miyind, miy = miny(0, s1.pts.Length, s1.pts, out miyind);
            if (other == "l" || other == "e" || s1.cusps[0].bot)
            {
                if (s1.bbox.Height / (float)s1.bbox.Width < 0.2)
                    return false;
                if (s1.cusps.Length > 5)
                    for (int i = 1; i < s1.cusps.Length - 1; i++)
                        if ((s1.cusps[i].index < miyind * .9 ||
                            s1.cusps[i].index > miyind * 1.1) && s1.cusps[i].curvature > 0)
                            return false;
                allograph = "iscript";
                return true;
            }
            bool left;
            double straight = V2D.Straightness(s1.pts, out left);
            if ((s1.cusps[0].pt.Y - s1.pts[miyind].Y + 0.0) / s1.bbox.Height > -.1 && straight > 0.15)
            {
                /* script i */
                if (V2D.Straightness(s1.pts, 0, miyind) > 0.2)
                {
                    int maxstartind, maxstart = maxy(0, miyind, s1.pts, out maxstartind);
                    if (maxstartind == -1 || angle(s1.pts[maxstartind], s1.pts[0], new PointF(1, 0)) > 90 ||
                        V2D.Straightness(s1.pts, maxstartind, miyind) > 0.2)
                        return false;
                }
                int mayind, may = maxy(miyind, s1.pts.Length, s1.pts, out mayind);
                if (mayind == -1 || V2D.Straightness(s1.pts, miyind, mayind) > 0.3 || V2D.Straightness(s1.pts, mayind, s1.pts.Length) > 0.3)
                    return false;
                if (s1.intersects.Length > 1 && s1.intersects[s1.intersects.Length - 2] > miyind)
                    return false;
                PointF d1 = V2D.Sub(s1.pts[miyind], s1.pts[0]);
                PointF d2 = V2D.Sub(s1.last, s1.pts[miyind]);
                if ((s1.dist - s1.distances[miyind]) / V2D.Length(d1) < 0.5)
                    return false;
                double angUp = angle(V2D.Normalize(d1), new Point(1, -1));
                double angDo = angle(V2D.Normalize(d2), new Point(1, 1));
                if ((other == "17" || other == "1^" || other == "^1") && !left && V2D.Straightness(s1.pts, miyind, s1.pts.Length, out left) > 0.07)
                    return false;
                if (s1.pts[0].X < s1.last.X && angUp < 90 && angDo < 55)
                {
                    if (s1.cusps[0].top)
                        allograph = "iz";
                    else allograph = "iscript";
                    return true;
                }
            }
            int myind, my = maxy(0, s1.pts.Length, s1.pts, out myind);
            if (s1.intersects.Length > 1 && s1.intersects[s1.intersects.Length - 2] < myind && s1.intersects[s1.intersects.Length - 1] > myind)
                return false;
            double ang1 = angle(s1.last, s1.pts[0], new PointF(0, 1));
            if (ang1 > 90)
                return false;
            int botind; maxy(0, s1.pts.Length, s1.pts, out botind);
            Point reallast = s1.s.GetPoint(s1.s.GetPoints().Length - 1);
            if (s1.pts.Length - 1 > botind)
                reallast = s1.pts[(s1.pts.Length + botind) / 2];
            else if (V2D.Dist(reallast, s1.pts[botind]) / s1.bbox.Height < 0.05 ||
                     angle(reallast, s1.pts[botind], V2D.Sub(s1.pts[botind * 2 / 3], s1.pts[botind])) < 28)
                reallast = s1.last;
            double tailhooklen = V2D.Dist(reallast, s1.pts[botind]) / s1.bbox.Height;
            if ((tailhooklen > 0.1 && V2D.Det(V2D.Sub(reallast, s1.pts[botind]), V2D.Sub(s1.pts[botind / 2], s1.pts[botind])) > 0) ||
                (s1.intersects.Length > 0 && s1.intersects[s1.intersects.Length - 1] > botind))
            {
                allograph = "j";
                return true;
            }
            if (Math.Abs(ang1) < 25 && straight < 0.1)
            {
                allograph = left || straight < 0.45 ? "i" : "j";
                return true;
            }
            if (Math.Abs(ang1) < 70 && left)
            {
                allograph = "i";
                return true;
            }
            return false;
        }
        bool match2_j(CuspSet s, CuspSet s1, string letter, string other, ref string allograph, ref int baseline)
        {
            if (!(s.bbox.Width / (float)s1.bbox.Height < 0.2 && s.bbox.Height / (float)s1.bbox.Height < 0.2))
            {
                if (s.bbox.Width > InkPixel * 5 || s.bbox.Height > InkPixel * 11 || s.bbox.Bottom > s1.bbox.Top)
                    return false;
            }
            if (s.bbox.Left > s1.bbox.Right && s.bbox.Bottom > s1.bbox.Top) // can't be to the right
                return false;
            if (other.Contains("INT") && s.s.Id != s1.s.Id + 1)
                return false;
            if (s.bbox.Width > InkPixel * 10 || s.bbox.Height > InkPixel * 15)
                return false;
            if ((s.bbox.Bottom - s1.bbox.Top + 0.0) / s1.bbox.Height > 0.2)
                return false;
            if ((other == "0" && s1.curvatures[s1.curvatures.Length / 2] > 0) || other.Contains("2") || other == "gamma" || other == "y" || other == "superset" || other == "y4" || other[0] == '\\' || other[0] == '(' || other == "l" || other == "J" || other == "INTtop")
            {
                if (other.Contains("2") && s1.intersects.Length > 0)
                {
                    double tailang = angle(s1.last, s1.pts[s1.intersects[s1.intersects.Length - 1]], new Point(1, -1));
                    if (tailang > 30)
                        return false;
                }
                allograph = "j";
                baseline = (s1.bbox.Top + s1.bbox.Bottom) / 2;
                return true;
            }
            int startcusp = 0;
            if (s1.cusps.Length > 2 && s1.cusps[0].pt.Y > s1.cusps[1].pt.Y)
            {
                /* script j */
                if (s1.intersects.Length > 1 && s1.intersects[s1.intersects.Length - 2] > s1.cusps[1].index)
                {
                    int botloopind; maxy(s1.cusps[1].index, s1.pts.Length, s1.pts, out botloopind);
                    if (V2D.Straightness(s1.pts, s1.cusps[1].index, botloopind) > 0.2 ||
                        V2D.Straightness(s1.pts, botloopind, s1.pts.Length) > 0.3 ||
                        (s1.distances[botloopind] - s1.cusps[1].dist) / V2D.Dist(s1.pts[botloopind], s1.cusps[1].pt) > 1.5 ||
                        (s1.dist - s1.distances[botloopind]) / V2D.Dist(s1.pts[botloopind], s1.last) > 1.5)
                        return false;
                    baseline = (s1.bbox.Top + s1.bbox.Bottom) / 2;
                    return true;
                }
                PointF d1 = V2D.Sub(s1.cusps[1].pt, s1.cusps[0].pt);
                PointF d2 = V2D.Sub(s1.cusps[s1.l].pt, s1.cusps[1].pt);
                double angUp = angle(d1, new Point(1, -2));
                double angDo = angle(d2, new Point(1, 2));
                if (angUp < 30 && angDo < 45)
                {
                    allograph = "jscript";
                    baseline = (s1.bbox.Top + s1.bbox.Bottom) / 2;
                    return true;
                }

                startcusp = 1;
            }
            else if (s1.cusps[1].pt.Y < s1.cusps[0].pt.Y)
                return false;
            if (s1.cusps[startcusp].pt.Y > s1.cusps[startcusp + 1].pt.Y)
                return false;
            if (other == "-L" || other[0] == ',' || other[0] == ')' || (s1.cusps.Length == 2 && s1.avgCurveSeg(4, s1.pts.Length) >= 0.065) ||
                                (s1.cusps.Length == 3 && s1.cusps[startcusp + 1].curvature > 0) ||
                                (s1.cusps.Length == 4 && s1.cusps[startcusp + 1].curvature > 0 && s1.cusps[s.nl].curvature > 0))
            {
                baseline = (s1.bbox.Top + s1.bbox.Bottom) / 2;
                allograph = "j";
                return true;
            }
            return false;
        }
        bool match2_xx(CuspSet s, CuspSet s1, string letter, string other)
        {
            if (s.bbox.Right < s1.bbox.Right)
            {
                CuspSet tmp = s;
                s = s1;
                s1 = tmp;
                string l = letter;
                letter = other;
                other = l;
            }
            if (!(other == "superset" || other.Contains(")") || other.Contains("partial")) ||
                !(letter == "subset" || letter.Contains("c") || letter.Contains("(")))
                return false;
            float[] ints = s.s.FindIntersections(s1.s.Ink.CreateStrokes(new int[] { s1.s.Id }));
            if (ints.Length == 0)
            {
                int minindex;
                minx(0, s.pts.Length, s.pts, out minindex);
                float dist;
                s1.s.NearestPoint(s.pts[minindex], out dist);
                if (dist / s1.bbox.Width > 0.15)
                    return false;
                if (other[0] == ')' && letter[0] == '(' && dist / InkPixel > 2)
                    return false;
            }
            double yoverlap = Math.Min(s.bbox.Bottom, s1.bbox.Bottom) - Math.Max(s.bbox.Top, s1.bbox.Top);
            if ((s1.bbox.Height < s.bbox.Height && (s1.bbox.Height - yoverlap) / s1.bbox.Height > 0.3) ||
                    (s.bbox.Height < s1.bbox.Height && (s.bbox.Height - yoverlap) / s.bbox.Height > 0.3))
                return false;
            return true;
        }

        //TODO: make this better so that upleft-downright, downleft-upright X's are reliably recognized
        bool match2_x(CuspSet s, CuspSet s1, string letter, string other, ref Strokes stks)
        {
            Recognition r = Classification(s1.s); //if we're splitting something apart
            Stroke old = s1.s;
            CuspSet cross;
            CuspSet stem;
            string stemchar;
            string crosschar;
            double s1ang = Math.Abs(Math.Abs(angle(s1.last, s1.pts[0], new Point(-1, 1)) - 90) - 90);
            double sang = Math.Abs(Math.Abs(angle(s.last, s.pts[0], new Point(-1, 1)) - 90) - 90);
            if (s1ang < sang || s.bbox.Width / (float)s.bbox.Height > 2.5)
            {
                cross = s;
                stem = s1;
                stemchar = other;
                crosschar = letter;
            }
            else
            {
                cross = s1;
                stem = s;
                stemchar = letter;
                crosschar = other;
            }
            if (stemchar.Contains("1") && crosschar.Contains("c"))
            {
                float dist;
                stem.s.NearestPoint(cross.pts[0], out dist);
                if ((cross.pts[0].Y - stem.bbox.Top + 0.0) / stem.bbox.Height > 0.5 && dist / stem.bbox.Height < 0.15)
                    return false;
            }
            if (stemchar == "-" && crosschar == "(c")
                return false;
            int stemtopind = stem.last.Y <= stem.pts[0].Y ? stem.pts.Length - 1 : 0;
            int stembotind = stem.last.Y > stem.pts[0].Y ? stem.pts.Length - 1 : 0;
            Point stembottom = stem.last.Y > stem.pts[0].Y ? stem.last : stem.pts[0];
            Point stemtop = stem.last.Y <= stem.pts[0].Y ? stem.last : stem.pts[0];
            Point crosslast = cross.last.X < cross.pts[0].X ? cross.pts[0] : cross.last;
            if (crosslast.X < stembottom.X)
                return false;
            bool left;

            int stemminind; minx(stemtopind < stembotind ? stemtopind : stembotind, stemtopind < stembotind ? stembotind : stemtopind, stem.pts, out stemminind);
            if (stemminind == -1 || Math.Abs(stem.distances[stembotind] - stem.distances[stemminind]) > Math.Abs(stem.distances[stemtopind] - stem.distances[stemminind]))
                stemminind = stembotind;
            double stemang1 = angle(stem.pts[stemminind], stemtop, new Point(-1, 1));
            if (stemang1 > 32 || stem.straight > 0.4)
                return false;
            if (crosschar.Contains("7") && stem.bbox.Height / (float)cross.bbox.Height < 0.65)
                return false;
            float[] ints = cross.s.FindIntersections(stem.s.Ink.CreateStrokes(new int[] { stem.s.Id }));
            float[] stemints = stem.s.FindIntersections(cross.s.Ink.CreateStrokes(new int[] { cross.s.Id }));
            int stemintind = (int)convertIndexBack((int)stemints[0], stem.skipped);
            if (ints.Length != 1)
                return false;
            Point inter = getPt(ints[0], cross.s.GetPoints());
            int ly = inter.Y - miny(0, (int)convertIndexBack((int)ints[0], cross.skipped), cross.pts);
            int my = maxy((int)ints[0], cross.pts.Length, cross.pts) - inter.Y;
            double crossvert = angle(cross.last, cross.pts[0], new PointF(0, 1));
            double crossx = angle(cross.last, cross.pts[0], V2D.Sub(stembottom, stemtop));
            crossx = Math.Abs(Math.Abs(crossx - 90) - 90);
            if (Math.Abs(Math.Abs(crossvert - 90) - 90) < 14 && crossx > 50)
                return false;
            float[] ints2 = stem.s.FindIntersections(stem.s.Ink.CreateStrokes(new int[] { cross.s.Id }));
            int crossind = convertIndexBack((int)ints[0], cross.skipped);
            int stemind = convertIndexBack((int)ints2[0], stem.skipped);
            if (crossind == -1 || stemind == -1 ||
                (cross.distances[crossind] / cross.dist < .1 && V2D.Dist(cross.pts[0], inter) / cross.dist < .1) ||
                (stem.distances[stemind] / stem.dist < .15 && V2D.Dist(stem.pts[0], inter) / stem.dist < .1))
                return false;
            double crossang = angle(cross.last, cross.pts[0], new PointF(1, 0));
            if (crossang < 5)
                return false;
            if ((cross.last.X > cross.pts[0].X ? (cross.last.Y < cross.pts[0].Y) : (cross.pts[0].Y < cross.last.Y)) &&
                crossang < 20 && cross.straight < 0.2)
                return false;
            double stemang = angle(stembottom, stemtop, new Point(0, 1));
            int rightind, leftind;
            maxx(0, cross.pts.Length / 2, cross.pts, out rightind);
            V2D.MaxDist(cross.pts, V2D.Normalize(V2D.Sub(cross.last, cross.pts[rightind])), out left, out leftind, rightind, cross.pts.Length);
            double crossbarang = angle(cross.pts[rightind], cross.pts[leftind], new Point(1, -2));
            double stembarang = angle(stembottom, stemtop, new Point(-1, 0));
            double stemhgt = (cross.pts[leftind].Y - stembottom.Y + 0.0) / (cross.pts[leftind].Y - cross.bbox.Top);
            if (stemhgt > 0.14 && (crosschar.Contains("z") || crosschar.Contains("2")) && crossbarang + stembarang < (stemhgt > 0.25 ? 60 : 50) - crossbarang)
                return false;
            double f_dist = V2D.MaxDist(stem.pts, V2D.Normalize(V2D.Sub(stemtop, inter)), out left, stemtopind > stem.pts.Length / 2 ? stemind : stemtopind, stemtopind > stem.pts.Length / 2 ? stemtopind : stemind);
            if (crossang < 20 && f_dist / V2D.Dist(stemtop, inter) > 0.17 && left && angle(inter, stembottom, new PointF(0, -1)) < 20)
                return false;
            double f_ang = angle(stemtop, inter, new PointF(0, -1));
            if (f_ang > 25 && stemtop.X > inter.X && crossx > 45 && Math.Abs((Math.Abs(stem.angles[stemind]) - Math.PI / 2)) < 0.6)
            {
                int top_f_ind; miny(stemtopind > 0 ? stemintind : stemtopind, stemtopind > 0 ? stemtopind : stemintind, stem.pts, out top_f_ind);
                if (!(top_f_ind < 0 || top_f_ind >= stem.angles.Length))
                    if (((stemtopind == 0 && top_f_ind > 0 && Math.Abs(stem.angles[top_f_ind]) < Math.PI / 6) ||
                        (stemtopind > 0 && Math.Abs(stem.angles[top_f_ind]) > Math.PI - Math.PI / 6)) && V2D.Straightness(stem.pts, stemtopind > 0 ? stemintind : stemtopind, stemtopind > 0 ? stemtopind : stem.pts.Length) > 0.15)
                        return false;
            }
            if (crossang + stemang < 30 && cross.straight < 0.2 && stem.straight < 0.2)
                return false;
            // Psi - straight vertical stem, upward cross centered

            // y - slanted stem, straight cross ending on stem, upward cross aligning with stem

            // 4 - straight stem, negative curvature cross, straight horizontal portion of cross, possible positive curvature hook, 

            // t - loopy bottom stem, straight cross

            // + - straight stem and cross

            // f - loopy top stem and cross

            // x - anything else
            int cintind = convertIndexBack((int)ints[0], cross.skipped);
            int cornerind;
            V2D.MaxDist(cross.pts, V2D.Normalize(V2D.Sub(cross.pts[0], cross.pts[cintind])), out left, out cornerind, 0, cintind);
            double wavy = angle(cross.pts[0], cross.pts[cornerind], V2D.Sub(cross.pts[cintind], cross.pts[cornerind]));
            int mi_x_ind;
            int mi_x = minx(0, cross.pts.Length, cross.pts, out mi_x_ind);
            int maxind;
            int boty = maxy((int)(convertIndexBack((int)(ints[0] + 0.5), cross.skipped)), cross.pts.Length, cross.pts, out maxind);
            if ((stem.bbox.Bottom - boty + 0.0) / stem.bbox.Height > .2 && stemang < 27.5 && // for rejecting 4's that loop up like a curvy X
                ((wavy < 125 && cross.curvatures[cross.cusps[1].index < crossind ? cross.cusps[1].index : crossind / 2] < 0)))
                return false;
            if (maxind == -1)
                return false;
            if (cross.distances[crossind] / cross.dist > .8)
                return false;
            double ang1 = angle(stembottom, stemtop, new PointF(0, 1));
            double ang = angle(cross.pts[0], inter, new PointF(1, 0));
            if (ang > 25 && ang1 < 75)
            {
                stks = stks.Ink.CreateStrokes(new int[] { s.s.Id, s1.s.Id });
                if (r != null && r.strokes.Count > 1 && r.alt != Unicode.G.GREEK_SMALL_LETTER_PI)
                {
                    if (r.strokes.Count > 2)
                        return false;
                    UnClassify(r.strokes[0]);
                    r.strokes.Remove(old);
                    FullClassify(r.strokes[0], true);
                }
                return true;
            }
            return false;
        }
        bool match2_y(CuspSet s, CuspSet s1, string letter, string other, ref int baseline)
        {
            if (s.s.Id > s1.s.Id + 1)
                return false;
            CuspSet cross;
            CuspSet stem;
            string stemchar;
            if (Math.Sign(V2D.Normalize(V2D.Sub(s.last, s.pts[0])).X) >
                Math.Sign(V2D.Normalize(V2D.Sub(s1.pts[s1.pts.Length - 1], s1.pts[0])).X))
            {
                cross = s;
                stem = s1;
                stemchar = other;
            }
            else
            {
                cross = s1;
                stem = s;
                stemchar = letter;
            }
            if (!stemchar.Contains("/") && !stemchar.Contains(")") && !stemchar.Contains("1") && stemchar != "fbase" && !stemchar.Contains("(") && stemchar != "INTtop" &&
                (stemchar != "" || stem.cusps[0].pt.X < stem.cusps[stem.l].pt.X || stem.cusps[0].pt.Y > stem.cusps[stem.l].pt.Y))
                return false;
            if (stemchar[0] == '(' && V2D.Straightness(stem.pts) > 0.14)
                return false;
            if (cross.pts[0].X > cross.last.X)
                return false;
            Rectangle bounds = Rectangle.Union(s.bbox, s1.bbox);
            float near = stem.s.NearestPoint(cross.cusps[cross.l].pt);
            if (V2D.Dist(getPt(near, stem.s.GetPoints()), cross.cusps[cross.l].pt) / stem.dist > 0.2)
                return false;
            float[] ints = cross.s.FindIntersections(stem.s.Ink.CreateStrokes(new int[] { stem.s.Id }));
            Point inter = ints.Length > 0 ? getPt(ints[0], cross.s.GetPoints()) : cross.pts[cross.pts.Length - 1];
            if (ints.Length == 0)
            {
                float dist;
                stem.s.NearestPoint(inter, out dist);

                double distRatio = dist / s.bbox.Width;
                if (distRatio > 0.15)
                    return false;
            }
            // ratio of dist btwn stem/cross and the length of the stem
            double interRatio = V2D.Dist(inter, stem.pts[stem.pts.Length - 1]) / stem.dist;
            if (interRatio < 0.3 || (ints.Length > 0 && cross.distances[convertIndexBack((int)ints[0], cross.skipped)] / cross.dist < .775))
            {
                if (cross.last == inter || angle(stem.pts[0], inter, V2D.Sub(cross.last, inter)) > 10)
                    return false;
            }
            if (V2D.Straightness(cross.pts) > 0.3)
            {
                if (angle(stem.pts[0], inter, V2D.Sub(cross.last, ints.Length < 1 || ints[0] > cross.pts.Length - 3 ? cross.pts[cross.pts.Length - 3] : inter)) > 40)
                    return false;
            }
            Point extender = V2D.Sub(inter, cross.pts[0]);
            if (extender.X == 0 && extender.Y == 0)
                return false;
            Point tailExtender = V2D.Sub(inter, stem.pts[stem.pts.Length - 1]);
            if (V2D.Length(tailExtender) / stem.dist < 0.3)
                return false;
            double anglw = angle(extender, new PointF(0, 1));
            if (anglw > 75)
                return false;
            if ((cross.cusps[cross.l].pt.Y - bounds.Top + 0.0) / bounds.Height < 0.75 && V2D.Dist(cross.cusps[cross.l].pt, inter) / cross.dist > 0.35)
                return false;
            double ang1 = angle(stem.last, stem.pts[0], new PointF(0, 1));
            if (ang1 < 75)
            {
                baseline = (Math.Min(cross.bbox.Top, stem.bbox.Top) + Math.Max(cross.bbox.Bottom, stem.bbox.Bottom)) / 2;
                return true;
            }
            return false;
        }

        bool match2_z(CuspSet s, CuspSet s1, string letter, string other)
        {
            if (s.bbox.Width / (float)s1.bbox.Width > 3.5 ||
                ((s1.bbox.Right - s.bbox.Left + 0.0) / s1.bbox.Width > 2))
                return false;
            if (letter == "-" || letter == "~")
            { //removed: || letter == "/" - Gal Peleg
                if (other == "2" || other == "z" || other == "2z" || other == "z2")
                    return true;
            }
            return false;
        }

        bool match2_bbZ(CuspSet s, CuspSet s1, string letter, string other)
        {
            //Unicode Character 'DOUBLE-STRUCK CAPITAL Z' (U+2124)
            //Also known as "Blackboard Bold Z" - The set of all Integers

            //If base character is as a 'Z' - (looking for 'Z' + '/' = bbZ)
            if (other == "z" || other == "z2" || other == "2z")
            {
                int maxind; maxx(0, s1.pts.Length / 2, s1.pts, out maxind);
                int minind; minx(s1.pts.Length / 2, s1.pts.Length, s1.pts, out minind);
                PointF vert = new PointF(0, -1);
                Point cross = s.pts[0].Y < s.last.Y ? V2D.Sub(s.pts[0], s.last) : V2D.Sub(s.last, s.pts[0]);
                double relang = angle(s1.pts[maxind], s1.pts[minind], cross);
                if (s.straight < 0.2)
                {
                    float[] ints = s1.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id }));
                    if (ints.Length > 0)
                    {
                        if (relang > 10)
                            return false;
                        if (angle(s1.pts[maxind], s1.pts[minind], vert) < 20)
                            return false;
                    }
                }

                //Check to see that the 2nd character's dimentions are correct (relative to the 'Z')
                if (s1.bbox.Height / (float)s.bbox.Height > 2.0)
                    return false;

                //Check to see that the 2nd character's position is correct (relative to the 'Z')
                double horizontalPos = (s1.bbox.Right - s.bbox.Left + 0.0) / s1.bbox.Width;
                if (horizontalPos < 0.5 || horizontalPos > 1.75)
                    return false;
                double verticalPos = (s1.bbox.Bottom - s.bbox.Top + 0.0) / s1.bbox.Height;
                if (verticalPos < 0.6 || verticalPos > 1.4)
                    return false;

                //Construct Point A (Bottom/Left)
                int b1xind, b1x = minx(0, s.pts.Length, s.pts, out b1xind);
                Point A = s.pts[b1xind];

                //Construct Point B (Top/Right)
                int t1yind, t1y = maxx(0, s.pts.Length, s.pts, out t1yind);
                Point B = s.pts[t1yind];

                //Verify that Line Segment AB's straightness is within limits
                if (V2D.Straightness(s.pts) > 0.22)
                    return false;

                //Verify that Line Segment AB's slope (angle) is within limits
                if (angle(B, A, new PointF(1, 0)) > 75 || angle(B, A, new PointF(1, 0)) < 20)
                    return false;

                return true;
            }

            //If base character is as a '7' or '>' - (looking for '7' + 'L' = bbZ)
            else if (other == "7" || other == ">")
            {

                //Check to see that the new character is appropriate
                if (letter != "L" && letter != "<")
                    return false;

                //Check to see that the 2nd character's dimentions are correct (relative to the '7' or '>')
                double relativeHeight = s1.bbox.Height / (float)s.bbox.Height;
                if (relativeHeight > 1.3 || relativeHeight < 0.7)
                    return false;

                double relativeWidth = s1.bbox.Width / (float)s.bbox.Width;
                if (relativeWidth > 1.8 || relativeWidth < 0.7)
                    return false;

                //Construct Point A (Top/Right corner of '7')
                int t1yind, t1y = maxx(0, s1.pts.Length, s1.pts, out t1yind);
                Point A = s1.pts[t1yind];

                //Construct Point B (Top of 'L')
                int t2yind, t2y = miny(0, s.pts.Length, s.pts, out t2yind);
                Point B = s.pts[t2yind];

                //Check relative proximity of points A & B
                if (Math.Abs((B.X - A.X) / (float)s1.bbox.Width) > 0.40)
                    return false;

                if (Math.Abs((B.Y - A.Y) / (float)s1.bbox.Height) > 0.30)
                    return false;

                //Construct Point C (Bottom of '7')
                int b1xind, b1x = maxy(0, s1.pts.Length, s1.pts, out b1xind);
                Point C = s1.pts[b1xind];

                //Construct Point D (Bottom/Left corner of 'L')
                int b2xind, b2x = minx(0, s.pts.Length, s.pts, out b2xind);
                Point D = s.pts[b2xind];

                //Compare Slopes AC & BD (by angles)
                double angleAC = angle(A, C, new PointF(1, 0));
                double angleBD = angle(B, D, new PointF(1, 0));
                double ratio = angleAC / (float)angleBD;
                if (ratio > 1.2 || ratio < 0.8)
                    return false;

                return true;
            }

            else { return false; }
        }

        bool match2_bbN(CuspSet s, CuspSet s1, string letter, string other)
        {
            //Unicode Character 'DOUBLE-STRUCK CAPITAL N' (U+2115)
            //Also known as "Blackboard Bold N" - The set of all Natural Numbers

            //Check to see that the base character was recognized as a 'N'
            if (other != "N")
                return false;

            //Check to see that the 2nd character's height/length is correct (relative to the 'N')
            double heightRatio = s1.bbox.Height / (float)s.bbox.Height;
            if (heightRatio > 1.7 || heightRatio < 0.7)
                return false;

            //Construct Point A (Bottom)
            int b1xind, b1x = maxy(0, s.pts.Length, s.pts, out b1xind);
            Point A = s.pts[b1xind];

            //Construct Point B (Top)
            int t1yind, t1y = miny(0, s.pts.Length, s.pts, out t1yind);
            Point B = s.pts[t1yind];

            //Verify that Line Segment AB's straightness is within limits
            if (V2D.Straightness(s.pts) > 0.22)
                return false;

            //Verify that Line Segment AB's slope (angle) is within limits
            double angleAB = angle(B, A, new PointF(1, 0));
            if (angleAB > 100 || angleAB < 65)
                return false;

            //Check to see that the 2nd character's position is correct (relative to the 'N')
            double horizontalDiff = Math.Abs((s.bbox.Left - s1.bbox.Left + 0.0) / s1.bbox.Width);
            if (horizontalDiff > 0.4)
                return false;

            return true;
        }

        bool match3_bbQ(CuspSet s, CuspSet s1, string letter, string other)
        {
            //Unicode Character 'DOUBLE-STRUCK CAPITAL Q' (U+211A)
            //Also known as "Blackboard Bold Q" - The set of all Rational Numbers

            //Check to see that the base character was recognized as a 'Q'
            if (other != "Q")
                return false;

            //Check to see that the 2nd character's height/length is correct (relative to the 'Q')
            double heightRatio = s1.bbox.Height / (float)s.bbox.Height;
            if (heightRatio > 2.5 || heightRatio < 0.5)
                return false;

            //Construct Point A (Bottom)
            int b1xind, b1x = maxy(0, s.pts.Length, s.pts, out b1xind);
            Point A = s.pts[b1xind];

            //Construct Point B (Top)
            int t1yind, t1y = miny(0, s.pts.Length, s.pts, out t1yind);
            Point B = s.pts[t1yind];

            //Verify that Line Segment AB's straightness is within limits
            if (V2D.Straightness(s.pts) > 0.22)
                return false;

            //Verify that Line Segment AB's slope (angle) is within limits
            double angleAB = angle(B, A, new PointF(1, 0));
            if (angleAB > 100 || angleAB < 80)
                return false;

            //Check to see that the 2nd character's position is correct (relative to the 'Q')
            double horizontalDiff = (s.bbox.Left - s1.bbox.Left + 0.0) / s1.bbox.Width;
            if (horizontalDiff > 0.4)
                return false;

            return true;
        }

        bool match2_bbC(CuspSet s, CuspSet s1, string letter, string other)
        {
            //Unicode Character 'DOUBLE-STRUCK CAPITAL C' (U+2102)
            //Also known as "Blackboard Bold C" - The set of all Complex Numbers

            //Check to see that the base character was recognized as a 'c'
            if (other != "c" && other != "cd" && other != "c(" && other != "(c" && other != "c0" && other != "C")
                return false;

            //Check to see that the 2nd character's height/length is correct (relative to the 'C')
            double heightRatio = s1.bbox.Height / (float)s.bbox.Height;
            if (heightRatio > 3.0 || heightRatio < 0.50)
                return false;

            //Construct Point A (Bottom)
            int b1xind, b1x = maxy(0, s.pts.Length, s.pts, out b1xind);
            Point A = s.pts[b1xind];

            //Construct Point B (Top)
            int t1yind, t1y = miny(0, s.pts.Length, s.pts, out t1yind);
            Point B = s.pts[t1yind];

            //Verify that Line Segment AB's straightness is within limits
            if (V2D.Straightness(s.pts) > 0.22)
                return false;

            //Verify that Line Segment AB's slope (angle) is within limits
            double angleAB = angle(B, A, new PointF(1, 0));
            if (angleAB > 100 || angleAB < 80)
                return false;

            //Check to see that the 2nd character's position is correct (relative to the 'Q')
            double horizontalDiff = (s.bbox.Left - s1.bbox.Left + 0.0) / s1.bbox.Width;
            if (horizontalDiff > 0.5)
                return false;

            return true;
        }

        bool match3_identicalTo(CuspSet s, CuspSet s1, string letter, string other, ref string allograph)
        {

            double widthRatio = s1.bbox.Width / (float)s.bbox.Width;
            if (widthRatio < 0.3 || widthRatio > 1.5)
                return false;

            if (Math.Abs(s.bbox.Right - s1.bbox.Right) / (float)s.bbox.Width > 0.40)
                return false;

            if (Math.Abs(s.bbox.Bottom - s1.bbox.Bottom) / (float)s.bbox.Width > 1.0)
                return false;

            //Unicode Character 'IDENTICAL TO' (U+2261) - "≡"
            if (letter.Contains("-") && other.Contains("="))
            {
                allograph = "≡"; return true;
            }
            //Unicode Character 'ALMOST EQUAL TO' (U+2248) - "≈"
            else if (letter.Contains("~") && other.Contains("~"))
            {
                allograph = "≈"; return true;
            }
            else { return false; }
        }

        bool match3_approximatelyEqualTo(CuspSet s, CuspSet s1, string letter, string other, ref string allograph)
        {

            CuspSet lower, upper; String suspect;

            if (letter.Contains("~") && other.Contains("-"))
            {
                lower = s1; upper = s; suspect = "asympEqTo";
            }
            else if (letter.Contains("-") && other.Contains("~"))
            {
                lower = s; upper = s1; suspect = "asympEqTo";
            }
            else if (letter.Contains("-") && other.Contains("asympEqTo"))
            {
                upper = s1; lower = s; suspect = "aproxEqTo";
            }
            else if (letter.Contains("~") && other.Contains("="))
            {
                upper = s; lower = s1; suspect = "aproxEqTo";
            }
            else { return false; }

            double widthRatio = s1.bbox.Width / (float)s.bbox.Width;
            if (widthRatio < 0.667 || widthRatio > 1.5)
                return false;

            if (Math.Abs(s.bbox.Right - s1.bbox.Right) / (float)s1.bbox.Width > 0.40)
                return false;

            double orientation = (lower.bbox.Top - upper.bbox.Bottom) / (float)lower.bbox.Width;
            if (orientation < 0 || orientation > 0.5)
                return false;

            //Unicode Character 'ASYMPTOTICALLY EQUAL TO' (U+2243)
            if (suspect == "asympEqTo")
            {
                allograph = "asympEqTo"; return true;
            }
            //Unicode Character 'APPROXIMATELY EQUAL TO' (U+2245)
            else if (suspect == "aproxEqTo")
            {
                allograph = "aproxEqTo"; return true;
            }
            else { return false; }
        }

        bool match2_lessThanOrEqualTo(CuspSet s, CuspSet s1, string letter, string other, ref string allograph)
        {

            double widthRatio = s1.bbox.Width / (float)s.bbox.Width;
            if (widthRatio < 0.5 || widthRatio > 2.0)
                return false;

            if (Math.Abs(s.bbox.Right - s1.bbox.Right) / (float)s1.bbox.Width > 0.50)
                return false;

            double orientation = (s.bbox.Top - s1.bbox.Bottom) / (float)s1.bbox.Height;
            if (orientation < 0 || orientation > 0.5)
                return false;

            //LESS-THAN OR EQUAL TO
            if (letter.Contains("-") && other.Contains("<"))
            {
                allograph = "lessOrEq"; return true;
            }
            //GREATER-THAN OR EQUAL TO
            else if (letter.Contains("-") && other.Contains(">"))
            {
                allograph = "greatOrEq"; return true;
            }
            //SUBSET OF OR EQUAL TO
            else if (letter.Contains("-") && other.Contains("c"))
            {
                allograph = "subsetOrEq"; return true;
            }
            //SUPERSET OF OR EQUAL TO
            else if (letter.Contains("-") && other.Contains("superset"))
            {
                allograph = "supersetOrEq"; return true;
            }
            else { return false; }
        }

        bool match2_pathIntegral(CuspSet s, CuspSet s1, string letter, string other)
        {

            if (!other.Contains("s") && !other.Contains("s") && !other.Contains("INTbot")
                && !other.Contains("INTtop") && !other.Contains("fbase"))
                return false;
            if (!letter.Contains("0"))
                return false;

            double widthRatio = s1.bbox.Width / (float)s.bbox.Width;
            if (widthRatio < 0.5 || widthRatio > 3)
                return false;

            if (Math.Abs(s.bbox.Right - s1.bbox.Right) / (float)s1.bbox.Width > 0.5)
                return false;

            double orientation = (s1.bbox.Bottom - s.bbox.Bottom) / (float)s1.bbox.Height;
            if (orientation < 0.2 || orientation > 0.6)
                return false;

            return true;
        }

        bool match2_circledSlash(CuspSet s, CuspSet s1, string letter, string other, ref string allograph)
        {

            if (!other.Contains("0") && !letter.Contains("0"))
                return false;
            if (other.Contains("0") && letter.Contains("0"))
                return false;

            CuspSet line, circle; String lineString;
            if (letter.Contains("0"))
            {
                line = s1; circle = s; lineString = other;
            }
            else if (other.Contains("0"))
            {
                line = s; circle = s1; lineString = letter;
            }
            else { return false; }

            double widthRatio = circle.bbox.Width / (float)line.bbox.Width;
            if (widthRatio < 0.33 || widthRatio > 3.0)
                return false;
            double heightRatio = circle.bbox.Height / (float)line.bbox.Height;
            if (heightRatio < 0.33 || heightRatio > 3.0)
                return false;

            if (Math.Abs(line.bbox.Right - circle.bbox.Right) / (float)circle.bbox.Width > 0.5)
                return false;
            if (Math.Abs(line.bbox.Top - circle.bbox.Top) / (float)circle.bbox.Width > 0.5)
                return false;

            //Construct Point A (top of character)
            int t1yind, t1y = miny(0, line.pts.Length, line.pts, out t1yind);
            Point A = line.pts[t1yind];

            //Construct Point B (Bottom of character)
            int b1yind, b1y = maxy(0, line.pts.Length, line.pts, out b1yind);
            Point B = line.pts[b1yind];

            if ((lineString.Contains("/") || lineString.Contains("1")) && A.X > B.X)
                allograph = "circledSlash";
            else if ((lineString.Contains("\\") || lineString.Contains("1")) && A.X < B.X)
                allograph = "circledBackslash";
            else { return false; }

            return true;
        }

        bool match2_circledDot(CuspSet s, CuspSet s1, string letter, string other)
        {
            CuspSet dot = s, circle = s1;
            if (letter.Contains("0"))
            {
                dot = s1;
                circle = s;
            }
            else if (!other.Contains("0"))
                return false;

            if (dot.bbox.Height > InkPixel * 15 || dot.bbox.Width > InkPixel * 15)
                return false;
            if (!circle.bbox.Contains(dot.bbox))
                return false;
            double horizontalRatio = (circle.bbox.Right - dot.bbox.Right) / (float)circle.bbox.Width;
            if (horizontalRatio < 0.3 || horizontalRatio > 0.7) { return false; }

            double verticalRatio = (circle.bbox.Bottom - dot.bbox.Bottom) / (float)circle.bbox.Height;
            if (verticalRatio < 0.3 || verticalRatio > 0.7) { return false; }

            return true;
        }

        bool match3_circledPlus(CuspSet s, CuspSet s1, string letter, string other)
        {

            if ((other.Contains("θ") || other.Contains("Θ") || other.Contains("circledMinus")) && letter.Contains("1"))
            {
                double proportion = s1.bbox.Width / (float)s.bbox.Height;
                if (proportion < 0.40 || proportion > 2.0)
                    return false;

                double position = (s1.bbox.Right - s.bbox.Right) / (float)s1.bbox.Width;
                if (position < 0.3 || position > 0.7)
                    return false;
            }

            else if (other.Contains("circledVertBar") && letter.Contains("-"))
            {
                double proportion = s1.bbox.Height / (float)s.bbox.Width;
                if (proportion < 0.50 || proportion > 2.0)
                    return false;

                double position = (s.bbox.Top - s1.bbox.Top) / (float)s1.bbox.Height;
                if (position < 0.3 || position > 0.7)
                    return false;
            }

            else if (other.Contains("+") && letter.Contains("0"))
            {
                bool lastStrokeofPlus = true; //true = vertical stem, false = horizontal bar
                if (s1.bbox.Width > s1.bbox.Height)
                    lastStrokeofPlus = false;

                if (lastStrokeofPlus)
                { //vertical stem
                    double proportion = s1.bbox.Height / (float)s.bbox.Height;
                    if (proportion < 0.33 || proportion > 3.0)
                        return false;
                    double position = (s.bbox.Right - s1.bbox.Right) / (float)s.bbox.Width;
                    if (position < 0.2 || position > 0.8)
                        return false;
                }
                else
                { //horizontal bar
                    double proportion = s1.bbox.Width / (float)s.bbox.Width;
                    if (proportion < 0.33 || proportion > 3.0)
                        return false;
                    double position = (s.bbox.Top - s1.bbox.Top) / (float)s.bbox.Height;
                    if (position < 0.2 || position > 0.8)
                        return false;
                }
            }
            else { return false; }
            return true;
        }

        bool match3_circledTimes(CuspSet s, CuspSet s1, string letter, string other)
        {

            if (other.Contains("circledSlash") && (letter.Contains("\\") || letter.Contains("1"))) { }
            else if ((other.Contains("circledBackslash") || other.Contains("Q"))
                && (letter.Contains("/") || letter.Contains("1"))) { }
            else if (letter.Contains("0") && (other.Contains("x") || other.Contains("xx") || other.Contains("X"))) { }
            else { return false; }

            double widthRatio = s1.bbox.Width / (float)s.bbox.Width;
            if (widthRatio < 0.5 || widthRatio > 3.0)
                return false;
            double heightRatio = s1.bbox.Height / (float)s.bbox.Height;
            if (heightRatio < 0.5 || heightRatio > 3.0)
                return false;

            if (Math.Abs(s.bbox.Right - s1.bbox.Right) / (float)s1.bbox.Width > 0.6)
                return false;
            if (Math.Abs(s.bbox.Top - s1.bbox.Top) / (float)s1.bbox.Height > 0.6)
                return false;

            return true;
        }

        bool match2_angle(CuspSet s, CuspSet s1, string letter, string other, ref string allograph)
        {
            if (s.bbox.Right > s1.bbox.Right || s.bbox.Top < s1.bbox.Top || s.bbox.Left < s1.bbox.Left)
                return false;
            int minind; minx(0, s1.pts.Length, s1.pts, out minind);
            if (minind < 1)
                return false;
            if ((other != "L" && other != "<") ||
                (V2D.Straightness(s1.pts, 0, minind) > 0.2 || V2D.Straightness(s1.pts, minind, s1.pts.Length) > 0.2))
                return false;
            if (!s.cusps[0].top || !s.cusps[s.l].bot || s.straight > 0.5 || s.cusps.Length > 3)
                return false;
            string tmpallograph = "measuredAngle";
            double baseang = angle(s1.last, s1.pts[minind], new Point(1, 1));
            if (baseang < 35)
                tmpallograph = "sphericalAngle";
            if (angle(s1.last, s1.pts[minind], new PointF(1, 0)) > 70 ||
                angle(s1.pts[0], s1.pts[minind], new PointF(1, 0)) > 70)
                return false;

            allograph = tmpallograph;

            return true;
        }

        bool match3_mapsTo(CuspSet s, CuspSet s1, string letter, string other, ref string allograph)
        {

            double sCenter = (s.bbox.Bottom + s.bbox.Top) / 2;
            double s1Center = (s1.bbox.Bottom + s1.bbox.Top) / 2;

            if (letter.Contains("1") && (other.Contains("rarrow-2") || other.Contains("rarrow-1")))
            {

                if (Math.Abs(s.bbox.Left - s1.bbox.Left) / (float)s1.bbox.Width > 0.2)
                    return false;
                if (Math.Abs(sCenter - s1Center) / (float)s.bbox.Height > 0.2)
                    return false;
                allograph = "mapsTo";
            }
            else if ((other.Contains("1") && letter.Contains("rarrow-1")) ||
           (other.Contains("assertion") && (letter.Contains(">") || letter.Contains(")")
                || letter.Contains("7") || letter.Contains("superset"))))
            {

                if (s.bbox.Right < s1.bbox.Right)
                    return false;
                if (Math.Abs(sCenter - s1Center) / (float)s1.bbox.Height > 0.4)
                    return false;
                allograph = "mapsTo";
            }

            else if (other.Contains("1") && letter.Contains("-"))
            {
                if (Math.Abs(s.bbox.Left - s1.bbox.Left) / (float)s1.bbox.Height > 0.1)
                    return false;
                if (Math.Abs(sCenter - s1Center) / (float)s1.bbox.Height > 0.2)
                    return false;
                double lengthRatio = s1.dist / (float)s.dist;
                if (lengthRatio < 0.3 || lengthRatio > 4.0)
                    return false;
                allograph = "assertion";
            }


            else { return false; }
            return true;
        }

        bool match3_Xi(CuspSet s, CuspSet s1, string letter, string other, ref string allograph)
        {

            if (letter.Contains("-") && other.Contains("-"))
            {
                if (s.bbox.Width / (float)s1.bbox.Width > 0.5)
                    return false;
                if (s.bbox.Bottom < s1.bbox.Bottom)
                    return false;
                Rectangle box = s.s.GetBoundingBox();
                box = Rectangle.Union(box, s1.s.GetBoundingBox());
                Strokes inside = filter(s.s.Ink.HitTest(box, 1));
                inside.Remove(s.s);
                inside.Remove(s1.s);
                if (inside.Count > 0)
                    return false;
                allograph = "trapez=";
            }
            else if (letter.Contains("-") && other.Contains("trapez="))
            {
                double widthRatio = s.bbox.Width / (float)s1.bbox.Width;
                if (widthRatio < 2 || widthRatio > 8)
                    return false;
                if (s.bbox.Bottom < s1.bbox.Bottom)
                    return false;
                allograph = "Xi";
            }
            else if (letter.Contains("-") && other.Contains("="))
            {
                if (s.bbox.Width / (float)s1.bbox.Width > 0.75)
                    return false;
                if (s.bbox.Bottom > s1.bbox.Bottom)
                    return false;
                allograph = "Xi";
            }
            else { return false; }

            return true;
        }

        bool match2_Y(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (!letter.Contains("1") && !letter.Contains(")") && !letter.Contains("(") && letter != "/" && letter != "\\")
                return false;
            if (other != "v" && other != "uv" && other != Unicode.S.SQUARE_ROOT.ToString())
                return false;
            if ((s1.bbox.Bottom - s.bbox.Top + 0.0) / s.bbox.Height > 0.275)
                return false;
            int minyind; miny(0, s.pts.Length, s.pts, out minyind);
            int maxyind; maxy(0, s1.pts.Length, s1.pts, out maxyind);
            if (V2D.Dist(s1.pts[maxyind], s.pts[minyind]) / Math.Max(s1.bbox.Height, s.bbox.Height) > 0.25)
                return false;
            Rectangle cbounds = Rectangle.Union(s.s.GetBoundingBox(), s1.s.GetBoundingBox());
            xhgt = (cbounds.Top + cbounds.Bottom) / 2;
            return true;
        }
        bool match2_Psi(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            CuspSet stem = s1;
            CuspSet cross = s;
            string stemsym = other;
            string crosssym = letter;
            if (s.bbox.Bottom > s1.bbox.Bottom)
            {
                stem = s;
                cross = s1;
                stemsym = letter;
                crosssym = other;
            }
            if (stemsym == "")
                return false;
            if (stemsym[0] != '1' && stemsym[0] != '(' && stemsym != "/" && stemsym != "\\")
                return false;
            if (crosssym != "v" && crosssym != "uv" && crosssym != "nu")
                return false;
            if ((maxy(0, stem.pts.Length, stem.pts) - stem.bbox.Top + 0.0) / stem.bbox.Height < 0.15)
                return false;
            float[] ints = stem.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { cross.s.Id }));
            if (Math.Abs(stem.distances[convertIndexBack((int)ints[0], stem.skipped)] / stem.dist - 0.5) > 0.25)
                return false;
            Point interPt = getPt(ints[0], stem.s.GetPoints());
            if (angle(stem.pts[0], interPt, V2D.Sub(cross.last, interPt)) < 8)
                return false;
            if ((cross.last.Y - stem.bbox.Top + 0.0) / stem.bbox.Height > 0.2) // cross doesn't go high enough -- more like a 4
                return false;
            float[] intC = cross.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { stem.s.Id }));
            int crossind = convertIndexBack((int)intC[0], cross.skipped);
            if ((cross.dist - cross.distances[crossind]) / cross.dist < .33) // cross doesn't go high enough -- more like a 4
                return false;
            float[] ints2 = cross.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { stem.s.Id }));
            if (Math.Abs(cross.distances[convertIndexBack((int)ints2[0], cross.skipped)] / cross.dist - 0.5) > 0.25)
                return false;
            Rectangle cbounds = Rectangle.Union(stem.s.GetBoundingBox(), cross.s.GetBoundingBox());
            xhgt = (cbounds.Top + cbounds.Bottom) / 2;
            return true;
        }
        bool match2_7(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if ((letter != "-" && letter != "~" && letter != "/" && letter != "\\") ||
                (!other.Contains("7") && other != "y" && other != ">" && other != "superset"
                && !(other.Contains(")") && V2D.Straightness(s1.pts) > 0.2)))
                return false;
            if (other == "17" && s1.cusps[0].bot)
                return false;
            if (s.bbox.Width / (float)s1.bbox.Width > 2.75 ||
                ((s1.bbox.Right - s.bbox.Left + 0.0) / s1.bbox.Width > 2))
                return false;
            if (s1.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id })).Length > 1)
                return false;
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        bool match2_8(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if ((!letter.Contains("c") && letter != "partial" && letter != "6" && letter != "a" && letter != "0") ||
                (other != "partial" && !other.Contains("c") && other != "6" && other != "a" && other != "0" && other != "<"))
                return false;
            CuspSet top = s1.bbox.Top < s.bbox.Top ? s1 : s;
            CuspSet bot = s1 == top ? s : s1;
            if ((bot.bbox.Top - top.bbox.Bottom + 0.0) / bot.bbox.Height > 0.3)
                return false;
            if ((bot.bbox.Top - top.bbox.Bottom + 0.0) / bot.bbox.Height < -0.4)
                return false;
            if ((bot.bbox.Right - top.bbox.Left + 0.0) / bot.bbox.Width < .5)
                return false;
            if ((top.bbox.Right - bot.bbox.Left + 0.0) / top.bbox.Width < .5)
                return false;
            if ((bot.bbox.Right - top.bbox.Right + 0.0) / bot.bbox.Width > .5)
                return false;
            xhgt = (Math.Min(s.bbox.Top, s1.bbox.Top) + Math.Max(s.bbox.Bottom, s1.bbox.Bottom)) / 2;
            return true;
        }
        bool match2_Sigma(CuspSet s, CuspSet s1, string letter, string other, ref  int xhgt, ref Strokes stks)
        {
            if (s.bbox.Width / (s.bbox.Height + 1) < s1.bbox.Width / (s1.bbox.Height + 1))
            {
                CuspSet tmp = s;
                s = s1;
                s1 = tmp;
                string tstr = letter;
                letter = other;
                other = tstr;
            }
            if (Math.Abs(s.bbox.Width - s1.bbox.Width) / (float)Math.Min(s.bbox.Width, s1.bbox.Width) > 3)
                return false;
            CuspSet bar = s;
            CuspSet sig = s1;
            if (bar.straight > 0.15 || 180 - Math.Abs(angle(bar.pts[0], bar.last, new PointF(1, 0))) > 20)
                return false;
            if (other != "2" && !other.Contains("z") && other != "Z")
                return false;
            Point barleft = bar.last.X < bar.pts[0].X ? bar.last : bar.pts[0];
            float dist, dist2;
            Point sigTop = getPt(sig.s.NearestPoint(barleft, out dist), sig.s.GetPoints());
            Point barTop = getPt(bar.s.NearestPoint(sig.pts[0], out dist2), bar.s.GetPoints());
            if (Math.Min(dist, dist2) / sig.bbox.Height > 0.15)
                return false;
            if ((barleft.Y - s1.bbox.Top + 0.0) / s1.bbox.Height > 0.2)
                return false;
            return true;
        }
        bool match2_Perp(CuspSet s, CuspSet s1, string letter, string other, ref  int xhgt, ref Strokes stks, ref string allograph)
        {
            if (s.bbox.Height == 0 || s1.bbox.Height == 0)
                return false;
            if (s.bbox.Width / s.bbox.Height < s1.bbox.Width / s1.bbox.Height)
            {
                string l = letter; letter = other; other = l;
                CuspSet cs = s; s = s1; s1 = cs;
            }
            if (letter != "=" && letter != "-" && letter != "~" && letter != "/" && letter != "\\" && !letter.Contains("(") && !letter.Contains(")"))
                return false;
            if (s1.straight > 0.2)
                return false;

            if (Math.Max(s1.bbox.Height, s.bbox.Width) / (float)Math.Min(s1.bbox.Height, s.bbox.Width) > 2)
                return false;
            if (angle(s.last, s.pts[0], new PointF(1, 0)) > 35 && angle(s.pts[0], s.last, new PointF(1, 0)) > 35)
                return false;

            double stemStraightness = V2D.Straightness(s1.pts);
            string tmpAllograph = "perp";
            float[] ints = s1.s.FindIntersections(s1.s.Ink.CreateStrokes(new int[] { s.s.Id }));
            if (ints.Length == 0)
            {
                float distI;
                s.s.NearestPoint(s1.s.GetPoint(s1.s.GetPoints().Length - 1), out distI);
                if (distI / Math.Max(s1.bbox.Height, s.bbox.Width) > 0.125)
                    return false;
            }
            if (ints.Length == 0)
            {
                if ((s.bbox.Left - s1.bbox.Right + 0.0) / s1.bbox.Height > .05 || s.bbox.Right < s1.bbox.Right || (s.bbox.Bottom - s1.bbox.Bottom + 0.0) / s1.bbox.Height > 0.5)
                    return false;
            }
            else
            {
                Point interPt = getPt(ints[ints.Length - 1], s1.s.GetPoints());
                double stemRatio = (s1.bbox.Bottom - interPt.Y + 0.0) / s1.bbox.Height; // smaller is more of a T
                if (tmpAllograph == "perp" && stemRatio > 0.19 * Math.Pow(1 / (1 + stemStraightness), 7))
                    return false;
            }

            Rectangle box = new Rectangle(s.bbox.Location, new Size(s.bbox.Width, s1.bbox.Height * 3 / 2));
            Strokes below = filter(s.s.Ink.HitTest(box, 1));
            below.Remove(s.s);
            below.Remove(s1.s);
            if (below.Count > 1)
            {
                int which = 0;
                int bot = below[0].GetBoundingBox().Bottom;
                for (int i = 1; i < below.Count; i++)
                    if (below[i].GetBoundingBox().Bottom < bot)
                    {
                        bot = below[i].GetBoundingBox().Bottom;
                        which = i;
                    }
                below = below.Ink.CreateStrokes(new int[] { below[which].Id });
            }
            if (below.Count == 1 && below.GetBoundingBox().Width / (float)below.GetBoundingBox().Height < 3)
                return true;
            Recognition r = Classification(s.s);
            if (r != null && r.strokes.Count > 1)
            { // if we're breaking apart an '=', we have to update the top stroke to be a '-'
                if (r.strokes.Count > 2)
                    return false;
                Strokes newrec = r.strokes.Ink.CreateStrokes();
                foreach (Stroke sr in r.strokes)
                    if (sr.Id != s.s.Id)
                        newrec.Add(sr);
                FullClassify(r.strokes[0], false);//new Recognition(newrec, "-", newrec.GetBoundingBox().Bottom,
                //newrec.GetBoundingBox().Top, false));
            }
            stks = s1.s.Ink.CreateStrokes(new int[] { s.s.Id, s1.s.Id });
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            allograph = tmpAllograph;
            return true;
        }
        bool match2_T(CuspSet s, CuspSet s1, string letter, string other, ref  int xhgt, ref Strokes stks, ref string allograph)
        {
            if (s.bbox.Height == 0 || s1.bbox.Height == 0)
                return false;
            if (s.bbox.Width / s.bbox.Height < s1.bbox.Width / s1.bbox.Height)
            {
                string l = letter; letter = other; other = l;
                CuspSet cs = s; s = s1; s1 = cs;
            }
            if (letter != "=" && letter != "-" && letter != "~" && letter != "/" && letter != "\\" && !letter.Contains("(") && !letter.Contains(")"))
                return false;

            Rectangle box = new Rectangle(V2D.Add(s.bbox.Location, new Point(s.bbox.Width / 4, -s1.bbox.Height / 2)), new Size(s.bbox.Width / 2, s1.bbox.Height / 2));
            Strokes above = filter(s.s.Ink.HitTest(box, 1));
            above.Remove(s.s);
            above.Remove(s1.s);
            if (above.Count > 1)
            {
                int which = 0;
                int top = above[0].GetBoundingBox().Top;
                for (int i = 1; i < above.Count; i++)
                    if (above[i].GetBoundingBox().Top > top)
                    {
                        top = above[i].GetBoundingBox().Top;
                        which = i;
                    }
                above = above.Ink.CreateStrokes(new int[] { above[which].Id });
            }
            if (above.Count == 1 && above.GetBoundingBox().Width / (float)above.GetBoundingBox().Height < 3)
                return false;

            if (Math.Max(s1.bbox.Height, s.bbox.Width) / (float)Math.Min(s1.bbox.Height, s.bbox.Width) > 2.4)
                return false;
            if (angle(s.last, s.pts[0], new PointF(1, 0)) > 35 && angle(s.pts[0], s.last, new PointF(1, 0)) > 35)
                return false;

            double stemStraightness = V2D.Straightness(s1.pts);
            string tmpAllograph = "T";
            if ((stemStraightness > 0.17 || angle(s1.last, s1.pts[0], new PointF(0, 1)) > 25))
            {
                if (s1.avgCurveSeg(3, s1.pts.Length - 3) < 0)
                    return false;
                else
                {
                    for (int i = 1; i < s1.cusps.Length - 1; i++)
                        if (s1.cusps[i].curvature < 0)
                            return false;
                    tmpAllograph = "J";
                }
            }
            float[] ints = s1.s.FindIntersections(s1.s.Ink.CreateStrokes(new int[] { s.s.Id }));
            if (ints.Length == 0)
            {
                float distI;
                s.s.NearestPoint(s1.s.GetPoint(0), out distI);
                if (distI / Math.Max(s1.bbox.Height, s.bbox.Width) > 0.125)
                    return false;
            }
            if (ints.Length == 0)
            {
                if ((s.bbox.Left - s1.bbox.Right + 0.0) / s1.bbox.Height > .05 || s.bbox.Right < s1.bbox.Right || (s.bbox.Top - s1.bbox.Top + 0.0) / s1.bbox.Height > 0.5)
                    return false;
            }
            else
            {
                Point interPt = getPt(ints[ints.Length - 1], s1.s.GetPoints());
                double stemRatio = (interPt.Y - s1.bbox.Top + 0.0) / s1.bbox.Height; // smaller is more of a T
                if (tmpAllograph == "T" && stemRatio > 0.19 * Math.Pow(1 / (1 + stemStraightness), 7))
                    return false;
                else if (tmpAllograph == "J" && stemRatio > 0.19)
                    return false;
            }
            Recognition r = Classification(s.s);
            if (r != null && r.strokes.Count > 1)
            { // if we're breaking apart an '=', we have to update the top stroke to be a '-'
                if (r.strokes.Count > 2)
                    return false;
                Strokes newrec = r.strokes.Ink.CreateStrokes();
                foreach (Stroke sr in r.strokes)
                    if (sr.Id != s.s.Id)
                        newrec.Add(sr);
                FullClassify(r.strokes[0], false);//new Recognition(newrec, "-", newrec.GetBoundingBox().Bottom,
                //newrec.GetBoundingBox().Top, false));
            }
            stks = s1.s.Ink.CreateStrokes(new int[] { s.s.Id, s1.s.Id });
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            allograph = tmpAllograph;
            return true;
        }
        bool match2_Estart(CuspSet s, CuspSet s1, string letter, string other)
        {
            if (letter != "-" && letter != "~" && letter != "/" && letter != "\\" && letter != ".")
                return false;
            if (other != "L")
            {
                if (other[0] == '(' || other == "<")
                {
                    int ind;
                    if (angle(s1.cusps[1].pt, s1.cusps[0].pt, new PointF(-1, 0)) < 25)
                        return false;
                    double dist = V2D.MaxDist(s1.pts, V2D.Normalize(V2D.Sub(s1.last, s1.pts[0])), out ind);
                    if (V2D.Straightness(s1.pts) < 0.25 || angle(s1.last, s1.pts[ind], new PointF(1, 0)) > 35)
                        return false;
                    if (s1.pts.Count() < 6 || angle(s1.pts[5], s1.pts[0], new PointF(-1, 0)) < 15)
                        return false;
                }
                else
                    return false;
            }
            if (s1.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id })).Length == 0)
            {
                float ndist;
                s1.s.NearestPoint(s.s.GetPoint(0), out ndist);
                if (ndist / s1.bbox.Width > 0.2 && ndist / s.bbox.Width > 0.2)
                    return false;
            }
            if (letter == "." && s.bbox.Bottom < s1.bbox.Top && (s.bbox.Width + 0.0) / s.bbox.Height < 3)
                return false;
            if ((s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s1.bbox.Height < 0.2)
                return false;
            return true;
        }

        bool match2_memberof(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (letter != "-" || (other[0] != 'c' && other[0] != '(') || s1.bbox.Width / (float)s1.bbox.Height < 0.45)
                return false;
            int botind; maxy(0, s1.pts.Length, s1.pts, out botind);
            if (botind == -1)
                return false;
            if (Math.Abs(s1.angles[botind]) < 2.8 && Math.Abs(s1.angles[botind - 1]) < 2.8)
                return false;
            Point bot = s1.pts[0].Y < s1.last.Y ? s1.last : s1.pts[0];
            if ((bot.X - s1.bbox.Left + 0.0) / s1.bbox.Width < 0.25)
                return false;
            if (s.bbox.Width / (float)s1.bbox.Width > 1.35)
                return false;
            Rectangle overlap = Rectangle.Intersect(s1.bbox, s.bbox);
            if ((overlap.Width + Math.Max(0, s.bbox.Right - s1.bbox.Right)) / (float)(s.bbox.Width) < 0.75 || (overlap.Width / (float)s1.bbox.Width) < 0.5)
                return false;
            return true;
        }


        bool match2_I(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (letter != "-" && letter != "~" && letter != ".")
                return false;
            if (other == "+")
            {
                Recognition pl = Classification(s1.s);
                Stroke bar = pl.strokes[0];
                if (pl.strokes[0].GetBoundingBox().Height > pl.strokes[1].GetBoundingBox().Height)
                    bar = pl.strokes[1];
                double barhgt = (bar.GetBoundingBox().Top - pl.strokes.GetBoundingBox().Top + 0.0);
                if (barhgt / pl.strokes.GetBoundingBox().Height > 0.33 && barhgt / InkPixel > 3)
                    return false;
            }
            if (other != "T" && other != "+" && other != "1" && (other != "J" || s1.straight > 0.3))
            {
                if (other != "perp")
                    return false;
                else
                {
                    if (Math.Abs(s1.bbox.Top - s.bbox.Top + 0.0) / s1.bbox.Height >= .2)
                        return false;
                }
            }
            else if (Math.Abs(s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s1.bbox.Height >= .33)
                return false;
            Rectangle box = s.s.GetBoundingBox();
            box = Rectangle.Union(box, s1.s.GetBoundingBox());
            box = new Rectangle(new Point(box.Left + box.Width / 4, box.Bottom), new Size(box.Width / 2, box.Height));
            Strokes below = filter(s.s.Ink.HitTest(box, 1));
            below.Remove(s.s);
            below.Remove(s1.s);
            if (below.Count > 1)
            {
                int which = 0;
                int bot = below[0].GetBoundingBox().Bottom;
                for (int i = 1; i < below.Count; i++)
                    if (below[i].GetBoundingBox().Bottom < bot)
                    {
                        bot = below[i].GetBoundingBox().Bottom;
                        which = i;
                    }
                below = below.Ink.CreateStrokes(new int[] { below[which].Id });
            }
            if (below.Count == 1 && below.GetBoundingBox().Width / (float)below.GetBoundingBox().Height < 3)
                return false;
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        bool match2_E(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (letter != "/" && letter != "-" && letter != "~" && letter != ".")
                return false;
            if (other != "Estart" && other != "t")
                if (!match_lb(s1))
                    return false;
            if (letter == ".")
            {
                if (s1.dist / V2D.Dist(s1.last, s1.pts[0]) > 1.3)
                    return false;
            }
            float dist;
            Point closest = s.pts[0].X < s.last.X ? s.pts[0] : s.last;
            Point near = getPt(s1.s.NearestPoint(closest, out dist), s1.s.GetPoints());
            if (closest.X > near.X && dist / s.bbox.Width > 0.1 && (s.bbox.Right - s1.bbox.Right + 0.0) / s.bbox.Width > 0.5)
                return false;
            //basically, make sure the new stroke(the second/middle line of the E) is is the middle
            if ((s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s.bbox.Height < 0.2)
                return false;
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }

        bool match2_exists(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {

            CuspSet bigguy, littleguy;
            String littleString;

            if (letter == "-" || letter == "~" || letter == ".")
            {
                if (other != "]")
                    return false;

                littleguy = s1;
                bigguy = s;
                littleString = letter;


            }
            else if (letter == "]")
            {
                if (!(other == "-" || other == "~" || other == "."))
                    return false;

                littleguy = s;
                bigguy = s1;
                littleString = other;

            }
            else
            {
                return false;
            }

            if (littleString == ".")
            {
                if (littleguy.dist / V2D.Dist(littleguy.last, littleguy.pts[0]) > 1.3)
                    return false;
            }
            //basically, make sure the new stroke(the second/middle line of the exists) is is the middle
            if ((littleguy.bbox.Bottom - bigguy.bbox.Bottom + 0.0) / bigguy.bbox.Height < 0.2)
                return false;
            xhgt = (littleguy.bbox.Top + littleguy.bbox.Bottom) / 2;
            return true;
        }




        bool match2_F(CuspSet s, CuspSet s1, string letter, string other, Recognition precog, ref int xhgt)
        {
            if (letter != "-" && letter != "~" && letter != ".")
                return false;
            if (other != "T" && other != "t" && other != "r" && other != "sqrt")
                return false;
            if (s1.s.SelfIntersections.Length > 0 && s1.bbox.Width / (float)s.bbox.Width < 0.5)
                return false;
            Stroke stem = s1.s;
            if (precog.strokes.Count > 1)
            {
                if (precog.strokes[0].GetBoundingBox().Height > precog.strokes[1].GetBoundingBox().Height)
                    stem = precog.strokes[0];
                else stem = precog.strokes[1];
            }
            float dist;
            Point near = getPt(stem.NearestPoint(s.pts[0].X < s.last.X ? s.pts[0] : s.last, out dist), stem.GetPoints());
            if (dist / s.bbox.Width > 0.1 && (s.bbox.Right - s1.bbox.Right + 0.0) / s.bbox.Width > 0.5)
                return false;
            if (other == "r" || other == "sqrt")
            {
                Point topRight = s1.last.Y < s1.pts[0].Y ? s1.last : s1.pts[0];
                int maxyind; maxy(0, s1.pts.Length, s1.pts, out maxyind);
                Point botLeft = s1.pts[maxyind];//new Point(s1.bbox.Left, s1.bbox.Bottom);
                int looplobeind;
                bool left;
                V2D.MaxDist(s1.pts, botLeft, V2D.Normalize(V2D.Sub(botLeft, topRight)), out left, out looplobeind, 0, s1.pts.Length);
                if ((s1.distances[looplobeind] - s1.cusps[1].dist) / s1.bbox.Height < 0.2)
                    looplobeind = s1.cusps[1].index;
                float[] ints = s.s.FindIntersections(s1.s.Ink.CreateStrokes(new int[] { s1.s.Id }));
                if (ints.Length != 0)
                {
                    Point interpt = getPt(ints[0], s.s.GetPoints());
                    double overhang = V2D.Dist(interpt, s.pts[0]) / s.bbox.Width;
                    double angles = angle(interpt, s1.pts[looplobeind], new PointF(0, 1)) +
                                            angle(topRight, s1.pts[looplobeind], new PointF(1, 0));
                    if (angle(s1.pts[Math.Max(looplobeind - 4, 0)], s1.pts[looplobeind],
                        V2D.Sub(s1.pts[Math.Min(s1.pts.Length - 1, looplobeind + 4)], s1.pts[looplobeind])) > 120)
                        return false;
                    if (V2D.Straightness(s1.pts, looplobeind, (2 * s1.pts.Length + looplobeind) / 3) > 0.12 ||
                         (overhang > 0.1 && angles > 15))
                        return false;
                    double stemang = Math.Max(angle(interpt, s1.pts[looplobeind], new PointF(0, 1)), angle(botLeft, s1.pts[looplobeind], new PointF(0, 1)));
                    if (overhang > 0.3 && stemang > 8)
                        return false;
                }
                else
                {
                    Point nearpt = getPt(s1.s.NearestPoint(s.s.GetPoint(0)), s1.s.GetPoints());
                    if (V2D.Dist(nearpt, s.s.GetPoint(0)) / s1.bbox.Width > Math.Max(0.1, InkPixel * 2 / s1.bbox.Width) && !s1.bbox.Contains(s.bbox))
                        return false;
                    double roofAng = angle(s1.pts[looplobeind], topRight, new Point(-1, 0));
                    if (roofAng > 45)
                        return false;
                }
            }
            if (letter == ".")
            {
                if (s1.s.SelfIntersections.Length > 0 && s1.bbox.Width / (float)s.bbox.Width < 0.5)
                    return false;
                if (s1.dist / V2D.Dist(s1.last, s1.pts[0]) > 1.3)
                    return false;
            }
            double crossRatio = (stem.GetBoundingBox().Bottom - s.bbox.Bottom + 0.0) / stem.GetBoundingBox().Height;
            if (crossRatio < 0.2 || crossRatio > 0.9)
                return false;
            xhgt = (stem.GetBoundingBox().Top + stem.GetBoundingBox().Bottom) / 2;
            return true;
        }
        bool match2_lambda(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            double a1 = angle(s1.last, s1.pts[0], new Point(1, 1));
            double a = angle(s.last, s.pts[0], new Point(1, 1));
            if (a1 > a)
            {
                CuspSet tmp = s;
                s = s1;
                s1 = tmp;
                string tmps = letter;
                letter = other;
                other = tmps;
            }
            if (s.straight > 0.2)
                return false;
            if (s.bbox.Top > s1.bbox.Bottom || s.bbox.Bottom < s1.bbox.Top)
                return false;
            if (s1.bbox.Width / (float)s1.bbox.Height > 1.5)
                return false;
            double crossang = angle(s.last, s.pts[0], new PointF(-1, 0));
            if (crossang <= 35 || crossang > 90)
                return false;
            Point inter;
            float[] ints = s.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s1.s.Id }));
            if (ints.Length == 0)
            {
                float dist;
                inter = getPt(s1.s.NearestPoint(s.pts[0], out dist), s1.s.GetPoints());
                if (dist / Math.Max(s.dist, inter.X - s1.bbox.Left) > 0.19)
                    return false;
            }
            else
            {
                inter = getPt(ints[0], s.s.GetPoints());

                float[] ints2 = s1.s.FindIntersections(s1.s.Ink.CreateStrokes(new int[] { s.s.Id }));
                int ind1 = convertIndexBack((int)ints[0], s.skipped);
                int ind2 = convertIndexBack((int)ints2[0], s1.skipped);
                if ((ind1 != -1 && s.distances[ind1] / s.bbox.Width > (s1.pts[ind2].Y - s1.bbox.Top) / (float)s1.bbox.Height * 5 / 9) || (ind2 != -1 && s1.distances[ind2] / s1.dist < .2))
                    return false;

            }
            Point tr = new Point(s.bbox.Right, s.bbox.Top);
            Point bl = new Point(s.bbox.Left, s.bbox.Bottom);
            Point br = new Point(s1.bbox.Right, s1.bbox.Bottom);
            double barang = angle(inter, bl, V2D.Sub(inter, br));
            if (Math.Abs(barang - 45) > 45)
                return false;
            double descender = (br.Y - bl.Y + 0.0) / s1.bbox.Height;
            if (descender < -0.4 || (descender < -0.25 && descender + s.bbox.Height / (float)s1.bbox.Height > 0.65))
                return false;
            double interRatio = (s1.bbox.Bottom - inter.Y + 0.0) / s1.bbox.Height;
            if (interRatio < 0.2 || interRatio > 0.85)
                return false;
            xhgt = s.s.GetBoundingBox().Top;
            return true;
        }
        bool match2_f(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (s1.bbox.Height < s.bbox.Height)
            {
                string tmp = letter;
                letter = other;
                other = tmp;
                CuspSet tc = s;
                s = s1;
                s1 = tc;
                if ((s1.bbox.Height - s.bbox.Width + 0.0) / s1.bbox.Height < 0.2)
                    return false;
            }
            if (other != "[" && other != "c" && other != "fbase" && other != "/" && !other.Contains("(") && other != "r" && !other.Contains("INT") && other != "s" &&
                other != "sqrt" && other != "P" && other != "p" && other != "^")
                return false;
            Point inter;
            float[] ints = s1.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id }));
            if (letter != "-" && letter != "~" && letter != "\\" && letter != "DIV" && letter != "/")
            {
                bool left;
                if (ints.Length == 0 || V2D.Straightness(s1.pts, out left) < 0.12 || !left || (letter != "<" && letter != "c" && letter != "L" && letter != "r"))
                    return false;
            }
            double stemStr = V2D.Straightness(s1.pts);
            if (stemStr < 0.1)
                return false;
            if (stemStr < .12)
            {
                int topind, ty = miny(0, s1.pts.Length, s1.pts, out topind);
                if (s1.curvatures[topind] < 0 && Math.Abs(s1.angles[topind]) > Math.PI / 6)
                    return false;
                if (s1.curvatures[topind] > 0 && Math.Abs(s1.angles[topind]) < Math.PI - Math.PI / 6)
                    return false;
            }
            if (s1.cusps.Length > 5)
                return false;
            if (s1.pts[0].Y > s1.last.Y)
            {
                if (s1.cusps[s1.cusps.Length - 1].bot)
                    return false;
            }
            else if (s1.cusps[0].bot)
                return false;
            if (s.bbox.Top > s1.bbox.Bottom || s.bbox.Bottom < s1.bbox.Top)
                return false;
            if (angle(s.last, s.pts[0], new PointF(1, 0)) > 45 && angle(s.pts[0], s.last, new PointF(1, 0)) > 45 && s.bbox.Width / (float)s1.bbox.Height > .2)
                return false;
            if (ints.Length == 0)
            {
                float dist;
                inter = getPt(s1.s.NearestPoint(s.pts[0], out dist), s1.s.GetPoints());
                if (dist / s1.bbox.Width > Math.Min(.15, InkPixel * 20 / s1.bbox.Width))
                    return false;
            }
            else inter = getPt(ints[0], s1.s.GetPoints());
            if ((ints.Length == 0 || (inter.X - s.bbox.Left + 0.0) / s.bbox.Width > 0.8) && s.straight > 0.3)
                return false;
            if ((s1.bbox.Bottom - inter.Y + 0.0) / s1.bbox.Height < 0.1)
                return false;
            xhgt = s.s.GetBoundingBox().Top;
            return true;
        }
        bool match2_G(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (!letter.Contains("7") && letter != ">" && letter != "superset")
                return false;
            if (other[0] != 'c' && other != "b6" && other != "6")
                return false;
            if ((s.bbox.Top - s1.bbox.Top + 0.0) / s1.bbox.Height < 0.25)
                return false;
            float dist;
            s.s.NearestPoint(s1.last, out dist);
            if (dist / s1.bbox.Width > .5)
                return false;
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        bool match2_P(CuspSet s, CuspSet s1, string letter, string other, bool intersects, ref int xhgt)
        {
            if (s.bbox.Height > s1.bbox.Height)
            {
                string tmp = letter;
                letter = other;
                other = tmp;
                CuspSet tc = s;
                s = s1;
                s1 = tc;
            }
            if (!letter.Contains(")") && letter != ">" && letter != "superset" && letter != "7" && letter != "0")
                return false;
            if (letter == "0" && ((s.bbox.Top - s1.bbox.Top + 0.0) / s1.bbox.Height > 0.15 || (s1.bbox.Right - s.bbox.Left + 0.0) / s.bbox.Width > 0.255))
                return false;
            double elevation = (s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s1.bbox.Height;
            if (elevation < 0.25)
                return false;
            if (!intersects)
            {
                float dist1; s1.s.NearestPoint(s.s.GetPoint(0), out dist1);
                float dist2; s1.s.NearestPoint(s.s.GetPoint(s.s.GetPoints().Length - 1), out dist2);
                double maxdist = Math.Min((3 + (elevation - .25) * 5) * InkPixel, s.bbox.Height / 3);
                if (dist1 > maxdist && dist2 > maxdist)
                    return false;
            }
            if (other != "" && !other.Contains("1") && other[0] != '(' && other != "/")
                return false;
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        bool match2_phi(CuspSet s, CuspSet s1, string letter, string other, bool intersection, ref string allograph)
        {
            if (!intersection)
                return false;
            CuspSet vert, circle;
            if (letter.Contains("1") && other.Contains("0"))
            {
                vert = s; circle = s1;
            }
            else if (other.Contains("1") && letter.Contains("0"))
            {
                vert = s1; circle = s;
            }
            else { return false; }

            if (!intersection && !s1.bbox.Contains(s.bbox))
                return false;

            double vang = angle(s.pts[0], s.last, new PointF(0, -1));
            if (vang > 20 && vang < 160)
                return false;

            double heightRatio = vert.bbox.Height / (float)circle.bbox.Height;
            if (heightRatio >= 1.3)
            {
                if (!intersection)
                    return false;
                allograph = "phi";
            }
            else if (heightRatio >= 0.5 && heightRatio < 1.3) { allograph = "circledVertBar"; }
            else { return false; }

            return true;
        }

        bool match3_notequal(CuspSet s, CuspSet s1, string letter, Recognition other, ref Strokes stks, ref int xhgt)
        {
            if (letter != "/")
                return false;
            Strokes combined = s.s.Ink.CreateStrokes();
            combined.Add(s.s);
            if (other.allograph != "=")
            {
                combined.Add(s1.s);
                Strokes tmp = s.s.Ink.HitTest(combined.GetBoundingBox(), 75);
                tmp.Remove(combined);
                if (tmp.Count != 1)
                    return false;
                if (tmp[0].GetBoundingBox().Width / (float)tmp[0].GetBoundingBox().Height < 2.5)
                    return false;
                combined.Add(tmp);
            }
            else
                combined.Add(Classification(s1.s).strokes);
            Rectangle eqBox = other.strokes.GetBoundingBox();
            stks = combined;
            return true;
        }
        bool match3_r2arrow(CuspSet s, CuspSet s1, string letter, Recognition other, ref Strokes stks, ref int xhgt)
        {
            if (!">7,) superset".Contains(letter))
                return false;
            Strokes combined = s.s.Ink.CreateStrokes();
            combined.Add(s.s);
            if (other.allograph != "=")
            {
                combined.Add(s1.s);
                Strokes tmp = s.s.Ink.HitTest(combined.GetBoundingBox(), 75);
                tmp.Remove(combined);
                if (tmp.Count != 1)
                    return false;
                if (tmp[0].GetBoundingBox().Width / (float)tmp[0].GetBoundingBox().Height < 2.5)
                    return false;
                combined.Add(tmp);
            }
            else
                combined.Add(Classification(s1.s).strokes);
            Rectangle eqBox = other.strokes.GetBoundingBox();
            if (s.bbox.Left < eqBox.Left || s.bbox.Left > eqBox.Right ||
                s.bbox.Top > eqBox.Top || s.bbox.Bottom < eqBox.Bottom ||
                s.bbox.Right < eqBox.Right)
                return false;
            if (!letter.Contains("7") && (s.bbox.Left - eqBox.Right + 0.0) / s.bbox.Width > -0.25)
                return false;
            stks = combined;
            return true;
        }
        bool match2_r1arrow(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            // FIXME: alternates don't have a mechanism for interpreting result as multiple characters
            if (!">7), superset".Contains(letter) || other != "-")
                return false;
            if (letter == "7" && s1.bbox.Width < s.bbox.Width && (s1.bbox.Right - s.bbox.Left + 0.0) / s.bbox.Width > 0)
                return false;
            double botRatio = (s.bbox.Bottom - (s1.pts[0].X < s1.last.X ? s1.last.Y : s1.pts[0].Y) + 0.0) / s.bbox.Height;
            double topRatio = ((s1.pts[0].X < s1.last.X ? s1.last.Y : s1.pts[0].Y) - s.bbox.Top + 0.0) / s.bbox.Height;
            if (s.bbox.Left < s1.bbox.Left || s.bbox.Left > s1.bbox.Right || botRatio < 0.1 || topRatio < 0.1)
                return false;
            if (filter(s.s.Ink.HitTest(Rectangle.Union(s1.bbox, s.bbox), 10)).Count > 2)
                return false;
            return true;
        }
        bool match2_l1arrow(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            // FIXME: alternates don't have a mechanism for interpreting result as multiple characters
            if (System.Windows.Forms.SystemInformation.UserName != "Tim" && s.bbox.Width < s1.bbox.Height)
                return false;
            if (!"<c( subset".Contains(other) || letter != "-")
                if ("<c( subset".Contains(letter) && other == "-")
                {
                    CuspSet tmp = s;
                    s = s1;
                    s1 = tmp;
                    string let = letter;
                    letter = other;
                    other = let;
                }
                else
                    return false;
            double botRatio = (s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s1.bbox.Height;
            double topRatio = (s.bbox.Top - s1.bbox.Top + 0.0) / s1.bbox.Height;
            if (s1.bbox.Right > s.bbox.Right || s1.bbox.Right < s.bbox.Left || botRatio < 0.1 || topRatio < 0.1)
                return false;
            if ((s1.bbox.Left - s.bbox.Left + 0.0) / s.bbox.Width > 0.2)
                return false;
            if (filter(s1.s.Ink.HitTest(Rectangle.Union(s.bbox, s1.bbox), 10)).Count > 2)
                return false;
            float[] isects = s.s.FindIntersections(s1.s.Ink.CreateStrokes(new int[] { s1.s.Id }));
            if (isects.Length != 0)
            {
                double dist = s.distances[convertIndexBack((int)isects[0], s.skipped)];
                if (dist / s.dist > 0.1 && dist / s.dist < 0.9) return false;
            }
            xhgt = s.bbox.Top + s.bbox.Height / 2;
            return true;
        }
        bool match2_D(CuspSet s, CuspSet s1, string letter, string other, bool intersects, ref int xhgt)
        {
            if (!letter.Contains(")") && letter != ">" && letter != "superset")
                return false;
            if (s.straight < 0.2)
                return false;
            if (other[0] != '1' && other[0] != '(' && other != "/")
                return false;
            if (!intersects && letter != "superset")
            {
                float dist1; s1.s.NearestPoint(s.s.GetPoint(0), out dist1);
                float dist2; s1.s.NearestPoint(s.s.GetPoint(s.s.GetPoints().Length - 1), out dist2);
                if (dist1 > InkPixel * 3 && dist2 > InkPixel * 3)
                    return false;
            }
            if ((s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s1.bbox.Height > 0.25 ||
                (s.bbox.Top - s1.bbox.Top + 0.0) / s1.bbox.Height > .25)
                return false;
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        bool match2_theta(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt, ref string allograph)
        {

            CuspSet bar, circle;
            if ((letter.Contains("-") || letter.Contains("~")) && other.Contains("0"))
            {
                bar = s; circle = s1;
            }
            else if ((other.Contains("-") || other.Contains("~")) && letter.Contains("0"))
            {
                bar = s1; circle = s;
            }
            else { return false; }

            int linehgt = (bar.bbox.Top + bar.bbox.Bottom) / 2;
            if (linehgt - circle.bbox.Top < 0.2 * circle.bbox.Height) return false;
            if (linehgt - circle.bbox.Top > 0.8 * circle.bbox.Height) return false;

            xhgt = linehgt;

            if (bar.bbox.Width / (float)circle.bbox.Width < 0.7)
                allograph = "Θ";
            else if (circle.bbox.Width / (float)circle.bbox.Height < 0.75)
                allograph = "θ";
            else { allograph = "circledMinus"; }

            return true;
        }
        bool match2_b(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (!letter.Contains(")") && letter != ">" && letter != "superset")
                return false;
            if (s.straight < 0.15)
                return false;
            if (!other.Contains("1") && !other.Contains("(") && other != "/")
                return false;
            if (true)
            { // !intersects) {
                float dist1; s1.s.NearestPoint(s.s.GetPoint(0), out dist1);
                float dist2; s1.s.NearestPoint(s.s.GetPoint(s.s.GetPoints().Length - 1), out dist2);
                if (dist1 > InkPixel * 3 && dist2 > InkPixel * 3)
                    return false;
            }
            if ((s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s1.bbox.Height > 0.25 ||
                (s.bbox.Top - s1.bbox.Top + 0.0) / s1.bbox.Height < .25)
                return false;
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        bool match2_B(CuspSet s, CuspSet s1, string letter, string other, bool intersects, ref string allograph, ref int xhgt)
        {
            if (letter != "3")
                return false;
            if (!other.Contains("1") && other[0] != '(' && other != "/")
                return false;
            if (!intersects)
            {
                float distT1, distT2;
                float distB1, distB2;
                s1.s.NearestPoint(s.s.GetPoint(0), out distT1);
                s.s.NearestPoint(s1.s.GetPoint(0), out distT2);
                s1.s.NearestPoint(s.last, out distB1);
                s.s.NearestPoint(s1.last, out distB2);
                double nearThresh = Math.Max(InkPixel * 4 / s.bbox.Width, 0.1);
                if (distT1 / s.bbox.Width > nearThresh && distT2 / s.bbox.Width > nearThresh)
                    return false;
                if (distB1 / s.bbox.Width > nearThresh && distB2 / s.bbox.Width > nearThresh)
                    return false;
            }
            if ((s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s1.bbox.Height > 0.2 || (s1.straight > 0.15 && other[0] == '('))
                allograph = "beta";
            else allograph = "B";
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        bool match2_Q(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (other != "0")
                return false;
            if (s.pts[0].X > s1.bbox.Left + s1.bbox.Width * .2 && s.pts[0].Y > s1.bbox.Top + s1.bbox.Height * .2 &&
                s.pts[0].X < s1.bbox.Right - s1.bbox.Width * .2 && s.pts[0].Y < s1.bbox.Bottom - s1.bbox.Height * .1 &&
                s.last.X > s1.bbox.Right - s1.bbox.Width / 2 && s.last.Y > (s1.bbox.Top + 3 * s1.bbox.Height / 4))
            {
                xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
                return true;
            }
            return false;
        }
        bool match2_a(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            CuspSet c = s;
            CuspSet tail = s1;
            if ("c0".IndexOf(other[0]) != -1)
            {
                c = s1;
                tail = s;
            }
            else if (!letter.Contains("c") && !letter.Contains("0"))
                return false;
            float dist1, dist2;
            tail.s.NearestPoint(c.s.GetPoint(0), out dist1);
            tail.s.NearestPoint(c.s.GetPoint(c.s.GetPoints().Length - 1), out dist2);
            if (dist1 / tail.bbox.Height > 0.12 || dist2 / tail.bbox.Height > 0.12)
                return false;
            if ((tail.bbox.Bottom - c.pts[0].Y + 0.0) / tail.bbox.Height < 0.15)
                return false;
            if ((tail.cusps.Length > 4 && V2D.Straightness(tail.pts) > 0.4) || tail.cusps[0].pt.Y > tail.cusps[tail.l].pt.Y)
                return false;
            if ((tail.angles[0] < .6 && tail.angles[0] > -2.3) || tail.cusps[0].bot || tail.cusps[tail.l].top || tail.intersects.Length > 0)
                return false;
            if (c.bbox.Top < tail.bbox.Top || c.pts[0].X > tail.bbox.Right)
                return false;
            return true;
        }
        bool match2_d(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            CuspSet c = s;
            CuspSet tail = s1;
            if ((letter != "" && "c0".IndexOf(letter[0]) == -1) || (!other.Contains("1") && !other.Contains(")") && !other.Contains("(")))
                return false;
            if (tail.cusps.Length > 4 || tail.cusps[0].pt.Y > tail.cusps[tail.l].pt.Y)
                return false;
            bool left;
            if (V2D.Straightness(tail.pts, out left) > 0.2 && !left)
                return false;
            if (c.bbox.Top < tail.bbox.Top ||
                c.bbox.Left > tail.bbox.Left)
                return false;
            return true;
        }
        bool match3_bbRk(CuspSet cs, ref string letter, ref Strokes instks, ref int midpt)
        {
            if (instks.Count != 3) return false;
            Stroke s2 = null, s3 = null;
            foreach (Stroke s in instks) if (s.Id != cs.s.Id) { if (s2 == null) s2 = s; else s3 = s; }
            if (s2 == null) return false;
            if (s3 == null) return false;
            CuspSet cs2 = FeaturePoints(s2);
            CuspSet cs3 = FeaturePoints(s3);
            Recognition r2 = Classification(s2);
            Recognition r3 = Classification(s3);
            if (r2 == null || r3 == null || r2.levelsetby == 0 || r3.levelsetby == 0) return false;
            string o2 = r2.allograph, o3 = r3.allograph;
            bool i2, i3;
            Strokes tst;
            tst = cs.s.Ink.CreateStrokes(new int[] { s2.Id });
            i2 = cs.s.FindIntersections(tst).Length > 0;
            tst = cs.s.Ink.CreateStrokes(new int[] { s3.Id });
            i3 = cs.s.FindIntersections(tst).Length > 0;

            /* check if vert lines are so and parallel, taken from match2_eq */
            if (o2 == null || (!o2.Contains("1") && !o2.Contains("/")))
                return false;
            if (o3 == null || (!o3.Contains("1") && !o3.Contains("/")))
                return false;
            Rectangle dummy = cs3.bbox; dummy.Inflate(10000, 0);
            double ratio = (Rectangle.Intersect(dummy, cs2.bbox).Height + 0.0) / Math.Min(cs3.bbox.Height, cs2.bbox.Height);
            if (ratio < 0.3)
                return false;
            if (cs3.bbox.Width / Math.Max(cs2.bbox.Height, cs2.bbox.Width) > 2 || cs2.bbox.Width / Math.Max(cs3.bbox.Height, cs3.bbox.Width) > 2)
                return false;
            PointF d1 = V2D.Sub(cs3.last, cs3.pts[0]);
            PointF d2 = V2D.Sub(cs2.pts[cs2.pts.Length - 1], cs2.pts[0]);
            if (d1.Y < 0)
            {
                d1 = new PointF(-d1.X, -d1.Y);
                d2 = new PointF(-d2.X, -d2.Y);
            }
            double lRatio = V2D.Length(d1) / V2D.Length(d2);
            if (lRatio > 1) lRatio = 1 / lRatio;
            if (lRatio < 0.5 * Math.Min(1, (V2D.Length(d1) + V2D.Length(d2)) / InkPixel / 75))
                return false;
            double ang1 = angle(V2D.Normalize(d1), new PointF(0, 1));
            double ang2 = angle(V2D.Normalize(d1), V2D.Normalize(d2));
            if (ang1 > 45 || ang2 > 45)
                return false;
            int mi_y = Math.Min(cs3.s.GetBoundingBox().Top, cs2.s.GetBoundingBox().Top);
            int ma_y = Math.Max(cs3.s.GetBoundingBox().Bottom, cs2.s.GetBoundingBox().Bottom);

            /* check if at least one of the lines makes an R */
            string rkstr = null;
            int x = 0;
            if (!(match2_Rk(cs, cs2, letter, o2, i2, ref x, ref rkstr) && rkstr == "R")
                && !(match2_Rk(cs, cs3, letter, o3, i3, ref x, ref rkstr) && rkstr == "R")) return false;

            /* The curved part of the R must either extend to the left of the leftmost vertical stroke, or intersect it. */
            //CuspSet left = cs2.bbox.Left < cs3.bbox.Left ? cs2 : cs3;
            //if(cs.bbox.Left > left.bbox.Left && !(left == cs2 ? i2 : i3)) return false;

            /* The first point of the curvy stroke and the point where the curvy stroke comes back
             * furthest to the left must both be closer to the first straight stroke than the second
             straight one.*/

            //Construct Point A (First point of the curvy stroke)
            Point A = cs.pts[0];

            //Construct Point B (Point where the curvy stroke comes back furthest to the left)
            int t1xind, t1x = minx(cs.pts.Length / 4, cs.pts.Length, cs.pts, out t1xind);
            Point B = cs.pts[t1xind];

            CuspSet right = cs2.bbox.Left > cs3.bbox.Left ? cs2 : cs3;
            int rightXIndex = (right.bbox.Left + right.bbox.Right) / 2;

            if (A.X > rightXIndex || B.X > rightXIndex)
                return false;

            letter = "bbR";
            midpt = instks.GetBoundingBox().Top + instks.GetBoundingBox().Height / 2;
            return true;
        }
        bool match2_Rk(CuspSet s, CuspSet s1, string letter, string other, bool intersection, ref int xhgt, ref string allograph)
        {
            if (!letter.Contains("2") && letter != "7" && letter != "}" && !letter.Contains("z") && !letter.Contains("<") && !letter.Contains("a"))
                return false;
            if (!other.Contains("1") && !other.Contains("/"))
                return false;
            if ((s.last.Y - s1.bbox.Top + 0.0) / s1.bbox.Height < 0.65)
                return false;
            if (!intersection)
            {
                int minind, mx = minx((int)(.25 * s.pts.Length), (int)(.75 * s.pts.Length), s.pts, out minind);
                float dist, dist2;
                if ((s.pts[minind].Y - s1.pts[0].Y + 0.0) / s1.bbox.Height > 0.75)
                    return false;
                s1.s.NearestPoint(s.pts[minind], out dist);
                s.s.NearestPoint(s1.pts[0], out dist2);
                if ((Math.Min(dist, dist2) / s.bbox.Width > 0.3 ||
                    (Math.Min(mx, s.pts[0].X) - s1.bbox.Right) / (float)s.bbox.Width > 0.1))
                    return false;
            }
            if ((s.bbox.Top - s1.bbox.Top + 0.0) / s1.bbox.Height > 0.325)
                allograph = "k";
            else allograph = "R";
            return true;
        }
        bool match2_A(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (s1.cusps.Length < 3)
                return false;
            int apexCusp = 1;
            if (s1.cusps[0].top && s1.cusps[1].bot && s1.cusps[2].top)
                apexCusp = 2;
            if (apexCusp == 2 && angle(s1.cusps[1].pt, s1.cusps[0].pt, V2D.Sub(s1.cusps[1].pt, s1.cusps[2].pt)) > 15)
                return false;
            Point[] hull = new Point[] { s1.cusps[apexCusp - 1].pt, s1.cusps[apexCusp].pt, s1.last };
            Strokes overlap = filter(s1.s.Ink.HitTest(hull, 40));
            if (!overlap.Contains(s.s) && s1.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id })).Length < 2)
                return false;
            if (s1.cusps.Length > 2 && (s.bbox.Width / (s.bbox.Height + 0.0001) + 0.0) > 0.9 && s1.cusps[apexCusp - 1].pt.X < s1.cusps[apexCusp].pt.X)
            {
                if (other == "n")
                {
                    xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
                    return true;
                }

                float[] ints = s1.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id }));
                Point p1 = ints.Length == 2 ? getPt(ints[0], s1.s.GetPoints()) : s1.pts[Math.Max(0, s1.cusps[apexCusp].index - 3)];
                Point p2 = ints.Length == 2 ? getPt(ints[1], s1.s.GetPoints()) : s1.pts[Math.Min(s1.pts.Length - 1, s1.cusps[apexCusp].index + 3)];
                double apexAng = angle(p1, s1.cusps[apexCusp].pt, V2D.Sub(p2, s1.cusps[apexCusp].pt));
                if (apexAng < 10 && (V2D.Straightness(s1.pts, 0, s1.cusps[apexCusp].index) > 0.12 || V2D.Straightness(s1.pts, s1.cusps[apexCusp].index, s1.pts.Length) > 0.12))
                    return false;
                if (s1.cusps[apexCusp - 1].bot && s1.cusps[s1.l].bot &&
                    s1.cusps[apexCusp].top &&
                    Math.Abs(s1.cusps[apexCusp].pt.X - (s1.bbox.Left + s1.bbox.Right) / 2 + 0.0) / s1.bbox.Width < .7 &&
                    Math.Abs(s1.cusps[apexCusp].curvature) > 0.2 &&
                    V2D.Straightness(s1.pts, s1.cusps[apexCusp - 1].index, s1.cusps[apexCusp].index) < .2 &&
                    V2D.Straightness(s1.pts, s1.cusps[apexCusp].index, s1.cusps[s1.l].index) < .3)
                {
                    xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
                    return true;
                }
            }
            return false;
        }


        bool intersectCheck(Line l1, Line l2)
        {

            double slope1 = (l1.EndPoint.Y - l1.BeginPoint.Y + 0.0) / (l1.EndPoint.X - l1.BeginPoint.X);
            double slope2 = (l2.EndPoint.Y - l2.BeginPoint.Y + 0.0) / (l2.EndPoint.X - l2.BeginPoint.X);

            if (slope1 == slope2)
                return false;

            double intercept1 = l1.EndPoint.Y - slope1 * l1.EndPoint.X;
            double intercept2 = l2.EndPoint.Y - slope2 * l2.EndPoint.X;

            double intersectX = (intercept2 - intercept1) / (slope1 - slope2);

            if (intersectX > Math.Max(l1.EndPoint.X, l1.BeginPoint.X) || intersectX > Math.Max(l2.EndPoint.X, l2.BeginPoint.X) ||
                intersectX < Math.Min(l1.EndPoint.X, l1.BeginPoint.X) || intersectX < Math.Min(l2.EndPoint.X, l2.BeginPoint.X))

                return false;


            return true;

        }




        bool match2_forall(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {

            if (other != "v" && letter != "v" && other != "~" && other != "sqrt")
            {
                return false;
            }

            if (letter == "v")
            {
                // Do people actually draw this in this order?
                CuspSet cs = s; s = s1; s1 = cs;
                string temp = letter; letter = other; other = letter;
            }


            int tipofvIndex, startind;
            maxy(0, s1.pts.Length - 1, s1.pts, out tipofvIndex);
            miny(0, tipofvIndex, s1.pts, out startind);
            if (startind == -1) startind = 0;
            if (startind > 0 && (V2D.Straightness(s1.pts, 0, startind) > 0.2 || (s1.pts[startind].Y - s1.bbox.Top + 0.0) / s1.bbox.Height > 0.25))
                return false;

            Line leftSideOfV = new Line(s1.pts[startind], s1.pts[tipofvIndex]);
            Line rightSideOfV = new Line(s1.last, s1.pts[tipofvIndex]);

            Line crossLine = new Line(s.pts[0], s.pts[s.pts.Length - 1]);

            if (crossLine.BeginPoint.X == crossLine.EndPoint.X) return false;
            float slope = Math.Abs(crossLine.BeginPoint.Y - crossLine.EndPoint.Y) / (float)Math.Abs(crossLine.BeginPoint.X - crossLine.EndPoint.X);
            if (slope > 0.6) return false;

            int mid = (crossLine.BeginPoint.Y + crossLine.EndPoint.Y) / 2;
            double frac = (mid - leftSideOfV.BeginPoint.Y) / (double)(leftSideOfV.EndPoint.Y - leftSideOfV.BeginPoint.Y);
            if (frac < 0.20 || frac > 0.75) return false;
            frac = (mid - rightSideOfV.BeginPoint.Y) / (double)(rightSideOfV.EndPoint.Y - rightSideOfV.BeginPoint.Y);
            if (frac < 0.20 || frac > 0.75) return false;

            bool hitsleft = intersectCheck(leftSideOfV, crossLine);
            bool hitsright = intersectCheck(rightSideOfV, crossLine);

            Point left = s.pts[0];
            Point right = s.last;
            if (left.X > right.X)
            {
                Point t = right;
                right = left;
                left = t;
            }

            float dist;
            if (!hitsleft)
            {
                s1.s.NearestPoint(left, out dist);
                if (dist > s.dist / 2) return false;
            }
            if (!hitsright)
            {
                s1.s.NearestPoint(right, out dist);
                if (dist > s.dist / 2) return false;
            }

            xhgt = (s.bbox.Top + s.bbox.Bottom) / 2;
            return true;
        }

        bool match2_k(CuspSet s, CuspSet s1, string letter, string other, bool intersection, ref int xhgt, ref string newmatch)
        {
            float[] ints = s1.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id }));
            if (letter == "")
                return false;
            if (!intersection && letter[0] == '(' && other[0] == ')')
                return false;
            float dist;
            int minind, mx = minx((int)(.25 * s.pts.Length), (int)(.75 * s.pts.Length), s.pts, out minind);
            if (minind < 0)
                return false;
            if ((s1.bbox.Bottom - s.bbox.Bottom + 0.0) / s1.bbox.Height > 0.4)
                return false;

            Point nearPt = getPt(s1.s.NearestPoint(s.pts[minind], out dist), s1.s.GetPoints());
            if (!intersection)
            {
                if (minind < 0 || minind >= s.pts.Length) // fix an outofbounds exception
                    return false;
                if (dist / s.bbox.Width > 0.3)
                    return false;
            }

            if (s1.straight > 0.3 && other != "l" && !other.Contains("7"))
                return false;
            if ((letter == "(" || letter.Contains("alpha") || letter == "fbase" || letter == "2" || letter == "L" || letter == "<" || letter.Contains("c") || (letter == "\\" && (ints.Length == 2 || s.s.GetPoint(0).X > s1.s.GetPoint(0).X))) &&
                 (other.Contains("1") || other == "\\" || other == "e" || other == "l" || other == "/" || other.Contains("(") || other.Contains(")") || other == "fbase" || other.Contains("INT")))
            {
                if (s.pts[0].X < s1.pts[0].X || V2D.Dist(s1.pts[0], s.pts[0]) / s1.bbox.Height < 0.2)
                    return false;
                xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
                if ((Math.Max(s.last.X, s.pts[0].X) - nearPt.X + 0.0) / s.bbox.Width < .25)
                    return false;
                if (!other.Contains("7") && s1.straight > 0.15 && s1.bbox.Width / (float)s.bbox.Width > 0.5 && s1.bbox.Height / (float)s.bbox.Height < 1.5)
                    newmatch = "xx";
                else if ((s.bbox.Top - s1.bbox.Top + 0.0) / s1.bbox.Height < 0.25)
                    newmatch = "K";
                else newmatch = "k2";
                return true;
            }
            return false;
        }
        bool match2_5(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt, ref Strokes stks)
        {
            if (s.bbox.Width / (float)s.bbox.Height < s1.bbox.Width / (float)s1.bbox.Height)
            {
                string tmp = letter;
                letter = other;
                other = tmp;
                CuspSet tmps = s;
                s = s1;
                s1 = tmps;
            }
            if ((letter != "/" && letter != "-" && letter != "~" && letter != "=") || (other != ")" && other != "," && other != "\\" && other != ">" && other != "y" && other != "b" && other != "s" && other != "}"))
                return false;
            if (other == "\\")
            {
                if ((s.bbox.Bottom - s1.bbox.Top + 0.0) / s1.bbox.Height > 0.3)
                    return false;
                int rightind; maxx(0, s1.pts.Length, s1.pts, out rightind);
                if (rightind > s1.pts.Length - 2 || s1.curvatures[rightind] < -Math.PI / 2 - Math.PI / 6)
                    return false;
            }
            if (other == ")" || other == ",")
            {
                int rightind; maxx(0, s1.pts.Length, s1.pts, out rightind);
                double stemang = angle(s1.pts[rightind], s1.pts[0], new PointF(1, 0));
                if (stemang > 60 || s1.distances[rightind] / s1.dist < 0.2)
                    return false;
            }
            if (other == ">" && angle(s.last, s.cusps[0].pt, new Point(1, -1)) > 45)
                return false;
            Rectangle box = new Rectangle(V2D.Add(s.bbox.Location, new Point(s.bbox.Width / 4, -s1.bbox.Height / 2)), new Size(s.bbox.Width / 2, s1.bbox.Height / 2));
            Strokes above = filter(s.s.Ink.HitTest(box, 1));
            above.Remove(s.s);
            above.Remove(s1.s);
            if (above.Count > 1)
            {
                int which = 0;
                int top = above[0].GetBoundingBox().Top;
                for (int i = 1; i < above.Count; i++)
                    if (above[i].GetBoundingBox().Top > top)
                    {
                        top = above[i].GetBoundingBox().Top;
                        which = i;
                    }
                above = above.Ink.CreateStrokes(new int[] { above[which].Id });
            }
            for (int i = s1.intersects.Length - 1; i >= 0; i--)
            {
                if (s1.intersects[i] < s1.pts.Length * 7 / 8)
                {
                    if (angle(s1.last, s1.pts[s1.intersects[i]], new Point(1, -1)) < 80)
                        return false;
                }
            }
            if (above.Count == 1 && Classification(above[0]) != null && above.GetBoundingBox().Width / (float)above.GetBoundingBox().Height < 3 &&
                Classification(above[0]).alt != Unicode.S.SQUARE_ROOT && Classification(above[0]).alt != Unicode.I.INTEGRAL &&
                Classification(above[0]).alt != Unicode.N.N_ARY_SUMMATION)
                return false;
            Point s1top = s1.pts[0].Y < s1.last.Y ? s1.pts[0] : s1.last;
            Point sleft = s.pts[0].X < s.last.X ? s.pts[0] : s.last;
            if ((sleft.X - s1top.X + 0.0) / s.bbox.Width > .6 || (sleft.X - s1top.X + 0.0) / s.bbox.Width < -.8)
                return false;
            if ((s.bbox.Width / (float)s1.bbox.Width) > 4 || ((s.bbox.Width / (float)s1.bbox.Width) > 3 && (s1.bbox.Left - s.bbox.Left + 0.0) / s1.bbox.Width > 0.5) || (s.bbox.Width / (float)s1.bbox.Width) < 0.25)
                return false;
            Point npt = getPt(s.s.NearestPoint(s1.s.GetPoint(0)), s.s.GetPoints());
            if ((npt.Y - s1.bbox.Top + 0.0) / s1.bbox.Height > 0.2)
                return false;
            if ((npt.Y - s1.bbox.Top + 0.0) / s1.bbox.Height < -0.3)
                return false;
            Recognition r = Classification(s.s);
            if (r != null && r.strokes.Count > 1)
            { // if we're breaking apart an '=', we have to update the top stroke to be a '-'
                if (r.strokes.Count > 2)
                    return false;
                Strokes newrec = r.strokes.Ink.CreateStrokes();
                foreach (Stroke sr in r.strokes)
                    if (sr.Id != s.s.Id)
                        newrec.Add(sr);
                FullClassify(r.strokes[0], new Recognition(newrec, "-", newrec.GetBoundingBox().Bottom,
                    newrec.GetBoundingBox().Top, false));
            }
            stks = s1.s.Ink.CreateStrokes(new int[] { s.s.Id, s1.s.Id });
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        bool match2_t(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt, ref string newmatch)
        {
            if (s1.bbox.Width / (s1.bbox.Height + 0.0) > s.bbox.Width / (s.bbox.Height + 0.0))
            {
                CuspSet tmp = s; s = s1; s1 = tmp;
                string tstr = letter; letter = other; other = tstr;
            }
            if (s1.bbox.Width / (float)s1.bbox.Height > 1.5)
                return false;
            if (s1.cusps.Length > 5)
                return false;
            if (other.Contains("2") || other.Contains("z") || other.Contains(">") || other.Contains("7"))
            {
                int rightind, mx = maxx(0, s1.pts.Length / 2, s1.pts, out rightind);
                if (s1.cusps.Length > 2 && s1.cusps[1].top && s1.distances[rightind] / (s1.dist - s1.cusps[2].dist) > 0.35)
                    return false;
            }
            float[] ints = s1.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id }));
            float[] cints;
            Point inter;
            if (ints.Length == 0)
            {
                float dist;
                inter = getPt(s1.s.NearestPoint(s.pts[0], out dist), s1.s.GetPoints());
                if (s.bbox.Bottom < s1.bbox.Top || s.bbox.Top > s1.bbox.Bottom)
                    return false;
                if ((s.bbox.Right - s1.bbox.Right + 0.0) / s.bbox.Width > 0.66 || (s1.bbox.Left - s.bbox.Left + 0.0) / s.bbox.Width > 0.33)
                {
                    if (dist / s1.bbox.Width > Math.Min(.15, InkPixel * 20 / s1.bbox.Width))
                        return false;
                }
                ints = new float[] { s1.s.NearestPoint(s.pts[0]) };
                cints = new float[] { 0 };
            }
            else
            {
                cints = s.s.FindIntersections(s1.s.Ink.CreateStrokes(new int[] { s1.s.Id }));
                inter = getPt(ints[0], s1.s.GetPoints());
            }
            double stemang = angle(s1.pts[0], inter, new PointF(0, -1));
            if ((s.bbox.Right - s1.bbox.Left + 0.0) / s.bbox.Width > 0.9 && ((s.last.X < s.pts[0].X ? s.last : s.pts[0]).X - inter.X + 0.0) / s.bbox.Width > -0.15 &&
                (s1.pts[0].X - s1.bbox.Left + 0.0) / s1.bbox.Width > 0.5 &&
                (V2D.Straightness(s1.s.GetPoints(), 0, (int)ints[0]) > 0.12 - Math.Min(0.04, (stemang / (float)45) * .08) || stemang > 45))
                return false; // memberof rejection
            if ((getPt(cints[0], s.s.GetPoints()).X - s.bbox.Left) / s.bbox.Width < 0.2)
            { // try 'memberof' rejection test
                int tint = convertIndexBack((int)ints[0], s1.skipped);
                if (s1.bbox.Width / (float)s1.bbox.Height > 0.5 && (s1.pts[0].X - s1.pts[tint].X) / (float)s1.bbox.Width > .6 &&
                    angle(s1.pts[0], s1.pts[tint], new PointF(0, -1)) > 45)
                    return false;
            }
            if (s1.cusps.Length > 3)
            {
                if (s1.angles[s1.angles.Length - 2] < -.5 && s1.angles[s1.angles.Length - 2] > -2.5)
                    return false;
                if (s1.cusps[0].top && s1.cusps[1].bot && s1.cusps[2].top)
                    return false;
                if (angle(s1.cusps[1].pt, s1.cusps[0].pt, new PointF(-1, 0)) < 25)
                    return false;
            }
            else if (other != "l" && !s1.cusps[0].bot)
            {
                bool found_flat = false;
                for (int m = s1.pts.Length * 3 / 4; m < s1.pts.Length - 1; m++)
                    if (s1.angles[m] > 0)
                        found_flat = true;
                if (!found_flat)
                {
                    int maxind;
                    V2D.MaxDist(s1.pts, V2D.Normalize(V2D.Sub(s1.pts[0], s1.last)), out maxind);
                    double hookdist = (s1.dist - s1.distances[maxind]) / s1.dist;
                    if (hookdist < 0.1)
                        return false;
                    double hookscalefactor = 1 - Math.Max(.3 - hookdist, 0);
                    if (angle(s1.pts[0], s1.pts[maxind], V2D.Sub(s1.last, s1.pts[maxind])) > (s.bbox.Width / (float)s1.bbox.Height > .75 ? 140 : 145) * hookscalefactor)
                        return false;
                    double aa = angle(s1.pts[0], s1.pts[maxind], new PointF(0, -1));
                    if (aa > 35)
                        return false;
                    if (convertIndexBack((int)ints[0], s1.skipped) >= maxind)
                        return false;
                }
            }
            if (s1.cusps.Length > 2 && s1.cusps[0].pt.X < s1.cusps[2].pt.X && s1.cusps[1].top && s1.cusps[2].bot && s1.cusps[0].bot)
            {
                if (ints.Length != 2)
                    return false;
                Point p1 = getPt(ints[0], s1.s.GetPoints());
                Point p2 = getPt(ints[1], s1.s.GetPoints());
                if ((s.pts[0].X < p1.X && s.last.X < p1.X) || (s.pts[0].X > p1.X && s.last.X > p1.X))
                    return false;
                double apexAng = angle(s1.pts[(s1.cusps[1].index + (int)ints[0]) / 2], s1.cusps[1].pt, V2D.Sub(s1.pts[(s1.cusps[1].index + (int)ints[1]) / 2], s1.cusps[1].pt));
                double barhgt = (Math.Max(p1.Y, p2.Y) - s1.cusps[1].pt.Y) / (float)s1.bbox.Height;
                if (apexAng >= 5 && barhgt > 0.25)
                {
                    if (apexAng > 20 - (Math.Max(0, barhgt - 0.25) * 12))
                        return false;
                    if (V2D.Straightness(s1.pts, 0, s1.cusps[1].index) < 0.12 && V2D.Straightness(s1.pts, s1.cusps[1].index, s1.pts.Length) < 0.12)
                        return false;
                }
                if (Math.Abs(s1.cusps[1].curvature) > 0.4)
                {
                    newmatch = "script-t";
                    return true;
                }
            }
            if (letter != "-" && letter != "~" && letter != "/" && letter != "^")
                return false;
            if (s.bbox.Height > s.bbox.Width)
                return false;
            if (other == "fbase" || other == "z" || other == "2" || other == "y" || other == "g" || other == "q" ||
                (other.IndexOf('t') >= 0 && Array.BinarySearch(_words, other) >= 0))
                return false;
            if (other == "l" || other == "~")
            {
                newmatch = "t";
                return true;
            }
            if (s1.pts[0].Y > s1.pts[s1.pts.Length - 1].Y)
                return false;
            double ang = angle(s1.last, s1.pts[0], new Point(10, 20));
            int interInd = Math.Max(0, convertIndexBack((int)ints[0], s1.skipped));
            if (s1.cusps.Length == 3 && s1.cusps[1].pt.Y > s1.last.Y)
            {
                double hookang = angle(s1.cusps[1].pt, inter, V2D.Sub(s1.cusps[1].pt, s1.last));
                double hookstr = V2D.Straightness(s1.pts, s1.cusps[1].index, s1.pts.Length);
                if (hookang < 30 - Math.Min(1d, hookstr / 0.3) * 10)
                    return false;
            }
            bool left;
            int maxInd;
            int maxyind, my = maxy(0, s1.pts.Length, s1.pts, out maxyind);
            if (maxyind < s1.pts.Length - 2 && s1.curvatures[maxyind] > 0)
                return false;
            int botyind = maxyind;
            for (int i = maxyind; i > 0; i--)
                if (s1.pts[i].Y < s1.last.Y)
                {
                    botyind = i;
                    break;
                }
            double md = V2D.MaxDist(s1.pts, V2D.Normalize(V2D.Sub(s1.pts[s1.pts.Length - 2], s1.pts[interInd])), out left, out maxInd, interInd, s1.pts.Length - 2);
            double botStraight = md / V2D.Dist(s1.pts[s1.pts.Length - 2], s1.pts[interInd]);
            if (s1.straight < 0.12)
            {
                if (cints == null)
                    cints = new float[] { 0 };
                double barratio = s.distances[convertIndexBack((int)cints[0], s.skipped)] / s.dist;
                if (V2D.Dist(s1.pts[botyind], s1.last) / s1.bbox.Height < .1 - (barratio > 0.3 ? 0 : (.3 - barratio) / .45))
                    return false;
                if (maxyind < s1.pts.Length - 1)
                {
                    double topang = angle(s1.pts[0], s1.pts[maxyind], V2D.Sub(s1.pts[0], s1.last));
                    double botang = angle(s1.pts[0], s1.pts[maxyind], V2D.Sub(s1.last, s1.pts[maxyind]));
                    if (botang < 10 || topang < 10 || botang < s1.distances[maxyind] / (s1.dist - s1.distances[maxyind]) * 3.5 + 8 && topang < 15)
                        if (V2D.Straightness(s1.pts, 0, maxyind) < .1 &&
                        V2D.Straightness(s1.pts, maxyind, s1.pts.Length) < .1)
                            return false;
                }
            }
            double topStraight = interInd < 1 ? 0 : V2D.Straightness(s1.pts, Math.Min(interInd - 1, 2), interInd);
            if (V2D.Dist(s1.pts[maxInd], s1.last) / (s1.bbox.Height + 0.0) < .13)
                return false;
            if (botStraight < 0.08 * Math.Max(0.08, ((s.bbox.Width * 1.1) / s1.bbox.Height)) || topStraight > botStraight || !left)
                return false;
            if ((s1.distances[s1.distances.Length - 1] - s1.distances[interInd]) / Math.Max(s1.dist, s.dist) < .4)
                return false;
            if (Math.Abs((s1.s.GetPoint((int)ints[0]).Y - s1.pts[0].Y + 0.0) / s1.bbox.Height - 0.5) > 0.4 &&
                s.dist / s1.distances[s1.distances.Length - 1] > .8 && V2D.Straightness(s1.pts) < 0.15)
                return false;
            xhgt = getPt(ints[0], s1.s.GetPoints()).Y;
            newmatch = "t";
            return true;
        }
        bool match2_2(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if (other != "2partial" || letter != "\\")
                return false;
            float near1; s1.s.NearestPoint(s.pts[0], out near1);
            float near2; s.s.NearestPoint(s1.last, out near2);
            if (near1 / s1.bbox.Height > 0.2 || near2 / s1.bbox.Height > 0.25)
                return false;
            xhgt = (s1.bbox.Top + s1.bbox.Bottom) / 2;
            return true;
        }
        public bool match2_4(CuspSet s, CuspSet s1, string letter, string other, ref int xhgt)
        {
            if ("oO0".IndexOf(other[0]) != -1) 
                return false;
            double down1 = angle(s1.last, s1.pts[0], new Point(-1, 1));
            double down = angle(s.last, s.pts[0], new Point(-1, 1));
            CuspSet cross;
            CuspSet stem;
            string crLet;
            if (letter.Contains("1") || letter == "/" || letter.Contains("(") || letter == "fbase" || letter.Contains("INT") || letter.Contains(")"))
            {
                cross = s1;
                stem = s;
                crLet = other;
            }
            else
            {
                if (other[0] != '1' && other != "/" && other[0] != '(' && other[0] != ')')
                    return false;
                cross = s;
                stem = s1;
                crLet = letter;
            }
            if (cross.cusps.Length > 5)
                return false;
            if ((stem.pts[0].X - cross.pts[0].X + 0.0) / cross.bbox.Width < 0.25 &&
                (cross.pts[0].Y - stem.pts[0].Y + 0.0) / stem.bbox.Height > 0.25)
                return false;
            Rectangle bounds = Rectangle.Union(cross.s.GetBoundingBox(), stem.s.GetBoundingBox());
            float[] ints = cross.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { stem.s.Id }));
            if (ints.Length > 2)
                return false;
            if (ints.Length == 2)
            {
                int topIntInd = convertIndexBack((int)ints[0], cross.skipped);
                if (topIntInd == -1 || (cross.pts[topIntInd].Y - stem.bbox.Top + 0.0) / stem.bbox.Height > 0.25)
                    return false;
            }
            // 2-stroke lower k rejection
            if ((crLet == "L" || crLet == "uv" || crLet == "v") && angle(cross.last, cross.cusps[cross.nl].pt, new Point(1, -1)) < 30 &&
                (stem.bbox.Bottom - cross.bbox.Bottom + 0.0) / stem.bbox.Height < 0.25)
                return false;
            int interInd = ints.Length > 0 ? convertIndexBack((int)ints[ints.Length - 1], cross.skipped) : cross.pts.Length - 1;

            double stemstr = V2D.Straightness(stem.pts);
            int angYind = 0;  // test to see if stem is loopy to the left like a y ... reject over a threshold
            for (int i = 0; i < stem.angles.Length; i++)
                if (Math.Abs(stem.angles[i]) < Math.PI / 4 + Math.Max(0, stemstr - .12))
                {
                    angYind = i;
                    break;
                }
            double stemang = angle(stem.pts[angYind], stem.last, new PointF(0, -1));
            if ((stem.dist - stem.distances[angYind]) / stem.dist > .1 && stemang > 45 || stemstr > 0.2)
                return false;
            /*
            float neardist;
            stem.s.NearestPoint(cross.last, out neardist);
            double yang = angle(cross.pts[interInd > cross.pts.Length-3 ? cross.pts.Length-:interInd-3], cross.last, V2D.Sub(cross.pts[interInd], stem.pts[0]));
            if ((yang < 10 || (yang < stemang)) && neardist/stem.bbox.Width < 0.15)
                return false;
            if (ints.Length > 0 && angle(stem.pts[0], interPt, V2D.Sub(cross.last, interPt)) <8)
                return false;
            */
            if (ints.Length > 0)
            {
                bool moreOnLeft;
                int maxInd;
                V2D.MaxDist(cross.pts, stem.pts[0], V2D.Normalize(V2D.Sub(cross.pts[interInd], stem.pts[0])), out moreOnLeft, out maxInd, 0, interInd);
                if (!moreOnLeft)
                    return false;
            }
            int topcrossind, my = miny(0, cross.pts.Length / 2, cross.pts, out topcrossind);
            if (topcrossind < 0 || cross.distances[topcrossind] / cross.bbox.Width > 0.1 && V2D.Straightness(cross.pts, 0, topcrossind) > 0.15)
                return false;
            bool left = true;
            int farInd;
            double farthest = V2D.MaxDist(cross.pts, V2D.Normalize(V2D.Sub(cross.last, cross.pts[topcrossind])), out left, out farInd, topcrossind, cross.pts.Length);
            if (farthest / cross.dist < 0.1)
                return false;
            if (cross.distances[interInd] / cross.dist < 0.5)
                return false;
            int ma_y = maxy(0, interInd, cross.pts);
            double cratio = (ma_y - stem.bbox.Top + 0.0) / stem.bbox.Height;
            if (cratio < 0.2 || cratio > 0.9 || farInd == 0 ||
                V2D.Straightness(cross.pts, farInd, cross.pts.Length) > 0.6)
                return false;
            Point interPt = ints.Length > 0 ? getPt(ints[ints.Length - 1], cross.s.GetPoints()) : cross.cusps[cross.l].pt;
            if (ints.Length == 0)
            {
                float dist;
                stem.s.NearestPoint(interPt, out dist);
                if (dist / cross.bbox.Width > 0.1 && dist / InkPixel > 4)
                    return false;
            }
            else
            {
                if ((stem.bbox.Bottom - interPt.Y) / (float)Math.Max(cross.bbox.Width, stem.bbox.Height) < 0.2 - Math.Max(0, .1 - stem.straight) &&
                    (Math.Min(cross.pts[0].X, cross.last.X) - interPt.X) / (float)cross.bbox.Width > .2)
                    return false;
            }
            double crossStr = V2D.Straightness(cross.pts);
            if (crossStr < .2 || (!letter.Contains("(") && crossStr < 0.3 && angle(cross.pts[0], interPt, V2D.Sub(stem.pts[0], interPt)) < 45))
                return false;
            if ((ints.Length == 0 || (cross.last.X - interPt.X + 0.0) / bounds.Width < .3) &&
                (cross.avgCurveSeg(3, interInd - 2) > -0.095 && cross.maxCurveSeg(3, interInd - 2) > -0.25 && V2D.Straightness(cross.pts) < 0.3 &&
                angle(stem.pts[0], stem.last, new PointF(0, -1)) > 15))
                return false;
            if (Math.Sign(cross.curvatures[farInd]) != -1 && (cross.intersects.Length == 0 || cross.intersects[0] > farInd))
                return false;
            int mi_xind, ma_xind;
            int mi_x = minx(0, cross.pts.Length, cross.pts, out mi_xind);
            int ma_x = maxx(0, interInd, cross.pts, out ma_xind);
            if ((stem.bbox.Bottom - ma_y + 0.0) / stem.bbox.Height < 0.2 && V2D.MaxDist(stem.pts, V2D.Normalize(V2D.Sub(stem.last, stem.pts[0])), out left) / stem.bbox.Height > 0.2)
                return false;
            float mi_x_dist, ma_x_dist;
            stem.s.NearestPoint(getPt(mi_xind, cross.pts), out mi_x_dist);
            stem.s.NearestPoint(getPt(ma_xind, cross.pts), out ma_x_dist);
            if (mi_x_dist < .35 * ma_x_dist)
                return false;
            if (stem.pts[0].X - mi_x < InkPixel * 4)
                return false;
            xhgt = cross.pts[cross.pts.Length - 1].Y;
            return true;
        }
        bool match2_pl(CuspSet s, CuspSet s1, string letter, string other, ref Strokes stks, ref string allograph)
        {
            if (other == "INTbot")
                return false;
            if ((0.0 + s.bbox.Width) / s.bbox.Height < (0.0 + s1.bbox.Width) / s1.bbox.Height)
            {
                CuspSet tmp = s;
                s = s1;
                s1 = tmp;
                string l = letter;
                letter = other;
                other = l;
            }
            if (other.Contains("2") || other.Contains("z") || other == "7")
                return false;
            if (s.bbox.Width / (s1.bbox.Height + InkPixel * 10) > 2)
                return false;
            int botind; maxy(0, s1.pts.Length, s1.pts, out botind);
            int topind = botind < s1.pts.Length / 3 ? s1.pts.Length - 1 : 0;
            double reals1_str = V2D.Straightness(s1.pts, s1.pts[topind], V2D.Sub(s1.pts[botind], s1.pts[topind]), 0, s1.pts.Length, V2D.Dist(s1.pts[botind], s1.pts[topind]));
            if (reals1_str > .4)
                return false;
            float[] ints = s.s.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s1.s.Id }));
            if (ints.Length == 0 || ints.Length > 2 || (ints.Length == 2 && ints[1] - ints[0] > 3 && s.bbox.Width / s.bbox.Height < 3))
                return false;
            Point intersect = getPt(ints[0], s.s.GetPoints());
            bool left; // test for z like stem
            if (V2D.Straightness(s1.pts, V2D.Sub(s1.last, s1.pts[0]), 0, s1.pts.Length, s1.bbox.Height, out left) > 0.2 && s1.pts[0].Y < s1.last.Y &&
                (s1.pts[0].X - intersect.X + 0.0) / s.bbox.Width / s.bbox.Width * s1.bbox.Height > 0.25)
                return false;
            double sizeFactor = Math.Min(1, Math.Max(0.5, (InkPixel * 200) / s.bbox.Width) + .25);
            int miny = FeaturePointDetector.miny(0, s1.pts.Length, s1.pts);
            double endratio = (intersect.Y - miny + 0.0) / s1.bbox.Height * Math.Min(1, (s1.bbox.Height / (float)s.bbox.Width));
            if (endratio > 0.9 * sizeFactor || endratio < 0.15 * (1 - sizeFactor))
                return false;
            int minx = FeaturePointDetector.minx(0, s.pts.Length, s.pts);
            double endratio2 = (intersect.X - minx + 0.0) / s.bbox.Width;
            double barang = angle(s.last, s.pts[0], new PointF(s.last.X > s.pts[0].X ? 1 : -1, 0));
            endratio2 *= (1 + barang / 180.0);
            if (endratio2 > 0.9 || endratio2 < 0.05)
                return false;
            if (angle(s.last, s.pts[0], new PointF(0, 1)) < 50 ||
                angle(s.pts[0], s.last, new PointF(0, 1)) < 50)
                return false;
            if (letter != "-" && letter != "sqrt" && letter != "<" && letter != ">" && letter != "\\" && letter != "~" && letter != "." && letter != "/")
                return false;
            if ((s.bbox.Height / ((float)s.bbox.Width) > 0.5 && s.straight > 0.2) ||
                (reals1_str > 0.15 && ((s1.cusps[s1.l].bot && !s1.cusps[0].top) || (s1.cusps[s1.l].top && !s1.cusps[0].bot))))
                return false;
            if ((letter == "/" || letter == "\\") && (other == "/" || other == "\\"))
                return false;
            double str = s.straight;
            if (s.cusps.Length > 2 && str >= 0.25)
            { //  handle hook at start of cross arising from transition from down stroke
                str = V2D.Straightness(s.pts, s.cusps[1].index, s.pts.Length);
                if (angle(s.cusps[1].pt, s.pts[0], V2D.Sub(s.cusps[1].pt, s.last)) > 90)
                    return false;
            }
            if (str > 0.25 && s.cusps.Length > 2 && s.cusps[1].curvature < 0)
                return false;
            Recognition r = Classification(s1.s);
            if (r != null && r.strokes.Count > 1)
            { // if we're breaking apart a perp, we have to update the bot stroke to be a '-'
                if (r.strokes.Count > 2)
                    return false;
                stks = s1.s.Ink.CreateStrokes(new int[] { r.strokes[0].Id, r.strokes[1].Id, s.s.Id });
                allograph = "+/-";
            }
            else allograph = "+";
            return true;
        }
        public Hashtable Recogs = new Hashtable();
        public Guid IgnoreGuid = new Guid("{061ca66b-fb79-41f1-a112-8bf0b5a7a436}");
        public Guid AltGuid = new Guid("{ffa80596-769a-4073-bd6f-fda8e42aabd0}");
        bool match_H(CuspSet s, string letter, ref Strokes stks, ref int xhgt, ref string allograph)
        {
            Stroke s1 = null;
            Stroke s2 = null;
            Stroke sh = null;

            Strokes inside = null;
            if (stks.Count == 0)
                return false;
            if (letter == "~" || letter == "-" || letter == "^" || letter == "t" || letter == "+")
            {
                inside = filter(s.s.Ink.HitTest(stks.GetBoundingBox(), 50));
                if (inside.Count > 3)
                {
                    Strokes toRemove = stks.Ink.CreateStrokes();
                    foreach (Stroke hs in inside)
                        if (hs.Id != s.s.Id && hs.FindIntersections(s.s.Ink.CreateStrokes(new int[] { s.s.Id })).Length == 0)
                        {
                            toRemove.Add(hs);
                            if (inside.Count - toRemove.Count == 3)
                                break;
                        }
                    inside.Remove(toRemove);
                    if (inside.Count != 3)
                        return false;
                }
                else if (inside.Count != 3)
                    return false;
                s1 = inside[0].Id != s.s.Id ? inside[0] : inside[1];
                s2 = inside[2].Id != s.s.Id ? inside[2] : inside[1];
                sh = s.s;
            }
            else if (s.bbox.Height / (float)s.bbox.Width > 3)
            {
                Rectangle vertBox = s.s.GetBoundingBox();
                vertBox.Inflate((int)(vertBox.Height * .15), (int)(vertBox.Height * .15));
                inside = filter(s.s.Ink.HitTest(vertBox, 1));
                foreach (Stroke hs in inside)
                    if (hs.Id != s.s.Id)
                        if (hs.GetBoundingBox().Width > hs.GetBoundingBox().Height)
                            sh = hs;
                if (sh == null)
                    return false;
                if (inside.Count > 3)
                {
                    Strokes toRemove = stks.Ink.CreateStrokes();
                    int mid = (sh.GetBoundingBox().Left + sh.GetBoundingBox().Right) / 2;
                    foreach (Stroke hs in inside)
                        if (hs.Id != s.s.Id && s.s.GetBoundingBox().Left < mid && hs.GetBoundingBox().Right < mid)
                            toRemove.Add(hs);
                        else if (hs.Id != s.s.Id && s.s.GetBoundingBox().Right > mid && hs.GetBoundingBox().Right > mid)
                            inside.Remove(toRemove);
                }
                if (inside.Count != 3)
                {
                    if (inside.Count != 2)
                        return false;
                    vertBox = sh.GetBoundingBox();
                    vertBox.Inflate((int)(vertBox.Width * .15), (int)(vertBox.Width * .15));
                    inside = filter(s.s.Ink.HitTest(vertBox, 1));
                    if (inside.Count != 3)
                        return false;
                }
                s1 = s.s;
                foreach (Stroke hs in inside)
                    if (hs.Id != s.s.Id)
                        if (hs.GetBoundingBox().Width > hs.GetBoundingBox().Height)
                            sh = hs;
                        else s2 = hs;
            }
            else
                return false;
            if (sh == null || s1 == null || s2 == null)
                return false;
            Strokes reallyInside = inside.Ink.HitTest(inside.GetBoundingBox(), 10);
            reallyInside.Remove(inside);
            if (reallyInside.Count > 0) // H's the contain something are more likely ( - ) division structures or something
                return false;
            float dist;
            Stroke sl = s1.GetBoundingBox().Left < s2.GetBoundingBox().Left ? s1 : s2;
            Stroke sr = s1.GetBoundingBox().Left < s2.GetBoundingBox().Left ? s2 : s1;
            if (sl.FindIntersections(sh.Ink.CreateStrokes(new int[] { sh.Id })).Length > 0)
                dist = 0;
            else sl.NearestPoint(sh.GetPoint(0), out dist);
            if (dist / sh.GetBoundingBox().Width > 0.2)
                return false;
            if (sr.FindIntersections(sh.Ink.CreateStrokes(new int[] { sh.Id })).Length > 0)
                dist = 0;
            else sr.NearestPoint(sh.GetPoint(sh.GetPoints().Length - 1), out dist);
            if (dist / sh.GetBoundingBox().Width > 0.2 / (V2D.Straightness(sr.GetPoints()) / 0.1 + sr.GetBoundingBox().Height / (float)sl.GetBoundingBox().Height))
                return false;
            if (V2D.Straightness(s1.GetPoints()) > 0.3 || V2D.Straightness(s2.GetPoints()) > 0.3)
                return false;
            double angA1a = angle(sl.GetPoint(sl.GetPoints().Length - 1), sl.GetPoint(0), new Point(1, -1));
            double angA1b = angle(sl.GetPoint(sl.GetPoints().Length - 1), sl.GetPoint(0), new Point(-1, 1));
            double angA2a = angle(sr.GetPoint(sr.GetPoints().Length - 1), sr.GetPoint(0), new Point(1, 1));
            double angA2b = angle(sr.GetPoint(sr.GetPoints().Length - 1), sr.GetPoint(0), new Point(-1, -1));
            double angA1 = Math.Min(angA1a, angA1b);
            double angA2 = Math.Min(angA2a, angA2b);
            float dista;
            double ratio = (sh.GetPoint(sh.GetPoints().Length / 2).Y - sl.GetBoundingBox().Top + 0.0) / sl.GetBoundingBox().Height;
            double ratio2 = (sh.GetPoint(sh.GetPoints().Length / 2).Y - sr.GetBoundingBox().Top + 0.0) / sr.GetBoundingBox().Height;
            s1.NearestPoint(sr.GetPoint(0).Y < sr.GetPoint(sr.GetPoints().Length - 1).Y ? sr.GetPoint(0) : sr.GetPoint(sr.GetPoints().Length - 1), out dista);
            if ((sl.GetBoundingBox().Right > sr.GetBoundingBox().Left || dista / (sr.GetBoundingBox().Right - sl.GetBoundingBox().Left + 0.0) < 0.2) && angA1 < 50 && angA2 < 50)
            {
                if (ratio < 0.3)
                    return false;
                if (ratio2 < 0.3)
                    return false;
                stks = inside;
                allograph = "A";
                return true;
            }
            double ang1 = angle(s1.GetPoint(s1.GetPoints().Length - 1), s1.GetPoint(0), new PointF(0, 1));
            double ang2 = angle(s2.GetPoint(s2.GetPoints().Length - 1), s2.GetPoint(0), new PointF(0, 1));
            if (Math.Abs(ang1) < 25 && Math.Abs(ang2) < 25)
            {
                if (ratio < 0.3 || ratio > 0.7)
                    return false;
                if (ratio2 < 0.3 || ratio2 > 0.7)
                    return false;
                stks = inside;
                xhgt = (s1.GetBoundingBox().Top + s1.GetBoundingBox().Bottom) / 2;
                allograph = "H";
                return true;
            }
            return false;

        }
        bool match_K(CuspSet s, ref Strokes stks, ref int xhgt)
        {
            Stroke stem = null;
            Stroke upbar = null;
            Stroke downbar = null;
            bool downrev = false;
            bool uprev = false;
            Rectangle testRect = Rectangle.Empty;
            Strokes hit = null;
            for (int trystem = 0; trystem < 2; trystem++)
            {
                Stroke astem = null;
                if (s.bbox.Height / (float)s.bbox.Width > (trystem == 0 ? 3 : 8))
                {
                    astem = s.s;
                    testRect = new Rectangle(new Point(s.bbox.Left, (s.pts[0].Y * 2 + s.last.Y) / 3),
                        new Size(s.bbox.Height / 3, s.bbox.Height / 3));
                }
                else
                {
                    bool ltor = s.last.X > s.pts[0].X;
                    bool down = ltor ? (s.last.Y > s.pts[0].Y) : (s.pts[0].Y > s.last.Y);
                    testRect = new Rectangle(
                        new Point((ltor ? s.pts[0].X : s.last.X) - (int)(InkPixel * 5), down ? s.bbox.Top - Math.Max(s.bbox.Width, s.bbox.Height) / 2 : s.bbox.Bottom - Math.Max(s.bbox.Height, s.bbox.Width) / 2),
                                new Size((int)(InkPixel * 5) + 2 * Math.Max(s.bbox.Width, s.bbox.Height) / 3, Math.Max(s.bbox.Height, s.bbox.Width)));
                }
                hit = filter(stks.Ink.HitTest(testRect, 1));
                if (hit.Count == 2)
                {
                    int pad = (astem == null) ? (int)(Math.Max(InkPixel * 10, testRect.Width * .3)) : 0;
                    testRect.Size = new Size(testRect.Width + testRect.Left - hit.GetBoundingBox().Left + pad, testRect.Height);
                    testRect.Offset(hit.GetBoundingBox().Left - testRect.Left - pad, 0);
                    hit = filter(stks.Ink.HitTest(testRect, 1));
                }
                if (hit.Count == 3)
                    break;
            }
            if (hit.Count == 3)
            {
                foreach (Stroke ks in hit)
                {
                    double aspect = ks.GetBoundingBox().Height / (float)ks.GetBoundingBox().Width;
                    if (aspect > 3 && ks.GetBoundingBox().Height / (float)hit.GetBoundingBox().Height > 0.8)
                    {
                        stem = ks;
                        if (V2D.Straightness(ks.GetPoints()) > 0.2)
                            return false;
                    }
                    else
                    {
                        Point p1 = ks.GetPoint(0);
                        Point p2 = ks.GetPoint(ks.GetPoints().Length - 1);
                        if (p1.X < p2.X)
                        {
                            if (p1.Y > p2.Y)
                                upbar = ks;
                            else downbar = ks;
                        }
                        else
                        {
                            if (p1.Y < p2.Y)
                            {
                                upbar = ks;
                                uprev = true;
                            }
                            else
                            {
                                downbar = ks;
                                downrev = true;
                            }
                        }
                        if (V2D.Straightness(ks.GetPoints()) > (upbar != null && upbar.Id == ks.Id ? 0.15 : 0.3))
                            return false;
                    }
                }
                if (downbar == null || upbar == null || stem == null)
                    return false;
                Point downdir = V2D.Mul(V2D.Sub(downbar.GetPoint(downbar.GetPoints().Length - 1), downbar.GetPoint(0)), (downrev ? -1 : 1));
                Point updir = V2D.Mul(V2D.Sub(upbar.GetPoint(upbar.GetPoints().Length - 1), upbar.GetPoint(0)), (uprev ? -1 : 1));
                if (angle(downdir, updir) > 150)
                    return false;
                if (downbar.GetBoundingBox().Top < upbar.GetBoundingBox().Top)
                    return false;
                float distBars, upStem;
                upbar.NearestPoint(downrev ? downbar.GetPoint(downbar.GetPoints().Length - 1) : downbar.GetPoint(0), out distBars);
                stem.NearestPoint(uprev ? upbar.GetPoint(upbar.GetPoints().Length - 1) : upbar.GetPoint(0), out upStem);
                float upRatio = upStem / Math.Max(upbar.GetBoundingBox().Width, upbar.GetBoundingBox().Height);
                float distRatio = distBars / Math.Max(downbar.GetBoundingBox().Width, downbar.GetBoundingBox().Height);
                if ((upRatio > 0.25 && distRatio > 0.25) || upRatio > 0.4 || distRatio > 0.4)
                    return false;
                stks = hit;
                xhgt = (s.bbox.Top + s.bbox.Bottom) / 2;
                return true;
            }
            return false;
        }
        bool match_pi(CuspSet s, string letter, ref Strokes instks)
        {
            if (instks.Count < 2)
                return false;
            Rectangle box = instks.GetBoundingBox();
            box.Location = V2D.Add(box.Location, new Point(0, -box.Height / 2));
            box.Size = new Size(box.Width, 3 * box.Height / 2);
            Strokes stks = filter(s.s.Ink.HitTest(box, 20));
            while (stks.Count >= 4)
            {
                int which = 0;
                int whichbot = 0;
                int top = stks[0].GetBoundingBox().Top;
                int bot = stks[0].GetBoundingBox().Bottom;
                for (int i = 1; i < stks.Count; i++)
                {
                    if (stks[i].GetBoundingBox().Top < top)
                    {
                        which = i;
                        top = stks[i].GetBoundingBox().Top;
                    }
                    if (stks[i].GetBoundingBox().Bottom > bot)
                    {
                        whichbot = i;
                        bot = stks[i].GetBoundingBox().Bottom;
                    }
                }
                Stroke above = stks[which];
                if (stks[whichbot].GetBoundingBox().Width / (float)stks[whichbot].GetBoundingBox().Height > 2.25)
                    stks.Remove(stks[whichbot]);
                else
                {
                    stks.Remove(above);
                    if (stks.Count > 3)
                        continue;
                    Recognition ar = Classification(above);
                    if (ar == null || (ar.alt != Unicode.I.INTEGRAL && ar.alt != Unicode.N.N_ARY_SUMMATION && ar.alt != Unicode.S.SQUARE_ROOT))
                        if (!(above.GetBoundingBox().Width / (float)above.GetBoundingBox().Height > 3 &&
                        above.GetBoundingBox().Left < stks.GetBoundingBox().Left && above.GetBoundingBox().Right > stks.GetBoundingBox().Right))
                            return false;
                }
            }
            if (stks.Count == 3 && stks.Contains(s.s))
            {
                Stroke bar = null;
                double r0 = stks[0].GetBoundingBox().Width / (float)stks[0].GetBoundingBox().Height;
                double r1 = stks[1].GetBoundingBox().Width / (float)stks[1].GetBoundingBox().Height;
                double r2 = stks[2].GetBoundingBox().Width / (float)stks[2].GetBoundingBox().Height;
                double rght = -1;
                if (r0 > r1 && r0 > r2)
                {
                    rght = r0;
                    bar = stks[0];
                }
                else if (r1 > r0 && r1 > r2)
                {
                    bar = stks[1];
                    rght = r1;
                }
                else
                {
                    rght = r2;
                    bar = stks[2];
                }
                if (rght < 1.5)
                    return false;
                stks.Remove(bar);
                Stroke s1 = stks[0];
                Point s1top = s1.GetPoint(0).Y < s1.GetPoint(s1.GetPoints().Length - 1).Y ? s1.GetPoint(0) : s1.GetPoint(s1.GetPoints().Length - 1);
                Stroke s2 = stks[1];
                Point s2top = s2.GetPoint(0).Y < s2.GetPoint(s2.GetPoints().Length - 1).Y ? s2.GetPoint(0) : s2.GetPoint(s2.GetPoints().Length - 1);
                if ((s1.GetBoundingBox().Top + s1.GetBoundingBox().Bottom) / 2 < bar.GetBoundingBox().Top)
                    return false;
                if ((s2.GetBoundingBox().Top + s2.GetBoundingBox().Bottom) / 2 < bar.GetBoundingBox().Top)
                    return false;
                if (s1.GetBoundingBox().Right < bar.GetBoundingBox().Left || s1.GetBoundingBox().Left > bar.GetBoundingBox().Right)
                    return false;
                if (s2.GetBoundingBox().Right < bar.GetBoundingBox().Left || s2.GetBoundingBox().Left > bar.GetBoundingBox().Right)
                    return false;
                stks.Add(bar);
                double hgtRatio = (letter == "-" ? 0.2 : 0.3);
                float s1dist, s2dist;
                Point s1npt = getPt(bar.NearestPoint(s1top, out s1dist), bar.GetPoints());
                Point s2npt = getPt(bar.NearestPoint(s2top, out s2dist), bar.GetPoints());
                if ((s1npt.Y - s1top.Y + 0.0) / s1.GetBoundingBox().Height > 0.25 ||
                    (s2npt.Y - s2top.Y + 0.0) / s2.GetBoundingBox().Height > 0.25)
                    return false;
                int barhgt = bar.GetBoundingBox().Height;
                if (s1dist / (barhgt + s1.GetBoundingBox().Height) > hgtRatio && s2dist / (barhgt + s2.GetBoundingBox().Height) > hgtRatio)
                    return false;
                if (s1.GetPoint(0).X < bar.GetBoundingBox().Left || s2.GetPoint(0).X > bar.GetBoundingBox().Right)
                    return false;
                float s1len = 0;
                for (int i = 0; i < s1.GetPoints().Length - 1; i++)
                    s1len += (float)V2D.Dist(s1.GetPoint(i), s1.GetPoint(i + 1));
                float s2len = 0;
                for (int i = 0; i < s2.GetPoints().Length - 1; i++)
                    s2len += (float)V2D.Dist(s2.GetPoint(i), s2.GetPoint(i + 1));
                int s1botind; maxy(0, s1.GetPoints().Length, s1.GetPoints(), out  s1botind);
                int s2botind; maxy(0, s2.GetPoints().Length, s2.GetPoints(), out s2botind);
                double ang1 = angle(s1.GetPoint(s1botind), s1top, new PointF(0, 1));
                double ang2 = angle(s2.GetPoint(s2botind), s2top, new PointF(0, 1));
                if (s1top.Y > s2.GetBoundingBox().Bottom || s2top.Y > s1.GetBoundingBox().Bottom)
                    return false;
                int maxhgt = Math.Max(s1.GetBoundingBox().Height, s2.GetBoundingBox().Height);
                int minhgt = Math.Min(s1.GetBoundingBox().Height, s2.GetBoundingBox().Height);
                double mhgtratio = minhgt / (double)Math.Max(bar.GetBoundingBox().Width, bar.GetBoundingBox().Height);
                if (mhgtratio < 0.2)
                    return false;
                if ((s1top.Y - s2.GetBoundingBox().Top + 0.0) / maxhgt > 0.6 ||
                    (s2top.Y - s1.GetBoundingBox().Top + 0.0) / maxhgt > 0.6)
                    return false;
                double s1diag = Math.Sqrt(s1.GetBoundingBox().Width * s1.GetBoundingBox().Width + s1.GetBoundingBox().Height * s1.GetBoundingBox().Height);
                double s2diag = Math.Sqrt(s2.GetBoundingBox().Width * s2.GetBoundingBox().Width + s2.GetBoundingBox().Height * s2.GetBoundingBox().Height);
                if (s1len / s1diag > 2 || s2len / s2diag > 2)
                    return false;
                Recognition barrec = Classification(bar);
                if (barrec != null && (Math.Max(s1.GetBoundingBox().Bottom, s2.GetBoundingBox().Bottom) - barrec.strokes.GetBoundingBox().Bottom + 0.0) / s1.GetBoundingBox().Height < 0.2 && barrec.alt.Character == Unicode.S.SQUARE_ROOT)
                    return false;
                if ((bar.GetPoint(bar.GetPoints().Length - 1).X - bar.GetBoundingBox().Left + 0.0) / bar.GetBoundingBox().Width < 0.5)
                    return false;
                if (s1.FindIntersections(s2.Ink.CreateStrokes(new int[] { s2.Id })).Length > 0)
                    return false;
                if (Math.Abs(ang1) < 45 && Math.Abs(ang2) < 45)
                {
                    Recognition r = Classification(bar);
                    if (r != null && r.strokes.Count > 1 && r.alt != Unicode.G.GREEK_SMALL_LETTER_PI)
                    {
                        if (r.strokes.Count > 2)
                            return false;
                        UnClassify(r.strokes[0]);
                        r.strokes.Remove(bar);
                        FullClassify(r.strokes[0], new Recognition(r.strokes, "-", r.strokes.GetBoundingBox().Bottom,
                            r.strokes.GetBoundingBox().Top, false));
                    }
                    instks = instks.Ink.CreateStrokes(new int[] { s1.Id, s2.Id, bar.Id });
                    return true;
                }
            }
            return false;
        }

        bool match_ellipsis(CuspSet s, string letter, ref Strokes instks)
        {
            if (letter != ".")
                return false;
            return false;
            Strokes stks = filter(s.s.Ink.HitTest(new Rectangle(V2D.Add(s.s.GetPoint(0), new Point(-(int)InkPixel * 40, -(int)InkPixel * 5)), new Size((int)(InkPixel * 85), (int)(InkPixel * 10))), 100));
            if (stks.Count >= 4)
            {
                SortedList<int, Stroke> sl = new SortedList<int, Stroke>();
                foreach (Stroke sk in stks)
                    sl.Add(sk.GetBoundingBox().Left - s.s.GetBoundingBox().Left, sk);
                while (stks.Count > 4)
                    stks.Remove(sl.Values[sl.Values.Count - 1]);
            }
            stks.Remove(s.s);
            if (stks.Count == 2)
            {
                Recognition r1 = Classification(stks[0]);
                Recognition r2 = Classification(stks[1]);
                if ((r1 != null && (r1.allograph == "." || r1.alt == Unicode.T.TWO_DOT_LEADER)) && (r2 != null && (r2.allograph == "." || r2.alt == Unicode.T.TWO_DOT_LEADER)))
                {
                    instks = instks.Ink.CreateStrokes(new int[] { stks[0].Id, stks[1].Id, s.s.Id });
                    return true;
                }
            }
            return false;
        }

        bool match3_surfaceIntegral(CuspSet s, string letter, ref Strokes instks)
        {
            if (s.s.Ink.Strokes.Count < 3 || (letter != "0"))
                return false;

            Stroke other = null; Stroke other2 = null;
            foreach (Stroke istroke in s.s.Ink.Strokes)
                if (istroke.Id < s.s.Id && (other == null || istroke.Id > other.Id))
                    other = istroke;
            foreach (Stroke istroke in s.s.Ink.Strokes)
                if (istroke.Id < s.s.Id - 1 && (other2 == null || istroke.Id > other2.Id))
                    other2 = istroke;
            Recognition r = Classification(other); Recognition r2 = Classification(other2);
            if (r == null || r2 == null)
                return false;
            String allograph = r.allograph; String allograph2 = r2.allograph;

            if (!allograph.Contains("s") && !allograph.Contains("s") && !allograph.Contains("fbase")
                && !allograph.Contains("INTbot") && !allograph.Contains("INTtop"))
                return false;
            if (!allograph2.Contains("s") && !allograph2.Contains("s") && !allograph2.Contains("fbase")
                && !allograph2.Contains("INTbot") && !allograph2.Contains("INTtop"))
                return false;

            Rectangle recUnion = Rectangle.Union(other.GetBoundingBox(), other2.GetBoundingBox());
            double widthRatio = recUnion.Width / (float)s.bbox.Width;
            if (widthRatio < 0.4 || widthRatio > 2.0)
                return false;
            double heightRatio = recUnion.Height / (float)s.bbox.Height;
            if (heightRatio < 2.0 || heightRatio > 8.0)
                return false;

            double recHorCenter = (recUnion.Right + recUnion.Left) / 2;
            double recVertCenter = (recUnion.Bottom + recUnion.Top) / 2;
            double sHorCenter = (s.bbox.Right + s.bbox.Left) / 2;
            double sVertCenter = (s.bbox.Bottom + s.bbox.Top) / 2;
            if (Math.Abs((recHorCenter - sHorCenter) / (float)recUnion.Width) > 0.4)
                return false;
            if (Math.Abs((recVertCenter - sVertCenter) / (float)recUnion.Height) > 0.4)
                return false;

            return true;
        }

        bool match_plmi(CuspSet s, ref string letter, ref Strokes stks)
        {
            if (s.s.Ink.Strokes.Count < 2 || (letter != "-" && letter != "~"))
                return false;
            Stroke other = null;
            foreach (Stroke istroke in s.s.Ink.Strokes)
                if (istroke.Id < s.s.Id && (other == null || istroke.Id > other.Id))
                    other = istroke;
            if (other == null)
                return false;
            Rectangle otherbounds = other.GetBoundingBox();
            Recognition or = Classification(other);
            Point othercent = new Point((otherbounds.Left + otherbounds.Right) / 2, otherbounds.Bottom);
            if (othercent.X < s.bbox.Left || othercent.X > s.bbox.Right)
                return false;
            if (1.6 < s.bbox.Width / (float)otherbounds.Width)
            {
                if (s.bbox.Left < otherbounds.Left && s.bbox.Right > otherbounds.Right)
                {
                    if (2.5 < s.bbox.Width / (float)otherbounds.Width)
                        return false;
                }
                else
                    return false;
            }
            Recognition r = Classification(other);
            if (r == null)
                r = FullClassify(other);
            if (r == null || r.allograph == "" || (r.allograph != "+" && !char.IsLower(r.allograph[0])))
                return false;
            if (r.allograph == "+")
            {
                Stroke bar = r.strokes[0];
                Stroke stem = r.strokes[1];
                if (bar.GetBoundingBox().Width < stem.GetBoundingBox().Width)
                {
                    Stroke tmp = bar;
                    bar = stem;
                    stem = tmp;
                }
                if ((bar.GetBoundingBox().Bottom - stem.GetBoundingBox().Top + 0.0) / stem.GetBoundingBox().Height < 0.33)
                    letter = "I";
                else letter = "+/-";
            }
            else return false;
            stks = r.strokes;
            stks.Add(s.s);
            return true;
        }
        bool match_bar(CuspSet s, Recognition r, ref string letter, ref Strokes stks)
        {
            if (r == null || s.s.Ink.Strokes.Count < 2 || (letter != "-" && letter != "~"))
                return false;
            bool tmp = Math.Abs((r.strokes.GetBoundingBox().Left + r.strokes.GetBoundingBox().Right) / 2.0 - (s.s.GetBoundingBox().Left + s.s.GetBoundingBox().Right) / 2.0) / r.strokes.GetBoundingBox().Width < 0.4;
            int center = (r.strokes.GetBoundingBox().Left + r.strokes.GetBoundingBox().Right) / 2;
            if (char.IsLetter(r.alt.Character) &&
                r.strokes.GetBoundingBox().Top > s.s.GetBoundingBox().Bottom &&
                (r.strokes.GetBoundingBox().Top - s.s.GetBoundingBox().Bottom + 0.0) / r.strokes.GetBoundingBox().Height < 0.3 &&
                Math.Abs(s.s.GetBoundingBox().Width - r.strokes.GetBoundingBox().Width + 0.0) / r.strokes.GetBoundingBox().Width < 1 &&
                 center > s.bbox.Left && center < s.bbox.Right)
                letter = char.ToUpper(r.alt.Character).ToString();
            else return false;
            stks = r.strokes;
            stks.Add(s.s);
            return true;
        }
        bool match2_eq(CuspSet s, CuspSet s2, string letter, string other)
        {
            //if (letter != "-" && letter != "~" && letter != "/" && letter != "\\" && letter != "." && (s.bbox.Width/(float)s.bbox.Height) < 2.5)
            if (letter != "-" && letter != "/" && letter != "\\" && letter != "." && (s.bbox.Width / (float)s.bbox.Height) < 2.5)
                if (s.bbox.Width / (float)s.bbox.Height < 1.5 || s.dist / s.bbox.Width > 1.5)
                    return false;
            //if (other != "-" && other != "~" && other != "/" && other != "\\" && other != "." && other != "uv")
            if (other != "-" && other != "/" && other != "\\" && other != "." && other != "uv")
                if (s2.bbox.Width / (float)s2.bbox.Height < 1.5 || s2.dist / s2.bbox.Width > 1.5)
                    return false;
            Rectangle dummy = s.bbox; dummy.Inflate(0, 10000);
            double ratio = (Rectangle.Intersect(dummy, s2.bbox).Width + 0.0) / Math.Min(s.bbox.Width, s2.bbox.Width);
            if (ratio < 0.3)
                return false;
            if (s.bbox.Height / Math.Max(s2.bbox.Width, s2.bbox.Height) > 2 || s2.bbox.Height / Math.Max(s.bbox.Width, s.bbox.Height) > 2)
                return false;
            PointF d1 = V2D.Sub(s.last, s.pts[0]);
            PointF d2 = V2D.Sub(s2.pts[s2.pts.Length - 1], s2.pts[0]);
            if (d1.X < 0)
                d1 = new PointF(-d1.X, -d1.Y);
            if (d2.X < 0)
                d2 = new PointF(-d2.X, -d2.Y);
            double lRatio = V2D.Length(d1) / V2D.Length(d2);
            if (lRatio > 1) lRatio = 1 / lRatio;
            if (lRatio < 0.5 * Math.Min(1, (V2D.Length(d1) + V2D.Length(d2)) / InkPixel / 75))
                return false;
            // test bar angles against each other and horizontal
            double ang1 = angle(V2D.Normalize(d1), new PointF(1, 0));
            double ang3 = angle(V2D.Normalize(d2), new PointF(1, 0));
            double ang2 = angle(V2D.Normalize(d1), V2D.Normalize(d2));
            float maxdim = Math.Max(s.bbox.Width, s.bbox.Height) / InkPixel;
            if (ang3 > ang1)
                maxdim = Math.Max(s2.bbox.Width, s2.bbox.Height) / InkPixel;
            if (Math.Min(ang1, ang3) > 45 || (ang1 + ang3) / 2 > 35 || (maxdim > 5 && ang2 - Math.Min(ang1, ang3) > 40))
                return false;

            int mi_x = Math.Min(s.s.GetBoundingBox().Left, s2.s.GetBoundingBox().Left);
            int ma_x = Math.Max(s.s.GetBoundingBox().Right, s2.s.GetBoundingBox().Right);
            return true;
        }
        public bool match_5(CuspSet s)
        {
            if (s.cusps.Length < 4)
                return false;
            double aspect = (s.bbox.Width / (0.0 + s.bbox.Height));
            if (aspect < 0.3)
                return false;
            if (s.cusps.Length < 4)
                return false;
            int topind; miny(0, s.pts.Length, s.pts, out topind);
            if (topind == -1)
                return false;
            int midstemind; maxy(topind, s.pts.Length / 2, s.pts, out midstemind);
            bool left;
            if (midstemind == -1)
                return false;
            int topcornerind; V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[0], s.pts[midstemind])), out left, out topcornerind, 0, midstemind);
            if (topcornerind == -1)
                return false;
            int start = 0;
            double tang = angle(s.pts[topcornerind], s.pts[0], new PointF(0, -1));
            if (tang < 30 && s.distances[topcornerind] / s.bbox.Height < 0.3)
            {
                start = topcornerind;
                V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[start], s.pts[midstemind + start])), out left, out topcornerind, 0, midstemind + start);
            }
            if (angle(s.cusps[1].pt, s.pts[0], V2D.Sub(s.cusps[1].pt, s.cusps[2].pt)) < 20 && s.cusps[1].pt.X > s.pts[0].X)
                start = s.cusps[1].index;
            if (Math.Abs(s.cusps[start == 0 ? 1 : 2].dist - s.distances[topcornerind]) / s.dist < 0.1)
                topcornerind = s.cusps[start == 0 ? 1 : 2].index;
            if (!left)
                return false;
            double topBarAng = angle(s.pts[Math.Max(topcornerind, 5)], s.pts[start], new PointF(-1, 0));
            if (topBarAng > 40)
                return false;
            if ((s.pts[start].X - s.bbox.Left + 0.0) / s.bbox.Width < 0.25)
                return false;
            int midCusp = topcornerind < s.cusps[1].index ? 1 : 2;
            if (topcornerind == s.cusps[2].index)
                midCusp = 3;
            else if (topcornerind < s.cusps[2].index && midCusp == 2 && V2D.Straightness(s.pts, topcornerind, s.cusps[2].index) < .06 && s.cusps[2].curvature > 0)
                midCusp = 3;
            if (Math.Abs(s.curvatures[topcornerind]) < 0.1)
            {
                int tmpcorner;
                s.maxCurveSeg(2, midstemind - 2, out tmpcorner);
                if (tmpcorner > topcornerind && angle(s.pts[0], s.pts[topcornerind], V2D.Sub(s.cusps[midCusp].pt, s.pts[topcornerind])) >
                    angle(s.pts[0], s.pts[tmpcorner], V2D.Sub(s.cusps[midCusp].pt, s.pts[tmpcorner])))
                    topcornerind = tmpcorner;
            }
            double topBarStr = V2D.Straightness(s.pts, start, topcornerind);
            double downAng = angle(s.cusps[midCusp].pt, s.pts[topcornerind], new Point(-1, 2));
            double topCornerCrv = s.curvatures[topcornerind];
            int midcuspInd = s.cusps[midCusp].index;
            if (midCusp == s.cusps.Length - 1)
                return false;
            if (downAng > 35 && s.bbox.Height / InkPixel < 20)
            {
                for (int m = topcornerind + 2; m < s.cusps[midCusp].index; m++)
                    if (s.curvatures[m] < s.curvatures[midcuspInd])
                        midcuspInd = m;
                downAng = angle(s.pts[midcuspInd], s.pts[topcornerind], new Point(-1, 2));
            }
            double downBarStr = V2D.Straightness(s.pts, topcornerind, midcuspInd);
            double downBarCrv = s.curvatures[midcuspInd];
            double topcornerAng = angle(s.pts[start], s.pts[topcornerind], V2D.Sub(s.pts[midcuspInd], s.pts[topcornerind]));
            double midcornerAng = angle(s.pts[midcuspInd + 2], s.pts[midcuspInd], new PointF(0, -1));
            if (downAng > (topBarStr < 0.1 ? 40 : 35) && midcuspInd == s.cusps[midCusp].index &&
                    s.cusps[midCusp - 1].curvature > -1.25 && s.cusps[midCusp].curvature > -0.4)
                return false;
            if ((s.distances[midcuspInd] - s.distances[topcornerind]) / s.bbox.Height < Math.Max(InkPixel * 6 / (midcuspInd - topcornerind) / s.bbox.Height, .075))
                return false;
            if ((downBarStr > 0.14 || topBarStr > 0.14) && topCornerCrv > -.4 && downBarCrv > -.6 && topcornerAng > 100 && midcornerAng > 100)
                return false;
            if (topcornerAng > 140 || (topCornerCrv > -.2 && downBarCrv > -.4) || (topcornerAng > 130 && downBarCrv > -.6 && topBarStr > .1 && downBarStr > .1))
                return false;
            if ((downAng > 40 || topBarStr > 0.125) && downBarCrv > -.3 && midcornerAng > 100)
                return false;
            if (topCornerCrv > -.3 && topBarStr > .2 && downBarStr > 0.09)
                return false;
            if (downBarCrv > -.2 || (topCornerCrv > -.2 && V2D.Straightness(s.pts, start, midcuspInd) < .25 &&
                    !(topBarStr < 0.1 && s.distances[topcornerind] / s.bbox.Width > 0.6)) || topcornerAng > 140)
                return false;
            if (topCornerCrv > -.7 && downBarStr > 0.20 && downBarCrv > -.5)
                return false;
            if (s.cusps.Length > 4 && s.cusps[3].curvature > 1 && s.cusps[3].curvature > -topCornerCrv)
                return false;
            int nextCusp = s.cusps[midCusp].index == midcuspInd ? midCusp + 1 : midCusp;
            if (s.cusps[nextCusp].pt.Y < s.pts[midcuspInd].Y &&
                    (s.cusps[nextCusp].curvature < 0 || (topCornerCrv > -.6 && downBarCrv > -.6 && s.cusps[nextCusp].curvature > .7)))
                return false;
            for (int c = midCusp + 1; c < s.cusps.Length - 1; c++)
            {
                double cuspang = angle(s.cusps[c - 1].pt, s.cusps[c].pt, V2D.Sub(s.pts[s.cusps[c].index + Math.Min(10, (s.cusps[c + 1].index - s.cusps[c].index) / 3)], s.cusps[c].pt));
                if (s.cusps[1].pt.Y > s.cusps[c].pt.Y && cuspang < 70)
                    return false;
            }
            if (s.cusps.Length < 7 &&
                    s.pts[start].X < s.cusps[midCusp].pt.X ||
                    s.pts[start].X < s.pts[topcornerind].X ||
                    downAng > 65 ||
                    s.cusps[3].pt.X < s.last.X ||
                    s.pts[start].Y > s.cusps[3].pt.Y ||
                    s.cusps[midCusp].pt.Y > s.last.Y)
                return false;
            else return true;
        }
        bool match_v(CuspSet s)
        {
            if (s.cusps.Length > 4 || (0.0 + s.bbox.Height) / s.bbox.Width < 0.4)
                return false;
            if (s.intersects.Length > 1 && s.distances[s.intersects[1]] - s.distances[s.intersects[0]] > InkPixel * 2)
                return false;
            int botind;
            int my = maxy(0, s.pts.Length, s.pts, out botind);
            if (botind < 2 || botind > s.pts.Length - 3)
                return false;
            bool left;
            double startStraight = V2D.Straightness(s.pts, botind / 4, botind - 1);
            double stopStraight = V2D.Straightness(s.pts, botind, (s.pts.Length + botind) / 2 + 1);
            double startStraight2 = V2D.Straightness(s.pts, 2, botind);
            double stopStraight2 = V2D.Straightness(s.pts, botind, s.pts.Length, out left);
            if (angle(s.pts[0], s.pts[botind], V2D.Sub(s.last, s.pts[botind])) <
                .6 * angle(s.pts[botind / 2], s.pts[botind], V2D.Sub(s.pts[botind + (s.pts.Length - botind) / 2], s.pts[botind])))
                return false;
            int topind; miny(botind, s.pts.Length, s.pts, out topind);
            double vang = angle(s.pts[0], s.pts[botind], V2D.Sub(s.pts[botind + (topind - botind) / 2], s.pts[botind]));
            double stemang = angle(s.pts[0], s.pts[botind], new PointF(0, -1));
            if (left && startStraight2 < 0.8 && (stopStraight > 0.06 || stopStraight2 > 0.06) &&
                stemang < 10 && vang < 35 - stemang && Math.Abs(s.angles[topind]) > (stopStraight2 > 0.1 ? 2.6 : 2.85)) // this is probably an 'r'
                return false;
            if (s.cusps[0].left &&
                s.pts[botind].X < s.last.X &&
                s.pts[botind].Y > s.cusps[0].pt.Y &&
                s.pts[botind].Y > s.last.Y &&
                s.cusps[s.l].top && !s.cusps[0].bot &&
                stopStraight2 < (left ? Math.Min(0.25, Math.Max(0.16, stemang / 100.0)) : 0.14) &&
                startStraight2 < 0.15)
                return true;
            return false;
        }
        bool match_nu(CuspSet s)
        {
            if ((s.bbox.Height + 0.0) / s.bbox.Width < 0.5)
                return false;
            if (s.intersects.Length > 1 && s.distances[s.intersects[1]] - s.distances[s.intersects[0]] > InkPixel * 5)
                return false;
            if (s.straight < 0.1)
                return false;
            int botind;
            int my = maxy(0, s.pts.Length, s.pts, out botind);
            if (botind < 2 || botind > s.pts.Length - 3)
                return false;
            if (s.cusps.Length == 4 && s.cusps[2].index > botind && s.cusps[2].curvature > 0)
                return false;
            bool left;
            double startStraight = V2D.Straightness(s.pts, 0, botind);
            double stopStraight = V2D.Straightness(s.pts, botind, s.pts.Length, out left);
            foreach (CuspRec c in s.cusps)
                if (c.index > botind)
                {
                    double ang = angle(s.pts[botind], c.pt, new PointF(-1, 0));
                    if (ang < 12)
                        return false;
                    break;
                }
            if (stopStraight > 0.5)
                return false;
            if (s.cusps[0].left &&
                s.cusps[1].pt.Y > s.cusps[0].pt.Y &&
                s.cusps[1].pt.Y > s.last.Y &&
                !s.cusps[s.l].bot &&
                (startStraight < .25 && stopStraight > 0.13 && !left))
                return true;
            return false;
        }

        bool match_mu(CuspSet s)
        { // Greek letter mu
            if (s.cusps.Length != 5 && s.cusps.Length != 4)
                return false;

            //int tailCupIndex = s.cusps.Length - 1;
            Point last = (s.cusps.Length == 5 ? s.cusps[4].pt : s.s.GetPoint(s.s.GetPoints().Length - 1));

            if (!s.cusps[0].top && s.cusps[0].bot &&
                s.cusps[0].pt.Y > s.cusps[2].pt.Y && s.cusps[0].pt.Y > last.Y &&
                s.cusps[2].pt.Y > s.cusps[1].pt.Y && last.Y > s.cusps[3].pt.Y &&
                s.cusps[1].top && s.cusps[3].top && s.cusps[1].pt.X < s.cusps[3].pt.X &&
                s.cusps[0].pt.X < s.cusps[2].pt.X && s.cusps[2].pt.X < last.X)
            {
                //int noIntersections = s.intersects.Length;

                // approx length of last arc on right hand side of mu
                double lastDist = (s.cusps.Length == 5 ? s.dist - s.cusps[3].dist : V2D.Dist(last, s.cusps[3].pt));
                // proportionate sizes of first and last arcs
                double dist = (s.cusps[1].dist - s.cusps[0].dist) / lastDist;
                // we want the first arc to generally have negative curvature (as opposed to m which should be more positive)
                double avgCurve = s.avgCurve(0, 1);
                double topRatio = ((double)Math.Abs(s.cusps[3].pt.Y - s.cusps[1].pt.Y)) / s.bbox.Height;
                //System.Console.WriteLine(topRatio);
                if (angle(s.last, s.cusps[s.nl].pt, new PointF(1, 0)) < 45)
                    return false;
                if (avgCurve < 0.33f && dist > 1.3333f && dist < 5 && topRatio < 0.27)
                    return true;
            }

            return false;
        }
        void lobefeatures(CuspSet s, int start, int stop, out int botind, out int topind, out int leftind, out int rightind)
        {
            int botlobe, toplobe, leftlobe, rightlobe;
            botlobe = maxy(start, stop, s.pts, out botind);
            toplobe = miny(start, stop, s.pts, out topind);
            leftlobe = minx(start, stop, s.pts, out leftind);
            rightlobe = maxx(start, stop, s.pts, out rightind);
        }
        public bool match_Delta(CuspSet s, ref string allograph)
        {
            /* we try to recognize nabla/del and the left- and right- pointing triangles as well */

            int startCusp = 0;
            // allow an initial segment that doubles-back on itself
            if (s.cusps.Length == 5 && V2D.Straightness(s.pts, s.cusps[0].index, s.cusps[1].index) < 0.2 &
                angle(s.cusps[1].pt, s.cusps[0].pt, V2D.Sub(s.cusps[1].pt, s.cusps[2].pt)) < 15)
                startCusp = 1;
            Point last = s.last;
            if (s.cusps.Length - startCusp != 4 && s.cusps.Length - startCusp != 5) return false;
            if (V2D.Straightness(s.pts, s.cusps[startCusp].index, s.cusps[startCusp + 1].index) > 0.2) return false;
            if (V2D.Straightness(s.pts, s.cusps[startCusp + 1].index, s.cusps[startCusp + 2].index) > 0.2) return false;
            if (V2D.Straightness(s.pts, s.cusps[startCusp + 2].index, s.cusps[startCusp + 3].index) > 0.2)
            {
                //allow last segment to be rather curvy  as long as it backtracks along the start segment
                bool leftside;
                int maxind;
                V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.cusps[startCusp + 3].pt, s.cusps[startCusp + 2].pt)), out leftside, out maxind,
                    s.cusps[startCusp + 2].index, s.cusps[startCusp + 3].index);
                if (V2D.Straightness(s.pts, s.cusps[startCusp + 2].index, maxind) > 0.15 ||
                    V2D.Straightness(s.pts, maxind, s.cusps[startCusp + 3].index) > 0.3 ||
                    V2D.Dist(s.pts[maxind], s.cusps[startCusp + 0].pt) / Math.Min(s.bbox.Width, s.bbox.Height) > 0.25 ||
                    angle(s.cusps[startCusp + 3].pt, s.pts[maxind], V2D.Sub(s.cusps[startCusp + 1].pt, s.cusps[startCusp + 0].pt)) > 45)
                    return false;
                last = s.pts[maxind];
            }
            if (s.cusps.Length == 5 + startCusp)
            {  // allow an extra segment retracing first
                if ((V2D.Straightness(s.pts, s.cusps[startCusp + 3].index, s.cusps[startCusp + 4].index) > 0.2 ||
                angle(s.cusps[startCusp + 4].pt, s.cusps[startCusp + 3].pt, V2D.Sub(s.cusps[startCusp + 1].pt, s.cusps[startCusp + 0].pt)) > 15))
                    return false;
                last = s.cusps[startCusp + 3].pt;
            }

            // find extrema
            Point left = s.cusps[startCusp + 0].pt;
            Point top = s.cusps[startCusp + 1].pt;
            Point right = s.cusps[startCusp + 2].pt;
            Point bottom = s.cusps[startCusp + 0].pt;
            for (int i = startCusp; i <= startCusp + 2; i++)
            {
                if (s.cusps[i].pt.X < left.X) left = s.cusps[i].pt;
                if (s.cusps[i].pt.Y < top.Y) top = s.cusps[i].pt;
                if (s.cusps[i].pt.X > right.X) right = s.cusps[i].pt;
                if (s.cusps[i].pt.Y > bottom.Y) bottom = s.cusps[i].pt;
            }

            // which triangle case is it?
            Point mid = new Point(), a = new Point(), b = new Point();
            double dist = Double.PositiveInfinity;
            double Ad, Bd;
            string al = null;
            Ad = top.X - left.X;
            Bd = right.X - top.X;
            if (Ad > 0 && Bd > 0 && Math.Abs(Ad / (Ad + Bd) - 0.5) < dist)
            {
                dist = Math.Abs(Ad / (Ad + Bd) - 0.5);
                mid = top; a = left; b = right;
                al = "Delta";
            }
            Ad = bottom.X - left.X;
            Bd = right.X - bottom.X;
            if (Ad > 0 && Bd > 0 && Math.Abs(Ad / (Ad + Bd) - 0.5) < dist)
            {
                dist = Math.Abs(Ad / (Ad + Bd) - 0.5);
                mid = bottom; a = left; b = right;
                al = "nabla";
            }
#if false // this doesn't work -- confusion between Delta and ltri if Delta is drawn with its top next to right side but still taller than base
            Ad = left.Y - top.Y;
            Bd = bottom.Y - left.Y;
            if(Ad > 0 && Bd > 0 && Math.Abs(Ad/(Ad+Bd) - 0.5) < dist) {
                dist = Math.Abs(Ad/(Ad+Bd) - 0.5);
                mid = left; a = top; b = bottom;
                al = "ltri";
            }
            Ad = right.Y - top.Y;
            Bd = bottom.Y - right.Y;
            if(Ad > 0 && Bd > 0 && Math.Abs(Ad/(Ad+Bd) - 0.5) < dist) {
                dist = Math.Abs(Ad/(Ad+Bd) - 0.5);
                mid = right; a = top; b = bottom;
                al = "rtri";
            }
#endif

            if (al == null) return false;

            //Reject triangles that are not close to being isoceles across the two equal sides
            double aDist = V2D.Dist(mid, a);
            double bDist = V2D.Dist(mid, b);
            if (aDist > 1.5 * bDist || bDist > 1.5 * aDist) return false;

            //Make sure the end point is relatively close to the start point
            double thresh = al == "nabla" ? 0.45 : 0.3;
            double avglen = 0;
            for (int i = startCusp + 1; i < startCusp + 4; i++) avglen += V2D.Dist(s.cusps[i].pt, s.cusps[i - 1].pt);
            avglen /= 3;
            if (V2D.Dist(s.cusps[startCusp + 0].pt, last) > thresh * avglen &&
                V2D.Dist(s.last, s.cusps[s.l - 3].pt) > thresh * avglen &&
                V2D.Dist(s.last, s.pts[0]) > thresh * avglen &&
                (s.cusps.Length == 4 || angle(s.last, s.pts[0], V2D.Sub(s.last, s.cusps[s.nl].pt)) > 10)) return false;

            allograph = al;
            return true;
        }

        int closestPointInArray(Point[] pointArray, int[] indexArray, Point refpoint)
        {
            double mindist = 0;
            int minind = -1;
            foreach (int i in indexArray)
            {
                double cdist = V2D.Dist(pointArray[i], refpoint);
                if (cdist < mindist || minind == -1)
                {
                    mindist = cdist;
                    minind = i;
                }
            }

            return minind;
        }


        int closestPointInArray(Point[] pointArray, Point refpoint)
        {
            double mindist = 0;
            int minind = -1;
            for (int i = 0; i < pointArray.Length; i++)
            {
                double cdist = V2D.Dist(pointArray[i], refpoint);
                if (pointArray[i] != refpoint && (cdist < mindist || minind == -1))
                {
                    mindist = cdist;
                    minind = i;
                }
            }

            return minind;
        }

        bool match_infinity(CuspSet s)
        {
            if (s.intersects.Length >= 2)
            {
                int i1 = s.intersects[s.intersects.Length / 2 - 1];
                int i2 = s.intersects[s.intersects.Length / 2];
                if ((s.distances[i2] - s.distances[i1]) / s.bbox.Width < 0.25)
                    return false;
                int rind, bind, tind;
                maxx(i1, i2, s.pts, out rind);
                if (rind == -1 || s.curvatures[rind] < 0)
                    return false;
                maxy(rind, i2, s.pts, out bind);
                if (bind == -1 || s.curvatures[bind] < 0 || V2D.Straightness(s.pts, bind, i2) > 0.4 || V2D.Straightness(s.pts, rind, bind) > 0.4)
                    return false;
                miny(i1, rind, s.pts, out tind);
                if (tind == -1 || s.curvatures[tind] < 0 || V2D.Straightness(s.pts, i1, tind) > 0.4 || V2D.Straightness(s.pts, tind, rind) > 0.4)
                    return false;
                if (s.cusps[0].right || s.cusps[s.l].right)
                    return false;
                int ltop, lbot;
                miny(i2, s.pts.Length, s.pts, out ltop);
                maxy(0, i1, s.pts, out lbot);
                if (Math.Abs(s.angles[ltop]) > Math.PI / 2 || (lbot >= 0 && Math.Abs(s.angles[lbot]) < Math.PI / 2))
                    return false;
                if (maxx(0, i1, s.pts) > Math.Max(s.pts[i1].X, s.pts[i2].X) ||
                    maxx(i2, s.pts.Length, s.pts) > Math.Max(s.pts[i1].X, s.pts[i2].X))
                    return false;
                if ((Math.Min(s.pts[i1].X, s.pts[i2].X) - s.bbox.Left + 0.0) / s.bbox.Width < 0.2)
                    return false;
                return true;
            }
            return false;
        }
        bool match_2infty(CuspSet s)
        {

            //simple test to catch lopsided stuff... test width height ratio
            if (s.bbox.Width < s.bbox.Height * 0.8)
            {
                return false;
            }


            //now we need to find the two "centers" - the points of intersection
            //in the middle, or at least approximate them if they don't exist

            int smallerIntersectIndex, largerIntersectIndex;

            //if we have many intersections, find the once closest to the center, then find its paired intersection
            if (s.intersects.Length > 2)
            {
                Point centerpoint = new Point(s.bbox.X + s.bbox.Width / 2, s.bbox.Y + s.bbox.Height / 2);
                int intersectionPoint = closestPointInArray(s.pts, s.intersects, centerpoint);
                int otherIntersectionPoint = closestPointInArray(s.pts, s.pts[intersectionPoint]);

                smallerIntersectIndex = Math.Min(intersectionPoint, otherIntersectionPoint);
                largerIntersectIndex = Math.Max(intersectionPoint, otherIntersectionPoint);


            }
            //easy to compute "center" if there is ONLY one intersect 
            else if (s.intersects.Length == 2)
            {
                if (V2D.Dist(s.pts[0], s.last) / s.bbox.Width > 0.4)
                    return false;
                smallerIntersectIndex = s.intersects[0];
                largerIntersectIndex = s.intersects[1];
            }
            //if no intersect, we need to be a bit more clever. Calculate the point
            //closest to the beginning of the stroke which is between the 2nd and 3rd
            //"humps" (min/maxes on each side)
            else
            {
                if (s.cusps.Length < 4 && V2D.Straightness(s.pts, 0, s.cusps[1].index) < 0.2 &&
                    V2D.Straightness(s.pts, s.cusps[1].index, s.pts.Length) < 0.2)
                    return false;
                if (V2D.Dist(s.pts[0], s.last) / s.bbox.Width > 0.25)
                    return false;

                int topfirsthalf, bottomfirsthalf, topsecondhalf, bottomsecondhalf;
                //get top of first curve
                int to_1_y = miny(0, s.pts.Length / 2, s.pts, out topfirsthalf);
                //get bottom of first curve
                int bo_1_y = maxy(0, s.pts.Length / 2, s.pts, out bottomfirsthalf);
                //get top of secondt curve
                int to_2_y = miny(s.pts.Length / 2, s.pts.Length - 1, s.pts, out topsecondhalf);
                //and the bottom of the second curve
                int bot_2_y = maxy(s.pts.Length / 2, s.pts.Length - 1, s.pts, out bottomsecondhalf);


                int[] keypoints = new int[4];
                keypoints[0] = topfirsthalf;
                keypoints[1] = bottomfirsthalf;
                keypoints[2] = topsecondhalf;
                keypoints[3] = bottomsecondhalf;

                Array.Sort(keypoints);

                double mindist = 0;
                int minind = -1;
                for (int i = keypoints[1]; i <= keypoints[2]; i++)
                {
                    double cdist = V2D.Dist(s.pts[i], s.pts[0]);
                    if (cdist < mindist || minind == -1)
                    {
                        mindist = cdist;
                        minind = i;
                    }
                }
                smallerIntersectIndex = 0;
                largerIntersectIndex = minind;
            }





            //now, find the humps again but this time segragate using the intersect values
            int realtop1, realtop2, realtop2a, realtop2b, realbottom1, realbottom2, realbottom2a, realbottom2b;

            miny(smallerIntersectIndex, largerIntersectIndex, s.pts, out realtop1);
            miny(largerIntersectIndex, s.pts.Length, s.pts, out realtop2a);
            miny(0, smallerIntersectIndex, s.pts, out realtop2b);

            if (realtop2b < 0 || s.pts[realtop2a].Y < s.pts[realtop2b].Y)
            {
                realtop2 = realtop2a;
            }
            else
            {
                realtop2 = realtop2b;
            }


            maxy(smallerIntersectIndex, largerIntersectIndex, s.pts, out realbottom1);
            maxy(largerIntersectIndex, s.pts.Length, s.pts, out realbottom2a);
            maxy(0, smallerIntersectIndex, s.pts, out realbottom2b);

            if (realbottom2b < 0 || s.pts[realbottom2a].Y > s.pts[realbottom2b].Y)
            {
                realbottom2 = realbottom2a;
            }
            else
            {
                realbottom2 = realbottom2b;
            }


            //now do cooridinate tests to assign the just found values to the 
            //appropriate quadrant
            int topLeftHalf, topRightHalf, bottomLeftHalf, bottomRightHalf;

            if (s.pts[realtop1].X > s.pts[realtop2].X)
            {
                topRightHalf = realtop1;
                topLeftHalf = realtop2;
            }
            else
            {
                topLeftHalf = realtop1;
                topRightHalf = realtop2;
            }

            if (s.pts[realbottom1].X > s.pts[realbottom2].X)
            {
                bottomRightHalf = realbottom1;
                bottomLeftHalf = realbottom2;
            }
            else
            {
                bottomLeftHalf = realbottom1;
                bottomRightHalf = realbottom2;
            }



            //we now angle-test the larger intersect point. This will give us one
            //of four cases which will let us figure out which direction the
            //shape is being drawn in.

            //based on this info, we then decide which quadrants needs to be tested
            //for what kind of curvature (clockwise vs. counterclockwise)

            double theAngle = s.angles[largerIntersectIndex];
            int ccwi, ccwx, cwi, cwx;

            if (theAngle > 0 && theAngle < Math.PI / 2)
            {
                cwi = topRightHalf;
                cwx = bottomRightHalf;
                ccwi = topLeftHalf;
                ccwx = bottomLeftHalf;
            }
            else if (theAngle < 0 && theAngle > -Math.PI / 2)
            {
                ccwx = topRightHalf;
                ccwi = bottomRightHalf;
                cwx = topLeftHalf;
                cwi = bottomLeftHalf;
            }
            else if (theAngle > Math.PI / 2)
            {
                cwi = topRightHalf;
                cwx = bottomRightHalf;
                ccwi = topLeftHalf;
                ccwx = bottomLeftHalf;
            }
            else
            {
                //if (theAngle < - Math.PI/2) {
                ccwx = topRightHalf;
                ccwi = bottomRightHalf;
                cwx = topLeftHalf;
                cwi = bottomLeftHalf;
            }

            //make a copy because we're going to change this variable and we'll need it later
            int cwisave = cwi;
            int ccwisave = ccwi;

            //one or neither of these booleans will be true if the left or right side of the
            //shape is the part which contains the beginning & end of the stroke
            bool ccwInterrupt = false, cwInterrupt = false;


            //perform the clockwise test by cycling from start to finish
            //note that we allow 0.1 in the opposite direction so we
            //don't trigger on pen wobbles
            int distToGo = cwx - cwi;
            if (distToGo < 0)
            {
                distToGo += s.pts.Length;
            }

            for (int i = 0; i <= distToGo; i++)
            {
                if ((cwi + i) >= s.pts.Length)
                    cwInterrupt = true;
                if (s.curvatures[(cwi + i) % s.pts.Length] <= -0.1)
                {
                    return false;
                }
            }



            //same thing: perform the counterclockwise test by cycling from 
            //start to finish. note that we allow 0.1 in the opposite direction so we
            //don't trigger on pen wobbles
            int distToGo2 = ccwx - ccwi;
            if (distToGo2 < 0)
            {
                distToGo2 += s.pts.Length;
            }

            for (int i = 0; i <= Math.Min(s.pts.Length - 5, distToGo2); i++)
                if ((s.dist - s.distances[(ccwi + i) % s.pts.Length]) / s.bbox.Width > 0.1)
                {
                    if ((ccwi + i) >= s.pts.Length)
                        ccwInterrupt = true;
                    if (s.curvatures[(ccwi + i) % s.pts.Length] >= 0.1)
                    {
                        return false;
                    }
                }


            //now we find the "height" of the figure at the humps on the side
            //which is not "interrupted" (doesn't have the start/end). Compare this
            //to the distance between the start and endpoints to make sure they aren't
            //too far apart. This prevents the "Christian Fish" from being recognized
            //as infinity.
            if (ccwInterrupt || cwInterrupt)
            {
                double maxdist;
                if (ccwInterrupt)
                    maxdist = V2D.Dist(s.pts[ccwisave], s.pts[ccwx]);
                else
                    maxdist = V2D.Dist(s.pts[cwisave], s.pts[cwx]);
                if (V2D.Dist(s.pts[0], s.pts[s.pts.Length - 1]) > maxdist * .7)
                {
                    return false;
                }
            }


            //we made it through the battery of tests, return true.
            return true;

        }




        public bool match_8(CuspSet s, ref int xhgt)
        {
            if (s.intersects.Length < 2)
                return false;
            int start = 0, stop = s.pts.Length - 1;
            SortedList inters = new SortedList();
            for (int i = 0; i < s.intersects.Length; i++)
            {
                int cnt = 0;
                while (inters.Contains(s.pts[s.intersects[i]].Y + cnt))
                    cnt++;
                inters.Add(s.pts[s.intersects[i]].Y + cnt, i);
            }
            int inter0 = s.intersects[(int)inters.GetValueList()[inters.GetValueList().Count - 1]];
            int inter1 = s.intersects[(int)inters.GetValueList()[inters.GetValueList().Count - 2]];
            if (inter0 > inter1)
            {
                int tmp = inter0; inter0 = inter1; inter1 = tmp;
            }
            if (inter0 > 0)
                start = 0;
            if (inter1 < s.intersects.Length - 1)
                stop = s.pts.Length - 1;
            Point startPt = s.pts[start];
            Point stopPt = s.pts[stop];
            if (startPt.Y > s.pts[inter0].Y || stopPt.Y > s.pts[inter1].Y)
                return false;
            if ((s.distances[inter0] + s.dist - s.distances[inter1] + 0.0) / s.dist < 0.25)
                return false;
            double near = V2D.Dist(startPt, stopPt);
            for (int i = 0; i < convertIndex(5, s.skipped); i++)
            {
                double disttostart = V2D.Dist(s.s.GetPoint(i), stopPt);
                if (disttostart < near)
                {
                    near = disttostart;
                }
            }
            if ((Math.Max(maxx(0, inter0, s.pts), maxx(inter1, s.pts.Length, s.pts)) - minx(inter0, inter1, s.pts)) / (float)s.bbox.Width < 0.2)
                return false;
            //if ((maxx(inter0, inter1, s.pts) - Math.Min(minx(0, inter0, s.pts), minx(inter1, s.pts.Length, s.pts)))/(float)s.bbox.Width < 0.2)
            //    return false;
            for (int i = convertIndex(s.s.GetPoints().Length - 6, s.skipped); i < s.s.GetPoints().Length; i++)
            {
                double disttostart = V2D.Dist(s.s.GetPoint(i), startPt);
                if (disttostart < near)
                {
                    near = disttostart;
                }
            }
            if (V2D.Straightness(s.pts, 0, inter0) < .12 && V2D.Straightness(s.pts, inter1, s.pts.Length) < .12)
                return false;
            if ((near / s.bbox.Width > 0.7 ||
                (near / s.bbox.Width > 0.4 && angle(s.pts[0], s.pts[4], new PointF(-1, 0)) < 25 &&
                   angle(s.last, s.pts[s.pts.Length - 4], new Point(1, 0)) < 25)) &&
                s.intersects.Length == 2)
                return false;
            bool lefty = Math.Sign(s.avgCurveSeg(inter0, inter1)) == -1;
            int botind, topind, leftind, rightind;
            if (inter1 + 2 >= s.pts.Length) return false;
            lobefeatures(s, inter0 + 2, inter1 + 2, out botind, out topind, out leftind, out rightind);
            double maxgood0 = s.maxCurveSeg((int)Math.Ceiling(inter0 * .1), inter0);
            double maxgood1 = s.maxCurveSeg(inter1, s.curvatures.Length - 3);
            if (lefty)
            {
                if (leftind > inter1)
                    leftind = inter0;
                if (!(leftind < botind && botind < rightind && rightind < topind))
                    return false;
                if (s.avgCurveSeg(start, inter0) < 0 && s.avgCurveSeg(inter1, stop) < 0)
                    return false;
                if (maxgood0 < 0 && -maxgood0 > maxgood1)
                    return false;
                if (maxgood1 < 0 && -maxgood1 < maxgood0)
                    return false;
                for (int i = 3; i < inter0; i++)
                    if (s.curvatures[i] < -0.45)
                        return false;
                for (int i = inter1; i < s.curvatures.Length - 3; i++)
                    if (s.curvatures[i] < -0.45)
                        return false;
            }
            else
            {
                if (rightind > inter1)
                    rightind = inter0;
                if (!(rightind < botind && botind < leftind && leftind < topind))
                    return false;
                if (s.avgCurveSeg(start, inter0) > 0 && s.avgCurveSeg(inter1, stop) > 0)
                    return false;
                if (maxgood0 > 0 && maxgood0 > -maxgood1)
                    return false;
                if (maxgood1 > 0 && maxgood1 > -maxgood0)
                    return false;
                for (int i = (int)Math.Ceiling(inter0 * .1); i < inter0; i++)
                    if (s.curvatures[i] > 0.45)
                        return false;
                for (int i = inter1; i < s.curvatures.Length - 3; i++)
                    if (s.curvatures[i] > 0.45)
                        return false;
            }
            xhgt = s.pts[topind].Y;
            return true;
        }
        public bool match_rp(CuspSet s)
        {
            if (s.cusps.Length > 4 || s.intersects.Length > 2 || s.pts.Length < 5)
                return false;
            if (s.intersects.Length == 2 && V2D.Dist(s.pts[s.intersects[0]], s.pts[s.intersects[1]]) / s.bbox.Height > 0.15)
                return false;

            if (s.cusps[0].pt.Y > s.last.Y && (s.bbox.Bottom - s.cusps[0].pt.Y + 0.0) / s.bbox.Height > 0.25)
                return false;

            if (s.cusps[0].pt.Y < s.last.Y && (s.bbox.Bottom - s.last.Y + 0.0) / s.bbox.Height > 0.25)
                return false;

            double ang = angle(s.s.GetPoint(s.s.GetPoints().Length - 1), s.s.GetPoint(0), new PointF(0, 1));
            double angn = angle(s.s.GetPoint(s.s.GetPoints().Length - 1), s.s.GetPoint(0), new PointF(0, -1));
            ang = Math.Min(ang, angn);
            Point vec = V2D.Sub(s.last, s.cusps[0].pt);
            PointF dir = V2D.Normalize(vec);
            bool left = true;
            int farInd;
            double farthest = V2D.MaxDist(s.pts, dir, out left, out farInd, 0, s.pts.Length);
            for (int i = (int)(s.pts.Length * .9); i < s.pts.Length - 1; i++)
            {
                bool tleft;
                double far = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[i], s.pts[0])), out tleft);
                if (tleft)
                    return false;
                if (far < farthest)
                {
                    farthest = far;
                }
            }
            int maxIndex;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.last, s.cusps[1].pt)), out left, out maxIndex, s.cusps[1].index, s.pts.Length);
            if (left && s.curvatures[maxIndex] < 0 && s.last.X - s.cusps[1].pt.X > 0)
                return false;
            if (s.avgCurveSeg(s.pts.Length / 4, 3 * s.pts.Length / 4) < 0)
                return false;
            for (int c = 1; c < s.cusps.Length - 1; c++)
                if ((s.cusps[c].curvature < 0 || (s.cusps[c].curvature > 2.9 && s.intersects.Length > 1 && s.intersects[0] < s.cusps[c].index &&
                    s.intersects[1] > s.cusps[c].index)) &&
                    V2D.Dist(s.cusps[c].pt, s.pts[0]) / s.bbox.Height > 0.1 && V2D.Dist(s.cusps[c].pt, s.last) / s.bbox.Height > 0.1)
                    return false;
            double aspect = farthest / V2D.Length(vec);
            if (aspect > 0.6 || (aspect > 0.4 && V2D.Straightness(s.pts, 0, farInd) > 0.175 && V2D.Straightness(s.pts, farInd, s.pts.Length) > 0.175))
                return false;
            if (ang < 16 || (ang < 45 && aspect > (left ? 0.10 : 0.06)))
            {
                double nearCenter = double.MaxValue;
                int centIndex = -1;
                for (int i = 0; i < s.pts.Length; i++)
                    if (centIndex == -1 || Math.Abs(s.pts[i].Y - (s.bbox.Top + s.bbox.Bottom) / 2) < nearCenter)
                    {
                        nearCenter = Math.Abs(s.pts[i].Y - (s.bbox.Top + s.bbox.Bottom) / 2);
                        centIndex = i;
                    }
                bool stleft, enleft;
                double startcurve = V2D.Straightness(s.pts, 3, centIndex, out stleft);
                double endcurve = V2D.Straightness(s.pts, centIndex, s.pts.Length - 2, out enleft);
                if ((stleft && startcurve > 0.12) || (enleft && endcurve > 0.12))
                {
                    return false;
                }
                else if (!stleft && !enleft && aspect > 0.04 && farthest > InkPixel * 2)
                    return true;
            }

            return false;
        }


        bool match_E_loopy(CuspSet s)
        {


            //ONE and only one intersection allowed
            int firstIntersectIndex;
            int secondIntersectIndex;
            int loopRightIndex;
            if (s.intersects.Length == 2)
            {
                firstIntersectIndex = s.intersects[0];
                secondIntersectIndex = s.intersects[1];
                maxx(firstIntersectIndex, secondIntersectIndex, s.pts, out loopRightIndex);
            }
            else if (s.intersects.Length == 0)
            {
                int midcusp = -1;
                for (int i = 1; i < s.cusps.Length - 1; i++)
                    if (s.cusps[i].curvature > 1)
                    {
                        midcusp = i;
                        break;
                    }
                if (midcusp == -1)
                    return false;
                firstIntersectIndex = s.cusps[midcusp].index;
                secondIntersectIndex = s.cusps[midcusp].index;
                loopRightIndex = s.cusps[midcusp].index;
            }
            else
                return false;

            int minxind; minx(0, firstIntersectIndex, s.pts, out minxind);
            int minxind2; minx(secondIntersectIndex, s.pts.Length, s.pts, out minxind2);
            if (minxind == -1 || minxind2 == -1)
                return false;
            if (V2D.Straightness(s.pts, 0, minxind) < 0.2 &&
                V2D.Straightness(s.pts, minxind, firstIntersectIndex) < 0.2 &&
                V2D.Straightness(s.pts, secondIntersectIndex, minxind2) < 0.2 &&
                V2D.Straightness(s.pts, minxind2, s.pts.Length) < 0.2)
                return false;
            if (V2D.Straightness(s.pts, 0, firstIntersectIndex) < 0.5 ||
                V2D.Straightness(s.pts, secondIntersectIndex, s.pts.Length) < 0.5)
                return false;

            //test curvature. The whole thing should be CCW
            for (int i = 1; i < s.pts.Length - 1; i++)
            {
                if (s.curvatures[i] + s.curvatures[i - 1] >= 0.1 && (s.intersects.Length > 0 || Math.Abs(i - firstIntersectIndex) > 3))
                    return false;
            }


            //now test to make sure the points are in this order from top to bottom:
            //first point, loopRight point, last point

            if (!(s.pts[0].Y < s.pts[loopRightIndex].Y && s.pts[loopRightIndex].Y < s.pts[s.pts.Length - 1].Y))
            {
                return false;
            }

            //alignment tests

            //first, the X distance between the top and bottom points should be somewhat close to zero
            //we'll use the totally arbirary value of 40% of the width

            if (Math.Abs(s.pts[0].X - s.pts[s.pts.Length - 1].X) > s.bbox.Width * 0.4)
            {
                return false;
            }


            //also, the Y distance between the center of the bounding box and the "loopRight" point
            //(that is, the point which is the further right on the small loop) should be pretty
            //small. Let's use 40%.


            if (Math.Abs(s.pts[loopRightIndex].Y - (s.bbox.Y + s.bbox.Height / 2)) > s.bbox.Height * 0.4)
            {
                return false;
            }


            int toploop = miny(firstIntersectIndex, secondIntersectIndex, s.pts);
            int botloop = maxy(firstIntersectIndex, secondIntersectIndex, s.pts);
            if ((toploop - s.bbox.Top + 0.0) / s.bbox.Height < 0.2 ||
                (s.bbox.Bottom - botloop + 0.0) / s.bbox.Height < 0.2)
                return false;


            return true;
        }

        bool match_lb(CuspSet s) { return match_bracket(s, new PointF(-1, 0)); }
        bool match_rb(CuspSet s) { return match_bracket(s, new PointF(1, 0)); }
        bool match_bracket(CuspSet s, PointF dir)
        {
            bool left;
            int tlInd;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[0], s.pts[s.pts.Length / 2])), out left, out tlInd, 0, s.pts.Length / 2);
            int blInd;
            V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[s.pts.Length / 2], s.last)), out left, out blInd, s.pts.Length / 2, s.pts.Length);
            double topstr = V2D.Straightness(s.pts, 0, tlInd);
            if (topstr > 0.23)
                return false;
            if (V2D.Straightness(s.pts, tlInd, blInd) > 0.125)
                return false;
            if (V2D.Straightness(s.pts, blInd, s.pts.Length) > 0.2 + Math.Max(0, .14 - topstr))
                return false;
            bool reverse = s.pts[0].Y > s.last.Y;
            Point tr = reverse ? s.last : s.pts[0];
            Point tl = reverse ? s.pts[blInd] : s.pts[tlInd];
            Point bl = reverse ? s.pts[tlInd] : s.pts[blInd];
            Point br = reverse ? s.pts[0] : s.last;
            if (reverse)
            {
                int tmp = tlInd;
                tlInd = blInd;
                blInd = tmp;
            }
            double tang2 = angle(tr, tl, V2D.Sub(bl, tl));
            double tang3 = angle(s.pts[reverse ? (tlInd + s.pts.Length - 1) / 2 : tlInd / 2],
                                 s.pts[tlInd], V2D.Sub(s.pts[(tlInd + blInd) / 2], s.pts[tlInd]));
            double hang = angle(tl, tr, dir);
            if ((tang2 > 120 || tang3 > 110) && hang > 25)
                return false;
            double tang = angle(tl, tr, dir);
            if (tang > 45)
                return false;
            double bang = angle(bl, br, dir);
            if (bang > 45)
                return false;
            foreach (CuspRec c in s.cusps)
                if (Math.Abs(s.distances[tlInd] - c.dist) / s.bbox.Height > 0.1 && c.index > tlInd + (reverse ? -2 : 2) &&
                    Math.Abs(s.distances[blInd] - c.dist) / s.bbox.Height > 0.1 && c.index < blInd - (reverse ? -2 : 2) && Math.Abs(c.curvature) > 0.2)
                    return false;
            double stemang = angle(bl, tl, new PointF(0, 1));
            if (stemang > 15)
                return false;
            if ((reverse ? s.dist - s.distances[tlInd] : s.distances[tlInd]) / s.bbox.Height < 0.15)
                return false;
            if ((reverse ? s.distances[blInd] : s.dist - s.distances[blInd]) / s.bbox.Height < 0.15)
                return false;
            if (Math.Abs(s.distances[blInd] - s.distances[tlInd]) / s.bbox.Height < 0.75)
                return false;
            if (s.bbox.Height / (float)s.bbox.Width < 1.25)
                return false;
            return true;
        }
        bool match_lp(CuspSet s, ref string allograph)
        {
            if ((s.intersects.Length > 1 && s.intersects[1] > s.pts.Length / 8 &&
                (s.distances[s.intersects[1]] - s.distances[s.intersects[0]]) / InkPixel > 2) || s.intersects.Length > 3)
                return false;
            if (s.cusps[0].pt.Y > s.last.Y && (s.bbox.Bottom - s.cusps[0].pt.Y + 0.0) / s.bbox.Height > 0.25)
                return false;
            if (s.cusps[0].pt.Y < s.last.Y && (s.bbox.Bottom - s.last.Y + 0.0) / s.bbox.Height > 0.25)
                return false;
            int leftInd, botInd, rightInd;
            int mi_x = minx(0, s.pts.Length, s.pts, out leftInd);
            int ma_x = (s.pts[0].X + s.last.X) / 2;
            if (s.avgCurveSeg(s.pts.Length / 2, s.pts.Length) > 0)
                return false;
            if (s.avgCurveSeg(s.pts.Length / 4, 3 * s.pts.Length / 4) > 0)
                return false;
            if (s.dist / (float)s.bbox.Height > 2)
                return false;
            int ma_y = maxy(0, s.pts.Length, s.pts, out botInd);
            if (leftInd > botInd)
                return false;
            int ri_x = maxx(leftInd, s.pts.Length, s.pts, out rightInd);
            if (rightInd < botInd)
                return false;
            for (int i = 1; i < s.cusps.Length - 1; i++)
                if (s.cusps[i].curvature > 0)
                    return false;
            double topstr = V2D.Straightness(s.pts, s.cusps[0].index, s.cusps[1].index);
            double botstr = V2D.Straightness(s.pts, s.cusps[1].index, s.cusps[2].index);
            if (s.cusps.Length == 3 && topstr < 0.07 && botstr < 0.1 && angle(s.cusps[0].pt, s.cusps[1].pt, V2D.Sub(s.last, s.cusps[1].pt)) < 30)
            {
                allograph = "1)";
                return true;
            }
            if (s.cusps.Length > 3 && topstr < 0.12 && botstr < 0.12 &&
                angle(s.cusps[2].pt, s.cusps[1].pt, new PointF(0, 1)) < 15 &&
                angle(s.cusps[1].pt, s.cusps[0].pt, new PointF(-1, 0)) < 15)
                return false;
            if (Math.Abs(s.cusps[1].curvature) > -.55 &&
                Math.Sign(s.avgCurve(0, s.cusps.Length - 1)) == -1 &&
                V2D.Dist(s.cusps[0].pt, s.last) / s.dist > .18 &&
                s.cusps[0].pt.Y < s.last.Y &&
                (s.last.X > s.cusps[s.nl].pt.X || s.cusps[0].pt.X > s.cusps[s.nl].pt.X || (s.cusps[s.l].dist - s.cusps[s.nl].dist) / InkPixel < 1) &&
                (ma_x - mi_x + 0.0) / s.bbox.Height < .5 &&
                s.cusps[0].top && s.cusps[s.l].bot)
            {
                allograph = s.straight < 0.22 ? "(1" : "(c";
                return true;
            }
            return false;
        }
        bool match_mi(CuspSet s)
        {
            if ((s.bbox.Width + 0.0) / s.bbox.Height < 0.5)
                return false;
            if ((s.bbox.Width + 0.0) / s.bbox.Height > 2 && s.bbox.Height < InkPixel * 5)
                return true;
            if (s.dist / s.bbox.Width > 1.4 && s.bbox.Height > InkPixel * 5)
                return false;
            if (s.cusps.Length > 3 && Math.Sign(s.cusps[1].curvature) != Math.Sign(s.cusps[2].curvature))
                return false;
            if (s.dist / s.bbox.Width > 1.6)
                return false;
            if ((s.bbox.Width + 0.0) / s.bbox.Height > 4)
                return true;
            int mincusp = 0, maxcusp = s.l;
            if (s.cusps[0].pt.X > s.cusps[1].pt.X)
                mincusp = 1;
            if (s.last.X < s.cusps[s.nl].pt.X)
                maxcusp = s.nl;
            if (V2D.Straightness(s.pts, s.cusps[mincusp].index, s.cusps[maxcusp].index) < 0.3)
                return true;
            return false;
        }
        bool match_c(CuspSet s, ref string allograph)
        {
            int startcusp = 0;
            double leadAng = angle(s.cusps[1].pt, s.cusps[0].pt, new PointF(0, 1));
            if (s.cusps[0].top && !s.cusps[1].bot && leadAng < 70 && (s.cusps[1].curvature > 0.4 || (s.cusps[1].curvature < -.4 && s.intersects.Length > 1 && s.intersects[1] > s.cusps[1].index && s.intersects[0] < s.cusps[1].index)))
            {
                int topcind, topc = miny(s.cusps[1].index, (s.pts.Length - s.cusps[1].index) / 2 + s.cusps[1].index, s.pts, out topcind);
                if ((topc - s.cusps[0].pt.Y + 0.0) / s.bbox.Height > 0.225)
                    return false;
                startcusp = 1;
            }
            else if (s.cusps[1].pt.X > s.cusps[0].pt.X)
            {
                startcusp = 1;
                if ((s.cusps[1].dist - s.cusps[0].dist) / s.dist > .2 &&
                    (s.cusps[0].pt.Y - s.cusps[1].pt.Y + 0.0) / s.bbox.Height > 0.1)
                    return false;
            }
            if (s.cusps.Length > 5 + startcusp)
                return false;
            if (s.intersects.Length >= 2 && ((startcusp == 0 && s.intersects[0] > 5) || (startcusp == 1 && s.intersects[0] > s.cusps[startcusp].index)))
                return false;
            double aspect = (s.bbox.Width + 0.0) / s.bbox.Height;
            if (aspect > 2.5 || aspect < 0.35)
                return false;
            if (s.curvatures[s.curvatures.Length / 2] > 0)
                return false;
            if (s.cusps.Length == 4 + startcusp)
            {
                if (V2D.Straightness(s.pts, s.cusps[startcusp].index, s.cusps[startcusp + 1].index) < 0.12 &&
                    V2D.Straightness(s.pts, s.cusps[startcusp + 1].index, s.cusps[startcusp + 2].index) < 0.12 &&
                    angle(s.cusps[startcusp + 2].pt, s.cusps[startcusp + 1].pt, new PointF(0, 1)) < 15 &&
                    angle(s.cusps[startcusp + 1].pt, s.cusps[startcusp + 0].pt, new PointF(-1, 0)) < 15)
                    return false;
            }
            int lobepoints = s.pts.Length - s.cusps[startcusp].index;
            int max_x_ind, min_x_ind, min_y_ind, max_y_ind, last_x_ind;
            miny(s.cusps[startcusp].index, s.pts.Length, s.pts, out min_y_ind);
            maxy(s.cusps[startcusp].index, s.pts.Length, s.pts, out max_y_ind);
            int mi_x = minx(s.cusps[startcusp].index, s.pts.Length - 1, s.pts, out min_x_ind);
            int last_x = maxx(s.cusps[startcusp].index + lobepoints * 3 / 4, s.pts.Length, s.pts, out last_x_ind);
            int ma_x = (maxx(s.cusps[startcusp].index, s.cusps[startcusp].index + lobepoints / 4, s.pts, out max_x_ind) + last_x) / 2;
            if (max_x_ind == -1)
                return false;
            double startAng = angle(s.pts[min_x_ind], s.pts[max_x_ind], new PointF(-1, 0));
            if (startAng > 68)
                return false;
            double ang = angle(s.last, s.pts[max_x_ind], new PointF(0, 1));
            if (ang > 70)
                return false;
            for (int i = startcusp + 1; i < s.cusps.Length - 1; i++)
                if (Math.Abs(s.cusps[i].curvature) > 1.7 ||
                    (Math.Abs(s.cusps[i].curvature) > .9 && s.cusps[i].pt.X < s.pts[0].X && s.cusps[i].pt.Y < s.last.Y && s.cusps[i].pt.Y > s.pts[0].Y &&
                    (V2D.Straightness(s.pts, 0, s.cusps[i].index) < 0.15 || V2D.Straightness(s.pts, s.cusps[i].index, s.pts.Length) < 0.15)) ||
                    Math.Sign(s.cusps[i].curvature) == 1)
                    return false;
            bool left;
            int maxDist;
            double md = V2D.MaxDist(s.pts, V2D.Normalize(V2D.Sub(s.pts[last_x_ind], s.pts[0])), out left, out maxDist, 3, last_x_ind);
            if (md / V2D.Dist(s.pts[last_x_ind], s.cusps[startcusp].pt) < 0.285)
                return false;
            double cang = angle(s.cusps[startcusp].pt, s.pts[maxDist], V2D.Sub(s.pts[last_x_ind], s.pts[maxDist]));
            if (cang > 86)
                return false;
            double aa = angle(s.last, s.pts[maxDist], new Point(1, 1));
            double str1 = V2D.Straightness(s.pts, 3, maxDist);
            double str2 = V2D.Straightness(s.pts, maxDist, s.cusps[s.l].index - 3);
            if (str1 < 0.1 && str2 < 0.1 || (str1 < 0.1 && str2 < .12 && aa < 40))
                return false;
            if (s.cusps[startcusp].pt.Y < s.cusps[s.l].pt.Y &&
                ((str1 > 0.2 && aa > 20) || (str1 > 0.14 && aa > 25) || (str2 > 0.14) ||
                 ((str1 > 0.1 || str2 > 0.1) && aa > 30 && (s.angles[max_y_ind]) < -3 || s.angles[max_y_ind] > 0)) &&
                ((str1 > 0.04 || s.distances[maxDist] / s.dist < 0.5) && (str2 > 0.04 || (s.dist - s.distances[maxDist]) / s.dist < 0.5)) &&
                !s.cusps[startcusp].bot && (s.cusps[s.l].right || !s.cusps[s.l].top))
            {
                allograph = startcusp == 1 && s.cusps[1].pt.Y > s.pts[0].Y ? "cd" : "c" + (cang > 70 ? "(" : "");
                if ((ma_x - mi_x + 0.0) / s.bbox.Height > .4)
                    return true;
                else if ((ma_x - mi_x + 0.0) / s.bbox.Height > .3)
                    return true;
            }
            return false;
        }
        bool match_superset(CuspSet s)
        {
            if (s.cusps.Length > 4 || (s.intersects.Length > 0 && s.intersects[0] > 5) || s.cusps[0].right || s.cusps[s.l].right)
                return false;
            if (s.cusps.Length < 3)
                return false;
            double ang = angle(s.cusps[s.l].pt, s.cusps[0].pt, new PointF(0, 1));
            if (ang > 55)
                return false;
            if ((s.bbox.Width + 0.0) / s.bbox.Height > 2)
                return false;
            int ma_x = maxx(0, s.pts.Length - 1, s.pts);
            int mi_x = (minx(0, s.s.GetPoints().Length / 4, s.s.GetPoints()) + minx(s.s.GetPoints().Length * 3 / 4, s.s.GetPoints().Length, s.s.GetPoints())) / 2;
            for (int i = 1; i < s.cusps.Length - 1; i++)
                if ((Math.Abs(s.cusps[i].curvature) > .9 && s.cusps[i].pt.Y > s.pts[0].Y) ||
                    Math.Sign(s.cusps[i].curvature) == -1)
                    return false;
            if (V2D.Dist(s.cusps[0].pt, s.cusps[2].pt) / s.dist > .18 &&
                s.cusps[0].pt.Y < s.cusps[2].pt.Y &&
                (s.avgCurveSeg(0, s.pts.Length / 2) > 0.05 && s.avgCurveSeg(s.pts.Length / 2, s.pts.Length) > 0.05) &&
                s.cusps[0].top && (s.cusps[s.l].left || s.cusps[s.l].bot))
                if ((ma_x - mi_x + 0.0) / s.bbox.Height > .4)
                    return true;
                else if ((ma_x - mi_x + 0.0) / s.bbox.Height > .3)
                {
                    double angEnds = angle(s.cusps[s.l].pt, s.pts[s.cusps[s.l].index - 3],
                                           V2D.Sub(s.pts[s.cusps[0].index + 3], s.cusps[0].pt));
                    if (angEnds < 25)
                        return true;
                }
            return false;
        }
        bool match_sigma(CuspSet s)
        {
            int hookInd = s.nl;
            if (s.cusps[hookInd].curvature < .5 && s.cusps[hookInd].curvature > -.5)
                return false;
            if (s.cusps[hookInd].curvature < 0 && s.s.SelfIntersections.Length < 2)
                return false;
            if (s.bbox.Width / s.bbox.Height > 2 || s.bbox.Height / s.bbox.Width > 2)
                return false;
            if (s.cusps[0].bot || s.cusps[s.l].bot || !s.cusps[s.nl].top)
                return false;
            int maxx = FeaturePointDetector.maxx(0, s.cusps[hookInd].index, s.pts);
            if ((maxx - s.bbox.Left + 0.0) / s.bbox.Width < 0.3 * ((s.cusps[hookInd].pt.X - s.pts[0].X) / (maxx - s.bbox.Left + 0.0) > 0.3 ? 1.5 : 1))
                return false;
            if (angle(s.cusps[hookInd].pt, s.last, new PointF(-1, 0)) > 35 ||
                V2D.Straightness(s.pts, s.cusps[hookInd].index, s.pts.Length) > 0.15)
                return false;
            for (int i = 1; i < s.nl; i++)
                if (s.cusps[i].curvature > 0 || s.cusps[i].curvature < -0.7)
                    return false;
            if (s.avgCurve(0, s.nl) > 0)
                return false;
            return true;
        }
        public bool match_o(CuspSet s, ref string allograph)
        {
            if (s.pts.Length < 5)
                return false;
            int curvSign = Math.Sign((s.curvatures[s.pts.Length / 3] + s.curvatures[s.pts.Length / 3 - 1] + s.curvatures[s.pts.Length / 3 + 1]) / 3);
            for (int c = 2; c < s.cusps.Length - 1; c++)
                if (Math.Sign(s.avgCurve(c - 1, c)) != curvSign)
                    return false;
            int topstartlobe = miny(0, s.pts.Length / 2, s.pts);
            if ((topstartlobe - s.bbox.Top + 0.0) / s.bbox.Height > 0.3 && s.pts[0].X > s.last.X &&
                miny(s.pts.Length / 2, s.pts.Length, s.pts) == s.bbox.Top && (s.bbox.Bottom - topstartlobe + 0.0) / s.bbox.Width > 1)
                return false;
            double opening;
            if (Math.Abs(s.pts[0].Y - s.last.Y + 0.0) / s.bbox.Height > 0.3 && Math.Abs(s.pts[0].X - s.last.X + 0.0) / s.bbox.Width < 0.2 &&
                (s.bbox.Right - s.pts[0].X + 0.0) / s.bbox.Width < 0.3)
                ;// return false;
            int maxxind = -1;
            for (int i = 0; i < s.pts.Length; i++)
                if (Math.Abs(s.angles[i] - Math.PI / 2) < Math.PI / 6 || Math.Abs(s.angles[i] + Math.PI / 2) < Math.PI / 6)
                    if (maxxind == -1 || s.pts[i].X > s.pts[maxxind].X)
                        maxxind = i;
            if (maxxind == -1)
                return false;
            if (s.bbox.Width / (float)s.bbox.Height > 3.5 || s.bbox.Height / (float)s.bbox.Width > 3.9)
                return false;
            Rectangle bounds = s.bbox;
            bounds.Size = new Size(s.pts[maxxind].X - s.bbox.Left, bounds.Height);
            bool overlap = curvSign < 0 ? (s.pts[0].X - s.last.X + 0.0) / s.bbox.Width > 0.1 : (s.last.X - s.pts[0].X + 0.0) / s.bbox.Width > 0.1;
            if ((miny(s.pts.Length / 4, s.pts.Length, s.pts) - s.pts[0].Y + 0.0) / s.bbox.Height > .4)
                return false;
            for (int i = s.pts.Length / 4; i < s.pts.Length; i++)
            {
                if (curvSign < 0)
                {
                    if ((Math.Abs(s.angles[i]) < Math.PI / 6 && (s.pts[i].Y - bounds.Top + 0.0) / bounds.Height > 0.5) ||
                        (Math.Abs(s.angles[i] - Math.PI / 2) < Math.PI / 6 && (s.pts[i].X - bounds.Left + 0.0) / bounds.Width < 0.5) ||
                        (Math.Abs(Math.PI / 2 + s.angles[i]) < Math.PI / 6 && (s.pts[i].X - bounds.Left + 0.0) / bounds.Width > 0.5) ||
                        (Math.Abs(Math.Abs(s.angles[i]) - Math.PI) < Math.PI / 6 && (s.pts[i].Y - bounds.Top + 0.0) / bounds.Height < 0.5))
                    {
                        if (V2D.Straightness(s.pts, i, s.pts.Length) < 0.2 && angle(s.last, s.pts[i], new Point(1, -1)) < 60 &&
                            (s.pts[i].Y - bounds.Top + 0.0) / bounds.Height < 0.4)
                            overlap = true;
                        else if (V2D.Dist(s.pts[i], s.last) / Math.Max(s.bbox.Width, s.bbox.Height) > 0.2)
                            return false;
                        break;
                    }
                }
                else
                {
                    if (Math.Abs(s.angles[i] - Math.PI / 2) < Math.PI / 6 && (s.pts[i].X - bounds.Left + 0.0) / bounds.Width > 0.5)
                        return false;
                    if (Math.Abs(Math.PI / 2 + s.angles[i]) < Math.PI / 6 && (s.pts[i].X - bounds.Left + 0.0) / bounds.Width < 0.5)
                        return false;
                    if (Math.Abs(s.angles[i]) < Math.PI / 6 && (s.pts[i].Y - bounds.Top + 0.0) / bounds.Height < 0.5)
                        return false;
                    if (Math.Abs(Math.Abs(s.angles[i]) - Math.PI) < Math.PI / 6 && (s.pts[i].Y - bounds.Top + 0.0) / bounds.Height > 0.5)
                        return false;
                }
            }
            if (!closedLoop(s, out opening))
            {
                if ((s.last.Y - s.bbox.Top + 0.0) / s.bbox.Height > 0.3 && s.angles[s.angles.Length - 1] > 1.5)
                    return false;
                double closeAng = angle(s.s.GetPoint(0), s.pts[2], V2D.Sub(s.pts[s.pts.Length - 3], s.last));
                double openRatio = opening / s.bbox.Width;
                if (openRatio > 0.45 && !(closeAng < 100 && openRatio < 0.6 && (s.last.Y - s.bbox.Top + 0.0) / s.bbox.Height < 0.35))
                    return false;
            }
            if (s.cusps.Length == 2)
            {
                allograph = "0";
                return true;
            }
            if (!s.cusps[0].bot && !s.cusps[s.l].bot &&
                (V2D.Straightness(s.pts, s.cusps[0].index, s.cusps[1].index) > 0.1 ||
                V2D.Straightness(s.pts, s.cusps[1].index, s.cusps[2].index) > 0.1))
            {
                int rightlobeind, rightlobe = maxx(0, s.pts.Length / 2, s.pts, out rightlobeind);
                int topyind, topy = miny(rightlobeind, s.pts.Length, s.pts, out topyind);
                int topstartyind, topstarty = miny(0, rightlobeind, s.pts, out topstartyind);
                if (topstartyind == -1) topstarty = s.pts[topstartyind = 0].Y;
                if ((topstarty - topy + 0.0) / s.bbox.Height > 0.55)
                    return false; // failed attempt at rejecting partials
                allograph = "sigma";
                int right = 0, left = s.pts.Length - 1;
                if (s.pts[right].X < s.pts[left].X)
                {
                    int tmp = right;
                    right = left;
                    left = tmp;
                }
                double finalAng = angle(s.pts[right], s.pts[left], new Point(1, -1));
                if (finalAng > 75 || !overlap)
                    allograph = "0";
                if (s.intersects.Length > 0)
                {
                    rightlobe = Math.Max(s.pts[s.intersects[s.intersects.Length - 1]].X, rightlobe);
                    double matchDist = double.MaxValue;
                    int match = -1;
                    for (int i = s.intersects.Length - 2; i >= 0; i--)
                        if (match == -1 || V2D.Dist(s.pts[s.intersects.Length - 1], s.pts[s.intersects[i]]) < matchDist)
                        {
                            matchDist = V2D.Dist(s.pts[s.intersects.Length - 1], s.pts[s.intersects[i]]);
                            match = i;
                        }
                    if (match == -1)
                        return false;
                    int miy = miny(s.intersects[match], s.intersects[s.intersects.Length - 1], s.pts);
                    int may = maxy(s.intersects[match], s.intersects[s.intersects.Length - 1], s.pts);
                    if ((may - miy + 0.0) / s.bbox.Height > 0.15 && (s.intersects[s.intersects.Length - 1] - s.intersects[match]) / (float)s.pts.Length < 0.3)
                        allograph = "o";
                }
                if ((s.last.X - rightlobe + 0.0) / (rightlobe - s.bbox.Left) > 0.15)
                    ;
                else allograph = "0";
                return true;
            }
            return false;
        }
        bool match_theta(CuspSet s)
        {
            for (int c = 1; c < s.cusps.Length - 1; c++)
                if (Math.Sign(s.avgCurve(c - 1, c)) != Math.Sign(s.cusps[c].curvature))
                    return false;
            int topind, leftind, botind, rightind, top2ind, left2ind;
            int top1 = miny(0, s.pts.Length / 2, s.pts, out topind);
            minx(0, s.pts.Length / 2, s.pts, out leftind);
            if (leftind <= topind)
                return false;
            maxy(0, s.pts.Length, s.pts, out botind);
            if (botind <= leftind)
                return false;
            int top2 = miny(leftind, s.pts.Length, s.pts, out top2ind);
            maxx(leftind, top2ind, s.pts, out rightind);
            if (rightind <= botind)
                return false;
            if (top2ind <= rightind)
                return false;
            if (Math.Abs(top2 - top1 + 0.0) / s.bbox.Height > 0.3)
                return false;
            minx(rightind, s.pts.Length, s.pts, out left2ind);
            if (left2ind <= top2ind || (s.dist - s.distances[left2ind]) / s.bbox.Width < 0.3)
                return false;//
            double barHeightRatio = (V2D.Mul(V2D.Add(s.pts[left2ind], s.last), 0.5f).Y - s.bbox.Top + 0.0) / s.bbox.Height;
            if (barHeightRatio < 0.29)
                return false;
            if (angle(s.last, s.pts[left2ind], new PointF(1, 0)) > 35)
                return false;
            if (V2D.Straightness(s.pts, left2ind, s.pts.Length) > 0.3)
                return false;
            return true;
        }
        bool match_gt(CuspSet s)
        {
            if (s.cusps.Length > 4)
                return false;
            int midcusp = 1;
            for (int i = 2; i < s.cusps.Length - 1; i++)
                if (s.cusps[i].pt.X > s.cusps[midcusp].pt.X)
                    midcusp = i;
            for (int i = 1; i < midcusp; i++)
                if (s.cusps[i].pt.Y > s.cusps[midcusp].pt.Y)
                    return false;
            for (int i = midcusp + 1; i < s.cusps.Length - 1; i++)
                if (s.cusps[i].pt.Y < s.cusps[midcusp].pt.Y)
                    return false;
            int midind = s.cusps[midcusp].index;
            if (s.cusps[s.l].index - midind < 4 || midind < 4)
                return false;
            double ang = angle(s.pts[midind - 3], s.pts[s.cusps[0].index + 3], new PointF(1, 0));
            double ang2 = angle(s.cusps[s.l].pt, s.pts[midind + 3], new PointF(0, 1));
            if ((ang < 11 ? Math.Max(0, ang - 5) : ang) + ang2 <= 43)
                return false;
            double ststraight = V2D.Straightness(s.pts, 0, midind);
            double enstraight = V2D.Straightness(s.pts, midind, s.pts.Length);
            if (s.distances[midind] / s.dist < 0.25 || s.distances[midind] / s.dist > .75)
                return false;
            if (s.cusps[0].pt.Y < s.cusps[midcusp].pt.Y &&
                s.cusps[midcusp].pt.Y < s.cusps[s.l].pt.Y &&
                s.cusps[midcusp].pt.X > s.cusps[0].pt.X &&
                s.cusps[midcusp].pt.X > s.cusps[s.l].pt.X &&
                Math.Abs(s.cusps[midcusp].curvature) > 0.25 &&
                ((ststraight < .125 && enstraight < 0.125) ||
                (ststraight < 0.09 && enstraight < 0.2) || (ststraight < .2 && enstraight < 0.09)))
                return true;
            return false;
        }
        public bool match_1(CuspSet s, ref string allograph)
        {
            if ((0.0 + s.bbox.Height) / s.bbox.Width > 3 || s.straight < 0.06)
            {
                int midpt = 0;
                for (int i = 0; i < s.pts.Length; i++)
                    if ((s.pts[i].Y - s.bbox.Top + 0.0) / s.bbox.Height > 0.5)
                    {
                        midpt = i;
                        break;
                    }
                int botind; maxy(0, s.pts.Length, s.pts, out botind);
                int topind; miny(0, s.pts.Length, s.pts, out topind);
                double str = V2D.Straightness(s.pts, s.pts[0], V2D.Sub(s.pts[botind], s.pts[0]), 0, s.pts.Length, V2D.Dist(s.pts[botind], s.pts[0]));
                double botstr = V2D.Straightness(s.pts, s.pts[midpt], V2D.Sub(s.pts[botind], s.pts[midpt]), midpt, s.pts.Length, V2D.Dist(s.pts[botind], s.pts[midpt]));
                if (str < .12 && V2D.Straightness(s.pts, 0, midpt) < .12 && botstr < .12)
                {
                    allograph = "1";
                    return true;
                }
                double lstr = V2D.Straightness(s.pts, s.pts[topind], V2D.Sub(s.pts[botind], s.pts[topind]), 0, s.pts.Length, V2D.Dist(s.pts[botind], s.pts[topind]));
                double tstr = V2D.Straightness(s.pts, botind, s.pts.Length);
                if (lstr < 0.12 && V2D.Dist(s.pts[topind], s.pts[botind]) / s.dist > 0.85)
                {
                    allograph = "1";
                    return true;
                }
                if (lstr < 0.135 && tstr < 0.12 && (s.pts[botind].Y - s.last.Y + 0.0) / s.bbox.Height < 0.3 && angle(s.pts[0], s.pts[botind], V2D.Sub(s.last, s.pts[botind])) < 45)
                {
                    allograph = "1";
                    return true;
                }
            }
            if (s.intersects.Length > 1 && (s.distances[s.intersects[s.intersects.Length / 2]] - s.distances[s.intersects[s.intersects.Length / 2 - 1]]) / s.dist > 0.2)
                return false;
            PointF lead = V2D.Sub(s.cusps[1].pt, s.cusps[0].pt);
            double ang = angle(V2D.Normalize(lead), new Point(1, -1));
            double anglead = angle(V2D.Normalize(lead), new PointF(0, -1));
            double angstem = angle(s.cusps[1].pt, s.last, new PointF(0, -1));
            if (angstem > anglead)
                return false;
            if (ang > 25 && angle(s.cusps[0].pt, s.cusps[1].pt, V2D.Sub(s.last, s.cusps[1].pt)) > 15)
                return false;
            // this next if makes no sense (compares a relative distance to an absolute position)
            //if (lead.Y > s.last.Y)
            //    return false;
            double angtail = angle(s.cusps[s.l].pt, s.cusps[1].pt, new PointF(0, 1));
            if (angtail > 25)
                return false;
            if (V2D.Straightness(s.pts, 0, s.cusps[1].index) > 0.14 ||
                V2D.Straightness(s.pts, s.cusps[1].index, s.pts.Length) > 0.14)
                return false;
            if (s.cusps[0].pt.Y - s.cusps[1].pt.Y < 0.5 * (s.cusps[s.l].pt.Y - s.cusps[1].pt.Y)) allograph = "17";
            else if (angstem < 0.5 * anglead) allograph = "1^";
            else allograph = "^1";
            return true;
        }
        public bool match_7(CuspSet s, ref string allograph)
        {
            int startind = 0;
            if (s.cusps.Length >= 4)
            {
                int leadInd = 1;
                if (s.cusps.Length == 5)
                {
                    if (angle(s.cusps[0].pt, s.cusps[1].pt, V2D.Sub(s.cusps[2].pt, s.cusps[1].pt)) < 50 &&
                        s.cusps[2].pt.Y < s.cusps[1].pt.Y)
                        leadInd = 2;
                }
                double angLead = angle(s.cusps[leadInd - 1].pt, s.cusps[leadInd].pt, new PointF(0, 1));
                double angLead2 = angle(s.cusps[1].pt, s.cusps[0].pt, new PointF(0, 1));
                if (angLead < 35 || angLead2 < 35)
                    startind = leadInd;
                else return false;
            }
            if (s.cusps.Length - startind != 3)
                return false;
            int midind = s.cusps[startind + 1].index;
            if (s.cusps[s.l].index - midind < 4 || midind < 4 || midind - 3 == s.cusps[startind].index)
                return false;
            if (s.distances[midind] / s.dist < 0.1)
                return false;
            double ang = angle(s.pts[midind - 3], s.pts[s.cusps[startind].index + 3], new PointF(1, 0));
            double ang2 = angle(s.last, s.pts[midind + 3], new PointF(0, 1));
            if ((ang < 11 ? Math.Max(0, ang - 5) : ang) + ang2 > 43)
                return false;
            if (s.cusps[startind + 1].dist / (s.cusps[s.l].dist - s.cusps[startind + 1].dist) > 1.4)
                return false;
            bool left;
            if (s.cusps[startind + 1].pt.Y < s.cusps[s.l].pt.Y &&
                s.cusps[startind + 1].pt.X > s.cusps[startind].pt.X &&
                V2D.Straightness(s.pts, s.cusps[startind].index, s.cusps[startind + 1].index) < 0.2 &&
                Math.Abs(s.avgCurve(startind + 1, s.l)) < 0.1 &&
                (V2D.Straightness(s.pts, s.cusps[startind + 1].index, s.pts.Length, out left) < 0.13 || left))
            {

                double angleVert = angle(s.last, s.cusps[startind + 1].pt, new PointF(1, 0));
                if (angleVert > 70 && angleVert < 110 && s.bbox.Height < s.bbox.Width)
                    allograph = "not7";
                else { allograph = "7"; }
                return true;
            }
            return false;
        }
        public bool match_L(CuspSet s)
        {
            if (s.cusps.Length > 4 || s.cusps[1].index > s.pts.Length - 2)
                return false;
            double ang = Math.Min(angle(s.last, s.cusps[1].pt, new Point(2, -1)),
                          angle(s.last, s.cusps[1].pt, new Point(1, 0)));
            double ang2 = angle(s.cusps[1].pt, s.cusps[0].pt, new PointF(0, 1));
            double ang3 = angle(s.cusps[1].pt, s.pts[s.cusps[1].index - Math.Max(2, s.cusps[1].index / 3)],
                          V2D.Sub(s.cusps[1].pt, s.pts[s.cusps[1].index + Math.Max(2, (s.pts.Length - s.cusps[1].index) / 3)]));
            double ang4 = angle(s.cusps[1].pt, s.pts[0], V2D.Sub(s.cusps[1].pt, s.last));
            if (ang2 + ang > 35 || (ang > 11 && (s.cusps[s.l].dist - s.cusps[1].dist) / s.cusps[1].dist > .8) || (ang3 > 120 - (ang - 10) && ang4 > 110 - (ang - 10)))
                return false;
            if ((s.dist - s.cusps[1].dist) / s.bbox.Height < 0.2)
                return false;
            if ((s.cusps[s.l].dist - s.cusps[1].dist) / s.cusps[1].dist > 1.4)
                return false;
            if (s.cusps[1].pt.Y > s.cusps[0].pt.Y &&
                s.cusps[1].pt.X < s.last.X &&
                s.cusps[1].curvature < -.25 &&
                V2D.Straightness(s.pts, 0, s.cusps[1].index) +
                V2D.Straightness(s.pts, s.cusps[1].index, s.pts.Length) < ((ang + ang2) < 15 ? 0.3 : 0.2))
                return true;
            return false;
        }
        bool match_backwardsL(CuspSet s)
        {
            if (s.cusps.Length > 4)
                return false;
            double ang = angle(s.last, s.cusps[1].pt, new PointF(-1, 0));
            double ang2 = angle(s.cusps[1].pt, s.cusps[0].pt, new PointF(0, 1));
            if (ang2 > 20 || ang > 35 || ang2 + ang > 35 || (ang > 11 && (s.cusps[s.l].dist - s.cusps[1].dist) / s.cusps[1].dist > .9))
                return false;
            if ((s.cusps[s.l].dist - s.cusps[1].dist) / s.cusps[1].dist > 1.4)
                return false;
            if ((s.cusps[s.l].dist - s.cusps[1].dist) / s.cusps[1].dist < .25)
                return false;
            if (s.cusps[1].pt.Y > s.cusps[0].pt.Y &&
                s.cusps[1].pt.X > s.last.X &&
                s.cusps[1].curvature > .15 &&
                V2D.Straightness(s.pts, 0, s.cusps[1].index) < 0.2 &&
                V2D.Straightness(s.pts, s.cusps[1].index, s.pts.Length) < 0.17)
                return true;
            return false;
        }
        public bool match_caret(CuspSet s)
        {
            int minind, startind = 0, my = miny(0, s.pts.Length, s.pts, out minind);
            if (s.cusps.Length > 4)
                return false;
            if (s.intersects.Length > 0)
            {
                for (int i = 0; i < s.intersects.Length - 1; i++)
                    if (s.intersects[i] < minind && s.intersects[i + 1] > minind)
                        if ((s.distances[s.intersects[i + 1]] - s.distances[s.intersects[i]]) / s.bbox.Height > 0.05)
                            return false;
            }
            int botind;
            maxy(0, s.pts.Length / 2, s.pts, out botind);
            if (botind > s.pts.Length * .05 && s.distances[botind] / s.bbox.Height > 0.1)
            {
                startind = botind;
                miny(botind, s.pts.Length, s.pts, out minind);
                if (V2D.Straightness(s.pts, 0, startind) > 0.15)
                    return false;
                if (angle(s.pts[0], s.pts[botind], V2D.Sub(s.pts[minind], s.pts[botind])) > 10)
                    return false;
            }
            if (V2D.Straightness(s.pts, startind, minind) > 0.15 ||
                V2D.Straightness(s.pts, minind, s.pts.Length) > 0.2)
                return false;
            foreach (CuspRec r in s.cusps)
                if (r.index < s.pts.Length - 1 && r.index > minind + 2 && r.curvature > 0)
                    return false;
            double topang = angle(s.pts[(int)(minind * .7)], s.pts[minind],
                                  V2D.Sub(s.pts[(int)(minind + (s.pts.Length - minind) * .3)], s.pts[minind]));
            double botang = angle(s.pts[(int)(minind * .3)], s.pts[minind],
                                  V2D.Sub(s.pts[(int)(minind + (s.pts.Length - minind) * .7)], s.pts[minind]));
            if (botang < topang * .6 || topang > 90 || topang > Math.Max(55, botang * (topang > 80 ? 1.5 : 1.75)))
                return false;
            if (s.pts[startind].X < s.pts[minind].X &&
                s.last.X > s.pts[startind].X &&
                s.pts[minind].Y < s.pts[startind].Y &&
                s.pts[minind].Y < s.last.Y &&
                !s.cusps[s.l].top && (s.bbox.Bottom - s.pts[startind].Y + 0.0) / s.bbox.Height < 0.4 &&
                (s.bbox.Bottom - s.pts[minind].Y + 0.0) / s.bbox.Width > 0.25 &&
                Math.Abs(s.curvatures[minind]) > 0.2)
                return true;
            return false;
        }

        bool match_lt(CuspSet s)
        {
            if (s.cusps.Length < 3 || s.intersects.Length > 1)
                return false;
            double ang = angle(s.last, s.cusps[1].pt, V2D.Sub(s.cusps[0].pt, s.cusps[1].pt));
            if (s.cusps[1].curvature > -0.5)
            {
                if (ang > 85)
                    return false;
            }
            int leftind, left = minx(0, s.pts.Length, s.pts, out leftind);
            int rightind, right = maxx(leftind, s.pts.Length, s.pts, out rightind);
            double str1 = V2D.Straightness(s.pts, 0, leftind);
            double str2 = V2D.Straightness(s.pts, leftind, rightind);
            if ((s.dist - s.distances[rightind]) / s.dist > 0.1)
                return false;
            if (angle(s.pts[0], s.pts[leftind], V2D.Sub(s.last, s.pts[leftind])) < 20)
                return false;
            if ((s.cusps[0].pt.Y < s.pts[leftind].Y && s.pts[leftind].Y < s.cusps[2].pt.Y) &&
                !s.cusps[2].top &&
                s.pts[leftind].X < s.cusps[0].pt.X &&
                s.pts[leftind].X < s.cusps[2].pt.X &&
                ((str1 < 0.125 && str2 < 0.125) || (str1 < 0.06 && str2 < 0.15)))
                return true;
            return false;
        }

        bool match2_cdots(CuspSet s, CuspSet s2, string letter, string other)
        {
            if (letter != "." && letter != ","/* && letter != "/" && letter != "\\" */)
                return false;
            if (other != "." && other != ","/* && other != "/" && other != "\\" */)
                return false;
            if (V2D.Dist(s.pts[0], s2.pts[0]) > 500) return false;
            double ang = theAngle(s, s2);
            if (ang > 67.5 && ang <= 112.5)
                return true;
            return false;
        }

        bool match2_vdots(CuspSet s, CuspSet s2, string letter, string other)
        {
            if (letter != "." && letter != ","/* && letter != "/" && letter != "\\"*/ )
                return false;
            if (other != "." && other != ","/*&& other != "/" && other != "\\" */)
                return false;
            if (V2D.Dist(s.pts[0], s2.pts[0]) > 500) return false;
            double ang = theAngle(s, s2);
            if (ang <= 22.5 || ang > 157.5)
                return true;
            return false;
        }
        bool match2_ddots_up_right(CuspSet s, CuspSet s2, string letter, string other)
        {
            if (letter != "." && letter != ","/* && letter != "/" && letter != "\\" */)
                return false;
            if (other != "." && other != ","/* && other != "/" && other != "\\"*/)
                return false;
            if (V2D.Dist(s.pts[0], s2.pts[0]) > 500) return false;
            double ang = theAngle(s, s2);
            if (ang > 22.5 && ang <= 67.5)
                return true;
            return false;
        }
        bool match2_ddots_down_right(CuspSet s, CuspSet s2, string letter, string other)
        {
            if (letter != "." && letter != ","/* && letter != "/" && letter != "\\" */)
                return false;
            if (other != "." && other != ","/* && other != "/" && other != "\\" */)
                return false;
            if (V2D.Dist(s.pts[0], s2.pts[0]) > 500) return false;
            double ang = theAngle(s, s2);
            if (ang > 112.5 && ang <= 157.5)
                return true;
            return false;
        }

        bool match3_cdots(CuspSet s, CuspSet s2, string letter, string other)
        {
            if (letter != "." && letter != ","/* && letter != "/" && letter != "\\" */)
                return false;
            if (other != ":" && other != ".." && other != "⋰" && other != "⋱"/* && other != "i" && other != "j"*/)
                return false;
            double ang = theAngle(s, s2);
            if (ang > 67.5 && ang <= 112.5)
                return true;
            return false;
        }
        bool match3_vdots(CuspSet s, CuspSet s2, string letter, string other)
        {
            if (letter != "." && letter != ","/* && letter != "/" && letter != "\\" */
                                                                                      )
                return false;
            if (other != ":" && other != ".." && other != "⋰" && other != "⋱"/* && other != "i" && other != "j"*/)
                return false;
            double ang = theAngle(s, s2);
            if (ang <= 22.5 || ang > 157.5)
                return true;
            return false;
        }
        bool match3_ddots_up_right(CuspSet s, CuspSet s2, string letter, string other)
        {
            if (letter != "." && letter != ","/* && letter != "/" && letter != "\\" */
                                                                                      )
                return false;
            if (other != ":" && other != ".." && other != "⋰" && other != "⋱"/* && other != "i" && other != "j"*/)
                return false;
            double ang = theAngle(s, s2);
            if (ang > 22.5 && ang <= 67.5)
                return true;
            return false;
        }
        bool match3_ddots_down_right(CuspSet s, CuspSet s2, string letter, string other)
        {
            if (letter != "." && letter != ","/* && letter != "/" && letter != "\\"*/)
                return false;
            if (other != ":" && other != ".." && other != "⋰" && other != "⋱"/* && other != "i" && other != "j"*/)
                return false;
            double ang = theAngle(s, s2);
            if (ang > 112.5 && ang <= 157.5)
                return true;
            return false;
        }

        private static double theAngle(CuspSet s, CuspSet s2)
        {
            Point ctr1 = new Point((s.bbox.Left + s.bbox.Right) / 2, (s.bbox.Top + s.bbox.Bottom) / 2);
            Point ctr2 = new Point((s2.bbox.Left + s2.bbox.Right) / 2, (s2.bbox.Top + s2.bbox.Bottom) / 2);
            Point vec;
            if (ctr1.X < ctr2.X)
                vec = V2D.Sub(ctr1, ctr2);
            else
                vec = V2D.Sub(ctr2, ctr1);
            double ang = angle(new Point(0, 1), vec);
            //            Console.WriteLine("angle: " + ang);
            return ang;
        }
        public static Point getPt(float i, Point[] pts)
        {
            if (i != (float)(int)i)
            {
                Point p1 = pts[(int)i];
                Point p2 = pts[(int)i + 1];
                return (Point)(V2D.Add(p1, V2D.Mul(V2D.Sub(p2, p1), (i - (int)i))));
            }
            return pts[(int)i];
        }
        private double[] LocalCurvatures(Point[] inkPts, Point[] scrPts, out double[] distances, out double[] angles)
        {
            angles = new double[inkPts.Length];
            double[] curvatures = new double[inkPts.Length];
            double[] curvatures2 = new double[inkPts.Length];
            double[] curvatures3 = new double[inkPts.Length];
            for (int i = 0; i < angles.Length - 1; i++)
            {
                angles[i] = DirectionChange(new Point(inkPts[i].X + 1, inkPts[i].Y), inkPts[i], inkPts[i + 1]);
            }
            angles[angles.Length - 1] = angles[angles.Length - 2];
            distances = new double[inkPts.Length]; distances[0] = 0;
            for (int i = 1; i < curvatures.Length - 1; i++)
            {
                curvatures[i] = DirectionChange(inkPts[i - 1], inkPts[i], inkPts[i + 1]);
                if (i > 1 &&
                    Math.Sign(curvatures[i - 1]) != Math.Sign(curvatures[i]) && Math.Sign(curvatures[i - 2]) != Math.Sign(curvatures[i - 1]) &&
                    Math.Abs(curvatures[i - 1]) < Math.Abs(curvatures[i - 2]) + Math.Abs(curvatures[i]))
                    curvatures[i - 1] = -curvatures[i - 1];
                if (i > 1 && i < curvatures.Length - 2)
                    curvatures2[i] = DirectionChange(inkPts[i - 2], inkPts[i], inkPts[i + 2]) / 2;
                else curvatures2[i] = 0;
                if (i > 2 && i < curvatures.Length - 3)
                    curvatures3[i] = DirectionChange(inkPts[i - 3], inkPts[i], inkPts[i + 3]) / 3;
                else curvatures3[i] = 0;
                distances[i] = distances[i - 1] + V2D.Dist(inkPts[i - 1], inkPts[i]);
            }
            if (inkPts.Length < 4)
                return curvatures;
            curvatures3[2] = curvatures3[3] * 2 / 3;
            curvatures3[1] = curvatures3[3] / 3;
            curvatures2[1] = curvatures2[2] / 2;
            int firstSign = -1;
            double[] smoothed = new double[curvatures.Length];
            for (int i = 2; i < smoothed.Length - 1; i++)
            {
                if (firstSign == -1 && Math.Sign(curvatures[i]) != 0 || i == 2)
                    firstSign = i;
                double d = 150 / (distances[i] - distances[i - 1]);
                smoothed[i] = Math.Abs(curvatures[i]) > .5 ? curvatures[i] : (curvatures[i] + curvatures2[i] + curvatures3[i]) / 3;
            }
            for (int i = 0; i < firstSign; i++)
                smoothed[i] = smoothed[firstSign];
            smoothed[smoothed.Length - 1] = smoothed[smoothed.Length - 2];
            distances[distances.Length - 1] = distances[distances.Length - 2] + V2D.Dist(inkPts[inkPts.Length - 1], inkPts[inkPts.Length - 2]);
            if (SmoothInput)
            {
                double[] realSmooth = new double[curvatures.Length];
                for (int i = 1; i < realSmooth.Length - 1; i++)
                    realSmooth[i] = (smoothed[i - 1] + smoothed[i] + smoothed[i + 1]) / 3;
                realSmooth[0] = smoothed[0];
                realSmooth[realSmooth.Length - 1] = smoothed[smoothed.Length - 1];
                return realSmooth;
            }
            return smoothed;
        }
        private static double DirectionChange(Point a, Point b, Point c)
        {
            // Use points as vectors to calculate the angle change.
            Point v1 = new Point(b.X - a.X, b.Y - a.Y);
            Point v2 = new Point(c.X - b.X, c.Y - b.Y);
            if (v1.X == 0 && v1.Y == 0)
                return 0;
            if (v2.X == 0 && v2.Y == 0)
                return 0;
            return V2D.Angle(v1, v2);
        }
    }
}