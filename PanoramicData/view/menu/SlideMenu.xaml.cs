using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CombinedInputAPI;
using PanoramicData.Properties;
using System.Windows.Navigation;
using PanoramicData.controller.input;
using PanoramicData.controller.view;

namespace PanoramicData.view.menu
{
    /// <summary>
    /// Interaction logic for SlideMenu.xaml
    /// </summary>
    public partial class SlideMenu : UserControl
    {
        public delegate void DownHandler(object sender, EventArgs e);
        public event DownHandler Down;

        public delegate void ExitHandler(object sender, EventArgs e);
        public event ExitHandler Exit;

        public delegate void CenterHandler(object sender, EventArgs e);
        public event CenterHandler Center;

        public delegate void ClearHandler(object sender, EventArgs e);
        public event ClearHandler Clear;
        
        private DispatcherTimer _hideTimer = new DispatcherTimer();
        private long _hideTimerStartTime = 0;

        public SlideMenu()
        {
            InitializeComponent();

            SlideMenuItemTemplate item = new SlideMenuItemTemplate();
            item.label.Content = "Close Menu";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/img/down.png"));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 20, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(down_TouchDownEvent));

            item = new SlideMenuItemTemplate();
            item.label.Content = "Exit Application";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/img/exit.png"));
            contentPanel.Children.Insert(0, item);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(exit_TouchDownEvent));

            item = new SlideMenuItemTemplate();
            item.label.Content = "Center Canvas";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/img/home.png"));
            contentPanel.Children.Insert(0, item);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(center_TouchDownEvent));

            item = new SlideMenuItemTemplate();
            item.label.Content = "Clear Canvas";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/img/clear.png"));
            contentPanel.Children.Insert(0, item);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(clear_TouchDownEvent));
        }

        public void ShowMenu()
        {
            _hideTimer.Interval = TimeSpan.FromSeconds(1.0 / 60.0);
            _hideTimer.Tick += new EventHandler(_hideTimer_Tick);
            _hideTimerStartTime = DateTime.Now.Ticks;
            _hideTimer.Start();
        }

        public void Reset()
        {
            _hideTimer.Stop();
        }

        private void _hideTimer_Tick(object sender, EventArgs e)
        {
            if (_hideTimerStartTime + TimeSpan.FromSeconds(5).Ticks < DateTime.Now.Ticks)
            {
                if (Down != null)
                {
                    Down(this, new EventArgs());
                }
                _hideTimer.Stop();
            }
        }
        
        private void down_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if (Down != null)
            {
                Down(this, new EventArgs());
            }
        }


        private void exit_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Exit != null)
            {
                Exit(this, new EventArgs());
            }
        }

        private void clear_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Clear != null)
            {
                Clear(this, new EventArgs());
            }
        }

        private void center_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Center != null)
            {
                Center(this, new EventArgs());
            }
        }

        private void SlideMenuItemTemplate_TouchDown(object sender, TouchEventArgs e)
        {
            DatasetConfiguration ds = ((DatasetConfiguration)(sender as SlideMenuItemTemplate).DataContext);
            MainViewController.Instance.LoadData(ds);
        }
    }
}
