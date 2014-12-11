using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Diagnostics;

namespace InputFramework.DeviceDriver
{
    public class NWPointDeviceDriver : PointDeviceDriver
    {
        public const int MAX_TOUCHES = 32;
        public const int NUM_POLYGON_POINTS = 4;

        public delegate void NWReceiveTouchInfoDelegate(Int32 deviceId, Int32 deviceStatus, Int32 packetID, Int32 touches, Int32 ghostTouches);
        public delegate void NWConnectEventDelegate(Int32 deviceId);
        public delegate void NWDisconnectEventDelegate(Int32 deviceId);

        private NWReceiveTouchInfoDelegate eventHandler;
        private NWConnectEventDelegate connectEventHandler;
        private NWDisconnectEventDelegate disconnectEventHandler;

        private Int32 myDeviceID;
        private Int32 currentPacketID;
        private Int32 currentTouches;
        private Int32 currentGhostTouches;

        private Dictionary<Int32, NWTouchPoint> touchList = new Dictionary<Int32, NWTouchPoint>();

        #region DLL Import Functions

        //Declare all the functions we will be using from the Multi-Touch DLL.
        //These functions are all defined within NWMultiTouch.h but we have to redefine them here
        //for use within Managed C#.

        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 OpenDevice(Int32 deviceID, NWReceiveTouchInfoDelegate callbackFn);
        [DllImport("NWMultiTouch.dll")]
        public static extern void CloseDevice(Int32 deviceID);
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 GetConnectedDeviceCount();
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 GetConnectedDeviceID(Int32 deviceNo);
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 GetTouch(Int32 deviceID, Int32 packetID, out NWTouchPoint touchPoint, Int32 touch, Int32 ghostTouch);
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 GetPolygon(Int32 deviceID, Int32 packetID, Int32 touches, Int32 ghostTouches, [In, Out] NWPoint[] pointArray, Int32 size);
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 GetTouchDeviceInfo(Int32 deviceID, out NWDeviceInfo deviceInfo);
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 SetReportMode(Int32 deviceID, Int32 reportMode);
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 GetReportMode(Int32 deviceID);
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 SetKalmanFilterStatus(Int32 deviceID, bool kalmanOn);
        [DllImport("NWMultiTouch.dll")]
        public static extern Int32 SetTouchScreenDimensions(Int32 deviceID, float xMin, float yMin, float xMax, float yMax);
        [DllImport("NWMultiTouch.dll")]
        public static extern void SetConnectEventHandler(NWConnectEventDelegate connectHandler);
        [DllImport("NWMultiTouch.dll")]
        public static extern void SetDisconnectEventHandler(NWDisconnectEventDelegate disconnectHandler);

        #endregion

        #region Structures

        //Define the required structures - from NWMultiTouch.h
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NWTouchPoint
        {
            public Int32 TouchID;
            public Int32 TouchType;
            public Int64 TouchStart;
            public NWPoint TouchPos;
            public float Velocity;
            public float Acceleration;
            public float TouchArea;
            public Int32 TouchEventType;
            public Int32 ConfidenceLevel;
            public float Height;
            public float Width;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NWDeviceInfo
        {
            public Int32 SerialNumber;
            public Int32 ModelNumber;
            public Int32 VersionMajor;
            public Int32 VersionMinor;
            public Int32 ProductID;
            public Int32 VendorID;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NWDebugTouchInfo
        {
            public NWPoint[][] actualSizes;
            public float touchSymmetry;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NWPoint
        {
            public float x;
            public float y;
        }

        #endregion

        #region Enums

        //Define the enums that are used - from NWMultiTouch.h
        public enum SuccessCode
        {
            SUCCESS = 1,
            ERR_DEVICE_NOT_OPEN = -1,
            ERR_INVALID_PACKET_ID = -2,
            ERR_INVALID_TOUCH_ID = -3,
            ERR_TOO_MANY_TOUCHES = -4,
            ERR_DEVICE_DOES_NOT_EXIST = -5,
            ERR_DISPLAY_DOES_NOT_EXIST = -6,
            ERR_FUNCTION_NOT_SUPPORTED = -7,
            ERR_INVALID_SENSOR_NUMBER = -8,
            ERR_SLOPES_MODE_NOT_SUPPORTED = -9
        }

        public enum TouchEventType
        {
            TE_TOUCH_DOWN = 1,
            TE_TOUCHING = 2,
            TE_TOUCH_UP = 3
        }

        public enum TouchType
        {
            TT_TOUCH = 1,
            TT_GHOSTTOUCH = 2,
            TT_CENTROID = 3
        }

        public enum DeviceStatus
        {
            DS_CONNECTED = 1,
            DS_TOUCH_INFO = 2,
            DS_DISCONNECTED = 3
        }

        public enum ReportModes
        {
            RM_NONE = 0,
            RM_MOUSE = 1,
            RM_MULTITOUCH = 2,
            RM_DIGITISER = 4,
            RM_SLOPESMODE = 8
        }

        #endregion

        public NWPointDeviceDriver()
        {
            //Create instances of the event handlers.
            eventHandler = new NWReceiveTouchInfoDelegate(ReceiveTouchInformation);
            connectEventHandler = new NWConnectEventDelegate(ConnectEventHandler);
            disconnectEventHandler = new NWDisconnectEventDelegate(DisconnectEventHandler);

            //Set the connect and disconnect event handlers.
            SetConnectEventHandler(connectEventHandler);
            SetDisconnectEventHandler(disconnectEventHandler);

            //Retrieve the number of currently connected devices.
            Int32 deviceCount = GetConnectedDeviceCount();

            myDeviceID = -1;

            if (deviceCount > 0)
            {
                //Just get the first touch screen reported.
                //This function returns the serial number of the touch screen which is used to uniquely 
                //identify the touch screen.
                myDeviceID = GetConnectedDeviceID(0);

                //Initialise the device and set up the touch event handler.
                //All multi-touch information will be received by the eventHandler function.
                OpenDevice(myDeviceID, eventHandler);

                //Now set the report mode to receive multi-touch information.
                //In this case we just want to receive MultiTouch information.
                //To combine both mouse and multi-touch we could use :
                //
                //          (int)ReportModes.RM_MULTITOUCH | (int)ReportModes.RM_MOUSE
                SetReportMode(myDeviceID, (int)ReportModes.RM_MULTITOUCH);

                //Set the screen dimensions. By default, these are set to the size of
                //the screen in pixels.
                int height = (int)SystemParameters.PrimaryScreenHeight;
                int width = (int)SystemParameters.PrimaryScreenWidth;
                SetTouchScreenDimensions(myDeviceID, 0, 0, width, height);

                //Turn the Kalman Filters on - they are turned off by default.
                //SetKalmanFilterStatus(myDeviceID, true);
            }
        }

        public override void Dispose()
        {
            //Cleanup the DLL resources on closing the application.

            //Close the event handlers.
            SetConnectEventHandler(null);
            SetDisconnectEventHandler(null);

            //Close the open device if necessary.
            if (myDeviceID >= 0)
            {
                CloseDevice(myDeviceID);
            }
        }

        private void ConnectEventHandler(Int32 deviceId)
        {
            //This will get called every time a touch screen device is plugged into the system.
            //string[] items = { deviceId.ToString() + " connected." };
        }

        private void DisconnectEventHandler(Int32 deviceId)
        {
            //This will get called every time a touch screen device is unplugged from the system.
            //string[] items = { deviceId.ToString() + " disconnected." };
        }

        //Event Handler that receives all the multi-touch information.
        private void ReceiveTouchInformation(Int32 deviceId, Int32 deviceStatus, Int32 packetID, Int32 touches, Int32 ghostTouches)
        {


            //Check if we have received touch information.
            if (deviceStatus == (int)DeviceStatus.DS_TOUCH_INFO)
            {
                currentPacketID = packetID;
                currentTouches = touches;
                currentGhostTouches = ghostTouches;

                //Loop through all the touches.
                for (int tch = 0; tch < MAX_TOUCHES; tch++)
                {
                    //If the bit is set then a touch with this ID exists.
                    if ((currentTouches & (1 << tch)) > 0)
                    {
                        //Get the touch information.
                        NWTouchPoint touchPt = new NWTouchPoint();
                        SuccessCode retCode = (SuccessCode)GetTouch(myDeviceID, currentPacketID, out touchPt, (1 << tch), 0);
                        if (retCode == SuccessCode.SUCCESS)
                        {
                            //Debug.WriteLine(touchPt.ConfidenceLevel);

                            // fill touch list for detection of "lost" touches
                            if ((TouchEventType)touchPt.TouchEventType == TouchEventType.TE_TOUCH_DOWN)
                            {
                                // check if all other touches are still valid
                                foreach (KeyValuePair<Int32, NWTouchPoint> touch in touchList)
                                {
                                    if (!((currentTouches & touch.Key) > 0))
                                    {
                                        // invalid!
                                        Debug.WriteLine("Invalid Touch: " + touch.Key);

                                        // resend up for this touch
                                        PointEventArgs resend = new PointEventArgs();
                                        resend.Handled = false;
                                        resend.DeviceUID = new DeviceUID() { DeviceID = touch.Value.TouchID, DeviceDriverID = this.DeviceDriverID };
                                        resend.PointEventType = PointEventType.LeftUp;
                                        resend.PointScreen = new PressurePoint(touch.Value.TouchPos.x, touch.Value.TouchPos.y);
                                        resend.DeviceType = DeviceType.MultiTouch;
                                        OnPointEvent(resend);

                                        // remove invalid from list
                                        touchList.Remove(touch.Value.TouchID);
                                        break;
                                    }
                                }
                                if (touchList.ContainsKey(touchPt.TouchID)) return; // ok .. if we got a second down event .. ignore it
                                touchList.Add(touchPt.TouchID, touchPt);
                            }
                            else if ((TouchEventType)touchPt.TouchEventType == TouchEventType.TE_TOUCH_UP)
                            {
                                touchList.Remove(touchPt.TouchID);
                            }

                            PointEventArgs pointEventArgs = new PointEventArgs();

                            pointEventArgs.Handled = false;
                            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = touchPt.TouchID, DeviceDriverID = this.DeviceDriverID };
                            pointEventArgs.PointEventType = convertTouchEventType((TouchEventType)touchPt.TouchEventType);
                            pointEventArgs.PointScreen = new PressurePoint(touchPt.TouchPos.x, touchPt.TouchPos.y);
                            pointEventArgs.DeviceType = DeviceType.MultiTouch;

                            if (pointEventArgs.PointEventType == PointEventType.LeftDown)
                            {
                                OnAddDeviceEvent(pointEventArgs.DeviceUID);
                            }

                            OnPointEvent(pointEventArgs);

                            if (pointEventArgs.PointEventType == PointEventType.LeftUp)
                            {
                                OnRemoveDeviceEvent(pointEventArgs.DeviceUID);
                            }
                        }
                    }
                }
            }
        }

        private PointEventType convertTouchEventType(TouchEventType touchEventType)
        {
            switch (touchEventType)
            {
                case TouchEventType.TE_TOUCH_DOWN:
                    return PointEventType.LeftDown;
                case TouchEventType.TE_TOUCH_UP:
                    return PointEventType.LeftUp;
                case TouchEventType.TE_TOUCHING:
                    return PointEventType.Drag;
                default:
                    return PointEventType.Unknown;
            }
        }
    }
}
