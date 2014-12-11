using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace starPadSDK.GestureBarLib.UICommon
{
    public class InitNotify
    {
        protected static Timer _timer = null;

        public static void Initiate()
        {
            if (_timer == null)
            {
                _timer = new Timer();
                _timer.Interval = 500;
                _timer.Tick += new EventHandler(_timer_Tick);

                _timer.Start();
            }
        }

        public static void Reinitiate()
        {
            _timer = null;

            Initiate();
        }

        static void _timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();

            if (OnTrueInit != null)
            {
                OnTrueInit(sender, e);
            }
        }

        public static event EventHandler OnTrueInit;
    }
}
