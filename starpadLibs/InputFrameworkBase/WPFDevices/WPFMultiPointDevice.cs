using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

using InputFramework.DeviceDriver;
using System.Diagnostics;

namespace InputFramework.WPFDevices
{
    public class WPFMultiPointDevice : WPFDevice
    {
        public static readonly RoutedEvent PreviewMultiPointDownEvent = EventManager.RegisterRoutedEvent("PreviewMultiPointDown", RoutingStrategy.Tunnel, typeof(RoutedMultiPointEventHandler), typeof(WPFMultiPointDevice));
        public static readonly RoutedEvent PreviewMultiPointUpEvent = EventManager.RegisterRoutedEvent("PreviewMultiPointUp", RoutingStrategy.Tunnel, typeof(RoutedMultiPointEventHandler), typeof(WPFMultiPointDevice));
        public static readonly RoutedEvent PreviewMultiPointDragEvent = EventManager.RegisterRoutedEvent("PreviewMultiPointDrag", RoutingStrategy.Tunnel, typeof(RoutedMultiPointEventHandler), typeof(WPFMultiPointDevice));

        public static readonly RoutedEvent MultiPointDownEvent = EventManager.RegisterRoutedEvent("MultiPointDown", RoutingStrategy.Bubble, typeof(RoutedMultiPointEventHandler), typeof(WPFMultiPointDevice));
        public static readonly RoutedEvent MultiPointUpEvent = EventManager.RegisterRoutedEvent("MultiPointUp", RoutingStrategy.Bubble, typeof(RoutedMultiPointEventHandler), typeof(WPFMultiPointDevice));
        public static readonly RoutedEvent MultiPointDragEvent = EventManager.RegisterRoutedEvent("MultiPointDrag", RoutingStrategy.Bubble, typeof(RoutedMultiPointEventHandler), typeof(WPFMultiPointDevice));

        private UIElement mVisualRoot;
        private IInputElement mHitInputElement;

        public WPFMultiPointDevice(DeviceUID deviceUID, UIElement visualRoot) : base(deviceUID)
        {
            mVisualRoot = visualRoot;
        }

        public void deviceManager_MultiPointEvent(Object Sender, MultiPointEventArgs multiPointEventArgs)
        {   
            if (mVisualRoot.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                mVisualRoot.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new EventHandler<MultiPointEventArgs>(deviceManager_MultiPointEvent), Sender, multiPointEventArgs);
            }
            else
            {
                UIElement hitTestRoot = mVisualRoot;

                // transform the input point to the hitTest element coordinate system
                Point pointVisualRoot = hitTestRoot.PointFromScreen(multiPointEventArgs.PointScreen.Point);

                // do the hittest
                mHitInputElement = null;
                VisualTreeHelper.HitTest(hitTestRoot, new HitTestFilterCallback(HitTestFilter), new HitTestResultCallback(HitTestResult), new PointHitTestParameters(pointVisualRoot));

                // Event routing and sending
                if(mHitInputElement != null)
                {
                    RoutedEvent tunnelingEventType;
                    RoutedEvent bubbelingEventType;

                    switch (multiPointEventArgs.MultiPointEventType)
                    {
                        case MultiPointEventType.Down:
                            tunnelingEventType = WPFMultiPointDevice.PreviewMultiPointDownEvent;
                            bubbelingEventType = WPFMultiPointDevice.MultiPointDownEvent;
                            break;
                        case MultiPointEventType.Up:
                            tunnelingEventType = WPFMultiPointDevice.PreviewMultiPointUpEvent;
                            bubbelingEventType = WPFMultiPointDevice.MultiPointUpEvent;
                            break;
                        case MultiPointEventType.Drag:
                            tunnelingEventType = WPFMultiPointDevice.PreviewMultiPointDragEvent;
                            bubbelingEventType = WPFMultiPointDevice.MultiPointDragEvent;
                            break;
                        default:
                            // todo: maybe create a special RoutedEvent for unknown events
                            Trace.WriteLine("WPFMultiPointDevice: Unknown MultiPointEvent");
                            return;
                    }

                    RoutedMultiPointEventArgs tunnelingEventArgs = new RoutedMultiPointEventArgs(tunnelingEventType, multiPointEventArgs, this);
                    mHitInputElement.RaiseEvent(tunnelingEventArgs);

                    RoutedMultiPointEventArgs bubblingEventArgs = new RoutedMultiPointEventArgs(bubbelingEventType, multiPointEventArgs, this);
                    mHitInputElement.RaiseEvent(bubblingEventArgs);
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
            mHitInputElement = result.VisualHit as IInputElement;
            if (mHitInputElement != null)
            {
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }
    }
}
