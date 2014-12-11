using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Touchco;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using mil.conoto;
using mil.AnotoPen;
using System.Collections;
using System.IO;
using InputFramework;

namespace InputFramework.DeviceDriver {

    public class AnotoTouchcoPointDeviceDriver : PointDeviceDriver {

        private int penTouchSeparationInterval = 60;
        /// <summary>
        /// defines the interval a Touchco contact waits for a pen down 
        /// before it sends its own down event
        /// </summary>
        public int PenTouchSeparationInterval {
            get { return penTouchSeparationInterval; }
            set { penTouchSeparationInterval = value; }
        }

        private int penTouchSeparationDistanceThreshold = 5;
        /// <summary>
        /// Defines the minimum pixel distance to separate touch and pen events
        /// </summary>
        public int PenTouchSeparationDistanceThreshold {
            get { return penTouchSeparationDistanceThreshold; }
            set { penTouchSeparationDistanceThreshold = value; }
        }
                
        private int interval = 10;
        /// <summary>
        /// Interval between contact polling in milliseconds
        /// </summary>
        public int Interval {
            get { return interval; }
            set { interval = value; }
        }

        // global members
        private string touchcoConfigFilename = "";                                          // path of Touchco configuration file
        private string anotoConfigFilename = "";                                            // path of Anoto configuration file

        // Touchco members
        private Sensor sensor;                                                              // TouchCo sensor
        private float sensorWidth;                                                          // Resolution of foil
        private float sensorHeight;                                                         // Resolution of foil
        private Thread workerThread;                                                        // Touch reading thread
        private Dictionary<DeviceUID, DateTime> lastTouches;                                // dictionary with last touches and their timestamp
        private ConotoElementManager touchcoConotoElementManager;                           // Conoto Element Manager - data container for configuration data
        private ConotoConvert touchcoConotoConvert;                                         // Conoto Converter for transforming touch positions to screen coordinates

        // Anoto members
        public delegate void ConotoFunctionRaisedHandler(Object sender, DeviceUID deviceUID, long pageID, string function, long functionID, double x, double y, float intensity, PointEventType type);
        public event ConotoFunctionRaisedHandler ConotoFunctionRaised;
        protected AnotoStreamingServer anotoStreamingServer;                                // Anoto Streaming Server
        private ConotoElementManager anotoConotoElementManager;                             // Conoto Element Manager - data container for configuration data
        private ConotoConvert anotoConotoConvert;                                           // Conoto Converter for transforming anoto data to screen coordinates

        // pen/touch separation members
        Dictionary<DeviceUID, PenTouchSeparationTouchData> separationProcessingList;        // separation processing list
        List<DeviceUID> ignoreList;                                                         // ignore list - PointEvents of that DeviceUID will not be fired 

        /// <summary>
        /// Constructor; Initializes TouchCo without configuration file. Touch positions will be the positions from the foil.
        /// </summary>
        public AnotoTouchcoPointDeviceDriver(string anotoConfigFilename)
            : this("", anotoConfigFilename) {
        }

        /// <summary>
        /// Constructor; Initializes TouchCo with given Conoto configuration file.
        /// </summary>
        public AnotoTouchcoPointDeviceDriver(string touchcoConfigFilename, string anotoConfigFilename) {
            separationProcessingList = new Dictionary<DeviceUID, PenTouchSeparationTouchData>();
            ignoreList = new List<DeviceUID>();

            // inits Touchco
            this.touchcoConfigFilename = touchcoConfigFilename;
            InitTouchco();

            // inits Anoto
            this.anotoConfigFilename = anotoConfigFilename;
            InitAnoto();
        }

        /// <summary>
        /// Returns true when the interval between the last OnPointEvent of this DeviceUID is greater than the defined Interval.
        /// </summary>
        /// <param name="deviceUID"></param>
        /// <returns></returns>
        private bool IntervalElapsed(DeviceUID deviceUID) {
            TimeSpan timeSpan;
            if (lastTouches.ContainsKey(deviceUID))
                timeSpan = DateTime.Now - lastTouches[deviceUID];
            else
                timeSpan = new TimeSpan(1, 0, 0, 0);

            if (timeSpan.TotalMilliseconds < Interval) return false;
            else return true;
        }

        #region Pen/Touch separation

        /// <summary>
        /// Does the pen and touch separation before a PointEvent gets fired.
        /// </summary>
        /// <param name="pointEventArgs"></param>
        public void PreviewOnPointEvent(PointEventArgs pointEventArgs) {

            try {
                if (pointEventArgs.DeviceType == DeviceType.MultiTouch) {
                    // pen/touch separation - multitouch separation
                    if (MultitouchSeparation(pointEventArgs)) return;

                    // only sends touch if time span is greater than the polling interval
                    if (pointEventArgs.PointEventType == PointEventType.Drag || pointEventArgs.PointEventType == PointEventType.Move) {
                        if (IntervalElapsed(pointEventArgs.DeviceUID)) {
                            if (!lastTouches.ContainsKey(pointEventArgs.DeviceUID))
                                lastTouches.Add(pointEventArgs.DeviceUID, DateTime.Now);
                            else
                                lastTouches[pointEventArgs.DeviceUID] = DateTime.Now;
                        } else return;
                    }

                } else if (pointEventArgs.DeviceType == DeviceType.Stylus) {
                    // pen/touch separation - pen part
                    PenSeparation(pointEventArgs);
                }
            } catch (Exception) {
                Debug.WriteLine("AnotoTouchcoPointDeviceDriver: Error while pen/touch separation processing.");
            }

            // only fires a event if device uid isn't ignored by pen/touch separation
            if(!ignoreList.Contains(pointEventArgs.DeviceUID))
                OnPointEvent(pointEventArgs);
        }

        /// <summary>
        /// Pen/Touch Separation - Multitouch part.
        /// Each touch will wait a defined interval (PenTouchSeparationInterval) before it sends its down event. In this interval
        /// all PointEvents get stored into a internal processing list, which is used by the PenSeparation() method to decide wheter
        /// a touch is a pen or not.
        /// </summary>
        /// <param name="pointEventArgs"></param>
        private bool MultitouchSeparation(PointEventArgs pointEventArgs) {

            // Touch Down Event
            if (pointEventArgs.PointEventType == PointEventType.LeftDown) {
                // puts the touch onto ignore list and stores it into internal separation processing list
                ignoreList.Add(pointEventArgs.DeviceUID);
                separationProcessingList.Add(pointEventArgs.DeviceUID, new PenTouchSeparationTouchData(pointEventArgs));
                return true;
            } 
            // Touch Drag Event
            else if (pointEventArgs.PointEventType == PointEventType.Move || pointEventArgs.PointEventType == PointEventType.Drag) {
                if (separationProcessingList.ContainsKey(pointEventArgs.DeviceUID)) {
                    // gets corresponding separation data and down point event
                    PenTouchSeparationTouchData touchData = separationProcessingList[pointEventArgs.DeviceUID];
                    TimePointEvent downPointEvent = touchData.Touches[0];

                    // wait interval not elapsed - stores event into internal separation processing list
                    if (!touchData.IsPen.HasValue &&
                        (DateTime.Now - downPointEvent.Timestamp).Milliseconds < PenTouchSeparationInterval) {
                        touchData.AddTouch(pointEventArgs);
                        return true;
                    } 
                    // touch is pen - puts touch onto ignore list and removes it from the internal separation processing list
                    else if (touchData.IsPen.HasValue && touchData.IsPen.Value) {
                        if (!ignoreList.Contains(pointEventArgs.DeviceUID))
                            ignoreList.Add(pointEventArgs.DeviceUID);
                        if (separationProcessingList.ContainsKey(pointEventArgs.DeviceUID))
                            separationProcessingList.Remove(pointEventArgs.DeviceUID);
                        return true;
                    }

                    // in all other cases: fire all stored point events
                    foreach (TimePointEvent pointEvent in touchData.Touches) {
                        OnPointEvent(pointEvent.PointEventArgs);
                    }
                    OnPointEvent(pointEventArgs);
                    if (ignoreList.Contains(pointEventArgs.DeviceUID))
                        ignoreList.Remove(pointEventArgs.DeviceUID);
                    if (separationProcessingList.ContainsKey(pointEventArgs.DeviceUID))
                        separationProcessingList.Remove(pointEventArgs.DeviceUID);
                    return true;
                }
            } 
            // Touch Up Event
            else if (pointEventArgs.PointEventType == PointEventType.LeftUp) {
                if (separationProcessingList.ContainsKey(pointEventArgs.DeviceUID)) {
                    // gets corresponding separation data
                    PenTouchSeparationTouchData touchData = separationProcessingList[pointEventArgs.DeviceUID];

                    // touch is pen - ignores touch, clears internal lists and returns
                    if (touchData.IsPen.HasValue && touchData.IsPen.Value) {
                        if (ignoreList.Contains(pointEventArgs.DeviceUID))
                            ignoreList.Remove(pointEventArgs.DeviceUID);
                        if (separationProcessingList.ContainsKey(pointEventArgs.DeviceUID))
                            separationProcessingList.Remove(pointEventArgs.DeviceUID);
                        return true;
                    }

                    // in all other cases: fire all stored point events
                    foreach (TimePointEvent pointEvent in touchData.Touches) {
                        OnPointEvent(pointEvent.PointEventArgs);
                    }
                    OnPointEvent(pointEventArgs);
                    if (ignoreList.Contains(pointEventArgs.DeviceUID))
                        ignoreList.Remove(pointEventArgs.DeviceUID);
                    if (separationProcessingList.ContainsKey(pointEventArgs.DeviceUID))
                        separationProcessingList.Remove(pointEventArgs.DeviceUID);
                    return true;
                }
                if(ignoreList.Contains(pointEventArgs.DeviceUID)) {
                    ignoreList.Remove(pointEventArgs.DeviceUID);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Pen/Touch Separation - Pen part.
        /// If a Pen PointDown gets fired, this method calculates the minimal distance to all touches in the
        /// internal processing list and decides wheter a touch is a pen or not.
        /// </summary>
        /// <param name="pointEventArgs"></param>
        private void PenSeparation(PointEventArgs pointEventArgs) {
            if (pointEventArgs.PointEventType == PointEventType.LeftDown) {

                // calculates the minimal distance for each touch in internal processing list 
                for (int i = 0; i < separationProcessingList.Count; i++) {
                    DeviceUID currentUID = separationProcessingList.ElementAt(i).Key;
                    PenTouchSeparationTouchData currentTouchData = separationProcessingList.ElementAt(i).Value;

                    // iterates over all stored point events of the current touch and calculates
                    // the minimal distance to the current touch - if the distance is smaller than than
                    // the defined SeparationDistanceThreshold the touch gets marked as pen
                    double shortestDistance = PenTouchSeparationDistanceThreshold;
                    foreach (TimePointEvent pointEvent in currentTouchData.Touches) {
                        if (pointEvent.GetDistanceToPoint(pointEventArgs) < shortestDistance)
                            shortestDistance = pointEvent.GetDistanceToPoint(pointEventArgs);
                    }

                    // IsPen decision
                    if (shortestDistance >= PenTouchSeparationDistanceThreshold)
                        currentTouchData.IsPen = false;
                    else
                        currentTouchData.IsPen = true;

                }
            }
        }

        #endregion

        #region Touchco input handling

        /// <summary>
        /// Opens Touchco device and starts reader thread.
        /// </summary>
        private void InitTouchco() {
            lastTouches = new Dictionary<DeviceUID, DateTime>();

            try {
                touchcoConotoElementManager = new ConotoElementManager();
                touchcoConotoElementManager.LoadConfigFile(touchcoConfigFilename);
                touchcoConotoConvert = new ConotoConvert(touchcoConotoElementManager);
            } catch(FileNotFoundException) {
                Debug.WriteLine("AnotoTouchcoPointDeviceDriver: Touchco configuration file doesn't exists.");
            } catch(Exception) {
                Debug.WriteLine("AnotoTouchcoPointDeviceDriver: Error while loading Touchco configuration file.");
            }

            try {
                sensor = OpenSensor();
            } catch (Exception) {
                Debug.WriteLine("AnotoTouchcoPointDeviceDriver: Error while opening Touchco sensor.");
            }

            if (sensor != null) {
                workerThread = new Thread(new ThreadStart(ReadTouchcoTouches));
                workerThread.Start();
            }
        }

        /// <summary>
        /// Opens first TouchCo device.
        /// </summary>
        /// <returns>Sensor or null if there is no device</returns>
        private Sensor OpenSensor() {
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

        /// <summary>
        /// Reads contacts from the TouchCo DLL and fires Point Events.
        /// </summary>
        private void ReadTouchcoTouches() {
            while (true) {
                TCContactFrame contactFrame;
                uint numFrames = sensor.GetNumFramesInQueue();

                // for each frame in queue
                for (uint i = 0; i < numFrames; i++) {
                    contactFrame = sensor.GetQueuedContactFrame();

                    // for each contact in queue
                    for (uint j = 0; j < contactFrame.numContacts; j++) {
                        TCContact currentContact = contactFrame.contacts[j];

                        PointEventArgs pointEventArgs = new PointEventArgs();
                        pointEventArgs.DeviceType = DeviceType.MultiTouch;

                        // device UID
                        pointEventArgs.DeviceUID = new DeviceUID() { DeviceDriverID = this.DeviceDriverID, DeviceID = (long)currentContact.uid };
                        pointEventArgs.Handled = false;

                        // transforms points to screen space or forwards foil positions if there is no configuration file
                        if (touchcoConfigFilename != "") {
                            double x, y;
                            long functionID; string function;
                            touchcoConotoConvert.Convert(sensor.SensorInfo.serialNumber, currentContact.x, currentContact.y, out function, out functionID, out x, out y);
                            pointEventArgs.PointScreen = new PressurePoint(x, y, currentContact.force);
                        } else
                            pointEventArgs.PointScreen = new PressurePoint(currentContact.x, currentContact.y, currentContact.force);

                        
                        // OPTIONAL (allows the application to read contact information)
                        pointEventArgs.Args = currentContact;

                        // defines event type and fires point events
                        switch (currentContact.eventType) {
                            case Event.START:
                                pointEventArgs.PointEventType = PointEventType.LeftDown;
                                PreviewOnPointEvent(pointEventArgs);
                                break;

                            case Event.UPDATE:
                                pointEventArgs.PointEventType = PointEventType.Drag;
      
                                // calculates time span since last touch
                                TimeSpan timeSpan;
                                if (lastTouches.ContainsKey(pointEventArgs.DeviceUID))
                                    timeSpan = DateTime.Now - lastTouches[pointEventArgs.DeviceUID];
                                else
                                    timeSpan = new TimeSpan(1, 0, 0, 0);

                                // only sends touch if time span is greater than the polling interval
                                if (timeSpan.TotalMilliseconds > Interval) {
                                    PreviewOnPointEvent(pointEventArgs);

                                    if (!lastTouches.ContainsKey(pointEventArgs.DeviceUID))
                                        lastTouches.Add(pointEventArgs.DeviceUID, DateTime.Now);
                                    else
                                        lastTouches[pointEventArgs.DeviceUID] = DateTime.Now;
                                }
                                break;

                            case Event.END:
                            case Event.NONE:
                                pointEventArgs.PointEventType = PointEventType.LeftUp;
                                PreviewOnPointEvent(pointEventArgs);
                                if (lastTouches.ContainsKey(pointEventArgs.DeviceUID))
                                    lastTouches.Remove(pointEventArgs.DeviceUID);

                                break;
                        }
                    }
                }
            }
        }

        #endregion

        #region Anoto input handling

        private void InitAnoto() {

            try {
                anotoConotoElementManager = new ConotoElementManager();
                anotoConotoElementManager.LoadConfigFile(anotoConfigFilename);
                anotoConotoConvert = new ConotoConvert(anotoConotoElementManager);
            } catch (FileNotFoundException) {
                Debug.WriteLine("AnotoTouchcoPointDeviceDriver: Anoto configuration file doesn't exists.");
            } catch (Exception) {
                Debug.WriteLine("AnotoTouchcoPointDeviceDriver: Error while loading Anoto configuration file.");
            }

            try {
                mil.AnotoPen.AnotoPen.Initialize("71ad1f27386ec4f5d91e15eca16f46c4");
                anotoStreamingServer = new AnotoStreamingServer();
                anotoStreamingServer.IsFixingPatternJump = true;
                anotoStreamingServer.OnStroke += new AnotoStreamingServer.AnotoEventHandler(AnotoServer_OnStroke);
                anotoStreamingServer.OnPenConnect += new AnotoStreamingServer.AnotoEventHandler(AnotoServer_OnPenConnect);
                anotoStreamingServer.Start();
            } catch (Exception) {
                Debug.WriteLine("AnotoTouchcoPointDeviceDriver: Error while starting Anoto streaming server.");
            }
        }

        protected void AnotoServer_OnPenConnect(object sender, AnotoPenEvent args) {
            OnAddDeviceEvent(new DeviceUID() { DeviceID = args.PenId, DeviceDriverID = this.DeviceDriverID });
        }

        protected void AnotoServer_OnStroke(object sender, AnotoPenEvent args) {
            string function;
            long functionID;
            double x, y;
            float intensity;

            intensity = args.Intensity;
            intensity = 1 - (intensity / 255);
            if (intensity > 1.0)
                intensity = 1.0f;
            if (intensity < 0.0)
                intensity = 0.05f;

            anotoConotoConvert.Convert((ulong)args.PageId, args.X, args.Y, out function, out functionID, out x, out y);

            if (function == "0") {
                PointEventArgs pointEventArgs = new PointEventArgs();

                pointEventArgs.DeviceType = DeviceType.Stylus;
                pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = args.PenId, DeviceDriverID = this.DeviceDriverID };
                pointEventArgs.Handled = false;
                switch (args.Type) {
                    case AnotoPenEventType.StrokeStart:
                        pointEventArgs.PointEventType = PointEventType.LeftDown;
                        break;
                    case AnotoPenEventType.StrokeDrag:
                        pointEventArgs.PointEventType = PointEventType.Drag;
                        break;
                    case AnotoPenEventType.StrokeEnd:
                        pointEventArgs.PointEventType = PointEventType.LeftUp;
                        break;
                }

                pointEventArgs.PointScreen = new PressurePoint(x, y, intensity);

                pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
                pointEventArgs.InBetweenPointsScreen[0] = pointEventArgs.PointScreen;

                PreviewOnPointEvent(pointEventArgs);
            } else {
                if (ConotoFunctionRaised != null) {
                    PointEventType type = PointEventType.Unknown;
                    switch (args.Type) {
                        case AnotoPenEventType.StrokeStart: type = PointEventType.LeftDown; break;
                        case AnotoPenEventType.StrokeDrag: type = PointEventType.Drag; break;
                        case AnotoPenEventType.StrokeEnd: type = PointEventType.LeftUp; break;
                    }

                    ConotoFunctionRaised(this, new DeviceUID() { DeviceDriverID = this.DeviceDriverID, DeviceID = args.PenId }, args.PageId, function, functionID, x, y, intensity, type);
                }
            }
        }

        #endregion

        /// <summary>
        /// Aborts the touch processing and closes the sensor.
        /// </summary>
        public override void Dispose() {
            // close Touchco
            try {
                if(workerThread != null)
                    workerThread.Abort();
            } catch (ThreadAbortException) { Debug.WriteLine("TouchcoPointDeviceDriver: Couldn't abort working thread."); }
            if (sensor != null) {
                sensor.Close();
                sensor = null;
            }

            // close Anoto
            if (anotoStreamingServer != null) {
                anotoStreamingServer.Stop();
                anotoStreamingServer.Dispose();
            }
            mil.AnotoPen.AnotoPen.Dispose();
        }
    }

    #region Temporary Pen/Touch separation classes
    public class TimePointEvent {
        private DateTime timestamp;
        public DateTime Timestamp {
            get { return timestamp; }
        }

        private PointEventArgs pointEventArgs;
        public PointEventArgs PointEventArgs {
            get { return pointEventArgs; }
        }

        public TimePointEvent(DateTime timestamp, PointEventArgs pointEventArgs) {
            this.timestamp = timestamp;
            this.pointEventArgs = pointEventArgs;
        }

        public double GetDistanceToPoint(PointEventArgs p) {
            Vector vec = new Vector(pointEventArgs.PointScreen.X - p.PointScreen.X, pointEventArgs.PointScreen.Y - p.PointScreen.Y);
            return vec.Length;
        }
    }

    public class PenTouchSeparationTouchData {

        bool? isPen;
        public bool? IsPen {
            set { isPen = value; }
            get { return isPen; }
        }

        List<TimePointEvent> touches = new List<TimePointEvent>();
        public List<TimePointEvent> Touches {
            get { return touches; }
        }

        public PenTouchSeparationTouchData(PointEventArgs downPointEventArgs) {
            AddTouch(downPointEventArgs);
        }

        public void AddTouch(PointEventArgs p) {
            touches.Add(new TimePointEvent(DateTime.Now, p));
        }
    }
    #endregion
}
