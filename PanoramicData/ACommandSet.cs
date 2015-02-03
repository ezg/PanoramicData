using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using BrownRecognitionCommon;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System.Windows.Media;
using starPadSDK.AppLib;
using PanoramicDataDBConnector;
using PanoramicDataModel;
using PanoramicData.model.view;
using PanoramicData.view.other;
using PanoramicData.view.table;
using PanoramicData.view.vis;
using PanoramicData.utils;

namespace PanoramicData
{
    // This creates the set of gestures to recognize on this InqScene.  The gestures are broken down into right-button and 
    // stylus-button gesture sets.
    // The MathCommand and CurveCommand gestures activate special-purpose editing objects which want most other
    // gestures be deactivated to avoid conflicts.  So when one of these commands is recognized, InitGestures will get
    // called and it will install an appropriate set of gestures depending on which editing modes have been (de)activated.
    public class ACommandSet : CommandSet, CreateChartCallback,
        ConnectCallback, CreateVisTableCallback, CreateSliderCallback, CreatePieChartCallback,
        ShortcutCallback
    {

        /// <summary>
        ///  this example handler is called when a Stroq has been drawn that the Gesture recognizer has
        ///  definitively determined is not a gesture.  By returning false, this method indicates that it has not
        ///  handled the non-gesture Stroq, and the underlying Stroq processing machinery will add the Stroq
        ///  to the Scene. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        virtual protected bool notAGestureHandler(object sender, object d, Stroq s) { return false; }
        /// <summary>
        ///  this method is called automatically to initialize the active Gestures when the CommandSet is created.
        ///  It is also called whenever a Gesture Command wants to change the active gestures, such as when activating
        ///  the Math or Curve editors.
        /// </summary>
        /// 
        APage _aPage = null;

        protected override void InitGestures()
        {
            base.InitGestures();  // cleans out all current Gestures
        }
        protected override void InitRightGestures()
        {
            base.InitRightGestures();

            // Scribble delete gesture
            List<Type> t = new List<Type>();
            //t.Add(typeof(InkTable));
            OneStrokeGesture g = null;
            g = new ConnectGesture(_can, 
                new Type[] { typeof(VisualizationContainerView) },
                new Type[] { typeof(FilterModelAttachment) }, false,
                this);
            _gest.Add(g);

            g = new ScribbleGesture(_can, t);
            _gest.Add(g);

            g = new ShortcutGesture(_can, this);
            _gest.Add(g);

            ButtonGesture bg = new LassoGesture(_gest, _can);
           
            bg = new DownRightGesture(_gest, _can);
            bg.AddButtonGestureParts(new CreateChartButtonGesturePart(_can, this, FilterRendererType.Histogram));
            bg.AddButtonGestureParts(new CreateChartButtonGesturePart(_can, this, FilterRendererType.Plot));
            bg.AddButtonGestureParts(new CreateChartButtonGesturePart(_can, this, FilterRendererType.Line));
            _gest.Add(bg);

            bg = new ShapeGesture(_gest, _can);
            bg.AddButtonGestureParts(new CreatePieChartGesturePart(_can, this));
            bg.AddButtonGestureParts(new CreateVisTableButtonGesturePart(_can, this));
            _gest.Add(bg);

            g = new ConnectGesture(_can,
                new Type[] { typeof(VisualizationContainerView), typeof(FilterModelAttachment) },
                new Type[] { typeof(VisualizationContainerView), typeof(FilterModelAttachment) }, true,
                this);
            _gest.Add(g);

        }

        public ACommandSet(APage scene)
            : base(scene)
        {
            _aPage = scene;
            NonGestureEvent += new Gesturizer.StrokeUnrecognizedHandler(notAGestureHandler);
        }

        public void CreateChartExecuteCallback(Stroq s, FilterRendererType type)
        {
            /*Rct bounds = s.GetBounds();
            InkChart inkChart = new InkChart(_can);
            inkChart.RenderTransform = new TranslateTransform(bounds.TopLeft.X, bounds.TopLeft.Y);
            _can.AddNoUndo(inkChart);*/

            s.BackingStroke.DrawingAttributes.Color = Color.FromArgb(0x88, 0x00, 0x8D, 0xff);
            _can.AddNoUndo(s);

            Rct bounds = s.GetBounds();

            AxisFeedback axis = new AxisFeedback(_can, type);
            axis.RenderTransform = new TranslateTransform(bounds.TopLeft.X, bounds.TopLeft.Y);
            axis.Width = bounds.Width;
            axis.Height = bounds.Height;
            s.Move(new Vec(-bounds.TopLeft.X, -bounds.TopLeft.Y));
            axis.AddStroq(s);

            _can.AddNoUndo(axis);

            _can.Rem(s);
        }
        
        public void ConnectExecuteCallback(List<FrameworkElement> startElements, List<FrameworkElement> endElements, Stroq stroq)
        {
            FilterHolderViewModel startModel = null;
            FilterHolderViewModel endModel = null;
            FilteringType filteringType = FilteringType.Filter;

            // partial connect
            if (startElements.Count == 0 || endElements.Count == 0)
            {
                if (startElements.Count == 0 && endElements[0] is VisualizationContainerView)
                {
                    //endModel = ((VisualizationContainerView)endElements[0]).FilterHolderViewModel;
                    VisualizationContainerView filter = new VisualizationContainerView();
                    FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateCopy(endModel, true, false);
                    filterHolderViewModel.FilterRendererType = FilterRendererType.Table;
                    filterHolderViewModel.Center = new Point();
                    //filter.FilterHolderViewModel = filterHolderViewModel;
                    filter.InitPostionAndDimension(new Pt(stroq[0].X - VisualizationContainerView.WIDTH / 2.0, stroq[0].Y - VisualizationContainerView.HEIGHT / 2.0), new Vector2(VisualizationContainerView.WIDTH, VisualizationContainerView.HEIGHT));
                    endModel.AddIncomingFilter(filterHolderViewModel, FilteringType.Filter, true);
                }
                else if (endElements.Count == 0 && startElements[0] is VisualizationContainerView)
                {
                   // startModel = ((VisualizationContainerView)startElements[0]).FilterHolderViewModel;
                    VisualizationContainerView filter = new VisualizationContainerView();
                    FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateCopy(startModel, true, false);
                    filterHolderViewModel.FilterRendererType = FilterRendererType.Table;
                    filterHolderViewModel.Center = new Point();
                    //filter.FilterHolderViewModel = filterHolderViewModel;
                    filter.InitPostionAndDimension(new Pt(stroq[-1].X - VisualizationContainerView.WIDTH / 2.0, stroq[-1].Y - VisualizationContainerView.HEIGHT / 2.0), new Vector2(VisualizationContainerView.WIDTH, VisualizationContainerView.HEIGHT));
                    filterHolderViewModel.AddIncomingFilter(startModel, FilteringType.Filter, true);
                }
            }

            else if  (startElements.Count > 0 && endElements.Count > 0)
            {
                if (startElements[0] is VisualizationContainerView)
                {
                    //startModel = ((VisualizationContainerView) startElements[0]).FilterHolderViewModel;
                }

                if (endElements[0] is VisualizationContainerView)
                {
                    //endModel = ((VisualizationContainerView) endElements[0]).FilterHolderViewModel;
                }
                else if (endElements[0] is FilterModelAttachment)
                {
                    endModel = ((FilterModelAttachment) endElements[0]).Destination;
                    filteringType = ((FilterModelAttachment) endElements[0]).FilteringType;
                }

                if (startModel != null && endModel != null &&
                    startModel != endModel)
                {
                    endModel.AddIncomingFilter(startModel, filteringType);
                }
            }
        }

        public void CreateVisTableExecuteCallback(Stroq s)
        {
            s.BackingStroke.DrawingAttributes.Color = Color.FromArgb(0x88, 0x00, 0x8D, 0xff);
            _can.AddNoUndo(s);

            Rct bounds = s.GetBounds();

            TableFeedback table = new TableFeedback(_can);
            table.RenderTransform = new TranslateTransform(bounds.TopLeft.X, bounds.TopLeft.Y);
            table.Width = bounds.Width;
            table.Height = bounds.Height;
            s.Move(new Vec(-bounds.TopLeft.X, -bounds.TopLeft.Y));
            table.AddStroq(s);

            _can.AddNoUndo(table);

            _can.Rem(s);
        }

        public void CreateSliderExecuteCallback(Stroq s)
        {
            s.BackingStroke.DrawingAttributes.Color = Color.FromArgb(0x88, 0x00, 0x8D, 0xff);
            _can.AddNoUndo(s);

            Rct bounds = s.GetBounds();

            SliderFeedback axis = new SliderFeedback(_can);
            axis.RenderTransform = new TranslateTransform(bounds.TopLeft.X, bounds.TopLeft.Y);
            axis.Width = bounds.Width;
            axis.Height = bounds.Height;
            s.Move(new Vec(-bounds.TopLeft.X, -bounds.TopLeft.Y));
            axis.AddStroq(s);

            _can.AddNoUndo(axis);

            _can.Rem(s);
        }
        
        public void CreatePieChartExecuteCallback(BrownRecognitionCommon.BrownShape brownShape, Stroq s)
        {
            if (brownShape.ShapeType == ShapeType.Circle ||
                brownShape.ShapeType == ShapeType.Ellipse)
            {

                s.BackingStroke.DrawingAttributes.Color = Color.FromArgb(0x88, 0x00, 0x8D, 0xff);
                _can.AddNoUndo(s);

                Rct bounds = s.GetBounds();

                AxisFeedback axis = new AxisFeedback(_can, FilterRendererType.Pie);
                axis.RenderTransform = new TranslateTransform(bounds.TopLeft.X, bounds.TopLeft.Y);
                axis.Width = bounds.Width;
                axis.Height = bounds.Height;
                s.Move(new Vec(-bounds.TopLeft.X, -bounds.TopLeft.Y));
                axis.AddStroq(s);

                _can.AddNoUndo(axis);

                _can.Rem(s);
            }
        }

        public void ShortcutExecuteCallback(List<AttributeView> startElements, string recog)
        {
            //startElements[0].ShortcutGesture(recog);
        }
    }
}
