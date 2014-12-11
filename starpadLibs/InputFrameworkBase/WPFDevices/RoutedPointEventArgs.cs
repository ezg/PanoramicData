using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace InputFramework.WPFDevices
{
    public delegate void RoutedPointEventHandler(Object sender, RoutedPointEventArgs pointEventArgs);     

    public class RoutedPointEventArgs : RoutedEventArgs
    {
        public PointEventArgs PointEventArgs { get; set; }
        public WPFPointDevice WPFPointDevice { get; set; }

        public RoutedPointEventArgs(RoutedEvent routedEvent, PointEventArgs pointEventArgs, WPFPointDevice wpfPointDevice) : base(routedEvent)
        {
            PointEventArgs = pointEventArgs;
            WPFPointDevice = wpfPointDevice;
        }

        public Point GetPosition(Visual relativeTo)
        {
            try
            {
                return relativeTo.PointFromScreen(PointEventArgs.PointScreen.Point);
            }
            catch (Exception ex)
            {
                return new Point();
            }
        }

        public StylusPointCollection GetStylusPoints(Visual relativeTo)
        {
            StylusPointCollection temp = new StylusPointCollection(PointEventArgs.InBetweenPointsScreen.Length);
            foreach (PressurePoint p in PointEventArgs.InBetweenPointsScreen)
            {
                Point tp = relativeTo.PointFromScreen(p.Point);
                temp.Add(new StylusPoint(tp.X, tp.Y, p.Intensity));
            }
            return temp;
        }

        public Point[] GetInBeweenPoints(Visual relativeTo)
        {
            Point[] points = new Point[PointEventArgs.InBetweenPointsScreen.Length];
            for(int i = 0; i < PointEventArgs.InBetweenPointsScreen.Length; i++) {
                points[i] = relativeTo.PointFromScreen(PointEventArgs.InBetweenPointsScreen[i].Point);
            }
            return points;
        }

        public float Intensity
        {
            get
            {
                return PointEventArgs.PointScreen.Intensity;
            }
        }

        public DeviceType DeviceType
        {
            get
            {
                return PointEventArgs.DeviceType;
            }
        }

        public override string ToString()
        {
            return PointEventArgs.ToString();
        }
    }
}
