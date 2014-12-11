#if SURFACE
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
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;
using starPadSDK.CharRecognizer;
using starPadSDK.AppLib;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using System.Runtime.InteropServices;
using Line = System.Windows.Shapes.Line;
using InputFramework.DeviceDriver;
using InputFramework.WPFDevices;

namespace starPadSDK.SurfaceLib
{
    public class InteractiveSpaceInsertionCommand : SymbolCommand
    {
        InqScene                  _can;
        BitmapSource              _image;
        InteractiveSpaceInsertion _iinsert;
        Image                     _feedback = new Image();
        Image                     _feedback_bottom = new Image();
        static Dictionary<object, Image> _feedbacks = new Dictionary<object, Image>();
        static Dictionary<object, Image> _feedbackBottoms = new Dictionary<object, Image>();

        void PointUpEvent(object sender, RoutedPointEventArgs e)
        {
            if (_feedbacks.ContainsKey(e.PointEventArgs.DeviceUID))
            {
                _feedbacks[e.PointEventArgs.DeviceUID].Visibility = Visibility.Collapsed;
                _feedbackBottoms[e.PointEventArgs.DeviceUID].Visibility = Visibility.Collapsed;
                _feedbacks.Remove(e.PointEventArgs.DeviceUID);
                _feedbackBottoms.Remove(e.PointEventArgs.DeviceUID);
            }
        }
        void PointDragEvent(object sender, RoutedPointEventArgs e)
        {
            if (e.PointEventArgs.DeviceType == DeviceType.MultiTouch ||
                _can.CollectedPoints(e.PointEventArgs.DeviceUID) == null)
                return;
            // get rid of any previous selection rectangle feedback first
            PointUpEvent(sender, e);
            // if the Point devices has stylus points, then it must be a pen, so we check to 
            // see if the crop mark gesture has been drawn.  If it has, we start displaying the
            // dashed line feedback for the selection rectangle.
            Stroq s = new Stroq(_can.CollectedPoints(e.PointEventArgs.DeviceUID));
            var fd = new starPadSDK.CharRecognizer.FeaturePointDetector();
            var features = fd.FeaturePoints(s.OldStroke());
            // a space insertion is started with a line-hook, or something which closely resembles one.
            if (s.Cusps().Distance > 100 && s.Cusps().Length == 2 && s.Cusps().Straightness(0, 1) < 0.2)
            {
                _feedback.Visibility = Visibility.Visible;
                _feedback.Stretch = Stretch.Uniform;
                _feedback.Source = _image;
                _feedback_bottom.Visibility = Visibility.Visible;
                _feedback_bottom.Stretch = Stretch.Uniform;
                _feedback_bottom.Source = _image;
                //Rectangle feedback = new Rectangle();
                _feedbacks.Add(e.PointEventArgs.DeviceUID, _feedback);
                _feedbackBottoms.Add(e.PointEventArgs.DeviceUID, _feedback_bottom);
                //global::starPadSDK.AppLib.Properties.Resources.spacerImage;
                //feedback.Width = 20;//s.GetBounds().MaxDim;
                //feedback.Height = 5;// s.GetBounds().MaxDim;
                //feedback.Stroke = Brushes.Black;
                _feedback.Opacity = 0.2;
                _feedback_bottom.Opacity = 0.6;
                //feedback.StrokeDashArray = new DoubleCollection(new double[] { 20, 20 });
                // rotate the rectangle to make selection perpendicular to the line drawn
                //double ang /* degrees */ = Math.PI/2 + Math.Atan2(s.GetBounds().Width, s.GetBounds().Height);

                // This is the correct value, but for some reason it doesn't work as expected. What gives?!
                // In particular, it doesn't allow for any selections.
                double ang = Math.Atan2(s.First().Y - s.Last().Y, s.First().X - s.Last().X);

                // the center point of the feedback.
                Pt center = s.First();
                center.X -= _image.Width / 2 * Math.Cos(ang);
                center.Y -= _image.Height / 2 * Math.Sin(ang);
                _feedback.RenderTransform = new MatrixTransform(Mat.Translate(center) * Mat.Rotate(ang, center));
                center.X += _image.Width * Math.Sin(ang);
                center.Y -= _image.Height * Math.Cos(ang);
                _feedback_bottom.RenderTransform = new MatrixTransform(Mat.Translate(center) * Mat.Rotate(ang, center));
            }
        }

        public delegate void InteractiveSpaceInsertionHandler(Stroq s, object device, Rct where, bool top);
        public event InteractiveSpaceInsertionHandler InteractiveSpaceInsertionEvent;

        public InteractiveSpaceInsertionCommand(InqScene can) : base("-")
        {
            _image = global::SurfaceLib.Properties.Resources.spacerImage.LoadBitmap();
            _can = can;
            _can.SceneLayer.Children.Add(_feedback);
            _can.SceneLayer.Children.Add(_feedback_bottom);
            _feedback.Visibility = Visibility.Collapsed;
            _feedback_bottom.Visibility = Visibility.Collapsed;
            _can.AddHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(PointDragEvent));
            _can.AddHandler(WPFPointDevice.PreviewPointUpEvent, new RoutedPointEventHandler(PointUpEvent));
            _iinsert = new InteractiveSpaceInsertion(_can, this);
        }
        public override bool Test(Stroq s, object device)  {
            CommandSet.PauseData pdata = (CommandSet.PauseData)s.Property[CommandSet.PauseData.PauseDataGuid];
            if (_feedbacks.ContainsKey(device))
                if (WPFUtil.GetBounds(_feedbacks[device]).IntersectsWith(new Rct(pdata.Touch).Inflate(10, 10)) ||
                    WPFUtil.GetBounds(_feedbackBottoms[device]).IntersectsWith(new Rct(pdata.Touch).Inflate(10, 10)))
                    return true;
            return false;
        }
        public override void Clear()
        {
            _can.RemoveHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(PointDragEvent));
            _can.RemoveHandler(WPFPointDevice.PreviewPointUpEvent, new RoutedPointEventHandler(PointUpEvent));
        }
        public override void Fire(Stroq[] strokes, object device)
        {
            // recognized the gesture, so stop inking and callback the gesture handler to
            // perform a function

            CommandSet.PauseData pdata = (CommandSet.PauseData)strokes[0].Property[CommandSet.PauseData.PauseDataGuid];

            bool top = false;

            if (_feedbacks.ContainsKey(device))
                if (WPFUtil.GetBounds(_feedbacks[device]).IntersectsWith(new Rct(pdata.Touch).Inflate(10, 10)))
                    top = true;
                else if (WPFUtil.GetBounds(_feedbackBottoms[device]).IntersectsWith(new Rct(pdata.Touch).Inflate(10, 10)))
                    top = false;

            _can.SetInkEnabled(device, false);
            if (InteractiveSpaceInsertionEvent != null)
                InteractiveSpaceInsertionEvent(strokes[0], device, strokes[0].GetBounds(), top);
        }
        public class InteractiveSpaceInsertion
        {
            InqScene _scene;
            Dictionary<object, Rectangle>       _selectionRects = new Dictionary<object, Rectangle>();
            Dictionary<Rectangle, double>       _angles = new Dictionary<Rectangle, double>();
            Dictionary<Rectangle, Vec>          _other = new Dictionary<Rectangle, Vec>();
            Dictionary<Rectangle, Pt>           _start = new Dictionary<Rectangle, Pt>();
            Dictionary<Rectangle, LnSeg>        _line = new Dictionary<Rectangle, LnSeg>();
            Dictionary<Rectangle, Ellipse>      _proj = new Dictionary<Rectangle, Ellipse>();
            Dictionary<Rectangle, double>       _dist = new Dictionary<Rectangle, double>();
            Dictionary<Rectangle, SelectionObj> _sel = new Dictionary<Rectangle, SelectionObj>();
            Dictionary<Rectangle, bool>         _top = new Dictionary<Rectangle, bool>();
            Dictionary<Rectangle, Pt>           _pinch = new Dictionary<Rectangle, Pt>();
            public class dragData
            {
                public SurfacePauseData PauseData;
                public Vec Offset;
            }

            public InteractiveSpaceInsertion(InqScene scene, InteractiveSpaceInsertionCommand command)
            {
                _scene = scene;
                command.InteractiveSpaceInsertionEvent += new InteractiveSpaceInsertionCommand.InteractiveSpaceInsertionHandler(interactiveSpaceInsertionEvent);
            }

            bool doesIntersect(Rectangle sel, LnSeg l, Rct stroq)
            {
                return WPFUtil.GetBounds(sel).IntersectsWith(stroq);
            }

            // We have been activated! Make some feedback for the user.
            void interactiveSpaceInsertionEvent(Stroq s, object device, Rct where, bool top)
            {

                SurfacePauseData pdata = s.Property[CommandSet.PauseData.PauseDataGuid] as SurfacePauseData;
                Rectangle selection = new Rectangle();
                //double ang = Math.Abs(Math.Atan(s.GetBounds().Width / s.GetBounds().Height));
                double ang = Math.Atan2(s.First().Y - s.Last().Y, s.First().X - s.Last().X) - Math.PI / 2;
                selection.Width = 0;
                // the length is the distance between the finger contact point and the pen contact point
                selection.Height = (pdata.ContactDev.GetPosition(_scene) - (Point)s.Last()).Length;

                // a black dashed rectangle is the way to be
                if (top)
                    selection.Stroke = Brushes.Black;
                else // bottom select
                    selection.Stroke = Brushes.Blue;
                selection.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });

                selection.Tag = new dragData() { Offset = s.Last() - where.TopRight, PauseData = pdata };
                _scene.SceneLayer.Children.Add(selection);
                selection.RenderTransform = new MatrixTransform(Mat.Translate(where.TopLeft) * Mat.Rotate(ang, where.TopLeft));
                // provide the angle so that the rectangle will know which angle to be at.
                _angles.Add(selection, ang);
                // provide the contact vector so that it knows what line to project onto.
                _other.Add(selection, s.Last() - where.TopLeft);
                // provide the contact point for testing.
                _start.Add(selection, s.Last());
                // get the line itself
                _line.Add(selection, new LnSeg(s.Last(), pdata.ContactDev.GetPosition(_scene)));

                _dist.Add(selection, 0);

                _top.Add(selection, top);

                Ellipse ell = new Ellipse();
                ell.Width = 10;
                ell.Height = 10;
                ell.Fill = Brushes.Black;
                ell.RenderTransform = new MatrixTransform(Mat.Translate(s.First()));
                _proj.Add(selection, ell);
                _scene.SceneLayer.Children.Add(ell);

                // make the contacts that comprise the gesture (ie, the pen and touch contacts) go to the
                // selection rectangle instead of to the APage
                pdata.ContactDev.Capture(selection);
                pdata.PenDev.Capture(selection); // turns off inking and routes the pen events to dragSelectionBox
                // whenever we get a contact event on the selection rectangle, 
                // update the selection rectangle
                selection.AddHandler(WPFPointDevice.PointDragEvent, new RoutedPointEventHandler(dragSelectionBox));
                selection.AddHandler(WPFPointDevice.PointUpEvent, new RoutedPointEventHandler(endSelectionBox));
            
                //coompute selection here

                // pretend our selection is very large

                List<Stroq> stroqs = new List<Stroq>();
                /*
                if (!top)
                {
                    selection.Width = 9999;
                    selection.UpdateLayout();
                    /// foreach (FrameworkElement fe in _scene.Elements) ...
                    foreach (Stroq sq in _scene.Stroqs)
                        if (doesIntersect(selection, _line[selection], sq.GetBounds()))
                            stroqs.Add(sq);
                }
                 */

                foreach (Stroq sq in _scene.Stroqs)
                {
                    double frac = _line[selection].LnClosestFraction(sq.GetBounds().Center);

                    LnSeg lnang = new LnSeg(sq.GetBounds().Center, _line[selection].A);

                    double angle = _line[selection].Direction.SignedAngle(lnang.Direction);

                    if (0 <= frac && 1 > frac && (angle > 0 && !top) || (angle < 0 && top))
                        stroqs.Add(sq);
                }

                SelectionObj sel = new SelectionObj(stroqs.ToArray());
                // raise the event SelectionStartTransformingEvent
                _scene.RaiseStartTransformingEvent(sel);
                selection.Opacity = 0.2;
                _sel.Add(selection, sel);
            }
            // Go back to the real world. You're not in Kansas anymore!
            void endSelectionBox(object sender /* this is a Rectangle */ , RoutedPointEventArgs e)
            {
                e.Handled = true;
                if (e.WPFPointDevice.Contact().IsFingerRecognized == true)
                {
                    Rectangle selection = sender as Rectangle;
                    dragData pdata = selection.Tag as dragData; 
                    pdata.PauseData.ContactDev.Capture(null);
                    pdata.PauseData.PenDev.Capture(null);
               
                    selection.RemoveHandler(WPFPointDevice.PointUpEvent, new RoutedPointEventHandler(endSelectionBox));
                
                    _scene.SceneLayer.Children.Remove(sender as Rectangle);

                    // raise stop transforming event so that math is drawn again
                    _scene.RaiseStopTransformingEvent(_sel[sender as Rectangle]);

                    // free resources
                    _sel.Remove(sender as Rectangle);
                    _angles.Remove(sender as Rectangle);
                    _other.Remove(sender as Rectangle);
                    _start.Remove(sender as Rectangle);
                    _scene.SceneLayer.Children.Remove(_proj[sender as Rectangle]);
                    _proj.Remove(sender as Rectangle);
                    _dist.Remove(sender as Rectangle);
                    _top.Remove(sender as Rectangle);
                    if (_pinch.ContainsKey(sender as Rectangle))
                        _pinch.Remove(sender as Rectangle);
                }
            }
            // update the selection rectangle display
            void dragSelectionBox(object sender, RoutedPointEventArgs e)
            {
                e.Handled = true;
                Rectangle selection = sender as Rectangle;
                double angle = _angles[selection];
                Vec startDir = _other[selection];
                Pt startPoint = _start[selection];
                LnSeg line = _line[selection];
                Ellipse ell = _proj[selection];
                // amount previously translated
                double dist_already = _dist[selection];
                SelectionObj sel = _sel[selection];
                bool top = _top[selection];

                dragData pdata = selection.Tag as dragData;

                Pt p1 = pdata.PauseData.ContactDev.GetPosition(_scene);
                try
                {
                    Pt p2 = pdata.PauseData.PenDev.GetPosition(_scene);
                    LnSeg new_line = new LnSeg(p2, p1);
                    // goal is to go from _line to new_line through transformations.
                    Pt projection = line.LnClosestPoint(p1);


                    LnSeg lnperp = new LnSeg(p1, projection);
                    LnSeg lnang = new LnSeg(p1, line.A);

                    // how much to rotate by
                    double angle_off = line.Direction.SignedAngle(new_line.Direction);

                    // how much to scale by
                    double scale = 1;//new_line.Length / line.Length;

                    // how much to translate by
                    Vec translate = new_line.Center - line.Center;


                    sel.XformBy(Mat.Scale(new Vec(scale, 1), line.Center));

                    sel.XformBy(Mat.Rotate(new Rad(angle_off), line.Center));

                    sel.XformBy(Mat.Translate(translate));


                    _line[selection] = new_line;
                    _angles[selection] = angle + angle_off;
                }
                catch /* Pen no longer down, go into dumb mode */
                {
                    // the projection of the pen point onto the line.
                    Pt projection = line.LnClosestPoint(p1);

                    ell.RenderTransform = new MatrixTransform(Mat.Translate(projection));

                    LnSeg lnperp = new LnSeg(p1, projection);
                    LnSeg lnang = new LnSeg(p1, line.A);

                    Pt offset = new Pt();
                    offset.X = 0;
                    offset.Y = 0;

                    double w = lnperp.Length;

                    double dot = lnang.Direction.SignedAngle(line.Direction);
                    if (dot > 0)
                    {
                        offset.Y -= w * Math.Sin(angle);
                        offset.X -= w * Math.Cos(angle);
                    }

                    selection.Width = (w < 0) ? 0 : w;

                    double h = line.LnClosestFraction(projection) * line.Length;
                    if (h < 0)
                    {
                        h *= -1;
                        offset.X -= h; ;// *Math.Cos(angle);
                    }

                    selection.Height = h > 0 ? h : 0;
                    selection.RenderTransform = new MatrixTransform(Mat.Translate(line.A + offset) * Mat.Rotate(angle, line.A + offset));

                    //selection.Width = (p1 - projection).Length;

                    double distance_necessary = (p1 - projection).Length - dist_already;

                    //if (line.LnClosestFraction(p1) < 0)
                    //if (top)
                    //    distance_necessary *= -1;

                    // move everything along the perpendicular vector however far is necessary
                    Vec mv = new Vec();
                    mv.X = distance_necessary * Math.Cos(angle);
                    mv.Y = distance_necessary * Math.Sin(angle);

                    if (dot > 0)
                    {
                        mv.Y *= -1;
                        mv.X *= -1;
                    }

                    _dist[selection] = (p1 - projection).Length;

                    SelectionObj selr = _sel[selection];
                    _scene.RaisePreTransformEvent(selr);
                    sel.MoveBy(mv);
                    _scene.RaisePostTransformEvent(selr);
                    _scene.SetSelection(Mouse.PrimaryDevice, selr); // which device should we associate the selection with?  Choose the Mouse as a hacky default
                    _scene.Feedback(Mouse.PrimaryDevice).Opacity = 0.1;
                }
            }
        }
    }
}
#endif