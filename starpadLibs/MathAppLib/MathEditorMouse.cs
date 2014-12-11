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

namespace starPadSDK.AppLib {
    public class MouseMathFactorer : MathEditor.FactorerUI
    {
        /// Mouse factoring interactions w/ typeset expressions
        /// 
        protected const int smallFontSize = 25;
        protected const int largeFontSize = 55;
        protected EWPF.HitBox _firstSelectedExpr;
        protected EWPF.HitBox _secondSelectedExpr;
        protected DateTime    _downTime = DateTime.MinValue;

        public MouseMathFactorer(MathEditor medit):base(medit) { }
        protected override void mathExpressionAdded(ContainerVisualHost cvh, bool allowTaps)
        {
            setupToGrabInput(cvh);
        }
        protected override void MathTapped(ContainerVisualHost frozenVisual)
        {
            MathEditor.ExprTags exprTags = frozenVisual.Tag as MathEditor.ExprTags;
            exprTags.Active = !exprTags.Active;
            TermColorizer.ClearFactorMarks(exprTags.Expr, exprTags.NumIterationsFromOriginal);
            _firstSelectedExpr = _secondSelectedExpr = null;
            if (exprTags.FontSize == largeFontSize)
                _medit.UpdateParseFeedback();
            else _medit.createNextExpr(frozenVisual);
            setupToGrabInput(frozenVisual);
        }

        void cvh_StylusDown(object sender, StylusDownEventArgs e) {
            _medit.Scene.SetInkEnabledForDevice(e.StylusDevice, false);
            e.Handled = true;
        }

        void setupToGrabInput(ContainerVisualHost cvh) {
            MathEditor.ExprTags exprTags = cvh.Tag as MathEditor.ExprTags;
            cvh.PreviewStylusDown -= new StylusDownEventHandler(cvh_StylusDown);
            cvh.MouseMove -= new MouseEventHandler(mouseMoveOverFrozenExpression);
            cvh.MouseDown -= new MouseButtonEventHandler(mouseClickOnFrozenExpression);
            if (exprTags.FontSize > smallFontSize)
                exprTags.Active = true;
            if (exprTags.Active) {
                if (exprTags.FontSize < largeFontSize)
                    TermColorizer.ClearFactorMarks(exprTags.Expr, exprTags.NumIterationsFromOriginal);
                cvh.PreviewStylusDown += new StylusDownEventHandler(cvh_StylusDown);
                cvh.MouseMove += new MouseEventHandler(mouseMoveOverFrozenExpression);
                cvh.MouseDown += new MouseButtonEventHandler(mouseClickOnFrozenExpression);
            }
        }

        EWPF.HitBox _clearExprOnClick = null;
        Pt _firstMouseDown = new Pt();
        Pt _secondMouseDown = new Pt();
        void mouseClickOnFrozenExpression(object sender, MouseButtonEventArgs e)
        {
            ContainerVisualHost frozenVisual = sender as ContainerVisualHost;
            MathEditor.ExprTags exprTags     = frozenVisual.Tag as MathEditor.ExprTags;
            Pt                  where        = e.GetPosition(_medit.MathUICanvas);
            _downTime = DateTime.Now;

            // first convert the mouse coordinate to the local coordinate system of the Expr's visual
            ContactArea area = frozenVisual.FromPt(where, new Vec(10, 10));

            // then pick a term to factor
            EWPF.HitBox selectedExpr = EWPF.PickExpressionTerm(exprTags.Expr, exprTags.FontSize, area);

            _clearExprOnClick = null;
            if (selectedExpr != null) {
                if (_firstSelectedExpr == null || Expr.FindPath(_firstSelectedExpr.HitExpr, selectedExpr.HitExpr).Count == 0) {
                    if (_firstSelectedExpr != null && object.ReferenceEquals(selectedExpr.HitExpr, _firstSelectedExpr.HitExpr)) {
                        _clearExprOnClick = _firstSelectedExpr;
                    }
                    if (_secondSelectedExpr != null && object.ReferenceEquals(selectedExpr.HitExpr, _secondSelectedExpr.HitExpr)) {
                        _clearExprOnClick = _secondSelectedExpr;
                    }
                    if (_firstSelectedExpr != null && _clearExprOnClick == null) {
                        if (_secondSelectedExpr != null)
                            TermColorizer.ClearFactorMarks(_secondSelectedExpr.HitExpr, exprTags.NumIterationsFromOriginal);
                        _secondSelectedExpr = selectedExpr;
                        _secondMouseDown = where;
                        _medit.firstGhostTerm = _medit.CreateGhostTerm(where, exprTags.FontSize, _firstSelectedExpr);
                    }
                    else if (_clearExprOnClick == null) {
                        _firstSelectedExpr = selectedExpr;
                        _firstMouseDown = where;
                        _medit.firstGhostTerm = _medit.CreateGhostTerm(where, exprTags.FontSize, _firstSelectedExpr);
                    }
                    if (_clearExprOnClick == null)
                        TermColorizer.MarkFactor(selectedExpr.HitExpr, exprTags.NumIterationsFromOriginal);
                }
                else {
                    if (_firstSelectedExpr != null)
                        _clearExprOnClick = _firstSelectedExpr;
                    _secondMouseDown = where;
                    _secondSelectedExpr = selectedExpr;
                }

                Mouse.Capture(frozenVisual);
                frozenVisual.MouseMove -= new MouseEventHandler(mouseMoveOverFrozenExpression);
                frozenVisual.MouseMove += new MouseEventHandler(mouseDragFrozenExpressionTerm);
                frozenVisual.MouseUp += new MouseButtonEventHandler(frozenVisual_MouseUp);
                _medit.Scene.SetInkEnabledForDevice(e.Device, false);
                e.Handled = true;
            }
        }

        void frozenVisual_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ContainerVisualHost frozenVisual = sender as ContainerVisualHost;
            MathEditor.ExprTags etags = frozenVisual.Tag as MathEditor.ExprTags;
            if (_clearExprOnClick != null) {
                if (_secondSelectedExpr == _clearExprOnClick || (_secondSelectedExpr != null && _firstSelectedExpr != null && 
                    _secondSelectedExpr.Box.bbox == _firstSelectedExpr.Box.bbox))  // hack ! if the 1st and 2nd expr are the same term, they won't be equal because of selection refinement's copying of Exprs
                    _secondSelectedExpr = null;
                if (_firstSelectedExpr == _clearExprOnClick)
                    _firstSelectedExpr = null;
                if (_firstSelectedExpr == null && _secondSelectedExpr != null) {
                    _firstSelectedExpr = _secondSelectedExpr;
                    _secondSelectedExpr = null;
                }
                TermColorizer.ClearFactorMarks(etags.Expr, etags.NumIterationsFromOriginal);
                if (_firstSelectedExpr != null)
                    TermColorizer.MarkFactor(_firstSelectedExpr.HitExpr, etags.NumIterationsFromOriginal);
            }

            Mouse.Capture(null);
            _medit.Scene.SetInkEnabledForDevice(e.Device, false);
            _medit.MathUICanvas.Children.Remove(_medit.firstGhostTerm);
            frozenVisual.MouseMove -= new MouseEventHandler(mouseDragFrozenExpressionTerm);
            frozenVisual.MouseUp -= new MouseButtonEventHandler(frozenVisual_MouseUp);
            frozenVisual.MouseMove += new MouseEventHandler(mouseMoveOverFrozenExpression);

            if (_firstSelectedExpr == null) {
                TermColorizer.ClearFactorMarks(etags.Expr, etags.NumIterationsFromOriginal);
                EWPF.UpdateVisual(frozenVisual, etags.Expr, etags.FontSize, Colors.Black, Brushes.Black);
                return;
                if (etags != null && etags.FontSize == largeFontSize)
                    _medit.UpdateParseFeedback();
                else _medit.createNextExpr(frozenVisual);
            }

            if (DateTime.Now.Subtract(_downTime).TotalMilliseconds > 250)
            {
                if (_medit.outputVisualExpr != null)
                {
                    if ((_medit.outputVisualExpr.Tag as MathEditor.ExprTags).Expr != etags.Expr) {
                        _medit.createNextExpr(_medit.outputVisualExpr);
                        _firstSelectedExpr = _secondSelectedExpr = null;
                    } 
                    else {
                        _medit.undisplayExpr(_medit.outputVisualExpr);
                        TermColorizer.ClearFactorMarks(etags.Expr, etags.NumIterationsFromOriginal);
                        if (_firstSelectedExpr != null)
                            TermColorizer.MarkFactor(_firstSelectedExpr.HitExpr, etags.NumIterationsFromOriginal);
                        EWPF.UpdateVisual(frozenVisual, etags.Expr, etags.FontSize, Colors.Black, Brushes.Black);
                    }
                }
            }
            else {
                if (_firstSelectedExpr != null && _secondSelectedExpr != null) {
                    int ind1, ind2;
                    Expr first = Expr.FindCommonAncestor(_firstSelectedExpr.Path[0], _firstSelectedExpr.HitExpr, _secondSelectedExpr.HitExpr, out ind1, out ind2);
                    if (first != null) {
                        _firstSelectedExpr = EWPF.FindTermBox(_firstSelectedExpr.Path[0], etags.FontSize, first);
                        _secondSelectedExpr = null;
                        TermColorizer.ClearFactorMarks(_firstSelectedExpr.Path[0], etags.NumIterationsFromOriginal);
                        TermColorizer.MarkFactor(_firstSelectedExpr.HitExpr, etags.NumIterationsFromOriginal);
                    }
                }
                EWPF.UpdateVisual(frozenVisual, etags.Expr, etags.FontSize, Colors.Black, Brushes.Black);
                //EngineLoader.ClearFactorMarks(etags.Expr, etags.NumIterationsFromOriginal);
                //if (etags != null && etags.FontSize == largeFontSize)
                //    _medit.UpdateParseFeedback();
                //else _medit.createNextExpr(frozenVisual);
            }
        }

        void mouseDragFrozenExpressionTerm(object sender, MouseEventArgs e)
        {
            if (_firstSelectedExpr == null || DateTime.Now.Subtract(_downTime).TotalMilliseconds < 250)
                return;
            _clearExprOnClick = null;
            ContainerVisualHost frozenVisual = sender as ContainerVisualHost;
            MathEditor.ExprTags tags = frozenVisual.Tag as MathEditor.ExprTags;
            _medit.TwoFingers = (_firstMouseDown-_secondMouseDown).Length  > ((Pt)e.GetPosition(_medit.Scene) - _firstMouseDown).Length ?
                                MathEditor.TwoFingerOp.Pinching : MathEditor.TwoFingerOp.Stretching;
            if (_secondSelectedExpr != null)
                _medit.dragPairOfTermsTo(frozenVisual, _firstSelectedExpr, _secondSelectedExpr, frozenVisual.Tag as MathEditor.ExprTags, e.GetPosition(_medit.MathUICanvas), e.GetPosition(_medit.MathUICanvas), false);
            else {
                Box exprBox = EWPF.Measure(_firstSelectedExpr.Path[0], tags.FontSize);
                Pt topLeftOfExpr = ((Mat)frozenVisual.RenderTransform.Value) * new Pt();
                // get the bounding Rct for the '=' if it's there
                Box hitBox = EWPF.HitBox.FindBox(_firstSelectedExpr.HitExpr, exprBox);
                Rct hitRct = hitBox == null ? Rct.Null : hitBox.BBoxRefOrigin.Translated((Vec)topLeftOfExpr - (Vec)exprBox.BBoxRefOrigin.TopLeft);
                Vec movement = (Pt)e.GetPosition(_medit.MathUICanvas) - hitRct.Center;
                movement = new Vec(Math.Abs(movement.X), Math.Abs(movement.Y));
                _medit.dragSingleTermTo(frozenVisual, _firstSelectedExpr, frozenVisual.Tag as MathEditor.ExprTags, e.GetPosition(_medit.MathUICanvas),
                    Keyboard.IsKeyDown(Key.LeftShift) ? ((movement.Y > movement.X && movement.Y > 25) ? MathEditor.TwoFingerOp.Graphing : MathEditor.TwoFingerOp.Stretching) : MathEditor.TwoFingerOp.Fixed);
            }
        }

        void mouseMoveOverFrozenExpression(object sender, MouseEventArgs e)
        {
            ContainerVisualHost frozenVisual = sender as ContainerVisualHost;
            MathEditor.ExprTags exprTags = frozenVisual.Tag as MathEditor.ExprTags;

            ContactArea area = frozenVisual.FromPt(e.GetPosition(_medit.MathUICanvas), new Vec(10, 10));

            // then pick a term to factor
            EWPF.HitBox selectedExpr = EWPF.PickExpressionTerm(exprTags.Expr, exprTags.FontSize, area);

            if (selectedExpr != null)
                _medit.hitExprBoundsVisual = _medit.highlightSelectedExprTerm(frozenVisual, selectedExpr, _medit.hitExprBoundsVisual);
        }
    }
}
