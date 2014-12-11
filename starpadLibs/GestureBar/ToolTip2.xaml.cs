using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using starPadSDK.GestureBarLib.UICommon;
using System.Windows.Shapes;

namespace starPadSDK.GestureBarLib
{
	public partial class ToolTip2
	{
		public ToolTip2()
		{
			this.InitializeComponent();
		}

        DateTime lastDisplay = DateTime.MinValue;
        Storyboard FadeIn = null;

        public void Display(FrameworkElement parentCommand)
        {
            if (!starPadSDK.GestureBarLib.GestureExplorer.IsExplorerOpen())
            {
                // Update

                lastDisplay = DateTime.Now;

                // Reposition tooltip

                FrameworkElement tooltipParent = (FrameworkElement)this.Parent;
                FrameworkElement mainWindow = Helpers.GetEldestAncestor(tooltipParent);



                this.RenderTransform = new TranslateTransform(Helpers.GetLocationTrans(parentCommand, tooltipParent).Left, 0);

                

                Rect rc = Helpers.GetLocationTrans(this, mainWindow);
                double mainWindowRight = Helpers.GetLocation(mainWindow).Right - 20;

                if (rc.Right > mainWindowRight)
                {
                    this.RenderTransform = new TranslateTransform(Helpers.GetLocationTrans(parentCommand, tooltipParent).Left - (rc.Right - mainWindowRight), 
                        0);
                }

                // Animate

                FadeIn = (Storyboard)this.Resources["FadeIn"];
                FadeIn.Begin(this, true);

                // Resize tooltip if necessary

                Helpers.ForceUpdateLayout(DescriptionLabel);

                this.Height = Helpers.GetLocation(DescriptionLabel).Bottom;
                BackgroundRectangle.Height = this.Height;
            }
        }

        public bool IsVisible
        {
            get
            {
                return ToolTipCanvas.Opacity > 0;
            }
        }

        public void Hide()
        {
            if (FadeIn != null)
                FadeIn.Stop(this);

            if (IsVisible)
            {
                

                TimeSpan delta = DateTime.Now.Subtract(lastDisplay);

                if (delta.TotalMilliseconds > 1500)
                {
                    Storyboard fadeOut = (Storyboard)this.Resources["FadeOut"];

                    fadeOut.Begin(this);
                }
            }
        }

        private void ToolTipCanvas_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
        }

        private void Rectangle_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ToolTipCanvas.Opacity > 0)
            {
                Hide();
            }
        }

        public string Title
        {
            get
            {
                return TitleLabel.Text;
            }

            set
            {
                TitleLabel.Text = value;
            }
        }

        public string Description
        {
            get
            {
                return DescriptionLabel.Text;
            }

            set
            {
                DescriptionLabel.Text = value;
            }
        }

	}
}