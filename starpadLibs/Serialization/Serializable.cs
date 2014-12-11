using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.AppLib;
using starPadSDK.MathExpr;

namespace Serialization
{
    public interface Serializable
    {
        Model GetModel();
        void InitFromModel(Model model);
    }

    #region mainModels

    [Serializable]
    public class Model
    {
    }

    [Serializable]
    public class StroqModel : Model
    {
        public static implicit operator StroqModel(Stroq stroq)
        {
            StroqModel m = new StroqModel();
            m.Id = stroq.Id.ToString();
            m.IsHighlighter = stroq.BackingStroke.DrawingAttributes.IsHighlighter;
            m.Height = stroq.BackingStroke.DrawingAttributes.Height;
            m.Width = stroq.BackingStroke.DrawingAttributes.Width;
            m.Color = stroq.BackingStroke.DrawingAttributes.Color;
            m.SecondRenderPass = stroq.SecondRenderPass;
            m.SecondRenderPassColor = stroq.SecondRenderPassColor;
            m.SecondRenderPassPressureFactors = stroq.SecondRenderPassPressureFactors;
            m.StylusPoints = new List<StylusPointModel>();

            foreach (var sp in stroq.StylusPoints)
            {
                m.StylusPoints.Add(sp);
            }

            return m;
        }

        public static implicit operator Stroq(StroqModel m)
        {
            Stroq s = new Stroq(new StylusPointCollection(m.StylusPoints.Select((p) => (StylusPoint)(p))));
            s.Id = Guid.Parse(m.Id);
            s.BackingStroke.DrawingAttributes.IsHighlighter = m.IsHighlighter;
            s.BackingStroke.DrawingAttributes.Height = m.Height;
            s.BackingStroke.DrawingAttributes.Width = m.Width;
            s.BackingStroke.DrawingAttributes.Color = m.Color;
            s.SecondRenderPass = m.SecondRenderPass;
            s.SecondRenderPassColor = m.SecondRenderPassColor;
            s.SecondRenderPassPressureFactors = m.SecondRenderPassPressureFactors;
            return s;
        }
        public string Id { get; set; }
        public List<StylusPointModel> StylusPoints { get; set; }
        public bool IsHighlighter { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public ColorModel Color { get; set; }
        public bool SecondRenderPass { get; set; }
        public ColorModel SecondRenderPassColor { get; set; }
        public List<float> SecondRenderPassPressureFactors { get; set; }
        public List<string> TableGroups { get; set; }
        public List<string> BulletListGroups { get; set; }
    }

    [Serializable]
    public class WidgetModel : Model
    {
        public SizeModel Size { get; set; }
        public PositionModel Position { get; set; }
        public AngleModel Angle { get; set; }
    }

    [Serializable]
    public class ShapeModel : WidgetModel
    {
        public List<PointModel> Points { get; set; }
        public ColorModel Color { get; set; }
        public string Text { get; set; }
        public List<Guid> AssociatedStroqs { get; set; }
        public Expr RenderableExpr { get; set; }
        public bool IsRenderableExprNumeric { get; set; }
    }

    [Serializable]
    public class BubbleModel : WidgetModel
    {
        public string Text { get; set; }
        public string DisplayMode { get; set; }
        public List<Guid> AssociatedStroqs { get; set; }
        public List<Guid> HeaderStroqs { get; set; }
        public List<LabelModel> ProvidedLabels { get; set; }
        public List<LabelModel> ConsumedLabels { get; set; }
        public List<PointModel> Outline { get; set; }
        public Expr RenderableExpr { get; set; }
        public bool IsRenderableExprNumeric { get; set; }
    }

    [Serializable]
    public class ImageModel : WidgetModel
    {
        public string FileName { get; set; }
        public string Link { get; set; }
    }

    [Serializable]
    public class TextModel : WidgetModel
    {
        public List<TextLineModel> LineModels { get; set; }
    }

    [Serializable]
    public class TableModel : WidgetModel
    {
        public int NumberOfRows { get; set; }
        public int NumberOfCols { get; set; }
        public List<CellModel> CellModels { get; set; }
        public List<LabelModel> ConsumedLabels { get; set; }
    }

    [Serializable]
    public class ChartModel : WidgetModel
    {
        public List<List<PointModel>> Series { get; set; }
        public List<String> SerieNames { get; set; }
        public List<LabelModel> ConsumedLabels { get; set; }
        public List<Guid> AssociatedStroqs { get; set; }
    }

    [Serializable]
    public class ChartCleanModel : WidgetModel
    {
        public List<string> Headers { get; set; }
        public List<List<string>> Values { get; set; }
        public string XHeader { get; set; }
        public string YHeader { get; set; }
        public string Type { get; set; }
    }

    #endregion

    #region subModels
    [Serializable]
    public class LabelModel : Model
    {
        public string Id { get; set; }
        public List<Guid> AssociatedStroqs { get; set; }
    }


    [Serializable]
    public class AngleModel
    {
        public static implicit operator AngleModel(double angle)
        {
            AngleModel m = new AngleModel();
            m.A = angle;
            return m;
        }

        public static implicit operator double(AngleModel angleModel)
        {
            return angleModel.A;
        }

        public double A { get; set; }
    }

    [Serializable]
    public class SizeModel
    {
        public static implicit operator SizeModel(Vec vec)
        {
            SizeModel m = new SizeModel();
            m.W = vec.X;
            m.H = vec.Y;
            return m;
        }

        public static implicit operator Vec(SizeModel sizeModel)
        {
            return new Vec(sizeModel.W, sizeModel.H);
        }

        public double W { get; set; }
        public double H { get; set; }
    }

    [Serializable]
    public class PositionModel
    {
        public static implicit operator PositionModel(Pt point)
        {
            PositionModel m = new PositionModel();
            m.X = point.X;
            m.Y = point.Y;
            return m;
        }

        public static implicit operator Pt(PositionModel positionModel)
        {
            return new Pt(positionModel.X, positionModel.Y);
        }

        public double X { get; set; }
        public double Y { get; set; }
    }

    [Serializable]
    public class PointModel
    {
        public static implicit operator PointModel(Pt point)
        {
            PointModel m = new PointModel();
            m.X = point.X;
            m.Y = point.Y;
            return m;
        }

        public static implicit operator Pt(PointModel pointModel)
        {
            return new Pt(pointModel.X, pointModel.Y);
        }

        public double X { get; set; }
        public double Y { get; set; }
    }

    [Serializable]
    public class StylusPointModel
    {
        public static implicit operator StylusPointModel(StylusPoint point)
        {
            StylusPointModel m = new StylusPointModel();
            m.X = point.X;
            m.Y = point.Y;
            m.PressureFactor = point.PressureFactor;
            return m;
        }

        public static implicit operator StylusPoint(StylusPointModel stylusPointModel)
        {
            return new StylusPoint(stylusPointModel.X, stylusPointModel.Y, stylusPointModel.PressureFactor);
        }

        public double X { get; set; }
        public double Y { get; set; }
        public float PressureFactor { get; set; }

    }

    [Serializable]
    public class ColorModel
    {
        public static implicit operator ColorModel(Color color)
        {
            ColorModel m = new ColorModel();
            m.A = color.A;
            m.R = color.R;
            m.G = color.G;
            m.B = color.B;
            return m;
        }

        public static implicit operator Color(ColorModel m)
        {
            return Color.FromArgb(m.A, m.R, m.G, m.B);
        }

        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    [Serializable]
    public class TextLineModel
    {
        public string Text { get; set; }
        public int IndentLevel { get; set; }
        public bool Bullet { get; set; }
    }

    [Serializable]
    public class CellModel
    {
        public SizeModel Size { get; set; }
        public PositionModel Position { get; set; }
        public string Text { get; set; }
        public string HeaderDirection { get; set; }
        public string DisplayMode { get; set; }
        public List<Guid> AssociatedStroqs { get; set; }
        public List<LabelModel> ProvidedLabels { get; set; }
        public Expr RenderableExpr { get; set; }
        public bool IsRenderableExprNumeric { get; set; }
    }

    #endregion

    public class SerializationException : Exception
    {
        public SerializationException() { }
        public SerializationException(string message) : base(message) { }
    }

}
