﻿using PanoramicData.model.view;
using PanoramicData.utils;
using PanoramicData.view.inq;
using PanoramicData.view.schema;
using PanoramicData.view.table;
using PanoramicDataDBConnector;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using starPadSDK.WPFHelp;
using starPadSDK.AppLib;
using PixelLab.Common;
using PanoramicData.controller.data;
using System.IO;
using PanoramicData.controller.input;
using PanoramicData.model.data;
using PanoramicData.model.data.mssql;
using PanoramicData.model.view_new;
using PanoramicData.view.vis;
using PanoramicData.model.data.sim;
using PanoramicData.Properties;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PanoramicData.controller.view
{
    public class MainViewController
    {
        private Gesturizer _gesturizer = new Gesturizer();
        private static MainViewController _instance;

        private MainViewController(InkableScene root, MainWindow window)
        {
            _root = root;
            _window = window;

            _mainModel = new MainModel();
            foreach (var file in Directory.GetFiles(@"Resources\configs"))
            {
                _mainModel.DatasetConfigurations.Add(DatasetConfiguration.FromFile(file));
            }
            LoadData(_mainModel.DatasetConfigurations.Where(ds => ds.Name == Settings.Default.InitialDataSet).First());

            AttributeViewModel.AttributeViewModelDropped += AttributeViewModelDropped;
            AttributeViewModel.AttributeViewModelMoved += AttributeViewModelMoved;
            ResizerRadialControlExecution.Dropped += ResizerRadialControlExecution_Dropped;
            ColumnTreeView.DatabaseTableDropped += Resizer_DatabaseTableDropped;
            Colorer.ColorerDropped += ColorerDropped;
            DatabaseManager.ErrorMessageChanged += DatabaseManager_ErrorMessageChanged;
            _root.InkCollectedEvent += root_InkCollectedEvent;

            _gesturizer.AddGesture(new PanoramicData.view.inq.ScribbleGesture(_root));
        }

        public static void CreateInstance(InkableScene root, MainWindow window)
        {
            _instance = new MainViewController(root, window);
        }
        
        public static MainViewController Instance
        {
            get
            {
                return _instance;
            }
        }

        private InkableScene _root;
        public InkableScene InkableScene
        {
            get
            {
                return _root;
            }
        }

        private ObservableCollection<VisualizationViewModel> _visualizationViewModels = new ObservableCollection<VisualizationViewModel>();
        public ObservableCollection<VisualizationViewModel> VisualizationViewModels
        {
            get
            {
                return _visualizationViewModels;
            }
        }

        private MainModel _mainModel;
        public MainModel MainModel
        {
            get
            {
                return _mainModel;
            }
        }

        private SchemaModel _schemaModel;
        public SchemaModel SchemaModel
        {
            get
            {
                return _schemaModel;
            }
        }

        private SchemaViewer _schemaViewer = new SchemaViewer();

        private MainWindow _window;
        public MainWindow MainWindow
        {
            get
            {
                return _window;
            }
        }

        public void LoadData(DatasetConfiguration datasetConfiguration)
        {
            SchemaViewModel schemaViewModel = new SchemaViewModel();
            _schemaModel = null;

            if (datasetConfiguration.Backend == "MSSQL")
            {
                _schemaModel = new MSSQLSchemaModel(datasetConfiguration);
            }
            else if (datasetConfiguration.Backend == "SIM")
            {
                _schemaModel = new SimSchemaModel(datasetConfiguration);
            }

            schemaViewModel.SchemaModel = _schemaModel;            
            _schemaViewer.DataContext = schemaViewModel;
        }

        public void ShowSchemaViewer(Point pos)
        {
            SchemaViewModel model = _schemaViewer.DataContext as SchemaViewModel;
            model.Position = new Point(pos.X - model.Size.X / 2.0, pos.Y - model.Size.Y / 2.0);
            InkableScene.Add(_schemaViewer);
        }

        public VisualizationViewModel CreateVisualizationViewModel(AttributeOperationModel attributeOperationModel)
        {
            VisualizationViewModel visModel = VisualizationViewModelFactory.CreateDefault(_schemaModel, attributeOperationModel);
            _visualizationViewModels.Add(visModel);
            return visModel;
        }

        private void DatabaseManager_ErrorMessageChanged(object sender, string e)
        {
            if (e != "")
            {
                MainModel.ErrorMessage = "Failure in connecting to Database Server:" + " \n=>" + e;
            }
            else
            {
                MainModel.ErrorMessage = "";
            }
        }

        
        void AttributeViewModelMoved(object sender, AttributeViewModelEventArgs e)
        {
            HitTester hitTester = new HitTester();
            Dictionary<Type, HitTestFilterBehavior> filters = new Dictionary<Type, HitTestFilterBehavior>();
            filters.Add(typeof(AttributeViewModelEventHandler), HitTestFilterBehavior.ContinueSkipChildren);
            List<DependencyObject> hits = hitTester.GetHits(InkableScene, e.Bounds, new Type[] { typeof(AttributeViewModelEventHandler) }.ToList(), filters);

            var orderderHits = hits.Select(dep => dep as FrameworkElement)
                .OrderBy(fe => (fe.GetBounds(InkableScene).Center.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();

            foreach (var element in InkableScene.VisualDescendentsOfType<FrameworkElement>())
            {
                if (element is AttributeViewModelEventHandler)
                {
                    (element as AttributeViewModelEventHandler).AttributeViewModelMoved(
                        sender as AttributeViewModel, e,
                        hits.Count > 0 ? orderderHits[0] == element : false);
                }
            }
        }

        void AttributeViewModelDropped(object sender, AttributeViewModelEventArgs e)
        {
            HitTester hitTester = new HitTester();
            Dictionary<Type, HitTestFilterBehavior> filters = new Dictionary<Type, HitTestFilterBehavior>();
            filters.Add(typeof(AttributeViewModelEventHandler), HitTestFilterBehavior.ContinueSkipChildren);
            List<DependencyObject> hits = hitTester.GetHits(InkableScene, e.Bounds, new Type[] { typeof(AttributeViewModelEventHandler) }.ToList(), filters);

            double width = e.UseDefaultSize ? VisualizationContainerView.WIDTH : e.Bounds.Width;
            double height = e.UseDefaultSize ? VisualizationContainerView.HEIGHT : e.Bounds.Height;
            Vector2 size = new Vector2(width, height);
            Point position = new Vector2(e.Bounds.Center.X, e.Bounds.Center.Y) - size / 2.0;

            if (hits.Count > 0)
            {
                var orderderHits = hits.Select(dep => dep as FrameworkElement)
                    .OrderBy(dep => dep is ColumnTreeView)
                    .ThenBy(fe => (fe.GetBounds(InkableScene).Center.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();

                (orderderHits[0] as AttributeViewModelEventHandler).AttributeViewModelDropped(
                    sender as AttributeViewModel, e);
            }
            else
            {
                VisualizationContainerView visualizationContainerView = new VisualizationContainerView();
                visualizationContainerView.DataContext = CreateVisualizationViewModel(e.AttributeOperationModel);
                visualizationContainerView.InitPostionAndDimension(position, size);
               /* FilterHolder filter = new FilterHolder();
                FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateDefault(columnDescriptor, tableModel);
                filterHolderViewModel.Center = new Point();
                if (e.LinkFromFilterModel != null)
                {
                    filterHolderViewModel.AddIncomingFilter(e.LinkFromFilterModel, FilteringType.Filter);
                }
                filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(position, new Vec(width, height));*/
            }
        }
        private void ColorerDropped(object sender, DatabaseTableEventArgs e)
        {
            HitTester hitTester = new HitTester();
            Dictionary<Type, HitTestFilterBehavior> filters = new Dictionary<Type, HitTestFilterBehavior>();
            filters.Add(typeof(AttributeViewModelEventHandler), HitTestFilterBehavior.ContinueSkipChildren);
            List<DependencyObject> hits = hitTester.GetHits(InkableScene, e.Bounds, new Type[] { typeof(AttributeViewModelEventHandler) }.ToList(), filters);

            if (hits.Count == 0)
            {
                double width = e.DefaultSize ? VisualizationContainerView.WIDTH : e.Bounds.Width;
                double height = e.DefaultSize ? VisualizationContainerView.HEIGHT : e.Bounds.Height;
                Point position = e.Bounds.Center;
                position.X -= width / 2.0;
                position.Y -= height / 2.0;

                TableModel tableModel = e.TableModel;

                VisualizationContainerView filter = new VisualizationContainerView();
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

                filterHolderViewModel.Center = new Point(position.X + VisualizationContainerView.WIDTH / 2.0,
                    position.Y + VisualizationContainerView.HEIGHT / 2.0);
                //filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(position, new Vector2(VisualizationContainerView.WIDTH, VisualizationContainerView.HEIGHT));

                filterHolderViewModel.Color = e.FilterModel.Color;
                e.FilterModel.AddIncomingFilter(filterHolderViewModel, FilteringType.Filter, true);
            }
        }

        void Resizer_DatabaseTableDropped(object sender, DatabaseTableEventArgs e)
        {
            HitTester hitTester = new HitTester();
            Dictionary<Type, HitTestFilterBehavior> filters = new Dictionary<Type, HitTestFilterBehavior>();
            filters.Add(typeof(AttributeViewModelEventHandler), HitTestFilterBehavior.ContinueSkipChildren);
            List<DependencyObject> hits = hitTester.GetHits(InkableScene, e.Bounds, new Type[] { typeof(AttributeViewModelEventHandler) }.ToList(), filters);

            if (hits.Count == 0)
            {
                double width = e.DefaultSize ? VisualizationContainerView.WIDTH : e.Bounds.Width;
                double height = e.DefaultSize ? VisualizationContainerView.HEIGHT : e.Bounds.Height;
                Point position = e.Bounds.Center;
                position.X -= width / 2.0;
                position.Y -= height / 2.0;

                TableModel tableModel = e.TableModel;

                VisualizationContainerView filter = new VisualizationContainerView();
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

                    filterHolderViewModel.Center = new Point(position.X + VisualizationContainerView.WIDTH / 2.0,
                        position.Y + VisualizationContainerView.HEIGHT / 2.0);
                   // filter.FilterHolderViewModel = filterHolderViewModel;
                    filter.InitPostionAndDimension(position, new Vector2(VisualizationContainerView.WIDTH, VisualizationContainerView.HEIGHT));
                }
            }
        }

        void ResizerRadialControlExecution_Dropped(object sender, AttributeViewModelEventArgs e)
        {
            if (e.Type == AttributeViewModelEventArgType.Copy)
            {
                /*FilterHolder filter = new FilterHolder();
                FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateCopy(e.FilterModel);
                filterHolderViewModel.Center = new Point();
                filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(e.Bounds.TopLeft, new Vec(e.Bounds.Width, e.Bounds.Height));*/
            }
            else if (e.Type == AttributeViewModelEventArgType.Snapshot)
            {
                /*FilterHolder filter = new FilterHolder();
                filter.FilterHolderViewModel = (FilterHolderViewModel)e.FilterModel;
                filter.InitPostionAndDimension(e.Bounds.TopLeft, new Vec(e.Bounds.Width, e.Bounds.Height));*/
            }
        }


        void root_InkCollectedEvent(object sender, InkCollectedEventArgs e)
        {
            IList<IGesture> recognizedGestures = _gesturizer.Recognize(e.InkStroke.Clone());

            foreach (IGesture recognizedGesture in recognizedGestures.ToList())
            {
                if (recognizedGesture is PanoramicData.view.inq.ScribbleGesture)
                {
                    PanoramicData.view.inq.ScribbleGesture scribble = recognizedGesture as PanoramicData.view.inq.ScribbleGesture;
                    foreach (IScribbable hitScribbable in scribble.HitScribbables)
                    {
                        if (hitScribbable is InkStroke)
                        {
                            _root.Remove(hitScribbable as InkStroke);
                        }
                    }
                }
            }

            if (recognizedGestures.Count == 0)
            {
                _root.Add(e.InkStroke);
            }
        }
    }
}
