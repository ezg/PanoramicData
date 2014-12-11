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
	public partial class PenDownDot
	{
		public PenDownDot()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
		}

        public void Hide()
        {
            Storyboard board = (Storyboard)Resources["HideDot"];
            board.Begin(this);

            /*IsHitTestVisible = false;
            Opacity = 0;*/
        }

        public void Show()
        {
            Storyboard board = (Storyboard)Resources["ShowDot"];
            board.Begin(this);

            //Opacity = 100;
        }
	}
}