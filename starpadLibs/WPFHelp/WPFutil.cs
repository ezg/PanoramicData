using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Resources;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Input;
using starPadSDK.Utils;
using starPadSDK.Inq;
using starPadSDK.Geom;

namespace starPadSDK.WPFHelp {
    static public class WPFUtil
    {
        static Hashtable mappings = null;
        static Hashtable ShiftMappings = null;
        public static char         GetCharFromKey(KeyEventArgs key)
        {
            if (mappings == null)
            {
                mappings = new Hashtable();
                ShiftMappings = new Hashtable();

                mappings.Add(Key.Tab, '\t');

                mappings.Add(Key.A, 'a');
                mappings.Add(Key.B, 'b');
                mappings.Add(Key.C, 'c');
                mappings.Add(Key.D, 'd');
                mappings.Add(Key.E, 'e');
                mappings.Add(Key.F, 'f');
                mappings.Add(Key.G, 'g');
                mappings.Add(Key.H, 'h');
                mappings.Add(Key.I, 'i');
                mappings.Add(Key.J, 'j');
                mappings.Add(Key.K, 'k');
                mappings.Add(Key.L, 'l');
                mappings.Add(Key.M, 'm');
                mappings.Add(Key.N, 'n');
                mappings.Add(Key.O, 'o');
                mappings.Add(Key.P, 'p');
                mappings.Add(Key.Q, 'q');
                mappings.Add(Key.R, 'r');
                mappings.Add(Key.S, 's');
                mappings.Add(Key.T, 't');
                mappings.Add(Key.U, 'u');
                mappings.Add(Key.V, 'v');
                mappings.Add(Key.W, 'w');
                mappings.Add(Key.X, 'x');
                mappings.Add(Key.Y, 'y');
                mappings.Add(Key.Z, 'z');



                ShiftMappings.Add(Key.A, 'A');
                ShiftMappings.Add(Key.B, 'B');
                ShiftMappings.Add(Key.C, 'C');
                ShiftMappings.Add(Key.D, 'D');
                ShiftMappings.Add(Key.E, 'E');
                ShiftMappings.Add(Key.F, 'F');
                ShiftMappings.Add(Key.G, 'G');
                ShiftMappings.Add(Key.H, 'H');
                ShiftMappings.Add(Key.I, 'I');
                ShiftMappings.Add(Key.J, 'J');
                ShiftMappings.Add(Key.K, 'K');
                ShiftMappings.Add(Key.L, 'L');
                ShiftMappings.Add(Key.M, 'M');
                ShiftMappings.Add(Key.N, 'N');
                ShiftMappings.Add(Key.O, 'O');
                ShiftMappings.Add(Key.P, 'P');
                ShiftMappings.Add(Key.Q, 'Q');
                ShiftMappings.Add(Key.R, 'R');
                ShiftMappings.Add(Key.S, 'S');
                ShiftMappings.Add(Key.T, 'T');
                ShiftMappings.Add(Key.U, 'U');
                ShiftMappings.Add(Key.V, 'V');
                ShiftMappings.Add(Key.W, 'W');
                ShiftMappings.Add(Key.X, 'X');
                ShiftMappings.Add(Key.Y, 'Y');
                ShiftMappings.Add(Key.Z, 'Z');




                mappings.Add(Key.D0, '0');
                mappings.Add(Key.D1, '1');
                mappings.Add(Key.D2, '2');
                mappings.Add(Key.D3, '3');
                mappings.Add(Key.D4, '4');
                mappings.Add(Key.D5, '5');
                mappings.Add(Key.D6, '6');
                mappings.Add(Key.D7, '7');
                mappings.Add(Key.D8, '8');
                mappings.Add(Key.D9, '9');
                mappings.Add(Key.NumPad0, '0');
                mappings.Add(Key.NumPad1, '1');
                mappings.Add(Key.NumPad2, '2');
                mappings.Add(Key.NumPad3, '3');
                mappings.Add(Key.NumPad4, '4');
                mappings.Add(Key.NumPad5, '5');
                mappings.Add(Key.NumPad6, '6');
                mappings.Add(Key.NumPad7, '7');
                mappings.Add(Key.NumPad8, '8');
                mappings.Add(Key.NumPad9, '9');


                mappings.Add(Key.Oem3, '`');
                ShiftMappings.Add(Key.Oem3, '~');



                ShiftMappings.Add(Key.D1, '!');
                ShiftMappings.Add(Key.D2, '@');
                ShiftMappings.Add(Key.D3, '#');
                ShiftMappings.Add(Key.D4, '$');
                ShiftMappings.Add(Key.D5, '%');
                ShiftMappings.Add(Key.D6, '^');
                ShiftMappings.Add(Key.D7, '&');
                ShiftMappings.Add(Key.D8, '*');
                ShiftMappings.Add(Key.D9, '(');
                ShiftMappings.Add(Key.D0, ')');
                ShiftMappings.Add(Key.OemMinus, '_');
                ShiftMappings.Add(Key.OemPlus, '+');
                mappings.Add(Key.OemPlus, '=');
                mappings.Add(Key.OemMinus, '-');

                mappings.Add(Key.Add, '+');
                mappings.Add(Key.Subtract, '-');



                /*            ShiftMappings.Add(Key.Oem4, '{');
                            ShiftMappings.Add(Key.Oem6, '}');
                            mappings.Add(Key.Oem4, '[');
                            mappings.Add(Key.Oem6, ']');*/


                ShiftMappings.Add(Key.OemOpenBrackets, '{');
                ShiftMappings.Add(Key.OemCloseBrackets, '}');
                mappings.Add(Key.OemOpenBrackets, '[');
                mappings.Add(Key.OemCloseBrackets, ']');



                mappings.Add(Key.Oem5, '\\');
                ShiftMappings.Add(Key.Oem5, '|');

                mappings.Add(Key.OemBackslash, '\\');
                ShiftMappings.Add(Key.OemBackslash, '|');

                mappings.Add(Key.Oem1, ';');
                ShiftMappings.Add(Key.Oem1, ':');

                //mappings.Add(Key.OemSemicolon, ';');
                //ShiftMappings.Add(Key.OemSemicolon, ':');

                //ShiftMappings.Add(Key.Oem7, '\"');
                //mappings.Add(Key.Oem7, '\'');
                ShiftMappings.Add(Key.OemQuotes, '\"');
                mappings.Add(Key.OemQuotes, '\'');


                mappings.Add(Key.OemComma, ',');
                ShiftMappings.Add(Key.OemComma, '<');

                mappings.Add(Key.OemPeriod, '.');
                ShiftMappings.Add(Key.OemPeriod, '>');

                mappings.Add(Key.Oem2, '/');
                ShiftMappings.Add(Key.Oem2, '?');
            }

            try
            {
                if (key.KeyboardDevice.IsKeyDown(Key.LeftShift) || key.KeyboardDevice.IsKeyDown(Key.RightShift))
                {
                    return (char)ShiftMappings[key.Key];
                }
                else
                {
                    return (char)mappings[key.Key];
                }
            }
            catch
            {
                return '\0';
            }

        }
        public static TextBox      MakeText(string text, Rct rct) {
            TextBox tb = new TextBox();
            Pt loc = rct.BottomLeft;
            double fontHeight = rct.Height / 2;
            tb.Text = text;
            tb.FontSize = fontHeight;
            tb.AcceptsReturn = true;
            tb.BorderThickness = new Thickness(0);
            tb.RenderTransform = new TranslateTransform(loc.X, loc.Y - tb.FontSize * 2);
            tb.Focusable = true;
            return tb;
        }

        public static Rct          TransformFromAtoB(this Rct rc, FrameworkElement srcElt, FrameworkElement destElt)
        {
            Pt newTopLeft = rc.TopLeft.TransformFromAtoB(srcElt, destElt);
            Pt newBotRIght = rc.BottomRight.TransformFromAtoB(srcElt, destElt);

            return new Rct(newTopLeft, newBotRIght);
        }
        public static Pt           TransformFromAtoB(this Pt pt, FrameworkElement srcElt, FrameworkElement destElt) {
            FrameworkElement eltParent     = GetCommonAncestor(srcElt, destElt);
            GeneralTransform transToParent = srcElt.TransformToAncestor(eltParent);
            GeneralTransform transToDest   = eltParent.TransformToDescendant(destElt);
            Point result = transToParent.Transform(pt);
            result = transToDest.Transform(result);

            return result;
        }
        public static Rct          GetBounds(this FrameworkElement elt) { return GetBounds(elt, (FrameworkElement)elt.Parent); }
        public static Rct          GetBounds(this FrameworkElement elt, FrameworkElement parent) {
            if (parent == null)
                return new Rct(0, 0, elt.Width, elt.Height);

            //if (elt.ActualHeight == 0 && elt.ActualWidth == 0)
            //    elt.UpdateLayout();
            GeneralTransform trans = elt.TransformToAncestor(parent);

            Rct result = trans.TransformBounds(new Rect(new Point(0, 0), new Size(elt.ActualWidth, elt.ActualHeight)));

            return result;
        }
        public static Rct          GetBoundsTrans(this FrameworkElement elt, FrameworkElement transTo)
        {
            FrameworkElement parent = (FrameworkElement)elt.Parent;

            Rct bounds = GetBounds(elt, parent);

            Pt topLeft     = bounds.TopLeft.TransformFromAtoB(    parent, transTo);
            Pt bottomRight = bounds.BottomRight.TransformFromAtoB(parent, transTo);

            return new Rct(topLeft, bottomRight);
        }

        public static Stroq        PolygonOutline(Polygon p) {
            List<Pt> pts = new List<Pt>();
            Mat rmat = (Mat)p.RenderTransform.Value;
            foreach (Point pt in p.Points)
                pts.Add(rmat * (Pt)pt);
            return new Stroq(pts);
        }
        public static PathGeometry Geometry(IEnumerable<Pt> pts) {
            PathGeometry geom = new PathGeometry();
            PathFigure pf = new PathFigure();
            pf.StartPoint = pts.First();
            geom.Figures = new PathFigureCollection(new PathFigure[] { pf });
            List<Point> points = new List<Point>();
            for (int i = 1; i < pts.Count(); i++)
                points.Add(pts.ElementAt(i));
            PolyLineSegment ps = new PolyLineSegment(points.ToArray(), true);
            pf.Segments.Add(ps);

            return geom;
        }
        public static bool         GeometryContains(Geometry hull, Stroq s) {
            for (int i = 0; i < s.Count(); i++)
                if (!hull.FillContains(s[i]))
                    return false;
            return true;
        }
        public static LnSeg        LineSeg(Line l) {
            Mat rmat = (Mat)l.RenderTransform.Value;
            Pt p1 = new Pt(l.X1, l.Y1);
            Pt p2 = new Pt(l.X2, l.Y2);
            return new LnSeg(rmat * p1, rmat * p2);
        }
        public static Pt[]         GetOutline(FrameworkElement elt, FrameworkElement parent) {
            if (parent == null)
                return new Pt[0];

            elt.UpdateLayout();
            GeneralTransform trans = new MatrixTransform(Mat.Identity);

            try {
                trans = elt.TransformToAncestor(parent);
            }
            catch (System.InvalidOperationException ex) {
            }

            Pt[] bounds = new Pt[] { new Pt(), new Pt(elt.ActualWidth, 0), new Pt(elt.ActualWidth, elt.ActualHeight), new Pt(0, elt.ActualHeight) };
            if (elt.RenderTransformOrigin != new Point())
            {
                for (int i = 0; i < bounds.Length; i++)
                    bounds[i] = bounds[i] - new Vec(elt.ActualWidth * elt.RenderTransformOrigin.X, elt.ActualHeight * elt.RenderTransformOrigin.Y);
            }
            for (int i = 0; i < bounds.Length; i++)
                bounds[i] = trans.Transform(bounds[i]);
            return bounds;
        }

        public static string       GetAppDir()
        {
            string ans = typeof(WPFUtil).Assembly.Location;//Application.ExecutablePath;

            while (ans.Length > 0)
            {
                if (ans[ans.Length - 1] == '\\')
                    break;
                else
                    ans = ans.Substring(0, ans.Length - 1);
            }

            if (ans.Substring(ans.Length - 1, 1) != "\\")
                ans += "\\";

            return ans;
        }

        public static void         BringToFront(this FrameworkElement elt)
        {
            if (elt.Parent != null)
                Canvas.SetZIndex(elt, GetHighestZIndex((Panel)elt.Parent) + 1);
        }
        public static void         SendToBack(this FrameworkElement elt)
        {
            ChangeZOrder(elt, false);
        }
        public static int          GetHighestZIndex(Panel panel)
        {
            int result = 0;

            foreach (FrameworkElement elt in panel.Children)
                result = Math.Max(Canvas.GetZIndex(elt), result);

            return result;
        }
        // Adapted from: http://www.dotnet-blog.com/index.php/2007/12/24/bringtofront-and-sendtoback-in-wpf/
        static void                ChangeZOrder(FrameworkElement elt, bool bringToFront)
        {

            int iNewIndex = -1;
            Canvas parentElement = (Canvas)elt.Parent;
            if (bringToFront)
            {
                foreach (UIElement elem in parentElement.Children)
                    if (elem.Visibility != Visibility.Collapsed)
                        ++iNewIndex;
            }
            else
            {
                iNewIndex = 0;
            }

            int iOffset = (iNewIndex == 0) ? +1 : -1;
            int iElemCurIndex = Canvas.GetZIndex(elt);

            foreach (UIElement child in parentElement.Children)
            {
                if (child == elt)
                    Canvas.SetZIndex(elt, iNewIndex);
                else
                {
                    int iZIndex = Canvas.GetZIndex(child);

                    if ((bringToFront && iElemCurIndex < iZIndex) ||
                        (!bringToFront && iZIndex < iElemCurIndex))
                    {
                        Canvas.SetZIndex(child, iZIndex + iOffset);
                    }
                }
            }
        }

        public static List<FrameworkElement> EnumerateAncestors(this FrameworkElement elt)
        {
            if (elt == null)
                return new List<FrameworkElement>();

            List<FrameworkElement> output = new List<FrameworkElement>();
            output.Add(elt);

            try
            {
                output.AddRange((elt.Parent as FrameworkElement).EnumerateAncestors());
            }
            catch
            {
            }
            return output;
        }
        public static FrameworkElement       GetEldestAncestor(this FrameworkElement elt)
        {
            return elt.EnumerateAncestors().Last();
        }
        public static Panel                  GetEldestPanelAncestor(this FrameworkElement elt)
        {
            List<FrameworkElement> list = elt.EnumerateAncestors();

            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i] is Panel)
                    return (Panel)list[i];

            return null;
        }
        public static Panel                  GetEldestVisiblePanelAncestor(this FrameworkElement elt)
        {
            List<FrameworkElement> list = elt.EnumerateAncestors();

            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i] is Panel) {
                    Panel result = list[i] as Panel;
                    if (result.IsVisible)
                        return result;
                }

            return null;
        }
        public static ArrayList              GetAllChildren(this Panel panel)
        {
            ArrayList output = new ArrayList();
            output.Add(panel);

            foreach (FrameworkElement elt in panel.Children)
            {
                if (elt is Panel)
                {
                    output.AddRange((elt as Panel).GetAllChildren());
                }
                else if (elt is ContentControl)
                {
                    output.Add(elt);

                    if (((ContentControl)elt).Content is Panel)
                    {
                        output.AddRange((((ContentControl)elt).Content as Panel).GetAllChildren());
                    }
                    else
                    {
                        output.Add(((ContentControl)elt).Content);
                    }
                }
                else if (elt is UserControl)
                {
                    output.Add(elt);

                    if (((UserControl)elt).Content is Panel)
                    {
                        output.AddRange((((UserControl)elt).Content as Panel).GetAllChildren());
                    }
                    else
                    {
                        output.Add(((UserControl)elt).Content);
                    }
                }
                else if (elt is Decorator)
                {
                    output.Add(elt);

                    UIElement decChild = ((Decorator)elt).Child;

                    if (decChild is Panel)
                    {
                        output.AddRange((decChild as Panel).GetAllChildren());
                    }
                    else
                    {
                        output.Add(decChild);
                    }
                }
                else
                {
                    output.Add(elt);
                }
            }
            return output;
        }
        public static FrameworkElement       GetCommonAncestor(FrameworkElement eltA, FrameworkElement eltB)
        {
            List<FrameworkElement> ancestorsB = eltB.EnumerateAncestors();

            foreach (FrameworkElement elt in eltA.EnumerateAncestors())
                if (ancestorsB.Contains(elt))
                    return elt;

            return null;
        }
        public static void                   ForceUpdateLayout(this FrameworkElement elt)
        {
            GetEldestAncestor(elt).UpdateLayout();
        }
    }
}
