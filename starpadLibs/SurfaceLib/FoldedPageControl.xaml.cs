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

namespace starPadSDK.SurfaceLib
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class FoldedPageControl : UserControl
    {
        public FoldedPageControl(double width)
        {
            InitializeComponent();

            Rectangle vRect = new Rectangle();
            vRect.Width = width;
            vRect.Height = this.Height;

            LinearGradientBrush vGradient = new LinearGradientBrush();

            vGradient.StartPoint = new Point(0.5, 0);
            vGradient.EndPoint = new Point(0.5, 1);

            vGradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            vGradient.GradientStops.Add(new GradientStop(Colors.LightGray, 0.5));
            vGradient.GradientStops.Add(new GradientStop(Colors.White, 1.0));

            vRect.Fill = vGradient;

            canvas.Children.Add(vRect);
        }

        public object fold { get; set; }
    }
}
