using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using starPadSDK.AppLib;
using starPadSDK.WPFHelp;
using starPadSDK.SurfaceLib;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using System.Windows.Media.Imaging;


namespace starPadSDK.SurfaceLib
{
	public partial class SideToolbar
	{
		public SideToolbar()
		{
			this.InitializeComponent();

            Contacts.AddContactDownHandler(this, startDrag);
            Contacts.AddContactChangedHandler(this, moveDrag);
            Contacts.AddContactUpHandler(this, endDrag);
			// Insert code required on object creation below this point.
		}
        Pt down = new Pt();
        DateTime start = DateTime.MaxValue;
        public void startDrag(object sender, ContactEventArgs e)
        {
            start = DateTime.Now;
            down = e.Contact.GetPosition(this.Parent as FrameworkElement);
        }
        public void endDrag(object sender, ContactEventArgs e)
        {
            if (DateTime.Now.Subtract(start).TotalMilliseconds < 250)
                Visibility = Visibility.Collapsed;
            down = e.Contact.GetPosition(this.Parent as FrameworkElement);
        }
        public void moveDrag(object sender, ContactEventArgs e)
        {
            Pt cur = e.Contact.GetPosition(this.Parent as FrameworkElement);
            this.RenderTransform = new MatrixTransform(Mat.Translate(cur - down) * (Mat)this.RenderTransform.Value);

            down = cur;
        }
	}
}