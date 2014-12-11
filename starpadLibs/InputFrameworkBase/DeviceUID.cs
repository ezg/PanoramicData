using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Text;

namespace InputFramework
{
    public class DeviceUID 
    {
        private static DeviceUID sEmptyDeviceUID = new DeviceUID();
        /// <summary>
        /// Default empty DeviceUID. (points to a internal static value)
        /// </summary>
        public static DeviceUID EmptyDeviceUID{ get{ return sEmptyDeviceUID;}}

        public DeviceUID()
        {
            DeviceDriverID = -1;
            DeviceID = -1;
        }

        public long DeviceDriverID { set; get; }
        public long DeviceID { set; get; }

        public override int GetHashCode()
        {
            return ((int)DeviceDriverID ^ (int)(DeviceDriverID >> 32)) ^ ((int)DeviceID ^ (int)(DeviceID >> 32));
        }

        public override string ToString()
        {
            return "DeviceDriverID: " + DeviceDriverID + ", DeviceID: " + DeviceID;
        }

        public static bool operator == (DeviceUID x, DeviceUID y)
        {
            if (Object.Equals(x, null) && !Object.Equals(y, null)) return false;
            if (Object.Equals(x, null) && Object.Equals(y, null)) return true;

            return x.Equals(y);
        }

        public static bool operator !=(DeviceUID x, DeviceUID y)
        {
            if (Object.Equals(x, null) && !Object.Equals(y, null)) return true;
            if (Object.Equals(x, null) && Object.Equals(y, null)) return false;

            return !x.Equals(y);
        }

        public override bool Equals(object obj)
        {
            if (obj is DeviceUID)
            {
                return (((DeviceUID)obj).DeviceID == DeviceID) && (((DeviceUID)obj).DeviceDriverID == DeviceDriverID);
            }
            return base.Equals(obj);
        }

        public class EqualityComparer : IEqualityComparer<DeviceUID>
        {
            public bool Equals(DeviceUID x, DeviceUID y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(DeviceUID x)
            {
                return x.GetHashCode();
            }
        }
    }
}
