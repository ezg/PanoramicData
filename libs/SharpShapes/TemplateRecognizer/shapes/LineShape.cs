using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;
using starPadSDK.Inq;
using starPadSDK.Geom;
using BrownRecognitionCommon;

namespace TemplateRecognizer
{
    public class LineShape : BaseShape
    {
        public LineShape(BrownShape brownShape)
            : base(brownShape)
        {
            Coordinate[] coords;
            if (BrownShape.BrownInputStrokes.Count == 1)
            {
                coords = new Coordinate[BrownShape.BrownInputStrokes[0].StrokePoints.Length];

                int i = 0;
                foreach (System.Windows.Point pt in BrownShape.BrownInputStrokes[0].StrokePoints)
                {
                    coords[i] = new Coordinate(pt.X, pt.Y);
                    i++;
                }
            }
            else
            {
                coords = new Coordinate[BrownShape.ShapePoints.Length];
                for (int i = 0; i < BrownShape.ShapePoints.Length; i++)
                {
                    coords[i] = new Coordinate(BrownShape.ShapePoints[i].X, BrownShape.ShapePoints[i].Y);
                }
            }

            Geometry = new LineString(coords);
        }
    }
}
