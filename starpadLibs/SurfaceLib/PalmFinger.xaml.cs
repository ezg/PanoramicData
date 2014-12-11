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
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;

namespace starPadSDK.SurfaceLib
{
    /// <summary>
    /// Interaction logic for PalmFinger.xaml
    /// </summary>
    public partial class PalmFinger : UserControl
    {
        public PalmFinger()
        {
            InitializeComponent();
            Init();

            //_Label2 = new OutlinedText();
            //_Label2.Text = "Hello, World";
            //LayoutRoot.Children.Add(_Label2);

            //_Label2.Fill = new SolidColorBrush(Colors.White);
            //_Label2.Stroke = new SolidColorBrush(Colors.Black);
            //_Label2.StrokeThickness = 2;
        }
        public Vec Size
        {
            get { return new Vec(Icon.Source.Width, Icon.Source.Height); }
        }

        public bool ModeActive
        {
            get { return modeVIs.Visibility == Visibility.Visible; }
            set { modeVIs.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string Caption
        {
            get
            {
                return txtLabel.Text;
            }

            set
            {
                txtLabel.Text = value;
                txtLabelBk.Text = value;
            }
        }

        public void PlayCustomizeAnimation()
        {
            Storyboard board = (Storyboard)Resources["CustomizeComplete"];
            board.Begin(this);
        }

        public void Create(string caption, string icon)
        {
            Caption = caption;
            Icon.Source = new BitmapImage(new Uri(icon, UriKind.Absolute));

        }

        public PalmFinger(string caption, string icon)
        {
            InitializeComponent();

            Create(caption, icon);
            Init();
        }

        public void Init()
        {
            //Contacts.AddPreviewContactDownHandler(this, This_ContactDown);
            //Contacts.AddPreviewContactUpHandler(this, This_ContactUp);
            //Contacts.AddPreviewContactChangedHandler(this, This_ContactMoved);
        }

        Point _ContactDownPoint;
        bool _Moveable = false;

        private void This_ContactDown(object sender, ContactEventArgs e)
        {
            //e.Contact.Capture(this);
            //_ContactDownPoint = e.GetPosition(LayoutRoot);
            //_Down = true;
        }

        private void This_ContactUp(object sender, ContactEventArgs e)
        {
        }

        private void This_ContactMoved(object sender, ContactEventArgs e)
        {
            if (_Moveable)
            {
                Point pt = e.GetPosition(LayoutRoot);

                Rect rc = LayoutRoot.GetBoundsTrans(this);

                LayoutRoot.RenderTransform = new TranslateTransform(rc.Left + pt.X - _ContactDownPoint.X, rc.Top + pt.Y - _ContactDownPoint.Y);
                _ContactDownPoint = e.GetPosition(LayoutRoot);
            }
        }

        public bool Moveable
        {
            get
            {
                return _Moveable;
            }

            set
            {
                _Moveable = value;
            }
        }
    }
}
