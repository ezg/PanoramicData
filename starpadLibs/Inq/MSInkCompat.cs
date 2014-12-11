using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using starPadSDK.Geom;
using starPadSDK.Utils;
using Microsoft.Ink;
using starPadSDK.Inq;
using System.Runtime.Serialization;
using System.Drawing;

namespace starPadSDK.Inq.MSInkCompat {
    public class OldStrokeStuff {
        private static Guid CacheGuid = new Guid("{4766C0C5-7E42-4430-A13F-C972D1128B3C}");
        public const double Scale = 100;
        /// <summary>
        /// Only to be called if you don't want the result cached, otherwise do stroq.OldStroke().
        /// </summary>
        public static Stroke Convert(Stroq ins, Ink ink) {
            if(ink == null) ink = new Ink();
            return ink.CreateStroke(ins.Select((p) =>
            {
                Vec r = ((Vec)p)*Scale;
                return new System.Drawing.Point((int)(r.X+0.5), (int)(r.Y+0.5));
            }).ToArray());
        }
        // Arguably, this class doesn't need to be clonable, but it made a test case for cloning/serialization support
        [Serializable]
        protected class Cache : ICloneable, Stroq.IStroqCloneable {
            protected bool _dirty = true;
            protected Stroke _cached = null;
            protected Stroq _owner;
            public Cache(Stroq sq) {
                _owner = sq;
                _owner.PointChanged += (s, ix) => _dirty = true;
                _owner.PointsCleared += (s) => _dirty = true;
                _owner.PointsModified += (s, m) => _dirty = true;
            }
            public Stroke Cached() { return _dirty ? Convert() : _cached; }
            protected Stroke Convert() {
                _cached = OldStrokeStuff.Convert(_owner, null);
                _dirty = false;
                return _cached;
            }
            
            public object Clone() {
                Cache c = new Cache(_owner); // _owner will be fixed up in AfterClone
                c._dirty = _dirty;
                if(_cached != null) {
                    Ink ink = new Ink();
                    c._cached = ink.CreateStroke(_cached.GetPoints());
                }
                return c;
            }
            public void AfterClone(Stroq oldStroq, Stroq newStroq) {
                _owner = newStroq;
            }

            [OnDeserialized] //only takes effect if this class is marked as serializable, which, for now, seems like the wrong thing to do.
            private void onDeserializedFixEvents(StreamingContext sc) {
                _owner.PointChanged += (s, ix) => _dirty = true;
                _owner.PointsCleared += (s) => _dirty = true;
                _owner.PointsModified += (s, m) => _dirty = true;
            }
        }
        public static Microsoft.Ink.Stroke TheStroke(Stroq s) {
            object val;
            Cache c = s.Property.TryGetValue(CacheGuid, out val) ? (Cache)val : new Cache(s);
            return c.Cached();
        }
    }
    public static class Extensions {
        public static Stroke OldStroke(this Stroq s) {
            return OldStrokeStuff.TheStroke(s);
        }
        public static Stroke OldStroke(this Stroq s, Ink ink) {
            return OldStrokeStuff.Convert(s, ink);
        }
        public static int[] OldCusps(this Stroq s) {
            return s.OldStroke().PolylineCusps;
        }
        public static float[] OldSelfIntersections(this Stroq s) {
            return s.OldStroke().SelfIntersections;
        }
        public static float OldNearestPoint(this Stroq s, Pt p, out float dist) {
            float tval = s.OldStroke().NearestPoint(new Point((int)(p.X*OldStrokeStuff.Scale + 0.5), (int)(p.Y*OldStrokeStuff.Scale + 0.5)), out dist);
            dist /= (float)OldStrokeStuff.Scale;
            return tval;
        }
        public static float OldNearestPoint(this Stroq s, Pt p) {
            return s.OldStroke().NearestPoint(new Point((int)(p.X*OldStrokeStuff.Scale + 0.5), (int)(p.Y*OldStrokeStuff.Scale + 0.5)));
        }
        public static int[] OldPolylineCusps(this Stroq s) {
            return s.OldStroke().PolylineCusps;
        }
        /// <summary>
        /// Note: this should be expected to be significantly slower because it has to convert all the strokes into the same Ink before it can
        /// run the old FindIntersections().
        /// </summary>
        public static float[] OldFindIntersections(this Stroq s, StroqCollection ss) {
            Stroke oldStroke = s.OldStroke();
            Ink ink = oldStroke.Ink;
            List<int> ids = new List<int>();
            foreach (Stroq st in ss)
                if (st != s)
                    ids.Add(st.OldStroke(ink).Id);
            return oldStroke.FindIntersections(ink.CreateStrokes(ids.ToArray()));
        }
        public static IEnumerable<Pt> OldGetFlattenedBezierPoints(this Stroq s) {
            return s.OldStroke().GetFlattenedBezierPoints().Select((Point p) => new Pt(p.X/OldStrokeStuff.Scale, p.Y/OldStrokeStuff.Scale));
        }
    }
    /// <summary>
    /// This is primarily for internal use. You *must* dispose of it properly (call Dispose() or wrap in using(StroqInkMapper sim = new Str..pper(ss)) {} )
    /// to prevent memory leaks.
    /// </summary>
    public class StroqInkMapper : IDisposable {
        private Ink _ink = new Ink();
        public Ink Ink { get { return _ink; } }
        public StroqCollection Stroqs { get; private set; }
        private Dictionary<Stroq, Stroke> _map = new Dictionary<Stroq, Stroke>();
        private Dictionary<int, Stroq> _map2 = new Dictionary<int, Stroq>();
        public Stroke this[Stroq s] { get { return _map[s]; } }
        public Stroq this[Stroke s] { get { return _map2[s.Id]; } }
        public Stroq this[int id] { get { return _map2[id]; } }
        public Strokes this[IEnumerable<Stroq> sc] { get { return Ink.CreateStrokes(sc.Select((Stroq s) => this[s].Id).ToArray()); } }
        /// <summary>
        /// You can pass a Strokes object in to this method, or an IEnumerable&lt;Stroke&gt;; both are castable to this type.
        /// </summary>
        public IEnumerable<Stroq> this[System.Collections.IEnumerable ss] { get { return ss.Cast<Stroke>().Select((Stroke s) => this[s]); } }
        private bool _disposed = false;
        /// <summary>
        /// Remember, you must dispose of this properly by calling Dispose() when you're done with it or wrapping it in a using() call.
        /// </summary>
        /// <param name="stroqs"></param>
        public StroqInkMapper(StroqCollection stroqs) {
            Stroqs = stroqs;
            stroqs.Changed += stroqs_Changed;
            StroqsAdded(stroqs);
        }
        public void Dispose() {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization (destructor) code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing) {
            if(!_disposed) {
                if(disposing) {
                    // Dispose managed resources.
                    Stroqs.Changed -= stroqs_Changed;
                    DeleteStrokes(Stroqs); // detach callbacks from all the stroqs
                    // make sure further operations die
                    _ink = null;
                    Stroqs = null;
                    _map = null;
                    _map2 = null;
                }
                // Dispose unmanaged resources--but there are none for this class, so no code here.

                _disposed = true;
            }
        }
        // This destructor will run only if the Dispose method does not get called.
        // Do not provide destructors in types derived from this class.
        ~StroqInkMapper() {
            Dispose(false);
        }

        private void stroqs_Changed(object sender, StroqCollection.ChangedEventArgs e) {
            switch(e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    StroqsAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    StroqsDeleted(e.OldItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    if(StrokesDeleting != null) StrokesDeleting(_ink.Strokes);
                    _ink.DeleteStrokes();
                    _map.Clear();
                    _map2.Clear();

                    if(e.NewItems != null) StroqsAdded(e.NewItems);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void StroqsDeleted(IEnumerable<Stroq> stroqs) {
            if(StrokesDeleting != null) StrokesDeleting(this[stroqs]);
            DeleteStrokes(stroqs);
        }
        private void DeleteStrokes(IEnumerable<Stroq> stroqs) {
            foreach(Stroq sq in stroqs) {
                sq.PointChanged -= StroqChanged;
                sq.PointsCleared -= StroqChanged;
                sq.PointsModified -= StroqChanged;
                _map2.Remove(_map[sq].Id);
                _ink.DeleteStroke(_map[sq]);
                _map.Remove(sq);
            }
        }
        private void StroqsAdded(IEnumerable<Stroq> stroqs) {
            AddStrokes(stroqs);
            if(StrokesAdded != null) StrokesAdded(this[stroqs]);
        }
        private void AddStrokes(IEnumerable<Stroq> stroqs) {
            foreach(Stroq sq in stroqs) {
                Stroke s = sq.OldStroke(_ink);
                _map.Add(sq, s);
                _map2.Add(s.Id, sq);
                sq.PointChanged += StroqChanged;
                sq.PointsCleared += StroqChanged;
                sq.PointsModified += StroqChanged;
            }
        }

        private void StroqChanged(Stroq s, Mat? m) {
            if(!m.HasValue) StroqChanged(s);
            else {
                Stroke ms = _map[s];
                System.Drawing.Drawing2D.Matrix mm = new System.Drawing.Drawing2D.Matrix(
                    (float)m.Value[0, 0],
                    (float)m.Value[0, 1],
                    (float)m.Value[1, 0],
                    (float)m.Value[1, 1],
                    (float)(m.Value[0, 2]*OldStrokeStuff.Scale),
                    (float)(m.Value[1, 2]*OldStrokeStuff.Scale));
                ms.Transform(mm);
                if(StrokeTransformed != null) StrokeTransformed(ms, mm);
            }
        }
        private void StroqChanged(Stroq s) {
            if(StrokesDeleting != null) StrokesDeleting(Ink.CreateStrokes(new int[] { this[s].Id }));
            _map2.Remove(_map[s].Id);
            _ink.DeleteStroke(_map[s]);
            Stroke st = s.OldStroke(Ink);
            _map[s] = st;
            _map2.Add(st.Id, s);
            if(StrokesAdded != null) StrokesAdded(Ink.CreateStrokes(new int[] { st.Id }));
        }
        private void StroqChanged(Stroq s, int i) {
            StroqChanged(s);
        }
        public event Action<Strokes> StrokesAdded;
        public event Action<Strokes> StrokesDeleting;
        public event Action<Stroke, System.Drawing.Drawing2D.Matrix> StrokeTransformed;
    }
}
