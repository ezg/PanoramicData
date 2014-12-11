using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InputFramework.DeviceDriver;
using InputFramework;
using mil.win32;

namespace InputFramework.Helper
{

    public class MouseEmulator
    {
        private DeviceDriver.DeviceManager mDeviceManager;
        private bool mIsEnabled = false;

        public MouseEmulator(DeviceDriver.DeviceManager deviceManager)
        {
            mDeviceManager = deviceManager;
            mDeviceManager.PointEventHook = new InputFramework.DeviceDriver.DeviceManager.PointEventHookHandler(DeviceManager_Hook);
        }

        private bool DeviceManager_Hook(PointEventArgs args)
        {
            if (!mIsEnabled) return true;
            if (args.DeviceType == DeviceType.Mouse || args.DeviceType == DeviceType.Stylus) return true;
            int inputType = -1;
            switch(args.PointEventType){
                case PointEventType.LeftDown: inputType = 0; break;
                case PointEventType.LeftUp: inputType = 1; break;
                case PointEventType.RightDown: inputType = 2; break;    
                case PointEventType.RightUp: inputType = 3; break;
            }
            SendInputEvent.SendMouseInput(inputType, (int)args.PointScreen.X, (int)args.PointScreen.Y, args.PointScreen.Intensity);
            return false;
        }

        public void Enable()
        {
            mIsEnabled = true;
        }

        public void Disable()
        {
            mIsEnabled = false;
        }
    }
}
