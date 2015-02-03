using GeoAPI.Geometries;
using PanoramicData.utils;
using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PanoramicData.view.inq
{
    public class ScribbleGesture : IGesture
    {
        private InkableScene _inkableScene = null;

        public ScribbleGesture(InkableScene inkableScene)
        {
            this._inkableScene = inkableScene;
        }

        private IList<IScribbable> _hitScribbables;
        public IList<IScribbable> HitScribbables
        {
            get { return _hitScribbables; }
        }

        public bool Recognize(InkStroke inkStroke)
        {
            inkStroke = inkStroke.GetResampled(20);
            _hitScribbables = new List<IScribbable>();

            List<int> corners = ShortStraw.IStraw(inkStroke);

            if (corners.Count >= 5)
            {
                IPolygon stoqPoly = inkStroke.GetPolygon();
                ILineString inkStrokeLine = inkStroke.GetLineString();

                IList<Vector2> convexHull = Convexhull.convexhull(inkStroke.Points);
                IGeometry convexHullPoly = convexHull.Select(vec => new Point(vec.X, vec.Y)).ToList().GetPolygon();

                foreach (IScribbable existingInkStroke in _inkableScene.InkStrokes)
                {
                    if (inkStrokeLine.Intersects(existingInkStroke.Geometry))
                    {
                        _hitScribbables.Add(existingInkStroke);
                    }

                    // Check for small exisiting strokes that are completely covered by the scribble.
                    if (!_hitScribbables.Contains(existingInkStroke) && convexHullPoly.Contains(existingInkStroke.Geometry.Buffer(1)))
                    {
                        _hitScribbables.Add(existingInkStroke);
                    }
                }

                foreach (IScribbable existingScribbable in _inkableScene.Elements.Where(e => e is IScribbable))
                {
                    if (inkStrokeLine.Intersects(existingScribbable.Geometry))
                    {
                        _hitScribbables.Add(existingScribbable);
                    }

                    // Check for small exisiting strokes that are completely covered by the scribble.
                    if (!_hitScribbables.Contains(existingScribbable) && convexHullPoly.Contains(existingScribbable.Geometry))
                    {
                        //_hitScribbables.Add(existingScribbable);
                    }
                }
            }

            if (_hitScribbables.Count > 0)
            {
                return true;
            }
            return false;
        }
    }

    public interface IScribbable
    {
        IGeometry Geometry { get; }
    } 
}
