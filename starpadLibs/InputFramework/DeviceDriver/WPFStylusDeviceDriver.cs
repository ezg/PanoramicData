using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace InputFramework.DeviceDriver
{
    /// <summary>
    /// Class MouseDevice.cs
    /// Author: Adam Gokcezade
    /// Creation Date: 01. September 2008
    /// </summary>

    public class WPFStylusDeviceDriver : PointDeviceDriver
    {
        private Window mWindow;

        public WPFStylusDeviceDriver(Window window)
        {
            mWindow = window;

            mWindow.PreviewStylusDown += new StylusDownEventHandler(window_PreviewStylusDown);
            mWindow.PreviewStylusUp += new StylusEventHandler(window_PreviewStylusUp);
            mWindow.PreviewStylusButtonDown += new StylusButtonEventHandler(window_PreviewStylusButtonDown);
            mWindow.PreviewStylusButtonUp += new StylusButtonEventHandler(window_PreviewStylusButtonUp);
            mWindow.PreviewStylusMove += new StylusEventHandler(window_PreviewStylusMove);
            mWindow.PreviewStylusInAirMove += new StylusEventHandler(window_PreviewStylusInAirMove); 
        }

        public override void Dispose()
        {
        }

        private void window_PreviewStylusDown(Object sender, StylusDownEventArgs stylusDownEventArgs)
        {
            if (stylusDownEventArgs.StylusDevice != null)
            {
                PointEventArgs pointEventArgs = new PointEventArgs();

                initPointEvent(ref pointEventArgs, stylusDownEventArgs);

                pointEventArgs.PointEventType = PointEventType.LeftDown;

                OnPointEvent(pointEventArgs);
            }
        }

        private void window_PreviewStylusUp(Object sender, StylusEventArgs stylusEventArgs)
        {
            if (stylusEventArgs.StylusDevice != null)
            {
                PointEventArgs pointEventArgs = new PointEventArgs();

                initPointEvent(ref pointEventArgs, stylusEventArgs);

                pointEventArgs.PointEventType = PointEventType.LeftUp;

                OnPointEvent(pointEventArgs);
            }
        }

        private void window_PreviewStylusButtonDown(Object sender, StylusButtonEventArgs stylusButtonEventArgs)
        {
            if (stylusButtonEventArgs.StylusDevice != null)
            {
                if(stylusButtonEventArgs.StylusButton.Guid == StylusPointProperties.BarrelButton.Id) {
                    PointEventArgs pointEventArgs = new PointEventArgs();

                    initPointEvent(ref pointEventArgs, stylusButtonEventArgs);

                    pointEventArgs.PointEventType = PointEventType.RightDown;

                    OnPointEvent(pointEventArgs);
                }
            }
        }

        private void window_PreviewStylusButtonUp(Object sender, StylusButtonEventArgs stylusButtonEventArgs)
        {
            if (stylusButtonEventArgs.StylusDevice != null)
            {
                if (stylusButtonEventArgs.StylusButton.Guid == StylusPointProperties.BarrelButton.Id)
                {
                    PointEventArgs pointEventArgs = new PointEventArgs();

                    initPointEvent(ref pointEventArgs, stylusButtonEventArgs);

                    pointEventArgs.PointEventType = PointEventType.RightUp;

                    OnPointEvent(pointEventArgs);
                }
            }
        }

        private void window_PreviewStylusMove(Object sender, StylusEventArgs stylusEventArgs)
        {
            if (stylusEventArgs.StylusDevice != null)
            {
                PointEventArgs pointEventArgs = new PointEventArgs();

                initPointEvent(ref pointEventArgs, stylusEventArgs);

                pointEventArgs.PointEventType = PointEventType.Drag;

                OnPointEvent(pointEventArgs);
            }
        }


        private void window_PreviewStylusInAirMove(Object sender, StylusEventArgs stylusEventArgs)
        {
            if (stylusEventArgs.StylusDevice != null)
            {
                PointEventArgs pointEventArgs = new PointEventArgs();

                initPointEvent(ref pointEventArgs, stylusEventArgs);

                pointEventArgs.PointEventType = PointEventType.Move;

                OnPointEvent(pointEventArgs);
            }
        }

        private void initPointEvent(ref PointEventArgs pointEventArgs, StylusEventArgs stylusEventArgs)
        {
            pointEventArgs.Handled = false;
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = stylusEventArgs.StylusDevice.Id, DeviceDriverID = this.DeviceDriverID };
            pointEventArgs.DeviceType = DeviceType.Stylus;

            StylusPointCollection intermediatePointsWindow = stylusEventArgs.GetStylusPoints(mWindow);
            int numPoints = intermediatePointsWindow.Count;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                pointEventArgs.InBetweenPointsScreen[i] = new PressurePoint(mWindow.PointToScreen(intermediatePointsWindow[i].ToPoint()), intermediatePointsWindow[i].PressureFactor);
            }

            pointEventArgs.PointScreen = pointEventArgs.InBetweenPointsScreen[numPoints - 1];
        }
    }
}
