using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using InputFramework.WPFDevices;
using InputFramework;

namespace starPadSDK.AppLib
{
	public partial class Floatie  {
        Canvas           _ican = null;
        FrameworkElement _commandPanel = null;

        void ican_StylusInAirMove(object sender, StylusEventArgs e) {
            if (!new Rct(0, 0, ActualWidth, ActualHeight).Contains(e.GetPosition(this)))
                dockPanel.Visibility = Visibility.Hidden;
        }
        void ican_MouseMove(object sender, MouseEventArgs e) {
            if (!new Rct(0, 0, ActualWidth, ActualHeight).Contains(e.GetPosition(this)))
                dockPanel.Visibility = Visibility.Hidden;
        }
        void ican_PointMove(object sender, RoutedPointEventArgs e) {
            if (!new Rct(0,0,ActualWidth,ActualHeight).Contains(e.GetPosition(this)))
                dockPanel.Visibility = Visibility.Hidden;
        }

        void Shadow_MouseMove(object sender, MouseEventArgs e)               { dockPanel.Visibility = Visibility.Visible; }
        void Shadow_StylusInAirMove(object sender, StylusEventArgs e)        { dockPanel.Visibility = Visibility.Visible; }
        void Shadow_StylusButtonDown(object sender, StylusButtonEventArgs e) { dockPanel.Visibility = Visibility.Visible; e.Handled = true; }
        void Shadow_MouseDown(object sender, MouseButtonEventArgs e)         { dockPanel.Visibility = Visibility.Visible; e.Handled = true; }
        void Shadow_PointDown(object sender, RoutedEventArgs e)              { dockPanel.Visibility = Visibility.Visible; e.Handled = true; }

        public Floatie() {
            this.InitializeComponent();
            this.Shadow.AddHandler(WPFPointDevice.PreviewPointDownEvent, new RoutedPointEventHandler(Shadow_PointDown));
            this.Shadow.MouseMove += new System.Windows.Input.MouseEventHandler(Shadow_MouseMove);
            this.Shadow.StylusInAirMove += new System.Windows.Input.StylusEventHandler(Shadow_StylusInAirMove);
            this.Shadow.PreviewMouseDown += new MouseButtonEventHandler(Shadow_MouseDown);
            this.Shadow.PreviewStylusButtonDown += new StylusButtonEventHandler(Shadow_StylusButtonDown);
            dockPanel.Visibility = Visibility.Hidden;
        }

        public FrameworkElement CommandPanel {
            get { return _commandPanel; }
            set {
                _commandPanel = value;
                dockPanel.Children.Clear();
                if (value != null)
                    dockPanel.Children.Add(_commandPanel);
            }
        }

        public void SetInkCanvas(Canvas ican) {
            _ican = ican;
            ican.MouseMove += new System.Windows.Input.MouseEventHandler(ican_MouseMove);
            ican.StylusInAirMove += new System.Windows.Input.StylusEventHandler(ican_StylusInAirMove);
            ican.AddHandler(WPFPointDevice.PointMoveEvent, new RoutedPointEventHandler(ican_PointMove));
        }
	}
}