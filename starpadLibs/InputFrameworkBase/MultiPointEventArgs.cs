using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputFramework
{
    public enum MultiPointEventType
    {
        Unknown,
        Down,
        Up,
        Drag
    }

    public class MultiPointEventArgs : EventArgs
    {
        public DeviceUID DeviceUID { set; get; }
        public MultiPointEventType MultiPointEventType { set; get; }
        public PressurePoint PointScreen { set; get; }
        public long PointID { set; get; }

        public override string ToString()
        {
            return "DeviceUID: " + DeviceUID + ", EventType: " + MultiPointEventType + ", PointScreen: " + PointScreen;
        }
    }
}
