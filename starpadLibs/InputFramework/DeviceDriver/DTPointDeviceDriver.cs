using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using AxDIAMONDTOUCHLib;
using System.Windows.Forms.Integration;
using System.Windows;

namespace InputFramework.DeviceDriver
{
    public enum DTParameter
    {
        SINGLE_FINGER,
        TWO_FINGERS,
        MANY_FINGERS,
        TOUCH_FIST,
        TOUCH_WHOLEHAND
    }

    /// <summary>
    /// Class DTDeviceDriver.cs
    /// Author: Christian Rendl, Peter Brandl
    /// Creation Date: 11. November 2008
    /// Last Updated Date: 15. December 2008
    /// </summary>
    public class DTPointDeviceDriver : PointDeviceDriver, IDisposable
    {
        private static int K_FINGER_WIDTH_IN_PIXELS = -1;   // Size of finger touches, gets calculated if -1
        private static float DURATION_MAX = 25.0f;          // Duration which is Intensity = 1.0

        private bool diamondTouchStarted = false;           // Flag, if DiamondTouch is started
        private WindowsFormsHost host;                      // Host which contains OCX
        private AxDiamondTouch diamondTouchDevice;          // DiamondTouch Instance
        private int[] duration = new int[4];                // Duration of Touches

        System.Resources.ResourceManager resources =
            new System.Resources.ResourceManager(typeof(DTPointDeviceDriver));

        /// <summary>
        /// Constructor. Initializes and starts Diamond Touch.
        /// </summary>
        public DTPointDeviceDriver()
        {
            // Initializing Diamond Touch OCX
            this.diamondTouchDevice = new AxDiamondTouch();
            ((System.ComponentModel.ISupportInitialize)(this.diamondTouchDevice)).BeginInit();
            this.diamondTouchDevice.Enabled = true;
            this.diamondTouchDevice.Name = "diamondTouchDevice";
            this.diamondTouchDevice.Visible = false;
            this.diamondTouchDevice.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("diamondTouchDevice.OcxState")));

            this.diamondTouchDevice.Touch += new _DDiamondTouchEvents_TouchEventHandler(this.DiamondTouchDevice_Touch);
            ((System.ComponentModel.ISupportInitialize)(this.diamondTouchDevice)).EndInit();

            host = new WindowsFormsHost();
            host.Child = diamondTouchDevice;

            diamondTouchStarted = true;

            StartTouchTable();

            this.diamondTouchDevice.EventSegmentEnable = true;
            this.diamondTouchDevice.EventSignalEnable = true;

            PointEventArgs args = new PointEventArgs();
            OnPointEvent(args);
        }

        /// <summary>
        /// Start Diamond Touch Interaction.
        /// </summary>
        private void StartTouchTable()
        {
            int res;
            String str;

            res = diamondTouchDevice.Start(0);
            if (res != 0)
            {
                switch (res)
                {
                    case 1:
                        str = "Diamond Touch: Error starting DiamondTouch: already started"; break;
                    case 2:
                        str = "Diamond Touch: Error starting DiamondTouch: no device (couldn't find a USB DiamondTouch device)"; break;
                    case 3:
                        str = "Diamond Touch: Error starting DiamondTouch: open failed"; break;
                    case 4:
                        str = "Diamond Touch: Error starting DiamondTouch: serial device (not supported)"; break;
                    case 5:
                        str = "Diamond Touch: Error starting DiamondTouch: thread start failed (couldn't start up a thread for some reason)"; break;
                    default:
                        str = "Diamond Touch: Error starting DiamondTouch: unknown error"; break;
                }

                Console.WriteLine(str);
                MessageBox.Show(str);
            }
        }

        /// <summary>
        /// Handles a Touch. Fires OnPointEvent with PointEventArgs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DiamondTouchDevice_Touch(object sender, _DDiamondTouchEvents_TouchEvent args)
        {
            int receiverID = args.receiverId;
            float intensity;

            // Point Event Device
            PointEventArgs pointEventArgs = new PointEventArgs();
            pointEventArgs.Handled = false;
            pointEventArgs.DeviceType = DeviceType.MultiTouch;
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = args.receiverId, DeviceDriverID = DeviceDriverID };

            // Duration
            duration[receiverID]++;
            intensity = GetIntensity(receiverID);

            // Point Event Type
            switch (args.eventType)
            {
                case 1:
                    pointEventArgs.PointEventType = PointEventType.LeftDown;
                    break;
                case 2:
                    pointEventArgs.PointEventType = PointEventType.Drag;
                    break;
                case 3:
                    duration[receiverID] = 0;
                    pointEventArgs.PointEventType = PointEventType.LeftUp;
                    break;
                default:
                    pointEventArgs.PointEventType = PointEventType.Unknown;
                    break;
            }

            // Point Event Point Screen
            pointEventArgs.PointScreen = new PressurePoint(args.x, args.y, intensity);
            
            /*// Point Event Args
            object param = new Rect(new Point(args.ulx, args.uly), new Point(args.lrx, args.lry));
            pointEventArgs.Args = param;*/

            // Point Event Args [0] Touch type, [1] BoundingBox of touch
            object[] param = new object[2];
            DTParameter touchType = GetTouchType(args);
            param[0] = touchType;
            param[1] = new Rect(args.left, args.top, args.right - args.left, args.bottom - args.top);
            pointEventArgs.Args = param;

            // Notify to fire OnPointEvent
            OnPointEvent(pointEventArgs);
        }

        /// <summary>
        /// Calculates touch intensity.
        /// </summary>
        /// <param name="receiverID"></param>
        /// <returns>Intensity between 0.0 and 1.0</returns>
        private float GetIntensity(int receiverID)
        {
            float intensity = 0.0f;
            if (duration[receiverID] != 0)
                intensity = duration[receiverID] / DURATION_MAX;
            if (intensity > 1) intensity = 1;
            return intensity;
        }
        
        /// <summary>
        /// This method recognizes which type of touch occured.
        /// </summary>
        /// <param name="touch"></param>
        /// <returns>Touch type</returns>
        private DTParameter GetTouchType(_DDiamondTouchEvents_TouchEvent touch)
        {
            int segmentsX = touch.xSegmentCount;
            int segmentsY = touch.ySegmentCount;
            bool allSegsFingers = AreAllSegmentsFingers(touch);

            if (allSegsFingers && segmentsX == 1 && segmentsY == 1)
            {
                return DTParameter.SINGLE_FINGER;
            }
            else if (allSegsFingers && segmentsX <= 2 && segmentsY <= 2)
            {
                return DTParameter.TWO_FINGERS;
            }
            else if (GetDensity(touch) > .6)
            {
                return DTParameter.TOUCH_WHOLEHAND;
            }
            else if (segmentsX > 2 && segmentsY > 2)
            {
                return DTParameter.MANY_FINGERS;
            }
            else
            {
                return DTParameter.TOUCH_FIST;
            }
        }

        /// <summary>
        /// This method looks at an input event from the DiamondTouch table and
        /// checks to see if all points of contact between the hand and the table are
        /// finger sized.
        /// This method is used to help recognize TOUCH types.
        /// </summary>
        /// <param name="touch"></param>
        /// <returns>True if all segments are fingers, False if not</returns>
        private static bool AreAllSegmentsFingers(_DDiamondTouchEvents_TouchEvent touch)
        {
            int finger_width = K_FINGER_WIDTH_IN_PIXELS;

            // if the value for the constant K_FINGER_WIDTH_IN_PIXELS has not been set, then calculate the width of a single finger in pixels for this DiamondTouch and projector resolution
            if (K_FINGER_WIDTH_IN_PIXELS == -1)
            {
                K_FINGER_WIDTH_IN_PIXELS =
                        (int)(7d * 1024 /
                               touch.xSegmentString.Length);
                Console.Error.WriteLine("TODO: get the screen width from the system and do not use 1024 constant!");
            }

            // loop through the x touch segments and check the width of each one to see if it is larger than a finger's width
            for (int i = 0; i < touch.xSegmentCount; ++i)
            {
                int CurrentSegmentStart = (int)touch.xSegmentString[3 * i];
                int CurrentSegmentStop = (int)touch.xSegmentString[3 * i + 1];
                int CurrentSegmentMax = (int)touch.xSegmentString[3 * i + 2];
                if (CurrentSegmentStop -
                    CurrentSegmentStart >
                    K_FINGER_WIDTH_IN_PIXELS)
                {
                    return false; // this touch segment is larger than a finger, so return false
                }
            }

            // loop through the y touch segments and check the width of each one to see if it is larger than a finger's width
            for (int i = 0; i < touch.ySegmentCount; ++i)
            {
                int CurrentSegmentStart = (int)touch.ySegmentString[3 * i];
                int CurrentSegmentStop = (int)touch.ySegmentString[3 * i + 1];
                int CurrentSegmentMax = (int)touch.ySegmentString[3 * i + 2];
                if (CurrentSegmentStop -
                    CurrentSegmentStart >
                    K_FINGER_WIDTH_IN_PIXELS)
                {
                    return false; // this touch segment is larger than a finger, so return false
                }
            }

            // none of the touch segments are larger than a finger, so we return true
            return true;
        }

        private double GetDensity(_DDiamondTouchEvents_TouchEvent touch)
        {
            int minX = 999;
            int maxX = -999;
            int minY = 999;
            int maxY = -999;
            double totalWidthX = 0;
            double totalWidthY = 0;

            for (int i = 0; i < touch.xSegmentCount; ++i)
            {
                int CurrentSegmentStart = (int)touch.xSegmentString[3 * i];
                int CurrentSegmentStop = (int)touch.xSegmentString[3 * i + 1];
                totalWidthX += Math.Abs(CurrentSegmentStop - CurrentSegmentStart);
                if (CurrentSegmentStart < minX) minX = CurrentSegmentStart;
                if (CurrentSegmentStop > maxX) maxX = CurrentSegmentStop;
            }

            for (int i = 0; i < touch.ySegmentCount; ++i)
            {
                int CurrentSegmentStart = (int)touch.ySegmentString[3 * i];
                int CurrentSegmentStop = (int)touch.ySegmentString[3 * i + 1];
                totalWidthY += Math.Abs(CurrentSegmentStop - CurrentSegmentStart);
                if (CurrentSegmentStart < minY) minY = CurrentSegmentStart;
                if (CurrentSegmentStop > maxY) maxY = CurrentSegmentStop;
            }

            return (totalWidthX + totalWidthY) / ((maxX - minX) + (maxY - minY));
        }

        /// <summary>
        /// Disposes Object and stops Diamond Touch.
        /// </summary>
        public override void Dispose()
        {
            if (diamondTouchStarted)
            {
                try {
                    host.Dispose();
                } catch (Exception e) { Console.WriteLine(e.Message); }
                try {
                    diamondTouchDevice.Stop();
                } catch (Exception e) { Console.WriteLine(e.Message); }
                diamondTouchStarted = false;
            }
        }
    }
}
