using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.Inq.BobsCusps;
using Recognizer.NDollar;
using System.IO;
using BrownRecognitionCommon;
using PanoramicData.utils.inq;
using PanoramicData.model.view;
using PanoramicData.view.table;
using PanoramicData.view.vis;

namespace PanoramicData
{
    public class LassoGesture : ButtonGesture
    {
        InqScene _inqScene = null;
        public LassoGesture(Gesturizer gesturizer, InqScene inqScene)
            : base(gesturizer)
        {
            _inqScene = inqScene;
        }

        public override bool CommonTest(Stroq stroq, object device)
        {
            starPadSDK.AppLib.LassoCommand lc = new starPadSDK.AppLib.LassoCommand(_inqScene);
            var tt= lc.Test1(stroq, device);
            return tt;
        }
    }
        
    public class DownRightGesture : ButtonGesture
    {
        InqScene _inqScene = null;
        public DownRightGesture(Gesturizer gesturizer, InqScene inqScene)
            : base(gesturizer)
        {
            _inqScene = inqScene;
        }

        public override bool CommonTest(Stroq stroq, object device)
        {
            Cusps cusps = stroq.Cusps();
            Vec dir = (stroq[0] - stroq[-1]).Normal();

            if (cusps.Length == 3 &&
                cusps.Straightness(0, 1) < 0.15 && cusps.Straightness(1, 2) < 0.15 &&
                cusps.Distance > 500)
            {
                double angle = cusps.inSeg(1).Direction.UnsignedAngle(-cusps.outSeg(1).Direction);
                if (angle < (2 * Math.PI) / 3 && angle > Math.PI / 3)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class CreatePieChartGesturePart : ButtonGesturePart
    {
        InqScene _inqScene = null;
        CreatePieChartCallback _callback = null;
        public CreatePieChartGesturePart(InqScene inqScene, CreatePieChartCallback callback)
        {
            _inqScene = inqScene;
            _callback = callback;
        }
        public override bool Test(Stroq stroq, object device)
        {
            BrownShape brownShape = stroq.GetTag<BrownShape>(ShapeGesture.BROWN_SHAPE_ID);
            var intersectedStroqListener = new List<StroqListener>(_inqScene.GetIntersectedElements(stroq).Where((t) => t is StroqListener).Select((t) => t as StroqListener));
            var intersectedFilterHolder = new List<VisualizationContainerView>(_inqScene.GetIntersectedElements(stroq).Where((t) => t is VisualizationContainerView).Select((t) => t as VisualizationContainerView));
            if (brownShape != null && intersectedStroqListener.Count == 0 && intersectedFilterHolder.Count == 0 &&
                (brownShape.ShapeType == ShapeType.Circle || brownShape.ShapeType == ShapeType.Ellipse))
            {
                return true;
            }

            return false;
        }

        public override void Fire(Stroq[] strokes, object device)
        {
            _callback.CreatePieChartExecuteCallback(strokes[0].GetTag<BrownShape>(ShapeGesture.BROWN_SHAPE_ID), strokes[0]);
        }
        public override string Label { get { return "Create Pie Chart"; } }
    }

    public class CreateChartButtonGesturePart : ButtonGesturePart
    {
        InqScene _inqScene = null;
        CreateChartCallback _callback = null;
        FilterRendererType _type = FilterRendererType.Table;
        public CreateChartButtonGesturePart(InqScene inqScene, CreateChartCallback callback, FilterRendererType type)
        {
            _inqScene = inqScene;
            _callback = callback;
            _type = type;
        }
        public override bool Test(Stroq stroq, object device)
        {
            return true;
        }

        public override void Fire(Stroq[] strokes, object device)
        {
            _callback.CreateChartExecuteCallback(strokes[0], _type);
        }

        public override string Label
        {
            get
            {
                string label = "Create ";
                if (_type == FilterRendererType.Line)
                {
                    label += "Line ";
                } 
                else if (_type == FilterRendererType.Histogram)
                {
                    label += "Bar ";
                }
                else if (_type == FilterRendererType.Plot)
                {
                    label += "Scatter ";
                }
                else if (_type == FilterRendererType.Pie)
                {
                    label += "Pie ";
                }
                label += "Chart";
                return label;
            }
        }
    }

    public class StraighLineGesture : ButtonGesture
    {
        InqScene _inqScene = null;

        public StraighLineGesture(Gesturizer gesturizer, InqScene inqScene)
            : base(gesturizer)
        {
            _inqScene = inqScene;
        }

        public override bool CommonTest(Stroq stroq, object device)
        {
            Cusps cusps = stroq.Cusps();
            if (cusps.Length == 2 && cusps.Straightness(0, 1) < 0.15)// && dx < 0.07 && cusps.Distance > 70)
            {
                return true;
            }
            return false;
        }
    }
    
    public class ShapeGesture : ButtonGesture
    {
        public static Guid BROWN_SHAPE_ID = Guid.NewGuid();
        InqScene _inqScene = null;

        public ShapeGesture(Gesturizer gesturizer, InqScene inqScene)
            : base(gesturizer)
        {
            _inqScene = inqScene;
        }

        public override bool CommonTest(Stroq stroq, object device)
        {
            var intersectedElements = _inqScene.GetIntersectedElements(stroq);

            if (intersectedElements.Count == 0 && stroq.GetBounds().Area > 6400)
            {
                BrownShape brownShape = BrownRecognitionAPI.API.RecognizeBrownShape(new BrownInputStroke(stroq.Select((pt) => new Point(pt.X, pt.Y)).ToArray(), stroq));
                if (brownShape != null && 
                    brownShape.ShapeType != ShapeType.StraightLine && 
                    brownShape.ShapeType != ShapeType.Polyline && 
                    brownShape.ShapeType != ShapeType.Scribble)
                {
                    stroq.SetTag<BrownShape>(BROWN_SHAPE_ID, brownShape);
                    return true;
                }
            }
            return false;
        }
    }


    public class CreateVisTableButtonGesturePart : ButtonGesturePart
    {
        InqScene _inqScene = null;
        CreateVisTableCallback _callback = null;
        public CreateVisTableButtonGesturePart(InqScene inqScene, CreateVisTableCallback callback)
        {
            _inqScene = inqScene;
            _callback = callback;
        }
        public override bool Test(Stroq stroq, object device)
        {
            var intersectedElements = _inqScene.GetIntersectedElements(stroq);

            if (intersectedElements.Count == 0)
            {
                BrownShape brownShape = stroq.GetTag<BrownShape>(ShapeGesture.BROWN_SHAPE_ID);
                if (brownShape != null &&
                    (brownShape.ShapeType == ShapeType.Rect ||
                    brownShape.ShapeType == ShapeType.Square ||
                    brownShape.ShapeType == ShapeType.RoundedRect))
                {
                    return true;
                }
            }
            return false;
        }

        public override void Fire(Stroq[] strokes, object device)
        {
            _callback.CreateVisTableExecuteCallback(strokes[0]);
        }

        public override string Label { get { return "Create Table"; } }
    }

    public class CreateSliderButtonGesturePart : ButtonGesturePart
    {
        InqScene _inqScene = null;
        CreateSliderCallback _callback = null;
        public CreateSliderButtonGesturePart(InqScene inqScene, CreateSliderCallback callback)
        {
            _inqScene = inqScene;
            _callback = callback;
        }
        public override bool Test(Stroq stroq, object device)
        {
            var intersectedElements = _inqScene.GetIntersectedElements(stroq);

            if (intersectedElements.Count == 0)
            {
                BrownShape brownShape = stroq.GetTag<BrownShape>(ShapeGesture.BROWN_SHAPE_ID);
                if (brownShape != null &&
                    (brownShape.ShapeType == ShapeType.Rect ||
                    brownShape.ShapeType == ShapeType.Square ||
                    brownShape.ShapeType == ShapeType.RoundedRect))
                {
                    return true;
                }
            }
            return false;
        }

        public override void Fire(Stroq[] strokes, object device)
        {
            _callback.CreateSliderExecuteCallback(strokes[0]);
        }

        public override string Label { get { return "Create Slider"; } }
    }

    public class ShortcutGesture : OneStrokeGesture
    {
        public static Guid GESTURE_NAME_ID = Guid.NewGuid();

        InqScene _inqScene = null;
        ShortcutCallback _callback = null;

        public ShortcutGesture(InqScene inqScene, ShortcutCallback callback)
        {
            _inqScene = inqScene;
            _callback = callback;
        }

        public override bool Test(Stroq stroq, object device)
        { 
            // check if start of the gesture is over an element
            List<AttributeView> elements = _inqScene.GetIntersectedTypesRecursive<AttributeView>(stroq).Select((t) => t as AttributeView).ToList();
            if (elements.Count > 0)
            {
                // recognize the stroq
                InqAnalyzer inqAnalyzer = new InqAnalyzer();
                inqAnalyzer.AddStroke(stroq);
                inqAnalyzer.Analyze();
                string recog = inqAnalyzer.GetRecognizedString().ToLower();
                Console.WriteLine(" >>> SimpleGridViewColumnHeader Gesture : " + recog);

                if (recog.Equals("a") ||
                    recog.Equals("s") ||
                    recog.Equals("c") || 
                    recog.Equals("b") ||
                    recog.Equals("g") )
                {
                    stroq.SetTag<object[]>(GESTURE_NAME_ID, new object[] { elements, recog });
                    return true;
                }
            }
            return false;
        }

        public override void Fire(Stroq[] strokes, object device)
        {
            object[] passed = strokes[0].GetTag<object[]>(ShortcutGesture.GESTURE_NAME_ID);

            if (passed != null)
            {
                _callback.ShortcutExecuteCallback((List<AttributeView>)passed[0], (string)passed[1]);
            }
        }
    }

    public class ConnectGesture : OneStrokeGesture
    {
        public static Guid GESTURE_NAME_ID = Guid.NewGuid();

        InqScene _inqScene = null;
        ConnectCallback _callback = null;
        Type[] _typesFrom = null;
        Type[] _typesTo = null;
        bool _allowPartial = false;

        public ConnectGesture(InqScene inqScene, Type[] typesFrom, Type[] typesTo, bool allowPartial, ConnectCallback callback)
        {
            _typesFrom = typesFrom;
            _typesTo = typesTo;
            _inqScene = inqScene;
            _callback = callback;
            _allowPartial = allowPartial;
        }

        public override bool Test(Stroq stroq, object device)
        {
            // check if start of the gesture is over an element
            var startElements = new List<FrameworkElement>(_inqScene.GetIntersectedElements(new Rct(stroq[0], new Vec(1, 1))).Where((t) => _typesFrom.Contains(t.GetType())).Select((t) => t as FrameworkElement));
            
            // check if end of the gesture is over an element
            var endElements = new List<FrameworkElement>(_inqScene.GetIntersectedElements(new Rct(stroq[-1], new Vec(1, 1))).Where((t) => _typesTo.Contains(t.GetType())).Select((t) => t as FrameworkElement));

            if (stroq.Cusps().Length == 1 &&  (startElements.Count > 0 && endElements.Count > 0) || (_allowPartial && (startElements.Count > 0 || endElements.Count > 0)))
            {
                if ((startElements.Count > 0 && endElements.Count > 0) && startElements[0] == endElements[0])
                {
                    return false;
                }
                stroq.SetTag<object[]>(GESTURE_NAME_ID, new object[] { startElements, endElements });
                return true;
            }
            return false;
        }

        public override void Fire(Stroq[] strokes, object device)
        {
            object[] passed = strokes[0].GetTag<object[]>(ConnectGesture.GESTURE_NAME_ID);

            if (passed != null)
            {
                _callback.ConnectExecuteCallback((List<FrameworkElement>)passed[0], (List<FrameworkElement>)passed[1], strokes[0]);
            }
        }
    }

    public class CombineGesture : OneStrokeGesture
    {
        public static Guid GESTURE_NAME_ID = Guid.NewGuid();

        InqScene _inqScene = null;
        CombineCallback _callback = null;
        Type _type = null;

        public CombineGesture(InqScene inqScene, Type type, CombineCallback callback)
        {
            _type = type;
            _inqScene = inqScene;
            _callback = callback;
        }

        public override bool Test(Stroq stroq, object device)
        {
            if (stroq.IsLasso())
            {
                // check if start of the gesture is over an element
                var elements = new List<FrameworkElement>(_inqScene.GetIntersectedElements(stroq).Where((t) => t.GetType() == _type).Select((t) => t as FrameworkElement));
            
                if (elements.Count == 2)
                {
                    var startElements = new List<FrameworkElement>(_inqScene.GetIntersectedElements(new Rct(stroq[0], new Vec(1, 1))).Where((t) => t.GetType() == _type).Select((t) => t as FrameworkElement));
                    var endElements = new List<FrameworkElement>(_inqScene.GetIntersectedElements(new Rct(stroq[-1], new Vec(1, 1))).Where((t) => t.GetType() == _type).Select((t) => t as FrameworkElement));
                    if (startElements.Count > 0 && endElements.Count > 0 &&
                        startElements[0] == endElements[0])
                    {
                        stroq.SetTag<object[]>(GESTURE_NAME_ID, new object[] { startElements[0], elements.Where(ee => ee != startElements[0]).First() });
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Fire(Stroq[] strokes, object device)
        {
            object[] passed = strokes[0].GetTag<object[]>(CombineGesture.GESTURE_NAME_ID);

            if (passed != null)
            {
                _callback.CombineExecuteCallback((FrameworkElement)passed[0], (FrameworkElement)passed[1]);
            }
        }
    }

    public interface CreateChartCallback
    {
        void CreateChartExecuteCallback(Stroq s, FilterRendererType type);
    }
    
    public interface ConnectCallback
    {
        void ConnectExecuteCallback(List<FrameworkElement> startElements, List<FrameworkElement> endElements, Stroq stroq);
    }

    public interface ShortcutCallback
    {
        void ShortcutExecuteCallback(List<AttributeView> startElements, string recog);
    }

    public interface CombineCallback
    {
        void CombineExecuteCallback(FrameworkElement e1, FrameworkElement e2);
    }

    public interface CreateTableCallback
    {
        void CreateTableExecuteCallback(SelectionObj res);
    }

    public interface CreateVisTableCallback
    {
        void CreateVisTableExecuteCallback(Stroq s);
    }

    public interface CreateSliderCallback
    {
        void CreateSliderExecuteCallback(Stroq s);
    }
    
    public interface CreatePieChartCallback
    {
        void CreatePieChartExecuteCallback(BrownShape brownShape, Stroq s);
    }


    public class ScribbleGesture : OneStrokeGesture
    {
        InqScene _inqScene = null;
        List<Type> _typesToIgnore = null;
        public ScribbleGesture(InqScene inqScene, List<Type> typesToIgnore)
        {
            _inqScene = inqScene;
            _typesToIgnore = typesToIgnore;
        }

        public override bool Test(Stroq stroq, object device)
        {
            starPadSDK.AppLib.ScribbleTapCommand stc = new starPadSDK.AppLib.ScribbleTapCommand(_inqScene, _typesToIgnore);
            return stc.Test1(stroq, device);
        }

        public override void Fire(Stroq[] strokes, object device)
        {
            starPadSDK.AppLib.ScribbleTapCommand stc = new starPadSDK.AppLib.ScribbleTapCommand(_inqScene, _typesToIgnore);
            SelectionObj deletions = stc.GetResult(strokes, device);

            foreach (var elem in deletions.Elements)
            {
                if (elem is FilterModelAttachment)
                {
                    (elem as FilterModelAttachment).CheckScribbleDelete(strokes[0]);
                }
                else if (elem is System.Windows.Controls.ContentControl)
                {
                    _inqScene.Rem(elem);
                }
            }

            foreach (var stroq in deletions.Strokes)
            {
                List<Label> labels = InkTableSegmenter.GetAllLabels(InkTableContentCollection.GetInkTableContentCollection(new StroqCollection(deletions.Strokes)));
                foreach (var label in labels)
                {
                    label.FireLabelDeleted();
                }
            }

            _inqScene.Rem(new StroqCollection(deletions.Strokes));

            /*foreach (var elem in deletions.Elements)
            {
                if (MarkCanvas.GetMarkCanvas(elem) != null)
                {
                    if (MarkCanvas.GetMarkCanvas(elem).RemoveChild(elem))
                        _inqScene.Rem(MarkCanvas.GetMarkCanvas(elem));
                }
            }*/
        }
    }
}
