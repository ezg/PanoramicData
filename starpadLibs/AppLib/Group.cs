using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Inq.BobsCusps;

namespace starPadSDK.AppLib {
    public class GroupMgr {
        List<Group> _groups = new List<Group>();

        public Group Create(Stroq[] strokes, FrameworkElement[] elements) {
            List<Stroq>            stroqLeaves = new List<Stroq>();
            List<FrameworkElement> eleLeaves   = new List<FrameworkElement>();
            List<Group>            grouped     = new List<Group>();
            foreach (Stroq s in strokes) {
                Group g = Find(s);
                if (g == null)
                    stroqLeaves.Add(s);
                else if (!grouped.Contains(g))
                    grouped.Add(g);
            }
            foreach (FrameworkElement e in elements) {
                Group g = Find(e);
                if (g == null)
                    eleLeaves.Add(e);
                else if (!grouped.Contains(g))
                    grouped.Add(g);
            }
            return new Group(stroqLeaves.ToArray(), eleLeaves.ToArray(), grouped.ToArray());
        }
        public void  Add(Group g) {
            foreach (Group gg in g.Groups)
                Rem(gg);
            _groups.Add(g); 
        }
        public void  Rem(Group g) {
            foreach (Group gg in g.Groups)
                _groups.Add(gg);
            _groups.Remove(g);
        }
        public Group Find(object e) {
            foreach (Group g in _groups)
                if (g.Find(e))
                    return g;
            return null;
        }
    }
    public class Group {
        List<Stroq>              _stroqs   = new List<Stroq>();
        List<FrameworkElement>   _elements = new List<FrameworkElement>();
        List<Group>              _groups   = new List<Group>();

        public Group(Stroq[] strokes, FrameworkElement[] elements, Group[] groups) {
            if (strokes != null)
                _stroqs = new List<Stroq>(strokes);
            if (elements != null)
                _elements = new List<FrameworkElement>(elements);
            if (groups != null)
                _groups = new List<Group>(groups);
        }
        public Stroq[]                Strokes  { get { return _stroqs.ToArray(); }}
        public FrameworkElement[]     Elements { get { return _elements.ToArray(); } }
        public Group[]                Groups   { get { return _groups.ToArray(); } }

        public List<Stroq>            AllStrokes() {
            List<Stroq> strokes = new List<Stroq>(Strokes);
            foreach (Group g in _groups)
                strokes.AddRange(g.AllStrokes());
            return strokes;
        }
        public List<FrameworkElement> AllElements() {
            List<FrameworkElement> elements = new List<FrameworkElement>(Elements);
            foreach (Group g in _groups)
                elements.AddRange(g.AllElements());
            return elements;
        }
        public bool                   Find(object s) {
            if (s is Stroq && _stroqs.Contains(s as Stroq))
                return true;
            if (s is FrameworkElement && _elements.Contains(s as FrameworkElement))
                return true;
            foreach (Group g in _groups)
                if (g.Find(s))
                    return true;
            return false;
        }
    }
}
