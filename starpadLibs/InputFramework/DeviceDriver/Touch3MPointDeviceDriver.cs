using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace InputFramework.DeviceDriver
{
    public class Touch3MPointDeviceDriver : PointDeviceDriver
    {
        private int interval = 10;
        /// <summary>
        /// Interval between drag events
        /// </summary>
        public int Interval
        {
            get { return interval; }
            set { interval = value; }
        }

        /// <summary>
        /// Represents 3M Touch. ID is Touch ID, X and Y are the coordinates of a touch, IsValid marks if
        /// a touch is still valid.
        /// </summary>
        private class Touch3M
        {
            private int id;
            public int ID
            {
                get { return id; }
                set { id = value; }
            }
            private bool isDown;
            public bool IsDown
            {
                get { return isDown; }
                set { isDown = value; }
            }
            private int x;
            public int X
            {
                get { return x; }
                set { x = value; }
            }
            private int y;
            public int Y
            {
                get { return y; }
                set { y = value; }
            }
            private bool isValid;
            public bool IsValid
            {
                get { return isValid; }
                set { isValid = value; }
            }

            public Touch3M(int id, bool down, int x, int y)
            {
                this.id = id;
                this.isDown = down;
                this.x = x;
                this.y = y;
                this.isValid = true;
            }

            public override bool Equals(object touch)
            {
                if (touch is Touch3M && this.id == ((Touch3M)touch).id) return true;
                else return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        /// <summary>
        /// Delegate for DLL library.
        /// </summary>
        /// <param name="id">Touch ID</param>
        /// <param name="down">True if Down, False if Up</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public delegate void CallBack(Int32 id, bool down, Int32 x, Int32 y);

        private DateTime lastTouch;
        private List<Touch3M> currentTouches = new List<Touch3M>();         // Current Touches from DLL
        private List<Touch3M> lastTouches = new List<Touch3M>();            // Last Touches from DLL, is needed to get Drag state of Touches

        #region DLL Import Functions

        /// <summary>
        /// Opens 3M Device.
        /// </summary>
        [DllImport("3MTouchLibrary.dll")]
        public static extern void OpenDevice();

        /// <summary>
        /// Closes 3M Device.
        /// </summary>
        [DllImport("3MTouchLibrary.dll")]
        public static extern void CloseDevice();

        /// <summary>
        /// Reads continuous touches from 3M Device. Notifies this object by given callback.
        /// </summary>
        [DllImport("3MTouchLibrary.dll")]
        public static extern void ReadTouches(CallBack x);

        #endregion

        private Thread workerThread;

        public Touch3MPointDeviceDriver()
        {
            OpenDevice();
            workerThread = new Thread(new ThreadStart(ReadTouchesFromDLL));
            workerThread.Start();
        }
        
        public override void Dispose()
        {
            try
            {
                workerThread.Abort();
            }
            catch (ThreadAbortException) { Debug.WriteLine("Touch3MDeviceDriver: Couldn't abort working thread."); }
            CloseDevice();
        }

        public void ReadTouchesFromDLL()
        {
            lastTouch = DateTime.Now;
            CallBack cb = new CallBack(ProcessTouches);
            ReadTouches(cb);
        }

        /// <summary>
        /// This method is called when the DLL delivers a new touch. The method stores all touches to a internal list.
        /// If all touches of a sequence are transfered, the DLL calls this method with (-1, true, -1, -1) - in that moment
        /// he processing of the touches gets started (Down, Drag, Up) and raises the input framework events.
        /// </summary>
        /// <param name="id">Touch ID</param>
        /// <param name="down">True if Down, False if Up</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public void ProcessTouches(Int32 id, bool down, Int32 x, Int32 y)
        {
            // Check state of touches DOWN, DRAG, UP
            if (id == -1 && x == -1 && y == -1)
            {
                TimeSpan timeSpan = DateTime.Now - lastTouch;

                foreach (Touch3M touch in currentTouches)
                {
                    PointEventArgs pointEventArgs = new PointEventArgs();
                    pointEventArgs.Handled = false;
                    pointEventArgs.DeviceType = DeviceType.MultiTouch;
                    pointEventArgs.DeviceUID = new DeviceUID() { DeviceDriverID = this.DeviceDriverID, DeviceID = (long)touch.ID };
                    pointEventArgs.PointScreen = new PressurePoint(touch.X, touch.Y, 1);

                    // Touch marked as Up
                    if (!touch.IsDown)
                    {
                        //Debug.WriteLine("Touch Up: " + touch.ID + " X:" + touch.X + " Y:" + touch.Y);
                        touch.IsValid = false;

                        pointEventArgs.PointEventType = PointEventType.LeftUp;
                        OnPointEvent(pointEventArgs);
                    }
                    // Touch marked as Down - determine wheter it is down or drag
                    else
                    {
                        if (lastTouches.Contains(touch))
                        {
                            //Debug.WriteLine("Touch Drag: " + touch.ID + " X:" + touch.X + " Y:" + touch.Y);

                            // Only notify in sample rate
                            if (timeSpan.TotalMilliseconds > Interval)
                            {
                                pointEventArgs.PointEventType = PointEventType.Drag;
                                OnPointEvent(pointEventArgs);
                                lastTouch = DateTime.Now;
                            }
                        }
                        else if (!lastTouches.Contains(touch))
                        {
                            //Debug.WriteLine("Touch Down: " + touch.ID + " X:" + touch.X + " Y:" + touch.Y);
                            
                            pointEventArgs.PointEventType = PointEventType.LeftDown;
                            OnPointEvent(pointEventArgs);
                        }
                    }
                }

                // Removes all invalid touches from list (up touches)
                currentTouches.RemoveAll(InvalidTouches);

                // stores current touches to last touches and creates a new current touches list
                lastTouches = currentTouches;
                currentTouches = new List<Touch3M>();
            }
            // New Touch
            else
            {
                // Store it to internal list
                currentTouches.Add(new Touch3M(id, down, x, y));
            }
        }

        /// <summary>
        /// Determines wheter a touch is valid or not.
        /// </summary>
        /// <param name="touch"></param>
        /// <returns></returns>
        private static bool InvalidTouches(Touch3M touch)
        {
            if (!touch.IsValid) return true;
            return false;
        }

    }
}
