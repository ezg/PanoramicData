using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

using InputFramework.DeviceDriver;
using System.Windows.Input;

namespace InputFramework.WPFDevices
{
    public delegate void RoutedMultiPointEventHandler(Object sender, RoutedMultiPointEventArgs multiPointEventArgs);     

    public class RoutedMultiPointEventArgs : RoutedEventArgs
    {
        public MultiPointEventArgs MultiPointEventArgs { get; set; }
        public WPFMultiPointDevice WPFMultiPointDevice { get; set; }

        public RoutedMultiPointEventArgs(RoutedEvent routedEvent, MultiPointEventArgs multiPointEventArgs, WPFMultiPointDevice wpfMultiPointDevice) : base(routedEvent)
        {
            MultiPointEventArgs = multiPointEventArgs;
            WPFMultiPointDevice = wpfMultiPointDevice;
        }

        public Point GetPosition(Visual relativeTo)
        {
            return relativeTo.PointFromScreen(MultiPointEventArgs.PointScreen.Point);
        }

        public float Intensity
        {
            get
            {
                return MultiPointEventArgs.PointScreen.Intensity;
            }
        }

        public override string ToString()
        {
            return MultiPointEventArgs.ToString();
        }
    }
}
