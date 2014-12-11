using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Ink;
using starPadSDK.Geom;
using starPadSDK.Utils;
using System.Windows.Input;
using System.Runtime.Serialization;
using System.IO;
using System.Windows.Media;

namespace starPadSDK.Inq {
    // INPROG: then continue propagating use of Stroq down and out through dostrokecollected, Featurepoints, etc.
    // TODO: drawing
    /// <summary>
    /// Serializing this will serialize all and only the properties specified which are serializable. Events are not serialized, so be sure any property
    /// you implement that uses them defines an [OnDeserialized] method to fix them up.
    /// Cloning will work similarly, only testing if the property is clonable. Iteration over the Stroq is over the Pts;
    /// see the StylusPoints, Property, and PressurePoints.
    /// </summary>
    [Serializable]
    public class Stroq : IList<Pt>, ICloneable {
        [NonSerialized]
        private Stroke _backingStroke;
        // The shadow is used during (de)serialization to store a serialized version of the stroke using StrokeCollection.
        private MemoryStream _backingStrokeShadow = null;
        /// <summary>
        /// The System.Windows.Ink.stroke backing this Stroq. Your best bet is to treat StylusPoints as read-only.
        /// Certainly, don't modify the stroke points and
        /// expect the Stroq to have any idea what you've done (ie, Stroq properties etc will get out of sync).
        /// </summary>
        public Stroke BackingStroke { get { return _backingStroke; } }
        private StylusPointCollection _pts { get { return _backingStroke.StylusPoints; } }
        public delegate void PointChangeHandler(Stroq s, int i);
        [field:NonSerialized]
        public event PointChangeHandler PointChanged;
        /// <summary>
        /// increased by one every time a change is made by the lowest level changers--ie, ignoring BatchEditing
        /// </summary>
        [NonSerialized]
        private ulong _changeSeq = 0;
        private bool BatchEditing { get { return _batchLevel > 0; } }
        [NonSerialized]
        private int _batchLevel = 0;
        
        /// <summary>
        /// Allows multiple changes to the stroq points to be made without firing any callbacks until the end.
        /// In the case of nested BatchEdits, there is only one callback made, at the end of the top level BatchEdit.
        /// The callback is made only if changes to the points were made. If an exception is thrown from the delegate after a change has been made
        /// <em>the callback will still be made</em> on the way up the stack from the exception. Attempts to serialize a stroke in the middle of a batch
        /// edit will raise an exception.
        /// </summary>
        /// <param name="fn">Lambda functions should work well here.</param>
        public void BatchEdit(Action fn) { BatchEdit((Stroq s) => fn()); }
        /// <summary>
        /// Allows multiple changes to the stroq points to be made without firing any callbacks until the end. The delegate given here will be given the
        /// stroq this is called on as an argument. In the case of nested BatchEdits, there is only one callback made, at the end of the top level BatchEdit.
        /// The callback is made only if changes to the points were made. If an exception is thrown from the delegate after a change has been made
        /// <em>the callback will still be made</em> on the way up the stack from the exception. Attempts to serialize a stroke in the middle of a batch
        /// edit will raise an exception.
        /// </summary>
        /// <param name="fn">Lambda functions should work well here.</param>
        public void BatchEdit(Action<Stroq> fn) { using(var x = BatchEdit()) fn(this); }
        /// <summary>
        /// Allows multiple changes to the stroq points to be made without firing any callbacks until the end. You must call Dispose() on the value
        /// returned from this method to allow callbacks again. In the case of nested BatchEdits,
        /// there is only one callback made, at the end of the top level BatchEdit. The callback is made only if changes to the points were made.
        /// Attempts to serialize a stroke in the middle of a batch edit will raise an exception. You can either store the return value somewhere and
        /// call Dispose() on it directly, if your lock is over a period of time, or wrap your use in a using() block
        /// like so: using(var xxx = &lt;your stroq&gt;.BatchEdit()) { &lt;your code goes here&gt; } (though you can name the variable anything instead of xxx).
        /// </summary>
        public BatchLock BatchEdit() { return new BatchLock(new BatchEditLockProxy(this), () => { if(PointsModified != null) PointsModified(this, null); }); }
        private class BatchEditLockProxy : IBatchLockable {
            public int BatchLevel { get { return _s._batchLevel; } set { _s._batchLevel = value; } }
            public ulong ChangeSeq { get { return _s._changeSeq; } }
            public string Name { get { return "Stroq"; } }
            private Stroq _s;
            public BatchEditLockProxy(Stroq s) { _s = s; }
        }

        /// <summary>
        /// The ixth point of the Stroq. As in Perl, negative indices count backwards from the end: poly[-1] is the last point of poly, poly[-2] the
        /// next-to-last point, etc.
        /// </summary>
        public Pt this[int ix] {
            get {
                return _pts[ix < 0 ? _pts.Count+ix : ix].ToPoint();
            }
            set {
                ix = ix < 0 ? _pts.Count+ix : ix;
                /* I think it's a language bug that I can't just say:
                _pts[ix].X = value.X;
                _pts[ix].Y = value.Y;
                 */
                StylusPoint sp = _pts[ix];
                sp.X = value.X;
                sp.Y = value.Y;
                _pts[ix] = sp;

                _changeSeq++;
                if(PointsModified != null && !BatchEditing) PointsModified(this, null);
            }
        }
        /// <summary>
        /// Transforms the stroke points by the specified matrix. At most one callback (PointsModified) is made.
        /// </summary>
        /// <param name="m"></param>
        public void XformBy(Mat m)
        {
            for (int i = 0; i < _pts.Count; i++) {
                Pt npt = m * _pts[i].ToPoint();
                _pts[i] = new StylusPoint(npt.X, npt.Y, _pts[i].PressureFactor);
            }
            _changeSeq++;
            if(PointsModified != null && !BatchEditing) PointsModified(this, m);
        }
        /// <summary>
        /// Move the points in the stroke by the given vector. At most one callback (PointsModified) is made.
        /// </summary>
        /// <param name="v"></param>
        public void Move(Vec v) {
            XformBy(Mat.Translate(v));
        }
        /// <summary>
        /// The ixth position of the Stroq. 0 is the first point (index 0), N-1 is the last (where N is the number of points in the Stroq). Fractional
        /// values interpolate the result between the two actual points on either side (so 0.5 would interpolate halfway between point 0 and point 1).
        /// </summary>
        public Pt this[double ix]
        {
            get {
                int ipart = (int)Math.Floor(ix);
                Pt a = _pts[ipart].ToPoint();
                double fpart = ix - ipart;
                if(fpart > 0) {
                    Pt b = _pts[ipart+1].ToPoint();
                    return a + fpart*(b-a);
                } else return a;
            }
        }

        /// <summary>
        /// This class is only public because otherwise the syntactic sugar we do with the Pressure field can't be used outside Stroq. Only use the
        /// array indexing methods of this class, and don't store objects of this type anywhere.
        /// </summary>
        public sealed class SyntacticSugarFloat {
            private Stroq _p;
            public delegate void CB(int ix, double val);
            private CB _before, _after;
            internal SyntacticSugarFloat(Stroq p, CB before, CB after) { _p = p; _before = before; _after = after; }
            /// <summary>
            /// The ixth value. As in Perl, negative indices count backwards from the end: x[-1] is the last value, x[-2] the
            /// next-to-last value, etc.
            /// </summary>
            /// <param name="ix"></param>
            /// <returns></returns>
            public float this[int ix] {
                get {
                    return ix < 0 ? _p._pts[_p._pts.Count+ix].PressureFactor : _p._pts[ix].PressureFactor;
                }
                set {
                    int i = ix < 0 ? _p._pts.Count+ix : ix;
                    if(_before != null) _before(i, value);
                    float old = _p._pts[i].PressureFactor;
                    /* I think it's a language bug I can't just say:
                    _p._pts[i].PressureFactor = value;
                     */
                    StylusPoint sp = _p._pts[i];
                    sp.PressureFactor = value;
                    _p._pts[i] = sp;
                    if(_after != null) _after(i, old);
                }
            }
            /// <summary>
            /// The ixth value. 0 is the first (index 0), N-1 is the last (where N is the number of values stored). Fractional
            /// arguments interpolate the result between the two actual values on either side (so 0.5 would interpolate halfway between value 0 and value 1).
            /// Note that negative indices do *not* work for this accessor.
            /// </summary>
            public float this[double ix] {
                get {
                    int ipart = (int)Math.Floor(ix);
                    double fpart = ix - ipart;
                    float a = _p._pts[ipart].PressureFactor;
                    float b = _p._pts[ipart+1].PressureFactor;
                    return (float)(a + fpart*(b-a));
                }
            }
        }
        /// <summary>
        /// The ixth pressure value of the Stroq. We use some
        /// syntactic sugar here to make it easier to set this value: you are intended to write something like <c>mypres = poly.Pressure[ix]</c>
        /// or <c>poly.Pressure[ix] = mypres</c>. Don't try to do anything with the returned value other than immediately call an array indexer.
        /// </summary>
        public SyntacticSugarFloat Pressure { 
            get {
                return new SyntacticSugarFloat(this, null, (ix, val) => { if(PointChanged != null) PointChanged(this, ix); }); 
            }
        }

        /// <summary>
        /// This class is only public because otherwise the syntactic sugar we do with the StylusPoints field can't be used outside Stroq. Only use the
        /// array indexing methods of this class and iterating over the members (foreach), and don't store objects of this type anywhere.
        /// </summary>
        public sealed class SyntacticSugarSP : IEnumerable<StylusPoint> {
            private Stroq _p;
            public delegate void CB(int ix, StylusPoint val);
            private CB _before, _after;
            internal SyntacticSugarSP(Stroq p, CB before, CB after) { _p = p; _before = before; _after = after; }
            /// <summary>
            /// The ixth value. As in Perl, negative indices count backwards from the end: x[-1] is the last value, x[-2] the
            /// next-to-last value, etc.
            /// </summary>
            /// <param name="ix"></param>
            /// <returns></returns>
            public StylusPoint this[int ix] {
                get {
                    return ix < 0 ? _p._pts[_p._pts.Count+ix] : _p._pts[ix];
                }
                set {
                    int i = ix < 0 ? _p._pts.Count+ix : ix;
                    if(_before != null) _before(i, value);
                    var old = _p._pts[i];
                    _p._pts[i] = value;
                    if(_after != null) _after(i, old);
                }
            }

            /// <summary>
            /// Iterate over all the points of the stroke. See also iterating over the Stroq itself, and the Stroq members Property and PressurePoints.
            /// </summary>
            public IEnumerator<StylusPoint> GetEnumerator() { foreach(var p in _p._pts) yield return p; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
        /// <summary>
        /// The ixth value of the Stroq, full data from the backing stroke. We use some
        /// syntactic sugar here to make it easier to set this value: you are intended to write something like <c>mysp = poly.StylusPoints[ix]</c>
        /// or <c>poly.StylusPoints[ix] = mysp</c>. Don't try to do anything with the returned value other than immediately call an array indexer. However,
        /// you can use foreach: foreach(var foo in Stroq.StylusPoints). See also iterating over the Stroq itself, and the members
        /// Property and PressurePoints.
        /// </summary>
        public SyntacticSugarSP StylusPoints {
            get {
                return new SyntacticSugarSP(this, null, (ix, val) => { if(PointChanged != null) PointChanged(this, ix); }); 
            }
        }

        /// <summary>
        /// This class is only public because otherwise the syntactic sugar we do with the Property field can't be used outside Stroq. Only use the
        /// public members (including indexers) of this class, and don't store objects of this type anywhere.
        /// </summary>
        public class SyntacticSugarDictionary<U,V> {
            private Dictionary<U,SortedDictionary<int,V>> _perpt;
            /// <summary>
            /// The per-point properties.
            /// </summary>
            public Dictionary<U, SortedDictionary<int, V>> PerPt { get { return _perpt; } }
            private Dictionary<U,V> _perpoly;
            /// <summary>
            /// The per-Stroq properties.
            /// </summary>
            public Dictionary<U, V> PerPoly { get { return _perpoly; } }
            private Stroq _poly;
            internal SyntacticSugarDictionary(Stroq poly, Dictionary<U, SortedDictionary<int, V>> perpt, Dictionary<U, V> perpoly) { _poly = poly;  _perpt = perpt; _perpoly = perpoly; }
            /// <summary>
            /// Look up the given per-Stroq property.
            /// </summary>
            public V this[U id] {
                get { return _perpoly[id]; }
                set { _perpoly[id] = value; }
            }
            /// <summary>
            /// Does the given per-Stroq property exist?
            /// </summary>
            public bool Exists(U id) {
                return _perpoly.ContainsKey(id);
            }
            /// <summary>
            /// Remove the per-Stroq property. 
            /// </summary>
            public void Remove(U id)
            {
                if (_perpoly.ContainsKey(id))
                {
                    _perpoly.Remove(id);
                }
            }
            /// <summary>
            /// Tries to get the per-Stroq property; returns whether it exists or not. (If not, value will not be filled in.)
            /// </summary>
            public bool TryGetValue(U id, out V value) {
                return _perpoly.TryGetValue(id, out value);
            }
            /// <summary>
            /// Look up the given per-point property. Negative indices wrap around, as for the main Stroq point and pressure indexers.
            /// </summary>
            public V this[U id, int ix] {
                get { return _perpt[id][ix < 0 ? _poly._pts.Count + ix : ix]; }
                set { 
                    if(!_perpt.ContainsKey(id)) _perpt[id] = new SortedDictionary<int,V>();
                    _perpt[id][ix < 0 ? _poly._pts.Count + ix : ix] = value;
                }
            }
            /// <summary>
            /// Does the given per-point property exist? Negative indices wrap around, as for the main Stroq point and pressure indexers.
            /// </summary>
            public bool Exists(U id, int ix) {
                return _perpt.ContainsKey(id) && _perpt[id].ContainsKey(ix < 0 ? _poly._pts.Count + ix : ix);
            }
            /// <summary>
            /// Tries to get the per-point property; returns whether it exists or not. (If not, value will not be filled in.) Negative indices wrap around, as for the main Stroq point and pressure indexers.
            /// </summary>
            public bool TryGetValue(U id, int ix, out V value) {
                SortedDictionary<int, V> ptprops;
                value = default(V);
                return _perpt.TryGetValue(id, out ptprops) && ptprops.TryGetValue(ix < 0 ? _poly._pts.Count + ix : ix, out value);
            }

            /// <summary>
            /// Allows iteration over all the per-stroke properties.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<KeyValuePair<U,V>> All() { return _perpoly.AsEnumerable(); }
            /// <summary>
            /// Allows iteration over all the per-point properties of a given point.
            /// </summary>
            /// <param name="ix"></param>
            /// <returns></returns>
            public IEnumerable<KeyValuePair<U,V>> All(int ix) { 
                foreach(var kvp in _perpt) {
                    V val;
                    if(kvp.Value.TryGetValue(ix, out val)) yield return new KeyValuePair<U,V>(kvp.Key, val);
                }
            }
            /// <summary>
            /// Allows iteration over values of a per-point property for all the points set.
            /// </summary>
            /// <param name="u"></param>
            /// <returns></returns>
            public IEnumerable<KeyValuePair<int, V>> All(U u) { return _perpt[u].AsEnumerable(); }
        }
        [NonSerialized]
        private Dictionary<Guid, object> _strokeProperties = new Dictionary<Guid,object>();
        [NonSerialized]
        private Dictionary<Guid, SortedDictionary<int, object>> _ptProperties = new Dictionary<Guid,SortedDictionary<int,object>>();
        // The shadows are used during (de)serialization to store all the serializable properties
        private Dictionary<Guid, object> _strokePropsSerialShadow;
        private Dictionary<Guid, SortedDictionary<int, object>> _ptPropsSerialShadow;
        /// <summary>
        /// Either the given per-Stroq property, or the given per-point property, depending on the arguments you give. We use some
        /// syntactic sugar here to make it easier to set these values: you are intended to write something like <c>myval = poly.Property[MyID]</c>
        /// or <c>poly.Property[MyID] = myval</c> (to reference a per-stroke property with Guid MyID) or <c>myval = poly.Property[MyID, ix]</c>
        /// or <c>poly.Property[MyID, ix] = myval</c> (to reference a per-point property with Guid MyID and for point index ix).
        /// Don't try to do anything with the returned value other than immediately call an array indexer.
        /// 
        /// For iteration over all the per-stroke properties, or over all the properties of a given point, or all the points of a given per-point property,
        /// use Property.All(...).
        /// </summary>
        //Argh! can't put [NonSerialized] on an autoimplemented property here!
        public SyntacticSugarDictionary<Guid, object> Property { get { return _property; } private set { _property = value; } }
        [NonSerialized]
        private SyntacticSugarDictionary<Guid, object> _property;

        [OnDeserialized]
        public void onDeserializedFix(StreamingContext sc) {
            if (_backingStrokeShadow != null) {
                _strokeProperties = _strokePropsSerialShadow;
                _strokePropsSerialShadow = null;
                _ptProperties = _ptPropsSerialShadow;
                _ptPropsSerialShadow = null;
                StrokeCollection stc = new StrokeCollection(_backingStrokeShadow);
                _backingStroke = stc[0];
                _backingStrokeShadow = null;
                Property = new SyntacticSugarDictionary<Guid, object>(this, _ptProperties, _strokeProperties);
            }
        }
        [OnSerializing]
        private void onSerializingPrepare(StreamingContext sc) {
            if(BatchEditing) throw new Exception("Stroqs being batch edited may not be serialized.");
            _strokePropsSerialShadow = new Dictionary<Guid, object>();
            foreach(var sp in _strokeProperties) {
                if(sp.Value.GetType().IsSerializable) _strokePropsSerialShadow[sp.Key] = sp.Value;
            }
            _ptPropsSerialShadow = new Dictionary<Guid, SortedDictionary<int, object>>();
            foreach(var pp in _ptProperties) {
                SortedDictionary<int, object> keyprops = new SortedDictionary<int, object>();
                foreach(var prop in pp.Value) {
                    if(prop.Value.GetType().IsSerializable) keyprops[prop.Key] = prop.Value;
                }
                if(keyprops.Count > 0) _ptPropsSerialShadow[pp.Key] = keyprops;
            }
            StrokeCollection stc = new StrokeCollection();
            stc.Add(_backingStroke);
            _backingStrokeShadow = new MemoryStream();
            stc.Save(_backingStrokeShadow);
            _backingStrokeShadow.Seek(0, SeekOrigin.Begin);
        }
        [OnSerialized]
        private void onSerializedRestore(StreamingContext sc) {
            _strokePropsSerialShadow = null;
            _ptPropsSerialShadow = null;
            _backingStrokeShadow = null;
        }

        public delegate void StroqCreatedHandler(Stroq p);
        /// <summary>
        /// This event is and should be called at the end of all of the code in the Stroq constructor, even of derived classes. It's a hook so that
        /// extensions can initialize whatever structures they may want.
        /// </summary>
        [field:NonSerialized]
        public static event StroqCreatedHandler StroqCreated;
        protected Stroq(Stroke backingStroke, bool suppressEvent) {
            _backingStroke = backingStroke;
            Property = new SyntacticSugarDictionary<Guid, object>(this, _ptProperties, _strokeProperties);
            if(!suppressEvent && StroqCreated != null) StroqCreated(this);
        }
        public Stroq() : this(new Stroke(new StylusPointCollection()), false) { }
        public Stroq(StylusPointCollection spc) : this(new Stroke(spc), false) { }
        protected Stroq(IEnumerable<Pt> pts, bool suppressEvent) : this(pts.Select((p) => (Point)p), suppressEvent) { }
        public Stroq(IEnumerable<Pt> pts) : this(pts, false) { }
        protected Stroq(IEnumerable<Point> pts, bool suppressEvent) : this(new Stroke(new StylusPointCollection(pts)), suppressEvent) { }
        public Stroq(IEnumerable<Point> pts) : this(pts, false) { }
        public Stroq(params Pt[] pts) : this(pts, false) { }
        public Stroq(Stroke s) : this(s, false) { }
        /// <summary>
        /// This is only used when cloning the Stroq.
        /// </summary>
        protected Stroq(Stroke s, Dictionary<Guid, object> strokeprops, Dictionary<Guid, SortedDictionary<int, object>> ptprops, Stroq oldstroq)
            : this(s, true) {
            _strokeProperties = strokeprops;
            _ptProperties = ptprops;
            Property = new SyntacticSugarDictionary<Guid, object>(this, _ptProperties, _strokeProperties);

            // notify the properties about the cloning
            foreach(var sp in _strokeProperties) {
                IStroqCloneable pval = sp.Value as IStroqCloneable;
                if(pval != null) pval.AfterClone(oldstroq, this);
            }
            foreach(var pp in _ptProperties) {
                foreach(var prop in pp.Value) {
                    IStroqCloneable pval = prop.Value as IStroqCloneable;
                    if(pval != null) pval.AfterClone(oldstroq, this);
                }
            }

            if(StroqCreated != null) StroqCreated(this);
        }

        [field:NonSerialized]
        public event Action<Stroq> PointsCleared;
        /// <summary>
        /// Delete all the points and point-specific properties.
        /// </summary>
        public void Clear() {
            _pts.Clear();
            _ptProperties.Clear();
            _changeSeq++;
            if(PointsCleared != null && !BatchEditing) PointsCleared(this);
        }

        private void SetPointProps(int ix, Pair<Guid, object>[] pointprops) {
            foreach(var prop in pointprops) {
                Property[prop.First, ix] = prop.Second;
            }
        }
        [field:NonSerialized]
        public event Action<Stroq, Mat?> PointsModified;
        public event Action<Stroq> VisibilityChanged;
        public void Add(StylusPoint p) { _pts.Add(p); _changeSeq++; if(PointsModified != null && !BatchEditing) PointsModified(this, null); }
        public void Add(Pt p) { Add(new StylusPoint(p.X, p.Y)); }
        public void Add(Pt p, float pressure) { Add(new StylusPoint(p.X, p.Y, pressure)); }
        public void Add(Pt p, float pressure, StylusPointDescription spd, params int[] parms) { Add(new StylusPoint(p.X, p.Y, pressure, spd, parms)); }
        public void Add(Pt p, params Pair<Guid, object>[] pointprops) {
            Add(new StylusPoint(p.X, p.Y));
            SetPointProps(_pts.Count-1, pointprops);
        }
        public void Add(Pt p, float pressure, params Pair<Guid, object>[] pointprops) {
            Add(new StylusPoint(p.X, p.Y, pressure));
            SetPointProps(_pts.Count-1, pointprops);
        }
        public void Add(Pt p, float pressure, StylusPointDescription spd, int[] parms, Pair<Guid, object>[] pointprops) {
            Add(new StylusPoint(p.X, p.Y, pressure, spd, parms));
            SetPointProps(_pts.Count-1, pointprops);
        }

        /// <summary>
        /// The number of points in the Stroq.
        /// </summary>
        public int Count { get { return _pts.Count; } }

        public void Insert(int ix, StylusPoint p) { 
            _pts.Insert(ix, p); 
            // move existing point props up to reflect new indices
            // what a pain it is that we have no enumerators that allow us to write to the structure
            // alternate method is to iterate by point indices, can then do setting. could even find which one likely to be more efficient
            Dictionary<Guid, SortedDictionary<int, object>> finalout = new Dictionary<Guid,SortedDictionary<int,object>>();
            foreach(var pp in _ptProperties) {
                SortedDictionary<int,object> output = new SortedDictionary<int,object>();
                foreach(var prop in pp.Value) {
                    output.Add(prop.Key < ix ? prop.Key : prop.Key + 1, prop.Value);
                }
                finalout.Add(pp.Key, output);
            }
            _ptProperties = finalout;
            _changeSeq++;
            if(PointsModified != null && !BatchEditing) PointsModified(this, null);
        }
        public void Insert(int ix, Pt p) { Insert(ix, new StylusPoint(p.X, p.Y)); }
        public void Insert(int ix, Pt p, float pressure) { Insert(ix, new StylusPoint(p.X, p.Y, pressure)); }
        public void Insert(int ix, Pt p, float pressure, StylusPointDescription spd, params int[] parms) {
            Insert(ix, new StylusPoint(p.X, p.Y, pressure, spd, parms));
        }
        public void Insert(int ix, Pt p, params Pair<Guid, object>[] pointprops) {
            Insert(ix, p);
            SetPointProps(ix, pointprops);
        }
        public void Insert(int ix, Pt p, float pressure, params Pair<Guid, object>[] pointprops) {
            Insert(ix, p, pressure);
            SetPointProps(ix, pointprops);
        }
        public void Insert(int ix, Pt p, float pressure, StylusPointDescription spd, int[] parms, Pair<Guid, object>[] pointprops) {
            Insert(ix, p, pressure, spd, parms);
            SetPointProps(ix, pointprops);
        }

        /// <summary>
        /// Delete the point and its point-specific properties. Returns the point that was removed.
        /// </summary>
        public StylusPoint RemoveAt(int ix) {
            StylusPoint p = _pts[ix];
            _pts.RemoveAt(ix);
            // move existing point props down to reflect new indices
            // what a pain it is that we have no enumerators that allow us to write to the structure
            // alternate method is to iterate by point indices, can then do setting. could even find which one likely to be more efficient
            Dictionary<Guid, SortedDictionary<int, object>> finalout = new Dictionary<Guid, SortedDictionary<int, object>>();
            foreach(var pp in _ptProperties) {
                SortedDictionary<int, object> output = new SortedDictionary<int, object>();
                foreach(var prop in pp.Value) {
                    if(prop.Key != ix) output.Add(prop.Key < ix ? prop.Key : prop.Key - 1, prop.Value);
                }
                finalout.Add(pp.Key, output);
            }
            _ptProperties = finalout;
            _changeSeq++;
            if(PointsModified != null && !BatchEditing) PointsModified(this, null);
            return p;
        }
        void IList<Pt>.RemoveAt(int index) { RemoveAt(index); }

        /// <summary>
        /// Remove the point and point-specific properties of the first point that matches the location given.
        /// Returns whether this was possible or not (if the point wasn't found).
        /// </summary>
        public bool Remove(Pt item) {
            int ix = IndexOf(item);
            if(ix != -1) {
                RemoveAt(ix);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Support iteration over the Pts of this Stroq. See also the members StylusPoints, Property, and PressurePoints.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Pt> GetEnumerator() {
            foreach(var p in _pts) yield return p;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
        /// <summary>
        /// Allow iteration over the pairs of Pt and pressure value of the Stroq. See also iterating over the Stroq directly, and the members
        /// StylusPoints and Property.
        /// </summary>
        public IEnumerable<Pair<Pt, float>> PressurePoints {
            get {
                foreach(var p in _pts) yield return new Pair<Pt, float>(p, p.PressureFactor);
            }
        }

        /// <summary>
        /// See the documentation for Stroq.Clone().
        /// </summary>
        public interface IStroqCloneable {
            void AfterClone(Stroq oldStroq, Stroq newStroq);
        }
        /// <summary>
        /// Clones the Stroq. This does as deep a clone as it can manage--properties are Cloned if they are ICloneable, but simply copied otherwise.
        /// Callbacks are *not* copied over. (For now. Is this the right decision?) However, if properties are IStroqCloneable, AfterClone will be
        /// called on the cloned property so it can fix callbacks, etc.
        /// </summary>
        /// <returns></returns>
        public Stroq Clone() {
            Dictionary<Guid,object> sprops = new Dictionary<Guid,object>(_strokeProperties.Count);
            foreach(var sp in _strokeProperties) {
                ICloneable pval = sp.Value as ICloneable;
                sprops[sp.Key] = pval == null ? sp.Value : (object)pval.Clone();
            }
            Dictionary<Guid, SortedDictionary<int, object>> ptprops = new Dictionary<Guid,SortedDictionary<int,object>>(_ptProperties.Count);
            foreach(var pp in _ptProperties) {
                ptprops[pp.Key] = new SortedDictionary<int,object>();
                foreach(var prop in pp.Value) {
                    ICloneable pval = prop.Value as ICloneable;
                    ptprops[pp.Key][prop.Key] = pval == null ? prop.Value : (object)pval.Clone();
                }
            }
            return new Stroq(_backingStroke.Clone(), sprops, ptprops, this);
        }
        object ICloneable.Clone() { return Clone(); }

        // TODO: cache this value
        /// <summary>
        /// Compute and return the bounding box for this stroke
        /// </summary>
        public Rct GetBounds() {
            return BackingStroke.GetBounds();
        }

        public bool Contains(Pt item) {
            return _pts.Any((p) => item == p);
        }

        public void CopyTo(Pt[] array, int arrayIndex) {
            foreach(Pt p in this) array[arrayIndex++] = p;
        }

        public bool IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// Return the index of the first element matching the argument, or -1 if no such element exists.
        /// </summary>
        public int IndexOf(Pt item) {
            return _pts.IndexOf((p) => item == p);
        }

        /// <summary>
        /// This operator allows drawing of Stroqs easily by just storing them in a list of children for a Canvas or whatever.
        /// </summary>
        public static implicit operator StroqElement(Stroq s) { return new StroqElement(s); }
        /// <summary>
        /// If you've been drawing Stroqs easily using StroqElements and the implicit cast, this helps you convert back to a Stroq.
        /// </summary>
        public static implicit operator Stroq(StroqElement se) { return se.Stroq; }

        private bool _secondRenderPass = false;
        public bool SecondRenderPass
        {
            get
            {
                return _secondRenderPass;
            }
            set
            {
                _secondRenderPass = value;
                if (PointsModified != null && !BatchEditing) PointsModified(this, null);
            }
        }

        private Guid _id = Guid.NewGuid();
        public Guid Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }
        
        [NonSerialized]
        private Color _secondRenderPassColor = Colors.Black;
        public Color SecondRenderPassColor
        {
            get
            {
                return _secondRenderPassColor;
            }
            set
            {
                _secondRenderPassColor = value;
                if (PointsModified != null && !BatchEditing) PointsModified(this, null);
            }
        }

        private List<float> _secondRenderPassPressureFactors = new List<float>();
        public List<float> SecondRenderPassPressureFactors
        {
            get
            {
                return _secondRenderPassPressureFactors;
            }
            set
            {
                _secondRenderPassPressureFactors = value;
                if (PointsModified != null && !BatchEditing) PointsModified(this, null);
            }
        }

        private bool _visible = true;
        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
                if (VisibilityChanged != null) VisibilityChanged(this);
            }
        }
    }
}
