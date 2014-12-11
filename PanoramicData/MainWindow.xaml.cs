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
using OxyPlot.Wpf;
using Path = System.Windows.Shapes.Path;
using PanoramicData.view.filter;
using PanoramicData.view.table;
using PanoramicData.utils.inq;
using PanoramicData.view.schema;
using PanoramicData.model.view;
using PanoramicData.controller.data;
using PanoramicData.controller.physics;
using PanoramicData.Properties;
using PanoramicData.utils;
using CombinedInputAPI;

namespace PanoramicData
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // unused static fields to load assemblies in the begining
        // instead of when they are actually used the first time.
        private static InqAnalyzer ia = new InqAnalyzer();
        private bool _renderTouchPoints = false;
        private Dictionary<TouchDevice, FrameworkElement> _deviceRenderings = new Dictionary<TouchDevice, FrameworkElement>();

        List<DependencyObject> _columnHeaderEventHandlerHitTestResults = new List<DependencyObject>();

        private bool _animationRunning = false;

        public static MainWindow CurrentInstance = null;

        public MainWindow()
        {
            //Thread.Sleep(1000);
            InitializeComponent();

            MouseTouchDevice.RegisterEvents(this);

            CurrentInstance = this;

            layoutRoot.PreviewTouchDown += layoutRoot_PreviewTouchDown;
            layoutRoot.PreviewTouchMove += layoutRoot_PreviewTouchMove;
            layoutRoot.PreviewTouchUp += layoutRoot_PreviewTouchUp;

            up.TouchDown += up_TouchDown;
            slideMenu.Down += slideMenu_Down;
            slideMenu.Exit += slideMenu_Exit;
            slideMenu.Clear += slideMenu_Clear;
            slideMenu.Center += slideMenu_Center;
            slideMenu.Export += slideMenu_Export;
            slideMenu.NBA += nba_DBSearch;
            slideMenu.Census += census_DBSearch;
            slideMenu.CSFaculty += csFaculty_DBSearch;
            slideMenu.Basball += baseball_DBSearch;
            slideMenu.Titanic += titanic_DBSearch;
            slideMenu.Hua1 += hua1_DBSearch;
            slideMenu.Hua2 += hua2_DBSearch;

            SimpleGridViewColumnHeader.Dropped += SimpleGridViewColumnHeader_Dropped;
            SimpleGridViewColumnHeader.Moved += SimpleGridViewColumnHeader_Moved;
            ResizerRadialControlExecution.Dropped += ResizerRadialControlExecution_Dropped;
            ColumnTreeView.DatabaseTableDropped += Resizer_DatabaseTableDropped;
            Colorer.ColorerDropped += ColorerDropped;
            DatabaseManager.ErrorMessageChanged += DatabaseManager_ErrorMessageChanged;

            //_speechRecognizer.Activated += _speechRecognizer_Activated;
            //_speechRecognizer.Deactivated += _speechRecognizer_Deactivated;
            //_speechRecognizer.Start();

            //FilterManager.Instance.Initialize(aPage);

            debugGrid.Visibility = Visibility.Hidden;
            mic.Visibility = Visibility.Hidden;

            // init view titanic dataset;
            loadTitanicData();
            //loadHuaData();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PhysicsController.SetRootCanvas(mainGrid);
            PhysicsController.InqScene = this.aPage;
            var d = PhysicsController.Instance; // dummy first call 
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            ResourceManager.WriteToFile();
        }

        void DatabaseManager_ErrorMessageChanged(object sender, string e)
        {
            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (e == "")
                {
                    errorGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    errorGrid.Visibility = Visibility.Visible;
                    errorMsg.Text = "Failure in connecting to Database Server:" + " \n=>" + e;
                }
            }));
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

        protected override void OnManipulationBoundaryFeedback(ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        void _speechRecognizer_Deactivated(object sender, EventArgs e)
        {
            mic.Visibility = Visibility.Hidden;
        }

        void _speechRecognizer_Activated(object sender, EventArgs e)
        {
            mic.Visibility = Visibility.Visible;
        }

        private HitTestResultBehavior columnHeaderEventHandlerHitTestResult(HitTestResult result)
        {
            if (result.VisualHit is ColumnHeaderEventHandler)
            {
                _columnHeaderEventHandlerHitTestResults.Add(result.VisualHit);
            }
            return HitTestResultBehavior.Continue;
        }

        private HitTestFilterBehavior columnHeaderEventHandlerHitTestFilter(DependencyObject o)
        {
            if (o.GetType() == typeof(ColumnHeaderEventHandler))
            {
                return HitTestFilterBehavior.ContinueSkipChildren;
            }
            else if (o.GetType() == typeof(Plot))
            {
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            }
            else if (o.GetType() == typeof(Plotter))
            {
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            } 
            else
            {
                return HitTestFilterBehavior.Continue;
            }
        }

        void SimpleGridViewColumnHeader_Moved(object sender, ColumnHeaderEventArgs e)
        {
            _columnHeaderEventHandlerHitTestResults.Clear();

            RectangleGeometry rectGeo = new RectangleGeometry(e.Bounds);

            VisualTreeHelper.HitTest(aPage,
                new HitTestFilterCallback(columnHeaderEventHandlerHitTestFilter),
                new HitTestResultCallback(columnHeaderEventHandlerHitTestResult),
                new GeometryHitTestParameters(rectGeo));

            var orderderHits = _columnHeaderEventHandlerHitTestResults.Select(dep => dep as FrameworkElement)
                .OrderBy(fe => (fe.GetBounds(aPage).Center.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();

            foreach (var element in aPage.VisualDescendentsOfType<FrameworkElement>())
            {
                if (element is ColumnHeaderEventHandler)
                {
                    (element as ColumnHeaderEventHandler).ColumnHeaderMoved(
                        sender as SimpleGridViewColumnHeader, e,
                        _columnHeaderEventHandlerHitTestResults.Count > 0 ? orderderHits[0] == element : false);
                }
            }
        }

        void SimpleGridViewColumnHeader_Dropped(object sender, ColumnHeaderEventArgs e)
        {
            _columnHeaderEventHandlerHitTestResults.Clear();

            RectangleGeometry rectGeo = new RectangleGeometry(e.Bounds);

            VisualTreeHelper.HitTest(aPage,
                new HitTestFilterCallback(columnHeaderEventHandlerHitTestFilter),
                new HitTestResultCallback(columnHeaderEventHandlerHitTestResult),
                new GeometryHitTestParameters(rectGeo));

            PanoramicDataColumnDescriptor columnDescriptor = e.ColumnDescriptor;
            double width = e.DefaultSize ? FilterHolder.WIDTH : e.Bounds.Width;
            double height = e.DefaultSize ? FilterHolder.HEIGHT : e.Bounds.Height;
            Pt position = e.Bounds.Center;
            position.X -= width / 2.0;
            position.Y -= height / 2.0;
            TableModel tableModel = e.TableModel;
            if (tableModel == null)
            {
                if (e.FilterModel == null)
                {
                    return;
                }
                tableModel = e.FilterModel.TableModel;
            }

            if (_columnHeaderEventHandlerHitTestResults.Count > 0)
            {
                var orderderHits = _columnHeaderEventHandlerHitTestResults.Select(dep => dep as FrameworkElement)
                    .OrderBy(dep => dep is ColumnTreeView)
                    .ThenBy(fe => (fe.GetBounds(aPage).Center.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();

                (orderderHits[0] as ColumnHeaderEventHandler).ColumnHeaderDropped(
                    sender as SimpleGridViewColumnHeader, e);
            }
            else
            {
                FilterHolder filter = new FilterHolder(aPage);
                FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateDefault(columnDescriptor, tableModel);
                filterHolderViewModel.Center = new Point();
                if (e.LinkFromFilterModel != null)
                {
                    filterHolderViewModel.AddIncomingFilter(e.LinkFromFilterModel, FilteringType.Filter);
                }
                filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(position, new Vec(width, height));
            }
        }

        private void ColorerDropped(object sender, DatabaseTableEventArgs e)
        {
            _columnHeaderEventHandlerHitTestResults.Clear();

            RectangleGeometry rectGeo = new RectangleGeometry(e.Bounds);

            VisualTreeHelper.HitTest(aPage,
                new HitTestFilterCallback(columnHeaderEventHandlerHitTestFilter),
                new HitTestResultCallback(columnHeaderEventHandlerHitTestResult),
                new GeometryHitTestParameters(rectGeo));

            if (_columnHeaderEventHandlerHitTestResults.Count == 0)
            {
                double width = e.DefaultSize ? FilterHolder.WIDTH : e.Bounds.Width;
                double height = e.DefaultSize ? FilterHolder.HEIGHT : e.Bounds.Height;
                Pt position = e.Bounds.Center;
                position.X -= width / 2.0;
                position.Y -= height / 2.0;

                TableModel tableModel = e.TableModel;

                FilterHolder filter = new FilterHolder(aPage);
                FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
                filterHolderViewModel.FilterRendererType = FilterRendererType.Table;
                filterHolderViewModel.TableModel = tableModel;

                foreach (var colorCd in e.FilterModel.GetColumnDescriptorsForOption(Option.ColorBy))
                {
                    filterHolderViewModel.AddOptionColumnDescriptor(Option.X, (PanoramicDataColumnDescriptor)colorCd.SimpleClone());
                }
                foreach (var colorCd in e.FilterModel.GetColumnDescriptorsForOption(Option.ColorBy))
                {
                    var cd = (PanoramicDataColumnDescriptor)colorCd.Clone();
                    cd.IsGrouped = true;
                    filterHolderViewModel.AddOptionColumnDescriptor(Option.ColorBy, cd);
                }

                filterHolderViewModel.Center = new Point(position.X + FilterHolder.WIDTH / 2.0,
                    position.Y + FilterHolder.HEIGHT / 2.0);
                filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(position, new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT));

                filterHolderViewModel.Color = e.FilterModel.Color;
                e.FilterModel.AddIncomingFilter(filterHolderViewModel, FilteringType.Filter, true);
            }
        }

        void Resizer_DatabaseTableDropped(object sender, DatabaseTableEventArgs e)
        {
            _columnHeaderEventHandlerHitTestResults.Clear();
           
            RectangleGeometry rectGeo = new RectangleGeometry(e.Bounds);

            VisualTreeHelper.HitTest(aPage,
                new HitTestFilterCallback(columnHeaderEventHandlerHitTestFilter),
                new HitTestResultCallback(columnHeaderEventHandlerHitTestResult),
                new GeometryHitTestParameters(rectGeo));

            if (_columnHeaderEventHandlerHitTestResults.Count == 0)
            {
                double width = e.DefaultSize ? FilterHolder.WIDTH : e.Bounds.Width;
                double height = e.DefaultSize ? FilterHolder.HEIGHT : e.Bounds.Height;
                Pt position = e.Bounds.Center;
                position.X -= width / 2.0;
                position.Y -= height / 2.0;

                TableModel tableModel = e.TableModel;

                FilterHolder filter = new FilterHolder(aPage);
                FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
                filterHolderViewModel.FilterRendererType = FilterRendererType.Pivot;
                filterHolderViewModel.TableModel = tableModel;


                PanoramicDataGroupDescriptor groupDescriptor = tableModel.ColumnDescriptors.Keys.First();
                if (groupDescriptor is PathInfo)
                {
                    PathInfo pi = groupDescriptor as PathInfo;
                    TableInfo root = pi.TableInfo;
                    if (pi.Path.Count > 0)
                    {
                        root = pi.Path.First().FromTableInfo;
                    }
                    List<PathInfo> pathInfos = tableModel.CalculateRecursivePathInfos();

                    foreach (var pp in pathInfos)
                    {
                        if (pp.Path.Count > 0)
                        {
                            Pivot p = new Pivot();
                            p.Label = pp.GetLabel();
                            p.Selected = false;
                            p.ColumnDescriptor = new DatabaseColumnDescriptor(pp.TableInfo.PrimaryKeyFieldInfo, pp);
                            filterHolderViewModel.AddPivot(p, this);
                        }
                    }

                    filterHolderViewModel.Center = new Point(position.X + FilterHolder.WIDTH/2.0,
                        position.Y + FilterHolder.HEIGHT/2.0);
                    filter.FilterHolderViewModel = filterHolderViewModel;
                    filter.InitPostionAndDimension(position, new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT));
                }
            }
        }

        void ResizerRadialControlExecution_Dropped(object sender, ColumnHeaderEventArgs e)
        {
            if (e.Command == ColumnHeaderEventArgsCommand.Copy)
            {
                FilterHolder filter = new FilterHolder(aPage);
                FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateCopy(e.FilterModel);
                filterHolderViewModel.Center = new Point();
                filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(e.Bounds.TopLeft, new Vec(e.Bounds.Width, e.Bounds.Height));
            }
            else if (e.Command == ColumnHeaderEventArgsCommand.Snapshot)
            {
                FilterHolder filter = new FilterHolder(aPage);
                filter.FilterHolderViewModel = (FilterHolderViewModel) e.FilterModel;
                filter.InitPostionAndDimension(e.Bounds.TopLeft, new Vec(e.Bounds.Width, e.Bounds.Height));
            }
        }

        private void layoutRoot_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Properties.Settings.Default.PanoramicDataEnableKeyboardShortcuts)
            {
                return;
            }

            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                return;
            }
            if (Key.Z == e.Key)
            {
                aPage.Background = Brushes.White;
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
                Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(100, 100), aPage);

                FilterHolder filter = new FilterHolder(aPage);
                FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
                filterHolderViewModel.FilterRendererType = FilterRendererType.Pivot;
               
                filterHolderViewModel.Center = new Point(position.X + FilterHolder.WIDTH / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
                filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(position, new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT));
            }
            if (Key.N == e.Key)
            {
                loadNbaData();
            }
        }

        private void loadTitanicData()
        {
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("titanic", "passenger");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] {playerPath},
                new string[][] {new string[] {"name", "passenger_class", "survived", "sex", "age", "home"}});

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(300, 400), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }

        private void loadHua1Data()
        {
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("hua", "subject_sessions");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "block", "trial" } });

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(300, 400), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }

        private void loadHua2Data()
        {
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("hua2", "within_subject_pairs");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "block", "trial" } });

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(300, 400), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }

        private void loadCoffeeData()
        {
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("coffee", "coffee_sales");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "sales_date", "sales", "profit" } });

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(300, 400), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            // filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }

        private void loadCensusData()
        {
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("census", "census");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "age", "education", "martial_status" } });

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(300, 400), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }

        private void loadFacultyData()
        {
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("faculty", "cs_faculty");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "Rank" } });

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(300, 400), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }

        private void loadBaseballData()
        {
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("lahman", "fact");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "year" } });

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(300, 400), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }

        private void loadTipData()
        {
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("tip", "tip");
            //TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
            //    new string[][] { new string[] { "weight", "weight" } });
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { playerPath },
                new string[][] { new string[] { "tip", "total_bill", "percentage" } });

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(300, 400), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);
            filterHolderViewModel.Center = new Point(position.X + 650 / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(650, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }


        public void loadNbaData()
        {
            PathInfo gameLogPath = ModelHelpers.GeneratePathInfo("nba", "game_log");
            PathInfo playerPath = ModelHelpers.GeneratePathInfo("nba", "game_log", "player");
            PathInfo teamPath = ModelHelpers.GeneratePathInfo("nba", "game_log", "team");
            TableModel theModel = ModelHelpers.GenerateTableModel(new PathInfo[] { gameLogPath, playerPath },
                new string[][] { new string[] { "pts", "mp" }, new string[] { "name" } });

            Pt position = MainWindow.CurrentInstance.TranslatePoint(new Point(100, 100), aPage);

            FilterHolder filter = new FilterHolder(aPage);
            FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateTable(theModel);

            //filterHolderViewModel.Center = new Point(position.X + FilterHolder.WIDTH / 2.0, position.Y + FilterHolder.HEIGHT / 2.0);
            filterHolderViewModel.FilterRendererType = FilterRendererType.Table;
            filter.FilterHolderViewModel = filterHolderViewModel;

            //filter.InitPostionAndDimension(position, new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT));

            aPage.SetSchemaViewerFilterModel(filterHolderViewModel);
        }

        void up_TouchDown(object sender, TouchEventArgs e)
        {
            if (!_animationRunning)
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
                    _animationRunning = false;
                };

                _animationRunning = true;
                storyBoard.Begin(this);
            }
        }

        void slideMenu_Center(object sender, EventArgs e)
        {
            aPage.RenderTransform = new MatrixTransform(Matrix.Identity);
            aPage.ClearInput();
        }

        void slideMenu_Clear(object sender, EventArgs e)
        {
            aPage.Clear();
            GC.Collect();
            //aPage.ClearSchemaViewer();
        }

        void slideMenu_Exit(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        void titanic_DBSearch(object sender, EventArgs e)
        {
            loadTitanicData();
        }

        void nba_DBSearch(object sender, EventArgs e)
        {
            loadNbaData();
        }

        void hua1_DBSearch(object sender, EventArgs e)
        {
            loadHua1Data();
        }

        void hua2_DBSearch(object sender, EventArgs e)
        {
            loadHua2Data();
        }

        private void csFaculty_DBSearch(object sender, EventArgs e)
        {
            loadFacultyData();
        }

        void census_DBSearch(object sender, EventArgs e)
        {
            loadCensusData();
        }

        void baseball_DBSearch(object sender, EventArgs e)
        {
            loadBaseballData();
        }

        void slideMenu_Export(object sender, EventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog1.Filter = "PowerPoint |*.pptx";
            saveFileDialog1.Title = "Export as PowerPoint";
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (saveFileDialog1.FileName != "")
            {
                // filter stuff (e.g., out of bounds) and calculate scale factor
                Pt p1 = MainWindow.CurrentInstance.TranslatePoint(new Point(0, 0), aPage);
                Pt p2 = MainWindow.CurrentInstance.TranslatePoint(new Point(MainWindow.CurrentInstance.ActualWidth, MainWindow.CurrentInstance.ActualHeight), aPage);

                Rct viewPointRct = new Rct(p1, p2);
                GeoAPI.Geometries.IGeometry geom = viewPointRct.GetPolygon();

                List<Serialization.Model> models = new List<Serialization.Model>();

                foreach (var elem in aPage.Elements)
                {
                    List<Pt> pts = new List<Pt>();
                    pts.Add(elem.TranslatePoint(new System.Windows.Point(0, 0), aPage));
                    pts.Add(elem.TranslatePoint(new System.Windows.Point(elem.ActualWidth, 0), aPage));
                    pts.Add(elem.TranslatePoint(new System.Windows.Point(elem.ActualWidth, elem.ActualHeight), aPage));
                    pts.Add(elem.TranslatePoint(new System.Windows.Point(0, elem.ActualHeight), aPage));
                   
                    // check if the element is visible
                    if (geom.Intersects(pts.GetPolygon()) && (elem is Serialization.Serializable))
                    {
                        models.Add((elem as Serialization.Serializable).GetModel());
                    }
                }

                try
                {
                    Serialization.Serializer.ExportToPowerPoint(
                        saveFileDialog1.FileName,
                        @"C:\Temp\test.pptx",
                        viewPointRct,
                        models);
                }
                catch (Serialization.SerializationException se)
                {
                    MessageBox.Show(se.Message);
                }
            }
        }

        void slideMenu_Down(object sender, EventArgs e)
        {
            if (!_animationRunning)
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
                    _animationRunning = false;
                };

                _animationRunning = true;
                storyBoard.Begin(this);
            }
        }

        private string createImageFromWindow()
        {
            // Save current canvas transform
            Transform transform = aPage.LayoutTransform;

            // reset current transform (in case it is scaled or rotated)
            this.LayoutTransform = null;

            // hide stuff
            Visibility slideMenuVis = slideMenu.Visibility;
            slideMenu.Visibility = Visibility.Hidden;
            Visibility upGridVis = upGrid.Visibility;
            upGrid.Visibility = Visibility.Hidden;


            // Get the size of canvas
            Size size = new Size(this.Width, this.Height);
            // Measure and arrange the surface
            // VERY IMPORTANT
            this.Measure(size);
            this.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap =
              new RenderTargetBitmap(
                (int)size.Width,
                (int)size.Height,
                96d,
                96d,
                PixelFormats.Pbgra32);
            renderBitmap.Render(this);

            // Create a file stream for saving image
            string filename = System.IO.Path.GetTempPath() + "test.png";
            using (FileStream outStream = new FileStream(filename, FileMode.Create))
            {
                // Use png encoder for our data
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                // push the rendered bitmap to it
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                // save the data to the stream
                encoder.Save(outStream);
            }

            // Restore previously saved layout
            this.LayoutTransform = transform;
            slideMenu.Visibility = slideMenuVis;
            upGrid.Visibility = upGridVis;

            return filename;
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Media.Matrix m = ((MatrixTransform)aPage.RenderTransform).Matrix;
            double scale = e.Delta > 0 ? 1.05 : 0.95;
            m.ScaleAtPrepend(scale, scale, e.GetPosition(aPage).X, e.GetPosition(aPage).Y);
            aPage.RenderTransform = new MatrixTransform(m);
            e.Handled = true;
        }

        private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            upGrid.RenderTransform = new TranslateTransform(this.ActualWidth - 95, this.ActualHeight - 90);
        }

        private void btnClose_Click_1(object sender, RoutedEventArgs e)
        {
            debugGrid.Visibility = Visibility.Hidden;
        }
    }
}
