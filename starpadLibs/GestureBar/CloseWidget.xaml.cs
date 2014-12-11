using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace starPadSDK.GestureBarLib
{
	public partial class CloseWidget
	{
		public CloseWidget()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
		}

        public event EventHandler OnClick;

        private void UserControl_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OnClick != null)
            {
                OnClick(sender, e);
            }
        }
	}
}