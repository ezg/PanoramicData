using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using mil.AnotoPenRemoting;

namespace InputFramework.DeviceDriver
{
    /// <summary>
    /// AnotoDeviceDriver that receives events from local as remote Anoto pens and forwards local events to remote ones.
    /// </summary>
    public class NetworkedAnotoDeviceDriver : AnotoDeviceDriver
    {
        public enum NetworkMode
        {
            Server,
            Client,
            Both
        }

        private IAnotoReceiver mNetworkReceiver;
        private IAnotoSender mNetworkSender;


        /// <summary>
        /// Construct NetworkedAnotoDeviceDriver without starting network or configuration
        /// </summary>
        public NetworkedAnotoDeviceDriver()
            : base()
        {            
        }

        /// <summary>
        /// Construct NetworkedAnotoDeviceDriver without starting the networking
        /// </summary>
        /// <param name="configfile">Path to the file storing the pattern configuration (pcf.xml)</param>
        public NetworkedAnotoDeviceDriver(string configfile)
            : base(configfile)
        {
        }
        
        /// <summary>
        /// Construct NetworkedAnotoDeviceDriver with configuration and start receiving and sending of events
        /// </summary>
        /// <param name="host">Hostname to connect to</param>
        /// <param name="port">Port which is used for receiving and sending the events</param>
        /// <param name="broadcast">true to enable UDP broadcasting (hostname is then ignored)</param>
        /// <param name="mode">Network mode (see <see cref="NetworkMode">NetworkMode</see>)</param>
        /// <param name="configfile">Path to the file storing the pattern configuration (pcf.xml)</param>
        public NetworkedAnotoDeviceDriver(string host, int port, bool broadcast, NetworkMode mode, string configfile)
            : base(configfile)
        {
            StartRemoting(host, port, broadcast, mode);
        }

        /// <summary>
        /// Construct NetworkedAnotoDeviceDriver without configuration but start receiving and sending of events
        /// </summary>
        /// <param name="host">Hostname to connect to</param>
        /// <param name="port">Port which is used for receiving and sending the events</param>
        /// <param name="broadcast">true to enable UDP broadcasting (hostname is then ignored)</param>
        /// <param name="mode">Network mode (see <see cref="NetworkMode">NetworkMode</see>)</param>
        public NetworkedAnotoDeviceDriver(string host, int port, bool broadcast, NetworkMode mode)
            : base()
        {
            StartRemoting(host, port, broadcast, mode);
        }
        /// <summary>
        /// Manually start the remoting. Call this function only if you have used the constructors without networking!
        /// </summary>
        /// <param name="host">Hostname to connect to</param>
        /// <param name="port">Port which is used for receiving and sending the events</param>
        /// <param name="broadcast">true to enable UDP broadcasting (hostname is then ignored)</param>
        /// <param name="mode">Network mode (see <see cref="NetworkMode">NetworkMode</see>)</param>
        public void StartRemoting(string host, int port, bool broadcast, NetworkMode mode)
        {
            AnotoEventSerializerFactory.UsedSerializer = AnotoEventSerializerFactory.SerializerType.Advanced;

            if (mode == NetworkMode.Client || mode == NetworkMode.Both)
            {
                mNetworkReceiver = new AnotoUDPReceiver(port);
                mNetworkReceiver.OnPenConnect += new mil.AnotoPen.AnotoStreamingServer.AnotoEventHandler(anotoServer_OnPenConnect);
                mNetworkReceiver.OnStroke += new mil.AnotoPen.AnotoStreamingServer.AnotoEventHandler(anotoServer_OnStroke);
                mNetworkReceiver.Open();
            }

            if (mode == NetworkMode.Server || mode == NetworkMode.Both)
            {                
                mNetworkSender = new AnotoUDPSender(host, port, broadcast);
                mNetworkSender.Open();
                mAnotoStreamingServer.OnPenConnect += new mil.AnotoPen.AnotoStreamingServer.AnotoEventHandler(mNetworkSender.SendEvent);
                mAnotoStreamingServer.OnPenDisconnect += new mil.AnotoPen.AnotoStreamingServer.AnotoEventHandler(mNetworkSender.SendEvent);
                mAnotoStreamingServer.OnStroke += new mil.AnotoPen.AnotoStreamingServer.AnotoEventHandler(mNetworkSender.SendEvent);                
            }            
        }

        #region IDisposable Members

        public override void Dispose()
        {
            base.Dispose();
            if (mNetworkSender != null && mNetworkSender is IDisposable) ((IDisposable)mNetworkSender).Dispose();
            if (mNetworkReceiver != null && mNetworkReceiver is IDisposable) ((IDisposable)mNetworkReceiver).Dispose();
        }

        #endregion
    }
}
