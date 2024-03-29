﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;
using starPadSDK.Inq;
using starPadSDK.Geom;
using BrownRecognitionCommon;

namespace TemplateRecognizer
{
    public class PolygonShape : BaseShape
    {
        public PolygonShape(BrownShape brownShape)
            : base(brownShape)
        {
            Coordinate[] coords;
            if (BrownShape.BrownInputStrokes.Count == 1)
            {
                coords = new Coordinate[BrownShape.BrownInputStrokes[0].StrokePoints.Length + 1];

                int i = 0;
                foreach (System.Windows.Point pt in BrownShape.BrownInputStrokes[0].StrokePoints)
                {
                    coords[i] = new Coordinate(pt.X, pt.Y);
                    i++;
                }
                coords[i] = new Coordinate(BrownShape.BrownInputStrokes[0].StrokePoints[0].X, BrownShape.BrownInputStrokes[0].StrokePoints[0].Y);
            }
            else
            {
                coords = new Coordinate[BrownShape.ShapePoints.Length];
                for (int i = 0; i < BrownShape.ShapePoints.Length; i++)
                {
                    coords[i] = new Coordinate(BrownShape.ShapePoints[i].X, BrownShape.ShapePoints[i].Y);
                }
            }


            LinearRing ring = new LinearRing(coords);
            Geometry = new Polygon(ring);
        }
    }
}
