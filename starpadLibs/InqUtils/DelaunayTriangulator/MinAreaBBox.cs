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

namespace InqUtils.DelaunayTriangulator
{
    public class MinAreaBBox
    {
        public Pt corner1;
        public Pt corner2;
        public Pt corner3;
        public Pt corner4;
        public Pt center;
        public double A;
        public double steep; // angle respect with the horizontal line.
        public ConvexHull ch;
        public List<Pt> points;
        public double width;
        public double height;
        public double top;
        public double right;
        public double bottom;
        public double left;

        /// <summary>
        /// The input of construct is the points in one stroq.
        /// </summary>
        /// <param name="points"></param>
        public MinAreaBBox(List<Pt> stroq_points)
        {
            ch = new ConvexHull(stroq_points);
            points = ch.chull;

            double minArea = 1000000;
            corner1 = new Pt();
            corner2 = new Pt();
            corner3 = new Pt();
            corner4 = new Pt();
            center = new Pt();

            if (points.Count == 2)
            {
                Pt pp1 = points[0];
                Pt pp2 = points[1];
                top = Math.Min(pp1.Y, pp2.Y);
                right = Math.Max(pp1.X, pp2.X);
                bottom = Math.Max(pp1.Y, pp2.Y);
                left = Math.Min(pp1.X, pp2.X);
                corner1.X = left;
                corner1.Y = top;
                corner2.X = right;
                corner2.Y = top;
                corner3.X = right;
                corner3.Y = bottom;
                corner4.X = left;
                corner4.Y = bottom;
                center.X = (left + right) / 2;
                center.Y = (top + bottom) / 2;
                A = (bottom - top) * (right - left);
                steep = 0;
                height = bottom - top;
                width = right - left;
            }
            else
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Pt p1 = points[i];
                    Pt p2 = i < points.Count - 1 ? points[i + 1] : points[0];

                    // The two parallel line functions
                    double steep1 = (p1.Y - p2.Y) / (p1.X - p2.X + 0.00001);
                    bRange b1 = FindRangeBySteep(steep1, points);

                    // The two parallel line functions, which are perpendicular to the first two
                    double steep2 = -1 / (steep1 + 0.00001);
                    bRange b2 = FindRangeBySteep(steep2, points);

                    //Console.WriteLine(points.Count);
                    //Console.WriteLine("b1 " + b1.bmax + " " + b1.bmin);
                    //Console.WriteLine("b2 " + b2.bmax + " " + b2.bmin + "\n");

                    // Find the area of the rectangle formed by the four lines.
                    Pt isec1, isec2, isec3, isec4;
                    double length1, length2, area;
                    isec1 = new Pt();
                    isec2 = new Pt();
                    isec3 = new Pt();
                    isec4 = new Pt();

                    isec1.X = (b2.bmax - b1.bmax) / (steep1 - steep2);
                    isec1.Y = steep1 * isec1.X + b1.bmax;
                    isec2.X = (b2.bmin - b1.bmax) / (steep1 - steep2);
                    isec2.Y = steep1 * isec2.X + b1.bmax;
                    length1 = Distance(isec1, isec2);

                    isec3.X = (b2.bmin - b1.bmin) / (steep1 - steep2);
                    isec3.Y = steep2 * isec3.X + b2.bmin;
                    length2 = Distance(isec2, isec3);

                    isec4.X = isec1.X + isec3.X - isec2.X;
                    isec4.Y = isec1.Y + isec3.Y - isec2.Y;

                    area = length1 * length2;
                    if (area < minArea)
                    {
                        minArea = area;
                        corner1.X = isec1.X; corner1.Y = isec1.Y;
                        corner2.X = isec2.X; corner2.Y = isec2.Y;
                        corner3.X = isec3.X; corner3.Y = isec3.Y;
                        corner4.X = isec4.X; corner4.Y = isec4.Y;
                        center.X = (isec2.X + isec4.X) / 2;
                        center.Y = (isec2.Y + isec4.Y) / 2;
                        steep = Math.Abs(steep1) < Math.Abs(steep2) ? steep1 : steep2;
                        if (steep == steep1)
                        {
                            width = Distance(corner1, corner2);
                            height = Distance(corner2, corner3);
                        }
                        else
                        {
                            height = Distance(corner1, corner2);
                            width = Distance(corner2, corner3);
                        }
                    }
                }
                A = minArea;
                top = Math.Min(Math.Min(corner1.Y, corner2.Y), Math.Min(corner3.Y, corner4.Y));
                right = Math.Max(Math.Max(corner1.X, corner2.X), Math.Max(corner3.X, corner4.X));
                bottom = Math.Max(Math.Max(corner1.Y, corner2.Y), Math.Max(corner3.Y, corner4.Y));
                left = Math.Min(Math.Min(corner1.X, corner2.X), Math.Min(corner3.X, corner4.X));
            }

            
        }

        public static double Distance(Pt a, Pt b)
        {
            double deltaX = a.X - b.X;
            double deltaY = a.Y - b.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public bRange FindRangeBySteep(double steep, List<Pt> points)
        {
            // y = ax + b -> b = y - ax
            double bmin = 1000000;
            double bmax = -1000000;
            double b;
            for (int j = 0; j < points.Count; j++)
            {
                b = points[j].Y - steep * points[j].X;
                if (b < bmin)
                    bmin = b;
                if (b > bmax)
                    bmax = b;
            }

            bRange val = new bRange(bmin, bmax);
            
            return val;
        }
    }

    public class bRange
    {
        public double bmin;
        public double bmax;

        public bRange(double _bmin, double _bmax)
        {
            bmin = _bmin;
            bmax = _bmax;
        }
    };
}
