using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;
using starPadSDK.Geom;
using NetTopologySuite.Operation.Distance;
using GeoAPI.Geometries;
using starPadSDK.Inq;
using BrownRecognitionCommon;

namespace TemplateRecognizer
{
    public class PyramidDiagramRecognizer
    {
        public static BrownTemplate Recognize(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            long t = DateTime.Now.Ticks;
            BrownRecognitionSettings set = BrownRecognitionSettings.Instance;
            BrownTemplate brownTemplate = new BrownTemplate(TemplateType.PyramidDiagram);
            
            List<BaseShape> triangles = new List<BaseShape>();
            triangles.AddRange(shapeDictionary[ShapeType.Triangle]);
            triangles.AddRange(shapeDictionary[ShapeType.RightTriangle]);
            triangles.AddRange(shapeDictionary[ShapeType.IsoscelesTriangle]);


            foreach (BaseShape triangle in triangles)
            {
                // too small of a rectangle, causes some of the NetTopoplogy buffer operations to crash.
                if (triangle.Geometry.EnvelopeInternal.Area <= 2)
                {
                    continue;
                }

                List<BaseShape> containedShapes = GeometryHelpers.ShapesThatAreContained((Geometry)triangle.Geometry.Envelope.Buffer(set.PyramidDiagramContainsThreshold), new ShapeType[] { ShapeType.StraightLine, ShapeType.Polyline }, shapeDictionary);
                IEnvelope envelope = triangle.Geometry.EnvelopeInternal;

                List<PyramidDivider> dividers = new List<PyramidDivider>();

                // collect the straigth line dividers
                foreach (BaseShape line in containedShapes)
                {
                    if (line.BrownShape.ShapeType == ShapeType.StraightLine)
                    {
                        PyramidDivider d = new PyramidDivider((LineString)line.Geometry, line, false);
                        if (d.Orientation != Orientation.Unknown)
                        {
                            dividers.Add(d);
                        }
                    }
                }
               
                if (dividers.Count > 0)
                {
                    triangle.BrownShape.SetTemplateBuildingBlock(TemplateType.PyramidDiagram, TemplateBuildingBlocks.Block);
                    brownTemplate.BrownShapes.Add(triangle.BrownShape);
                    
                    foreach (PyramidDivider divider in dividers)
                    {
                        if (!brownTemplate.BrownShapes.Contains(divider.BaseShape.BrownShape))
                        {
                            divider.BaseShape.BrownShape.SetTemplateBuildingBlock(TemplateType.PyramidDiagram, TemplateBuildingBlocks.Divider);
                            brownTemplate.BrownShapes.Add(divider.BaseShape.BrownShape);
                        }
                    }
                }
            }

            Console.WriteLine("Recognize time (Pyramid): " + TimeSpan.FromTicks(DateTime.Now.Ticks - t).Milliseconds);
            return brownTemplate;
        }

        public static List<BrownShape> CleanUp(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            long t = DateTime.Now.Ticks;
            BrownRecognitionSettings set = BrownRecognitionSettings.Instance;
            List<BrownShape> cleanShapes = new List<BrownShape>();

            List<BaseShape> triangles = new List<BaseShape>();
            triangles.AddRange(shapeDictionary[ShapeType.Triangle]);
            triangles.AddRange(shapeDictionary[ShapeType.RightTriangle]);
            triangles.AddRange(shapeDictionary[ShapeType.IsoscelesTriangle]);


            foreach (BaseShape triangle in triangles)
            {
                // too small of a rectangle, causes some of the NetTopoplogy buffer operations to crash.
                if (triangle.Geometry.EnvelopeInternal.Area <= 2)
                {
                    continue;
                }
                List<BaseShape> containedShapes = GeometryHelpers.ShapesThatAreContained((Geometry)triangle.Geometry.Envelope.Buffer(set.PyramidDiagramContainsThreshold), new ShapeType[] { ShapeType.StraightLine, ShapeType.Polyline }, shapeDictionary);

                // find the triangle side that is closest to vertical or horizontal.
                bool vertical = false;
                double xAverage = 0.0;
                double yAverage = 0.0;
                double offset = double.MaxValue;
                int target = 0;

                for (int i = 0; i < 3; i++)
                {
                    System.Windows.Point A = triangle.BrownShape.ShapePoints[i];
                    System.Windows.Point B = triangle.BrownShape.ShapePoints[i + 1];
                    if (Math.Abs(A.X - B.X) < offset)
                    {
                        vertical = false;
                        xAverage = (A.X + B.X) / 2.0;
                        target = i;
                        offset = Math.Abs(A.X - B.X);
                    }
                    if (Math.Abs(A.Y - B.Y) < offset)
                    {
                        vertical = true;
                        yAverage = (A.Y + B.Y) / 2.0;
                        target = i;
                        offset = Math.Abs(A.Y - B.Y);
                    }
                }

                if (vertical)
                {
                    triangle.BrownShape.ShapePoints[target].Y = yAverage;
                    triangle.BrownShape.ShapePoints[target + 1].Y = yAverage;
                }
                else
                {
                    triangle.BrownShape.ShapePoints[target].X = xAverage;
                    triangle.BrownShape.ShapePoints[target + 1].X = xAverage;
                }
                if (target == 0)
                {
                    triangle.BrownShape.ShapePoints[3].X = triangle.BrownShape.ShapePoints[0].X;
                    triangle.BrownShape.ShapePoints[3].Y = triangle.BrownShape.ShapePoints[0].Y;
                }
                else if (target == 2)
                {
                    triangle.BrownShape.ShapePoints[0].X = triangle.BrownShape.ShapePoints[3].X;
                    triangle.BrownShape.ShapePoints[0].Y = triangle.BrownShape.ShapePoints[3].Y;
                }

                List<PyramidDivider> dividers = new List<PyramidDivider>();
                // add clean triangle and divide the base triangel into 3 "dividers"
                Coordinate[] coords = new Coordinate[4];
                for (int i = 1; i < triangle.BrownShape.ShapePoints.Length; i++)
                {
                    LineString lineString = new LineString(new Coordinate[] { 
                                new Coordinate(triangle.BrownShape.ShapePoints[i - 1].X, triangle.BrownShape.ShapePoints[i - 1].Y),
                                new Coordinate(triangle.BrownShape.ShapePoints[i].X, triangle.BrownShape.ShapePoints[i].Y)});
                    dividers.Add(new PyramidDivider(lineString, triangle, true, true));
                    coords[i-1] = (Coordinate)lineString.Coordinates[0];
                    coords[i] = (Coordinate) lineString.Coordinates[1];
                }
                Geometry cleanTriangle = new Polygon(new LinearRing(coords));
                cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(cleanTriangle, triangle.BrownShape));
               

                // collect the straigth line dividers
                foreach (BaseShape line in containedShapes)
                {
                    if (line.BrownShape.ShapeType == ShapeType.StraightLine)
                    {
                        PyramidDivider d = new PyramidDivider((LineString)line.Geometry, line, false);
                        if (d.Orientation != Orientation.Unknown)
                        {
                            dividers.Add(d);
                        }
                    }
                }

                // do alignment
                foreach (PyramidDivider divider in dividers)
                {
                    List<PyramidDivider> lineUps = new List<PyramidDivider>();
                    foreach (PyramidDivider compareDivider in dividers)
                    {
                        if (divider != compareDivider && divider.Orientation != Orientation.Special && compareDivider.Orientation != Orientation.Special)
                        {
                            if (divider.Orientation == compareDivider.Orientation)
                            {
                                if ((divider.Orientation == Orientation.Vertical && Math.Abs(divider.StartCoordinate().X - compareDivider.StartCoordinate().X) < set.PyramidDiagramAlignmentThreshold) ||
                                    (divider.Orientation == Orientation.Horizontal && Math.Abs(divider.StartCoordinate().Y - compareDivider.StartCoordinate().Y) < set.PyramidDiagramAlignmentThreshold))
                                {
                                    lineUps.Add(compareDivider);
                                }
                            }
                        }
                    }
                    if (lineUps.Count > 0)
                    {
                        lineUps.Add(divider);
                        double center = 0.0;
                        foreach (PyramidDivider lineUp in lineUps)
                        {
                            if (divider.Orientation == Orientation.Horizontal)
                            {
                                center += lineUp.StartCoordinate().Y;
                            }
                            else
                            {
                                center += lineUp.StartCoordinate().X;
                            }
                        }
                        center /= (double) lineUps.Count;

                        foreach (PyramidDivider lineUp in lineUps)
                        {
                            if (lineUp.Orientation == Orientation.Horizontal)
                            {
                                lineUp.SetY(center);
                            }
                            else
                            {
                                lineUp.SetX(center);
                            }
                        }
                    }
                    
                }

                // for each divider determine which other dividers are close to the start and endpoint. 
                foreach (PyramidDivider divider in dividers)
                {
                    // closest to startpoint
                    foreach (PyramidDivider compareDivider in dividers)
                    {
                        if (divider != compareDivider && (divider.Orientation != compareDivider.Orientation || compareDivider.Orientation == Orientation.Special))
                        {
                            Coordinate inter = divider.Intersection(compareDivider);
                            Point interPoint = new Point(inter);
                            if (inter != null && cleanTriangle.Buffer(1).Contains(interPoint))
                            {
                                double dist = 0.0;
                                if (compareDivider.Orientation == Orientation.Special)
                                {
                                    dist = inter.Distance(divider.StartCoordinate());
                                }
                                else
                                {
                                    dist = compareDivider.LineString.Distance(divider.LineString.StartPoint);
                                }
                                if (dist < divider.StartPointDividerDistance)
                                {
                                    divider.StartPointIntersection = inter;
                                    divider.StartPointDivider = compareDivider;
                                    divider.StartPointDividerDistance = dist;
                                }
                            }
                        }
                    }

                    // closest to endpoint
                    foreach (PyramidDivider compareDivider in dividers)
                    {
                        if (divider != compareDivider && divider.StartPointDivider != compareDivider && (divider.Orientation != compareDivider.Orientation || compareDivider.Orientation == Orientation.Special))
                        {
                            Coordinate inter = divider.Intersection(compareDivider);
                            Point interPoint = new Point(inter);
                            if (inter != null && cleanTriangle.Buffer(1).Contains(interPoint))
                            {
                                double dist = 0.0;
                                if (compareDivider.Orientation == Orientation.Special)
                                {
                                    dist = inter.Distance(divider.EndCoordinate());
                                }
                                else
                                {
                                    dist = compareDivider.LineString.Distance(divider.LineString.EndPoint);
                                }
                                if (dist < divider.EndPointDividerDistance)
                                {
                                    divider.EndPointIntersection = inter;
                                    divider.EndPointDivider = compareDivider;
                                    divider.EndPointDividerDistance = dist;
                                }
                            }
                        }
                    }
                }


                // do the initial cleanup 
                bool didCleanUp = true;
                while (didCleanUp)
                {
                    didCleanUp = false;
                    foreach (PyramidDivider divider in dividers)
                    {
                        if (!divider.Clean)
                        {
                            if (divider.Orientation == Orientation.Horizontal)
                            {
                                Geometry cl = GeometryHelpers.CreateLine(divider.StartPointIntersection,
                                                                         divider.EndPointIntersection);
                                divider.LineString = (LineString)cl;
                                divider.Clean = true;
                                didCleanUp = true;
                            }
                            else if (divider.Orientation == Orientation.Vertical)
                            {
                                Geometry cl = GeometryHelpers.CreateLine(divider.StartPointIntersection,
                                                                         divider.EndPointIntersection); 
                                divider.LineString = (LineString)cl;
                                divider.Clean = true;
                                didCleanUp = true;
                            }
                        }
                    }
                }                

                // convert to Brownshapes
                foreach (PyramidDivider divider in dividers)
                {
                    if (divider.BaseShape != triangle)
                    {
                        cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(divider.LineString, divider.BaseShape.BrownShape));
                    }
                }
            }
            
            Console.WriteLine("Clean up time: " + TimeSpan.FromTicks(DateTime.Now.Ticks - t).Milliseconds);
            return cleanShapes;
        }
    }

    public class PyramidDivider
    {
        public BaseShape BaseShape;
        public LineString LineString;
        public Orientation Orientation;
        public bool Clean;
        public string name = "straight line";

        public PyramidDivider StartPointDivider = null;
        public Coordinate StartPointIntersection = new Coordinate(double.MaxValue, double.MaxValue);
        public PyramidDivider EndPointDivider = null;
        public Coordinate EndPointIntersection = new Coordinate(double.MaxValue, double.MaxValue);

        public double StartPointDividerDistance = double.MaxValue;
        public double EndPointDividerDistance = double.MaxValue;

        public PyramidDivider(LineString line, BaseShape baseShape, bool special, bool clean = false)
        {
            BaseShape = baseShape;
            LineString = line;
            Clean = clean;
            Orientation = Orientation.Unknown;
            if (special)
            {
                Orientation = Orientation.Special;
            }
            else
            {
                if (GeometryHelpers.IsHorizontalLine(LineString, BrownRecognitionSettings.Instance.PyramidDiagramHorizontalLineToleranceInPercentOfLength))
                {
                    Orientation = Orientation.Horizontal;
                    double YCenter = (line.StartPoint.Y + line.EndPoint.Y) / 2.0;
                    foreach (Coordinate c in line.Coordinates)
                    {
                        c.Y = YCenter;
                    }
                }
                else if (GeometryHelpers.IsVerticalLine(LineString, BrownRecognitionSettings.Instance.PyramidDiagramVerticalLineToleranceInPercentOfLength))
                {
                    Orientation = Orientation.Vertical;
                    double XCenter = (line.StartPoint.X + line.EndPoint.X) / 2.0;
                    foreach (Coordinate c in line.Coordinates)
                    {
                        c.X = XCenter;
                    }
                }
            }
        }

        public void SetX(double x)
        {
            foreach (Coordinate c in LineString.Coordinates)
            {
                c.X = x;
            }
        }

        public void SetY(double y)
        {
            foreach (Coordinate c in LineString.Coordinates)
            {
                c.Y = y;
            }
        }

        public Coordinate StartCoordinate()
        {
            return (Coordinate)LineString.Coordinates[0];
        }

        public Coordinate EndCoordinate()
        {
            return (Coordinate)LineString.Coordinates[LineString.Coordinates.Length - 1];
        }

        public Coordinate Intersection(PyramidDivider pd)
        {
            Vec b1 = new Vec(this.LineString.StartPoint.X - this.LineString.EndPoint.X, this.LineString.StartPoint.Y - this.LineString.EndPoint.Y);
            Vec b2 = new Vec(pd.LineString.StartPoint.X - pd.LineString.EndPoint.X, pd.LineString.StartPoint.Y - pd.LineString.EndPoint.Y);
            Point b3 = new Point(this.LineString.StartPoint.X - pd.LineString.StartPoint.X, this.LineString.StartPoint.Y - pd.LineString.StartPoint.Y);

            double len1 = Math.Sqrt(b1.X * b1.X + b1.Y * b1.Y);
            double len2 = Math.Sqrt(b2.X * b2.X + b2.Y * b2.Y);

            double dot = (b1.X * b2.X + b1.Y * b2.Y);
            double deg = dot / (len1 * len2);
            if (Math.Abs(deg) == 1.0)
            {
                return null;
            }

            Coordinate coord = new Coordinate(0, 0);
            double div = b2.Y * b1.X - b2.X * b1.Y;
            double ua = (b2.X * b3.Y - b2.Y * b3.X) / div;
            double ub = (b1.X * b3.Y - b1.Y * b3.X) / div;
            coord.X = this.LineString.StartPoint.X + ua * b1.X;
            coord.Y = this.LineString.StartPoint.Y + ua * b1.Y;

            return coord;
        }
    }

    public enum Orientation { Horizontal, Vertical, Special, Unknown }
}
