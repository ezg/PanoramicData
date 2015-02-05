using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PanoramicData.utils
{
    public class Helpers
    {
        public static Color GetColorFromString(string input)
        {
            Color result = Color.FromArgb(0, 0, 0, 0);
            if (input.StartsWith("#"))
            {
                byte red = Convert.ToByte(input.Substring(1, 2), 16);
                byte green = Convert.ToByte(input.Substring(3, 2), 16);
                byte blue = Convert.ToByte(input.Substring(5, 2), 16);
                result = Color.FromArgb(255, red, green, blue);
            }
            return result;
        }

        public static float Distance(Point p1, Point p2)
        {
            var p = new Point(p1.X - p2.X, p1.Y - p2.Y);
            return (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }

        public static List<T> ListFromItems<T>(params T[] items)
        {
            return new List<T>(items);
        }
    }

    public static class Extensions
    {
        public static GeoAPI.Geometries.IPolygon GetPolygon(this List<Point> s)
        {
            GeoAPI.Geometries.ICoordinate[] coords;

            if (s.Count >= 3)
            {
                coords = new NetTopologySuite.Geometries.Coordinate[s.Count + 1];
                int i = 0;
                foreach (System.Windows.Point pt in s)
                {
                    coords[i] = new NetTopologySuite.Geometries.Coordinate(pt.X, pt.Y);
                    i++;
                }
                coords[i] = new NetTopologySuite.Geometries.Coordinate(s[0].X, s[0].Y);
            }
            else
            {
                coords = new NetTopologySuite.Geometries.Coordinate[4];
                coords[0] = new NetTopologySuite.Geometries.Coordinate(s[0].X, s[0].Y);
                coords[1] = new NetTopologySuite.Geometries.Coordinate(s[0].X + 1, s[0].Y);
                coords[2] = new NetTopologySuite.Geometries.Coordinate(s[0].X + 1, s[0].Y + 1);
                coords[3] = new NetTopologySuite.Geometries.Coordinate(s[0].X, s[0].Y);
            }

            return new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(coords));
        }
    }
}
