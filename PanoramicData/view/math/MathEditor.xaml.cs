using System.Linq;
using System.Windows.Threading;
using CombinedInputAPI;
using PanoramicDataModel;
using PixelLab.Common;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using starPadSDK.MathExpr;
using Label = PanoramicData.utils.inq.Label;
using PanoramicData.view.other;
using PanoramicData.view.table;
using PanoramicData.model.view;
using PanoramicData.controller.math;

namespace PanoramicData.view.math
{
    /// <summary>
    /// Interaction logic for MathEditor.xaml
    /// </summary>
    public partial class MathEditor : UserControl, ColumnHeaderEventHandler
    {
        public MathManager MathManager = null;
        public StroqCollection Stroqs = new StroqCollection();
        private DispatcherTimer _updateTimer = new DispatcherTimer();

        private MathEditorExecution _execution = null;

        private FilterModel _filterModel = null;
        private Delegate _outsidePointDelegate = null;
        private CalculatedColumnDescriptorInfo _calculatedColumnDescriptorInfo = null;

        public MathEditor(MathEditorExecution execution, FilterModel filterModel, CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo)
        {
            InitializeComponent();
            _filterModel = filterModel;
            _calculatedColumnDescriptorInfo = calculatedColumnDescriptorInfo;
            _calculatedColumnDescriptorInfo.PropertyChanged += _calculatedColumnDescriptorInfo_PropertyChanged;

            _outsidePointDelegate = new EventHandler<TouchEventArgs>(PointOutsideDownEvent);
            Application.Current.MainWindow.AddHandler(FrameworkElement.TouchDownEvent, _outsidePointDelegate, true);

            _execution = execution;
            aPage.StroqAddedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqAddedEvent);
            aPage.StroqRemovedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqRemovedEvent);
            aPage.StroqsAddedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsAddedEvent);
            aPage.StroqsRemovedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsRemovedEvent);

            aPage.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

            _updateTimer.Tick += new EventHandler(_updateTimer_Tick);
            MathManager = new MathManager(null, recognitionResultRenderer, null, _calculatedColumnDescriptorInfo);
            MathManager.ForceMath = true;

            recognitionGrid.SizeChanged += MathEditor_SizeChanged;
            this.Width = (RadialControl.OUTER_RADIUS*6);
            this.Height = (RadialControl.OUTER_RADIUS*2);

            MathManager.RecognitionChanged += _mathManager_RecognitionChanged;

            // recreate content
            lblName.Content = _calculatedColumnDescriptorInfo.Name;
            aPage.AddNoUndo(_calculatedColumnDescriptorInfo.Stroqs);

            foreach (var label in _calculatedColumnDescriptorInfo.ProvidedLabels.Keys)
            {
                Rct bounds = label.InkTableContents.GetStroqs().GetBounds();

                SimpleGridViewColumnHeader headerView = new SimpleGridViewColumnHeader(false, true);
                headerView.DataContext = _calculatedColumnDescriptorInfo.ProvidedLabels[label];
                headerView.TableModel = _filterModel.TableModel;
                headerView.Measure(new Size(double.PositiveInfinity,
                                         double.PositiveInfinity));

                headerView.Width = bounds.Width;
                headerView.HeaderBorder.Width = bounds.Width;
                headerView.Height = bounds.Height;
                headerView.HeaderBorder.Height = bounds.Height;

                headerView.RenderTransform = new TranslateTransform(
                    bounds.Left,
                    bounds.Top);
                aPage.AddNoUndo(headerView);
            }



            Rectangle r1 = null;
            for (int i = 0; i < 10; i++)
            {
                r1 = new Rectangle();
                r1.RadiusX = 6;
                r1.RadiusY = 6;
                r1.RenderTransform = new TranslateTransform(i, i);
                r1.Width = RadialControl.OUTER_RADIUS * 6;
                r1.Height = RadialControl.OUTER_RADIUS * 2;
                r1.Fill = new SolidColorBrush(Color.FromArgb(25, 30, 30, 30));
                canvasMain.Children.Add(r1);
            }

            r1 = new Rectangle();
            r1.RadiusX = 6;
            r1.RadiusY = 6;
            r1.Width = RadialControl.OUTER_RADIUS * 6;
            r1.Height = RadialControl.OUTER_RADIUS * 2;
            r1.Fill = Brushes.White;
            r1.Stroke = Brushes.DarkGray;
            r1.StrokeThickness = 3;
            canvasMain.Children.Add(r1);

            tree.InitTree(_filterModel.TableModel);
        }

        void _calculatedColumnDescriptorInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            foreach (var elem in aPage.Elements.Where(s => s is SimpleGridViewColumnHeader))
            {
                var tmp = elem.DataContext;
                elem.DataContext = null;
                elem.DataContext = tmp;
            }
        }

        void MathEditor_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MathManager.Width = recognitionGrid.ActualWidth;
            MathManager.Height = recognitionGrid.ActualHeight;
        }

        void _mathManager_RecognitionChanged(object sender, EventArgs e)
        {
            _updateTimer.Stop();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(500);
            _updateTimer.Start();
        }

        private void _updateTimer_Tick(object sender, EventArgs e)
        {
            bool numeric = true;
            Expr expr = MathManager.GetRenderableExpr(out numeric);
            BuiltInEngine builtInEngine = new BuiltInEngine();
            Console.WriteLine(">>>> " +builtInEngine.Numericize(expr).ToString());
            /*TextFilter filter = ((FrameworkElement)this).FindParent<TextFilter>();

            bool numeric = true;
            Expr expr = MathManager.GetRenderableExpr(out numeric);

            double number;
            string text;
            FormulaEvaluator.GetValue(expr, out text, out number);
            if (expr is WordSym && (expr as WordSym).Word == "")
            {
                text = "";
            }

            filter.UpdateFilterDescriptor(text);*/
            _updateTimer.Stop();
        }

        void stroqAddedEvent(Stroq s)
        {
            Stroqs.Add(s);
            MathManager.UpdateMathRecognition(Stroqs);
        }

        void stroqsAddedEvent(Stroq[] stroqs)
        {
            foreach (var s in stroqs)
            {
                Stroqs.Add(s);
            }
            MathManager.UpdateMathRecognition(Stroqs);
        }

        void stroqRemovedEvent(Stroq s)
        {
            Stroqs.Remove(s);
            MathManager.UpdateMathRecognition(Stroqs);
        }

        void stroqsRemovedEvent(Stroq[] stroqs)
        {
            foreach (var s in stroqs)
            {
                Stroqs.Remove(s);
            }
            MathManager.UpdateMathRecognition(Stroqs);
        }

        void PointOutsideDownEvent(Object sender, TouchEventArgs e)
        {
            if (e.Source != this)
            {
                Application.Current.MainWindow.RemoveHandler(FrameworkElement.TouchDownEvent, _outsidePointDelegate);
                dispose();
            }
        }

        public void ColumnHeaderMoved(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e, bool overElement)
        {
            
        }

        public void ColumnHeaderDropped(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e)
        {
            if (e.ColumnDescriptor is CalculatedColumnDescriptor &&
                (e.ColumnDescriptor as CalculatedColumnDescriptor).CalculatedColumnDescriptorInfo.Equals(
                    _calculatedColumnDescriptorInfo))
            {
                return;
            }

            InqScene inqScene = this.FindParent<InqScene>();
            Pt pt = aPage.TranslatePoint(new Point(0, 0), inqScene);
            Rct bounds = e.Bounds.Translated(-pt.GetVec());
            Stroq s = new Stroq(bounds.GetPoints());
            StroqCollection sc = new StroqCollection();
            sc.Add(s);

            PanoramicDataColumnDescriptor cd = (PanoramicDataColumnDescriptor) e.ColumnDescriptor.Clone();
            _calculatedColumnDescriptorInfo.CreateNewLabel(sc, cd);

            aPage.AddNoUndo(s);

            // create the header representation 
            SimpleGridViewColumnHeader headerView = new SimpleGridViewColumnHeader(false, true);
            headerView.DataContext = cd;
            headerView.TableModel = e.FilterModel.TableModel;
            headerView.Measure(new Size(double.PositiveInfinity,
                                     double.PositiveInfinity));

            headerView.Width = bounds.Width;
            headerView.HeaderBorder.Width = bounds.Width;
            headerView.Height = bounds.Height;
            headerView.HeaderBorder.Height = bounds.Height;

            headerView.RenderTransform = new TranslateTransform(
                bounds.Left,
                bounds.Top);
            aPage.AddNoUndo(headerView);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return new GeometryHitTestResult(this, IntersectionDetail.Intersects);
        }

        void dispose()
        {
            bool numeric = true; 
            BuiltInEngine builtInEngine = new BuiltInEngine();
            Expr expr = MathManager.GetRenderableExpr(out numeric);
            _calculatedColumnDescriptorInfo.Stroqs = Stroqs;
            _calculatedColumnDescriptorInfo.Name = lblName.Content.ToString();
            _calculatedColumnDescriptorInfo.Expr = builtInEngine.Numericize(expr);
            _execution.Dispose(this);
        }

        public void SetPosition(double x, double y)
        {
            this.RenderTransform = new TranslateTransform(x, y);
        }

        private void GridName_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            RadialMenuCommand filter = new RadialMenuCommand();
            filter.Name = "Filter";
            filter.AllowsStroqInput = true;
            filter.IsSelectable = false;

            Point curDrag = e.GetPosition(aPage);
            RadialControl rc = new RadialControl(
                filter,
               new MathEditorRadialControlExecution(_calculatedColumnDescriptorInfo, lblName, aPage));

            rc.SetPosition(
                curDrag.X - (RadialControl.OUTER_RADIUS * 4) / 2,
                curDrag.Y - (RadialControl.OUTER_RADIUS * 2) / 2);
            aPage.AddNoUndo(rc);
        }
    }

    public class MathEditorRadialControlExecution : RadialControlExecution
    {
        private InqScene _inqScene = null;
        private CalculatedColumnDescriptorInfo _calculatedColumnDescriptorInfo = null;
        private System.Windows.Controls.Label _label = null;

        public MathEditorRadialControlExecution(CalculatedColumnDescriptorInfo info, System.Windows.Controls.Label lbl, InqScene inqScene)
        {
            this._inqScene = inqScene;
            this._label = lbl;
            this._calculatedColumnDescriptorInfo = info;
        }

        public override void Dispose(RadialControl sender)
        {
            base.Dispose(sender);

            if (_inqScene != null)
            {
                _inqScene.Rem(sender as FrameworkElement);
            }
        }

        public override void ExecuteCommand(RadialControl sender, RadialMenuCommand cmd, string needle = null,
            StroqCollection stroqs = null)
        {
            base.ExecuteCommand(sender, cmd, needle, stroqs);

            if (stroqs != null)
            {
                _label.Content = needle.Trim();
            }
        }
    }

    public class MathEditorExecution
    {
        public virtual void Remove(MathEditor sender, RadialMenuCommand cmd) { }
        public virtual void Dispose(MathEditor sender) { }
    }
}