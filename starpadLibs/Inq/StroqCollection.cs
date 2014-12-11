using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Ink;
using starPadSDK.Geom;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Collections.Specialized;
using System.Collections;
using System.Windows;
using starPadSDK.Utils;
using starPadSDK.Inq.MSInkCompat;

namespace starPadSDK.Inq {
    [Serializable]
    public class StroqCollection : ICollection<Stroq>, ICloneable {
        private HashSet<Stroq> _stroqs;
        public StroqCollection() { _stroqs = new HashSet<Stroq>(); }
        public StroqCollection(IEnumerable<Stroq> ss) { _stroqs = new HashSet<Stroq>(ss); }
        private StroqCollection(HashSet<Stroq> stroqs) { _stroqs = stroqs; }
        /// <summary>
        /// Clones the Stroqs as well as the collection.
        /// </summary>
        /// <returns></returns>
        public StroqCollection Clone() {
            return new StroqCollection(_stroqs.Select((Stroq s) => s.Clone()));
        }

        // Stroq-specific stuff
        public void Draw(DrawingContext dc) {
            foreach(Stroq s in _stroqs) s.BackingStroke.Draw(dc);
        }
        public Rct GetBounds() {
            return _stroqs.Aggregate(Rct.Null, (Rct r, Stroq s) => r.Union(s.GetBounds()));
        }
        public StroqCollection HitTest(Pt p) {
            return new StroqCollection(_stroqs.Where((Stroq s) => s.BackingStroke.HitTest(p)));
        }
        public StroqCollection HitTest(Pt p, double diameter) {
            return new StroqCollection(_stroqs.Where((Stroq s) => s.BackingStroke.HitTest(p, diameter)));
        }
        public StroqCollection HitTest(Rct bounds, int percentageContained) {
            return new StroqCollection(_stroqs.Where((Stroq s) => s.BackingStroke.HitTest(bounds, percentageContained)));
        }
        public StroqCollection HitTest(IEnumerable<Pt> lasso, int percentageContained) {
            // we trade memory for time--only do the casting for each point once here, rather than embedding this in the Where which would duplicate
            // it for each stroq.
            List<Point> lasso2 = new List<Point>(lasso.Select((Pt p) => (Point)p));
            return new StroqCollection(_stroqs.Where((Stroq s) => s.BackingStroke.HitTest(lasso2, percentageContained)));
        }
        public StroqCollection HitTest(IEnumerable<Pt> path, StylusShape ss) {
            // we trade memory for time--only do the casting for each point once here, rather than embedding this in the Where which would duplicate
            // it for each stroq.
            List<Point> path2 = new List<Point>(path.Select((Pt p) => (Point)p));
            return new StroqCollection(_stroqs.Where((Stroq s) => s.BackingStroke.HitTest(path2, ss)));
        }
        public void Move(Vec delta) {
            _stroqs.ForEach((s, i) => s.Move(delta));
        }
        public Stroq OldNearestPoint(Pt p) {
            float finaldist;
            float finalix;
            return OldNearestPoint(p, out finaldist, out finalix);
        }
        public Stroq OldNearestPoint(Pt p, out float finaldist, out float finalix) {
            Stroq minstroq = null;
            float mindist = float.PositiveInfinity;
            float minix = 0;
            foreach(Stroq s in _stroqs) {
                float dist;
                float ix = s.OldNearestPoint(p, out dist);
                if(dist < mindist) {
                    minstroq = s;
                    mindist = dist;
                    minix = ix;
                }
            }
            finaldist = mindist;
            finalix = minix;
            return minstroq;
        }

       public class ChangedEventArgs : EventArgs {
            public readonly NotifyCollectionChangedAction Action;
            public readonly IEnumerable<Stroq> NewItems;
            public readonly IEnumerable<Stroq> OldItems;
            public ChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<Stroq> newItems, IEnumerable<Stroq> oldItems) {
                Action = action; NewItems = newItems; OldItems = oldItems;
            }
        }
        [field:NonSerialized]
        public event EventHandler<ChangedEventArgs> Changed;
        protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<Stroq> newItems, IEnumerable<Stroq> oldItems) {
            EventHandler<ChangedEventArgs> temp = Changed;
            if(temp != null) {
                temp(this, new ChangedEventArgs(action, newItems, oldItems));
            }
        }

        //ICollection<Stroq> Members
        public bool Add(Stroq item) {
            if(_stroqs.Add(item)) {
                OnCollectionChanged(NotifyCollectionChangedAction.Add, new[] { item }, null);
                return true;
            } else return false;
        }
        void ICollection<Stroq>.Add(Stroq item) { Add(item); }
        public void Clear() {
            List<Stroq> l = new List<Stroq>(_stroqs);
            _stroqs.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset, null, l.Count > 0 ? l : null);
        }
        public bool Contains(Stroq item) { return _stroqs.Contains(item); }
        public void CopyTo(Stroq[] array, int arrayIndex) { _stroqs.CopyTo(array, arrayIndex); }
        public int Count { get { return _stroqs.Count; } }
        public bool IsReadOnly { get { return false; } }
        public bool Remove(Stroq item) {
            if(_stroqs.Remove(item)) {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, null, new[] { item });
                return true;
            } else return false;
        }

        // IEnumerable<Stroq> Member
        public IEnumerator<Stroq> GetEnumerator() { return _stroqs.GetEnumerator(); }

        // IEnumerable Member
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        // ICloneable Member
        object ICloneable.Clone() { return Clone(); }

        // Extra members to support more, especially based on hashset
        public bool Remove(IEnumerable<Stroq> ss) {
            List<Stroq> removed = new List<Stroq>(ss.Where((Stroq s) => _stroqs.Remove(s)));
            if(removed.Count > 0) {
                EventHandler<ChangedEventArgs> temp = Changed;
                if(temp != null) temp(this, new ChangedEventArgs(NotifyCollectionChangedAction.Remove, null, removed));
                return true;
            } else return false;
        }
        public bool Add(IEnumerable<Stroq> ss) {
            List<Stroq> added = new List<Stroq>(ss.Where((Stroq s) => _stroqs.Add(s)));
            if(added.Count > 0) {
                EventHandler<ChangedEventArgs> temp = Changed;
                if(temp != null) temp(this, new ChangedEventArgs(NotifyCollectionChangedAction.Add, added, null));
                return true;
            } else return false;
        }
        /// <summary>
        /// Returns the intersection of the two sets, without modifying either.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public StroqCollection Intersection(IEnumerable<Stroq> other) {
            HashSet<Stroq> result = new HashSet<Stroq>(_stroqs);
            result.IntersectWith(other is StroqCollection ? ((StroqCollection)other)._stroqs : other); // documentation says this is faster than passing in non-hashset ienumerable when comparers are the same
            return new StroqCollection(result);
        }
        public bool IsProperSubsetOf(IEnumerable<Stroq> other) {
            return _stroqs.IsProperSubsetOf(other is StroqCollection ? ((StroqCollection)other)._stroqs : other);
        }
        public bool IsProperSupersetOf(IEnumerable<Stroq> other) {
            return _stroqs.IsProperSupersetOf(other is StroqCollection ? ((StroqCollection)other)._stroqs : other);
        }
        public bool IsSubsetOf(IEnumerable<Stroq> other) {
            return _stroqs.IsSubsetOf(other is StroqCollection ? ((StroqCollection)other)._stroqs : other);
        }
        public bool IsSupersetOf(IEnumerable<Stroq> other) {
            return _stroqs.IsSupersetOf(other is StroqCollection ? ((StroqCollection)other)._stroqs : other);
        }
        public bool Overlaps(IEnumerable<Stroq> other) {
            return _stroqs.Overlaps(other is StroqCollection ? ((StroqCollection)other)._stroqs : other);
        }
        public bool SetEquals(IEnumerable<Stroq> other) {
            return _stroqs.SetEquals(other is StroqCollection ? ((StroqCollection)other)._stroqs : other);
        }
    }
}
