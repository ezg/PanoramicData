#if SURFACE
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using starPadSDK.Inq.MSInkCompat;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using starPadSDK.AppLib;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using InputFramework;
using InputFramework.WPFDevices;
using InputFramework.DeviceDriver;

namespace starPadSDK.SurfaceLib
{
    public class MirrorSelectCommand : OneStrokeGesture
    {
        InqScene _can;
        public List<SelectionInfo> _selectionInfoList { get; set; }

        SelectionInfo selectInfo;

        public class SelectionInfo
        {
            public Stroq inputStroke;
            public Stroq mirrorStroke;
            public Polyline selectDash;

            public Pt startPt;
            public Pt midPt;
            public Pt endPt;

            public object penDevice;
            public bool selectMode;
        }

        private SelectionInfo getNearestSelectionInfo(Pt pos)
        {
            SelectionInfo selectInfo = null;
            double minLength = Double.MaxValue;

            foreach (SelectionInfo info in _selectionInfoList)
            {
                Vector vec = new Vector(pos.X - info.endPt.X, pos.Y - info.endPt.Y);

                if (vec.Length < minLength)
                {
                    minLength = vec.Length;
                    selectInfo = info;
                }
            }

            return selectInfo;
        }
        private Polyline getDashLine(Stroq stroq)
        {
            Polyline line = new Polyline();
            line.Stroke = Brushes.Black;
            line.Opacity = 0.3;
            line.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });

            foreach (StylusPoint sp in stroq.StylusPoints)
            {
                line.Points.Add(new Pt(sp.X, sp.Y));
            }

            return line;
        }
        private bool isSmoothCurve(Stroq stroq)
        {
            return (stroq.Cusps().Length == 2 && stroq.Cusps().Straightness() > 0.2 && (stroq[-1] - stroq[0]).Length > 100);
        }
        void PointDownEvent(object sender, RoutedPointEventArgs e)
        {
            if (e.PointEventArgs.DeviceType == DeviceType.MultiTouch)
                return;
            Pt pos = e.GetPosition(_can);

            SelectionInfo selectInfo = new SelectionInfo();

            StylusPointCollection spCollection = new StylusPointCollection();
            spCollection.Add(new StylusPoint(pos.X, pos.Y));

            Stroq stroq = new Stroq(spCollection);

            selectInfo.inputStroke = stroq;
            selectInfo.mirrorStroke = stroq;
            selectInfo.selectDash = getDashLine(stroq);

            selectInfo.startPt = pos;
            selectInfo.midPt = pos;
            selectInfo.endPt = pos;

            selectInfo.penDevice = e.WPFPointDevice.DeviceUID;
            selectInfo.selectMode = false;

            _selectionInfoList.Add(selectInfo);
        }
        void PointUpEvent(object sender, RoutedPointEventArgs e)
        {
            SelectionInfo selectInfo = getNearestSelectionInfo(e.GetPosition(_can));

            if (selectInfo != null && !selectInfo.selectMode)
            {
                _selectionInfoList.Remove(selectInfo);

                _can.Rem(selectInfo.selectDash);
            }
        }
        void PointDragEvent(object sender, RoutedPointEventArgs e)
        {
            if (_can.CollectedPoints(e.PointEventArgs.DeviceUID) != null)
            {
                Pt pos = e.GetPosition(_can);

                SelectionInfo selectInfo = getNearestSelectionInfo(pos);

                if (selectInfo == null || selectInfo.selectMode)
                    return;

                selectInfo.inputStroke.Add(pos);

                if (!isSmoothCurve(selectInfo.inputStroke))
                {
                    _can.Rem(selectInfo.selectDash);

                    return;
                }

                selectInfo.endPt = pos;
                selectInfo.midPt = new Pt((selectInfo.startPt.X + selectInfo.endPt.X) / 2, (selectInfo.startPt.Y + selectInfo.endPt.Y) / 2);

                Matrix mat = new Matrix();
                mat.RotateAt(180, selectInfo.midPt.X, selectInfo.midPt.Y);

                selectInfo.mirrorStroke = selectInfo.inputStroke.Clone();
                selectInfo.mirrorStroke.XformBy(mat);

                _can.Rem(selectInfo.selectDash);

                selectInfo.selectDash = getDashLine(selectInfo.mirrorStroke);
                selectInfo.selectDash.IsHitTestVisible = false;  // bcz: Why is this needed? without this, sometimes the stroke being drawn is never processed by InqCanvas into a real stroke
                _can.SceneLayer.Children.Add(selectInfo.selectDash);

            }
        }

        public delegate void MirrorSelectHandler(Stroq stroq, SelectionInfo selectInfo);
        public event MirrorSelectHandler MirrorSelectEvent;

        public override bool Test(Stroq s, object device)
        {
            CommandSet.PauseData pdata = (CommandSet.PauseData)s.Property[CommandSet.PauseData.PauseDataGuid];

            foreach (SelectionInfo selectInfo in _selectionInfoList)
            {
                PointCollection pCollection = new PointCollection();

                Stroq wholeStroke = selectInfo.inputStroke.Clone();

                foreach (StylusPoint sp in selectInfo.mirrorStroke.StylusPoints)
                    wholeStroke.Add(sp);

                foreach (StylusPoint sp in wholeStroke.StylusPoints)
                    pCollection.Add(new Point(sp.X, sp.Y));

                StylusPointCollection spCollection = new StylusPointCollection();
                spCollection.Add(new StylusPoint(pdata.Touch.X, pdata.Touch.Y));

                Stroq stroq = new Stroq(spCollection);

                if (stroq.BackingStroke.HitTest(pCollection, 100))
                {
                    _can.SetInkEnabled(selectInfo.penDevice, false);

                    _can.Rem(selectInfo.selectDash);

                    selectInfo.inputStroke = wholeStroke;
                    selectInfo.mirrorStroke = wholeStroke;
                    selectInfo.selectDash = getDashLine(selectInfo.inputStroke);
                    selectInfo.selectDash.Opacity = 1.0;

                    _can.SceneLayer.Children.Add(selectInfo.selectDash);

                    selectInfo.selectMode = true;

                    this.selectInfo = selectInfo;

                    return true;
                }
            }

            return false;
        }
        MirrorSelection _mselect = null;
        public MirrorSelectCommand(InqScene can)
        {
            _can = can;

            _selectionInfoList = new List<SelectionInfo>();

            _can.AddHandler(WPFPointDevice.PreviewPointDownEvent, new RoutedPointEventHandler(PointDownEvent));
            _can.AddHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(PointDragEvent));
            _can.AddHandler(WPFPointDevice.PreviewPointUpEvent, new RoutedPointEventHandler(PointUpEvent));

            _mselect = new MirrorSelection(can, this);
        }
        public override void Clear()
        {
            _can.RemoveHandler(WPFPointDevice.PreviewPointDownEvent, new RoutedPointEventHandler(PointDownEvent));
            _can.RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(PointDragEvent));
            _can.RemoveHandler(WPFPointDevice.PreviewPointUpEvent, new RoutedPointEventHandler(PointUpEvent));
        }
        public override void Fire(Stroq[] strokes, object device)
        {
            _can.SetInkEnabled(device, false);
            if (MirrorSelectEvent != null)
                MirrorSelectEvent(strokes[0], selectInfo);
        }
        public class MirrorSelection
        {
            InqScene _scene;
            List<MirrorSelectCommand.SelectionInfo> _selectionInfoList;

            Button button;

            public MirrorSelection(InqScene scene, MirrorSelectCommand command)
            {
                _scene = scene;
                _selectionInfoList = command._selectionInfoList;

                button = new Button();
                Contacts.AddPreviewContactUpHandler(button, endPen);

                command.MirrorSelectEvent -= new MirrorSelectCommand.MirrorSelectHandler(mirrorSelectEvent);
                command.MirrorSelectEvent += new MirrorSelectCommand.MirrorSelectHandler(mirrorSelectEvent);
            }
            void mirrorSelectEvent(Stroq stroq, SelectionInfo selectInfo)
            {
                SurfacePauseData pdata = stroq.Property[CommandSet.PauseData.PauseDataGuid] as SurfacePauseData;

                Contacts.AddPreviewContactChangedHandler(selectInfo.selectDash, dragSelectionBox);
                Contacts.AddPreviewContactUpHandler(selectInfo.selectDash, endSelectionBox);
                Contacts.CaptureContact(pdata.ContactDev.Contact(), selectInfo.selectDash);
                //Contacts.CaptureContact(pdata.PenID, selectInfo.selectDash);
            }
            void endPen(object sender, ContactEventArgs e)
            {
                e.Handled = true;
            }
            void endSelectionBox(object sender, ContactEventArgs e)
            {
                MirrorSelectCommand.SelectionInfo selectInfo = getSelectionInfo(sender as Polyline);

                PointCollection pCollection = new PointCollection();

                foreach (StylusPoint sp in selectInfo.mirrorStroke.StylusPoints)
                {
                    pCollection.Add(new Point(sp.X, sp.Y));
                }

                List<Stroq> stroqs = new List<Stroq>();
                List<FrameworkElement> elements = new List<FrameworkElement>();

                foreach (Stroq s in _scene.Stroqs)
                {
                    if (isSelected(pCollection, s))
                    {
                        stroqs.Add(s);
                    }
                }

                foreach (FrameworkElement element in _scene.Elements)
                {
                    if (isSelected(pCollection, element))
                    {
                        elements.Add(element);
                    }
                }

                _scene.SetSelection(selectInfo.penDevice, new SelectionObj(stroqs, elements, selectInfo.mirrorStroke.ToArray()));

                Group gr = _scene.Groups.Create(_scene.Selection(selectInfo.penDevice).Strokes, _scene.Selection(selectInfo.penDevice).Elements);
                if (gr.Elements.Length != 0 || gr.Strokes.Length != 0 || gr.Groups.Length != 1)
                    _scene.Groups.Add(gr);

                _selectionInfoList.Remove(selectInfo);

                removeElement(selectInfo.selectDash);

                e.Handled = true;
            }
            void dragSelectionBox(object sender, ContactEventArgs e)
            {
                MirrorSelectCommand.SelectionInfo selectInfo = getSelectionInfo(sender as Polyline);

                Stroq cloneStroke = selectInfo.inputStroke.Clone();

                Point pos = e.GetPosition(_scene);

                Vector xAxis = new Vector(1, 0);
                Vector vec = new Vector(pos.X - selectInfo.midPt.X, pos.Y - selectInfo.midPt.Y);

                Double angle = Vector.AngleBetween(xAxis, vec);

                Matrix mat = new Matrix();
                mat.Translate(-selectInfo.midPt.X, -selectInfo.midPt.Y);
                mat.Rotate(-angle);

                cloneStroke.XformBy(mat);

                int index = -1;
                double minY = Double.MaxValue;
                double deviation = Math.Max(cloneStroke.BackingStroke.StylusPoints.Count / 50.0, 12.0);

                for (int i = 0; i < cloneStroke.BackingStroke.StylusPoints.Count; i++)
                {
                    StylusPoint sp = cloneStroke.BackingStroke.StylusPoints[i];

                    if (Math.Abs(sp.Y) < minY && sp.X > 0)
                    {
                        index = i;
                        minY = Math.Abs(sp.Y);
                    }
                }

                if (index >= 0 && cloneStroke.BackingStroke.StylusPoints[index].X <= vec.Length)
                {
                    int pointCount = selectInfo.inputStroke.BackingStroke.StylusPoints.Count;

                    double offsetX = pos.X - selectInfo.inputStroke.BackingStroke.StylusPoints[index].X;
                    double offsetY = pos.Y - selectInfo.inputStroke.BackingStroke.StylusPoints[index].Y;

                    int startIndex = index - pointCount / 4;
                    int endIndex = index + pointCount / 4;

                    mat = new Matrix();
                    mat.Rotate(angle);
                    mat.Translate(selectInfo.midPt.X, selectInfo.midPt.Y);

                    cloneStroke.XformBy(mat);

                    selectInfo.mirrorStroke = cloneStroke;

                    int currentIndex = 0;

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        currentIndex = getIndex(i, pointCount);

                        cloneStroke.BackingStroke.StylusPoints[currentIndex]
                            = new StylusPoint(cloneStroke.BackingStroke.StylusPoints[currentIndex].X + offsetX,
                                              cloneStroke.BackingStroke.StylusPoints[currentIndex].Y + offsetY);
                    }

                    if (startIndex == 0)
                    {
                        cloneStroke.BackingStroke.StylusPoints.Add(cloneStroke.BackingStroke.StylusPoints[0]);
                    }

                    if (currentIndex + 1 == pointCount)
                    {
                        cloneStroke.BackingStroke.StylusPoints.Add(cloneStroke.BackingStroke.StylusPoints[0]);
                    }

                    Collection<Pt> ptCollection = new Collection<Pt>();
                    PointCollection pCollection = new PointCollection();

                    foreach (StylusPoint sp in cloneStroke.StylusPoints)
                    {
                        ptCollection.Add(new Pt(sp.X, sp.Y));
                        pCollection.Add(new Point(sp.X, sp.Y));
                    }

                    foreach (Stroq s in _scene.Stroqs)
                    {
                        if (isSelected(pCollection, s))
                        {
                            Rct rect = s.GetBounds();
                            ptCollection.Add(new StylusPoint(rect.TopLeft.X, rect.TopLeft.Y));
                            ptCollection.Add(new StylusPoint(rect.TopRight.X, rect.TopRight.Y));
                            ptCollection.Add(new StylusPoint(rect.BottomRight.X, rect.BottomRight.Y));
                            ptCollection.Add(new StylusPoint(rect.BottomLeft.X, rect.BottomLeft.Y));
                        }
                    }

                    foreach (FrameworkElement element in _scene.Elements)
                    {
                        if (isSelected(pCollection, element))
                        {
                            Rct rect = WPFUtil.GetBounds(element);
                            ptCollection.Add(new StylusPoint(rect.TopLeft.X, rect.TopLeft.Y));
                            ptCollection.Add(new StylusPoint(rect.TopRight.X, rect.TopRight.Y));
                            ptCollection.Add(new StylusPoint(rect.BottomRight.X, rect.BottomRight.Y));
                            ptCollection.Add(new StylusPoint(rect.BottomLeft.X, rect.BottomLeft.Y));
                        }
                    }

                    IEnumerable ptEnum = GeomUtils.ConvexHull(ptCollection);
                    PointCollection dashCollection = new PointCollection();

                    foreach (Pt p in ptEnum)
                    {
                        dashCollection.Add(p);
                    }

                    selectInfo.selectDash.Points = dashCollection;
                }
            }

            private MirrorSelectCommand.SelectionInfo getSelectionInfo(Polyline selectDash)
            {
                MirrorSelectCommand.SelectionInfo selectInfo = null;

                foreach (MirrorSelectCommand.SelectionInfo info in _selectionInfoList)
                {
                    if (selectDash == info.selectDash)
                        selectInfo = info;
                }

                return selectInfo;
            }
            private int getIndex(int i, int pointCounts)
            {
                if (i < 0)
                {
                    return pointCounts + i;
                }
                else if (i >= pointCounts)
                {
                    return i - pointCounts;
                }

                return i;
            }
            private bool isSelected(PointCollection pCollection, Stroq stroq)
            {
                return stroq.BackingStroke.HitTest(pCollection, 50);
            }
            private bool isSelected(PointCollection pCollection, FrameworkElement element)
            {
                Rct rect = WPFUtil.GetBounds(element);

                StylusPointCollection spCollection = new StylusPointCollection();

                spCollection.Add(new StylusPoint(rect.TopLeft.X, rect.TopLeft.Y));
                spCollection.Add(new StylusPoint(rect.TopRight.X, rect.TopRight.Y));
                spCollection.Add(new StylusPoint(rect.BottomRight.X, rect.BottomRight.Y));
                spCollection.Add(new StylusPoint(rect.BottomLeft.X, rect.BottomLeft.Y));
                spCollection.Add(new StylusPoint(rect.TopLeft.X, rect.TopLeft.Y));

                Stroq stroq = new Stroq(spCollection);

                return stroq.BackingStroke.HitTest(pCollection, 50);
            }
            private void removeElement(FrameworkElement element)
            {
                _scene.Rem(element);
                _scene.SceneLayer.Children.Remove(element);
            }
        }
    }
}
#endif