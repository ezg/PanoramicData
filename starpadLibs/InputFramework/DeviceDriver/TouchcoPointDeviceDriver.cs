using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Touchco;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using mil.conoto;

namespace InputFramework.DeviceDriver {
    public class TouchcoPointDeviceDriver : PointDeviceDriver {

        private int interval = 10;
        /// <summary>
        /// Interval between contact polling in milliseconds
        /// </summary>
        public int Interval {
            get { return interval; }
            set { interval = value; }
        }

        private string configFilename = "";
        private Sensor sensor;
        private float sensorWidth;
        private float sensorHeight;
        private Thread workerThread;
        private Dictionary<DeviceUID, DateTime> lastTouches;
        private ConotoElementManager conotoElementManager = new ConotoElementManager();
        private ConotoConvert conotoConvert;

        public TouchcoPointDeviceDriver()
            : this("") {
        }

        public TouchcoPointDeviceDriver(string configFilename) {
            this.configFilename = configFilename;
            LoadConotoConfiguration(configFilename);
            conotoConvert = new ConotoConvert(conotoElementManager);

            lastTouches = new Dictionary<DeviceUID, DateTime>();

            sensor = OpenDevice();

            if (sensor != null) {
                workerThread = new Thread(new ThreadStart(ReadTouches));
                workerThread.Start();
            }
        }

        private Sensor OpenDevice() {
            Sensor sensor = null;
            UInt32 numDevs = Sensor.GetNumDevices();
            if (numDevs <= 0)
                return null;

            try {
                sensor = Sensor.OpenByIndex(0);
                sensorWidth = sensor.SensorInfo.width;
                sensorHeight = sensor.SensorInfo.height;
            } catch (Exception) { Debug.WriteLine("TouchcoPointDeviceDriver: Failed to open TouchCo sensor."); }

            return sensor;
        }

        private void ReadTouches() {
            while (true) {
                TCContactFrame contact;
                uint numFrames = sensor.GetNumFramesInQueue();
                for (uint i = 0; i < numFrames; i++) {
                    contact = sensor.GetQueuedContactFrame();

                    // for each contact
                    for (uint j = 0; j < contact.numContacts; j++) {
                        TCContact currentContact = contact.contacts[j];

                        PointEventArgs pointEventArgs = new PointEventArgs();
                        pointEventArgs.Handled = false;
                        pointEventArgs.DeviceType = DeviceType.MultiTouch;
                        pointEventArgs.DeviceUID = new DeviceUID() { DeviceDriverID = this.DeviceDriverID, DeviceID = (long)currentContact.uid };

                        if (configFilename != "") {
                            double x, y;
                            long functionID; string function;
                            conotoConvert.Convert("", currentContact.x, currentContact.y, out function, out functionID, out x, out y);
                            pointEventArgs.PointScreen = new PressurePoint(x, y, currentContact.force);

                        } else {
                            pointEventArgs.PointScreen = new PressurePoint(currentContact.x, currentContact.y, currentContact.force);

                        }
                        pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
                        pointEventArgs.InBetweenPointsScreen[0] = pointEventArgs.PointScreen;
                        
                        // OPTIONAL:
                        pointEventArgs.Args = currentContact;

                        switch (currentContact.eventType) {
                            case Event.START:
                                pointEventArgs.PointEventType = PointEventType.LeftDown;
                                OnPointEvent(pointEventArgs);
                                break;

                            case Event.UPDATE:
                                pointEventArgs.PointEventType = PointEventType.Drag;

                                // Calculate Time Span to last touch
                                TimeSpan timeSpan;
                                if (lastTouches.ContainsKey(pointEventArgs.DeviceUID))
                                    timeSpan = DateTime.Now - lastTouches[pointEventArgs.DeviceUID];
                                else
                                    timeSpan = new TimeSpan(1, 0, 0, 0);

                                // Only send touch if Time Span is greater than the polling interval
                                if (timeSpan.TotalMilliseconds > Interval) {
                                    OnPointEvent(pointEventArgs);

                                    if (!lastTouches.ContainsKey(pointEventArgs.DeviceUID))
                                        lastTouches.Add(pointEventArgs.DeviceUID, DateTime.Now);
                                    else
                                        lastTouches[pointEventArgs.DeviceUID] = DateTime.Now;
                                }
                                break;

                            case Event.END:
                            case Event.NONE:
                                pointEventArgs.PointEventType = PointEventType.LeftUp;
                                OnPointEvent(pointEventArgs);

                                if (lastTouches.ContainsKey(pointEventArgs.DeviceUID))
                                    lastTouches.Remove(pointEventArgs.DeviceUID);

                                break;
                        }
                    }
                }
            }
        }

        public void LoadConotoConfiguration(string config) {
            conotoElementManager.LoadConfigFile(config);
        }

        public override void Dispose() {
            try {
                workerThread.Abort();
            } catch (ThreadAbortException) { Debug.WriteLine("TouchcoPointDeviceDriver: Couldn't abort working thread."); }
            sensor.Close();
            sensor = null;
        }
    }
}
