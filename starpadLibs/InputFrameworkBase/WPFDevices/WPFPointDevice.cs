using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InputFramework.DeviceDriver;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls;

namespace InputFramework.WPFDevices
{
    /// <summary>
    /// Represents a single InputDevice that provides 2D point-input.
    /// Resembles System.Windows.Input.MouseDevice.
    /// Currently supports:
    /// - Event capturing (+ OutsideUp and OutsideDown events)
    /// - Multi-Window handling
    /// - Preview & Bubble Up, Down, Move and Drag events
    /// - Leave/Enter event
    /// 
    /// TODO:
    /// - CaptureModes Element and None
    /// - Got/LostCapture events
    /// </summary>
    public class WPFPointDevice : WPFDevice
    {
        #region Routed Event Declarations

        public static readonly RoutedEvent PreviewPointDownEvent = EventManager.RegisterRoutedEvent("PreviewPointDown", RoutingStrategy.Tunnel, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));
        public static readonly RoutedEvent PreviewPointUpEvent = EventManager.RegisterRoutedEvent("PreviewPointUp", RoutingStrategy.Tunnel, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));
        public static readonly RoutedEvent PreviewPointMoveEvent = EventManager.RegisterRoutedEvent("PreviewPointMove", RoutingStrategy.Tunnel, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));
        public static readonly RoutedEvent PreviewPointDragEvent = EventManager.RegisterRoutedEvent("PreviewPointDrag", RoutingStrategy.Tunnel, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));

        public static readonly RoutedEvent PointDownEvent = EventManager.RegisterRoutedEvent("PointDown", RoutingStrategy.Bubble, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));
        public static readonly RoutedEvent PointUpEvent = EventManager.RegisterRoutedEvent("PointUp", RoutingStrategy.Bubble, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));
        public static readonly RoutedEvent PointMoveEvent = EventManager.RegisterRoutedEvent("PointMove", RoutingStrategy.Bubble, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));
        public static readonly RoutedEvent PointDragEvent = EventManager.RegisterRoutedEvent("PointDrag", RoutingStrategy.Bubble, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));

        public static readonly RoutedEvent PreviewPointDownOutsideCapturedElementEvent = EventManager.RegisterRoutedEvent("PreviewPointDownOutsideCapturedElement", RoutingStrategy.Tunnel, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));
        public static readonly RoutedEvent PreviewPointUpOutsideCapturedElementEvent = EventManager.RegisterRoutedEvent("PreviewPointUpOutsideCapturedElement", RoutingStrategy.Tunnel, typeof(RoutedPointEventHandler), typeof(WPFPointDevice));

        public static readonly RoutedEvent PointDeviceEnterEvent = EventManager.RegisterRoutedEvent("PointDeviceEnter", RoutingStrategy.Direct, typeof(RoutedPointDeviceHoverEventHandler), typeof(WPFPointDevice));
        public static readonly RoutedEvent PointDeviceLeaveEvent = EventManager.RegisterRoutedEvent("PointDeviceLeave", RoutingStrategy.Direct, typeof(RoutedPointDeviceHoverEventHandler), typeof(WPFPointDevice));


        #endregion

        #region Private Fields

        private UIElement mVisualRoot = null;         // the root element to work on (by default null)
        private FrameworkElement mHitInputElement;    // helper field used for hit-testing
        private FrameworkElement mCapturedElement = null; // points to the captured element
        private LinkedList<FrameworkElement> mHoverElements; // a list of visual elements this pointer is currently over - this is used for generating "enter" / "leave" events
        private LinkedList<FrameworkElement> mTempHitInputElementTree;
        private CaptureMode mCaptureMode;          // actual capture mode

        internal static Dictionary<WPFPointDevice, UIElement> sCaptures = new Dictionary<WPFPointDevice, UIElement>(); // stores all captures of all PointDevices

        #endregion

        #region P/Invokes
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point lpPoint);
        #endregion

        #region Statics

        /// <summary>
        /// Use the systems windows z-order or ignore it.
        /// </summary>
        public static bool IgnoreWindowZOrder = false;

        #endregion

        /// <summary>
        /// ONLY USED INTERNALLY!!
        /// </summary>
        /// <param name="deviceUID"></param>
        /// <param name="visualRoot">bind event handling to certain visual element (e.g. Window or UIElement)</param>
        internal WPFPointDevice(DeviceUID deviceUID, UIElement visualRoot) : base(deviceUID)
        {
            mVisualRoot = visualRoot;
            mHoverElements = new LinkedList<FrameworkElement>();
            mTempHitInputElementTree = new LinkedList<FrameworkElement>();
        }

        /// <summary>
        /// ONLY USED INTERNALLY!!
        /// </summary>
        /// <param name="deviceUID"></param>
        internal WPFPointDevice(DeviceUID deviceUID) : base(deviceUID) {
            mHoverElements = new LinkedList<FrameworkElement>();
            mTempHitInputElementTree = new LinkedList<FrameworkElement>();
        }
        
        /// <summary>
        /// Gets the IInputElement that is captured by the device
        /// </summary>
        public FrameworkElement Captured
        {
            get
            {
                return mCapturedElement;
            }
            private set
            {
                if (mCapturedElement != value)
                {
                    sCaptures.Remove(this);
                    mCapturedElement = value;
                    if(value != null) sCaptures.Add(this, mCapturedElement);
                }
            }
        }

        // bcz: added GetPosition and ScreenPosition to maintain an abstract device state
        private PressurePoint screenPosition { set; get; }
        public Point GetPosition(FrameworkElement e) {  return e.PointFromScreen(screenPosition.Point); }

        /// <summary>
        /// Captures mouse input to the specified element using the specified CaptureMode.        
        /// </summary>
        /// <param name="element">The element to capture the device</param>
        /// <param name="captureMode">The capture policy to use</param>
        /// <returns>true if the element was able to capture the mouse; otherwise, false</returns>
        /// <remarks>
        /// When an element captures the mouse, it receives mouse input whether or not the cursor is within its borders.
        /// To release capture, call Capture passing null reference (Nothing in Visual Basic) as the element to capture.         
        /// </remarks>
        public bool Capture(FrameworkElement element, CaptureMode captureMode)
        {
            Captured = element;
            mCaptureMode = captureMode;
            return true;
        }

        /// <summary>
        /// Captures device input to the specified element.
        /// </summary>
        /// <param name="element">The element to capture the device</param>
        /// <returns>true if the element was able to capture the mouse; otherwise, false</returns>
        /// <remarks>
        /// When an element captures the mouse, it receives mouse input whether or not the cursor is within its borders.
        /// To release capture, call Capture passing null reference (Nothing in Visual Basic) as the element to capture. 
        /// The default CaptureMode is Subtree. 
        /// </remarks>
        public bool Capture(FrameworkElement element) { return Capture(element, CaptureMode.Element); }

        /// <summary>
        /// Removes device capture. After this call, typically no UIElement holds device capture.
        /// </summary>
        public void ReleaseCapture()
        {
            Capture(null);
        }

        /// <summary>
        /// Returns the visual element that is used as root for hit testing.
        /// By default the window below the given point is used except:
        /// - If IgnoreWindowZOrder is set only this application's windows are used AND not the z-order but the order of creation is used for selection (older ones first)
        /// - If Capturing is activ the captured element will receive the events
        /// - If a visual root was set in the constructor this will be the root element (capturing overrides this rule!)
        /// </summary>
        /// <param name="pointOnScreen"></param>
        /// <param name="ignoreCapturedElement">Ignores the currently captured element - this is important for the enter / leave events</param>
        /// <returns>Visual (Window or captured UIElement) that is below the point or null if an other window than the applications window was hit</returns>
        private UIElement GetHitTestRoot(Point pointOnScreen, bool ignoreCapturedElement)
        {            
            if (mCapturedElement != null && !ignoreCapturedElement && mCapturedElement.Visibility == Visibility.Visible) // capture is active
            {
                return mCapturedElement;
            }
            else if (mVisualRoot != null) // use the visual root field
            {
                return mVisualRoot;
            }
            else if (!IgnoreWindowZOrder) // normal mode
            {
                IntPtr hwnd = WindowFromPoint(new System.Drawing.Point((int)pointOnScreen.X, (int)pointOnScreen.Y));
                foreach (Window win in Application.Current.Windows)
                {
                    if ((new WindowInteropHelper(win)).Handle == hwnd)
                    {
                        return win;
                    }
                }
                return Application.Current.Windows[0]; //bcz: NEEDED to MAKE SURFACE SIMULATOR BEHAVE NICELY!!!
            }
            else // use only app windows
            {
                foreach (Window win in Application.Current.Windows)
                {
                    IInputElement res = win.InputHitTest(win.PointFromScreen(pointOnScreen));
                    if (res != null)
                    {
                        return win;
                    }
                }
            }

            // no valid source found
            return null;
        }

        private UIElement GetHitTestRoot(Point pointOnScreen)
        {
            return GetHitTestRoot(pointOnScreen, false);
        }

        /// <summary>
        /// ONLY USED INTERNALLY!!
        /// Process an incoming event
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="pointEventArgs"></param>
        internal void ProcessDeviceManagerPointEvent(Object Sender, PointEventArgs pointEventArgs)
        {
            

            // Raising events on UIElements has to be done in the UI Thread otherwise WPF throws an exception
            if (Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action<object, PointEventArgs>(ProcessDeviceManagerPointEvent_InUIThread), Sender, pointEventArgs);
            }
            else
            {
                ProcessDeviceManagerPointEvent_InUIThread(Sender, pointEventArgs);
            }
        }

        private void ProcessDeviceManagerPointEvent_InUIThread(object sender, PointEventArgs pointEventArgs)
        {
            // depending on the capturing the visualRoot is the hitTestRoot or the captured element
            UIElement hitTestRoot = GetHitTestRoot(pointEventArgs.PointScreen.Point);
            // if there is no root to be found .. ignore event
            if (hitTestRoot == null) return;

            // transform the input point to the hitTest element coordinate system
            Point pointVisualRoot = new Point() ;
            try {
                pointVisualRoot = hitTestRoot.PointFromScreen(pointEventArgs.PointScreen.Point);
            } catch (Exception) {
                Debug.WriteLine("Element not in Visual Tree.");
            }

            // do the hittest
            mHitInputElement = null;
            if (mCapturedElement == null)
                VisualTreeHelper.HitTest(hitTestRoot, new HitTestFilterCallback(HitTestFilter), new HitTestResultCallback(HitTestResult), new PointHitTestParameters(pointVisualRoot));
            else mHitInputElement = mCapturedElement;

            // if capturing is active: check if we need to send a PreviewPointDownOutsideCapturedElementEvent or PreviewPointUpOutsideCapturedElementEvent
            if (mCapturedElement != null && mHitInputElement == null)
            {
                if (pointEventArgs.PointEventType != PointEventType.Move &&
                    pointEventArgs.PointEventType != PointEventType.Drag &&
                    pointEventArgs.PointEventType != PointEventType.Unknown) // do we have a up or down
                {
                    // fire the PreviewPointOutsideCapturedElementEvents
                    RoutedEvent eventType;
                    if (pointEventArgs.PointEventType == PointEventType.LeftDown ||
                        pointEventArgs.PointEventType == PointEventType.MiddleDown ||
                        pointEventArgs.PointEventType == PointEventType.RightDown)
                    {
                        eventType = PreviewPointDownOutsideCapturedElementEvent;
                    }
                    else
                    {
                        eventType = PreviewPointUpOutsideCapturedElementEvent;
                    }
                    RoutedPointEventArgs outsideEventArgs = new RoutedPointEventArgs(eventType, pointEventArgs, this);
                    mCapturedElement.RaiseEvent(outsideEventArgs);

                    // if the capturing has changed we have to redo the hittest
                    if (mCapturedElement != hitTestRoot)
                    {
                        hitTestRoot = GetHitTestRoot(pointEventArgs.PointScreen.Point);
                        if (hitTestRoot == null) return;
                        pointVisualRoot = hitTestRoot.PointFromScreen(pointEventArgs.PointScreen.Point);

                        mHitInputElement = null;
                        VisualTreeHelper.HitTest(hitTestRoot, new HitTestFilterCallback(HitTestFilter), new HitTestResultCallback(HitTestResult), new PointHitTestParameters(pointVisualRoot));
                    }
                    else // the captured element hasn't changed so we sent the event to the captured element
                    {
                        mHitInputElement = mCapturedElement;
                    }
                }
                // if we have a move, drag or unknown event direct it to the captured element
                else
                {
                    mHitInputElement = mCapturedElement;
                }
            }

            // Event routing and sending
            if (mHitInputElement != null)
            {
                RoutedEvent tunnelingEventType;
                RoutedEvent bubbelingEventType;

                switch (pointEventArgs.PointEventType)
                {
                    case PointEventType.LeftDown:
                    case PointEventType.MiddleDown:
                    case PointEventType.RightDown:
                        tunnelingEventType = WPFPointDevice.PreviewPointDownEvent;
                        bubbelingEventType = WPFPointDevice.PointDownEvent;
                        break;
                    case PointEventType.LeftUp:
                    case PointEventType.MiddleUp:
                    case PointEventType.RightUp:
                        tunnelingEventType = WPFPointDevice.PreviewPointUpEvent;
                        bubbelingEventType = WPFPointDevice.PointUpEvent;
                        break;
                    case PointEventType.Move:
                        tunnelingEventType = WPFPointDevice.PreviewPointMoveEvent;
                        bubbelingEventType = WPFPointDevice.PointMoveEvent;
                        break;
                    case PointEventType.Drag:
                        tunnelingEventType = WPFPointDevice.PreviewPointDragEvent;
                        bubbelingEventType = WPFPointDevice.PointDragEvent;
                        break;
                    default:
                        // todo: maybe create a special RoutedEvent for unknown events
                        Trace.WriteLine("WPFPointDevice: Unknown PointEvent");
                        return;
                }

                screenPosition = pointEventArgs.PointScreen;
                RoutedPointEventArgs tunnelingEventArgs = new RoutedPointEventArgs(tunnelingEventType, pointEventArgs, this);
                mHitInputElement.RaiseEvent(tunnelingEventArgs);
                pointEventArgs.Handled = tunnelingEventArgs.Handled;

                if (!tunnelingEventArgs.Handled)
                {
                    if (mCapturedElement == null)
                        VisualTreeHelper.HitTest(hitTestRoot, new HitTestFilterCallback(HitTestFilter), new HitTestResultCallback(HitTestResult), new PointHitTestParameters(pointVisualRoot));
                    else mHitInputElement = mCapturedElement;
                    // VisualTreeHelper.HitTest(hitTestRoot, new HitTestFilterCallback(HitTestFilter), new HitTestResultCallback(HitTestResult), new PointHitTestParameters(pointVisualRoot));
                    if (mHitInputElement == null)
                        mHitInputElement = mCapturedElement;
                    if (mHitInputElement != null)
                    {
                        RoutedPointEventArgs bubblingEventArgs = new RoutedPointEventArgs(bubbelingEventType, pointEventArgs, this);
                        mHitInputElement.RaiseEvent(bubblingEventArgs);
                        pointEventArgs.Handled = bubblingEventArgs.Handled;
                    }
                }
            }

            // check if we need to send an enter / leave event
            if (mCapturedElement ==  null && ( pointEventArgs.PointEventType == PointEventType.Move || pointEventArgs.PointEventType == PointEventType.Drag))
            {
                mHitInputElement = null; 

                // perform a hittest ignoring the currently captured element
                UIElement hitTestRootHover = GetHitTestRoot(pointEventArgs.PointScreen.Point, true);
                if (hitTestRootHover != null) {
                    pointVisualRoot = hitTestRootHover.PointFromScreen(pointEventArgs.PointScreen.Point);
                    VisualTreeHelper.HitTest(hitTestRootHover, new HitTestFilterCallback(HitTestFilter), new HitTestResultCallback(HitTestResult), new PointHitTestParameters(pointVisualRoot));
                }

                if (mHitInputElement != null)
                {
                    if (mHoverElements.Count > 0)
                    {
                        // check if the element at the end of our "hovering" list matches the currently hit element
                        // if this is the case nothing has changed and we can skip all further tests
                        if (mHoverElements.Last() != mHitInputElement)
                        {
                            // build a linked list which contains the whole visual tree from the parent down to the hit element
                            mTempHitInputElementTree.Clear();
                            FrameworkElement newHitInputElement = mHitInputElement;
                            while (newHitInputElement != null)
                            {
                                mTempHitInputElementTree.AddFirst(newHitInputElement);
                                newHitInputElement = newHitInputElement.Parent as FrameworkElement;
                            }

                            // start the search for differences in the two lists by setting the nodes to the top of the lists (the top most parent in the visual tree)
                            LinkedListNode<FrameworkElement> currentHoverNode = mHoverElements.First;
                            LinkedListNode<FrameworkElement> currentHitInputNode = mTempHitInputElementTree.First;

                            // traverse down the list and check if the elements in both lists match and have a child element
                            // the loop is stopped at the point where the lists don't match
                            while (currentHoverNode != null && currentHitInputNode != null && currentHoverNode.Value == currentHitInputNode.Value)
                            {
                                currentHoverNode = currentHoverNode.Next;
                                currentHitInputNode = currentHitInputNode.Next;
                            }

                            // the remaining elements in the hover list are not included in the current hit and are therefor getting a "leaving" event
                            RoutedPointDeviceHoverEventArgs leaveEventArgs = new RoutedPointDeviceHoverEventArgs(WPFPointDevice.PointDeviceLeaveEvent, this);

                            while (currentHoverNode != null)
                            {
                                currentHoverNode.Value.RaiseEvent(leaveEventArgs);
                                currentHoverNode = currentHoverNode.Next;
                            }

                            // the remaining elements in the hitinput list have not been in the hover list up until now and therefor get an "enter" event
                            RoutedPointDeviceHoverEventArgs enterEventArgs = new RoutedPointDeviceHoverEventArgs(WPFPointDevice.PointDeviceEnterEvent, this);

                            while (currentHitInputNode != null)
                            {
                                currentHitInputNode.Value.RaiseEvent(enterEventArgs);
                                currentHitInputNode = currentHitInputNode.Next;
                            }

                            // the elements of the hit input list are now the element over which our device is "hovering" so just copy over the list to the hover list
                            mHoverElements = new LinkedList<FrameworkElement>(mTempHitInputElementTree);
                        }
                    }
                    else
                    {
                        // if the point device has not been hovering over any elements before, just store the element tree from the current hittest and send an "enter" event to all elements
                        RoutedPointDeviceHoverEventArgs enterEventArgs = new RoutedPointDeviceHoverEventArgs(WPFPointDevice.PointDeviceEnterEvent, this);

                        FrameworkElement newHoverElement = mHitInputElement;
                        while (newHoverElement != null)
                        {
                            newHoverElement.RaiseEvent(enterEventArgs);
                            mHoverElements.AddFirst(newHoverElement);
                            newHoverElement = newHoverElement.Parent as FrameworkElement;
                        }
                    }
                }
                else
                {
                    // if we didn't hit anything, all elements which the point device was previously hovering over are getting "leave" events
                    RoutedPointDeviceHoverEventArgs leaveEventArgs = new RoutedPointDeviceHoverEventArgs(WPFPointDevice.PointDeviceLeaveEvent, this);

                    LinkedListNode<FrameworkElement> currentHoverNode = mHoverElements.First;
                    while (currentHoverNode != null)
                    {
                        currentHoverNode.Value.RaiseEvent(leaveEventArgs);
                        currentHoverNode = currentHoverNode.Next;
                    }

                    // as there are now no elements which the point device is hovering over we clear the hover list
                    mHoverElements.Clear();
                }
            }
        }

        private HitTestFilterBehavior HitTestFilter(DependencyObject obj)
        {
            UIElement elem = obj as UIElement;

            if (elem != null)
            {
                // exclude objects from the hittest which have disabled hittesting or are not visible 
                if (elem.IsHitTestVisible == false || elem.Visibility != Visibility.Visible)
                {
                    return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                }
            }

            return HitTestFilterBehavior.Continue;
        }

        private HitTestResultBehavior HitTestResult(HitTestResult result)
        {
            mHitInputElement = result.VisualHit as FrameworkElement;
            if (mHitInputElement != null)
            {   
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }
    }

    /// <summary>
    /// Provides extension methods for the WPF class UIElement to ease capturing of PointDevices.
    /// </summary>
    public static class WPFPointDeviceCaptureExtensions
    {
        /// <summary>
        /// Attempts to force capture of the given PointDevice to this element
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="device">Device to capture</param>
        /// <returns>true if the mouse is successfully captured; otherwise, false</returns>
        public static bool CapturePointDevice(this FrameworkElement elem, WPFPointDevice device)
        {
            return device.Capture(elem);
        }

        /// <summary>
        /// Releases the PointDevice capture, if this element held the capture
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="device">Device that is to release</param>
        public static void ReleasePointDeviceCapture(this UIElement elem, WPFPointDevice device)
        {
            device.ReleaseCapture();
        }

        /// <summary>
        /// Is this point device already captured to this element?
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static bool IsPointDeviceCaptured(this UIElement elem, WPFPointDevice device)
        {
            return elem == device.Captured;            
        }

        /// <summary>
        /// Returns the number of PointDevices that are captured to this element
        /// </summary>
        /// <param name="elem"></param>
        /// <returns>Number of captured PointDevices</returns>
        public static int GetPointDeviceCaptureCount(this UIElement elem)
        {
            int i = 0;
            foreach (UIElement capt in WPFPointDevice.sCaptures.Values)
            {
                if (capt == elem) i++;
            }
            return i;
        }
    }
}
