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
    public class BullsEyeRecognizer
    {
        public static BrownTemplate Recognize(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            BrownTemplate brownTemplate = new BrownTemplate(TemplateType.BullsEye);
            BrownRecognitionSettings settings = BrownRecognitionSettings.Instance;

            List<BaseShape> ellipseOrCircles = new List<BaseShape>();
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Ellipse]);
            ellipseOrCircles.AddRange(shapeDictionary[ShapeType.Circle]);
            ellipseOrCircles.Sort(new BaseShapeRadiusComparer());
            ellipseOrCircles.Reverse();

            foreach (BaseShape bs in ellipseOrCircles)
            {
                PolygonShape circle = (PolygonShape)bs;
                List<BaseShape> containedShapes = GeometryHelpers.ShapesThatAreContained((Geometry)circle.Geometry, new ShapeType[] { ShapeType.Circle, ShapeType.Ellipse }, shapeDictionary);
                containedShapes.Sort(new BaseShapeRadiusComparer());
                containedShapes.Reverse();
                foreach (BaseShape containedShape in containedShapes)
                {
                    if (!brownTemplate.BrownShapes.Contains(bs.BrownShape))
                    {
                        bs.BrownShape.SetTemplateBuildingBlock(TemplateType.BullsEye, TemplateBuildingBlocks.Divider);
                        brownTemplate.BrownShapes.Add(bs.BrownShape);
                    }
                    if (!brownTemplate.BrownShapes.Contains(containedShape.BrownShape))
                    {
                        containedShape.BrownShape.SetTemplateBuildingBlock(TemplateType.BullsEye, TemplateBuildingBlocks.Divider);
                        brownTemplate.BrownShapes.Add(containedShape.BrownShape);
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
            ellipseOrCircles.Sort(new BaseShapeRadiusComparer());
            ellipseOrCircles.Reverse();

            foreach (BaseShape bs in ellipseOrCircles)
            {
                List<BaseShape> containedShapes = GeometryHelpers.ShapesThatAreContained((Geometry)bs.Geometry, new ShapeType[] { ShapeType.Circle, ShapeType.Ellipse }, shapeDictionary);
                containedShapes.Sort(new BaseShapeRadiusComparer());
                containedShapes.Reverse();
                foreach (BaseShape containedShape in containedShapes)
                {
                    if (!cleanedUp.Contains(bs))
                    {
                        Geometry circle = GeometryHelpers.CreateCircle((Coordinate)bs.Geometry.Centroid.Coordinate,
                                    Math.Max(bs.Geometry.EnvelopeInternal.Width, bs.Geometry.EnvelopeInternal.Height) / 2.0);
                        Console.WriteLine(circle.EnvelopeInternal.Area);
                        cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(circle, bs.BrownShape));
                        cleanedUp.Add(bs);
                    }
                    if (!cleanedUp.Contains(containedShape))
                    {
                        Geometry circle = GeometryHelpers.CreateCircle((Coordinate)bs.Geometry.Centroid.Coordinate,
                                    Math.Max(containedShape.Geometry.EnvelopeInternal.Width, containedShape.Geometry.EnvelopeInternal.Height) / 2.0);
                        Console.WriteLine(circle.EnvelopeInternal.Area);
                        cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(circle, containedShape.BrownShape));
                        cleanedUp.Add(containedShape);
                    }
                }
            }
            return cleanShapes;
        }        
    }

    public class BaseShapeRadiusComparer : IComparer<BaseShape>
    {
        public int Compare(BaseShape x, BaseShape y)
        {
            double radius1 = Math.Max(x.Geometry.EnvelopeInternal.Width, x.Geometry.EnvelopeInternal.Height) / 2.0;
            double radius2 = Math.Max(y.Geometry.EnvelopeInternal.Width, y.Geometry.EnvelopeInternal.Height) / 2.0;

            return radius1.CompareTo(radius2);
        }
    }
}

