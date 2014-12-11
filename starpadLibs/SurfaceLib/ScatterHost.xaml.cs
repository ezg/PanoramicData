using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace starPadSDK.SurfaceLib
{
	public partial class ScatterHost
	{
		public ScatterHost()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
		}

        public ScatterHost(FrameworkElement content)
        {
            this.InitializeComponent();

            Contents = content;
        }

        public FrameworkElement Contents
        {
            get
            {
                return null;
            }

            set
            {
                LayoutRoot.Children.Clear();
                LayoutRoot.Children.Add(value);

                //LayoutRoot.RenderTransform = new TranslateTransform(1000, 1000);
            }
        }
	}
}