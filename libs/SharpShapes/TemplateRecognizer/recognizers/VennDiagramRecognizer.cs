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
    public class VennDiagramRecognizer
    {
        public static BrownTemplate Recognize(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            BrownTemplate brownTemplate = new BrownTemplate(TemplateType.VennDiagram);

            List<BaseShape> ellipseOrCircles = new List<BaseShape>();
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Ellipse]);
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Circle]);

            foreach (BaseShape bs in ellipseOrCircles)
            {
                List<BaseShape> intersections = GeometryHelpers.ShapesThatIntersect(bs.Geometry, new ShapeType[] { ShapeType.Circle, ShapeType.Ellipse }, shapeDictionary);

                foreach (BaseShape intersection in intersections)
                {
                    if (!intersection.Geometry.Contains(bs.Geometry) && !bs.Geometry.Contains(intersection.Geometry))
                    {
                        bs.BrownShape.SetTemplateBuildingBlock(TemplateType.VennDiagram, TemplateBuildingBlocks.Block);
                        if (!brownTemplate.BrownShapes.Contains(bs.BrownShape))
                        {
                            brownTemplate.BrownShapes.Add(bs.BrownShape);
                        }
                    }
                }
            }

            return brownTemplate;
        }

        public static List<BrownShape> CleanUp(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            List<BrownShape> cleanShapes = new List<BrownShape>();
            List<BaseShape> cleanedUp = new List<BaseShape>();

            List<BaseShape> ellipseOrCircles = new List<BaseShape>();
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Ellipse]);
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Circle]);

            foreach (BaseShape bs in ellipseOrCircles)
            {
                List<BaseShape> intersections = GeometryHelpers.ShapesThatIntersect(bs.Geometry, new ShapeType[] { ShapeType.Circle, ShapeType.Ellipse }, shapeDictionary);
                foreach (BaseShape intersection in intersections)
                {
                    if (!intersection.Geometry.Contains(bs.Geometry) && !bs.Geometry.Contains(intersection.Geometry))
                    {
                        if (!cleanedUp.Contains(intersection))
                        {
                            Geometry circle = GeometryHelpers.CreateCircle((Coordinate)intersection.Geometry.Centroid.Coordinate,
                                                                Math.Max(intersection.Geometry.EnvelopeInternal.Width, intersection.Geometry.EnvelopeInternal.Height) / 2.0);
                            cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(circle, intersection.BrownShape));
                            cleanedUp.Add(intersection);
                        }
                    }
                }
            }

            return cleanShapes;
        }
    }
}
