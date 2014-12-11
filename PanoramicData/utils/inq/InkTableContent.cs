using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Collections;
using starPadSDK.Utils;

namespace PanoramicData.utils.inq
{
    public class InkTableContent
    {
        private static Dictionary<object, InkTableContent> _cache = new Dictionary<object, InkTableContent>();

        /// <summary>
        /// Static method to get an InkTableContent from a stroq. Uses caching. 
        /// </summary>
        /// <param name="stroq"></param>
        /// <returns></returns>
        public static InkTableContent GetInkTableContent(Stroq stroq)
        {
            if (!_cache.ContainsKey(stroq))
            {
                _cache[stroq] = new InkTableContent(stroq);
            }
            return _cache[stroq];
        }

        /// <summary>
        /// Static method to get an InkTableContent from a FrameworkElement. Uses caching. 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static InkTableContent GetInkTableContent(FrameworkElement e)
        {
            if (e is InkTableValueRenderer)
            {
                if (!_cache.ContainsKey(e))
                {
                    _cache[e] = new InkTableContent(e);
                }
                return _cache[e];
            }
            else
            {
                return null;
            }
        }

        public object Object { get; set; }
        public PanoramicData.utils.inq.Label Label { get; set; }
        public List<InkTableContentGroup> InkTableContentGroups = new List<InkTableContentGroup>();
        private SegmentationBound _frameworkElementSegmentationBound = null;

        private InkTableContent()
        {
            InkTableContentGroups = new List<InkTableContentGroup>();
        }

        private InkTableContent(Stroq stroq)
        {
            InkTableContentGroups = new List<InkTableContentGroup>();
            Object = stroq;
        }

        private InkTableContent(FrameworkElement e)
        {
            InkTableContentGroups = new List<InkTableContentGroup>();

            e.UpdateLayout();
            GeneralTransform trans = e.TransformToAncestor((FrameworkElement)e.Parent);
            Rct result = trans.TransformBounds(new Rect(new Point(0, 0), new Size(Math.Min(e.Width, e.ActualWidth), Math.Min(e.Height, e.ActualHeight))));
            _frameworkElementSegmentationBound = new SegmentationBound();
            _frameworkElementSegmentationBound.Bounds = result;
            _frameworkElementSegmentationBound.InflatedBounds = _frameworkElementSegmentationBound.Bounds.Inflate(10, 10);

            Object = e;
        }

        public void Move(Vec moveVec)
        {
            if (Object is Stroq)
            {
                (Object as Stroq).Move(moveVec);
            }
        }

        public void XformBy(Mat mat)
        {
            if (Object is Stroq)
            {
                (Object as Stroq).XformBy(mat);
            }
        }

        public InkTableContent Clone()
        {
            if (Object is Stroq)
            {
                return GetInkTableContent(((Stroq)Object).Clone());
            }
            return null;
        }

        public void ChangeColor(Color c)
        {
            if (Object is Stroq)
            {
                ((Stroq)Object).BackingStroke.DrawingAttributes.Color = c;
            }
        }

        public void UpdateCache()
        {
            if (Object is Stroq)
            {
                StroqCache.UpdateCache((Object as Stroq));
            }
        }

        public SegmentationBound SegmentationBound
        {
            get
            {
                if (Object is Stroq)
                {
                    return StroqCache.GetSegmentationBound((Stroq)Object);
                }
                else if (Object is FrameworkElement)
                {
                    return _frameworkElementSegmentationBound;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public class InkTableContentCollection : ICollection<InkTableContent>, ICloneable
    {
        private HashSet<InkTableContent> _inkTableContents;
        public InkTableContentCollection() { _inkTableContents = new HashSet<InkTableContent>(); }
        public InkTableContentCollection(IEnumerable<InkTableContent> cs) { _inkTableContents = new HashSet<InkTableContent>(cs); }
        private InkTableContentCollection(HashSet<InkTableContent> cs) { _inkTableContents = cs; }

        public InkTableContentCollection Clone()
        {
            return new InkTableContentCollection(_inkTableContents.Select((InkTableContent c) => c.Clone()));
        }

        public void Move(Vec moveVec)
        {
            _inkTableContents.ForEach((c, i) => c.Move(moveVec));
        }

        public void UpdateCache()
        {
            _inkTableContents.ForEach((c, i) => c.UpdateCache());
        }

        //ICollection<Stroq> Members
        public bool Add(InkTableContent item)
        {
            return _inkTableContents.Add(item);
        }
        void ICollection<InkTableContent>.Add(InkTableContent item) { Add(item); }
        public void Clear()
        {
            _inkTableContents.Clear();
        }
        public bool Contains(InkTableContent item) { return _inkTableContents.Contains(item); }
        public void CopyTo(InkTableContent[] array, int arrayIndex) { _inkTableContents.CopyTo(array, arrayIndex); }
        public int Count { get { return _inkTableContents.Count; } }
        public bool IsReadOnly { get { return false; } }
        public bool Remove(InkTableContent item)
        {
            return _inkTableContents.Remove(item);
        }

        // IEnumerable<Stroq> Member
        public IEnumerator<InkTableContent> GetEnumerator() { return _inkTableContents.GetEnumerator(); }

        // IEnumerable Member
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        // ICloneable Member
        object ICloneable.Clone() { return Clone(); }

        // Extra members to support more, especially based on hashset
        public bool Remove(IEnumerable<InkTableContent> cs)
        {
            List<InkTableContent> removed = new List<InkTableContent>(cs.Where((InkTableContent c) => _inkTableContents.Remove(c)));
            if (removed.Count > 0)
            {
                return true;
            }
            else return false;
        }
        public bool Add(IEnumerable<InkTableContent> cs)
        {
            List<InkTableContent> added = new List<InkTableContent>(cs.Where((InkTableContent c) => _inkTableContents.Add(c)));
            if (added.Count > 0)
            {
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Static method to get an InkTableContents from a StroqCollection. 
        /// </summary>
        /// <param name="stroqs"></param>
        /// <returns></returns>
        public static InkTableContentCollection GetInkTableContentCollection(StroqCollection stroqs)
        {
            InkTableContentCollection ret = new InkTableContentCollection();
            foreach (var stroq in stroqs)
            {
                ret.Add(InkTableContent.GetInkTableContent(stroq));
            }
            return ret;
        }

        /// <summary>
        /// Static method to get an InkTableContents from an array of FrameworkElements. 
        /// </summary>
        /// <param name="stroqs"></param>
        /// <returns></returns>
        public static InkTableContentCollection GetInkTableContentCollection(FrameworkElement[] es)
        {
            InkTableContentCollection ret = new InkTableContentCollection();
            foreach (var e in es)
            {
                var c = InkTableContent.GetInkTableContent(e);
                if (c != null)
                {
                    ret.Add(InkTableContent.GetInkTableContent(e));
                }
            }
            return ret;
        }

        public StroqCollection GetStroqs()
        {
            StroqCollection stroqs = new StroqCollection();
            foreach (var c in _inkTableContents)
            {
                if (c.Object is Stroq)
                {
                    stroqs.Add((Stroq)c.Object);
                }
            }
            return stroqs;
        }

        public List<FrameworkElement> GetFrameworkElements()
        {
            List<FrameworkElement> elems = new List<FrameworkElement>();
            foreach (var c in _inkTableContents)
            {
                if (c.Object is FrameworkElement)
                {
                    elems.Add((FrameworkElement)c.Object);
                }
            }
            return elems;
        }
    }

    public class InkTableContentGroup
    {
        public InkTableContentCollection InkTableContents { get; set; }

        public InkTableContentGroup()
        {
            InkTableContents = new InkTableContentCollection();
        }
    }

    public class SegmentationBound
    {
        public Rct Bounds { get; set; }
        public Rct InflatedBounds { get; set; }
    }

    public static class InkTableContentExtension
    {
        public static void AddInkTableContent(this InqScene inqScene, InkTableContentCollection coll)
        {
            inqScene.AddWithUndo(coll.GetFrameworkElements());
            inqScene.AddWithUndo(coll.GetStroqs());
        }

        public static void RemoveInkTableContent(this InqScene inqScene, InkTableContentCollection coll)
        {
            inqScene.Rem(coll.GetFrameworkElements());
            inqScene.Rem(coll.GetStroqs());
        }
    }

    public interface InkTableValueRenderer
    {
    }
}
