using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using starPadSDK.Inq;
using starPadSDK.Inq.BobsCusps;
using System.Windows.Ink;
using ShapeRecognizer;
using TemplateRecognizer;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using starPadSDK.Geom;
using SharpShapes.Properties;
using BrownRecognitionCommon;
using System.Threading;
using InputFramework.WPFDevices;
using InputFramework.DeviceDriver;

namespace SharpShapes
{
    /// <summary>
    /// Interaction logic for ShapeCanvas.xaml
    /// </summary>
    public partial class ShapeCanvas : UserControl
    {
        public BrownTemplate LastCleanedUpTemplate = null;
        public Mode RecognitionMode { get; set; }
        public List<BrownTemplate> CurrentRecognizedTemplates = new List<BrownTemplate>();
        
        Guid BrownShapeGuid = Guid.NewGuid();
        Point      _startDrag  = new Point();
      
        public ShapeCanvas()
        {
            InitializeComponent();
            RecognitionMode = Mode.ShapeRecognition;

            InqCanvas.StroqCollected += new InqCanvas.StroqCollectedEventHandler(InqCanvas_StroqCollected);

            // First we have to create a WPFDeviceManager and pass in the reference of the DeviceManager(which is a singelton).
            // This registers the WPFDeviceManager as listener of the DeviceManager meaning any input events from a device driver are received by
            // the WPFDeviceManager to be sent into the WPF event system.

            WPFDeviceManager WPFDeviceManager = new WPFDeviceManager(DeviceManager.Instance);

            // If we want to use input from one of the devices in our application we have to create an instance of the
            // corresponding device driver and register it with the Device Manager.
            // In this example we will be using the mouse and a TUIO device driver but we can specify as many as we would like to use.

            //DeviceManager.Instance.AddPointDeviceDriver(new Windows8SlateDeviceDriver(layoutRoot, WPFDeviceManager, aPage));
            DeviceManager.Instance.AddPointDeviceDriver(new SharpWhiteBoardDeviceDriver(this, WPFDeviceManager, InqCanvas, true));
        }
        
        public void Add(FrameworkElement r) {
            InqCanvas.Children.Add(r);

            // no handler for line types
            if (r is BrownShapeRenderer)
            {
                BrownShape bs = (r as BrownShapeRenderer).BrownShape;
                if (bs.ShapeType == ShapeType.StraightLine || bs.ShapeType == ShapeType.Polyline)
                {
                    return;
                }
            }
            r.MouseLeftButtonDown += new MouseButtonEventHandler(rect_MouseLeftButtonDown);
        }
        public void Remove(FrameworkElement r) { InqCanvas.Children.Remove(r); }

        public void RecognizeTemplate()
        {
            // get all strokes
            List<BrownInputStroke> brownInputStrokes = new List<BrownInputStroke>();
            foreach (Stroq s in InqCanvas.Stroqs)
            {
                brownInputStrokes.Add(new BrownInputStroke(s.Select((pt) => new Point(pt.X, pt.Y)).ToArray(), s));
            }

            // convert all BrownShapeRenders to strokes
            // add all BrownShapeRenders to the shapes list
            foreach (var c in InqCanvas.Children)
            {
                if (c is BrownShapeRenderer)
                {
                    BrownShape oldBs = (c as BrownShapeRenderer).BrownShape;
                    brownInputStrokes.Add(new BrownInputStroke(oldBs.ShapePoints, c));
                }
            }

            List<BrownTemplate> recognizedTemplates = BrownRecognitionAPI.API.RecognizeBrownTemplate(brownInputStrokes);
            CurrentRecognizedTemplates = recognizedTemplates;

            updateTemplateRecognitionUI(recognizedTemplates);
        }

        public void CleanUpTemplate(String templateString)
        {
            BrownTemplate clean = null;
            foreach (var template in CurrentRecognizedTemplates)
            {
                if (template.TemplateType.ToString().Equals(templateString))
                {
                    List<BrownShape> elements = BrownRecognitionAPI.API.GetCleanTemplateShapes(template);

                    // add all the cleaned up geometry. 
                    int counter = 0;
                    foreach (BrownShape bs in elements)
                    {
                        BrownShapeRenderer bsr = new BrownShapeRenderer(bs, template);
                        InqCanvas.Children.Add(bsr);

                        counter++;

                        // remove the stroqs that were part of this brownShape
                        foreach (BrownInputStroke bis in bs.BrownInputStrokes)
                        {
                            if (bis.Data is Stroq)
                            {
                                InqCanvas.Stroqs.Remove(bis.Data as Stroq);
                            }
                            else if (bis.Data is BrownShapeRenderer)
                            {
                                InqCanvas.Children.Remove(bis.Data as BrownShapeRenderer);
                            }
                        }
                    }

                    LastCleanedUpTemplate = clean = template;
                    break;
                }
            }
            if (clean != null)
            {
                CurrentRecognizedTemplates.Remove(clean);
            }
        }

        public void Undo()
        {
            List<BrownShapeRenderer> removeList = new List<BrownShapeRenderer>();
            foreach (var c in InqCanvas.Children)
            {
                if (c is BrownShapeRenderer && (c as BrownShapeRenderer).BrownTemplate == LastCleanedUpTemplate)
                {
                    removeList.Add(c as BrownShapeRenderer);
                }
            }

            foreach (var c in removeList)
            {
                InqCanvas.Children.Remove(c);
            }


            foreach (BrownShape bs in LastCleanedUpTemplate.BrownShapes)
            {
                foreach (BrownInputStroke bis in bs.BrownInputStrokes)
                {
                    if (bis.Data is Stroq)
                    {
                        InqCanvas.Stroqs.Add(bis.Data as Stroq);
                    }
                    else if (bis.Data is BrownShapeRenderer)
                    {
                        if (!InqCanvas.Children.Contains(bis.Data as BrownShapeRenderer))
                        {
                            // hack to get the z ordering right. 
                            if ((bis.Data as BrownShapeRenderer).BrownShape.ShapeType == ShapeType.StraightLine)
                            {
                                InqCanvas.Children.Add(bis.Data as BrownShapeRenderer);
                            }
                            else
                            {
                                InqCanvas.Children.Insert(0, bis.Data as BrownShapeRenderer);
                            }
                        }
                    }
                }
            }

            CurrentRecognizedTemplates.Add(LastCleanedUpTemplate);
            updateTemplateRecognitionUI(CurrentRecognizedTemplates);
        }

        public void StroqAdded(Stroq stroq)
        {
            if (stroq.Count < 2)
            {
                InqCanvas.Stroqs.Remove(stroq);
                return;
            }

            LastCleanedUpTemplate = null;

            // special treatment for scribble 
            var rs = BrownRecognitionAPI.API.RecognizeBrownShape(new BrownInputStroke(stroq.Select((pt) => new Point(pt.X, pt.Y)).ToArray(), stroq));
            if (rs != null && rs.ShapeType == ShapeType.Scribble)
            {
                Recognizer.SelectionObj res;
                if (Recognizer.RecognizeScribbleDelete(stroq, InqCanvas, out res))
                {
                    foreach (var s in res.Stroqs)
                        InqCanvas.Stroqs.Remove(s);
                    foreach (var m in res.Elements)
                        InqCanvas.Children.Remove(m);

                    if (RecognitionMode == Mode.TemplateRecognitionFeedback)
                        recognizeTemplateFeedbackScribble(res.Stroqs);
                    else if (RecognitionMode == Mode.TemplateRecognitionDirect)
                        updateTemplateRecognitionUI(new List<BrownTemplate>());
                }
            }
            else switch (RecognitionMode)
                {
                    case Mode.ShapeRecognition: if (!recognizeBasicShape(stroq)) InqCanvas.Stroqs.Remove(stroq); break;
                    case Mode.TemplateRecognitionFeedback: recognizeTemplateFeedback(stroq); break;
                    case Mode.TemplateRecognitionDirect: updateTemplateRecognitionUI(new List<BrownTemplate>()); break;
                }
        }

        void InqCanvas_StroqCollected(object sender, InqCanvas.StroqCollectedEventArgs e)  { StroqAdded(e.Stroq); }

        void recognizeTemplateFeedbackScribble(List<Stroq> deletedStroqs)
        {
            List<Stroq> effectedStroqs = new List<Stroq>();

            // remove the BrownShapeGuid from effectedStoqs
            foreach (var deleted in deletedStroqs)
            {
                if (deleted.Property.Exists(BrownShapeGuid))
                {
                    BrownShape deletedBrownShape = (BrownShape)deleted.Property[BrownShapeGuid];

                    foreach (var s in InqCanvas.Stroqs)
                    {
                        if (s.Property.Exists(BrownShapeGuid))
                        {
                            BrownShape bs = (BrownShape)s.Property[BrownShapeGuid];

                            if (deletedBrownShape == bs)
                            {
                                s.Property.Remove(BrownShapeGuid);
                                effectedStroqs.Add(s);
                            }
                        }
                    }
                }
            }

            // reregognize effectedStroqs
            foreach (Stroq s in effectedStroqs)
            {
                recognizeTemplateFeedbackProcessStroq(s);
            }

            List<BrownShape> brownShapes = recognizeTemplateFeedbackCollectBrownShapes();

            // do the template recognition
            List<BrownTemplate> recognizedTemplates = BrownRecognitionAPI.API.RecognizeBrownTemplate(brownShapes);
            CurrentRecognizedTemplates = recognizedTemplates;

            updateTemplateRecognitionUI(recognizedTemplates);
        }

        void recognizeTemplateFeedback(Stroq stroq)
        {
            // do shape recognition for the new stroq
            if (stroq != null)
            {
                recognizeTemplateFeedbackProcessStroq(stroq);
            }

            List<BrownShape> brownShapes = recognizeTemplateFeedbackCollectBrownShapes();

            // do the template recognition
            List<BrownTemplate> recognizedTemplates = BrownRecognitionAPI.API.RecognizeBrownTemplate(brownShapes);
            CurrentRecognizedTemplates = recognizedTemplates;

            updateTemplateRecognitionUI(recognizedTemplates);
        }

        List<BrownShape> recognizeTemplateFeedbackCollectBrownShapes()
        {
            List<BrownShape> brownShapes = new List<BrownShape>();

            // add all strokes to the shapes list
            foreach (var s in InqCanvas.Stroqs)
            {
                if (s.Property.Exists(BrownShapeGuid))
                {
                    BrownShape bs = (BrownShape)s.Property[BrownShapeGuid];
                    if (!brownShapes.Contains(bs))
                    {
                        brownShapes.Add(bs);
                    }
                }
            }

            // add all BrownShapeRenders to the shapes list
            foreach (var c in InqCanvas.Children)
            {
                if (c is BrownShapeRenderer)
                {
                    BrownShape oldBs = (c as BrownShapeRenderer).BrownShape;
                    BrownShape newBs = new BrownShape(oldBs.ShapeType, oldBs.ShapePoints,
                        new BrownInputStroke(oldBs.ShapePoints, c));
                    brownShapes.Add(newBs);
                }
            }
            return brownShapes;
        }

        void recognizeTemplateFeedbackProcessStroq(Stroq stroq)
        {
            BrownShape brownShape = BrownRecognitionAPI.API.RecognizeBrownShape(new BrownInputStroke(stroq.Select((pt) => new Point(pt.X, pt.Y)).ToArray(), stroq));
            if (brownShape != null)
            {
                stroq.Property[BrownShapeGuid] = brownShape;

                if (brownShape.ShapeType == ShapeType.StraightLine || brownShape.ShapeType == ShapeType.Polyline)
                {
                    List<BrownShape> lineShapes = new List<BrownShape>();
                    foreach (var s in InqCanvas.Stroqs)
                    {
                        if (s.Property.Exists(BrownShapeGuid))
                        {
                            BrownShape bs = (BrownShape)s.Property[BrownShapeGuid];

                            if (bs.ShapeType == ShapeType.StraightLine || bs.ShapeType == ShapeType.Polyline)
                                lineShapes.Add(bs);
                        }
                    }
                    BrownShape newBrownShape = BrownRecognitionAPI.API.RecognizeBrownShape(lineShapes);
                    if (newBrownShape != null && (brownShape.ShapeType != ShapeType.None))
                    {
                        foreach (BrownInputStroke bis in newBrownShape.BrownInputStrokes)
                        {
                            (bis.Data as Stroq).Property[BrownShapeGuid] = newBrownShape;
                        }
                    }
                }
            }
        }

        bool recognizeBasicShape(Stroq stroq)
        {
            BrownShape brownShape = BrownRecognitionAPI.API.RecognizeBrownShape(new BrownInputStroke(stroq.Select((pt) => new Point(pt.X, pt.Y)).ToArray(), stroq));
            if (brownShape != null)
            {
                BrownShapeRenderer bsr = new BrownShapeRenderer(brownShape);
                Add(bsr);
                foreach (BrownInputStroke bis in brownShape.BrownInputStrokes)
                {
                    InqCanvas.Stroqs.Remove(bis.Data as Stroq);
                }


                if (brownShape.ShapeType == ShapeType.StraightLine || brownShape.ShapeType == ShapeType.Polyline)
                {
                    List<BrownShape> lineShapes = new List<BrownShape>();
                    foreach (var c in InqCanvas.Children)
                    {
                        if (c is BrownShapeRenderer)
                        {
                            BrownShape bs = ((BrownShapeRenderer)c).BrownShape;

                            if (bs.ShapeType == ShapeType.StraightLine || bs.ShapeType == ShapeType.Polyline)
                                lineShapes.Add(bs);
                        }
                    }
                    BrownShape newBrownShape = BrownRecognitionAPI.API.RecognizeBrownShape(lineShapes);
                    if (newBrownShape != null)
                    {
                        BrownShapeRenderer newBsr = new BrownShapeRenderer(newBrownShape);
                        Add(newBsr);

                        List<BrownShapeRenderer> removeList = new List<BrownShapeRenderer>();
                        foreach (var c in InqCanvas.Children)
                        {
                            if (c is BrownShapeRenderer && lineShapes.Contains((c as BrownShapeRenderer).BrownShape))
                            {
                                removeList.Add(c as BrownShapeRenderer);
                            }
                        }
                        foreach (var r in removeList) 
                        {
                            InqCanvas.Children.Remove(r);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        void colorizeStroqByShapeType(Stroq stroq, ShapeType shapeType)
        {
            if (shapeType == ShapeType.Ellipse)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Red;
            else if (shapeType == ShapeType.Circle)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.DeepPink;
            else if (shapeType == ShapeType.Square)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Blue;
            else if (shapeType == ShapeType.Diamond)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Yellow;
            else if (shapeType == ShapeType.Rect)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.DarkBlue;
            else if (shapeType == ShapeType.RoundedRect)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Beige;
            else if (shapeType == ShapeType.Parallelogram)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Cyan;
            else if (shapeType == ShapeType.Trapezoid)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Purple;
            else if (shapeType == ShapeType.Triangle)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.LightGreen;
            else if (shapeType == ShapeType.IsoscelesTriangle)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Green;
            else if (shapeType == ShapeType.RightTriangle)
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Brown;
            else
            {
                stroq.BackingStroke.DrawingAttributes.Color = Colors.Black;
            }
        }

        void rect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) 
        {
            e.Handled = true;
            Mouse.Capture(sender as FrameworkElement);
            _startDrag = e.GetPosition(InqCanvas);
            (sender as FrameworkElement).MouseMove += new MouseEventHandler(rect_MouseMove);
            (sender as FrameworkElement).MouseUp += new MouseButtonEventHandler(rect_MouseUp);
            //InqCanvas.SetInkEnabled(Mouse.PrimaryDevice, false);
        }
        void rect_MouseUp(object sender, MouseButtonEventArgs e) 
        {
            e.Handled = true;
            (sender as FrameworkElement).MouseMove -= new MouseEventHandler(rect_MouseMove);
            (sender as FrameworkElement).MouseUp -= new MouseButtonEventHandler(rect_MouseUp);
            Mouse.Capture(null);
            //InqCanvas.SetInkEnabled(Mouse.PrimaryDevice, true);
        }
        void rect_MouseMove(object sender, MouseEventArgs e) 
        {
            var rect = sender as FrameworkElement;
            var curDrag = e.GetPosition(InqCanvas);
            Vector dragBy = curDrag - _startDrag;
            Rect bounds = new Rect(rect.RenderTransform.Transform(new Point()), new Size(rect.Width, rect.Height));

            foreach (var s in InqCanvas.Stroqs)
                if (bounds.Contains(s.GetBounds()))
                    s.Move(dragBy);

            if (rect.RenderTransform is TranslateTransform)
            {
                (rect.RenderTransform as TranslateTransform).X += dragBy.X;
                (rect.RenderTransform as TranslateTransform).Y += dragBy.Y;
            }
            _startDrag = curDrag;
            e.Handled = true;
        }
        
        void updateTemplateRecognitionUI(List<BrownTemplate> recognizedTemplates)
        {
            LastCleanedUpTemplate = null;
            if (recognizedTemplates.Count == 0)
            {
                CurrentRecognizedTemplates.Clear();
            }

            // Reset all strokes or color them for template recognition feedback mode
            foreach (Stroq s in InqCanvas.Stroqs)
            {
                s.BackingStroke.DrawingAttributes.Width = 2;
                s.BackingStroke.DrawingAttributes.Height = 2;
                ShapeType shapeType = ShapeType.None;
                if (s.Property.Exists(BrownShapeGuid))
                {
                    shapeType = ((BrownShape)s.Property[BrownShapeGuid]).ShapeType;
                }
                colorizeStroqByShapeType(s, shapeType);
            }

            // hide all clean up buttons first
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            mw.btnCleanUp.IsEnabled = recognizedTemplates.Count > 0;

            // Color strokes according to recognized templates. 
            foreach (BrownTemplate bt in recognizedTemplates)
            {
                foreach (BrownShape bs in bt.BrownShapes)
                {
                    foreach (BrownInputStroke bis in bs.BrownInputStrokes) 
                    {
                        if (bis.Data is Stroq)
                        {
                            colorizeStroqByShapeType(bis.Data as Stroq, bs.ShapeType);
                            (bis.Data as Stroq).BackingStroke.DrawingAttributes.Width = 4;
                            (bis.Data as Stroq).BackingStroke.DrawingAttributes.Height = 4;
                        }
                    }
                }
            }

            SharpShapesCommands.UpdateAllCanExecute();
        }

        void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SharpShapesCommands.UpdateAllCanExecute();
        }
    }

    public enum Mode { ShapeRecognition, TemplateRecognitionFeedback, TemplateRecognitionDirect };
}
