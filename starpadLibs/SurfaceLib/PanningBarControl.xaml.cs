using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using starPadSDK.WPFHelp;
using starPadSDK.Geom;

namespace starPadSDK.SurfaceLib
{
    /// <summary>
    /// Interaction logic for PanningBarControl.xaml
    /// </summary>
    public partial class PanningBarControl : SurfaceUserControl
    {
        ScatterView _Pages = null;
        Contact     _invoker;
        double      _ScaleRatio = 0;
        Point       _curScreenLocation = new Point();
        const double screenWidth = 1024;
        const double screenHeight = 768;

        // place the panning cell at the same coordinates as the Surface's view onto the virtual screen
        void updatePanningCell()
        {
            PanningCell.RenderTransform = new TranslateTransform(_curScreenLocation.X - screenWidth / 2, _curScreenLocation.Y - screenHeight / 2);
        }

        public delegate void PanBarMovedHandler(object sender, Pt prevLocation, Pt panLocation);
        public event PanBarMovedHandler PanBarMovedEvent;

        public PanningBarControl()  { InitializeComponent(); }
        public void Show(ScatterView pages, Contact invoker, Pt placementOfControl)
        {
            // center the panning bar on the indicated position
            RenderTransform = new TranslateTransform(placementOfControl.X, placementOfControl.Y);

            _invoker = invoker;
            _Pages   = pages;
            Init();
        }

        public void Init()
        {
            // The panningView is a scaled down view of the Surface display
            // so the ratio of the panningView's height to the height of the display determines
            // the scale factor since we don't allow panning vertically
            Rect rcPanningView = PanningView.GetBoundsTrans(this);
            _ScaleRatio = rcPanningView.Height / screenHeight;
            // compute where the left edge of the PanningCell is in PanningView coordinates
            Pt center = new Pt((PanningView.Width - PanningCell.Width * _ScaleRatio) / 2, PanningView.Height - PanningCell.Height * _ScaleRatio);
            // align the ShiftableCanvas origin with the left edge of the PanningCell in the panning bar
            PanningView.RenderTransform = new MatrixTransform(_ScaleRatio, 0, 0, _ScaleRatio, center.X, center.Y);

            // Create view
            foreach (ScatterViewItem item in _Pages.Items)
            {
                Image img = FrostyFreeze.CreateImageFromControl_Cropped(item);

                PanningView.Children.Add(img);
                img.Stretch = Stretch.None;
                img.RenderTransform = new MatrixTransform(Mat.Translate(-item.Width / 2, -item.Height / 2) *
                    (Mat)new Matrix(item.RenderTransform.Value.M11, item.RenderTransform.Value.M12, item.RenderTransform.Value.M21, item.RenderTransform.Value.M22, 0,0) *
                    Mat.Translate(item.Center));
            }
            _curScreenLocation = new Point(screenWidth / 2, screenHeight/2);
            updatePanningCell();

            // Setup events
            Contacts.CaptureContact(_invoker, LayoutRoot);
            Contacts.AddContactChangedHandler(LayoutRoot, PanningBarContactChanged);
            Contacts.AddContactUpHandler(LayoutRoot, PanningBarContactUp);
        }

        public void Hide()
        {
            Storyboard board = (Storyboard)Resources["Hide"];
            board.Completed += new EventHandler((object sender, EventArgs args) => this.Visibility = Visibility.Collapsed);
            board.Begin(this, true);
        }

        void PanningBarContactUp(object sender, ContactEventArgs e)
        {
            if (e.Contact == _invoker)
               Hide();

        }
        void PanningBarContactChanged(object sender, ContactEventArgs e)
        {
            if (e.Contact == _invoker)
                RaiseMoveTo(new Point(e.GetPosition(PanningView).X, screenHeight / 2));
            //RaiseMoveTo(e.GetPosition(PanningView));
        }

        public void RaiseMoveTo(Point screenPoint)
        {
            if (PanBarMovedEvent != null)
               PanBarMovedEvent(this, _curScreenLocation, screenPoint);
            _curScreenLocation = screenPoint;
            updatePanningCell();
        }
    }
}
