using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PanoramicData.utils
{
    public class HitTester
    {
        private List<DependencyObject> _hitTestResults = new List<DependencyObject>();
        private List<Type> _results;
        Dictionary<Type, HitTestFilterBehavior> _filters;

        public List<DependencyObject> GetHits(FrameworkElement root, Rect bounds, List<Type> results, Dictionary<Type, HitTestFilterBehavior> filters)
        {
            _hitTestResults.Clear();
            _results = results;
            _filters = filters;

            RectangleGeometry rectGeo = new RectangleGeometry(bounds);

            VisualTreeHelper.HitTest(root,
                new HitTestFilterCallback(hitTestFilter),
                new HitTestResultCallback(hitTestResult),
                new GeometryHitTestParameters(rectGeo));

            return _hitTestResults;
        }

        private HitTestResultBehavior hitTestResult(HitTestResult result)
        {
            foreach (var r in _results)
            {
                if (r.IsAssignableFrom(result.VisualHit.GetType()))
                {
                    if (result.VisualHit is FrameworkElement &&
                        (result.VisualHit as FrameworkElement).IsHitTestVisible)
                    {
                        _hitTestResults.Add(result.VisualHit);
                    }
                }

            }
            return HitTestResultBehavior.Continue;
        }

        private HitTestFilterBehavior hitTestFilter(DependencyObject o)
        {
            foreach (var f in _filters)
            {
                if (f.Key.IsAssignableFrom(o.GetType()))
                {
                    return f.Value;
                }
            }
            return HitTestFilterBehavior.Continue;
        }
    }
}
