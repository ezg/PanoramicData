using PanoramicData.model.view;
using PanoramicData.utils;
using PanoramicData.view.filter;
using PanoramicData.view.inq;
using PanoramicData.view.schema;
using PanoramicData.view.table;
using PanoramicDataDBConnector;
using PanoramicDataModel;
using starPadSDK.Geom;
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

namespace PanoramicData.controller.view
{
    public class MainViewController
    {
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
            LoadData(_mainModel.DatasetConfigurations[0]);

            SimpleGridViewColumnHeader.Dropped += SimpleGridViewColumnHeader_Dropped;
            SimpleGridViewColumnHeader.Moved += SimpleGridViewColumnHeader_Moved;
            ResizerRadialControlExecution.Dropped += ResizerRadialControlExecution_Dropped;
            ColumnTreeView.DatabaseTableDropped += Resizer_DatabaseTableDropped;
            Colorer.ColorerDropped += ColorerDropped;
            DatabaseManager.ErrorMessageChanged += DatabaseManager_ErrorMessageChanged;
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

        private MainModel _mainModel;
        public MainModel MainModel
        {
            get
            {
                return _mainModel;
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

            PathInfo pathInfo = ModelHelpers.GeneratePathInfo(datasetConfiguration.Schema, datasetConfiguration.Table);
            TableModel tableModel = ModelHelpers.GenerateTableModel(
                new PathInfo[] { pathInfo },
                new string[][] { new string[] {  } });

            schemaViewModel.TableModel = tableModel;
            _schemaViewer.DataContext = schemaViewModel;
        }

        public void ShowSchemaViewer(Pt pos)
        {
            SchemaViewModel model = _schemaViewer.DataContext as SchemaViewModel;
            model.Position = new Pt(pos.X - model.Size.X / 2.0, pos.Y - model.Size.Y / 2.0);
            InkableScene.Add(_schemaViewer);
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

        
        void SimpleGridViewColumnHeader_Moved(object sender, ColumnHeaderEventArgs e)
        {
            HitTester hitTester = new HitTester();
            Dictionary<Type, HitTestFilterBehavior> filters = new Dictionary<Type, HitTestFilterBehavior>();
            filters.Add(typeof(ColumnHeaderEventHandler), HitTestFilterBehavior.ContinueSkipChildren);
            List<DependencyObject> hits = hitTester.GetHits(InkableScene, e.Bounds, new Type[] { typeof(ColumnHeaderEventHandler) }.ToList(), filters);

            var orderderHits = hits.Select(dep => dep as FrameworkElement)
                .OrderBy(fe => (fe.GetBounds(InkableScene).Center.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();

            foreach (var element in InkableScene.VisualDescendentsOfType<FrameworkElement>())
            {
                if (element is ColumnHeaderEventHandler)
                {
                    (element as ColumnHeaderEventHandler).ColumnHeaderMoved(
                        sender as SimpleGridViewColumnHeader, e,
                        hits.Count > 0 ? orderderHits[0] == element : false);
                }
            }
        }

        void SimpleGridViewColumnHeader_Dropped(object sender, ColumnHeaderEventArgs e)
        {
            HitTester hitTester = new HitTester();
            Dictionary<Type, HitTestFilterBehavior> filters = new Dictionary<Type, HitTestFilterBehavior>();
            filters.Add(typeof(ColumnHeaderEventHandler), HitTestFilterBehavior.ContinueSkipChildren);
            List<DependencyObject> hits = hitTester.GetHits(InkableScene, e.Bounds, new Type[] { typeof(ColumnHeaderEventHandler) }.ToList(), filters);

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

            if (hits.Count > 0)
            {
                var orderderHits = hits.Select(dep => dep as FrameworkElement)
                    .OrderBy(dep => dep is ColumnTreeView)
                    .ThenBy(fe => (fe.GetBounds(InkableScene).Center.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();

                (orderderHits[0] as ColumnHeaderEventHandler).ColumnHeaderDropped(
                    sender as SimpleGridViewColumnHeader, e);
            }
            else
            {
                FilterHolder filter = new FilterHolder();
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
            HitTester hitTester = new HitTester();
            Dictionary<Type, HitTestFilterBehavior> filters = new Dictionary<Type, HitTestFilterBehavior>();
            filters.Add(typeof(ColumnHeaderEventHandler), HitTestFilterBehavior.ContinueSkipChildren);
            List<DependencyObject> hits = hitTester.GetHits(InkableScene, e.Bounds, new Type[] { typeof(ColumnHeaderEventHandler) }.ToList(), filters);

            if (hits.Count == 0)
            {
                double width = e.DefaultSize ? FilterHolder.WIDTH : e.Bounds.Width;
                double height = e.DefaultSize ? FilterHolder.HEIGHT : e.Bounds.Height;
                Pt position = e.Bounds.Center;
                position.X -= width / 2.0;
                position.Y -= height / 2.0;

                TableModel tableModel = e.TableModel;

                FilterHolder filter = new FilterHolder();
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
            HitTester hitTester = new HitTester();
            Dictionary<Type, HitTestFilterBehavior> filters = new Dictionary<Type, HitTestFilterBehavior>();
            filters.Add(typeof(ColumnHeaderEventHandler), HitTestFilterBehavior.ContinueSkipChildren);
            List<DependencyObject> hits = hitTester.GetHits(InkableScene, e.Bounds, new Type[] { typeof(ColumnHeaderEventHandler) }.ToList(), filters);

            if (hits.Count == 0)
            {
                double width = e.DefaultSize ? FilterHolder.WIDTH : e.Bounds.Width;
                double height = e.DefaultSize ? FilterHolder.HEIGHT : e.Bounds.Height;
                Pt position = e.Bounds.Center;
                position.X -= width / 2.0;
                position.Y -= height / 2.0;

                TableModel tableModel = e.TableModel;

                FilterHolder filter = new FilterHolder();
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

                    filterHolderViewModel.Center = new Point(position.X + FilterHolder.WIDTH / 2.0,
                        position.Y + FilterHolder.HEIGHT / 2.0);
                    filter.FilterHolderViewModel = filterHolderViewModel;
                    filter.InitPostionAndDimension(position, new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT));
                }
            }
        }

        void ResizerRadialControlExecution_Dropped(object sender, ColumnHeaderEventArgs e)
        {
            if (e.Command == ColumnHeaderEventArgsCommand.Copy)
            {
                FilterHolder filter = new FilterHolder();
                FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateCopy(e.FilterModel);
                filterHolderViewModel.Center = new Point();
                filter.FilterHolderViewModel = filterHolderViewModel;
                filter.InitPostionAndDimension(e.Bounds.TopLeft, new Vec(e.Bounds.Width, e.Bounds.Height));
            }
            else if (e.Command == ColumnHeaderEventArgsCommand.Snapshot)
            {
                FilterHolder filter = new FilterHolder();
                filter.FilterHolderViewModel = (FilterHolderViewModel)e.FilterModel;
                filter.InitPostionAndDimension(e.Bounds.TopLeft, new Vec(e.Bounds.Width, e.Bounds.Height));
            }
        }
    }
}
