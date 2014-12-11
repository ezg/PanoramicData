using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using starPadSDK.Inq;
using NetTopologySuite.Geometries;
using starPadSDK.Geom;
using NetTopologySuite.Operation.Distance;
using GeoAPI.Geometries;
using System.Windows.Media;
using BrownRecognitionCommon;

namespace TemplateRecognizer
{
    public static class TemplateRecognizer
    {
        static TemplateRecognizer()
        {
        }

        public static List<BrownTemplate> RecognizeTemplate(List<BrownShape> brownShapes)
        {
            Dictionary<ShapeType, List<BaseShape>> shapeDictionary = InitializeDictionary(brownShapes);
            List<BrownTemplate> recognizedTemplates = new List<BrownTemplate>();

            BrownTemplate bt;
            bt = OrgChartRecognizer.Recognize(shapeDictionary);
            if (bt.BrownShapes.Count > 0) recognizedTemplates.Add(bt);

            bt = PieChartRecognizer.Recognize(shapeDictionary);
            if (bt.BrownShapes.Count > 0) recognizedTemplates.Add(bt);

            bt = VennDiagramRecognizer.Recognize(shapeDictionary);
            if (bt.BrownShapes.Count > 0) recognizedTemplates.Add(bt);

            bt = TableDiagramRecognizer.Recognize(shapeDictionary);
            if (bt.BrownShapes.Count > 0) recognizedTemplates.Add(bt);

            bt = BullsEyeRecognizer.Recognize(shapeDictionary);
            if (bt.BrownShapes.Count > 0) recognizedTemplates.Add(bt);

            bt = PyramidDiagramRecognizer.Recognize(shapeDictionary);
            if (bt.BrownShapes.Count > 0) recognizedTemplates.Add(bt);

            return recognizedTemplates; 
        }

        public static List<BrownShape> CleanUpDiagram(BrownTemplate template)
        {
            Dictionary<ShapeType, List<BaseShape>> shapeDictionary = InitializeDictionary(template.BrownShapes);

            switch (template.TemplateType)
            {
                case TemplateType.OrgChart:
                    return OrgChartRecognizer.CleanUp(shapeDictionary);
                case TemplateType.VennDiagram:
                    return VennDiagramRecognizer.CleanUp(shapeDictionary);
                case TemplateType.PieChart:
                    return PieChartRecognizer.CleanUp(shapeDictionary);
                case TemplateType.TableDiagram:
                    return TableDiagramRecognizer.CleanUp(shapeDictionary);
                case TemplateType.BullsEye:
                    return BullsEyeRecognizer.CleanUp(shapeDictionary);
                case TemplateType.PyramidDiagram:
                    return PyramidDiagramRecognizer.CleanUp(shapeDictionary);
                default:
                    return new List<BrownShape>();
            }
        }

        private static Dictionary<ShapeType, List<BaseShape>> InitializeDictionary(List<BrownShape> brownShapes)
        {
            Dictionary<ShapeType, List<BaseShape>> shapeDictionary = new Dictionary<ShapeType, List<BaseShape>>();

            // Make sure we have an empty list for all ShapeTypes.
            foreach (ShapeType shapeType in Enum.GetValues(typeof(ShapeType)).Cast<ShapeType>())
            {
                shapeDictionary.Add(shapeType, new List<BaseShape>());
            }

            foreach (BrownShape bs in brownShapes)
            {
                ShapeType shapeType = bs.ShapeType;

                BaseShape shape;
                if (shapeType == ShapeType.StraightLine)
                {
                    shape = new LineShape(bs);
                }
                else if (shapeType == ShapeType.Polyline)
                {
                    shape = new LineShape(bs);
                }
                else
                {
                    shape = new PolygonShape(bs);
                }
               
                shapeDictionary[shapeType].Add(shape);
            }

            return shapeDictionary;
        }

    }
}
