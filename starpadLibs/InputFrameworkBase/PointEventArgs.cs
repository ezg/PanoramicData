using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace InputFramework
{
    public enum PointEventType
    {
        Unknown,
        LeftUp,
        LeftDown,
        MiddleUp,
        MiddleDown,
        RightUp,
        RightDown,
        Move,
        Drag
    }

    public enum DeviceType
    {
        Unknown,
        Mouse,
        Stylus,
        MultiTouch
    }

    public class PointEventArgs : EventArgs
    {
        public DeviceUID       DeviceUID             { set; get; }
        public DeviceType      DeviceType            { set; get; }
        public PointEventType  PointEventType        { set; get; }
        public PressurePoint   PointScreen           { set; get; }
        public PressurePoint[] InBetweenPointsScreen { set; get; }
        public StylusEventArgs StylusEventArgs       { get; set; }
        public bool            Handled               { set; get; }
        public object          Args                  { set; get; }

        public override string ToString()
        {
            return "DeviceUID: " + DeviceUID + ", DeviceType: " + DeviceType + ", EventType: " + PointEventType + ", PointScreen: " + PointScreen;
        }
    }
}
