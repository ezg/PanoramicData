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
    public class OrgChartRecognizer
    {
        public static BrownTemplate Recognize(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            long t = DateTime.Now.Ticks;
            BrownTemplate brownTemplate = new BrownTemplate(TemplateType.OrgChart);
            
            List<BaseShape> rects = new List<BaseShape>();
            rects.AddRange(shapeDictionary[ShapeType.Rect]);
            rects.AddRange(shapeDictionary[ShapeType.Square]);
            rects.AddRange(shapeDictionary[ShapeType.RoundedRect]);
            rects.AddRange(shapeDictionary[ShapeType.Parallelogram]);
            rects.AddRange(shapeDictionary[ShapeType.Trapezoid]);
            rects.Sort(new BaseShapeYComparer());
            
            foreach (BaseShape root in rects)
            {
                if (!brownTemplate.BrownShapes.Contains(root.BrownShape))
                {
                    if (root != null)
                    {
                        List<BaseShape> traversedShapes = new List<BaseShape>();
                        RecursiveFindConnectedShapes(root, traversedShapes, shapeDictionary);
                        if (traversedShapes.Count > 1)
                        {
                            int blockCount = 0;
                            foreach (BaseShape baseShape in traversedShapes)
                            {
                                if (baseShape is PolygonShape)
                                {
                                    blockCount++;
                                }
                            }
                            if (blockCount > 1)
                            {
                                foreach (BaseShape baseShape in traversedShapes)
                                {
                                    if (baseShape is LineShape)
                                    {
                                        baseShape.BrownShape.SetTemplateBuildingBlock(TemplateType.OrgChart, TemplateBuildingBlocks.Connector);
                                        brownTemplate.BrownShapes.Add(baseShape.BrownShape);
                                    }
                                    else if (baseShape is PolygonShape)
                                    {
                                        baseShape.BrownShape.SetTemplateBuildingBlock(TemplateType.OrgChart, TemplateBuildingBlocks.Block);
                                        brownTemplate.BrownShapes.Add(baseShape.BrownShape);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            Console.WriteLine("Recognize time (org chart): " + TimeSpan.FromTicks(DateTime.Now.Ticks - t).Milliseconds);
            return brownTemplate;
        }

        private static void RecursiveFindConnectedShapes(BaseShape baseShape, List<BaseShape> traversedShapes, Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            traversedShapes.Add(baseShape);
            List<BaseShape> closeShapes = GeometryHelpers.ShapesWithinDistance(baseShape.Geometry, BrownRecognitionSettings.Instance.OrgChartBlockConnectorDistanceThreshold, new ShapeType[] { ShapeType.Rect, ShapeType.Square, ShapeType.RoundedRect, ShapeType.Parallelogram, ShapeType.Trapezoid, ShapeType.StraightLine, ShapeType.Polyline }, shapeDictionary);
            foreach (BaseShape closeShape in closeShapes)
            {
                if (!traversedShapes.Contains(closeShape))
                {
                    RecursiveFindConnectedShapes(closeShape, traversedShapes, shapeDictionary);
                }
            }
        }

        public static List<BrownShape> CleanUp(Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            long t = DateTime.Now.Ticks;
            List<BrownShape> cleanShapes = new List<BrownShape>();

            List<BaseShape> usedRects = new List<BaseShape>();

            List<BaseShape> rects = new List<BaseShape>();
            rects.AddRange(shapeDictionary[ShapeType.Rect]);
            rects.AddRange(shapeDictionary[ShapeType.Square]);
            rects.AddRange(shapeDictionary[ShapeType.RoundedRect]);
            rects.AddRange(shapeDictionary[ShapeType.Parallelogram]);
            rects.AddRange(shapeDictionary[ShapeType.Trapezoid]);
            rects.Sort(new BaseShapeYComparer());

            foreach (BaseShape root in rects)
            {
                if (!usedRects.Contains(root))
                {
                    if (root != null)
                    {
                        Node node = new Node(root, null);
                        List<BaseShape> traversedShapes = new List<BaseShape>();
                        RecursiveBuildTreeStructure(node, null, traversedShapes, shapeDictionary);
                        List<List<Node>> levels = CalculateDimensions(node);

                        BuchheimTreeLayouter.Layout(node, levels);

                        List<Node> flatList = new List<Node>();
                        TreeToFlatNodeList(node, flatList);

                        foreach (Node c in flatList)
                        {
                            BrownShape bs = GeometryHelpers.ConvertToBrownShape(c.cleanShape, c.BaseShape.BrownShape);
                            cleanShapes.Add(bs);
                            usedRects.Add(c.BaseShape);
                        }

                        RecursiveCleanUpLines(node, cleanShapes);

                    }
                }
            }

            Console.WriteLine("Clean up time: " + TimeSpan.FromTicks(DateTime.Now.Ticks - t).Milliseconds);

            return cleanShapes;
        }

        private static void RecursiveBuildTreeStructure(Node node, Node parent, List<BaseShape> traversedShapes, Dictionary<ShapeType, List<BaseShape>> shapeDictionary)
        {
            Node newParent = parent;
            if (node.BaseShape is PolygonShape)
            {
                if (parent != null)
                {
                    parent.Children.Add(node);
                    // sort the children in x direction (from left to right) 
                    parent.Children.Sort(new NodeXComparer());

                    // add lines to this node
                    for (int i = traversedShapes.Count - 1; i >= 0; i--)
                    {
                        if (traversedShapes[i] is PolygonShape)
                        {
                            break;
                        }
                        node.LinesFromParent.Add(traversedShapes[i]);
                    }
                }
                newParent = node;
            }
            traversedShapes.Add(node.BaseShape);

            List<BaseShape> closeShapes = GeometryHelpers.ShapesWithinDistance(node.BaseShape.Geometry, BrownRecognitionSettings.Instance.OrgChartBlockConnectorDistanceThreshold, new ShapeType[] { ShapeType.Rect, ShapeType.Square, ShapeType.RoundedRect, ShapeType.Parallelogram, ShapeType.Trapezoid, ShapeType.StraightLine, ShapeType.Polyline }, shapeDictionary);
            foreach (BaseShape closeShape in closeShapes)
            {
                if (!traversedShapes.Contains(closeShape))
                {
                    RecursiveBuildTreeStructure(new Node(closeShape, parent), newParent, traversedShapes, shapeDictionary);
                }
            }
        }
        
        private static void RecursiveCleanUpLines(Node node, List<BrownShape> cleanShapes)
        {
            Coordinate currentCenter = (Coordinate)node.cleanShape.EnvelopeInternal.Centre;

            if (node.Children.Count > 0)
            {
                double yGap = node.Children[0].cleanShape.EnvelopeInternal.MinY - node.cleanShape.EnvelopeInternal.MaxY;
                double yCenter = node.cleanShape.EnvelopeInternal.MaxY + yGap / 2.0;
                cleanShapes.Add(
                    GeometryHelpers.ConvertToBrownShape(
                        GeometryHelpers.CreateLine(
                            new Coordinate(currentCenter.X, node.cleanShape.EnvelopeInternal.MaxY),
                            new Coordinate(currentCenter.X, yCenter)),
                        null));

                double minX = double.MaxValue;
                double maxX = double.MinValue;
                foreach (Node child in node.Children)
                {
                    Coordinate childCenter = (Coordinate)child.cleanShape.EnvelopeInternal.Centre;
                    BrownShape bs = GeometryHelpers.ConvertToBrownShape(
                            GeometryHelpers.CreateLine(
                                new Coordinate(childCenter.X, child.cleanShape.EnvelopeInternal.MinY),
                                new Coordinate(childCenter.X, yCenter)), null);
                    foreach (BaseShape baseShape in child.LinesFromParent)
                    {
                        foreach (BrownInputStroke bis in baseShape.BrownShape.BrownInputStrokes)
                        {
                            bs.BrownInputStrokes.Add(bis);
                        }
                    }
                    cleanShapes.Add(bs);

                    minX = Math.Min(childCenter.X, minX);
                    maxX = Math.Max(childCenter.X, maxX);

                    RecursiveCleanUpLines(child, cleanShapes);
                }

                cleanShapes.Add(GeometryHelpers.ConvertToBrownShape(GeometryHelpers.CreateLine(
                    new Coordinate(minX, yCenter),
                    new Coordinate(maxX, yCenter)), null));
            }
        }

        private static List<List<Node>> CalculateDimensions(Node node)
        {
            List<List<Node>> levels = new List<List<Node>>();
            int nodeCount = RecursiveCalculateDimensions(node, 0, levels, 0);

            double totalWidthChart = 0;
            double totalHeigthChart = 0;

            double greatestWidthChart = double.MinValue;
            double greatestHeigthChart = double.MinValue;

            foreach (List<Node> level in levels)
            {
                double totalWidthLevel = 0;
                double totalHeigthLevel = 0;

                double greatestWidthLevel = double.MinValue;
                double greatestHeigthLevel = double.MinValue;

                double totalHorizontalSpace = 0;
                double totalVerticalSpace = 0;
                double greatestHorizontalSpace = double.MinValue;
                double greatesVerticalSpace = double.MinValue;

                int counter = 0;
                foreach (Node n in level)
                {
                    totalWidthLevel += n.Envelope.Width;
                    totalHeigthLevel += n.Envelope.Height;

                    greatestWidthLevel = Math.Max(greatestWidthLevel, n.Envelope.Width);
                    greatestHeigthLevel = Math.Max(greatestHeigthLevel, n.Envelope.Height);

                    double horizontalSpace = 0;
                    double verticalSpace = 0;
                    if (n.Parent != null)
                    {
                        if (counter != 0)
                        {
                            horizontalSpace = n.Envelope.MinX - level[counter - 1].Envelope.MaxX;
                        }
                        verticalSpace = n.Envelope.MinY - n.Parent.Envelope.MaxY;
                    }
                    totalHorizontalSpace += horizontalSpace;
                    totalVerticalSpace += verticalSpace;
                    greatestHorizontalSpace = Math.Max(horizontalSpace, greatestHorizontalSpace);
                    greatesVerticalSpace = Math.Max(verticalSpace, greatesVerticalSpace);

                    if (BrownRecognitionSettings.Instance.OrgChartHeightPolicy == OrgChartHeightPolicies.KeepOriginal)
                    {
                        n.Dimenson.Height = n.Envelope.Height;
                    }
                    if (BrownRecognitionSettings.Instance.OrgChartWidthPolicy == OrgChartWidthPolicies.KeepOriginal)
                    {
                        n.Dimenson.Width = n.Envelope.Width;
                    }

                    if (n.Parent != null)
                    {
                        if (counter != 0)
                        {
                            if (BrownRecognitionSettings.Instance.OrgChartHorizontalSpacingPolicy == OrgChartHorizontalSpacingPolicies.StickToConstant)
                            {
                                n.Dimenson.HorizontalSpace = BrownRecognitionSettings.Instance.OrgChartConstantHorizontalSpacing;
                            }
                        }
                        if (BrownRecognitionSettings.Instance.OrgChartVerticalSpacingPolicy == OrgChartVerticalSpacingPolicies.StickToConstant)
                        {
                            n.Dimenson.VerticalSpace = BrownRecognitionSettings.Instance.OrgChartConstantVerticalSpacing;
                        }
                    }

                    counter++;
                }

                totalWidthChart += totalWidthLevel;
                totalHeigthChart += totalHeigthLevel;

                greatestWidthChart = Math.Max(greatestWidthChart, greatestWidthLevel);
                greatestHeigthChart = Math.Max(greatestHeigthChart, greatestHeigthLevel);

                counter = 0;
                foreach (Node n in level)
                {
                    if (BrownRecognitionSettings.Instance.OrgChartHeightPolicy == OrgChartHeightPolicies.GreatesOnSameLevel)
                    {
                        n.Dimenson.Height = greatestHeigthLevel;
                    }
                    if (BrownRecognitionSettings.Instance.OrgChartWidthPolicy == OrgChartWidthPolicies.GreatesOnSameLevel)
                    {
                        n.Dimenson.Width = greatestWidthLevel;
                    }
                    if (BrownRecognitionSettings.Instance.OrgChartHeightPolicy == OrgChartHeightPolicies.AverageOnSameLevel)
                    {
                        n.Dimenson.Height = totalHeigthLevel / level.Count;
                    }
                    if (BrownRecognitionSettings.Instance.OrgChartWidthPolicy == OrgChartWidthPolicies.AverageOnSameLevel)
                    {
                        n.Dimenson.Width = totalWidthLevel / level.Count;
                    }

                    if (n.Parent != null)
                    {
                        if (counter != 0)
                        {
                            if (BrownRecognitionSettings.Instance.OrgChartHorizontalSpacingPolicy == OrgChartHorizontalSpacingPolicies.AverageOnSameLevel)
                            {
                                n.Dimenson.HorizontalSpace = level.Count > 1 ? totalHorizontalSpace / (level.Count - 1) : totalHorizontalSpace;
                            }
                            if (BrownRecognitionSettings.Instance.OrgChartHorizontalSpacingPolicy == OrgChartHorizontalSpacingPolicies.GreatesOnSameLevel)
                            {
                                n.Dimenson.HorizontalSpace = greatestHorizontalSpace;
                            }
                        }
                        if (BrownRecognitionSettings.Instance.OrgChartVerticalSpacingPolicy == OrgChartVerticalSpacingPolicies.AverageOnSameLevel)
                        {
                            n.Dimenson.VerticalSpace =  level.Count > 1 ? totalVerticalSpace / (level.Count - 1) : totalVerticalSpace;
                        }
                        if (BrownRecognitionSettings.Instance.OrgChartVerticalSpacingPolicy == OrgChartVerticalSpacingPolicies.GreatesOnSameLevel)
                        {
                            n.Dimenson.VerticalSpace = greatesVerticalSpace;
                        }
                    }

                    counter++;
                }
            }

            foreach (List<Node> level in levels)
            {
                foreach (Node n in level)
                {
                    if (BrownRecognitionSettings.Instance.OrgChartHeightPolicy == OrgChartHeightPolicies.GreatestOnChart)
                    {
                        n.Dimenson.Height = greatestHeigthChart;
                    }
                    if (BrownRecognitionSettings.Instance.OrgChartWidthPolicy == OrgChartWidthPolicies.GreatestOnChart)
                    {
                        n.Dimenson.Width = greatestWidthChart;
                    }
                    if (BrownRecognitionSettings.Instance.OrgChartHeightPolicy == OrgChartHeightPolicies.AverageOnChart)
                    {
                        n.Dimenson.Height = totalHeigthChart / nodeCount;
                    }
                    if (BrownRecognitionSettings.Instance.OrgChartWidthPolicy == OrgChartWidthPolicies.AverageOnChart)
                    {
                        n.Dimenson.Width = totalWidthChart / nodeCount;
                    }
                }
            }
            return levels;
        }

        private static int RecursiveCalculateDimensions(Node node, int level, List<List<Node>> levels, int nodeCount)
        {
            if (levels.Count <= level)
            {
                levels.Add(new List<Node>());
            }
            levels[level].Add(node);

            foreach (Node c in node.Children)
            {
                nodeCount = RecursiveCalculateDimensions(c, level + 1, levels, nodeCount);
            }

            return nodeCount + 1;
        }

        private static void TreeToFlatNodeList(Node node, List<Node> flateNodeList)
        {
            flateNodeList.Add(node);
            foreach (Node child in node.Children)
            {
                TreeToFlatNodeList(child, flateNodeList);
            }
        }
    }

    public class Node
    {
        public BaseShape BaseShape = null;
        public Node Parent = null;
        public Envelope Envelope = null;
        public List<Node> Children = new List<Node>();
        public NodeDimension Dimenson = new NodeDimension();
        public Geometry cleanShape = null;
        public List<BaseShape> LinesFromParent = new List<BaseShape>();

        public Node(BaseShape baseShape, Node parent)
        {
            BaseShape = baseShape;
            Parent = parent;
            Envelope = (Envelope)BaseShape.Geometry.EnvelopeInternal;
        }
    }

    public class NodeXComparer : IComparer<Node>
    {
        public int Compare(Node x, Node y)
        {
            return x.Envelope.MinX.CompareTo(y.Envelope.MinX);
        }
    }

    public class BaseShapeYComparer : IComparer<BaseShape>
    {
        public int Compare(BaseShape a, BaseShape b)
        {
            return a.Geometry.Centroid.Y.CompareTo(b.Geometry.Centroid.Y);
        }
    }

    public class NodeDimension
    {
        public double Height;
        public double Width;
        public double HorizontalSpace;
        public double VerticalSpace;
    }
}

