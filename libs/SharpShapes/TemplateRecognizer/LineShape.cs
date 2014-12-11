using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;
using starPadSDK.Inq;
using ShapeRecognizer;
using starPadSDK.Geom;

namespace TemplateRecognizer
{
    public class LineShape : BaseShape
    {
        public LineShape(Stroq stroq, ShapeType shapeType)
            : base(stroq, shapeType)
        {
            Coordinate[] coords = new Coordinate[stroq.Count];

            int i = 0;
            foreach (Pt pt in stroq)
            {
                coords[i] = new Coordinate(pt.X, pt.Y);
                i++;
            }

            Geometry = new LineString(coords);
        }
    }
}
