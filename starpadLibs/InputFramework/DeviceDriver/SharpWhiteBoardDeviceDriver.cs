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
    /// Class SharpWhhiteBoardDeviceDriver.cs
    /// Author: EZ
    /// Creation Date: 30. June 2011
    /// 
    /// Provides an API for generating Point events for Touch events.
    /// </summary>
    public class SharpWhiteBoardDeviceDriver : PointDeviceDriver
    {
        // Event handler to key off change from ink mode to touch mode
        public event IWBModeChangedNotificationHandler IWBModeChangedNotification;
        public delegate void IWBModeChangedNotificationHandler(bool inkSwitchOn);

        // Event handler to capture mouse (and stylus) down events
        public event IWBMousePreviewDownNotificationHandler IWBMousePreviewDownNotification;
        public delegate void IWBMousePreviewDownNotificationHandler(Object sender, MouseButtonEventArgs e);

        public static bool Windows8Slate = true;
        private bool _inkSwitchOn = false;
        public bool InkSwitchOn { get { return _inkSwitchOn; } set { _inkSwitchOn = value; } }
        /// <summary>
        /// On the sharp whiteboard we want to allow touch events to generate ink when a software mode 
        /// has been chosen. (similar to how the mouse generates ink at the desktop w/ a mode switch).
        /// </summary>
        public static bool TouchGeneratesInkMode { get; set; }
        private bool _disableAutoSwitch = false;

        static public List<object> enabledDevices = new List<object>();
        private FrameworkElement windowInstance = null;
        public List<long> activeDevices = new List<long>();
        private HostMinatoMessageReceiver sharpMessageReceiver = null;
        public delegate void InqActiveStateHandler(object sender, bool active, StylusDownEventArgs e);
        public event InqActiveStateHandler InqActiveStateChangedEvent;
        public SharpWhiteBoardDeviceDriver(FrameworkElement inputElement, WPFDeviceManager manager, FrameworkElement unused, bool windows8Slate = true, bool hackFlag = false)
        {
            windowInstance = inputElement;
            Windows8Slate = windows8Slate;
            WindowsSharpTouchHelper.Driver = this;
            WindowsSharpTouchHelper.Manager = manager;

            //sharpMessageReceiver = new HostMinatoMessageReceiver(); // add an instance of win32 message receiver to trap pen messages from sharps IWB's
            // attach to ModeChangedEvent
            //sharpMessageReceiver.TouchPanelModeChangedEvent += new TouchPanelModeChangedEventHandler(OnTouchPanelModeChanged);

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
                //if (_inkSwitchOn)
                {
                    enabledDevices.Remove(e.Device);
                    enabledDevices.Add(e.Device);
                }
            };
            windowInstance.PreviewMouseRightButtonDown += windowInstance_PreviewMouseRightButtonDown;

            if (Windows8Slate)
            {
                //enabledDevices.Add(new DeviceUID() { DeviceID = -1, DeviceDriverID = this.DeviceDriverID });
                enabledDevices.Add(new DeviceUID() { DeviceID = -2, DeviceDriverID = this.DeviceDriverID });

                inputElement.PreviewStylusDown          += window_PreviewStylusDown;
                inputElement.PreviewTouchDown           += window_PreviewTouchDown;
                inputElement.PreviewMouseLeftButtonDown += window_PreviewMouseLeftButtonDown;     
            }

            // stylus events are wacky .. handling one of them should block subsequent Touch or Mouse events.  
            // But handling a StylusDown doesn't prevent a MouseDown from occurring.  So that's why they're commented out...
            //windowInstance.PreviewStylusDown += (Object sender, StylusDownEventArgs e) => PreviewStylusDown(e);
            //windowInstance.PreviewStylusUp += (Object sender, StylusEventArgs e) => PreviewStylusUp(e);
            //windowInstance.PreviewStylusMove += (Object sender, StylusEventArgs e) => PreviewStylusMove(e);

            windowInstance.PreviewTouchDown += (Object sender, TouchEventArgs e) => PreviewTouchDown(e);
            windowInstance.PreviewTouchUp += (Object sender, TouchEventArgs e) => PreviewTouchUp(e);
            windowInstance.PreviewTouchMove += (Object sender, TouchEventArgs e) => PreviewTouchMove(e);

            windowInstance.PreviewMouseDown += (Object sender, MouseButtonEventArgs e) => { if (e.StylusDevice == null) PreviewMouseDown(e); };
            windowInstance.PreviewMouseUp += (Object sender, MouseButtonEventArgs e)   => { if (e.StylusDevice == null) PreviewMouseUp(e); };
            windowInstance.PreviewMouseMove += (Object sender, MouseEventArgs e)       => {
                if (e.StylusDevice == null)
                    PreviewMouseMove(e); // generate point events for true mouse events only if we're inking (otherwise, multitouch behaviors start kicking in).
                else
                    if (e.StylusDevice != null && e.StylusDevice.Captured == null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus)  // kill all true stylus events
                        e.Handled = true;
            };
            windowInstance.PreviewMouseLeftButtonDown += (Object sender, MouseButtonEventArgs e) => {
                if (e.StylusDevice != null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus)   // kill all true stylus events
                    e.Handled = true;
            };
            windowInstance.PreviewMouseLeftButtonUp += (Object sender, MouseButtonEventArgs e) => {
                if (e.StylusDevice != null && e.StylusDevice.Captured == null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus)  // kill all true stylus events
                    e.Handled = true;
            };


            if (hackFlag)
            {
                if (true)
                {
                    _inkSwitchOn = !_inkSwitchOn;
                    changeInkMode();   // added rjc method to encapsulate ink mode switch into single method handler  _inkSwitchOn state must be set before method call
                    enabledDevices.Clear();
                    enabledDevices.Add(new DeviceUID() { DeviceID = -2, DeviceDriverID = this.DeviceDriverID });
                    if (!_inkSwitchOn) // user manually forces touch
                        _disableAutoSwitch = true;
                    else
                        _disableAutoSwitch = false; // user forces back to pen then enable auto switch

                    if (InqActiveStateChangedEvent != null)
                        InqActiveStateChangedEvent(this, _inkSwitchOn, null);

                }
            }
        }

        // hacky way to turn ink on and off globally across all input devices
        void windowInstance_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IWBMousePreviewDownNotification != null)
                IWBMousePreviewDownNotification(sender, e);

            if (e.RightButton == MouseButtonState.Pressed && (TouchGeneratesInkMode || (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))))
            {
                _inkSwitchOn = !_inkSwitchOn;
                changeInkMode();   // added rjc method to encapsulate ink mode switch into single method handler  _inkSwitchOn state must be set before method call
                if (!_inkSwitchOn) // user manually forces touch
                    _disableAutoSwitch = true;
                else
                    _disableAutoSwitch = false; // user forces back to pen then enable auto switch

                if (InqActiveStateChangedEvent != null)
                    InqActiveStateChangedEvent(this, _inkSwitchOn, null);

                e.Handled = true;
            }
        }

        void window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (InqActiveStateChangedEvent != null)
                if (!_inkSwitchOn && (e.StylusDevice == null || e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus))
                    InqActiveStateChangedEvent(sender, false, null);
        }

        void window_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (TouchGeneratesInkMode)
            {
                if (InqActiveStateChangedEvent != null && !_inkSwitchOn)
                {
                    InqActiveStateChangedEvent(sender, false, null);
                    _inkSwitchOn = false;
                }
            }
            else
            {
                if (InqActiveStateChangedEvent != null)
                {
                    InqActiveStateChangedEvent(sender, false, null);
                }
                _inkSwitchOn = false;
            }
        }

        void window_PreviewStylusDown(object sender, StylusDownEventArgs e)
        {
            if (InqActiveStateChangedEvent != null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus)
            {
                _inkSwitchOn = true;
                changeInkMode();
                enabledDevices.Add(e.Device);
                InqActiveStateChangedEvent(sender, true, e);
            }
        }

        /// <summary>
        /// Handle touch panel mode switch event's from Sharp's active pen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnTouchPanelModeChanged(object sender, TouchPanelModeChangedArgs e)
        {
            if (!_disableAutoSwitch)
            {
                if (e.PenMode)
                    _inkSwitchOn = true;
                else
                    _inkSwitchOn = false;
                changeInkMode();
            }
        }

        /// <summary>
        /// method to encapsulate ink mode switch into single method handler  _inkSwitchOn state must be set before method call
        /// </summary>
        public void changeInkMode(bool enableMouse = true)
        {
            enabledDevices.Clear();
            if (_inkSwitchOn)
            {// we know the id of the mouse, so we can turn it on here.

                Console.WriteLine("ON SWTICH!!!!!!!!!!!");
                enabledDevices.Add(new DeviceUID() { DeviceID = -1, DeviceDriverID = this.DeviceDriverID });
                enabledDevices.Add(new DeviceUID() { DeviceID = -2, DeviceDriverID = this.DeviceDriverID });

                if (IWBModeChangedNotification != null)
                    IWBModeChangedNotification(_inkSwitchOn);
            }
            else
            {

                Console.WriteLine("OFF SWTICH!!!!!!!!!!!");
                if (IWBModeChangedNotification != null)
                    IWBModeChangedNotification(_inkSwitchOn);
            }

        }

        private void initPointEvent(ref PointEventArgs pointEventArgs, MouseEventArgs mouseEventArgs)
        {
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = mouseEventArgs.StylusDevice == null ? -1 : -2, DeviceDriverID = this.DeviceDriverID }; // the mouse device has no id
            pointEventArgs.DeviceType = enabledDevices.Contains(pointEventArgs.DeviceUID) && _inkSwitchOn ? DeviceType.Stylus : DeviceType.MultiTouch;
            pointEventArgs.Handled = false;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
            pointEventArgs.InBetweenPointsScreen[0] = new PressurePoint(windowInstance.PointToScreen(mouseEventArgs.GetPosition(windowInstance)));
            pointEventArgs.PointScreen = new PressurePoint(windowInstance.PointToScreen(mouseEventArgs.GetPosition(windowInstance)));
        }
        public bool RaiseMouseDown(Object sender, MouseEventArgs args)
        {
            //Console.WriteLine("RaiseMouseDown");
            PointEventArgs pointEventArgs = new PointEventArgs();
            initPointEvent(ref pointEventArgs, args);
            // hack: a stylus device that is in inking mode generated this event which should have been handled in inqcanvas on stylus down. but wpf generates it anyway.
            if (args.StylusDevice != null && pointEventArgs.DeviceType == DeviceType.Stylus)
            {
                return true;
            }
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
            pointEventArgs.DeviceType = (_inkSwitchOn && TouchGeneratesInkMode) || enabledDevices.Contains(pointEventArgs.DeviceUID) ? DeviceType.Stylus : DeviceType.MultiTouch;
            pointEventArgs.Handled = false;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
            pointEventArgs.InBetweenPointsScreen[0] = new PressurePoint(windowInstance.PointToScreen(touchEventArgs.GetTouchPoint(windowInstance).Position));
            pointEventArgs.PointScreen = new PressurePoint(windowInstance.PointToScreen(touchEventArgs.GetTouchPoint(windowInstance).Position));
        }
        public bool RaiseTouchDown(Object sender, TouchEventArgs args)
        {
            //Console.WriteLine("RaiseTouchDown");
            RaiseDeviceAdded(args);
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
            RaiseDeviceRemoved(args);
            return pointEventArgs.Handled;
        }

        public void RaiseDeviceAdded(TouchEventArgs touchEventArgs)
        {
            OnAddDeviceEvent(new DeviceUID()
            {
                DeviceID = touchEventArgs.TouchDevice.Id,
                DeviceDriverID = this.DeviceDriverID
            });
        }
        public void RaiseDeviceRemoved(TouchEventArgs touchEventArgs)
        {
            OnRemoveDeviceEvent(new DeviceUID()
            {
                DeviceID = touchEventArgs.TouchDevice.Id,
                DeviceDriverID = this.DeviceDriverID
            });
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
            pointEventArgs.DeviceType = enabledDevices.Contains(pointEventArgs.DeviceUID) ? DeviceType.Stylus : DeviceType.MultiTouch;
            pointEventArgs.Handled = false;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
            pointEventArgs.InBetweenPointsScreen[0] = new PressurePoint(windowInstance.PointToScreen(stylusEventArgs.GetPosition(windowInstance)));
            pointEventArgs.PointScreen = new PressurePoint(windowInstance.PointToScreen(stylusEventArgs.GetPosition(windowInstance)));
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
            //return;
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
            if (RaiseMouseDown(windowInstance, e))
            {
                e.Handled = true;
            }
        }
        void PreviewMouseUp(MouseButtonEventArgs e)
        {
            if (RaiseMouseUp(windowInstance, e))
            {
                e.Handled = true;
            }
        }
        void PreviewMouseMove(MouseEventArgs e)
        {
            if (RaiseMouseDrag(windowInstance, e))
            {
                e.Handled = true;
            }
        }

        public override void Dispose()
        {
        }
    }


    static public class WindowsSharpTouchHelper
    {
        static public SharpWhiteBoardDeviceDriver Driver = null;
        static public WPFDeviceManager Manager = null;
        static public bool IsDeviceActive(long DeviceID)
        {
            return Driver.activeDevices.Contains(DeviceID);
        }
    }
}
