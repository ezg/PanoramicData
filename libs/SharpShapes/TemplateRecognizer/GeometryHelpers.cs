using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShapeRecognizer;
using NetTopologySuite.Geometries;
using GeoAPI.Geometries;

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

        public static Geometry CreateLine(Point from, Point to)
        {
            ICoordinate[] coords = new Coordinate[] { (Coordinate) from.Coordinate, (Coordinate) to.Coordinate };
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
}
