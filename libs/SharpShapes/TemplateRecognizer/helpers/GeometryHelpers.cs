using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;
using GeoAPI.Geometries;
using BrownRecognitionCommon;

namespace TemplateRecognizer
{
    static class GeometryHelpers
    {
        public static List<BaseShape> ShapesWithinDistance(Geometry geom, double distance, ShapeType[] shapeTypesToSearchFor, Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            List<BaseShape> matches = new List<BaseShape>();

            foreach (ShapeType shapeType in shapeTypesToSearchFor)
            {
                foreach (BaseShape bs in shapeDictionary[shapeType])
                {
                    if (geom != bs.Geometry)
                    {
                        if (geom.IsWithinDistance(bs.Geometry, distance))
                        {
                            matches.Add(bs);
                        }
                    }
                }
            }

            return matches;
        }

        public static List<BaseShape> ShapesThatIntersect(Geometry geom, ShapeType[] shapeTypesToSearchFor, Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            List<BaseShape> matches = new List<BaseShape>();

            foreach (ShapeType shapeType in shapeTypesToSearchFor)
            {
                foreach (BaseShape bs in shapeDictionary[shapeType])
                {
                    if (geom != bs.Geometry)
                    {
                        if (geom.Intersects(bs.Geometry))
                        {
                            matches.Add(bs);
                        }
                    }
                }
            }

            return matches;
        }

        public static List<BaseShape> ShapesThatAreContained(Geometry geom, ShapeType[] shapeTypesToSearchFor, Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            List<BaseShape> matches = new List<BaseShape>();

            foreach (ShapeType shapeType in shapeTypesToSearchFor)
            {
                foreach (BaseShape bs in shapeDictionary[shapeType])
                {
                    if (geom != bs.Geometry)
                    {
                        if (geom.Contains(bs.Geometry))
                        {
                            matches.Add(bs);
                        }
                    }
                }
            }

            return matches;
        }

        public static BaseShape ShapeWithSmallestY(ShapeType[] shapeTypesToSearchFor, Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            BaseShape found = null;
            double smallestY = double.MaxValue;
            foreach (BaseShape bs in GeometryHelpers.ShapesOfTypes(shapeTypesToSearchFor, shapeDictionary))
            {
                IPoint centroid = bs.Geometry.Centroid;
                if (centroid.Y < smallestY)
                {
                    found = bs;
                    smallestY = centroid.Y;
                }
            }
            return found;
        }

        public static Geometry CreateLine(Coordinate from, Coordinate to)
        {
            ICoordinate[] coords = new Coordinate[] { from, to };
            return new LineString(coords);
        }

        public static Geometry CreateRectangle(Coordinate center, double width, double heigth)
        {
            double halfW = width / 2.0;
            double halfH = heigth / 2.0;
            ICoordinate[] coords = new Coordinate[] 
            { 
                new Coordinate(center.X - halfW, center.Y - halfH),
                new Coordinate(center.X + halfW, center.Y - halfH),
                new Coordinate(center.X + halfW, center.Y + halfH),
                new Coordinate(center.X - halfW, center.Y + halfH),
                new Coordinate(center.X - halfW, center.Y - halfH)
            };
            LinearRing ring = new LinearRing(coords);

            return new Polygon(ring);
        }

        public static Geometry CreateCircle(Coordinate center, double radius)
        {
            int precision = 180;
            ICoordinate[] coords = new Coordinate[precision + 1];

            for (int i = 0; i < precision; i++)
            {
                coords[i] = new Coordinate(Math.Sin(i * Math.PI * 2.0 / (double)precision) * radius + center.X,
                                           Math.Cos(i * Math.PI * 2.0 / (double)precision) * radius + center.Y);
            }
            coords[precision] = new Coordinate(Math.Sin(0) * radius + center.X,
                                               Math.Cos(0) * radius + center.Y);

            LinearRing ring = new LinearRing(coords);

            return new Polygon(ring);
        }

        public static bool IsVerticalLine(IGeometry geom, double toleranceInPercentOfLength)
        {
            if (geom is LineString)
            {
                return Math.Abs(geom.Coordinates[0].X - geom.Coordinates[geom.Coordinates.Count() - 1].X) < toleranceInPercentOfLength * geom.Length;
            }
            return false;
        }

        public static bool IsHorizontalLine(IGeometry geom, double toleranceInPercentOfLength)
        {
            if (geom is LineString)
            {
                return Math.Abs(geom.Coordinates[0].Y - geom.Coordinates[geom.Coordinates.Count() - 1].Y) < toleranceInPercentOfLength * geom.Length;
            }
            return false;
        }

        public static void TranslateGeometry(IGeometry geom, double x, double y) 
        {
            geom.Apply(new TranslationFilter(new Coordinate(x, y)));
        }

        public static BrownShape ConvertToBrownShape(IGeometry geom, BrownShape brownShape)
        {
            BrownShape ret;
            if (brownShape != null)
            {
                ret = new BrownShape(brownShape.ShapeType, geom.Coordinates.Select((coord) => new System.Windows.Point(coord.X, coord.Y)).ToArray());
                ret.BrownInputStrokes = brownShape.BrownInputStrokes;
            }
            else
            {
                ret = new BrownShape(ShapeType.StraightLine, geom.Coordinates.Select((coord) => new System.Windows.Point(coord.X, coord.Y)).ToArray());
            }
            return ret;
        }

        private static List<BaseShape> ShapesOfTypes(ShapeType[] shapeTypesToSearchFor, Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            List<BaseShape> matches = new List<BaseShape>();

            foreach (ShapeType shapeType in shapeTypesToSearchFor)
            {
                foreach (BaseShape bs in shapeDictionary[shapeType])
                {
                    matches.Add(bs);
                }
            }

            return matches;
        }
    }

    class TranslationFilter : ICoordinateFilter
    {
        private readonly ICoordinate _trans;

        public TranslationFilter(ICoordinate trans)
        {
            _trans = trans;
        }

        public void Filter(ICoordinate coord)
        {
            coord.X += _trans.X;
            coord.Y += _trans.Y;
        }
    }

}
