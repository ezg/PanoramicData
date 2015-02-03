using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PanoramicData.view.inq
{
    public interface IStroqConsumer
    {
        Color StrokeColor { get; }

        FrameworkElement Element { get; }

        void Consume(Stroq stroq, List<IStroqConsumer> allConsumers);
    }
}
