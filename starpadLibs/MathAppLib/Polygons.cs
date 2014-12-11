using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.Serialization.Formatters.Binary;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;

namespace starPadSDK.AppLib
{
    public class PolygonBase : Canvas
    {
        protected Polygon         p = new Polygon();
        protected InqScene        can = null;
        public PolygonBase()
        {
            Children.Add(p);
        }
        public virtual PointCollection Points
        {
            get { return p.Points; }
            set
            {
                p.Points = value;
            }
        }
        public virtual Point[] TransformedPoints
        {
            get
            {
                List<Point> pts = new List<Point>();
                foreach (Pt p in Points)
                    pts.Add(RenderTransform.Transform(p));
                return pts.ToArray();
            }
            set
            {
                Pt p1 = value[0];
                Pt p2 = value[1];
                Pt p3 = value[2];
                double ang1 = -new LnSeg(p2, p1).SignedAngle(new LnSeg(new Pt(1, 0), new Pt()));
                double ang2 = -new LnSeg(p1, p2).SignedAngle(new LnSeg(new Pt(1, 0), new Pt()));

                if ((p2 - p1).Det(p3 - p2) < 0) 
                    RenderTransform = new MatrixTransform(Mat.Rotate(ang2, new Pt()) * Mat.Translate(p2));
                else
                    RenderTransform = new MatrixTransform(Mat.Rotate(ang1, new Pt()) * Mat.Translate(p1));
                List<Point> pts = new List<Point>();
                Mat inv = ((Mat)RenderTransform.Value).Inverse();
                foreach (Pt p in value)
                    pts.Add(inv * p);
                Points = new PointCollection(pts.ToArray());
                Width = (p2 - p1).Length;
                Height = 1;
                foreach (Pt p in value)
                    if (new LnSeg(p1, p2).Distance(p) > Height)
                        Height = new LnSeg(p1, p2).Distance(p);
            }
        }
        public Rct             Bounds
        {
            get
            {
                Rct bounds = Rct.Null;
                foreach (Pt p in TransformedPoints)
                    bounds = bounds.Union(p);
                return bounds;
            }
        }
        public Brush           Fill
        {
            get
            {
                return p.Fill;
            }
            set
            {
                p.Fill = value;
            }
        }
        public Brush           Stroke
        {
            get
            {
                return p.Stroke;
            }
            set
            {
                p.Stroke = value;
            }
        }
        public InqScene        getCan()  { return can; }
        public virtual void    polyButtonTap(object sender) { }
        public void setBorderThickness(double value) {p.StrokeThickness = value; }
    }

    public class Triangle : PolygonBase
    {
        Polygon rightAngleMarker = null;

        void addRightAngleMarker(int value)
        {
            //it is a right triangle, draw the little square
            Polygon poly = new Polygon();
            LnSeg a, b;
            if (value == 1)
            {
                a = new LnSeg(p.Points[1], p.Points[2]);
                b = new LnSeg(p.Points[2], p.Points[0]);
            }
            else if (value == 2)
            {
                a = new LnSeg(p.Points[0], p.Points[2]);
                b = new LnSeg(p.Points[2], p.Points[0]);
            }
            else
            {
                a = new LnSeg(p.Points[0], p.Points[1]);
                b = new LnSeg(p.Points[1], p.Points[2]);
            }

            Pt firstPtB, firstPtA;
            double ratio = 0, ratio2 = 0;


            //beginning seg is b
            if (a.Length < b.Length)
            {
                ratio = 0.2;
                ratio2 = 0.2 * a.Length / b.Length;
            }
            else
            {
                ratio = 0.2 * b.Length / a.Length;
                ratio2 = 0.2;
            }
            firstPtB = new Point(ratio2 * (b.B.X - b.A.X) + b.A.X, ratio2 * (b.B.Y - b.A.Y) + b.A.Y);

            firstPtA = new Point(ratio * (a.A.X - a.B.X) + a.B.X, ratio * (a.A.Y - a.B.Y) + a.B.Y);

            Vector vec = new Vector(firstPtA.X - b.A.X, firstPtA.Y - b.A.Y);
            Vector vec2 = new Vector(firstPtB.X - b.A.X, firstPtB.Y - b.A.Y);

            Vector vec3 = vec + vec2;
            Pt lastPoint = new Point(b.A.X + vec3.X, b.A.Y + vec3.Y);


            poly.Points = new PointCollection(new Point[] { b.A, firstPtB, lastPoint, firstPtA });

            poly.Stroke = Brushes.Red;
            Children.Add(poly);
            rightAngleMarker = poly;
        }

        public Triangle(InqScene c, Pt p1, Pt p2, Pt p3)
        {
            can = c;
            p.Points = new PointCollection(new Point[] { p1, p2, p3 });

            int value = isRightTriangle(p);
            if (value != -1)
                addRightAngleMarker(value);
        }
        public override PointCollection Points
        {
            get
            {
                return base.Points;
            }
            set
            {
                base.Points = value;
                if (rightAngleMarker != null)
                    Children.Remove(rightAngleMarker);
                rightAngleMarker = null;
                int angle = isRightTriangle(p);
                if (angle != -1)
                    addRightAngleMarker(angle);
            }
        }

        static public int isRightTriangle(Polygon p)
        {
            //check if it is a right triangle
            Pt a = p.Points[0];
            Pt b = p.Points[1];
            Pt c = p.Points[2];

            double l1, l2, l3;
            l1 = new LnSeg(a, b).Length2;
            l2 = new LnSeg(b, c).Length2;
            l3 = new LnSeg(a, c).Length2;

            if (l1 > l2 && l1 > l3)
            {
                if (l1 >= 0.9 * (l2 + l3) && l1 <= 1.2 * (l2 + l3))
                    return 1;
            }
            else if (l2 > l1 && l2 > l3)
            {
                if (l2 >= 0.9 * (l1 + l3) && l2 <= 1.2 * (l1 + l3))
                    return 2;
            }
            else if (l3 > l2 && l3 > l1)
            {
                if (l3 >= 0.9 * (l1 + l2) && l3 <= 1.2 * (l1 + l2))
                    return 3;
            }
            return -1;
        }
    }

    public class PolyRectangle : PolygonBase
    {
        private int                   anchor;
        private ContainerVisualHost[] sideExprs;
        private double[]              sideLengths;
        private Polygon[]             sideButtons;

        public override void polyButtonTap(object sender) {
            Polygon       poly  = sender as Polygon;
            ButtonTag     tag   = poly.Tag as ButtonTag;
            PolyRectangle prect = tag.PolyBase as PolyRectangle;
            foreach (Stroq r in tag.Range) // remove ink labels
                can.Rem(r);
            double newSideLength = (double)tag.eTag;

            int typesetExprFontSize = 25;
            ContainerVisualHost cvh  = EWPF.ToVisual(tag.eTag, typesetExprFontSize, Colors.Orange, Brushes.White, EWPF.DrawTop);
            ContainerVisualHost cvh2 = EWPF.ToVisual(tag.eTag, typesetExprFontSize, Colors.Orange, Brushes.White, EWPF.DrawTop);

            prect.setSideLength(tag.Side, newSideLength);
            prect.setSideLength((tag.Side + 2) % 4, newSideLength);
            if (prect.getButton(tag.Side) != null)
                prect.Children.Remove(prect.getButton(tag.Side));
            prect.Children.Remove(poly);
            if (!prect.isFirstAssciation(tag.Side)) {
                prect.autoResize();
                prect.updateOppositeExpressions();
            }
            prect.setupLabels(tag.Side, cvh);
            prect.setupLabels((tag.Side + 2) % 4, cvh2);
        }
        public double getSideLength(int side) { return sideLengths[side]; }
        public void setSideLength(int side, double length) { sideLengths[side] = length; }

        public override Point[] TransformedPoints
        {
            get
            {
                return base.TransformedPoints;
            }
            set
            {
                // base.Points = value;
                Point p1 = value[0];
                Point p2 = value[1];
                Point p3 = value[2];
                Point p4 = value[3];
                double ang = -new LnSeg(p2, p1).SignedAngle(new LnSeg(new Pt(1, 0), new Pt()));

                base.Points = new PointCollection(new Point[] { new Point(), new Point((p2-p1).Length,0), 
                                                               new Point((p2-p1).Length, (p3-p2).Length), 
                                                              new Point(0, (p3-p2).Length)});
                RenderTransform = new MatrixTransform(Mat.Rotate(ang, new Pt()) * Mat.Translate(p1));
                Width = (p2 - p1).Length;
                Height = (p3 - p2).Length;
                for (int i = 0; i < sideButtons.Length; i++)
                {
                    if (sideButtons[i] != null)
                        this.updateButton(i);
                }
            }
        }

        public PolyRectangle(InqScene c, Pt p1, Pt p2, Pt p3, Pt p4)
        {
            can = c;
            anchor = -1;
            sideLengths = new double[4] { -1, -1, -1, -1 };
            sideButtons = new Polygon[4];
            sideExprs = new ContainerVisualHost[4];
            TransformedPoints = new Point[] { p1, p2, p3, p4 };
            Width  = (p2 - p1).X;
            Height = (p3 - p1).Y;
        }

        public delegate void buttonAddedHandler(Polygon button, EventArgs e);
        public event buttonAddedHandler buttonAddedEvent;
        public void displayAssociationButton(int side, Expr etag, IEnumerable<Stroq> stroqs)
        {
            if (sideButtons[side] != null || sideLengths[side] != -1)
                return;
            sideButtons[side] = new Polygon();
            sideButtons[side].MouseDown += new MouseButtonEventHandler((object o, MouseButtonEventArgs e) => polyButtonTap(o));
            sideButtons[side].Tag = new ButtonTag(this, side, etag, new List<Stroq>(stroqs));
            if (buttonAddedEvent != null)
                buttonAddedEvent(sideButtons[side], null);
            updateButton(side);
            sideButtons[side].Stroke = Brushes.Blue;
            sideButtons[side].Fill = new SolidColorBrush(Color.FromArgb(60, 255, 99, 71));
            Children.Add(sideButtons[side]);
        }

        public void updateButton(int side)
        {
            Pt a = new Pt(), b = new Pt(), c = new Pt(), d = new Pt();
            switch (side)
            {
                case 0:
                    a = new Pt(this.Points[0].X + 1 / 3.0 * (this.Points[1].X - this.Points[0].X), this.Points[0].Y);
                    b = new Pt(this.Points[0].X + 2 / 3.0 * (this.Points[1].X - this.Points[0].X), this.Points[0].Y);
                    c = new Pt(this.Points[0].X + 2 / 3.0 * (this.Points[1].X - this.Points[0].X), this.Points[0].Y + 1/6.0*(this.Points[2].Y - this.Points[1].Y));
                    d = new Pt(this.Points[0].X + 1 / 3.0 * (this.Points[1].X - this.Points[0].X), this.Points[0].Y + 1 / 6.0 * (this.Points[2].Y - this.Points[1].Y));
                    break;
                case 1:
                    a = new Pt(this.Points[1].X, this.Points[1].Y + 1 / 3.0 * (this.Points[2].Y - this.Points[1].Y));
                    b = new Pt(this.Points[1].X, this.Points[1].Y + 2 / 3.0 * (this.Points[2].Y - this.Points[1].Y));
                    c = new Pt(this.Points[1].X - 1 / 6.0 * (this.Points[1].X - this.Points[0].X), this.Points[1].Y + 2 / 3.0 * (this.Points[2].Y - this.Points[1].Y));
                    d = new Pt(this.Points[1].X - 1 / 6.0 * (this.Points[1].X - this.Points[0].X), this.Points[1].Y + 1 / 3.0 * (this.Points[2].Y - this.Points[1].Y));
                    break;
                case 2:
                    a = new Pt(this.Points[0].X + 1 / 3.0 * (this.Points[1].X - this.Points[0].X), this.Points[3].Y - 1/6.0*(this.Points[3].Y - this.Points[0].Y));
                    b = new Pt(this.Points[0].X + 2 / 3.0 * (this.Points[1].X - this.Points[0].X), this.Points[3].Y - 1 / 6.0 * (this.Points[3].Y - this.Points[0].Y));
                    c = new Pt(this.Points[0].X + 2 / 3.0 * (this.Points[1].X - this.Points[0].X), this.Points[3].Y);
                    d = new Pt(this.Points[0].X + 1 / 3.0 * (this.Points[1].X - this.Points[0].X), this.Points[3].Y);
                    break;
                case 3:
                    a = new Pt(this.Points[0].X, this.Points[0].Y + 1 / 3.0 * (this.Points[2].Y - this.Points[1].Y));
                    b = new Pt(this.Points[0].X + 1 / 6.0 * (this.Points[1].X - this.Points[0].X), this.Points[0].Y + 1 / 3.0 * (this.Points[2].Y - this.Points[1].Y));
                    c = new Pt(this.Points[0].X + 1 / 6.0 * (this.Points[1].X - this.Points[0].X), this.Points[0].Y + 2 / 3.0 * (this.Points[2].Y - this.Points[1].Y));
                    d = new Pt(this.Points[0].X, this.Points[0].Y + 2 / 3.0 * (this.Points[2].Y - this.Points[1].Y));
                    break;
            }
            sideButtons[side].Points = new PointCollection(new Point[] { a, b, c, d });
        }

        public bool isFirstAssciation(int side)
        {
            if (anchor == -1)
            {
                anchor = side;
                return true;
            }
            return false;
        }

        public void removeButtons()
        {
            for (int i = 0; i < sideButtons.Length; i++)
                if (sideButtons[i] != null) {
                    Children.Remove(sideButtons[i]);
                    sideButtons[i] = null;
                }
        }

        public Polygon getButton(int side)
        {
            return sideButtons[side];
        }

        public void autoResize()
        {
            switch (anchor)
            {
                case 0:
                    LnSeg top = new LnSeg(this.Points[0], this.Points[1]);
                    double anchorLengthTop = top.Length;
                    double newLengthTop = anchorLengthTop * (sideLengths[1] / (double)sideLengths[0]);
                    this.Points[2] = new Pt(this.Points[2].X, this.Points[0].Y + newLengthTop);
                    this.Points[3] = new Pt(this.Points[3].X, this.Points[0].Y + newLengthTop);
                    break;
                case 1:
                    LnSeg right = new LnSeg(this.Points[1], this.Points[2]);
                    double anchorLengthRight = right.Length;
                    double newLengthRight = anchorLengthRight * (sideLengths[0] / (double)sideLengths[1]);
                    this.Points[0] = new Pt(this.Points[1].X - newLengthRight, this.Points[0].Y);
                    this.Points[3] = new Pt(this.Points[1].X - newLengthRight, this.Points[3].Y);
                    break;
                case 2:
                    LnSeg bottom = new LnSeg(this.Points[2], this.Points[3]);
                    double anchorLengthBottom = bottom.Length;
                    double newLengthBottom = anchorLengthBottom * (sideLengths[1] / (double)sideLengths[0]);
                    Points[0] = Points[3] - new Vec(0, newLengthBottom);
                    Points[1] = Points[2] - new Vec(0, newLengthBottom);
                    break;
                case 3:
                    LnSeg left = new LnSeg(this.Points[0], this.Points[3]);
                    double anchorLengthLeft = left.Length;
                    double newLengthLeft = anchorLengthLeft * (sideLengths[0] / (double)sideLengths[1]);
                    Points[1] = Points[0] + new Vec(newLengthLeft, 0);
                    Points[2] = Points[3] + new Vec(newLengthLeft, 0);
                    break;
            }
        }

        public void updateLabels(double xRatio, double yRatio)
        {
            if (xRatio != 1 && sideLengths[0] != -1)
            {
                sideLengths[2] = sideLengths[0] *= xRatio;
                setupLabels(0, EWPF.ToVisual(Math.Round(sideLengths[0],1), 25, Colors.Orange, Brushes.White, EWPF.DrawTop)); 
                setupLabels(2, EWPF.ToVisual(Math.Round(sideLengths[0],1), 25, Colors.Orange, Brushes.White, EWPF.DrawTop));
            }
            if (yRatio != 1 && sideLengths[1] != -1)
            {
                sideLengths[1]= sideLengths[3] *= yRatio;
                setupLabels(1, EWPF.ToVisual(Math.Round(sideLengths[1], 1), 25, Colors.Orange, Brushes.White, EWPF.DrawTop)); 
                setupLabels(3, EWPF.ToVisual(Math.Round(sideLengths[1], 1), 25, Colors.Orange, Brushes.White, EWPF.DrawTop));
            }
        }

        public void setupLabels(int side, ContainerVisualHost cvh)
        {
            Pt typesetExprOffset = Pt.Avg(Points[side],Points[(side+1)%4]) + 
                                        ((Pt)Points[side] - (Pt)Points[(side+1)%Points.Count]).Perp().Normal() * 20 +
                                        -new Vec(cvh.Width / 2, cvh.Height / 2);
            cvh.RenderTransform = new MatrixTransform(Mat.Translate(typesetExprOffset));
            Children.Remove(sideExprs[side]);
            Children.Add(sideExprs[side] = cvh);
        }

        public void updateOppositeExpressions() {
            for (int i = 0; i < sideExprs.Length; i++)
                if (sideExprs[i] != null)
                    setupLabels(i, sideExprs[i]);
        }

        public void addSideExpressions()
        {
            foreach (ContainerVisualHost cvh in sideExprs)
            {
                Children.Remove(cvh);
                if (cvh != null)
                    Children.Add(cvh);
            }
        }

        public bool isLabeled()
        {
            for (int i = 0; i < sideLengths.Length; i++)
            {
                if (sideLengths[i] != -1)
                    return true;
            }
            return false;
        }

        public double getUnitLengthPerPixel()
        {
            if (sideLengths[0] != -1)
                return (sideLengths[0] / new LnSeg(Points[0], Points[1]).Length);
            return (sideLengths[1] / new LnSeg(Points[1], Points[2]).Length);
        }
    }

    public class ButtonTag
    {
        public ButtonTag(PolygonBase pbase, int mySide, Expr expr, List<Stroq> rng)
        {
            PolyBase = pbase;
            Side     = mySide;
            eTag     = expr;
            Range    = rng;
        }

        public PolygonBase PolyBase { get; set; }
        public int Side             { get; set; }
        public Expr eTag            { get; set; }
        public List<Stroq> Range    { get; set; }
    }

    public class Cube : PolygonBase
    {
        private double _x, _y;
        public Cube(PointCollection orig, double x, double y)
        {
            p.Points = orig;
            _x = x; _y = y;
            this.Fill = new SolidColorBrush(Color.FromArgb(255, 135, 206, 250));
            this.Stroke = Brushes.Blue;
            Polygon backPanel = this.createBackPanel(x,y);
            this.createSidePanels(backPanel);
        }

        public Polygon createBackPanel(double x, double y)
        {
            Pt[] backPanelPts = new Pt[4];
            for (int i = 0; i < p.Points.Count; i++)
            {
                backPanelPts[i] = new Pt(p.Points[i].X + x, p.Points[i].Y + y);
            }
            Polygon backPanel = new Polygon();
            backPanel.Points = new PointCollection(new Point[]{backPanelPts[0], backPanelPts[1], backPanelPts[2], backPanelPts[3]});
            backPanel.Fill = new SolidColorBrush(Color.FromArgb(255, 135, 206, 250));
            backPanel.Stroke = Brushes.Blue;
            //Children.Add(backPanel);
            return backPanel;
        }
        public void createSidePanels(Polygon backPanel)
        {
            //for (int i = 0; i < 4; i++)
            //{
                Polygon poly = new Polygon();
                poly.Points = new PointCollection(new Point[] { p.Points[0], p.Points[1], backPanel.Points[1], backPanel.Points[0] });
                poly.Fill = new SolidColorBrush(Color.FromArgb(255, 135, 206, 250));
                poly.Stroke = Brushes.Blue;
                Children.Add(poly);

                Polygon poly2 = new Polygon();
                poly2.Points = new PointCollection(new Point[] { p.Points[1], p.Points[2], backPanel.Points[2], backPanel.Points[1] });
                poly2.Fill = new SolidColorBrush(Color.FromArgb(255, 135, 206, 250));
                poly2.Stroke = Brushes.Blue;
                Children.Add(poly2);
            //}
        }
        public override PointCollection Points
        {
            get
            {
                return base.Points;
            }
            set
            {
                base.Points = value;
                Children.Clear();
                Children.Add(p);
                Polygon backPanel = this.createBackPanel(_x, _y);
                this.createSidePanels(backPanel);
            }
        }
    }
}
