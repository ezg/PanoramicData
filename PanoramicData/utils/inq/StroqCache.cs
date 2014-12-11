using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.utils.inq
{
    public class StroqCache
    {
        private static double RESAMPLING_RATE = 120;
        private static Dictionary<Stroq, Stroq> _resampledStroqCache = new Dictionary<Stroq, Stroq>();
        private static Dictionary<Stroq, GeoAPI.Geometries.ILineString> _lineStringCache = new Dictionary<Stroq, GeoAPI.Geometries.ILineString>();
        private static Dictionary<Stroq, SegmentationBound> _segementationBoundCache = new Dictionary<Stroq, SegmentationBound>();

        public static Stroq GetResampledStroq(Stroq stroq)
        {
            if (!_resampledStroqCache.ContainsKey(stroq))
            {
                _resampledStroqCache[stroq] = PtHelpers.Resample(stroq, RESAMPLING_RATE);
            }
            return _resampledStroqCache[stroq];
        }

        public static GeoAPI.Geometries.ILineString GetLineString(Stroq stroq)
        {
            if (!_lineStringCache.ContainsKey(stroq))
            {
                _lineStringCache[stroq] = stroq.GetLineString();
            }
            return _lineStringCache[stroq];
        }

        public static SegmentationBound GetSegmentationBound(Stroq stroq)
        {
            if (!_segementationBoundCache.ContainsKey(stroq))
            {
                _segementationBoundCache[stroq] = getSegmentationBounds(stroq);
            }
            return _segementationBoundCache[stroq];
        }

        public static List<SegmentationBound> GetSegmentationBounds(StroqCollection stroqs)
        {
            List<SegmentationBound> bounds = new List<SegmentationBound>();

            foreach (var stroq in stroqs)
            {
                if (!_segementationBoundCache.ContainsKey(stroq))
                {
                    _segementationBoundCache[stroq] = getSegmentationBounds(stroq);
                }
                bounds.Add(_segementationBoundCache[stroq]);
            }

            return bounds;
        }

        public static void UpdateCache(StroqCollection stroqs)
        {
            foreach (var stroq in stroqs)
            {
                UpdateCache(stroq);
            }
        }

        public static void UpdateCache(Stroq stroq)
        {
            if (_resampledStroqCache.ContainsKey(stroq))
            {
                _resampledStroqCache[stroq] = PtHelpers.Resample(stroq, RESAMPLING_RATE);
            }

            if (_lineStringCache.ContainsKey(stroq))
            {
                _lineStringCache[stroq] = stroq.GetLineString();
            }

            if (_segementationBoundCache.ContainsKey(stroq))
            {
                _segementationBoundCache[stroq] = getSegmentationBounds(stroq);
            }
        }

        private static SegmentationBound getSegmentationBounds(Stroq s)
        {
            IEnumerable<Pt> pts = null;
            if (s.SecondRenderPass)
            {
                List<Pt> ps = new List<Pt>();
                for (int i = 0; i < Math.Min(s.Count, s.SecondRenderPassPressureFactors.Count); i++)
                {
                    if (s.SecondRenderPassPressureFactors[i] == 0)
                    {
                        ps.Add(s[i]);
                    }
                }
                pts = ps;
            }
            else
            {
                pts = s.Select(sp => new Pt(sp.X, sp.Y));
            }

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            foreach (var pt in pts)
            {
                minX = Math.Min(minX, pt.X);
                maxX = Math.Max(maxX, pt.X);
                minY = Math.Min(minY, pt.Y);
                maxY = Math.Max(maxY, pt.Y);
            }

            SegmentationBound bound = new SegmentationBound();
            bound.InflatedBounds = new Rct(minX, minY, maxX, maxY).Inflate(10, 10);
            bound.Bounds = new Rct(minX, minY, maxX, maxY);

            return bound;
        }
    }
}
