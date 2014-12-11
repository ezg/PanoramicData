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
    public class TableDiagramRecognizer
    {
        public static BrownTemplate Recognize(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            long t = DateTime.Now.Ticks;
            BrownRecognitionSettings set = BrownRecognitionSettings.Instance;
            BrownTemplate brownTemplate = new BrownTemplate(TemplateType.TableDiagram);
            
            List<BaseShape> squaresOrRects = new List<BaseShape>();
            squaresOrRects.AddRange(shapeDictionary[ShapeType.Square]);
            squaresOrRects.AddRange(shapeDictionary[ShapeType.Rect]);
            squaresOrRects.AddRange(shapeDictionary[ShapeType.RoundedRect]);


            foreach (BaseShape rect in squaresOrRects)
            {
                // too small of a rectangle, causes some of the NetTopoplogy buffer operations to crash.
                if (rect.Geometry.EnvelopeInternal.Area <= 2)
                {
                    continue;
                }

                List<BaseShape> containedShapes = GeometryHelpers.ShapesThatAreContained((Geometry)rect.Geometry.Buffer(set.TableDiagramContainsThreshold), new ShapeType[] { ShapeType.StraightLine, ShapeType.Polyline }, shapeDictionary);
                IEnvelope envelope = rect.Geometry.EnvelopeInternal;

                List<TableDivider> dividers = new List<TableDivider>();

                // collect the straigth line dividers
                foreach (BaseShape line in containedShapes)
                {
                    if (line.BrownShape.ShapeType == ShapeType.StraightLine)
                    {
                        TableDivider d = new TableDivider((LineString)line.Geometry, line);
                        if (d.Horizontal || d.Vertical)
                        {
                            dividers.Add(d);
                        }
                    }
                    else if (line.BrownShape.ShapeType == ShapeType.Polyline)
                    {
                        for (int i = 1; i < line.BrownShape.ShapePoints.Length; i++)
                        {
                            LineString lineString = new LineString(new Coordinate[] { 
                                new Coordinate(line.BrownShape.ShapePoints[i - 1].X, line.BrownShape.ShapePoints[i - 1].Y),
                                new Coordinate(line.BrownShape.ShapePoints[i].X, line.BrownShape.ShapePoints[i].Y),});

                            TableDivider d = new TableDivider(lineString, line);
                            if (d.Horizontal || d.Vertical)
                            {
                                dividers.Add(d);
                            }
                        }
                    }
                }
               
                if (dividers.Count > 0)
                {
                    rect.BrownShape.SetTemplateBuildingBlock(TemplateType.TableDiagram, TemplateBuildingBlocks.Block);
                    brownTemplate.BrownShapes.Add(rect.BrownShape);
                    
                    foreach (TableDivider divider in dividers)
                    {
                        if (!brownTemplate.BrownShapes.Contains(divider.BaseShape.BrownShape))
                        {
                            divider.BaseShape.BrownShape.SetTemplateBuildingBlock(TemplateType.TableDiagram, TemplateBuildingBlocks.Divider);
                            brownTemplate.BrownShapes.Add(divider.BaseShape.BrownShape);
                        }
                    }
                }
            }

            Console.WriteLine("Recognize time (table): " + TimeSpan.FromTicks(DateTime.Now.Ticks - t).Milliseconds);
            return brownTemplate;
        }

        public static List<BrownShape> CleanUp(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            long t = DateTime.Now.Ticks;
            BrownRecognitionSettings set = BrownRecognitionSettings.Instance;
            List<BrownShape> cleanShapes = new List<BrownShape>();            
            
            List<BaseShape> squaresOrRects = new List<BaseShape>();
            squaresOrRects.AddRange(shapeDictionary[ShapeType.Square]);
            squaresOrRects.AddRange(shapeDictionary[ShapeType.Rect]);
            squaresOrRects.AddRange(shapeDictionary[ShapeType.RoundedRect]);
            
            foreach (BaseShape rect in squaresOrRects)
            {
                // too small of a rectangle, causes some of the NetTopoplogy buffer operations to crash.
                if (rect.Geometry.EnvelopeInternal.Area <= 2)
                {
                    continue;
                }
                List<BaseShape> containedShapes = GeometryHelpers.ShapesThatAreContained((Geometry)rect.Geometry.Buffer(set.TableDiagramContainsThreshold), new ShapeType[] { ShapeType.StraightLine, ShapeType.Polyline }, shapeDictionary);
                IEnvelope envelope = rect.Geometry.EnvelopeInternal;

                cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(rect.Geometry.Envelope, rect.BrownShape));

                List<TableDivider> dividers = new List<TableDivider>();

                // collect the straigth line dividers
                foreach (BaseShape line in containedShapes)
                {
                    if (line.BrownShape.ShapeType == ShapeType.StraightLine)
                    {
                        TableDivider d = new TableDivider((LineString)line.Geometry, line);
                        if (d.Horizontal || d.Vertical)
                        {
                            dividers.Add(d);
                        }
                    }
                    else if (line.BrownShape.ShapeType == ShapeType.Polyline)
                    {
                        for (int i = 1; i < line.BrownShape.ShapePoints.Length; i++)
                        {
                            LineString lineString = new LineString(new Coordinate[] { 
                                new Coordinate(line.BrownShape.ShapePoints[i - 1].X, line.BrownShape.ShapePoints[i - 1].Y),
                                new Coordinate(line.BrownShape.ShapePoints[i].X, line.BrownShape.ShapePoints[i].Y),});

                            TableDivider d = new TableDivider(lineString, line);
                            if (d.Horizontal || d.Vertical)
                            {
                                dividers.Add(d);
                            }
                        }
                    }
                }

                // divide the base rect into 4 "dividers"
                TableDivider top = new TableDivider(new LineString(new Coordinate[] {new Coordinate(envelope.MinX, envelope.MinY), 
                                                                            new Coordinate(envelope.MaxX, envelope.MinY)}), rect, true);
                TableDivider left = new TableDivider(new LineString(new Coordinate[] {new Coordinate(envelope.MinX, envelope.MinY), 
                                                                             new Coordinate(envelope.MinX, envelope.MaxY)}), rect, true);
                TableDivider right = new TableDivider(new LineString(new Coordinate[] {new Coordinate(envelope.MaxX, envelope.MinY), 
                                                                             new Coordinate(envelope.MaxX, envelope.MaxY)}), rect, true);
                TableDivider bot = new TableDivider(new LineString(new Coordinate[] {new Coordinate(envelope.MinX, envelope.MaxY), 
                                                                             new Coordinate(envelope.MaxX, envelope.MaxY)}), rect, true);
                dividers.Add(top); top.name = "top";
                dividers.Add(left); left.name = "left";
                dividers.Add(right); right.name = "right";
                dividers.Add(bot); bot.name = "bot";

                // for each divider determine which other dividers are close to the start and endpoint. 
                foreach (TableDivider divider in dividers)
                {
                    // closest to startpoint
                    foreach (TableDivider d in dividers)
                    {
                        if (divider != d && (divider.Horizontal != d.Horizontal || divider.Vertical != d.Vertical)) 
                        {
                            double startPointDistance = divider.LineString.StartPoint.Distance(d.LineString);
                            if (startPointDistance < divider.StartPointDividerDistance)
                            {
                                divider.StartPointDividerDistance = startPointDistance;
                                divider.StartPointDivider = d;
                            }
                        }
                    }

                    // closest to endpoint
                    foreach (TableDivider d in dividers)
                    {
                        if (divider != d && divider.StartPointDivider != d && (divider.Horizontal != d.Horizontal || divider.Vertical != d.Vertical))
                        {
                            double endPointDistance = divider.LineString.EndPoint.Distance(d.LineString);
                            if (endPointDistance < divider.EndPointDividerDistance)
                            {
                                divider.EndPointDividerDistance = endPointDistance;
                                divider.EndPointDivider = d;
                            }
                        }
                    }
                }

                // do the easy cleanup (both close other dividers are already clean)
                bool didCleanUp = true;
                while (didCleanUp)
                {
                    didCleanUp = false;
                    foreach (TableDivider divider in dividers)
                    {
                        if (!divider.Clean && divider.StartPointDivider.Clean && divider.EndPointDivider.Clean)
                        {
                            if (divider.Horizontal)
                            {
                                Geometry cl = GeometryHelpers.CreateLine(new Coordinate(divider.StartPointDivider.LineString.StartPoint.X, divider.YCenter),
                                                                         new Coordinate(divider.EndPointDivider.LineString.StartPoint.X, divider.YCenter));
                                divider.LineString = (LineString)cl;
                                divider.Clean = true;
                                didCleanUp = true;
                            }
                            else if (divider.Vertical)
                            {
                                Geometry cl = GeometryHelpers.CreateLine(new Coordinate(divider.XCenter, divider.StartPointDivider.LineString.StartPoint.Y),
                                                                         new Coordinate(divider.XCenter, divider.EndPointDivider.LineString.StartPoint.Y));
                                divider.LineString = (LineString)cl;
                                divider.Clean = true;
                                didCleanUp = true;
                            }
                        }
                    }
                }

                // do the hard cleanup
                foreach (TableDivider divider in dividers)
                {
                    if (!divider.Clean)
                    {
                        Coordinate startPoint = new Coordinate(0, 0);
                        Coordinate endPoint = new Coordinate(0, 0);
                        if (divider.Horizontal) 
                        {
                            startPoint.Y = divider.YCenter;
                            endPoint.Y = divider.YCenter;
                            if (divider.StartPointDivider.Clean)
                            {
                                startPoint.X = divider.StartPointDivider.LineString.StartPoint.X;
                            }
                            else
                            {
                                startPoint.X = divider.StartPointDivider.XCenter;
                            }

                            if (divider.EndPointDivider.Clean)
                            {
                                endPoint.X = divider.EndPointDivider.LineString.StartPoint.X;
                            }
                            else
                            {
                                endPoint.X = divider.EndPointDivider.XCenter;
                            }
                        }

                        if (divider.Vertical)
                        {
                            startPoint.X = divider.XCenter;
                            endPoint.X = divider.XCenter;
                            if (divider.StartPointDivider.Clean)
                            {
                                startPoint.Y = divider.StartPointDivider.LineString.StartPoint.Y;
                            }
                            else
                            {
                                startPoint.Y = divider.StartPointDivider.YCenter;
                            }

                            if (divider.EndPointDivider.Clean)
                            {
                                endPoint.Y = divider.EndPointDivider.LineString.StartPoint.Y;
                            }
                            else
                            {
                                endPoint.Y = divider.EndPointDivider.YCenter;
                            }
                        }

                        Geometry cl = GeometryHelpers.CreateLine(startPoint, endPoint);
                        divider.LineString = (LineString)cl;
                        divider.Clean = true;
                    }
                }

                // do iterative alignment
                bool movement = true;
                int iterCount = 0;
                while (movement && iterCount < 5)
                {
                    movement = false;
                    foreach (TableDivider divider in dividers)
                    {
                        foreach (TableDivider d in dividers)
                        {
                            if (divider != d && divider.Horizontal && d.Horizontal)
                            {
                                if (Math.Abs(divider.LineString.StartPoint.X - d.LineString.StartPoint.X) < 3 ||
                                    Math.Abs(divider.LineString.StartPoint.X - d.LineString.EndPoint.X) < 3 ||
                                    Math.Abs(divider.LineString.EndPoint.X - d.LineString.StartPoint.X) < 3 ||
                                    Math.Abs(divider.LineString.EndPoint.X - d.LineString.EndPoint.X) < 3)
                                {
                                    if (Math.Abs(divider.LineString.StartPoint.Y - d.LineString.StartPoint.Y) < set.TableDiagramAlignmentThreshold &&
                                        Math.Abs(divider.LineString.StartPoint.Y - d.LineString.StartPoint.Y) != 0.0)
                                    {
                                        double newY = Math.Floor((divider.LineString.StartPoint.Y + d.LineString.StartPoint.Y) / 2.0);
                                        divider.LineString.StartPoint.Y = newY;
                                        divider.LineString.EndPoint.Y = newY;
                                        d.LineString.StartPoint.Y = newY;
                                        d.LineString.EndPoint.Y = newY;
                                        movement = true;
                                    }
                                }
                            }
                            if (divider != d && divider.Vertical && d.Vertical)
                            {
                                if (Math.Abs(divider.LineString.StartPoint.Y - d.LineString.StartPoint.Y) < 3 ||
                                    Math.Abs(divider.LineString.StartPoint.Y - d.LineString.EndPoint.Y) < 3 ||
                                    Math.Abs(divider.LineString.EndPoint.Y - d.LineString.StartPoint.Y) < 3 ||
                                    Math.Abs(divider.LineString.EndPoint.Y - d.LineString.EndPoint.Y) < 3)
                                {
                                    if (Math.Abs(divider.LineString.StartPoint.X - d.LineString.StartPoint.X) < set.TableDiagramAlignmentThreshold &&
                                        Math.Abs(divider.LineString.StartPoint.X - d.LineString.StartPoint.X) != 0.0)
                                    {
                                        double newX = Math.Floor((divider.LineString.StartPoint.X + d.LineString.StartPoint.X) / 2.0);
                                        divider.LineString.StartPoint.X = newX;
                                        divider.LineString.EndPoint.X = newX;
                                        d.LineString.StartPoint.X = newX;
                                        d.LineString.EndPoint.X = newX;
                                        movement = true;
                                    }
                                }
                            }
                        }
                    }
                    iterCount++;
                }

                // convert to Brownshapes
                foreach (TableDivider divider in dividers)
                {
                    if (divider.BaseShape != rect)
                    {
                        cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(divider.LineString, divider.BaseShape.BrownShape));
                    }
                }
            }

            Console.WriteLine("Clean up time: " + TimeSpan.FromTicks(DateTime.Now.Ticks - t).Milliseconds);
            return cleanShapes;
        }
    }

    public class TableDivider
    {
        public BaseShape BaseShape;
        public LineString LineString;
        public bool Horizontal;
        public bool Vertical;
        public double XCenter;
        public double YCenter;
        public bool Clean;
        public string name = "straight line";

        public TableDivider StartPointDivider = null;
        public TableDivider EndPointDivider = null;

        public double StartPointDividerDistance = double.MaxValue;
        public double EndPointDividerDistance = double.MaxValue;

        public TableDivider(LineString line, BaseShape baseShape, bool clean = false)
        {
            BaseShape = baseShape;
            LineString = line;
            Clean = clean;
            if (GeometryHelpers.IsHorizontalLine(LineString, BrownRecognitionSettings.Instance.TableDiagramHorizontalLineToleranceInPercentOfLength))
            {
                Horizontal = true;
                YCenter = (line.StartPoint.Y + line.EndPoint.Y) / 2.0;
            }
            else if (GeometryHelpers.IsVerticalLine(LineString, BrownRecognitionSettings.Instance.TableDiagramVerticalLineToleranceInPercentOfLength))
            {
                Vertical = true;
                XCenter = (line.StartPoint.X + line.EndPoint.X) / 2.0;
            }
            
        }
    }
}
