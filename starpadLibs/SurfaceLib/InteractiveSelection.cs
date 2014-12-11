#if SURFACE
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
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
using starPadSDK.CharRecognizer;
using starPadSDK.AppLib;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using InputFramework.WPFDevices;

namespace starPadSDK.SurfaceLib
{
    public class InteractiveSelectCommand : SymbolCommand
    {
        protected InqScene             _can;
        protected InteractiveSelection _iselect = null;
        protected static Dictionary<object, Rectangle> _feedback = new Dictionary<object, Rectangle>();

        void removeFeedback(object device) {
            if (_feedback.ContainsKey(device)) {
                _can.SceneLayer.Children.Remove(_feedback[device]);
                _feedback.Remove(device);
            }
        }
        void PointUpEvent(object sender, RoutedPointEventArgs e) { removeFeedback(e.PointEventArgs.DeviceUID); }
        void PointDragEvent(object sender, RoutedPointEventArgs e)
        {
            // get rid of any previous selection rectangle feedback first
            if (_feedback.ContainsKey(e.PointEventArgs.DeviceUID))
            {
                _feedback[e.PointEventArgs.DeviceUID].Visibility = Visibility.Collapsed;
                _feedback.Remove(e.PointEventArgs.DeviceUID);
            }
            // if the Point devices has stylus points, then it must be a pen, so we check to 
            // see if the crop mark gesture has been drawn.  If it has, we start displaying the
            // dashed line feedback for the selection rectangle.
            if (_can.CollectedPoints(e.PointEventArgs.DeviceUID) != null)
            {
                Stroq s = new Stroq(_can.CollectedPoints(e.PointEventArgs.DeviceUID));
                var fd = new starPadSDK.CharRecognizer.FeaturePointDetector();
                string allo = "";
                var features = fd.FeaturePoints(s.OldStroke());
                if (fd.match_7(features, ref allo) ||
                    (s.Cusps().Length > 2 && s.Cusps().Straightness(0, 1) < 0.2 && s.Cusps().Straightness(1, s.Cusps().Length - 1) < 0.3))
                {
                    Rectangle feedback = new Rectangle();
                    feedback.IsHitTestVisible = false;
                    _feedback.Add(e.PointEventArgs.DeviceUID, feedback);
                    _can.SceneLayer.Children.Add(feedback);
                    feedback.Visibility = Visibility.Visible;
                    feedback.Width = s.GetBounds().Width;
                    feedback.Height = s.GetBounds().Height;
                    feedback.Stroke = Brushes.Black;
                    feedback.Opacity = 0.2;
                    feedback.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 }); ;
                    feedback.RenderTransform = new MatrixTransform(Mat.Translate(s.GetBounds().TopLeft));
                }
            }
        }

        public delegate void InteractiveSelectHandler(Stroq s, object device, Rct where);
        public event InteractiveSelectHandler InteractiveSelectEvent;

        public InteractiveSelectCommand(InqScene can) : base("7")
        {
            _can = can;
            _can.AddHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(PointDragEvent));
            _can.AddHandler(WPFPointDevice.PreviewPointUpEvent, new RoutedPointEventHandler(PointUpEvent));

            _iselect = new InteractiveSelection(can, this);
        }
        public override bool Test(Stroq s, object device)
        {
            CommandSet.PauseData pdata = (CommandSet.PauseData)s.Property[CommandSet.PauseData.PauseDataGuid];
            if (_feedback.ContainsKey(device))
                if (WPFUtil.GetBounds(_feedback[device]).IntersectsWith(new Rct(pdata.Touch).Inflate(10, 10)))
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
            _can.SetInkEnabled(device, false);
            removeFeedback(device);
            if (InteractiveSelectEvent != null)
                InteractiveSelectEvent(strokes[0], device, strokes[0].GetBounds());
        }
        public class InteractiveSelection
        {
            protected InqScene _scene;
            protected Dictionary<object, Rectangle> _selectionRects = new Dictionary<object, Rectangle>();
            public InteractiveSelectCommand Command { get; set; }
            public InteractiveSelection(InqScene scene, InteractiveSelectCommand command)
            {
                _scene = scene;
                command.InteractiveSelectEvent += new InteractiveSelectCommand.InteractiveSelectHandler(interactiveSelectEvent);
            }
            public class dragData
            {
                public SurfacePauseData PauseData;
                public Vec Offset;
            }
            // when the '7' crop mark gesture is recognized, it calls this handler to display interactive feedback
            protected virtual void interactiveSelectEvent(Stroq s, object device, Rct where)
            {
                SurfacePauseData pdata = s.Property[CommandSet.PauseData.PauseDataGuid] as SurfacePauseData;
                Rectangle selection = new Rectangle();
                selection.Width = where.Width;
                selection.Height = where.Height;
                selection.Stroke = Brushes.Black;
                selection.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });
                selection.Tag = new dragData() { Offset = s.Last() - where.TopRight, PauseData = pdata };
                _scene.SceneLayer.Children.Add(selection);
                selection.RenderTransform = new MatrixTransform(Mat.Translate(where.TopLeft));
                // make the contacts that comprise the gesture (ie, the pen and touch contacts) go to the
                // selection rectangle instead of to the APage
                pdata.ContactDev.Capture(selection);
                pdata.PenDev.Capture(selection);// turns off inking and routes the pen events to dragSelectionBox
                // whenever we get a contact event on the selection rectangle, 
                // update the selection rectangle
                selection.AddHandler(WPFPointDevice.PointDragEvent, new RoutedPointEventHandler(dragSelectionBox));
                selection.AddHandler(WPFPointDevice.PointUpEvent, new RoutedPointEventHandler(endSelectionBox));
            }
            // actually make a selection (currently this only looks at make selections of Stroqs, not other FrameworkElements)
            protected virtual void endSelectionBox(object sender /* this is a Rectangle */ , RoutedPointEventArgs e)
            {
                Rectangle selection = sender as Rectangle;
                dragData pdata = selection.Tag as dragData;
                _scene.AreaSelect(WPFUtil.GetBounds(sender as Rectangle), true, e.WPFPointDevice);// Mouse.PrimaryDevice);// which device should we associate the selection with?  Choose the Mouse as a hacky default
                _scene.SceneLayer.Children.Remove(sender as Rectangle);
                pdata.PauseData.ContactDev.Capture(null);
                pdata.PauseData.PenDev.Capture(null);
                selection.RemoveHandler(WPFPointDevice.PointUpEvent, new RoutedPointEventHandler(endSelectionBox));
                e.Handled = true;
            }
            // update the selection rectangle display
            protected virtual void dragSelectionBox(object sender, RoutedPointEventArgs e)
            {
                Rectangle selection = sender as Rectangle;
                dragData pdata = selection.Tag as dragData;
                Pt p1 = pdata.PauseData.ContactDev.GetPosition(_scene);
                Pt p2 = pdata.PauseData.PenDev.GetPosition(_scene);
                selection.Width = Math.Abs(p1.X - p2.X);
                selection.Height = Math.Abs(p1.Y - p2.Y) + pdata.Offset.Y;
                selection.RenderTransform = new MatrixTransform(Mat.Translate(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y) - pdata.Offset.Y));
                e.Handled = true;
            }
        }
    }
}
#endif