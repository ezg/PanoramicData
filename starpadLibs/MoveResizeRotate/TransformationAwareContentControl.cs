using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using starPadSDK.Geom;

namespace DiagramDesigner
{
    public interface TransformationAwareContentControl
    {
        void PreTransformation();
        void PostTransformation();

        void NotifyMove(Pt delta);
        void NotifyScale(Vec delta, Vec offset);
        void NotifyRotate(double delta);
        void NotifyInteraction();
    }
}
