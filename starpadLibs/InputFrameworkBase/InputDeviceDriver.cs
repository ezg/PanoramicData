using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputFramework.DeviceDriver
{
    /// <summary>
    /// Class InputDevice.cs
    /// Author: Adam Gokcezade
    /// Creation Date: 27. August 2008
    /// </summary>

    public abstract class InputDeviceDriver : IDisposable
    {
        public delegate void AddDeviceHandler(Object sender, DeviceUID device);
        public delegate void RemoveDeviceHandler(Object sender, DeviceUID device);
        public event AddDeviceHandler AddDeviceEvent;
        public event RemoveDeviceHandler RemoveDeviceEvent;

        private static long sNextDeviceDriverID = 0;
        private long mDeviceDriverID; 

        public InputDeviceDriver()
        {
            mDeviceDriverID = sNextDeviceDriverID++;
        }

        public abstract void Dispose();

        protected void OnAddDeviceEvent(DeviceUID device)
        {
            if (AddDeviceEvent != null)
            {
                AddDeviceEvent(this, device);
            }
        }

        protected void OnRemoveDeviceEvent(DeviceUID device)
        {
            if (RemoveDeviceEvent != null)
            {
                RemoveDeviceEvent(this, device);
            }
        }

        public long DeviceDriverID
        {
            get
            {
                return mDeviceDriverID;
            }
        }
    }
}
