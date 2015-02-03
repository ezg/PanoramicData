using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PanoramicData.view.inq
{
    public class InkStrokeElement : FrameworkElement
    {
        protected DrawingVisual _dv;

        protected InkStroke _inkStroke;
        public InkStroke InkStroke { get { return _inkStroke; } }

        public InkStrokeElement(InkStroke inkStroke)
        {
            _dv = new DrawingVisual();
            _inkStroke = inkStroke;
            _inkStroke.Points.CollectionChanged += Points_CollectionChanged;
            Redraw();
            AddVisualChild(_dv);
            IsHitTestVisible = false;
        }

        void Points_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Redraw();
        }

        protected override int VisualChildrenCount { get { return 1; } }
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException("index", "InkStrokeElements only have one visual child");
            return _dv;
        }

        protected void Redraw()
        {
            this.RenderTransform = new MatrixTransform(Matrix.Identity);

            DrawingContext dc = _dv.RenderOpen();

            Pen pen = new Pen(Brushes.Black, 3);
            for (int i = 0; i < _inkStroke.Points.Count - 1; ++i)
            {
                Point s0 = _inkStroke.Points[i];
                Point s1 = _inkStroke.Points[i + 1];

                dc.DrawLine(pen, s0, s1);
            }
            
            dc.Close();
        }
    }
}
