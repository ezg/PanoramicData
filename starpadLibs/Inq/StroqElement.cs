using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using starPadSDK.Geom;
using starPadSDK.Utils;
using System.Windows;
using System.Windows.Media;

namespace starPadSDK.Inq {
    public class StroqElement : FrameworkElement {
        protected DrawingVisual _dv;
        protected Stroq _stroq;
        private bool _hasBeenDrawnBefore = false;
        public Stroq Stroq { get { return _stroq; } }
        public StroqElement(Stroq stroq) {
            _dv = new DrawingVisual();
            _stroq = stroq;
            _stroq.PointChanged += _stroq_PointChanged;
            _stroq.PointsCleared += _stroq_PointsCleared;
            _stroq.PointsModified += _stroq_PointsModified;
            _stroq.VisibilityChanged += _stroq_VisibilityChanged;
            _stroq.BackingStroke.DrawingAttributes.AttributeChanged += new System.Windows.Ink.PropertyDataChangedEventHandler(DrawingAttributes_AttributeChanged);
            Redraw();
            AddVisualChild(_dv);
            IsHitTestVisible = false;
        }

        void DrawingAttributes_AttributeChanged(object sender, System.Windows.Ink.PropertyDataChangedEventArgs e) 
        {
            Redraw();
        }

        protected void _stroq_PointsModified(Stroq s, Mat? m) {
            if (m != null && _hasBeenDrawnBefore)
            {
                Mat oldMat = ((MatrixTransform)this.RenderTransform).Matrix;
                Mat newMat = oldMat * (Mat)m;
                this.RenderTransform = new MatrixTransform(newMat);
            }
            else
            {
                Redraw();
            }
        }
        protected void _stroq_PointsCleared(Stroq s) 
        {
            Redraw();
        }
        protected void _stroq_PointChanged(Stroq s, int i) 
        {
            Redraw();
        }

        protected void _stroq_VisibilityChanged(Stroq s)
        {
            Redraw();
        }

        protected override int VisualChildrenCount { get { return 1; } }
        protected override Visual GetVisualChild(int index) {
            if(index != 0) throw new ArgumentOutOfRangeException("index", "StroqElements only have one visual child");
            return _dv;
        }

        protected void Redraw() {
            this.RenderTransform = new MatrixTransform(Matrix.Identity);

            DrawingContext dc = _dv.RenderOpen();
            if (_stroq.Visible)
            {
                //dc.DrawRectangle(Brushes.Aqua, new Pen(Brushes.Black, 1), _stroq.GetBounds());
                _hasBeenDrawnBefore = true;
                _stroq.BackingStroke.Draw(dc);
                if (_stroq.SecondRenderPass)
                {
                    Stroq secondRendering = _stroq.Clone();
                    secondRendering.BackingStroke.DrawingAttributes.Color = _stroq.SecondRenderPassColor;
                    for (int i = 0; i < Math.Min(secondRendering.Count, _stroq.SecondRenderPassPressureFactors.Count); i++)
                    {
                        var sp = secondRendering.StylusPoints[i];
                        sp.PressureFactor = _stroq.SecondRenderPassPressureFactors[i];
                        secondRendering.StylusPoints[i] = sp;
                    }
                    secondRendering.BackingStroke.Draw(dc);
                }
            }
            dc.Close();
        }
    }
}
