using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using starPadSDK.Inq;
using starPadSDK.Geom;
using starPadSDK.Inq.BobsCusps;
using System.Windows.Shapes;
using System.Windows.Media;
using starPadSDK.AppLib;
using System.Windows;

namespace InqUtils.DelaunayTriangulator
{
    /// <summary>
    /// Convex Hull, according to the algorithm described by Mark Nelson
    /// http://marknelson.us/2007/08/22/convex/
    /// </summary>
    public class ConvexHull
    {
        List<Pt> S;
        Pt leftmost;
        Pt rightmost;
        List<Pt> up_S;
        List<Pt> low_S;
        List<Pt> up_hull;
        List<Pt> low_hull;
        public List<Pt> chull;

        public ConvexHull(List<Pt> points)
        {
            S = points;
            up_hull = new List<Pt>();
            low_hull = new List<Pt>();
            up_S = new List<Pt>();
            low_S = new List<Pt>();

            Partition();
            BuildHalfHull(1);
            BuildHalfHull(-1);
            chull = up_hull;
            low_hull.Remove(leftmost);
            low_hull.Remove(rightmost);
            while (low_hull.Count != 0)
            {
                chull.Add(new Pt(low_hull[low_hull.Count - 1]));
                low_hull.RemoveAt(low_hull.Count - 1);
            }
        }

        public void Partition()
        {
            SortPoints();
            leftmost = new Pt(S[0]);
            rightmost = new Pt(S[S.Count - 1]);
            S.RemoveAt(0);
            S.RemoveAt(S.Count - 1);

            // Iterate all points to partition them into uppper or low 
            foreach (Pt p in S)
            {
                int val = Direction(leftmost, rightmost, p);
                if (val >= 0)
                    low_S.Add(p);
                else
                    up_S.Add(p);
            }
            
        }

        public void SortPoints()
        { 
            // Insert sort the points according to p.x
            for (int i = 1; i < S.Count; i++)
            {
                int j = i - 1;
                while (j >= 0 && S[i].X < S[j].X)
                {
                    j--;
                }

                if (j < 0)
                {
                    S.Insert(0, S[i]);
                    S.RemoveAt(i + 1);
                }
                else
                {
                    S.Insert(j + 1, S[i]);
                    S.RemoveAt(i + 1);
                }
            }
        }

        /// <summary>
        /// Check p is above the line between left and right or not.
        /// det >= 0 -> low, det < 0 -> upper
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public int Direction(Pt left, Pt right, Pt p)
        {
            double det = (left.X - p.X) * (right.Y - p.Y) - (right.X - p.X) * (left.Y - p.Y);

            return (int)det;
        }

        /// <summary>
        /// Build the upper and low half hull
        /// factor = 1 means low hull, factor = -1 means upper hull
        /// </summary>
        /// <param name="factor"></param>
        public void BuildHalfHull(int factor)
        {
            List<Pt> points;
            List<Pt> hull;
            if (factor == 1)
            {
                points = low_S;
                hull = low_hull;
            }
            else
            {
                points = up_S;
                hull = up_hull;
            }

            points.Add(rightmost);
            hull.Add(leftmost);

            while (points.Count != 0)
            {
                hull.Add(points[0]);
                points.RemoveAt(0);
                while (hull.Count >= 3)
                { 
                    int end = hull.Count - 1;
                    // Convexity check
                    if (factor * Direction(hull[end - 2], hull[end], hull[end - 1]) <= 0)
                        hull.RemoveAt(end - 1);
                    else
                        break;
                }
            }
        }
    }
}
