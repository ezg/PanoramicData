using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;

namespace TemplateRecognizer
{
    /// <summary>
    /// Implements a "tidy" tree layouter. 
    /// Based on "Improving Walker's Algorithm to Run in Linear Time" by C. Buchheim, M. Juenger
    /// & S. Leipert, Graph Drawing 2002. (http://citeseer.ist.psu.edu/buchheim02improving.html)
    /// </summary>
    static class BuchheimTreeLayouter
    {
        public static void Layout(Node root, List<List<Node>> levels)
        {
            NodeWrapper rootWrapped = recursiveInitialize(root, null, 0);
            FirstWalk(rootWrapped);
            SecondWalk(rootWrapped, -rootWrapped.prelim, levels);
            Coordinate centerOfRoot = (Coordinate) rootWrapped.WrappedNode.BaseShape.Geometry.EnvelopeInternal.Centre;
            ThirdWalk(rootWrapped, centerOfRoot.X, centerOfRoot.Y);

        }

        private static NodeWrapper recursiveInitialize(Node n, NodeWrapper parent, int level) 
        {
            NodeWrapper wrapped = new NodeWrapper(n, parent);
            wrapped.level = level;

            foreach (Node c in n.Children)
            {
                wrapped.Children.Add(recursiveInitialize(c, wrapped, level + 1));
            }

            return wrapped;
        }

        private static void FirstWalk(NodeWrapper v)
        {
            if (v.Children.Count == 0)
            {
                v.prelim = 0;
                NodeWrapper w = v.LeftSibling();
                if (w != null)
                {
                    v.prelim = w.prelim + (w.WrappedNode.Dimenson.Width + v.WrappedNode.Dimenson.Width) / 2.0 + v.WrappedNode.Dimenson.HorizontalSpace; // TODO: distance between siblings no children
                }
            }
            else
            {
                NodeWrapper defaultAncestor = v.Children[0];
                foreach (NodeWrapper w in v.Children)
                {
                    FirstWalk(w);
                    defaultAncestor = Apportion(w, defaultAncestor);
                }
                ExecuteShifts(v);

                double midpoint = 0.5 * (v.Children[0].prelim + v.Children[v.Children.Count - 1].prelim);
                NodeWrapper wl = v.LeftSibling();
                if (wl != null)
                {
                    v.prelim = wl.prelim + (wl.WrappedNode.Dimenson.Width + v.WrappedNode.Dimenson.Width) / 2.0 + v.WrappedNode.Dimenson.HorizontalSpace; // TODO: distance between siblings with children
                    v.mod = v.prelim - midpoint;
                }
                else
                {
                    v.prelim = midpoint;
                }
            }
        }

        private static void SecondWalk(NodeWrapper v, double m, List<List<Node>> levels)
        {
            double greatestHeightOnLevel = double.MinValue;
            if (v.level - 1 >= 0)
            {
                foreach (Node n in levels[v.level - 1])
                {
                    greatestHeightOnLevel = Math.Max(greatestHeightOnLevel, n.Dimenson.Height);
                }
            }
            else
            {
                greatestHeightOnLevel = 0;
            }
            v.x = v.prelim + m; 
            v.y = (v.Parent != null ? v.Parent.y : 0) + 
                  greatestHeightOnLevel + v.WrappedNode.Dimenson.VerticalSpace +
                  (v.Parent != null ? v.WrappedNode.Dimenson.Height / 2.0 : 0); 
            foreach (NodeWrapper w in v.Children)
            {
                SecondWalk(w, m + v.mod, levels);
            }
        }

        private static void ThirdWalk(NodeWrapper v, double offsetX, double offsetY)
        {
            v.WrappedNode.cleanShape = GeometryHelpers.CreateRectangle(
                new Coordinate(v.x + offsetX, v.y + offsetY), v.WrappedNode.Dimenson.Width, v.WrappedNode.Dimenson.Height);

            foreach (NodeWrapper w in v.Children)
            {
                ThirdWalk(w, offsetX, offsetY);
            }
        }

        private static NodeWrapper Apportion(NodeWrapper v, NodeWrapper defaultAncestor)
        {
            NodeWrapper a = defaultAncestor;
            NodeWrapper w = v.LeftSibling();
            if (w != null)
            {
                NodeWrapper vir = v;
                NodeWrapper vor = v;
                NodeWrapper vil = w;
                NodeWrapper vol = vir.Parent.Children[0];

                double sir = vir.mod;
                double sor = vor.mod;
                double sil = vil.mod;
                double sol = vol.mod;

                NodeWrapper nr = NextRight(vil);
                NodeWrapper nl = NextLeft(vir);
                while (nr != null && nl != null)
                {
                    vil = nr;
                    vir = nl;
                    vol = NextLeft(vol);
                    vor = NextRight(vor);
                    vor.ancestor = v;
                    double shift = (vil.prelim + sil) - (vir.prelim + sir) + (vil.WrappedNode.Dimenson.Width + vir.WrappedNode.Dimenson.Width) / 2.0 + vir.WrappedNode.Dimenson.HorizontalSpace; // TODO: distance leftmost with no children
                    if (shift > 0)
                    {
                        MoveSubtree(Ancestor(vil, v, defaultAncestor), v, shift);
                        sir += shift;
                        sor += shift;
                    }
                    sil += vil.mod;
                    sir += vir.mod;
                    sol += vol.mod;
                    sor += vor.mod;

                    nr = NextRight(vil);
                    nl = NextLeft(vir);
                }
                if (NextRight(vil) != null && NextRight(vor) == null)
                {
                    vor.thread = NextRight(vil);
                    vor.mod += sil - sor;
                }
                if (NextLeft(vir) != null && NextLeft(vol) == null)
                {
                    vol.thread = NextLeft(vir);
                    vol.mod += sir - sol;
                    a = v;
                }
            }
            return a;
        }

        private static NodeWrapper NextRight(NodeWrapper v)
        {
            return v.Children.Count > 0 ? v.Children[v.Children.Count - 1] : v.thread;
        }

        private static NodeWrapper NextLeft(NodeWrapper v)
        {
            return v.Children.Count > 0 ? v.Children[0] : v.thread;
        }

        private static void MoveSubtree(NodeWrapper wl, NodeWrapper wr, double shift)
        {
            double subtrees = wr.Number() - wl.Number();
            wr.change -= shift / subtrees;
            wr.shift += shift;
            wl.change += shift / subtrees;
            wr.prelim += shift;
            wr.mod += shift;
        }

        private static void ExecuteShifts(NodeWrapper v)
        {
            double shift = 0;
            double change = 0;
            NodeWrapper w = v.Children.Count > 0 ? v.Children[v.Children.Count - 1] : null;
            while (w != null)
            {
                w.prelim += shift;
                w.mod += shift;
                change += w.change;
                shift += w.shift + change;
                w = w.LeftSibling();
            }
        }

        private static NodeWrapper Ancestor(NodeWrapper vil, NodeWrapper v, NodeWrapper defaultAncestor)
        {
            if (vil.ancestor.Parent == v.Parent)
            {
                return vil.ancestor;
            }
            else
            {
                return defaultAncestor;
            }
        }
    }

    public class NodeWrapper
    {
        public Node WrappedNode = null;
        public NodeWrapper Parent = null;
        public List<NodeWrapper> Children = new List<NodeWrapper>();

        public int level = 0;
        public double mod = 0;
        public NodeWrapper thread = null;
        public double prelim = 0;
        public NodeWrapper ancestor = null;
        public double shift = 0;
        public double change = 0;
        public double x = 0;
        public double y = 0;

        public NodeWrapper(Node wrappedNode, NodeWrapper parent)
        {
            WrappedNode = wrappedNode;
            Parent = parent;
            ancestor = this;
        }

        public NodeWrapper LeftSibling()
        {
            if (Parent != null)
            {
                for (int i = 0; i < Parent.Children.Count; i++)
                {
                    if (this == Parent.Children[i])
                    {
                        return i > 0 ? Parent.Children[i - 1] : null;
                    }

                }
            }

            return null;
        }

        public double Number()
        {
            return this.LeftSibling() != null ? (this.LeftSibling().Number() + 1.0) : 0.0;
        }
    }
}
