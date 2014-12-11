using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Serialization;
using System.IO;
using starPadSDK.Inq;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrownRecognitionCommon
{
    /// <summary>
    /// Enumeration of all the differnet shape types supported by the BrownRecognition API.
    /// </summary>
    public enum ShapeType { None, Scribble, Polyline, StraightLine, Triangle, RightTriangle, IsoscelesTriangle, RoundedRect, Rect, Square, Parallelogram, Circle, Trapezoid, Ellipse, Diamond };

    /// <summary>
    /// Enumeration of all the differnet template types supported by the BrownRecognition API.
    /// </summary>
    public enum TemplateType { All, None, OrgChart, VennDiagram, PieChart, TableDiagram, BullsEye, PyramidDiagram };

    /// <summary>
    /// Enumeration of different template building blocks. A shape that is used in a template will have one of those assigned.
    /// </summary>
    public enum TemplateBuildingBlocks { None, Connector, Block, Divider };

    /// <summary>
    /// Encapsulates an input stroke. 
    /// </summary>
    public class BrownInputStroke
    {
        /// <summary>
        /// Points of the stroke.
        /// </summary>
        public Point[] StrokePoints;

        /// <summary>
        /// Any object that a user wants to attach to this stroke. This is useful to backtrack 
        /// application strokes. 
        /// </summary>
        public object Data;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="pts">The stroke points.</param>
        /// <param name="data">The stroke data.</param>
        public BrownInputStroke(Point[] pts, object data) { StrokePoints = pts; Data = data; }
    }

    /// <summary>
    /// Encapsulates a recognized template. 
    /// </summary>
    public class BrownTemplate
    {
        /// <summary>
        /// List of shapes that are part of this template.
        /// </summary>
        public List<BrownShape> BrownShapes = new List<BrownShape>();

        /// <summary>
        /// The recognized template type. 
        /// </summary>
        public TemplateType TemplateType;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="templateType">Recognized template type. </param>
        public BrownTemplate(TemplateType templateType) { this.TemplateType = templateType; }
    }

    /// <summary>
    /// Encapsulates a recognized shape. 
    /// </summary>
    public class BrownShape
    {
        /// <summary>
        /// The type of the recognized shape. 
        /// </summary>
        public ShapeType ShapeType;

        /// <summary>
        /// Clean version of the shape. 
        /// </summary>
        public Point[] ShapePoints;

        /// <summary>
        /// Input strokes that are used to make up this shape. 
        /// </summary>
        public List<BrownInputStroke> BrownInputStrokes = new List<BrownInputStroke>();
        private Dictionary<TemplateType, TemplateBuildingBlocks> _TemplateBuildingBlockAssignments = new Dictionary<TemplateType, TemplateBuildingBlocks>();

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="shapeType">Recognized shape type.</param>
        /// <param name="shapePoints">The clean shape points. </param>
        /// <param name="brownInputStroke">The input stroke (only useful if this shape has only one)</param>
        public BrownShape(ShapeType shapeType, Point[] shapePoints, BrownInputStroke brownInputStroke) 
        {
            ShapeType = shapeType;
            ShapePoints = shapePoints;
            if (brownInputStroke != null)
            {
                BrownInputStrokes.Add(brownInputStroke);
            }
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="shapeType">Recognized shape type.</param>
        /// <param name="shapePoints">The clean shape points. </param>
        public BrownShape(ShapeType shapeType, Point[] shapePoints)
        {
            ShapeType = shapeType;
            ShapePoints = shapePoints;
        }

        /// <summary>
        /// If this shape is part of a BrownRecognitionCommon.BrownTemplate, this method
        /// returns the corresponding BrownRecognitionCommon.TemplateBuildingBlock type of it.
        /// </summary>
        /// <param name="templateType">The template type.</param>
        /// <returns>The corresponding building block type.</returns>
        public TemplateBuildingBlocks GetTemplateBuildingBlock(TemplateType templateType)
        {
            if (_TemplateBuildingBlockAssignments.ContainsKey(templateType))
            {
                return _TemplateBuildingBlockAssignments[templateType];
            }
            else
            {
                return TemplateBuildingBlocks.None;
            }
        }

        /// <summary>
        /// Sets the BrownRecognitionCommon.TemplateBuildingBlock type of this shape. 
        /// </summary>
        /// <param name="templateType">The template type.</param>
        /// <param name="tbb">The corresponding building block type.</param>
        public void SetTemplateBuildingBlock(TemplateType templateType, TemplateBuildingBlocks tbb)
        {
            if (_TemplateBuildingBlockAssignments.ContainsKey(templateType))
            {
                _TemplateBuildingBlockAssignments[templateType] = tbb;
            }
            else
            {
                _TemplateBuildingBlockAssignments.Add(templateType, tbb);
            }
        }
    }

    // Only internal.
    public class BrownLine
    {
        public Point First;
        public Point Last;
        public object Data;
        public BrownLine(Point first, Point last, object data) { First = first; Last = last; Data = data; }
    }

   
    // Renders a BrownShape. This class is only used internally. 
    public class BrownShapeRenderer : Canvas
    {
        public BrownShape BrownShape = null;
        public Polyline Polyline = null;
        public BrownTemplate BrownTemplate = null;
        public BrownShapeRenderer(BrownShape brownShape, BrownTemplate brownTemplate = null)
            : base()
        {
            BrownShape = brownShape;
            BrownTemplate = brownTemplate;
            Polyline pl = new Polyline();
            pl.Points = new PointCollection(brownShape.ShapePoints);
            Brush fill = Brushes.Black;

            if (BrownTemplate == null)
            {
                ShapeType tt = brownShape.ShapeType;
                if (tt == ShapeType.Triangle) fill = Brushes.LightGreen;
                else if (tt == ShapeType.IsoscelesTriangle) fill = Brushes.Green;
                else if (tt == ShapeType.RightTriangle) fill = Brushes.Brown;
                else if (tt == ShapeType.Trapezoid) fill = Brushes.Purple;
                else if (tt == ShapeType.Ellipse) fill = Brushes.Red;
                else if (tt == ShapeType.Circle) fill = Brushes.Pink;
                else if (tt == ShapeType.Parallelogram) fill = Brushes.Cyan;
                else if (tt == ShapeType.Rect) fill = Brushes.DarkBlue;
                else if (tt == ShapeType.Square) fill = Brushes.Blue;
                else if (tt == ShapeType.Diamond) fill = Brushes.Yellow;
                else if (tt == ShapeType.RoundedRect) fill = Brushes.Beige;
            }
            else
            {
                if (BrownTemplate.TemplateType == TemplateType.VennDiagram)
                {
                    fill = new SolidColorBrush(Color.FromArgb(110, 255, 0, 0));
                }
                else
                {
                    fill = Brushes.LightGray;
                }
            }
            if (fill != Brushes.Black)
            {
                pl.Fill = fill;
            }
            pl.Stroke = Brushes.Black;
            pl.StrokeThickness = 3;

            Polyline = pl;
            this.RenderTransform = new TranslateTransform();
            this.Children.Add(pl);
        }
    }

    /// <summary>
    /// Enumeration of different width policies for blocks in an OrgChart.
    /// - KeepOriginal: Width will be roughly same as the input block.
    /// - AverageOnSameLevel: Width will be averaged over all input blocks on the same level.
    /// - GreatesOnSameLevel: Width will be the maximum width of all input blocks on the same level.
    /// - AverageOnChart: Width will be averaged over all input blocks in the chart.
    /// - GreatestOnChart: Width will be the maximum width of all input blocks in the chart.
    /// </summary>
    public enum OrgChartWidthPolicies { KeepOriginal, AverageOnSameLevel, GreatesOnSameLevel, AverageOnChart, GreatestOnChart };

    /// <summary>
    /// Enumeration of different height policies for blocks in an OrgChart.
    /// - KeepOriginal: Height will be roughly same as the input block.
    /// - AverageOnSameLevel: Height will be averaged over all input blocks on the same level.
    /// - GreatesOnSameLevel: Height will be the maximum height of all input blocks on the same level.
    /// - AverageOnChart: Height will be averaged over all input blocks in the chart.
    /// - GreatestOnChart: Height will be the maximum height of all input blocks in the chart.
    /// </summary>
    public enum OrgChartHeightPolicies { KeepOriginal, AverageOnSameLevel, GreatesOnSameLevel, AverageOnChart, GreatestOnChart };

    /// <summary>
    /// Enumeration of different horizontal spacing policies between blocks in an OrgChart.
    /// - AverageOnSameLevel: Spacing will be averaged over all input blocks on the same level.
    /// - GreatesOnSameLevel: Spacing will be the maximum space of all blocks on the same level.
    /// - StickToConstant: Spacing will be a constant value (see BrownRecognitionCommon.BrownRecognitionSettings).
    /// </summary>
    public enum OrgChartHorizontalSpacingPolicies { AverageOnSameLevel, GreatesOnSameLevel, StickToConstant };

    /// <summary>
    /// Enumeration of different vertical spacing policies between blocks in an OrgChart.
    /// - AverageOnSameLevel: Spacing will be averaged over all input blocks on the same level.
    /// - GreatesOnSameLevel: Spacing will be the maximum space of all blocks on the same level.
    /// - StickToConstant: Spacing will be a constant value (see BrownRecognitionCommon.BrownRecognitionSettings).
    /// </summary>
    public enum OrgChartVerticalSpacingPolicies { AverageOnSameLevel, GreatesOnSameLevel, StickToConstant };

    /// <summary>
    ///  Singelton that encapsulates all the settings used in the BrownRecognition API.
    /// </summary>
    public class BrownRecognitionSettings
    {
        private static BrownRecognitionSettings _instance;

        private BrownRecognitionSettings() {}

        /// <summary>
        /// Returns the current instance. 
        /// </summary>
        public static BrownRecognitionSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BrownRecognitionSettings();
                }
                return _instance;
            }
        }

        // OrgChart properties
        #region 
        /// <summary>
        /// Threshold in pixels of when a connector (line) is close to a block (used in OrgChart recognition). 
        /// </summary>
        public int OrgChartBlockConnectorDistanceThreshold = 25;

        /// <summary>
        /// Width policy for OrgChart (see BrownRecognitionCommon.OrgChartWidthPolicies).
        /// </summary>
        public OrgChartWidthPolicies OrgChartWidthPolicy = OrgChartWidthPolicies.AverageOnChart;

        /// <summary>
        /// Height policy for OrgChart (see BrownRecognitionCommon.OrgChartHeightPolicies).
        /// </summary>
        public OrgChartHeightPolicies OrgChartHeightPolicy = OrgChartHeightPolicies.AverageOnChart;

        /// <summary>
        /// Horizontal spacing policy for OrgChart (see BrownRecognitionCommon.OrgChartHorizontalSpacingPolicies).
        /// </summary>
        public OrgChartHorizontalSpacingPolicies OrgChartHorizontalSpacingPolicy = OrgChartHorizontalSpacingPolicies.StickToConstant;

        /// <summary>
        /// Vertical spacing policy for OrgChart (see BrownRecognitionCommon.OrgChartVerticalSpacingPolicies).
        /// </summary>
        public OrgChartVerticalSpacingPolicies OrgChartVerticalSpacingPolicy = OrgChartVerticalSpacingPolicies.StickToConstant;

        /// <summary>
        /// If OrgChartHorizontalSpacingPolicies is set to StickToConstant, this value will be used.
        /// </summary>
        public double OrgChartConstantHorizontalSpacing = 30;

        /// <summary>
        /// If OrgChartVerticalSpacingPolicies is set to StickToConstant, this value will be used.
        /// </summary>
        public double OrgChartConstantVerticalSpacing = 30;
        #endregion

        // PieChart properties
        #region
        /// <summary>
        /// Minimum length of divider lines in percentage of the circle radius (used for recognition). 
        /// Should be in the interval of [0, 1] 
        /// </summary>
        public double PieChartMinimumLengthOfLinesInPercentOfCircleRadius = 0.2;
        #endregion

        // TableDiagram properties
        #region
        /// <summary>
        /// Threshold in pixels of when a divider (line) is still inside a rectangle (used in TableDiagram recognition).
        /// </summary>
        public int TableDiagramContainsThreshold = 25;

        /// <summary>
        /// Threshold in pixels of when a divider (line) should snap to another divider line (used in TableDiagram clean-up).
        /// </summary>
        public int TableDiagramAlignmentThreshold = 10;

        /// <summary>
        /// Tolerance, in percentage of the total line length, of when a line gets recognized as a vertical line. 
        /// Should be in the interval of [0, 1] 
        /// </summary>
        public double TableDiagramVerticalLineToleranceInPercentOfLength = 0.3;

        /// <summary>
        /// Tolerance, in percentage of the total line length, of when a line gets recognized as a horizontal line. 
        /// Should be in the interval of [0, 1] 
        /// </summary>
        public double TableDiagramHorizontalLineToleranceInPercentOfLength = 0.3;
        #endregion

        // PyramidDiagram properties
        #region
        /// <summary>
        /// Threshold in pixels of when a divider (line) is still inside a triangle (used in TableDiagram recognition).
        /// </summary>
        public int PyramidDiagramContainsThreshold = 25;

        /// <summary>
        /// Threshold in pixels of when a divider (line) should snap to another divider line (used in PyramidDiagram clean-up).
        /// </summary>
        public int PyramidDiagramAlignmentThreshold = 10;

        /// <summary>
        /// Tolerance, in percentage of the total line length, of when a line gets recognized as a vertical line. 
        /// Should be in the interval of [0, 1] 
        /// </summary>
        public double PyramidDiagramVerticalLineToleranceInPercentOfLength = 0.3;

        /// <summary>
        /// Tolerance, in percentage of the total line length, of when a line gets recognized as a horizontal line. 
        /// Should be in the interval of [0, 1] 
        /// </summary>
        public double PyramidDiagramHorizontalLineToleranceInPercentOfLength = 0.3;
        #endregion


        /// <summary>
        /// Serializes all settings to XML.
        /// </summary>
        /// <returns>XML string containing all the current setting values.</returns>
        public static string SerializeToXML() 
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BrownRecognitionSettings));
            StringWriter stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, BrownRecognitionSettings.Instance);
            stringWriter.Close();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Deserializes settings from XML.
        /// </summary>
        /// <param name="xml">XML string containing all the setting values.</param>
        public static void DeserializeFromXML(string xml)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(BrownRecognitionSettings));
            StringReader stringReader = new StringReader(xml);
            _instance = (BrownRecognitionSettings)deserializer.Deserialize(stringReader);
            stringReader.Close();
        }
    }
}
