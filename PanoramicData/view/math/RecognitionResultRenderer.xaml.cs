using PanoramicData.utils.inq;
using starPadSDK.MathExpr;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.WPFHelp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PanoramicData.view.math
{
    public partial class RecognitionResultRenderer : UserControl
    {
        public RecognitionResultRenderer()
        {
            InitializeComponent();
        }

        public void Display(Expr toRender, bool numeric, double width, double height)
        {
            //grid.Children.Clear();
            Color color = Colors.Black;
            ContainerVisualHost cvh = EWPF.ToVisual(FormulaEvaluator.GetDisplayableExpr(toRender), 18, numeric ? Colors.Black : Colors.Blue, null, EWPF.DrawTop);

            this.Width = Math.Min(width, cvh.Width + 6);
            this.Height = Math.Min(height, cvh.Height + 6);

            double scale = Math.Min(1.0, Math.Min(this.Width / (cvh.Width + 6), this.Height / (cvh.Height + 6)));
            Matrix matrix = Matrix.Identity;
            matrix.Scale(scale, scale);
            cvh.RenderTransform = new MatrixTransform(matrix);

            //grid.Children.Add(cvh);
        }
    }
}
