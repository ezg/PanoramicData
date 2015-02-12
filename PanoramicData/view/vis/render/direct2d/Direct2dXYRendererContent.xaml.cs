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

namespace PanoramicData.view.vis.render.direct2d
{
    /// <summary>
    /// Interaction logic for Direct2dXYRendererContent.xaml
    /// </summary>
    public partial class Direct2dXYRendererContent : XYRendererContent
    {
        private XYScene _scene = null;
        public XYScene Scene
        {
            get
            {
                return _scene;
            }
            set
            {
                _scene = value;
                d2DControl.Scene = _scene;
            }
        }

        public Direct2dXYRendererContent()
        {
            InitializeComponent();
        }

        public override void Render(List<XYDataPoint> dataPoints, Dictionary<string, XYDataPointSeries> series)
        {
            if (_scene != null)
            {
                _scene.Render(dataPoints, series);
            }
        }
    }
}
