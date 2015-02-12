using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PanoramicData.view.vis.render
{
    public class XYRendererContent : UserControl
    {
        public virtual void Render(List<XYDataPoint> _dataPoints, Dictionary<string, XYDataPointSeries> _series)
        {
        }
    }
}
