using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using InputFramework.DeviceDriver;
using System.Windows;

namespace InputFramework.WPFDevices
{
    public class WPFDeviceManager
    {
        public delegate void WPFPointDeviceAddedHandler(Object sender, WPFPointDevice wpfPointDevice);
        public event WPFPointDeviceAddedHandler WPFPointDeviceAdded;
        public delegate void WPFPointDeviceRemovedHandler(Object sender, WPFPointDevice wpfPointDevice);
        public event WPFPointDeviceRemovedHandler WPFPointDeviceRemoved;

        public delegate void WPFMultiPointDeviceAddedHandler(Object sender, WPFMultiPointDevice wpfPointDevice);
        public event WPFMultiPointDeviceAddedHandler WPFMultiPointDeviceAdded;
        public delegate void WPFMultiPointDeviceRemovedHandler(Object sender, WPFMultiPointDevice wpfPointDevice);
        public event WPFMultiPointDeviceRemovedHandler WPFMultiPointDeviceRemoved;

        private Dictionary<DeviceUID, WPFPointDevice> mWPFPointDevices;
        private Dictionary<DeviceUID, WPFMultiPointDevice> mWPFMultiPointDevices;
        private DeviceManager mDeviceManager;
        private UIElement mVisualRoot;

        /// <summary>
        /// Construct device manager that will just use a certain Window or UIElement. All other windows and also the z-order of the windows is ignored.
        /// </summary>
        /// <param name="deviceManager"></param>
        /// <param name="visualRoot"></param>
        public WPFDeviceManager(DeviceManager deviceManager, UIElement visualRoot)
        {
            mWPFPointDevices = new Dictionary<DeviceUID, WPFPointDevice>();
            mWPFMultiPointDevices = new Dictionary<DeviceUID, WPFMultiPointDevice>();
            mDeviceManager = deviceManager;
            mVisualRoot = visualRoot;

            mDeviceManager.PointDeviceAdded += new DeviceManager.PointDeviceAddedHandler(deviceManager_PointDeviceAdded);
            mDeviceManager.PointDeviceRemoved += new DeviceManager.PointDeviceRemovedHandler(deviceManager_PointDeviceRemoved);
            mDeviceManager.MultiPointDeviceAdded += new DeviceManager.MultiPointDeviceAddedHandler(deviceManager_MultiPointDeviceAdded);
            mDeviceManager.MultiPointDeviceRemoved += new DeviceManager.MultiPointDeviceRemovedHandler(deviceManager_MultiPointDeviceRemoved);
        }

        /// <summary>
        /// Construct device manager.
        /// </summary>
        /// <param name="deviceManager"></param>
        public WPFDeviceManager(DeviceManager deviceManager)
        {
            mWPFPointDevices = new Dictionary<DeviceUID, WPFPointDevice>();
            mWPFMultiPointDevices = new Dictionary<DeviceUID, WPFMultiPointDevice>();
            mDeviceManager = deviceManager;
            mVisualRoot = null;

            mDeviceManager.PointDeviceAdded += new DeviceManager.PointDeviceAddedHandler(deviceManager_PointDeviceAdded);
            mDeviceManager.PointDeviceRemoved += new DeviceManager.PointDeviceRemovedHandler(deviceManager_PointDeviceRemoved);
            mDeviceManager.MultiPointDeviceAdded += new DeviceManager.MultiPointDeviceAddedHandler(deviceManager_MultiPointDeviceAdded);
            mDeviceManager.MultiPointDeviceRemoved += new DeviceManager.MultiPointDeviceRemovedHandler(deviceManager_MultiPointDeviceRemoved);
        }

        private void deviceManager_PointDeviceAdded(Object sender, DeviceUID device)
        {
            WPFPointDevice pointDevice = new WPFPointDevice(device, mVisualRoot);

            mDeviceManager.RegisterPointEventHandler(device, new DeviceManager.PointEventHandler(pointDevice.ProcessDeviceManagerPointEvent));

            mWPFPointDevices.Add(device, pointDevice);

            OnWPFPointDeviceAdded(pointDevice);

            //Console.WriteLine("Added!" + device.DeviceID);
        }

        private void deviceManager_PointDeviceRemoved(Object sender, DeviceUID device)
        {
            WPFPointDevice pointDevice = mWPFPointDevices[device];

            if(pointDevice != null) {
                mDeviceManager.UnRegisterPointEventHandler(device, pointDevice.ProcessDeviceManagerPointEvent);

                mWPFPointDevices.Remove(device);

                OnWPFPointDeviceRemoved(pointDevice);

                //Console.WriteLine("Removed!" + device.DeviceID);
            }
        }

        private void deviceManager_MultiPointDeviceAdded(Object sender, DeviceUID device)
        {
            WPFMultiPointDevice multiPointDevice = new WPFMultiPointDevice(device, mVisualRoot);

            mDeviceManager.RegisterMultiPointEventHandler(device, new DeviceManager.MultiPointEventHandler(multiPointDevice.deviceManager_MultiPointEvent));

            mWPFMultiPointDevices.Add(device, multiPointDevice);

            OnWPFMultiPointDeviceAdded(multiPointDevice);
        }

        private void deviceManager_MultiPointDeviceRemoved(Object sender, DeviceUID device)
        {
            WPFMultiPointDevice multiPointDevice = mWPFMultiPointDevices[device];

            if (multiPointDevice != null)
            {
                mDeviceManager.UnRegisterMultiPointEventHandler(device, multiPointDevice.deviceManager_MultiPointEvent);

                mWPFMultiPointDevices.Remove(device);

                OnWPFMultiPointDeviceRemoved(multiPointDevice);
            }
        }

        protected void OnWPFPointDeviceAdded(WPFPointDevice wpfPointDevice)
        {
            if (WPFPointDeviceAdded != null)
            {
                WPFPointDeviceAdded(this, wpfPointDevice);
            }
        }

        protected void OnWPFPointDeviceRemoved(WPFPointDevice wpfPointDevice)
        {
            if (WPFPointDeviceRemoved != null)
            {
                WPFPointDeviceRemoved(this, wpfPointDevice);
            }
        }

        protected void OnWPFMultiPointDeviceAdded(WPFMultiPointDevice wpfMultiPointDevice)
        {
            if (WPFMultiPointDeviceAdded != null)
            {
                WPFMultiPointDeviceAdded(this, wpfMultiPointDevice);
            }
        }

        protected void OnWPFMultiPointDeviceRemoved(WPFMultiPointDevice wpfMultiPointDevice)
        {
            if (WPFMultiPointDeviceRemoved != null)
            {
                WPFMultiPointDeviceRemoved(this, wpfMultiPointDevice);
            }
        }
    }
}
