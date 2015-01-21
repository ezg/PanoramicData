using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using FarseerPhysics.Dynamics;
using Microsoft.Research.DynamicDataDisplay;
using starPadSDK.Geom;
using System.IO;
using PanoramicDataModel;
using PanoramicDataDBConnector;
using starPadSDK.WPFHelp;
using starPadSDK.AppLib;
using PixelLab.Common;
using Path = System.Windows.Shapes.Path;
using PanoramicData.utils.inq;
using PanoramicData.view.schema;
using PanoramicData.model.view;
using PanoramicData.controller.data;
using PanoramicData.controller.physics;
using PanoramicData.Properties;
using PanoramicData.utils;
using CombinedInputAPI;
using PanoramicData.controller.view;
using starPadSDK.AppLib;
using System.Diagnostics;
using PanoramicData.view.vis;

namespace PanoramicData
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _startDrag1 = new Point();
        private Point _startDrag2 = new Point();

        private Point _current1 = new Point();
        private Point _current2 = new Point();
        private double length = 0.0;

        private TouchDevice _dragDevice1 = null;
        private TouchDevice _dragDevice2 = null;

        private Stopwatch _lastTapTimer = new Stopwatch();
        private Stopwatch _upTimer = new Stopwatch();

        private bool _renderTouchPoints = false;
        private Dictionary<TouchDevice, FrameworkElement> _deviceRenderings = new Dictionary<TouchDevice, FrameworkElement>();
        private bool _isSlideMenuAnimationRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            MouseTouchDevice.RegisterEvents(this);

            // init view controller
            MainViewController.CreateInstance(inkableScene, this);
            DataContext = MainViewController.Instance.MainModel;

            if (Settings.Default.RenderFingers)
            {
                this.Cursor = Cursors.None;
            }
            
            // init view titanic dataset;
            loadTitanicData();
            //loadHuaData();
            
            layoutRoot.PreviewTouchDown += layoutRoot_PreviewTouchDown;
            layoutRoot.PreviewTouchMove += layoutRoot_PreviewTouchMove;
            layoutRoot.PreviewTouchUp += layoutRoot_PreviewTouchUp;

            inkableScene.TouchDown += inkableScene_TouchDownEvent;

            up.TouchDown += up_TouchDown;
            slideMenu.Down += slideMenu_Down;
            slideMenu.Exit += slideMenu_Exit;
            slideMenu.Clear += slideMenu_Clear;
            slideMenu.Center += slideMenu_Center;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PhysicsController.SetRootCanvas(mainGrid);
            var d = PhysicsController.Instance; // dummy first call 
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            ResourceManager.WriteToFile();
        }
        
        void layoutRoot_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (Settings.Default.RenderFingers)
            {
                layoutRoot.Children.Remove(_deviceRenderings[e.TouchDevice]);
                _deviceRenderings.Remove(e.TouchDevice);
            }
        }

        void layoutRoot_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            if (Settings.Default.RenderFingers)
            {
                FrameworkElement cnv = _deviceRenderings[e.TouchDevice];
                Point pos = e.GetTouchPoint((IInputElement)layoutRoot).Position;
                (cnv.RenderTransform as TranslateTransform).X = pos.X;
                (cnv.RenderTransform as TranslateTransform).Y = pos.Y;
            }
        }

        void layoutRoot_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (Settings.Default.RenderFingers)
            {
                Canvas cnv = new Canvas();
                Path p = new Path();
                p.Effect = new DropShadowEffect()
                {
                    Color = Colors.Black,
                    Direction = 45,
                    ShadowDepth = 3,
                    BlurRadius = 5,
                    Opacity = 0.5
                };
                p.Data = Geometry.Parse("m 0,0 c 0,0 0.804,0.702 -0.604,1.908 0,0 -2.958,1.423 -5.141,4.927 0,0 -1.792,1.835 -1.73,2.506 -0.403,2.921 -0.88,2.841 -0.752,3.267 -1.14,1.398 0.181,4.944 2.25,1.956 0.044,0 1.169,-2.459 1.2,-2.426 0,0 0.871,-2.064 1.654,-2.527 1.366,0.05 0.633,6.248 0.81,6.673 0,0 -0.236,3.706 -0.083,10.864 0.344,3.79 2.509,1.769 2.734,0.728 0.274,-0.044 0.803,-9.988 1.126,-10.084 0,0 2.614,2.01 3.92,-0.905 0,0 2.615,1.508 3.619,-1.408 0,0 3.519,-0.301 3.217,-3.519 0,0 0.738,-6.031 -1.473,-10.252 L 10.21,-0.102 0,0 z");
                p.Fill = new SolidColorBrush(Helpers.GetColorFromString("#55606E"));
                p.Stroke = Brushes.White;
                p.StrokeThickness = 1;
                p.RenderTransform = new MatrixTransform(new Matrix()
                {
                    M11 = 2.1443171,
                    M12 = 0,
                    M21 = 0,
                    M22 = -2.1443171,
                    OffsetX = 2.7481961,
                    OffsetY = 62.730957
                });
                cnv.Children.Add(p);
                cnv.RenderTransform = new TranslateTransform();
                Point pos = e.GetTouchPoint((IInputElement)layoutRoot).Position;
                (cnv.RenderTransform as TranslateTransform).X = pos.X;
                (cnv.RenderTransform as TranslateTransform).Y = pos.Y;
                _deviceRenderings.Add(e.TouchDevice, cnv);
                cnv.IsHitTestVisible = false;
                layoutRoot.Children.Add(cnv);
            }
        }


        void inkableScene_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;


            /*var elements = new List<FrameworkElement>(this.GetIntersectedElements(new Rct(e.GetTouchPoint(this).Position, new Vec(1, 1))).Where((t) => t is
                FrameworkElement).Select((t) => t as FrameworkElement));

            if (elements.Count > 0)
                return;
            */
            if (_dragDevice1 == null)
            {
                _upTimer.Restart();
                e.Handled = true;
                e.TouchDevice.Capture(this);
                _startDrag1 = e.GetTouchPoint((FrameworkElement)this.Parent).Position;
                _current1 = e.GetTouchPoint((FrameworkElement)this).Position;

                this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(inkableScene_TouchMoveEvent));
                this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(inkableScene_TouchUpEvent));
                _dragDevice1 = e.TouchDevice;
            }
            else if (_dragDevice2 == null)
            {
                e.Handled = true;
                e.TouchDevice.Capture(this);
                _dragDevice2 = e.TouchDevice;
                _current2 = e.GetTouchPoint((FrameworkElement)this).Position;
            }
        }

        void inkableScene_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                length = 0.0;

                if (_dragDevice2 != null)
                {
                    _dragDevice1 = _dragDevice2;
                    _startDrag1 = _startDrag2;
                    _dragDevice2 = null;
                }
                else
                {
                    this.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(inkableScene_TouchMoveEvent));
                    this.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(inkableScene_TouchUpEvent));
                }

                Console.WriteLine();
                Console.WriteLine(_upTimer.ElapsedMilliseconds);
                Console.WriteLine(_lastTapTimer.ElapsedMilliseconds);
                if (_upTimer.ElapsedMilliseconds < 200 && _lastTapTimer.ElapsedMilliseconds != 0 &&
                    _lastTapTimer.ElapsedMilliseconds < 300)
                {
                    Pt pos = e.GetTouchPoint(inkableScene).Position;
                    /*_schemaViewer.RenderTransform = new TranslateTransform(
                        pos.X - _schemaViewer.Width / 2.0,
                        pos.Y - _schemaViewer.Height / 2.0);
                    this.AddNoUndo(_schemaViewer);
                    this.UpdateLayout();
                    Console.WriteLine((pos));
                    Console.WriteLine(this.TranslatePoint(pos, _schemaViewer));*/
                    MainViewController.Instance.ShowSchemaViewer(pos);
                }


                _upTimer.Stop();
                _lastTapTimer.Restart();
            }
            else if (e.TouchDevice == _dragDevice2)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice2 = null;
                length = 0.0;
            }

        }

        void inkableScene_TouchMoveEvent(object sender, TouchEventArgs e)
        {
            Point curDrag = e.GetTouchPoint((FrameworkElement)this.Parent).Position;

            if (e.TouchDevice == _dragDevice1)
            {
                if (_dragDevice2 == null)
                {
                    Vector dragBy = curDrag - _startDrag1;
                    inkableScene.RenderTransform = new MatrixTransform(((Mat)inkableScene.RenderTransform.Value) * Mat.Translate(dragBy));
                }
                _startDrag1 = curDrag;
                _current1 = e.GetTouchPoint((FrameworkElement)this).Position;
                e.Handled = true;
            }
            else if (e.TouchDevice == _dragDevice2)
            {
                _startDrag2 = curDrag;
                _current2 = e.GetTouchPoint((FrameworkElement)this).Position;
                e.Handled = true;
            }

            if (_dragDevice1 != null && _dragDevice2 != null)
            {
                double newLength = (_current1.GetVec() - _current2.GetVec()).Length;
                if (length != 0.0)
                {
                    Vector scalePos = (_current1.GetVec() + _current2.GetVec()) / 2.0;
                    double scale = newLength / length;

                    Matrix m1 = this.RenderTransform.Value;
                    m1.ScaleAtPrepend(scale, scale, scalePos.X, scalePos.Y);

                    inkableScene.RenderTransform = new MatrixTransform(m1);
                }
                length = newLength;
            }
        }

        protected override void OnManipulationBoundaryFeedback(ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
        
        private void layoutRoot_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Properties.Settings.Default.PanoramicDataEnableKeyboardShortcuts)
            {
                return;
            }
            if (e.Key == Key.F11)
            {
                if (WindowState == WindowState.Normal)
                {
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                    ResizeMode = ResizeMode.NoResize;
                }
                else if (WindowState == WindowState.Maximized)
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Normal;
                    ResizeMode = ResizeMode.CanResize;
                }
            }

            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                return;
            }
            if (Key.Z == e.Key)
            {
                MainViewController.Instance.InkableScene.Background = Brushes.White;
            }
            if (Key.D == e.Key)
            {
                DatabaseManager.Verbose = !DatabaseManager.Verbose;
            }
            if (Key.M == e.Key)
            {
                bool? multiSample = ResourceManager.GetBool("IsMultiSamplingEnabled");
                if (!multiSample.HasValue)
                {
                    multiSample = true;
                }
                ResourceManager.Add("IsMultiSamplingEnabled", !multiSample.Value);

            }
            if (Key.V == e.Key)
            {
                IDataObject iData = Clipboard.GetDataObject();
                string[] formats = iData.GetFormats();
                var obj = iData.GetData("Csv");
            }
            if (Key.Q == e.Key)
            {
                
                _renderTouchPoints = !_renderTouchPoints;
                if (_renderTouchPoints)
                {
                    this.Cursor = System.Windows.Input.Cursors.None;
                }
                else
                {
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
                }
            }
            if (Key.T == e.Key)
            {
                loadTitanicData();
            }
            if (Key.C == e.Key)
            {
                loadCoffeeData();
            } 
            if (Key.G == e.Key)
            {
                loadCensusData();
            }
            if (Key.F == e.Key)
            {
                loadFacultyData();
            }
            if (Key.B == e.Key)
            {
                loadBaseballData();
            }
            if (Key.L == e.Key)
            {
                loadTipData();
            }
            if (Key.P == e.Key)
            {
                Pt position = this.TranslatePoint(new Point(100, 100), MainViewController.Instance.InkableScene);

                VisualizationContainerView filter = new VisualizationContainerView();
                FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
                filterHolderViewModel.FilterRendererType = FilterRendererType.Pivot;
               
                filterHolderViewModel.Center = new Point(position.X + VisualizationContainerView.WIDTH / 2.0, position.Y + VisualizationContainerView.HEIGHT / 2.0);
                //filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(position, new Vec(VisualizationContainerView.WIDTH, VisualizationContainerView.HEIGHT));
            }
            if (Key.N == e.Key)
            {
                loadNbaData();
            }
        }

        private void loadTitanicData()
        {
            /*PathInfo playerPath = ModelHelpers.GeneratePathInfo("titanic", "passenger");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] {playerPath},
                new string[][] {new string[] {"name", "passenger_class", "survived", "sex", "age", "home"}});

            Pt position = this.TranslatePoint(new Point(300, 400), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }

        private void loadHua1Data()
        {
            /*PathInfo playerPath = ModelHelpers.GeneratePathInfo("hua", "subject_sessions");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "block", "trial" } });

            Pt position = this.TranslatePoint(new Point(300, 400), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }

        private void loadHua2Data()
        {
            /*PathInfo playerPath = ModelHelpers.GeneratePathInfo("hua2", "within_subject_pairs");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "block", "trial" } });

            Pt position = this.TranslatePoint(new Point(300, 400), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }

        private void loadCoffeeData()
        {
            /*PathInfo playerPath = ModelHelpers.GeneratePathInfo("coffee", "coffee_sales");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "sales_date", "sales", "profit" } });

            Pt position = this.TranslatePoint(new Point(300, 400), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            // filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }

        private void loadCensusData()
        {
            /*PathInfo playerPath = ModelHelpers.GeneratePathInfo("census", "census");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "age", "education", "martial_status" } });

            Pt position = this.TranslatePoint(new Point(300, 400), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }

        private void loadFacultyData()
        {
            /*PathInfo playerPath = ModelHelpers.GeneratePathInfo("faculty", "cs_faculty");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "Rank" } });

            Pt position = this.TranslatePoint(new Point(300, 400), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }

        private void loadBaseballData()
        {
            /*PathInfo playerPath = ModelHelpers.GeneratePathInfo("lahman", "fact");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "year" } });

            Pt position = this.TranslatePoint(new Point(300, 400), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }

        private void loadTipData()
        {
            /*PathInfo playerPath = ModelHelpers.GeneratePathInfo("tip", "tip");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "tip", "total_bill", "percentage" } });

            Pt position = this.TranslatePoint(new Point(300, 400), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }


        public void loadNbaData()
        {
            /*PathInfo gameLogPath = ModelHelpers.GeneratePathInfo("nba", "game_log");
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("nba", "game_log", "player");
            PathInfo teamPath = ModelHelpers.GeneratePathInfo("nba", "game_log", "team");
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { gameLogPath, playerPath },
                new string[][] { new string[] { "pts", "mp" }, new string[] { "name" } });

            Pt position = this.TranslatePoint(new Point(100, 100), MainViewController.Instance.InkableScene);

            FilterHolder filter = new FilterHolder();
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);

            //filterHolderViewModel.Center = new Point(position.X + FilterHolder.WIDTH / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filterHolderViewModel.FilterRendererType = FilterRendererType.Table;
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT));

            MainViewController.Instance.InkableScene.SetSchemaViewerFilterModel(filterHolderViewModel);*/
        }

        void up_TouchDown(object sender, TouchEventArgs e)
        {
            if (!_isSlideMenuAnimationRunning)
            {
                upGrid.RenderTransform = new TranslateTransform(this.ActualWidth - 95, this.ActualHeight - 90);
                slideMenu.Visibility = Visibility.Visible;
                slideMenu.Width = this.ActualWidth;
                slideMenu.RenderTransform = new TranslateTransform(0, this.ActualHeight);

                Storyboard storyBoard = new Storyboard();
                NameScope.SetNameScope(this, new NameScope());

                // slide menu animation
                TranslateTransform tt = (TranslateTransform)slideMenu.RenderTransform;
                RegisterName("slideMenuTransform", tt);
                DoubleAnimation anim = new DoubleAnimation();
                anim.From = this.ActualHeight;
                anim.To = this.ActualHeight - slideMenu.Height - 10;
                anim.EasingFunction = new SineEase();
                anim.Duration = TimeSpan.FromSeconds(0.8);
                storyBoard.Children.Add(anim);
                Storyboard.SetTargetName(anim, "slideMenuTransform");
                Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.YProperty));

                // show menu button animation
                tt = (TranslateTransform)upGrid.RenderTransform;
                RegisterName("showMenuTransform", tt);
                anim = new DoubleAnimation();
                anim.From = this.ActualWidth - 95;
                anim.To = this.ActualWidth;
                anim.EasingFunction = new SineEase();
                anim.Duration = TimeSpan.FromSeconds(0.4);
                storyBoard.Children.Add(anim);
                Storyboard.SetTargetName(anim, "showMenuTransform");
                Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.XProperty));

                storyBoard.Completed += (object sender1, EventArgs e1) =>
                {
                    upGrid.Visibility = Visibility.Hidden;
                    slideMenu.ShowMenu();
                    _isSlideMenuAnimationRunning = false;
                };

                _isSlideMenuAnimationRunning = true;
                storyBoard.Begin(this);
            }
        }

        void slideMenu_Center(object sender, EventArgs e)
        {
            MainViewController.Instance.InkableScene.RenderTransform = new MatrixTransform(Matrix.Identity);
            //MainViewController.Instance.InkableScene.ClearInput();
        }

        void slideMenu_Clear(object sender, EventArgs e)
        {
            //MainViewController.Instance.InkableScene.Clear();
            GC.Collect();
            //aPage.ClearSchemaViewer();
        }

        void slideMenu_Exit(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        void slideMenu_Down(object sender, EventArgs e)
        {
            if (!_isSlideMenuAnimationRunning)
            {
                upGrid.Visibility = Visibility.Visible;
                slideMenu.RenderTransform = new TranslateTransform(0, this.ActualHeight - slideMenu.Height - 10);
                upGrid.RenderTransform = new TranslateTransform(this.ActualWidth, this.ActualHeight - 90);

                Storyboard storyBoard = new Storyboard();
                NameScope.SetNameScope(this, new NameScope());
            
                // slide menu animation
                TranslateTransform tt = (TranslateTransform)slideMenu.RenderTransform;
                RegisterName("slideMenuTransform", tt);
                DoubleAnimation anim = new DoubleAnimation();
                anim.From = this.ActualHeight - slideMenu.Height - 10;
                anim.To = this.ActualHeight;
                anim.EasingFunction = new SineEase();
                anim.Duration = TimeSpan.FromSeconds(0.8);
                storyBoard.Children.Add(anim);
                Storyboard.SetTargetName(anim, "slideMenuTransform");
                Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.YProperty));

                // show menu button animation
                tt = (TranslateTransform)upGrid.RenderTransform;
                RegisterName("showMenuTransform", tt);
                anim = new DoubleAnimation();
                anim.From = this.ActualWidth;
                anim.To = this.ActualWidth - 95;
                anim.EasingFunction = new SineEase();
                anim.Duration = TimeSpan.FromSeconds(1.2);
                storyBoard.Children.Add(anim);
                Storyboard.SetTargetName(anim, "showMenuTransform");
                Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.XProperty));

                storyBoard.Completed += (object sender1, EventArgs e1) =>
                {
                    slideMenu.Visibility = Visibility.Hidden;
                    _isSlideMenuAnimationRunning = false;
                };

                _isSlideMenuAnimationRunning = true;
                storyBoard.Begin(this);
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Media.Matrix m = ((MatrixTransform)MainViewController.Instance.InkableScene.RenderTransform).Matrix;
            double scale = e.Delta > 0 ? 1.05 : 0.95;
            m.ScaleAtPrepend(scale, scale, e.GetPosition(MainViewController.Instance.InkableScene).X, e.GetPosition(MainViewController.Instance.InkableScene).Y);
            MainViewController.Instance.InkableScene.RenderTransform = new MatrixTransform(m);
            e.Handled = true;
        }

        private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            upGrid.RenderTransform = new TranslateTransform(this.ActualWidth - 95, this.ActualHeight - 90);
        }
    }
}
