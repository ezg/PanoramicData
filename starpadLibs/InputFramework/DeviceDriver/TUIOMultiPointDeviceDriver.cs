using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TUIO; 

namespace InputFramework.DeviceDriver
{
    public class TUIOMultiPointDeviceDriver : MultiPointDeviceDriver, TuioListener
    {
        private TuioClient mClient;

        public TUIOMultiPointDeviceDriver(int port)
        {
            mClient = new TuioClient(port);
            mClient.addTuioListener(this);
            mClient.connect();
        }

        public override void Dispose()
        {
            mClient.removeTuioListener(this);
            mClient.disconnect();
        }

        public void addTuioCursor(TuioCursor tuioCursor)
        {
            MultiPointEventArgs multiPointEventArgs = new MultiPointEventArgs();

            multiPointEventArgs.DeviceUID = new DeviceUID() { DeviceID = 0, DeviceDriverID = this.DeviceDriverID };
            multiPointEventArgs.MultiPointEventType = MultiPointEventType.Down;
            multiPointEventArgs.PointScreen = getPointScreen(tuioCursor);
            multiPointEventArgs.PointID = tuioCursor.getSessionID();

            //Console.WriteLine(tuioCursor.getSessionID() + ", " + tuioCursor.getFingerID());

            OnMultiPointEvent(multiPointEventArgs);
        }

        public void removeTuioCursor(TuioCursor tuioCursor)
        {
            MultiPointEventArgs multiPointEventArgs = new MultiPointEventArgs();

            multiPointEventArgs.DeviceUID = new DeviceUID() { DeviceID = 0, DeviceDriverID = this.DeviceDriverID };
            multiPointEventArgs.MultiPointEventType = MultiPointEventType.Up;
            multiPointEventArgs.PointScreen = getPointScreen(tuioCursor);
            multiPointEventArgs.PointID = tuioCursor.getSessionID();

            OnMultiPointEvent(multiPointEventArgs);
        }

        public void updateTuioCursor(TuioCursor tuioCursor)
        {
            MultiPointEventArgs multiPointEventArgs = new MultiPointEventArgs();

            multiPointEventArgs.DeviceUID = new DeviceUID() { DeviceID = 0, DeviceDriverID = this.DeviceDriverID };
            multiPointEventArgs.MultiPointEventType = MultiPointEventType.Drag;
            multiPointEventArgs.PointScreen = getPointScreen(tuioCursor);
            multiPointEventArgs.PointID = tuioCursor.getSessionID();

            OnMultiPointEvent(multiPointEventArgs);
        }

        private PressurePoint getPointScreen(TuioCursor tuioCursor)
        {
            return new PressurePoint(tuioCursor.getScreenX((int)System.Windows.SystemParameters.PrimaryScreenWidth), tuioCursor.getScreenY((int)System.Windows.SystemParameters.PrimaryScreenHeight));
        }

        #region notimplemented

        public void refresh(long timestamp)
        {
        }

        // Marker handling not implemented
        public void addTuioObject(TuioObject tuioObject)
        {
        }

        public void removeTuioObject(TuioObject tuioObject)
        {
        }

        public void updateTuioObject(TuioObject tuioObject)
        {
        }

        #endregion
    }
}
