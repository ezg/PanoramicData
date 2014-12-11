using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.utils.inq
{
    public interface StroqListener
    {
        void NotifyStroqAdded(Stroq s);

        void NotifyStroqRemoved(Stroq s);

        void NotifyStroqsRemoved(StroqCollection sc);

        void NotifyStroqsAdded(StroqCollection sc);
    }
}
