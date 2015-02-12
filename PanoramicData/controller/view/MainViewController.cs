using PanoramicData.model.view;
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
using PanoramicData.model.view;
using PanoramicData.view.vis;
using PanoramicData.model.data.sim;
using PanoramicData.Properties;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PanoramicData.controller.physics;

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
            DatabaseManager.ErrorMessageChanged += DatabaseManager_ErrorMessageChanged;
            _root.InkCollectedEvent += root_InkCollectedEvent;
            VisualizationViewModels.CollectionChanged += VisualizationViewModels_CollectionChanged;

            _gesturizer.AddGesture(new PanoramicData.view.inq.ConnectGesture(_root));
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

        private ObservableCollection<LinkViewModel> _linkViewModels = new ObservableCollection<LinkViewModel>();
        public ObservableCollection<LinkViewModel> LinkViewModels
        {
            get
            {
                return _linkViewModels;
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

        public void RemoveVisualizationViewModel(VisualizationContainerView visualizationContainerView)
        {
            _visualizationViewModels.Remove(visualizationContainerView.DataContext as VisualizationViewModel);
            PhysicsController.Instance.RemovePhysicalObject(visualizationContainerView);
            MainViewController.Instance.InkableScene.Remove(visualizationContainerView);
        }

        public LinkViewModel CreateLinkViewModel(LinkModel linkModel)
        {
            LinkViewModel linkViewModel = LinkViewModels.FirstOrDefault(lvm => lvm.ToVisualizationViewModel == VisualizationViewModels.Where(vvm => vvm.QueryModel == linkModel.ToQueryModel).First());
            if (linkViewModel == null)
            {
                linkViewModel = new LinkViewModel()
                {
                    ToVisualizationViewModel = VisualizationViewModels.Where(vvm => vvm.QueryModel == linkModel.ToQueryModel).First(),
                };
                _linkViewModels.Add(linkViewModel);
                LinkView linkView = new LinkView();
                linkView.DataContext = linkViewModel;
                _root.AddToBack(linkView);
            }
            if (!linkViewModel.LinkModels.Contains(linkModel))
            {
                linkViewModel.LinkModels.Add(linkModel);
                linkViewModel.FromVisualizationViewModels.Add(VisualizationViewModels.Where(vvm => vvm.QueryModel == linkModel.FromQueryModel).First());
            }
            
            return linkViewModel;
        }

        public void RemoveLinkViewModel(LinkModel linkModel)
        {
            foreach (var linkViewModel in LinkViewModels.ToArray()) 
            {
                if (linkViewModel.LinkModels.Contains(linkModel))
                {
                    linkViewModel.LinkModels.Remove(linkModel);
                }
                if (linkViewModel.LinkModels.Count == 0)
                {
                    LinkViewModels.Remove(linkViewModel);
                    _root.Remove(_root.Elements.First(e => e is LinkView && (e as LinkView).DataContext == linkViewModel));
                }
            }
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

        void VisualizationViewModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    (item as VisualizationViewModel).QueryModel.LinkModels.CollectionChanged -= LinkModels_CollectionChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    (item as VisualizationViewModel).QueryModel.LinkModels.CollectionChanged += LinkModels_CollectionChanged;
                }
            }
        }

        void LinkModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    RemoveLinkViewModel(item as LinkModel);
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    CreateLinkViewModel(item as LinkModel);
                }
            }
        }

        void root_InkCollectedEvent(object sender, InkCollectedEventArgs e)
        {
            IList<IGesture> recognizedGestures = _gesturizer.Recognize(e.InkStroke.Clone());

            foreach (IGesture recognizedGesture in recognizedGestures.ToList())
            {
                if (recognizedGesture is PanoramicData.view.inq.ConnectGesture)
                {
                    PanoramicData.view.inq.ConnectGesture connect = recognizedGesture as PanoramicData.view.inq.ConnectGesture;
                    LinkModel linkModel = new LinkModel()
                    {
                        FromQueryModel= connect.FromVisualizationViewModel.QueryModel,
                        ToQueryModel = connect.ToVisualizationViewModel.QueryModel
                    };
                    if (!linkModel.FromQueryModel.LinkModels.Contains(linkModel) &&
                        !linkModel.ToQueryModel.LinkModels.Contains(linkModel))
                    {
                        linkModel.FromQueryModel.LinkModels.Add(linkModel);
                        linkModel.ToQueryModel.LinkModels.Add(linkModel);
                    }
                }
                else if (recognizedGesture is PanoramicData.view.inq.ScribbleGesture)
                {
                    PanoramicData.view.inq.ScribbleGesture scribble = recognizedGesture as PanoramicData.view.inq.ScribbleGesture;
                    foreach (IScribbable hitScribbable in scribble.HitScribbables)
                    {
                        if (hitScribbable is InkStroke)
                        {
                            _root.Remove(hitScribbable as InkStroke);
                        }
                        else if (hitScribbable is VisualizationContainerView)
                        {
                            RemoveVisualizationViewModel(hitScribbable as VisualizationContainerView);
                        }
                        else if (hitScribbable is LinkView)
                        {
                            List<LinkModel> models = (hitScribbable as LinkView).GetLinkModelsToRemove(e.InkStroke.Geometry);
                            foreach (var model in models)
                            {
                                model.FromQueryModel.LinkModels.Remove(model);
                                model.ToQueryModel.LinkModels.Remove(model);
                            }
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
