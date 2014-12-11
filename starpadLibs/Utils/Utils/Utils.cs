using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// This file and namespace exists to remedy deficiencies in C#/.net relative to the C++ STL and to common lisp.
namespace starPadSDK.Utils {
    /// <summary>
    /// An ordered pair of two values of some kind.
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <typeparam name="V"></typeparam>
    public struct Pair<U, V> {
        public U First { get; set; }
        public V Second { get; set; }
        public Pair(U first, V second) : this() { First = first; Second = second; }
        public override bool Equals(object obj) {
            if(Object.ReferenceEquals(this, obj)) return true;
            if(Object.ReferenceEquals(this, null) || Object.ReferenceEquals(obj, null)) return false;
            Pair<U, V>? o = obj as Pair<U, V>?;
            return o.HasValue && First.Equals(o.Value.First) && Second.Equals(o.Value.Second);
        }
        public override int GetHashCode() { return First.GetHashCode()^Second.GetHashCode(); }
        public override string ToString() { return "<" + First.ToString() + ", " + Second.ToString() + ">"; }
        public static bool operator==(Pair<U, V> a, Pair<U, V> b) { return a.Equals(b); }
        public static bool operator!=(Pair<U, V> a, Pair<U, V> b) { return !a.Equals(b); }
    }
    /// <summary>
    /// This class is used to allow use of foreach on lists but still be able to, eg, get adjacent elements, change the value in the list, etc. You can
    /// write to the index and it changes the value in the for() loop of IList.RWIter(); writing to the value changes the value in the list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RWIter<T> {
        /// <summary>
        /// The current index into the list.
        /// </summary>
        public int i;
        private IList<T> _base;
        public RWIter(IList<T> baselist) { _base = baselist; }
        /// <summary>
        /// The element of the list at that index. Changing this changes the value in the list.
        /// </summary>
        public T Value { get { return _base[i]; } set { _base[i] = value; } }
        public static implicit operator T(RWIter<T> i) { return i.Value; }
    }
    public static class Utils {
        /// <summary>
        /// Return the index of the first item satisfying the predicate, or -1 if no item does.
        /// </summary>
        public static int IndexOf<T>(this IList<T> list, Func<T, bool> tester) {
            int i = 0;
            foreach(T t in list) {
                if(tester(t)) return i;
                i++;
            }
            return -1;
        }
        /// <summary>
        /// Enumerate the values of this sequence in pairs, as (i,i+1), (i+1, i+2), etc.
        /// </summary>
        public static IEnumerable<Pair<T, T>> ByPairs<T>(this IEnumerable<T> e) {
            bool gotone = false;
            T last = default(T);
            foreach(T val in e) {
                if(!gotone) gotone = true;
                else yield return new Pair<T, T>(last, val);
                last = val;
            }
        }
        /// <summary>
        /// A version of Reverse() for lists which is more efficient if indexing is constant-time, and which allows write access through the resulting IList
        /// to the original IList.
        /// </summary>
        public static IList<T> Reverse1<T>(this IList<T> list) { return new ReversedIList<T>(list); }
        /// <summary>
        /// Used by IList&lt;T&gt;.Reverse1() to give a version of Reverse() for lists which is more efficient if indexing is constant-time,
        /// and which allows write access through the resulting IList to the original IList.
        /// </summary>
        public class ReversedIList<T> : IList<T> {
            private IList<T> _l;
            public ReversedIList(IList<T> l) { _l = l; }
            private IEnumerable<T> ReverseEnum(IList<T> list) {
                for(int i = list.Count-1; i >= 0; i--) yield return list[i];
            }

            #region IList<T> Members
            public int IndexOf(T item) {
                int i = _l.Count-1;
                foreach(T t in ReverseEnum(_l)) {
                    if(t.Equals(item)) return i;
                    i--;
                }
                return -1;
            }
            public void Insert(int index, T item) { _l.Insert(_l.Count-1 - index, item); }
            public void RemoveAt(int index) { _l.RemoveAt(_l.Count-1 - index); }
            public T this[int index] { get { return _l[_l.Count-1 - index]; } set { _l[_l.Count-1 - index] = value; } }
            #endregion

            #region ICollection<T> Members
            public void Add(T item) { _l.Insert(0, item)/*_l.Add(item)?*/; }
            public void Clear() { _l.Clear(); }
            public bool Contains(T item) { return _l.Contains(item); }
            public void CopyTo(T[] array, int arrayIndex) { ReverseEnum(_l).CopyTo(array, arrayIndex); }
            public int Count { get { return _l.Count; } }
            public bool IsReadOnly { get { return _l.IsReadOnly; } }
            public bool Remove(T item) {
                int ix = IndexOf(item);
                if(ix == -1) return false;
                RemoveAt(ix);
                return true;
            }
            #endregion

            #region IEnumerable<T> Members
            public IEnumerator<T> GetEnumerator() { return ReverseEnum(_l).GetEnumerator(); }
            #endregion

            #region IEnumerable Members
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return ((System.Collections.IEnumerable)ReverseEnum(_l)).GetEnumerator();
            }
            #endregion
        }
        /// <summary>
        /// If you use this on an (I)List, be sure you test whether this or the IList.CopyTo is being called...and let me (tsm) know
        /// </summary>
        public static void CopyTo<T>(this IEnumerable<T> ie, T[] array, int ix) {
            foreach(T t in ie) {
                array[ix++] = t;
            }
        }

        /// <summary>
        /// Allow use of foreach on lists but still be able to, eg, get adjacent elements, change the value in the list, etc. Do a foreach(var i in list.RWIter())
        /// and you can set i.i to change the value in the for() loop of IList.RWIter(); i.Value is the value at the current position (current value of i.i);
        /// writing it changes the value in the list. There is also an implicit conversion to the type stored in the list.
        /// </summary>
        public static IEnumerable<RWIter<T>> RWIter<T>(this IList<T> list) {
            RWIter<T> ix = new RWIter<T>(list);
            for(ix.i = 0; ix.i < list.Count; ix.i++) yield return ix;
        }
        public static void ForEach<T>(this IList<T> list, Func<RWIter<T>, bool> fn) {
            foreach (var i in list.RWIter()) if (fn(i)) break;
        }
        public static void ForEach<T>(this IList<T> list, Action<RWIter<T>> fn) {
            foreach (var i in list.RWIter()) fn(i);
        }
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T, int> fn) {
            int i = 0;
            foreach(T t in ie) fn(t, i++);
        }
        private struct IxAgg<T> {
            public T value;
            public int index;
            public IxAgg(T val, int ix) { value = val; index = ix; }
        }
        // Unfortunately, C# doesn't posess any of the mechanisms C++ has to let us automatically figure out the max argument here. I suppose we could
        // complicate the logic by testing if the aggregator index is -1, but that's annoying.
        /// <summary>
        /// Returns the minimum value of a sequence, and puts the index of the first occurrence of that value into ix. The parameter max should be the
        /// maximum possible value of type T. (Double.PositiveInfinity for doubles, for instance.)
        /// </summary>
        public static T Min<T>(this IEnumerable<T> en, T max, out int ix) where T : IComparable<T> {
            int i = -1;
            IxAgg<T> result = en.Aggregate(new IxAgg<T>(max, -1), (agg, val) => {
                i++;
                if(val.CompareTo(agg.value) < 0) return new IxAgg<T>(val, i);
                else return agg;
            });
            ix = result.index;
            return result.value;
        }
        /// <summary>
        /// Returns the element of a sequence which has minimum value according to some function.
        /// </summary>
        /// <typeparam name="T">sequence element type</typeparam>
        /// <typeparam name="V">value returned by the valuing function</typeparam>
        /// <param name="en">the sequence</param>
        /// <param name="max">the maximum possible value of type V</param>
        /// <param name="selector">given a sequence element, return a corresponding value</param>
        /// <returns></returns>
        public static T Min<T,V>(this IEnumerable<T> en, V max, Func<T,V> selector) where V : IComparable<V> {
            return en.Aggregate(new Pair<V, T>(max, default(T)), (min, t) => { V v = selector(t); return v.CompareTo(min.First) < 0 ? new Pair<V, T>(v, t) : min; }).Second;
        }
        // Unfortunately, C# doesn't posess any of the mechanisms C++ has to let us automatically figure out the max argument here. I suppose we could
        // complicate the logic by testing if the aggregator index is -1, but that's annoying.
        /// <summary>
        /// Returns the maximum value of a sequence, and puts the index of the first occurrence of that value into ix. The parameter min should be the
        /// minimum possible value of type T. (Double.NegativeInfinity for doubles, for instance.)
        /// </summary>
        public static T Max<T>(this IEnumerable<T> en, T min, out int ix) where T : IComparable<T> {
            int i = -1;
            IxAgg<T> result = en.Aggregate(new IxAgg<T>(min, -1), (agg, val) =>
            {
                i++;
                if(val.CompareTo(agg.value) > 0) return new IxAgg<T>(val, i);
                else return agg;
            });
            ix = result.index;
            return result.value;
        }
        /// <summary>
        /// Returns the element of a sequence which has maximum value according to some function.
        /// </summary>
        /// <typeparam name="T">sequence element type</typeparam>
        /// <typeparam name="V">value returned by the valuing function</typeparam>
        /// <param name="en">the sequence</param>
        /// <param name="min">the minimum possible value of type V</param>
        /// <param name="selector">given a sequence element, return a corresponding value</param>
        /// <returns></returns>
        public static T Max<T, V>(this IEnumerable<T> en, V min, Func<T, V> selector) where V : IComparable<V> {
            return en.Aggregate(new Pair<V, T>(min, default(T)), (max, t) => { V v = selector(t); return v.CompareTo(max.First) > 0 ? new Pair<V, T>(v, t) : max; }).Second;
        }
    }
}
