using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.WPFHelp;

namespace starPadSDK.AppLib {
    public class EndPt : Canvas {
        public EndPt(Pt location) {
            Width = Height = 3;
            RenderTransform = new TranslateTransform(location.X-Width/2, location.Y-Height/2);
            Ellipse handl = new Ellipse();
            handl.Fill  = Brushes.Red;
            handl.Width = handl.Height = 3;
            handl.VerticalAlignment   = VerticalAlignment.Top;
            handl.HorizontalAlignment = HorizontalAlignment.Left;
            Children.Add(handl);
        }
    }
    public class Wiggle {
        void drawLine(Pt a, Pt b) {
            Stroq   l = Line[0];

            double sdelta = (a-b).Length/(l[-1]-l[0]).Length; sdelta = double.IsNaN(sdelta) || double.IsInfinity(sdelta) ? 1 : sdelta;
            Deg    ang    = (l[-1] - l[0]).SignedAngle(b - a);

            ScaleTransform     st = new ScaleTransform(sdelta, sdelta, l[0].X, l[0].Y);
            TranslateTransform tt = new TranslateTransform(a.X - l[0].X, a.Y - l[0].Y);
            RotateTransform    rt = new RotateTransform(ang.D, l[0].X, l[0].Y);

            l.XformBy((Mat) st. Value * (Mat)tt.Value*(Mat)rt.Value);
        }
        void endMoved(object sender, EventArgs e) { 
            if (A.Parent != null && B.Parent != null) 
                drawLine(WPFUtil.GetBounds(A).Center, WPFUtil.GetBounds(B).Center); 
        }  
        public Wiggle(Stroq line) : this(new EndPt(line[0]), new EndPt(line[-1]), new Stroq[] { line }) { }
        public Wiggle(EndPt a, EndPt b, Stroq[] line) {
            Line = line;
            A = a;
            B = b;
            a.LayoutUpdated += new EventHandler(endMoved);
            b.LayoutUpdated += new EventHandler(endMoved);
        }

        public Stroq[] Line { get; set; }
        public EndPt   B    { get; set; }
        public EndPt   A    { get; set; }
    }
}
