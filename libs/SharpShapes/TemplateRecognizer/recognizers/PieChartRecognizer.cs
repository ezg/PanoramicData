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
    public class PieChartRecognizer
    {
        public static BrownTemplate Recognize(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            BrownTemplate brownTemplate = new BrownTemplate(TemplateType.PieChart);
            BrownRecognitionSettings settings = BrownRecognitionSettings.Instance;

            List<BaseShape> ellipseOrCircles = new List<BaseShape>();
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Ellipse]);
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Circle]);

            List<BaseShape> lines = new List<BaseShape>();
            lines.AddRange(shapeDictionary[ShapeType.StraightLine]);
            lines.AddRange(shapeDictionary[ShapeType.Polyline]);

            List<BaseShape> dividers = new List<BaseShape>();
            List<BaseShape> blocks = new List<BaseShape>();
            foreach (BaseShape bs in ellipseOrCircles)
            {
                PolygonShape circle = (PolygonShape)bs;
                double radius = Math.Max(circle.Geometry.EnvelopeInternal.Width, circle.Geometry.EnvelopeInternal.Height) / 2.0; 

                foreach (BaseShape ls in lines)
                {
                    LineString lineString = (LineString)((LineShape)ls).Geometry;
                    // lines need to have a certain minium length. 
                    if (lineString.Length < radius * settings.PieChartMinimumLengthOfLinesInPercentOfCircleRadius)
                    {
                        continue;
                    }

                    if (circle.Geometry.Contains(lineString) || lineString.Intersects(circle.Geometry))
                    {
                        if (ls.BrownShape.ShapeType == ShapeType.Polyline && ls.BrownShape.ShapePoints.Length == 3)
                        {
                            dividers.Add(ls);
                            if (!blocks.Contains(circle)) blocks.Add(circle);
                        }
                        else if (ls.BrownShape.ShapeType == ShapeType.StraightLine) 
                        {
                            dividers.Add(ls);
                            if (!blocks.Contains(circle)) blocks.Add(circle);
                        }
                    }
                }
            }

            if (blocks.Count > 0 && dividers.Count > 0)
            {
                foreach (BaseShape bs in dividers)
                {
                    bs.BrownShape.SetTemplateBuildingBlock(TemplateType.PieChart, TemplateBuildingBlocks.Divider);
                    brownTemplate.BrownShapes.Add(bs.BrownShape);
                }
                foreach (BaseShape bs in blocks)
                {
                    bs.BrownShape.SetTemplateBuildingBlock(TemplateType.PieChart, TemplateBuildingBlocks.Divider);
                    brownTemplate.BrownShapes.Add(bs.BrownShape);
                }
            }

            return brownTemplate;
        }

        public static List<BrownShape> CleanUp(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            List<BrownShape> cleanShapes = new List<BrownShape>();

            BrownRecognitionSettings settings = BrownRecognitionSettings.Instance;

            List<BaseShape> ellipseOrCircles = new List<BaseShape>();
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Ellipse]);
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Circle]);

            List<BaseShape> lines = new List<BaseShape>();
            lines.AddRange(shapeDictionary[ShapeType.StraightLine]);
            lines.AddRange(shapeDictionary[ShapeType.Polyline]);

            foreach (BaseShape bs in ellipseOrCircles)
            {
                PolygonShape circle = (PolygonShape)bs;
                double radius = Math.Max(circle.Geometry.EnvelopeInternal.Width, circle.Geometry.EnvelopeInternal.Height) / 2.0;

                Geometry cleanCircle = GeometryHelpers.CreateCircle((Coordinate)bs.Geometry.Centroid.Coordinate, radius);
                cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(cleanCircle, bs.BrownShape));

                IPoint centroid = cleanCircle.Centroid;

                foreach (BaseShape ls in lines)
                {
                    LineString lineString = (LineString)((LineShape)ls).Geometry;
                    if (circle.Geometry.Contains(lineString) || lineString.Intersects(circle.Geometry))
                    {
                        if (ls.BrownShape.ShapeType == ShapeType.StraightLine)
                        {
                            // make sure the starpoint is the one closer to the centroid
                            if (lineString.EndPoint.Distance(centroid) < lineString.StartPoint.Distance(centroid))
                            {
                                lineString = (LineString)lineString.Reverse();
                            }

                            // check if this is a long divider (edge of circle to edge of circle)
                            Vec t1 = new Vec(lineString.EndPoint.X - centroid.Coordinate.X, lineString.EndPoint.Y - centroid.Coordinate.Y);
                            Vec t2 = new Vec(lineString.StartPoint.X - centroid.Coordinate.X, lineString.StartPoint.Y - centroid.Coordinate.Y);
                            Vec l = new Vec(lineString.EndPoint.X - lineString.StartPoint.X, lineString.EndPoint.Y - lineString.StartPoint.Y);
                            double lengthThreshold = l.Length * 0.25;
                            if (t1.Length - t2.Length < lengthThreshold &&
                                t1.UnsignedAngle(t2) > 2.5)
                            {
                                l.Normalize();
                                l *= radius;
                                Geometry line = GeometryHelpers.CreateLine((Coordinate)centroid.Coordinate,
                                    new Coordinate(l.X + centroid.Coordinate.X, l.Y + centroid.Coordinate.Y));
                                cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(line, ls.BrownShape));

                                l *= -1.0;
                                line = GeometryHelpers.CreateLine((Coordinate)centroid.Coordinate,
                                    new Coordinate(l.X + centroid.Coordinate.X, l.Y + centroid.Coordinate.Y));
                                cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(line, ls.BrownShape));
                            }
                            else
                            {
                                Vec v = new Vec(lineString.EndPoint.X - centroid.Coordinate.X, lineString.EndPoint.Y - centroid.Coordinate.Y);
                                v.Normalize();
                                v *= radius;

                                Geometry line = GeometryHelpers.CreateLine((Coordinate)centroid.Coordinate,
                                    new Coordinate(v.X + centroid.Coordinate.X, v.Y + centroid.Coordinate.Y));
                                cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(line, ls.BrownShape));
                            }
                        }
                        else if (ls.BrownShape.ShapeType == ShapeType.Polyline)
                        {
                            System.Windows.Point[] shapeRecogPoints = ls.BrownShape.ShapePoints;
                            System.Windows.Point center = shapeRecogPoints[1];
                            Vec offset = new Vec(center.X - centroid.X, center.Y - centroid.Y);

                            for (int i = 0; i < 3; i += 2)
                            {
                                Vec v = new Vec(shapeRecogPoints[i].X - offset.X - centroid.Coordinate.X,
                                                shapeRecogPoints[i].Y - offset.Y - centroid.Coordinate.Y);
                                v.Normalize();
                                v *= radius;

                                Geometry line = GeometryHelpers.CreateLine((Coordinate)centroid.Coordinate,
                                    new Coordinate(v.X + centroid.Coordinate.X, v.Y + centroid.Coordinate.Y));

                                BrownShape newBs = GeometryHelpers.ConvertToBrownShape(line, null);
                                newBs.BrownInputStrokes = ls.BrownShape.BrownInputStrokes;
                                newBs.ShapeType = ShapeType.StraightLine;
                                cleanShapes.Add(newBs);
                            }
                        }
                    }
                }
            }
                        
            return cleanShapes;
        }
    }
}
