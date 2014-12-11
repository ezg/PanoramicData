using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;
using starPadSDK.Inq;
using ShapeRecognizer;

namespace TemplateRecognizer
{
    public class BaseShape
    {
        public Geometry Geometry;
        public Stroq Stroq;

        protected ShapeType _shapeType;

        public BaseShape(Stroq stroq, ShapeType shapeType)
        {
            this.Stroq = stroq;
            this._shapeType = shapeType;
        }
    }
}
