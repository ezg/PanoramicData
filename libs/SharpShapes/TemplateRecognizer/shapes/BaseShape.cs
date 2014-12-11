using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;
using starPadSDK.Inq;
using BrownRecognitionCommon;

namespace TemplateRecognizer
{
    public class BaseShape
    {
        public Geometry Geometry;
        public BrownShape BrownShape;

        public BaseShape(BrownShape brownShape)
        {
            this.BrownShape = brownShape;
        }
    }
}
