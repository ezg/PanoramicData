using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using InputFramework;
using InputFramework.WPFDevices;
using InputFramework.DeviceDriver;
using System.Windows.Input;

namespace InputFramework.DeviceDriver
{

    /// <summary>
    /// Class Windows8SlateDeviceDriver.cs
    /// Author: EZ
    /// Creation Date: 30. June 2011
    /// 
    /// Provides an API for generating Point events for Touch events.
    /// </summary>
    public class Windows8SlateDeviceDriver : PointDeviceDriver
    {
        static public List<object> enabledDevices = new List<object>();
        private FrameworkElement windowInstance = null;
        public List<long> activeDevices = new List<long>();
        private HostMinatoMessageReceiver sharpMessageReceiver = null;

        private bool _inkSwitchOn = false;

        public Windows8SlateDeviceDriver(FrameworkElement window, WPFDeviceManager manager, FrameworkElement inqCanvas)
        {
            windowInstance = window;
           
            // attach to ModeChangedEvent
            //sharpMessageReceiver.TouchPanelModeChangedEvent += new TouchPanelModeChangedEventHandler(OnTouchPanelModeChanged);
            /*
            manager.WPFPointDeviceAdded += new WPFDeviceManager.WPFPointDeviceAddedHandler((Object sender, WPFPointDevice newDev) => activeDevices.Add(newDev.DeviceUID.DeviceID));
            manager.WPFPointDeviceRemoved += new WPFDeviceManager.WPFPointDeviceRemovedHandler((Object sender, WPFPointDevice newDev) =>
            {
                if (activeDevices.Contains(newDev.DeviceUID.DeviceID))
                    activeDevices.Remove(newDev.DeviceUID.DeviceID);
            });

            // hack!   we really s hould have enabled this device when we got UI action (eg right click), but 
            // we didn't know the id of the debvice when that  happened, so we defer enabling it until here.
            windowInstance.PreviewStylusDown += (Object sender, StylusDownEventArgs e) =>
            {
                enabledDevices.Remove(e.Device);
                enabledDevices.Add(e.Device);
            };
            */
            windowInstance.PreviewMouseRightButtonDown += windowInstance_PreviewMouseRightButtonDown;

            // stylus events are wacky .. handling one of them should block subsequent Touch or Mouse events.  
            // But handling a StylusDown doesn't prevent a MouseDown from occurring.  So that's why they're commented out...
            windowInstance.PreviewStylusDown += (Object sender, StylusDownEventArgs e) => PreviewStylusDown(e);
            windowInstance.PreviewStylusUp += (Object sender, StylusEventArgs e) => PreviewStylusUp(e);
            windowInstance.PreviewStylusMove += (Object sender, StylusEventArgs e) => PreviewStylusMove(e);

            windowInstance.PreviewTouchDown += (Object sender, TouchEventArgs e) => PreviewTouchDown(e);
            windowInstance.PreviewTouchUp += (Object sender, TouchEventArgs e) => PreviewTouchUp(e);
            windowInstance.PreviewTouchMove += (Object sender, TouchEventArgs e) => PreviewTouchMove(e);

            windowInstance.PreviewMouseDown += (Object sender, MouseButtonEventArgs e) => PreviewMouseDown(e);
            windowInstance.PreviewMouseUp += (Object sender, MouseButtonEventArgs e) => PreviewMouseUp(e);
            windowInstance.PreviewMouseMove += (Object sender, MouseEventArgs e) => PreviewMouseMove(e);
        }

        // hacky way to turn ink on and off globally across all input devices
        void windowInstance_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))))
            {
                _inkSwitchOn = !_inkSwitchOn;
                e.Handled = true;
            }
        }

        private void initPointEvent(ref PointEventArgs pointEventArgs, MouseEventArgs mouseEventArgs)
        {
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = -1, DeviceDriverID = this.DeviceDriverID };
            pointEventArgs.DeviceType = mouseEventArgs.StylusDevice == null ? (_inkSwitchOn ? DeviceType.Stylus : DeviceType.MultiTouch) : DeviceType.Stylus;
            pointEventArgs.Handled = false;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
            pointEventArgs.InBetweenPointsScreen[0] = new PressurePoint(windowInstance.PointToScreen(mouseEventArgs.GetPosition(windowInstance)));
            pointEventArgs.PointScreen = new PressurePoint(windowInstance.PointToScreen(mouseEventArgs.GetPosition(windowInstance)));
        }
        public bool RaiseMouseDown(Object sender, MouseEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = PointEventType.LeftDown;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }
        public bool RaiseMouseUp(Object sender, MouseButtonEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = PointEventType.LeftUp;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }
        public bool RaiseMouseDrag(Object sender, MouseEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = args.LeftButton != MouseButtonState.Released || args.RightButton != MouseButtonState.Released ? PointEventType.Drag : PointEventType.Move;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }

        private void initPointEvent(ref PointEventArgs pointEventArgs, TouchEventArgs touchEventArgs)
        {
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = touchEventArgs.TouchDevice.Id, DeviceDriverID = this.DeviceDriverID };
            pointEventArgs.DeviceType = DeviceType.MultiTouch;
            pointEventArgs.Handled = false;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
            pointEventArgs.InBetweenPointsScreen[0] = new PressurePoint(windowInstance.PointToScreen(touchEventArgs.GetTouchPoint(windowInstance).Position));
            pointEventArgs.PointScreen = new PressurePoint(windowInstance.PointToScreen(touchEventArgs.GetTouchPoint(windowInstance).Position));
        }
        public bool RaiseTouchDown(Object sender, TouchEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = PointEventType.LeftDown;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }
        public bool RaiseTouchUp(Object sender, TouchEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = PointEventType.LeftUp;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }
        public bool RaiseTouchMove(Object sender, TouchEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = PointEventType.Drag;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }

        private void initPointEvent(ref PointEventArgs pointEventArgs, StylusEventArgs stylusEventArgs)
        {
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = stylusEventArgs.StylusDevice.Id, DeviceDriverID = this.DeviceDriverID };
            pointEventArgs.DeviceType = stylusEventArgs.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch ? DeviceType.MultiTouch : DeviceType.Stylus;
            pointEventArgs.Handled = false;
            pointEventArgs.StylusEventArgs = stylusEventArgs;

            StylusPointCollection intermediatePointsWindow = new StylusPointCollection();
            StylusPointDescription spd = null;
            intermediatePointsWindow = stylusEventArgs.GetStylusPoints(windowInstance, spd = intermediatePointsWindow.Description);
            int numPoints = intermediatePointsWindow.Count;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                pointEventArgs.InBetweenPointsScreen[i] = new PressurePoint(windowInstance.PointToScreen(intermediatePointsWindow[i].ToPoint()), intermediatePointsWindow[i].PressureFactor);
            }
            pointEventArgs.PointScreen = pointEventArgs.InBetweenPointsScreen[numPoints - 1];
        }
        public bool RaiseStylusDown(Object sender, StylusDownEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = PointEventType.LeftDown;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }
        public bool RaiseStylusUp(Object sender, StylusEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = PointEventType.LeftUp;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }
        public bool RaiseStylusMove(Object sender, StylusEventArgs args)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            pointEventArgs.PointEventType = args.InAir ? PointEventType.Move : PointEventType.Drag;
            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }

        void PreviewTouchDown(TouchEventArgs e)
        {
            if (RaiseTouchDown(windowInstance, e))
            {
                e.Handled = true;
            }
        }
        void PreviewTouchUp(TouchEventArgs e)
        {
            if (RaiseTouchUp(windowInstance, e))
            {
                e.Handled = true;
            }
        }
        void PreviewTouchMove(TouchEventArgs e)
        {
            if (RaiseTouchMove(windowInstance, e))
            {
                e.Handled = true;
            }
        }

        void PreviewStylusDown(StylusDownEventArgs e)
        {
            if (RaiseStylusDown(windowInstance, e))
            {
                e.Handled = true;
            }
        }
        void PreviewStylusUp(StylusEventArgs e)
        {
            if (RaiseStylusUp(windowInstance, e))
            {
                e.Handled = true;
            }
        }
        void PreviewStylusMove(StylusEventArgs e)
        {
            if (RaiseStylusMove(windowInstance, e))
            {
                e.Handled = true;
            }
        }

        void PreviewMouseDown(MouseButtonEventArgs e)
        {
            if (e.StylusDevice == null) 
                if (RaiseMouseDown(windowInstance, e))
                {
                    e.Handled = true;
                }
        }
        void PreviewMouseUp(MouseButtonEventArgs e)
        {
            if (e.StylusDevice == null) 
                if (RaiseMouseUp(windowInstance, e))
                {
                    e.Handled = true;
                }
        }
        void PreviewMouseMove(MouseEventArgs e)
        {
            if (e.StylusDevice == null) 
                if (RaiseMouseDrag(windowInstance, e))
                {
                    e.Handled = true;
                }
        }

        public override void Dispose()
        {
        }
    }
}
