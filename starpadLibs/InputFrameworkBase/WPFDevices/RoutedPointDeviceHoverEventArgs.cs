using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace InputFramework.WPFDevices
{
    public delegate void RoutedPointDeviceHoverEventHandler(Object sender, RoutedPointDeviceHoverEventArgs routedPointDeviceEnterEventArgs);    

    public class RoutedPointDeviceHoverEventArgs : RoutedEventArgs
    {
        public WPFPointDevice WPFPointDevice { get; set; }

        public RoutedPointDeviceHoverEventArgs(RoutedEvent routedEvent, WPFPointDevice wpfPointDevice) : base(routedEvent)
        {
            WPFPointDevice = wpfPointDevice;
        }
    }
}
