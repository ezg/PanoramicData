using PanoramicData.view.Direct2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2D = Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

namespace PanoramicData.view.vis.render.direct2d
{
    public class XYScene : Scene
    {
        public virtual void Render(List<XYDataPoint> _dataPoints, Dictionary<string, XYDataPointSeries> _series) { }
    }
}
