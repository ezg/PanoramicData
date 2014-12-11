using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using Constant = starPadSDK.MathExpr.MathConstant;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using Microsoft.Research.DynamicDataDisplay.DataSources;

namespace starPadSDK.AppLib
{
    public partial class MathEditor : CommandSet.CommandEditor
    {
        MathRecognition _mrec;
        Canvas          _mathUICanvas = new Canvas(); // displays typeset expressions, selection boxes etc.
        Ruler           _ruler = null;
        bool            _inDelete = false; // flag to stop updating math while a Delete action is being undone (updating during the undo causes weird exceptions)
        List<object>    _activeSelections = new List<object>(); // actively dragged selection list used to determine when to release the BatchLock
        Expr            suspended = null; // used for drag&drop expression substitution

        protected ContainerVisualHost _underlay = new ContainerVisualHost(); // the region feedback
        protected StroqCollection     _mathStroqs = new StroqCollection(); // the Stroqs that are being recognized by the math engine
        protected BatchLock           _batchEdit = null;
        public StroqCollection MathStroqs   { get { return _mathStroqs; } }
        public Canvas          MathUICanvas {
            get { return _mathUICanvas; }
            set { _mathUICanvas = value; }
        }
        public TermDragMode TermDraggingMode = TermDragMode.Default;

        public enum  TermDragMode { Default, FactorOut, SplitApart, SplitIntoFraction }

        public class FactorerUI
        {
            protected MathEditor _medit;
            public FactorerUI(MathEditor medit)
            {
                _medit = medit;
                _medit.MathExpressionEvent += new MathExpressionHandler(mathExpressionAdded);
            }
            public void WatchCommands(TapOnMathCommand tc)
            {
                tc.TappedMathEvent += new TapOnMathCommand.TappedMathHandler(MathTapped);
            }

            protected virtual void mathExpressionAdded(ContainerVisualHost cvh, bool allowTaps) { }
            protected virtual void MathTapped(ContainerVisualHost frozenVisual) { }
        }
        public class GraphViewerUI
        {
            protected MathEditor _medit;
            public GraphViewerUI(MathEditor medit)
            {
                _medit = medit;
                _medit.MathDroppedEvent += new MathEditor.MathDroppedHandler(mathDropped);
            }
            public MathEditor MathEditor { get { return _medit; } }
            public void WatchCommands(GraphingCommand gc, EmptyGraphCommand ec)
            {
                gc.FunctionPlotEvent -= new GraphingCommand.FunctionPlotHandler(displayFunctionPlot);
                ec.FunctionPlotEvent -= new EmptyGraphCommand.FunctionPlotHandler(displayFunctionPlot);
                gc.FunctionPlotEvent += new GraphingCommand.FunctionPlotHandler(displayFunctionPlot);
                ec.FunctionPlotEvent += new EmptyGraphCommand.FunctionPlotHandler(displayFunctionPlot);
            }
            protected virtual void mathDropped(Parser.Range r, FrameworkElement e) { }
            protected virtual void displayFunctionPlot(IEnumerable<Parser.Range> funcs, Pt where) { }
            protected virtual void displayFunctionPlot(IEnumerable<Parser.Range> funcs, Rct where) { }
        }
        public class ExprTags
        {
            public bool                Active = false;
            public Expr                Expr = null;
            public ContainerVisualHost Output = null;
            public double              FontSize = 0;
            public Point               Offset = new Point(0, 0);
            public int                 NumIterationsFromOriginal = 0;
            public Guid                Id;
            public Color               Color = Colors.Black;

            public ExprTags(Guid id, Expr expr, double fontSize, Point offset, int numIterationsFromOriginal) {
                Id = id;
                FontSize = fontSize;
                Expr = ExprTransform.FixNegatives(expr);
                Offset = offset;
                NumIterationsFromOriginal = numIterationsFromOriginal;
            }
        }

        // converts old Ink Strokes to Stroqs and gets the aggregate bounding box
        Rct bbox(Microsoft.Ink.Strokes stks) {
            return _mrec.Sim[stks].Aggregate(Rct.Null, (Rct r, Stroq s) => r.Union(s.GetBounds()));
        }

        public void UpdateComputations() {
            List<ContainerVisualHost> visuals = new List<ContainerVisualHost>();
            foreach (FrameworkElement fe in _mathUICanvas.Children)
                if (fe is ContainerVisualHost)
                    visuals.Add(fe as ContainerVisualHost);
            List<Parser.ParseResult> presults = new List<Parser.ParseResult>();
            List<ContainerVisualHost> tags = new List<ContainerVisualHost>();
            foreach (ContainerVisualHost r in visuals)
                if (r.Tag != null && r.Tag is ExprTags && (r.Tag as ExprTags).NumIterationsFromOriginal == 0) {
                    Rct bounds = WPFUtil.GetBounds(r);
                    presults.Add(new Parser.ParseResult((r.Tag as ExprTags).Expr, new System.Drawing.Rectangle((int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height)));
                    tags.Add(r);
                }
            List<Parser.ParseResult> tempResults = new List<Parser.ParseResult>();
            foreach (ContainerVisualHost res in tags) {
                ExprTags etag = res.Tag as ExprTags;
                tempResults.Add(new Parser.ParseResult(etag.Expr, new System.Drawing.Rectangle()));
            }
            for (int i = 0; i < tags.Count; i++) {
                ContainerVisualHost res = tags[i];
                ExprTags etag = res.Tag as ExprTags;
                CompositeExpr ce = etag.Expr as CompositeExpr;
                if (ce != null && (ce.Head == new LetterSym(Unicode.R.RIGHTWARDS_DOUBLE_ARROW) || ce.Head == new LetterSym('→'))) {
                    Evaluator.UpdateMath(tempResults);

                    Expr theMath = Evaluator.SubstMathVarsBegin(presults, ce.Args[0]);
                    Expr result = tempResults[i].finalSimp;
                    if (etag.Output != null) {
                        _mathUICanvas.Children.Remove(etag.Output);
                        etag.Output = null;
                    }
                    ContainerVisualHost cvh = EWPF.ToVisual(result, etag.FontSize, Colors.Red, null, EWPF.DrawTop);

                    Box exprBox;
                    Rct exprBounds = WPFUtil.GetBounds(res);        // measure the starting expression
                    EWPF.MeasureTopLeft(theMath, etag.FontSize, out exprBox);
                    Box equalsBox = EWPF.HitBox.FindEqualsBox(theMath, exprBox);  // and find where the center of the equals sign is
                    Vec alignToCenter = new Vec(exprBounds.Right, exprBounds.Top - exprBox.bbox.Top);

                    Box resultBox;  // measure the resulting expression to find the offset from its baseline to its topLeft
                    Pt resultTopLeft = EWPF.MeasureTopLeft(result, etag.FontSize, out resultBox);

                    cvh.RenderTransform = new TranslateTransform(alignToCenter.X + 15, alignToCenter.Y + resultTopLeft.Y);
                    _mathUICanvas.Children.Add(cvh);
                    etag.Output = cvh;
                }
            }
            CheckForAssociation(true);
        }

        public delegate void MathChangedHandler(object o, MathRecognition source, Recognition charChanged);
        public event MathChangedHandler MathChangedEvent;
        void mrec_ParseUpdated(MathRecognition source, Recognition charChanged, bool updateMath) {
            Evaluator.TestForFunctionDefinition(this, _mrec.Ranges.Select((Parser.Range r) => r.Parse));
            if (!_inDelete && MathChangedEvent != null)
                MathChangedEvent(this, source, charChanged);
            /* Evaluate math if necessary */
            if (updateMath)
                try {
                    Evaluator.UpdateMath(_mrec.Ranges.Select((Parser.Range r) => r.Parse));
                }
                catch { }

            /* reset geometry displayed: range displays, etc */
            _underlay.Children.Clear();
            //_mathUICanvas.Children.Clear();
            foreach (ContainerVisualHost visual in _mathUICanvas.Children.OfType<ContainerVisualHost>().ToArray())
            {
                ExprTags tags = (visual.Tag as ExprTags);
                if (tags == null ||  tags.Color == Colors.Blue)
                    _mathUICanvas.Children.Remove(visual);
            }
            /* set up to draw background yellow thing for range displays */
            Brush fill3 = new SolidColorBrush(Color.FromArgb(50, 220, 220, 220));
            Brush fill2 = new SolidColorBrush(Color.FromArgb(75, 220, 220, 220));
            Brush fill1 = new SolidColorBrush(Color.FromArgb(100, 220, 220, 220));

            foreach (Parser.Range range in _mrec.Ranges) {
                Rct strokesBoundingBox = bbox(range.Strokes).Inflate(8, 8);  // the bounding box of the expressions ink strokes, inflated by 8
                //_mrec.Sim[range.Parse.Strokes];
                
                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                dc.DrawRoundedRectangle(fill3, null, strokesBoundingBox, 4, 4);
                dc.DrawRoundedRectangle(fill2, null, strokesBoundingBox.Inflate(-4, -4), 4, 4);
                dc.DrawRoundedRectangle(fill1, null, strokesBoundingBox.Inflate(-8, -8), 4, 4);
                dc.Close();
                _underlay.Children.Add(dv);

                // if the user draws a -> or =>, then display the approximated/simplified value of the expression to the right of the ink
                if (range.Parse != null && range.Parse.finalSimp != null) {
                    //Expr result = range.Parse.matrixOperationResult == null ? range.Parse.finalSimp : range.Parse.matrixOperationResult;

                    //ContainerVisualHost cvh = EWPF.ToVisual(result, 18, Colors.Black, null, EWPF.DrawTop);
                    //Point typesetExprOffset = new Point(strokesBoundingBox.Left, strokesBoundingBox.Bottom + 10);
                    //cvh.RenderTransform = new TranslateTransform(strokesBoundingBox.Right + 10, strokesBoundingBox.Center.Y);
                    //_inqCanvas.Children.Add(cvh);
                }

                // display the recognized expression below the ink strokes
                if (/*strokesBoundingBox.Contains(Mouse.GetPosition(_can)) && */range.Parse != null && range.Parse.expr != null) {
                    bool inUse = false;
                    foreach (ContainerVisualHost visual in _mathUICanvas.Children.OfType<ContainerVisualHost>().ToArray())
                    {
                        ExprTags tags = (visual.Tag as ExprTags);
                        if (tags != null && tags.Color != Colors.Blue && tags.Id == range.ID)
                        {
                            inUse = true;
                            break;
                        }
                    }
                    if (!inUse)
                    {
                        int typesetExprFontSize = smallFontSize;
                        TermColorizer.ClearFactorMarks(range.Parse.expr, -1);
                        ContainerVisualHost cvh = EWPF.ToVisual(range.Parse.expr, typesetExprFontSize, Colors.Blue, Brushes.White, EWPF.DrawTop);
                        Point typesetExprOffset = new Point(strokesBoundingBox.Left, strokesBoundingBox.Bottom + 10);
                        cvh.RenderTransform = new TranslateTransform(typesetExprOffset.X, typesetExprOffset.Y);
                        _mathUICanvas.Children.Add(cvh);

                        // this starts up the factoring interaction
                        cvh.Tag = new ExprTags(range.ID, range.Parse.expr, typesetExprFontSize, typesetExprOffset, 0);
                        ((ExprTags)cvh.Tag).Color = Colors.Blue;
                        addFactorHandlers(cvh, true);
                    }
                }

                /* colorize ink. Ideally we would have kept track of which ink strokes had changes and only update colorization in those ranges affected
 * by the changes. */
                if (range != null && range.Parse != null && range.Parse.root != null) _colorizer.Colorize(range.Parse.root, range.Strokes, _mrec);
            }
            UpdateComputations();
        }

        starPadSDK.MathUI.InkColorizer _colorizer = new starPadSDK.MathUI.InkColorizer();

        public void ResetColorizer() { _colorizer.Reset(); }

        public void CheckForAssociation(bool flag)
        {
            foreach (FrameworkElement fe in _can.Elements)
            {
                if (fe is PolyRectangle)
                {
                    PolyRectangle rect = fe as PolyRectangle;
                    rect.removeButtons();
                    if(flag)
                        rect.addSideExpressions();

                    foreach (Parser.Range range in _mrec.Ranges)
                    {
                        Expr myExpr = range.Parse.expr;
                        if (myExpr is IntegerNumber || myExpr is DoubleNumber)
                        {
                            Rct exprBounds = bbox(range.Strokes).Inflate(50, 50);
                            Rct infBox = rect.Bounds;
                            if (infBox.IntersectsWith(exprBounds))
                            {
                                Pt centerExpr = exprBounds.Center;
                                LnSeg top = new LnSeg(rect.TransformedPoints[0], rect.TransformedPoints[1]);
                                LnSeg right = new LnSeg(rect.TransformedPoints[1], rect.TransformedPoints[2]);
                                LnSeg bottom = new LnSeg(rect.TransformedPoints[2], rect.TransformedPoints[3]);
                                LnSeg left = new LnSeg(rect.TransformedPoints[3], rect.TransformedPoints[0]);

                                double distTop, distRight, distBottom, distLeft;
                                Double side = 0;
                                distTop = top.Distance(centerExpr);
                                distRight = right.Distance(centerExpr);
                                distBottom = bottom.Distance(centerExpr);
                                distLeft = left.Distance(centerExpr);
                                
                                if(myExpr is IntegerNumber)
                                    {IntegerNumber num = myExpr as IntegerNumber; side = (int)num.Num;}
                                else if (myExpr is DoubleNumber)
                                    {DoubleNumber num = myExpr as DoubleNumber; side = (double)num.Num;}
                                
                                if (side <= 0)
                                    return;

                                if (distTop < distRight && distTop < distBottom && distTop < distLeft)
                                        rect.displayAssociationButton(0, myExpr, RecognizedMath.RangeStrokes(range));
                                else if (distRight < distTop && distRight < distBottom && distRight < distLeft)
                                    rect.displayAssociationButton(1, myExpr, RecognizedMath.RangeStrokes(range));
                                else if (distBottom < distRight && distBottom < distLeft && distBottom < distTop)
                                    rect.displayAssociationButton(2, myExpr, RecognizedMath.RangeStrokes(range));
                                else
                                    rect.displayAssociationButton(3, myExpr, RecognizedMath.RangeStrokes(range));
                            }
                        }
                    }
                }
            }
        }

        public void UpdateRulerRange(Ruler ruler)
        {
            double minLength = 10000, rangeMax =ruler.Range.Max;
            PolyRectangle closestShape = null;
            int count = 0;
            ruler.UpdateLayout();

            foreach (FrameworkElement fe in _can.Elements)
            {
                if (fe is PolyRectangle)
                {
                    count++;
                    PolyRectangle rect = fe as PolyRectangle;
                    rect.setBorderThickness(1);
                    if (rect.isLabeled())
                    {
                        LnSeg seg = new LnSeg(rect.Bounds.Center, ruler.TranslatePoint(new Point(ruler.ActualWidth / 2, ruler.ActualHeight / 2), _can));
                        double distance = seg.Length;
                        if (distance < minLength)
                        {
                            minLength = distance;
                            rangeMax = rect.getUnitLengthPerPixel() * ruler.ActualWidth;
                            closestShape = rect;
                        }
                    }
                }
            }
            if (closestShape == null)
                rangeMax = ruler.ActualWidth / Ruler.PixelsPerUnit;
            ruler.Range = new Range<double>(0, rangeMax);
            if (closestShape != null && count > 1)
                closestShape.setBorderThickness(3);
        }

        public delegate void MathExpressionHandler(ContainerVisualHost cvh, bool allowTaps);
        public event MathExpressionHandler MathExpressionEvent;

        // adds interaction handlers to math expressions for factoring
        void addFactorHandlers(ContainerVisualHost cvh, bool addTapHandler) {
            if (MathExpressionEvent != null)
                MathExpressionEvent(cvh, addTapHandler);
        }

        override protected void stroqRemoved(Stroq s) { _mathStroqs.Remove(s); }
        override protected bool stroqAdded(Stroq s) { _mathStroqs.Add(s); return false; }
        override protected void stroqsRemoved(Stroq[] s) { _mathStroqs.Remove(s); }

        void selectionDroppedEvent(SelectionObj sobj) {
            if (sobj.Strokes.Count() == 0)
                return;
            Parser.Range found = null;
            foreach (Parser.Range range in _mrec.Ranges)
                if (sobj.Bounds.Contains(range.RBounds)) {
                    found = range;
                    break;
                }
            if (found == null)
                return;
            foreach (FrameworkElement fe in _can.Elements)
                if (found.RBounds.IntersectsWith(WPFUtil.GetBounds(fe))) {
                    if (MathDroppedEvent != null)
                        MathDroppedEvent(found, fe);
                    return;
                }
            if (MathDroppedEvent != null)
                MathDroppedEvent(found, null);
        }
        public MathEditor(InqScene can, bool interactiveDrag) : base(can) {
            _mrec = new MathRecognition(_mathStroqs);
            this.InteractiveDrag = interactiveDrag;
            _mrec.EnsureLoaded();
            _mrec.ParseUpdated += mrec_ParseUpdated;
            _can.Children.Insert(0, _underlay);
            _can.Children.Insert(0, _mathUICanvas);
            _underlay.HorizontalAlignment = HorizontalAlignment.Stretch;
            _mathUICanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
            _can.UndoRedo.StartDelete += new EventHandler((object sender, EventArgs e) => _inDelete = true);
            _can.UndoRedo.FinishDelete += new EventHandler((object sender, EventArgs e) => {
                _inDelete = false;
                if (MathChangedEvent != null) MathChangedEvent(this, null, null);
            });
            InteractiveDrag = true;
            _can.SelectionDroppedEvent += new InqScene.SelectionMovedHandler(selectionDroppedEvent);
            _can.SelectionPreTransformEvent += new InqScene.SelectionTransformHandler((SelectionObj s) => { 
                if (!InteractiveDrag && _batchEdit == null) 
                    _batchEdit = _mrec.BatchEdit();
            });
            _can.SelectionPostTransformEvent += new InqScene.SelectionTransformHandler((SelectionObj s) => {
                if (!InteractiveDrag && _batchEdit != null && _activeSelections.Count <= 1) {
                    _batchEdit.Dispose();
                    _batchEdit = null;
                }
                _activeSelections.Remove(s.Device);
            });
            _can.SelectionStartTransformingEvent += new InqScene.SelectionTransformHandler((SelectionObj s) => {
                if (InteractiveDrag) {
                    foreach (Parser.Range range in _mrec.Ranges)
                        if (s.Strokes.Count() > 0 && _mrec.Sim.Stroqs.Contains(s.Strokes[0]) && range.Strokes.Contains(_mrec.Sim[s.Strokes[0]]) && range.Parse != null)
                            suspended = range.Parse.expr;
                    _mathStroqs.Remove(s.Strokes);
                }
            }); 
            _can.SelectionStartTransformingEvent += new InqScene.SelectionTransformHandler((SelectionObj s) => { if (InteractiveDrag) _mathStroqs.Remove(s.Strokes); });
            _can.SelectionStopTransformingEvent += new InqScene.SelectionTransformHandler((SelectionObj s) => { if (InteractiveDrag && !s.Empty && s.Strokes.Count() > 0 && s.Strokes[0].BackingStroke.DrawingAttributes.Color == Colors.Black) _mathStroqs.Add(s.Strokes); });
            
            _can.SelectionDroppedEvent += new InqScene.SelectionMovedHandler((SelectionObj sel) => {
                if (InteractiveDrag)
                {
                    foreach (ContainerVisualHost vis in _mathUICanvas.Children.OfType<ContainerVisualHost>().ToArray())
                    {
                        ExprTags tags = (vis.Tag as ExprTags);
                        if (tags == null || 
                            tags.Color == Colors.Blue)
                            continue;
                        if (WPFUtil.GetBounds(vis).IntersectsWith(sel.Bounds) && suspended != null)
                        {
                            SortedList<int, ContainerVisualHost> theExprs = new SortedList<int, ContainerVisualHost>();
                            foreach (ContainerVisualHost visual in _mathUICanvas.Children.OfType<ContainerVisualHost>().ToArray())
                                if (visual.Tag != null && visual.Tag is ExprTags && tags.Id == (visual.Tag as ExprTags).Id && (visual.Tag as ExprTags).Color != Colors.Blue)
                                {
                                    ExprTags currentExprTags = visual.Tag as ExprTags;
                                    theExprs.Add(currentExprTags.NumIterationsFromOriginal, visual);
                                }
                            if (theExprs.Count == 0)
                                continue;
                            ExprTags last = (theExprs.Last().Value as ContainerVisualHost).Tag as ExprTags;
                            ContainerVisualHost frozenVisual = (theExprs.Last().Value as ContainerVisualHost);
                            Expr expr = last.Expr;
                            Expr newExpr = Engine.Substitute(expr, suspended.Args()[0], suspended.Args()[1]);
                            outputVisualExpr = displayModifiedExpr(newExpr.Clone(), new Pt(last.Offset.X, last.Offset.Y + frozenVisual.Height + 10), smallFontSize, last.Id, last.NumIterationsFromOriginal + 1, true);
                            createNextExpr(outputVisualExpr);
                            _can.UndoRedo.Undo();
                            break;
                        }
                    }
                   //_can.SetSelection(sel.Device, new SelectionObj());
               }
            });
            _can.SelectedChangedEvent += new InqScene.SelectedChangedHandler((object device, InqScene scene) => { _activeSelections.Remove(device);  });
            _ruler = new Ruler(Scene);
            _ruler.Visibility = Visibility.Collapsed;
        }

        public delegate void MathDroppedHandler(Parser.Range range, FrameworkElement ele);
        public event MathDroppedHandler MathDroppedEvent;
        public MathRecognition RecognizedMath
        {
            get { return _mrec; }
            set { _mrec = value; }
        }
        public void  ClearStroqs() { _mathStroqs.Clear(); }
        public bool  InteractiveDrag {
            get;
            set;
        }
        public Ruler Ruler
        {
            get { return _ruler; }
            set { _ruler = value; }
        }
        public void UpdateParseFeedback() { mrec_ParseUpdated(null, null, true); }
    }
}
