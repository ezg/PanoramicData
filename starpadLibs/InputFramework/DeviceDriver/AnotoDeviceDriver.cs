using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mil.AnotoPen;
using mil.conoto;
using System.Windows.Input;

namespace InputFramework.DeviceDriver
{
    /// <summary>
    /// Class AnotoDevice.cs
    /// Author: Adam Gokcezade
    /// Creation Date: 27. August 2008
    /// </summary>

    public class AnotoDeviceDriver : PointDeviceDriver, IDisposable
    {
        public delegate void ConotoFunctionRaisedHandler(Object sender, DeviceUID deviceUID, long pageID, string function, long functionID, double x, double y, float intensity, PointEventType type);
        public event ConotoFunctionRaisedHandler ConotoFunctionRaised;

        protected AnotoStreamingServer mAnotoStreamingServer;
        private ConotoElementManager mConotoElementManager;
        private ConotoConvert mConotoConvert;

        public AnotoDeviceDriver()
        {
            Initialize();    
        }

        public AnotoDeviceDriver(string configFile)
        {
            Initialize();
            LoadConotoConfiguration(configFile);
        }

        public override void Dispose()
        {
            mAnotoStreamingServer.Stop();
            mAnotoStreamingServer.Dispose();
            mil.AnotoPen.AnotoPen.Dispose();
        }

        public void LoadConotoConfiguration(string config)
        {
            mConotoElementManager.LoadConfigFile(config);
        }

        private void Initialize()
        {
            mil.AnotoPen.AnotoPen.Initialize("71ad1f27386ec4f5d91e15eca16f46c4");
            mAnotoStreamingServer = new AnotoStreamingServer();
            mAnotoStreamingServer.IsFixingPatternJump = true;
            mAnotoStreamingServer.OnStroke += new AnotoStreamingServer.AnotoEventHandler(anotoServer_OnStroke);
            mAnotoStreamingServer.OnPenConnect += new AnotoStreamingServer.AnotoEventHandler(anotoServer_OnPenConnect);

            mConotoElementManager = new ConotoElementManager();
            mConotoConvert = new ConotoConvert(mConotoElementManager);

            mAnotoStreamingServer.Start();
        }

        protected void anotoServer_OnPenConnect(object sender, AnotoPenEvent args)
        {
            OnAddDeviceEvent(new DeviceUID() { DeviceID = args.PenId, DeviceDriverID = this.DeviceDriverID });
        }

        protected void anotoServer_OnStroke(object sender, AnotoPenEvent args)
        {
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

            mConotoConvert.Convert((ulong)args.PageId, args.X, args.Y, out function, out functionID, out x, out y);

            if (function == "0")
            {
                PointEventArgs pointEventArgs = new PointEventArgs();

                pointEventArgs.DeviceType = DeviceType.Stylus;
                pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = args.PenId, DeviceDriverID = this.DeviceDriverID };
                pointEventArgs.Handled = false;

                switch (args.Type)
                {
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

                OnPointEvent(pointEventArgs);
            }
            else
            {
                if (ConotoFunctionRaised != null)
                {
                    PointEventType type = PointEventType.Unknown;
                    switch(args.Type){
                        case AnotoPenEventType.StrokeStart: type = PointEventType.LeftDown; break;
                        case AnotoPenEventType.StrokeDrag: type = PointEventType.Drag; break;
                        case AnotoPenEventType.StrokeEnd: type = PointEventType.LeftUp; break;
                    }

                    ConotoFunctionRaised(this, new DeviceUID() { DeviceDriverID = this.DeviceDriverID, DeviceID = args.PenId }, args.PageId, function, functionID, x, y, intensity, type);
                }
            }
        }
    }
}
