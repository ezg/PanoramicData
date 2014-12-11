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
	public partial class GestureTryItResult
	{
		public GestureTryItResult()
		{
			this.InitializeComponent();

            Init();
		}

        public void Init()
        {
            Storyboard board = (Storyboard)Resources["Reset"];
            board.Begin(this);

            //SuccessResult.Visibility = Visibility.Hidden;
            //SuccessResult.Opacity = 0.0;

            //FailureResult.Visibility = Visibility.Hidden;
            //FailureResult.Opacity = 0.0;
        }

        public void Success()
        {
            Storyboard board = (Storyboard)Resources["ShowSuccess"];
            board.Begin(this);
        }

        public void Failure()
        {
            Storyboard board = (Storyboard)Resources["ShowFailure"];
            board.Begin(this);
        }

        public event EventHandler OnClick;

        private void FailureResult_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OnClick != null)
            {
                
                OnClick(sender, e);
            }
        }

        private void SuccessResult_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OnClick != null)
            {
                
                OnClick(sender, e);
            }
        }
	}
}