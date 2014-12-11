using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections.Generic;
using starPadSDK.WPFHelp;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;

namespace starPadSDK.SurfaceLib
{
    // Palm print customization toolbox

	public partial class PalmCustomize
	{
        public static ScatterView PalmCustomizeScatter = null;

		public PalmCustomize()
		{
			this.InitializeComponent();

            Contacts.AddPreviewContactUpHandler(btnClose, btnClose_ContactUp);

            
		}

        Dictionary<ScatterViewItem, PalmFinger> ScatterMapping = new Dictionary<ScatterViewItem, PalmFinger>();

        Random _rand = new Random();

        int _ItemCount = 0;

        protected Point GetPoint(int k, int left, int top)
        {
            int numCols = 5;
            int spacing = 70;

            int col = (k % numCols);
            int row = (k - col) / numCols;

            return new Point(left + (col * spacing), top + (row * spacing));
        }

        Dictionary<ScatterViewItem, Point> OriginalPoints = new Dictionary<ScatterViewItem, Point>();

        public void AddItem(string caption, string path)
        {
            PalmFinger item = new PalmFinger(caption, WPFUtil.GetAppDir() + "images\\" + path);
            ScatterHost host = new ScatterHost();

            ScatterViewItem sItem = new ScatterViewItem();
            sItem.Content = host;

            int k = PalmCustomizeScatter.Items.Add(sItem);
            host.Width = host.Height = 1;

            Rect rcS = ChildPlaceholder.GetBoundsTrans(PalmCustomizeScatter);

            //sItem.Center = new Point(rcS.Left + (rcS.Width / 2) + _rand.Next((int)-rcS.Width / 2, (int)rcS.Width / 2),
            //    rcS.Top + (rcS.Height / 2) + _rand.Next((int)-rcS.Height / 2, (int)rcS.Height / 2));

            sItem.Center = GetPoint(_ItemCount, (int)rcS.Left + 50, (int)rcS.Top);
            OriginalPoints.Add(sItem, sItem.Center);


            sItem.CanRotate = false;

            sItem.ContactChanged += new ContactEventHandler(sItem_ContactChanged);
            sItem.ContactUp += new ContactEventHandler(sItem_ContactUp);

            sItem.Opacity = 0.0;

            ScatterMapping.Add(sItem, item);

            ChildPlaceholder.Children.Add(item);

            // Update position

            Rect rc = sItem.GetBoundsTrans(ChildPlaceholder);

            ScatterMapping[sItem].RenderTransform = new TranslateTransform(rc.Left, rc.Top);

            _ItemCount++;
        }

        void sItem_ContactUp(object sender, ContactEventArgs e)
        {
            
        }

        protected void UpdatePositionOfVisual(ScatterViewItem scatterItem)
        {
            Rect rc = scatterItem.GetBoundsTrans(ChildPlaceholder);

            ScatterMapping[scatterItem].RenderTransform = new TranslateTransform(rc.Left, rc.Top);

        }

        void sItem_ContactChanged(object sender, ContactEventArgs e)
        {
            ScatterViewItem scatterItem = (ScatterViewItem) e.Source;
            UpdatePositionOfVisual(scatterItem);

            // Update

            //ScatterViewItem scatterItem = (ScatterViewItem)e.Source;

            foreach (PalmFinger pf in PalmPrint.CurrentInstance.Fingers)
            {
                Rect rcPF = pf.GetBoundsTrans(ChildPlaceholder);
                //Point pt = Helpers.TransformPointFromAtoB(scatterItem.Center, PalmCustomizeScatter, ChildPlaceholder);

                Rect rcScatter = scatterItem.GetBoundsTrans(ChildPlaceholder);
                Point pt = new Point(rcScatter.Left + (rcScatter.Width / 2), rcScatter.Top + (rcScatter.Height / 2));


                if (rcPF.Contains(pt))
                {
                    ImageSource tempSource = pf.Icon.Source;
                    string tempCaption = pf.Caption;

                    pf.Icon.Source = ScatterMapping[scatterItem].Icon.Source;
                    pf.Caption = ScatterMapping[scatterItem].Caption;

                    //ScatterMapping[scatterItem].Visibility = Visibility.Collapsed;

                    scatterItem.Center = OriginalPoints[scatterItem];
                    UpdatePositionOfVisual(scatterItem);

                    ScatterMapping[scatterItem].Icon.Source = tempSource;
                    ScatterMapping[scatterItem].Caption = tempCaption;

                    Contacts.CaptureContact(e.Contact, null);

                    pf.PlayCustomizeAnimation();

                    break;
                }
            }
        }

        public void Show()
        {
            _ItemCount = 0;
            ScatterMapping = new Dictionary<ScatterViewItem, PalmFinger>();
            OriginalPoints = new Dictionary<ScatterViewItem, Point>();
            PalmCustomizeScatter.Items.Clear();
            ChildPlaceholder.Children.Clear();

            Visibility = Visibility.Visible;

            Storyboard board = (Storyboard)Resources["SlideIn"];
            board.Begin(this);

            // Setup scatterview

            PalmCustomizeScatter.BringToFront();

                //Canvas scatterParent = (Canvas)PalmCustomizeScatter.Parent;
                //Rect rcLoc = Helpers.GetLocationTrans(ChildPlaceholder, scatterParent);

                //PalmCustomizeScatter.RenderTransform = new TranslateTransform(rcLoc.Left, rcLoc.Top);
                //PalmCustomizeScatter.Width = ChildPlaceholder.ActualWidth;
                //PalmCustomizeScatter.Height = ChildPlaceholder.ActualHeight;

            // Populate

            AddItem("Delete", "button_cancel.png");
            AddItem("Split Page", "cache.png");
            AddItem("Paintbrush", "colorize.png");
            AddItem("Pencil", "easymoblog.png");
            
            AddItem("Pen", "signature.png");
            
            AddItem("E-mail", "thunderbird.png");
            
            AddItem("Chalk", "tutorials.png");
            
            AddItem("Share", "web.png");

            AddItem("Smear", "xpaint.png");





            PalmFinger item = new PalmFinger();
            item.Create("Delete", WPFUtil.GetAppDir() + "images\\button_cancel.png");
            //item.Moveable = true;
            //CustomizeItems.Children.Add(item);

            

            //PalmCustomizeScatter.Items.Add(item);

            //item = new PalmFinger("Split Page", Helpers.GetAppDir() + "images\\cache.png");
            //ScatterHost host = new ScatterHost(item);
            
            //ScatterViewItem sItem = new ScatterViewItem();
            //sItem.Content = host;

            //int k = PalmCustomizeScatter.Items.Add(sItem);
            //host.Width = host.Height = 1;

            //sItem.Center = new Point(-15000, -15000);


            //item = new PalmFinger("Paintbrush", Helpers.GetAppDir() + "images\\colorize.png");
            //PalmCustomizeScatter.Items.Add(item);

            //item = new PalmFinger("Pencil", Helpers.GetAppDir() + "images\\easymoblog.png");
            //PalmCustomizeScatter.Items.Add(item);

            //item = new PalmFinger("Pen", Helpers.GetAppDir() + "images\\signature.png");
            //PalmCustomizeScatter.Items.Add(item);

            //item = new PalmFinger("E-mail", Helpers.GetAppDir() + "images\\thunderbird.png");
            //PalmCustomizeScatter.Items.Add(item);

            //item = new PalmFinger("Chalk", Helpers.GetAppDir() + "images\\tutorials.png");
            //PalmCustomizeScatter.Items.Add(item);

            //item = new PalmFinger("Share", Helpers.GetAppDir() + "images\\web.png");
            //PalmCustomizeScatter.Items.Add(item);

            //item = new PalmFinger("Smear", Helpers.GetAppDir() + "images\\xpaint.png");
            //PalmCustomizeScatter.Items.Add(item);
        }

        private void btnClose_ContactUp(object sender, ContactEventArgs e)
        {
            Hide();
        }

        public void Hide()
        {
            //Storyboard board = (Storyboard)Resources["Dismiss"];
            //board.Begin(this);
            this.Visibility = Visibility.Collapsed;

            PalmCustomizeScatter.Items.Clear();
            ChildPlaceholder.Children.Clear();

        }
	}
}