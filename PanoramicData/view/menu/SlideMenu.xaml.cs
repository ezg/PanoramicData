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

        public delegate void ExportHandler(object sender, EventArgs e);
        public event ExportHandler Export;

        public delegate void FacebookHandler(object sender, EventArgs e);
        public event FacebookHandler Facebook;

        public delegate void TitanicHandler(object sender, EventArgs e);
        public event TitanicHandler Titanic;

        public delegate void NBAHandler(object sender, EventArgs e);
        public event NBAHandler NBA;

        public delegate void Hua1Handler(object sender, EventArgs e);
        public event Hua1Handler Hua1;

        public delegate void Hua2Handler(object sender, EventArgs e);
        public event Hua2Handler Hua2;

        public delegate void CensusHandler(object sender, EventArgs e);
        public event CensusHandler Census;

        public delegate void BaseballHandler(object sender, EventArgs e);
        public event BaseballHandler Basball;

        public delegate void CSFacultyHandler(object sender, EventArgs e);
        public event CSFacultyHandler CSFaculty;

        public delegate void DBSearchHandler(object sender, string needle);
        public event DBSearchHandler DBSearch;

        private DispatcherTimer _hideTimer = new DispatcherTimer();
        private long _hideTimerStartTime = 0;

        public SlideMenu()
        {
            InitializeComponent();
            
            SlideMenuItem item = new SlideMenuItem();
            item.label.Content = "Close Menu";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/down.png", UriKind.RelativeOrAbsolute));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 20, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(down_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "Exit Application";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/exit.png"));
            contentPanel.Children.Insert(0, item);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(exit_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "Center Canvas";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/home.png"));
            contentPanel.Children.Insert(0, item);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(center_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "Clear Canvas";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/clear.png"));
            contentPanel.Children.Insert(0, item);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(clear_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "Titanic";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/data_base.png"));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 100, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(titanic_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "NBA";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/data_base.png"));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 0, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(nba_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "Census";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/data_base.png"));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 0, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(census_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "Baseball";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/data_base.png"));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 0, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(baseball_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "CS Faculty";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/data_base.png"));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 0, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(faculty_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "Subject Sessions";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/data_base.png"));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 0, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(hua1_TouchDownEvent));

            item = new SlideMenuItem();
            item.label.Content = "Within Subject Pairs";
            item.img.Source = new BitmapImage(new Uri(@"pack://application:,,,/PanoramicData;component/Resources/data_base.png"));
            contentPanel.Children.Insert(0, item);
            item.Margin = new Thickness(0, 0, 0, 0);
            item.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(hua2_TouchDownEvent));

            /*down.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(down_TouchDownEvent));
            exit.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(exit_TouchDownEvent));
            center.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(center_TouchDownEvent));
            clear.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(clear_TouchDownEvent));
            titanic.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(titanic_TouchDownEvent));
            nba.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(nba_TouchDownEvent));
            //export.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(export_TouchDownEvent));
            //facebook.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(facebook_TouchDownEvent));

            string[] schemas = Properties.Settings.Default.PanoramicDataAccesibleSchemas.Split(new string[] { "," },
                StringSplitOptions.RemoveEmptyEntries);

            if (!schemas.Contains("nba"))
            {
                nba.Visibility = Visibility.Collapsed;
                imgNba.Visibility = Visibility.Collapsed;
                ellNba.Visibility = Visibility.Collapsed;
                lblNba.Visibility = Visibility.Collapsed;
            }
            if (!schemas.Contains("titanic"))
            {
                titanic.Visibility = Visibility.Collapsed;
                imgTitanic.Visibility = Visibility.Collapsed;
                ellTitanic.Visibility = Visibility.Collapsed;
                lblTitanic.Visibility = Visibility.Collapsed;
            }

            dbSearchTB.KeyDown += dbSearchTB_KeyDown;*/
        }

        private void faculty_TouchDownEvent(object sender, TouchEventArgs pointEventArgs)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (CSFaculty != null)
            {
                CSFaculty(this, new EventArgs());
            }
        }

        private void baseball_TouchDownEvent(object sender, TouchEventArgs pointEventArgs)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Basball != null)
            {
                Basball(this, new EventArgs());
            }
        }

        private void census_TouchDownEvent(object sender, TouchEventArgs pointEventArgs)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Census != null)
            {
                Census(this, new EventArgs());
            }
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

        private void dbSearchTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //if (dbSearchTB.Text.Trim() != "" && DBSearch != null)
                //{
                //    DBSearch(this, dbSearchTB.Text.Trim());
                //}
            }
        }

        private void down_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if (Down != null)
            {
                Down(this, new EventArgs());
            }
        }


        private void facebook_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if (Facebook != null)
            {
                Facebook(this, new EventArgs());
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

        private void nba_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (NBA != null)
            {
                NBA(this, new EventArgs());
            }
        }

        private void titanic_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Titanic != null)
            {
                Titanic(this, new EventArgs());
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

        private void export_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Export != null)
            {
                Export(this, new EventArgs());
            }
        }

        private void hua1_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Hua1 != null)
            {
                Hua1(this, new EventArgs());
            }
        }

        private void hua2_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            _hideTimerStartTime = DateTime.Now.Ticks;
            if (Hua2 != null)
            {
                Hua2(this, new EventArgs());
            }
        }
    }
}
