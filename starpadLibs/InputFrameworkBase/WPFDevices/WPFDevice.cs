using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InputFramework.DeviceDriver;

namespace InputFramework.WPFDevices
{
    public abstract class WPFDevice
    {
        private DeviceUID mDeviceUID;

        public WPFDevice(DeviceUID deviceUID)
        {
            mDeviceUID = deviceUID;
        }

        public DeviceUID DeviceUID
        {
            get {
                return mDeviceUID;
            }    
        }
    }
}
