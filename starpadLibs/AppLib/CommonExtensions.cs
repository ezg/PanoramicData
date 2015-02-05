using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.Inq.MSInkCompat;
using PixelLab.Common;

namespace starPadSDK.AppLib
{
    public static class CommonExtensions
    {
        public static Vec GetVec(this GeoAPI.Geometries.ICoordinate c)
        {
            return new Vec(c.X, c.Y);
        }

        public static Point GetWindowsPoint(this GeoAPI.Geometries.ICoordinate c)
        {
            return new Point(c.X, c.Y);
        }

        public static Point GetWindowsPoint(this GeoAPI.Geometries.IPoint c)
        {
            return new Point(c.X, c.Y);
        }
        
        public static Point GetWindowsPoint(this Vec c)
        {
            return new Point(c.X, c.Y);
        }

        public static Vec GetVec(this Point c)
        {
            return new Vec(c.X, c.Y);
        }

        public static Vec GetVec(this GeoAPI.Geometries.IPoint c)
        {
            return new Vec(c.X, c.Y);
        }

        public static Vec GetVec(this Pt c)
        {
            return new Vec(c.X, c.Y);
        }

        public static GeoAPI.Geometries.IPoint GetPoint(this Pt c)
        {
            return new NetTopologySuite.Geometries.Point(c.X, c.Y);
        }    

        public static GeoAPI.Geometries.IPoint GetPoint(this Point c)
        {
            return new NetTopologySuite.Geometries.Point(c.X, c.Y);
        }

        public static GeoAPI.Geometries.IPoint GetPoint(this GeoAPI.Geometries.ICoordinate c)
        {
            return new NetTopologySuite.Geometries.Point(c.X, c.Y);
        }

        public static Pt GetPt(this GeoAPI.Geometries.ICoordinate c)
        {
            return new Pt(c.X, c.Y);
        }

        public static GeoAPI.Geometries.ICoordinate GetCoord(this Vec c)
        {
            return new NetTopologySuite.Geometries.Coordinate(c.X, c.Y);
        }

        public static GeoAPI.Geometries.ILineString GetLineString(this Stroq s)
        {
            GeoAPI.Geometries.ICoordinate[] coords;
            if (s.Count > 1)
            {
                coords = new NetTopologySuite.Geometries.Coordinate[s.Count];
                int i = 0;
                foreach (System.Windows.Point pt in s)
                {
                    coords[i] = new NetTopologySuite.Geometries.Coordinate(pt.X, pt.Y);
                    i++;
                }
            }
            else
            {
                coords = new NetTopologySuite.Geometries.Coordinate[2];
                coords[0] = new NetTopologySuite.Geometries.Coordinate(s[0].X, s[0].Y);
                coords[1] = new NetTopologySuite.Geometries.Coordinate(s[0].X, s[0].Y + 1);
            }
            return new NetTopologySuite.Geometries.LineString(coords);
        }

        public static GeoAPI.Geometries.ILineString GetLineString(this IEnumerable<Point> s)
        {
            GeoAPI.Geometries.ICoordinate[] coords;
            coords = new NetTopologySuite.Geometries.Coordinate[s.Count()];
            int i = 0;
            foreach (System.Windows.Point pt in s)
            {
                coords[i] = new NetTopologySuite.Geometries.Coordinate(pt.X, pt.Y);
                i++;
            }

            return new NetTopologySuite.Geometries.LineString(coords);
        }

        public static IEnumerable<Pt> GetPoints(this Rct r)
        {
            List<Pt> pts = new List<Pt>();

            pts.Add(r.TopLeft);
            pts.Add(r.TopRight);
            pts.Add(r.BottomRight);
            pts.Add(r.BottomLeft);
            pts.Add(r.TopLeft);

            return pts;
        }

        public static GeoAPI.Geometries.ILineString GetLineString(this Rct r)
        {
            GeoAPI.Geometries.ICoordinate[] coords;
            coords = new NetTopologySuite.Geometries.Coordinate[5];
            coords[0] = new NetTopologySuite.Geometries.Coordinate(r.TopLeft.X, r.TopLeft.Y);
            coords[1] = new NetTopologySuite.Geometries.Coordinate(r.TopRight.X, r.TopRight.Y);
            coords[2] = new NetTopologySuite.Geometries.Coordinate(r.BottomRight.X, r.BottomRight.Y);
            coords[3] = new NetTopologySuite.Geometries.Coordinate(r.BottomLeft.X, r.BottomLeft.Y);
            coords[4] = new NetTopologySuite.Geometries.Coordinate(r.TopLeft.X, r.TopLeft.Y);
            return new NetTopologySuite.Geometries.LineString(coords);
        }

        public static GeoAPI.Geometries.IPolygon GetPolygon(this IEnumerable<Pt> s)
        {
            GeoAPI.Geometries.ICoordinate[] coords;
            coords = new NetTopologySuite.Geometries.Coordinate[s.Count() + 1];
            int i = 0;
            foreach (System.Windows.Point pt in s)
            {
                coords[i] = new NetTopologySuite.Geometries.Coordinate(pt.X, pt.Y);
                i++;
            }
            coords[i] = new NetTopologySuite.Geometries.Coordinate(s.First().X, s.First().Y);

            return new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(coords));
        }

        public static GeoAPI.Geometries.IPolygon GetPolygon(this IEnumerable<Point> s)
        {
            GeoAPI.Geometries.ICoordinate[] coords;
            if (s.Count() > 3)
            {
                coords = new NetTopologySuite.Geometries.Coordinate[s.Count() + 1];
                int i = 0;
                foreach (System.Windows.Point pt in s)
                {
                    coords[i] = new NetTopologySuite.Geometries.Coordinate(pt.X, pt.Y);
                    i++;
                }
                coords[i] = new NetTopologySuite.Geometries.Coordinate(s.First().X, s.First().Y);
            }
            else
            {
                coords = new NetTopologySuite.Geometries.Coordinate[5];
                coords[0] = new NetTopologySuite.Geometries.Coordinate(s.First().X, s.First().Y);
                coords[1] = new NetTopologySuite.Geometries.Coordinate(s.First().X + 1, s.First().Y);
                coords[2] = new NetTopologySuite.Geometries.Coordinate(s.First().X + 1, s.First().Y + 1);
                coords[3] = new NetTopologySuite.Geometries.Coordinate(s.First().X, s.First().Y + 1);
                coords[4] = new NetTopologySuite.Geometries.Coordinate(s.First().X, s.First().Y);
            }

            return new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(coords));
        }

        public static GeoAPI.Geometries.IPolygon GetPolygon(this Rct r)
        {
            return ((Rect)r).GetPolygon();
        }

        public static GeoAPI.Geometries.IPolygon GetPolygon(this Rect r)
        {
            GeoAPI.Geometries.ICoordinate[] coords;
            coords = new NetTopologySuite.Geometries.Coordinate[5];

            coords[0] = new NetTopologySuite.Geometries.Coordinate(r.TopLeft.X, r.TopLeft.Y);
            coords[1] = new NetTopologySuite.Geometries.Coordinate(r.TopRight.X, r.TopRight.Y);
            coords[2] = new NetTopologySuite.Geometries.Coordinate(r.BottomRight.X, r.BottomRight.Y);
            coords[3] = new NetTopologySuite.Geometries.Coordinate(r.BottomLeft.X, r.BottomLeft.Y);
            coords[4] = new NetTopologySuite.Geometries.Coordinate(r.TopLeft.X, r.TopLeft.Y);

            return new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(coords));
        }

        public static GeoAPI.Geometries.IPolygon GetPolygon(this Stroq s)
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

        public static Rct GetBounds(this GeoAPI.Geometries.IGeometry g)
        {
            return new Rct(new Pt(g.Centroid.X - g.EnvelopeInternal.Width / 2.0, g.Centroid.Y - g.EnvelopeInternal.Height / 2.0), 
                new Vec(g.EnvelopeInternal.Width, g.EnvelopeInternal.Height));
        }

        public static List<FrameworkElement> GetIntersectedElements(this InqScene inqScene, Stroq stroq, params Type[] types)
        {
            Rect bounds = stroq.GetBounds();
            return inqScene.GetIntersectedElements(bounds, types);
        }

        public static List<FrameworkElement> GetIntersectedElements(this InqScene inqScene, Rect bounds, params Type[] types)
        {
            var ret = new List<FrameworkElement>();
            GeoAPI.Geometries.IGeometry inGeom = bounds.GetPolygon();

            foreach (var element in inqScene.Elements)
            {
                if (types.Contains(element.GetType()) && !(double.IsNaN(element.ActualWidth)))
                {
                    GeoAPI.Geometries.IGeometry geom = GetPolygon(new Pt[] 
                        {
                            element.TranslatePoint(new Point(0, 0), inqScene),
                            element.TranslatePoint(new Point(element.ActualWidth, 0), inqScene),
                            element.TranslatePoint(new Point(element.ActualWidth, element.ActualHeight), inqScene),
                            element.TranslatePoint(new Point(0, element.ActualHeight), inqScene)
                        });
                    if ((inGeom.Intersects(geom) || inGeom.Contains(geom)))
                        ret.Add(element);
                }
            }
            return ret;
        }

        public static List<FrameworkElement> GetIntersectedElements(this InqScene inqScene, Stroq stroq)
        {
            Rect bounds = stroq.GetBounds();
            return inqScene.GetIntersectedElements(bounds);
        }

        public static List<FrameworkElement> GetIntersectedElements(this InqScene inqScene, Rect bounds)
        {
            var ret = new List<FrameworkElement>();
            GeoAPI.Geometries.IGeometry inGeom = bounds.GetPolygon();

            foreach (var element in inqScene.Elements)
            {
                if (element is GeometryElement)
                {
                    GeoAPI.Geometries.IGeometry g = ((GeometryElement) element).GetGeometry();
                    if (inGeom.Intersects(g) || inGeom.Contains(g))
                    {
                        ret.Add(element);
                    }
                }
                else
                {
                    if (!(double.IsNaN(element.ActualWidth)))
                    {
                        GeoAPI.Geometries.IGeometry geom = GetPolygon(new Pt[] 
                        {
                            element.TranslatePoint(new Point(0, 0), inqScene),
                            element.TranslatePoint(new Point(element.ActualWidth, 0), inqScene),
                            element.TranslatePoint(new Point(element.ActualWidth, element.ActualHeight), inqScene),
                            element.TranslatePoint(new Point(0, element.ActualHeight), inqScene)
                        });
                        if ((inGeom.Intersects(geom) || inGeom.Contains(geom)))
                            ret.Add(element);
                    }
                }
            }
            return ret;
        }

        public static List<FrameworkElement> GetIntersectedTypesRecursive<T>(this FrameworkElement mainElement, Stroq stroq)
        {
            Rect bounds = stroq.GetBounds();
            return mainElement.GetIntersectedTypesRecursive<T>(bounds);
        }

        public static List<FrameworkElement> GetIntersectedTypesRecursive<T>(this FrameworkElement mainElement, Rct bounds)
        {
            var ret = new List<FrameworkElement>();
            GeoAPI.Geometries.IGeometry inGeom = bounds.GetPolygon();

            List<FrameworkElement> allElements = mainElement.VisualDescendentsOfType<FrameworkElement>().Where(fr => fr is T).ToList();

            foreach (var element in allElements)
            {
                if (element is GeometryElement)
                {
                    GeoAPI.Geometries.IGeometry g = ((GeometryElement)element).GetGeometry();
                    if (inGeom.Intersects(g) || inGeom.Contains(g))
                    {
                        ret.Add(element);
                    }
                }
                else
                {
                    if (!(double.IsNaN(element.ActualWidth)))
                    {
                        GeoAPI.Geometries.IGeometry geom = GetPolygon(new Pt[] 
                        {
                            element.TranslatePoint(new Point(0, 0), mainElement),
                            element.TranslatePoint(new Point(element.ActualWidth, 0), mainElement),
                            element.TranslatePoint(new Point(element.ActualWidth, element.ActualHeight), mainElement),
                            element.TranslatePoint(new Point(0, element.ActualHeight), mainElement)
                        });
                        if ((inGeom.Intersects(geom) || inGeom.Contains(geom)))
                            ret.Add(element);
                    }
                }
            }
            return ret;
        }

        public static bool ContainsByReference<T>(this List<T> list, T item)
        {
            return list.Any(x => object.ReferenceEquals(x, item));
        }

        public static void RemoveByReference<T>(this List<T> list, T item)
        {
            list.RemoveAll(x => object.ReferenceEquals(x, item));
        }

        public static bool RemoveFirstByReference<T>(this List<T> list, T item)
        {
            var index = -1;
            for (int i = 0; i < list.Count; i++)
                if (object.ReferenceEquals(list[i], item))
                {
                    index = i;
                    break;
                }
            if (index == -1)
                return false;

            list.RemoveAt(index);
            return true;
        }

        public static bool HasSameElementsAs(this StroqCollection one, StroqCollection two)
        {
            if (one.Count != two.Count)
            {
                return false;
            }
            StroqCollection twoCopy = new StroqCollection(two.ToArray());
            foreach (var s in one)
            {
                if (twoCopy.Contains(s))
                {
                    twoCopy.Remove(s);
                }
            }
            if (twoCopy.Count > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void SetTag<T>(this Stroq stroq, Guid id, T value) 
        {
            stroq.Property[id] = value;
        }

        public static T GetTag<T>(this Stroq stroq, Guid id)
        {
            if (stroq.Property.Exists(id))
            {
                return (T) stroq.Property[id];
            }
            return default(T);
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> coll)
        {
            var c = new ObservableCollection<T>();
            foreach (var e in coll)
                c.Add(e);
            return c;
        }

        public static void Sort<TSource, TKey>(this Collection<TSource> source, Func<TSource, TKey> keySelector)
        {
            List<TSource> sortedList = source.OrderBy(keySelector).ToList();
            source.Clear();
            foreach (var sortedItem in sortedList)
                source.Add(sortedItem);
        }

        public static string TrimTo(this string input, int length)
        {
            return input.Length > length ? input.Substring(0, length) + "..." : input;
        }

        public static double Square(double x)
        {
            return x * x;
        }

        public static double Distance(Point pt1, Point pt2)
        {
            return Math.Sqrt(Square(pt1.X - pt2.X) + Square(pt1.Y - pt2.Y));
        }

        public static void TranslateBy(FrameworkElement elt, double deltaX, double deltaY)
        {
            Matrix cur = elt.RenderTransform.Value;

            elt.RenderTransform = new MatrixTransform(cur.M11, cur.M12, cur.M21, cur.M22, cur.OffsetX + deltaX, cur.OffsetY + deltaY);
        }

        public static void ScaleBy(FrameworkElement elt, double deltaX, double deltaY)
        {
            Matrix cur = elt.RenderTransform.Value;

            elt.RenderTransform = new MatrixTransform(cur.M11 * deltaX, cur.M12, cur.M21, cur.M22 * deltaY, cur.OffsetX, cur.OffsetY);
        }

        public static Size MeasureString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return new Size(0, 0);
            }
            var TextBlock = new TextBlock()
            {
                Text = s
            };
            TextBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            return new Size(TextBlock.DesiredSize.Width, TextBlock.DesiredSize.Height);
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random(0);
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class TranslationFilter : GeoAPI.Geometries.ICoordinateFilter
    {
        private readonly GeoAPI.Geometries.ICoordinate _trans;

        public TranslationFilter(GeoAPI.Geometries.ICoordinate trans)
        {
            _trans = trans;
        }

        public void Filter(GeoAPI.Geometries.ICoordinate coord)
        {
            coord.X += _trans.X;
            coord.Y += _trans.Y;
        }
    }

    /// <summary>
    /// Adds a method to a stroq:
    ///   IsRectangle(stroke) - is the stroke a rectanlge
    /// </summary>
    static public class RectangleTester
    {
        static Guid IS_RECTANGLE = new Guid("4540316A-9189-41FB-90E2-56CE9F01A487");
        static public bool IsRectangle(this Stroq stroke)
        {
            if (!stroke.Property.Exists(IS_RECTANGLE))
            {
                var fd = new starPadSDK.CharRecognizer.FeaturePointDetector();
                bool rect = fd.match_rect(fd.FeaturePoints(stroke.OldStroke()));
                stroke.Property[IS_RECTANGLE] = rect;
            }
            return (bool)stroke.Property[IS_RECTANGLE];
        }
    }
}
