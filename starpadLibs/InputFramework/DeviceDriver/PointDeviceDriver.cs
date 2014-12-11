using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputFramework.DeviceDriver
{
    /// <summary>
    /// Class PointDeviceDriver.cs
    /// Author: Adam Gokcezade
    /// Creation Date: 27. August 2008
    /// </summary>

    public abstract class PointDeviceDriver : InputDeviceDriver
    {
        public delegate void PointEventHandler(Object sender, PointEventArgs pointEventArgs);

        public event PointEventHandler PointEvent;

        protected void OnPointEvent(PointEventArgs pointEventArgs)
        {
            if (PointEvent != null)
            {
                PointEvent(this, pointEventArgs);
            }
        }
    }
}
