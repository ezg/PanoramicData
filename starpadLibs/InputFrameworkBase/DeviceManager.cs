using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Collections;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using InputFramework;

namespace InputFramework.DeviceDriver
{     
    /// <summary>
    /// Class DeviceManager.cs
    /// Author: Adam Gokcezade
    /// Creation Date: 29. August 2008
    /// </summary>
    public class DeviceManager : IDisposable
    {
        private static DeviceManager sInstance;
        private List<PointDeviceDriver> mPointDeviceDriver;
        private Dictionary<DeviceUID, PointEventHandler> mPointDevices;

        private List<MultiPointDeviceDriver> mMultiPointDeviceDriver;
        private Dictionary<DeviceUID, MultiPointEventHandler> mMultiPointDevices;

        public delegate void PointEventHandler(Object sender, PointEventArgs pointEventArgs);
        public delegate void MultiPointEventHandler(Object sender, MultiPointEventArgs multiPointEventArgs);

        public delegate void PointDeviceAddedHandler(Object sender, DeviceUID deviceUID);
        public event PointDeviceAddedHandler PointDeviceAdded;
        public delegate void PointDeviceRemovedHandler(Object sender, DeviceUID deviceUID);
        public event PointDeviceRemovedHandler PointDeviceRemoved;

        public delegate void MultiPointDeviceAddedHandler(Object sender, DeviceUID deviceUID);
        public event MultiPointDeviceAddedHandler MultiPointDeviceAdded;
        public delegate void MultiPointDeviceRemovedHandler(Object sender, DeviceUID deviceUID);
        public event MultiPointDeviceRemovedHandler MultiPointDeviceRemoved;

        public delegate bool PointEventHookHandler(PointEventArgs args);
        /// <summary>
        /// Is called before a point event is risen. If the hook returns false, the event will not be risen at all.
        /// </summary>
        public PointEventHookHandler PointEventHook;

        #region Constructor & Singleton GetInstance()

        private DeviceManager()
        {
            mPointDeviceDriver = new List<PointDeviceDriver>();
            mPointDevices = new Dictionary<DeviceUID, PointEventHandler>(new DeviceUID.EqualityComparer());

            mMultiPointDeviceDriver = new List<MultiPointDeviceDriver>();
            mMultiPointDevices = new Dictionary<DeviceUID, MultiPointEventHandler>(new DeviceUID.EqualityComparer());
        }

        /// <summary>
        /// Returns Instance of DeviceManager. DeviceManager is a Singleton
        /// </summary>
        public static DeviceManager Instance
        {
            get { return GetInstance(); }
        }

        public static DeviceManager GetInstance()
        {
            if (sInstance == null) sInstance = new DeviceManager();
            return sInstance;
        }

        public void Dispose()
        {
            List<PointDeviceDriver> pointDeviceDriverCopy = new List<PointDeviceDriver>(mPointDeviceDriver);
            foreach (PointDeviceDriver pointDeviceDriver in pointDeviceDriverCopy)
            {
                RemovePointDeviceDriver(pointDeviceDriver);
                pointDeviceDriver.Dispose();
            }

            List<MultiPointDeviceDriver> multiPointDeviceDriverCopy = new List<MultiPointDeviceDriver>(mMultiPointDeviceDriver);
            foreach (MultiPointDeviceDriver multiPointDeviceDriver in multiPointDeviceDriverCopy)
            {
                RemoveMultiPointDeviceDriver(multiPointDeviceDriver);
                multiPointDeviceDriver.Dispose();
            }
        }

        #endregion

        public void AddPointDeviceDriver(PointDeviceDriver pointDeviceDriver)
        {
            mPointDeviceDriver.Add(pointDeviceDriver);

            // register event handler
            pointDeviceDriver.PointEvent += new PointDeviceDriver.PointEventHandler(pointDeviceDriver_PointEvent);
            pointDeviceDriver.AddDeviceEvent += new InputDeviceDriver.AddDeviceHandler(pointDeviceDriver_AddDeviceEvent);
            pointDeviceDriver.RemoveDeviceEvent += new InputDeviceDriver.RemoveDeviceHandler(pointDeviceDriver_RemoveDeviceEvent);
        }

        public void RemovePointDeviceDriver(PointDeviceDriver driver)
        {
            if (mPointDeviceDriver.Contains(driver))
            {
                // unregister event handler
                driver.AddDeviceEvent -= pointDeviceDriver_AddDeviceEvent;
                driver.RemoveDeviceEvent -= pointDeviceDriver_RemoveDeviceEvent;
                driver.PointEvent -= pointDeviceDriver_PointEvent;

                RemovePointDevices(driver.DeviceDriverID);
                mPointDeviceDriver.Remove(driver);
            }
        }

        private void RemovePointDevices(long deviceDriverID)
        {
            List<DeviceUID> devicesToRemove = new List<DeviceUID>();
            foreach (KeyValuePair<DeviceUID, PointEventHandler> pointDevice in mPointDevices)
            {
                DeviceUID deviceUID = pointDevice.Key;
                if (deviceUID.DeviceDriverID == deviceDriverID)
                {
                    devicesToRemove.Add(deviceUID);
                }
            }

            foreach (DeviceUID deviceUID in devicesToRemove)
            {
                mPointDevices.Remove(deviceUID);
                OnPointDeviceRemoved(deviceUID);
            }
        }

        public void RegisterPointEventHandler(DeviceUID deviceUID, PointEventHandler pointEventHandler)
        {
            if(mPointDevices.ContainsKey(deviceUID)) {
                if(mPointDevices[deviceUID] != null) {
                    mPointDevices[deviceUID] += pointEventHandler;
                } else {
                    mPointDevices[deviceUID] = pointEventHandler;
                }
            }
        }

        public void UnRegisterPointEventHandler(DeviceUID deviceUID, PointEventHandler pointEventHandler)
        {
            if (mPointDevices.ContainsKey(deviceUID))
            {
                if (mPointDevices[deviceUID] != null)
                {
                    mPointDevices[deviceUID] -= pointEventHandler;
                }
            }
        }

        protected void OnPointDeviceAdded(DeviceUID deviceUID)
        {
            if (PointDeviceAdded != null)
            {
                PointDeviceAdded(this, deviceUID);
            }
        }

        protected void OnPointDeviceRemoved(DeviceUID deviceUID)
        {
            if (PointDeviceRemoved != null)
            {
                PointDeviceRemoved(this, deviceUID);
            }
        }

        private void pointDeviceDriver_PointEvent(Object sender, PointEventArgs pointEventArgs)
        {
            if (!mPointDevices.ContainsKey(pointEventArgs.DeviceUID))
            {
                mPointDevices.Add(pointEventArgs.DeviceUID, null);
                OnPointDeviceAdded(pointEventArgs.DeviceUID);
            }

            if (PointEventHook == null || PointEventHook(pointEventArgs)) // the hook enables blocking of events
            {
                PointEventHandler pointEventHandler = mPointDevices[pointEventArgs.DeviceUID];
                if (pointEventHandler != null)
                {
                    pointEventHandler(this, pointEventArgs);
                }
            }
        }

        private void pointDeviceDriver_AddDeviceEvent(Object sender, DeviceUID device)
        {
            if (!mPointDevices.ContainsKey(device))
            {
                mPointDevices.Add(device, null);
                OnPointDeviceAdded(device);
            }
        }

        private void pointDeviceDriver_RemoveDeviceEvent(object sender, DeviceUID device)
        {
            if (mPointDevices.ContainsKey(device))
            {
                mPointDevices.Remove(device);
                OnPointDeviceRemoved(device);
            }
        }

        public void AddMultiPointDeviceDriver(MultiPointDeviceDriver multiPointDeviceDriver)
        {
            mMultiPointDeviceDriver.Add(multiPointDeviceDriver);

            // register event handler
            multiPointDeviceDriver.MultiPointEvent += new MultiPointDeviceDriver.MultiPointEventHandler(multiPointDeviceDriver_MultiPointEvent);
            multiPointDeviceDriver.AddDeviceEvent += new InputDeviceDriver.AddDeviceHandler(multiPointDeviceDriver_AddDeviceEvent);
        }

        public void RemoveMultiPointDeviceDriver(MultiPointDeviceDriver driver)
        {
            if (mMultiPointDeviceDriver.Contains(driver))
            {
                // unregister event handler
                driver.AddDeviceEvent -= multiPointDeviceDriver_AddDeviceEvent;
                driver.MultiPointEvent -= multiPointDeviceDriver_MultiPointEvent;

                RemoveMultiPointDevices(driver.DeviceDriverID);
                mMultiPointDeviceDriver.Remove(driver);
            }
        }

        private void RemoveMultiPointDevices(long deviceDriverID)
        {
            List<DeviceUID> devicesToRemove = new List<DeviceUID>();
            foreach (KeyValuePair<DeviceUID, MultiPointEventHandler> multiPointDevice in mMultiPointDevices)
            {
                DeviceUID deviceUID = multiPointDevice.Key;
                if (deviceUID.DeviceDriverID == deviceDriverID)
                {
                    devicesToRemove.Add(deviceUID);
                }
            }

            foreach (DeviceUID deviceUID in devicesToRemove)
            {
                mMultiPointDevices.Remove(deviceUID);
                OnMultiPointDeviceRemoved(deviceUID);
            }
        }

        public void RegisterMultiPointEventHandler(DeviceUID deviceUID, MultiPointEventHandler multiPointEventHandler)
        {
            if (mMultiPointDevices.ContainsKey(deviceUID))
            {
                if (mMultiPointDevices[deviceUID] != null)
                {
                    mMultiPointDevices[deviceUID] += multiPointEventHandler;
                }
                else
                {
                    mMultiPointDevices[deviceUID] = multiPointEventHandler;
                }
            }
        }

        public void UnRegisterMultiPointEventHandler(DeviceUID deviceUID, MultiPointEventHandler multiPointEventHandler)
        {
            if (mMultiPointDevices.ContainsKey(deviceUID))
            {
                if (mMultiPointDevices[deviceUID] != null)
                {
                    mMultiPointDevices[deviceUID] -= multiPointEventHandler;
                }
            }
        }

        protected void OnMultiPointDeviceAdded(DeviceUID deviceUID)
        {
            if (MultiPointDeviceAdded != null)
            {
                MultiPointDeviceAdded(this, deviceUID);
            }
        }

        protected void OnMultiPointDeviceRemoved(DeviceUID deviceUID)
        {
            if (MultiPointDeviceRemoved != null)
            {
                MultiPointDeviceRemoved(this, deviceUID);
            }
        }

        private void multiPointDeviceDriver_MultiPointEvent(Object sender, MultiPointEventArgs multiPointEventArgs)
        {
            if (!mMultiPointDevices.ContainsKey(multiPointEventArgs.DeviceUID))
            {
                mMultiPointDevices.Add(multiPointEventArgs.DeviceUID, null);
                OnMultiPointDeviceAdded(multiPointEventArgs.DeviceUID);
            }

            MultiPointEventHandler multiPointEventHandler = mMultiPointDevices[multiPointEventArgs.DeviceUID];
            if (multiPointEventHandler != null)
            {
                multiPointEventHandler(this, multiPointEventArgs);
            }
        }

        private void multiPointDeviceDriver_AddDeviceEvent(Object sender, DeviceUID device)
        {
            if (!mMultiPointDevices.ContainsKey(device))
            {
                mMultiPointDevices.Add(device, null);
                OnMultiPointDeviceAdded(device);
            }
        }

    }
}
