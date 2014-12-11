using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TUIO; 

namespace InputFramework.DeviceDriver
{
    public class TUIOPointDeviceDriver : PointDeviceDriver, TuioListener
    {
        private TuioClient mClient;

        public TUIOPointDeviceDriver(int port)
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
            PointEventArgs pointEventArgs = new PointEventArgs();

            pointEventArgs.Handled = false;
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = tuioCursor.getSessionID(), DeviceDriverID = this.DeviceDriverID };
            pointEventArgs.PointEventType = PointEventType.LeftDown;
            pointEventArgs.PointScreen = getPointScreen(tuioCursor);
            pointEventArgs.DeviceType = DeviceType.MultiTouch;

            OnAddDeviceEvent(pointEventArgs.DeviceUID);
            OnPointEvent(pointEventArgs);
        }

        public void removeTuioCursor(TuioCursor tuioCursor)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();

            pointEventArgs.Handled = false;
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = tuioCursor.getSessionID(), DeviceDriverID = this.DeviceDriverID };
            pointEventArgs.PointEventType = PointEventType.LeftUp;
            pointEventArgs.PointScreen = getPointScreen(tuioCursor);
            pointEventArgs.DeviceType = DeviceType.MultiTouch;

            OnPointEvent(pointEventArgs);
            OnRemoveDeviceEvent(pointEventArgs.DeviceUID);
        }

        public void updateTuioCursor(TuioCursor tuioCursor)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();

            pointEventArgs.Handled = false;
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = tuioCursor.getSessionID(), DeviceDriverID = this.DeviceDriverID };
            pointEventArgs.PointEventType = PointEventType.Drag;
            pointEventArgs.PointScreen = getPointScreen(tuioCursor);
            pointEventArgs.DeviceType = DeviceType.MultiTouch;

            OnPointEvent(pointEventArgs);
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
