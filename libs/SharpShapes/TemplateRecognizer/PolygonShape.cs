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
    public class PolygonShape : BaseShape
    {
        public PolygonShape(Stroq stroq, ShapeType shapeType)
            : base(stroq, shapeType)
        {
            Coordinate[] coords = new Coordinate[stroq.Count + 1];

            int i = 0;
            foreach (Pt pt in stroq)
            {
                coords[i] = new Coordinate(pt.X, pt.Y);
                i++;
            }
            coords[i] = new Coordinate(stroq.First().X, stroq.First().Y);

            LinearRing ring = new LinearRing(coords);

            Geometry = new Polygon(ring);
        }
    }
}
