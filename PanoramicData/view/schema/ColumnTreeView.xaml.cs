using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CombinedInputAPI;
using PanoramicDataModel;
using PixelLab.Common;
using PanoramicData.model.view;
using PanoramicData.view.table;
using PanoramicData.view.inq;
using PanoramicData.model.data;
using PanoramicData.model.view;
using PanoramicData.view.vis;

namespace PanoramicData.view.schema
{
    /// <summary>
    /// Interaction logic for ColumnTreeView.xaml
    /// </summary>
    public partial class ColumnTreeView : UserControl, AttributeViewModelEventHandler
    {
        public SchemaViewModel SchemaViewModel { get; set; }

        private Point _treeViewStartDrag = new Point(0, 0);
        private Border _treeViewShadow = null;

        public ColumnTreeView()
        {
            InitializeComponent();
            tree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnTreeItemExpanded));
        }

        public int ItemsCount()
        {
            return tree.Items.Count;
        }

        public void InitTree(SchemaViewModel schemaViewModel)
        {
            SchemaViewModel = schemaViewModel;
            tree.Items.Clear();

            DatabaseRootOrgItem databaseRoot = new DatabaseRootOrgItem(SchemaViewModel.SchemaModel, "Tables");
            TreeViewItem item = new TreeViewItem();
            item.Header = databaseRoot;
            item.HeaderTemplate = FindResource("DatabaseRootTemplate") as DataTemplate;
            item.Items.Add(null);
            tree.Items.Add(item);

            if (SchemaViewModel.SchemaModel.NamedAttributeModels.Count > 0)
            {
                NamedAttributeRootOrgItem customFieldsRoot = new NamedAttributeRootOrgItem(SchemaViewModel.SchemaModel, "Custom Groups");
                item = new TreeViewItem();
                item.Header = customFieldsRoot;
                item.HeaderTemplate = FindResource("NamedAttributeRootTemplate") as DataTemplate;
                item.Items.Add(null);
                tree.Items.Add(item);
            }
            if (SchemaViewModel.SchemaModel.CalculatedAttributeModels.Count > 0)
            {
                CaclculatedAttributeRootOrgItem customFieldsRoot = new CaclculatedAttributeRootOrgItem(SchemaViewModel.SchemaModel, "Caclculated Fields");
                item = new TreeViewItem();
                item.Header = customFieldsRoot;
                item.HeaderTemplate = FindResource("CaclculatedAttributeRootTemplate") as DataTemplate;
                item.Items.Add(null);
                tree.Items.Add(item);
            }
        }

        static DependencyObject VisualUpwardSearch<T>(DependencyObject source)
        {
            while (source != null && source.GetType() != typeof(T))
                source = VisualTreeHelper.GetParent(source);

            return source;
        }


        private void OnTreeItemExpanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            OrgItem orgItem = item.Header as OrgItem;
            if (item != null && item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                foreach (var oi in orgItem.Children)
                {
                    TreeViewItem subItem = new TreeViewItem();
                    subItem.Header = oi;
                    if (oi is OriginOrgItem)
                    {
                        subItem.HeaderTemplate = FindResource("OriginTemplate") as DataTemplate;
                        subItem.Items.Add(null);
                    }
                    else if (oi is AttributeOrgItem)
                    {
                        AttributeModel attributeModel = (AttributeModel)oi.Data;
                        AttributeViewModel attributeViewModel = new AttributeViewModel(null, new AttributeOperationModel(attributeModel))
                        {
                            IsNoChrome = true,
                            IsMenuEnabled = false
                        };

                        DataTemplate template = new DataTemplate();
                        FrameworkElementFactory tbFactory = new FrameworkElementFactory(typeof(AttributeView));
                        tbFactory.SetValue(AttributeView.DataContextProperty, attributeViewModel);
                        template.VisualTree = tbFactory;
                        subItem.HeaderTemplate = template;

                    }
                    item.Items.Add(subItem);

                }
            }
        }

        public void AttributeViewModelMoved(AttributeViewModel sender, AttributeViewModelEventArgs e, bool overElement)
        {
        }

        public void AttributeViewModelDropped(AttributeViewModel sender, AttributeViewModelEventArgs e)
        {
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return new GeometryHitTestResult(this, IntersectionDetail.Intersects);
        }

        private void TreeViewItem_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void TreeViewItem_OnLoaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(TreeView_TouchDownEvent));
        }

        private void FrameworkElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).RemoveHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(TreeView_TouchDownEvent));
        }

        private void TreeView_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject) as TreeViewItem;
            if (treeViewItem != null)
            {
                if (treeViewItem.Header is DatabaseRootOrgItem || treeViewItem.Header is OriginOrgItem)
                {
                    var element = (FrameworkElement)sender;
                    e.TouchDevice.Capture(element);

                    InkableScene inkableScene = this.FindParent<InkableScene>();
                    _treeViewStartDrag = e.GetTouchPoint(inkableScene).Position;

                    element.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(TreeView_TouchMoveEvent));
                    element.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(TreeView_TouchUpEvent));
                }
                else
                {
                    treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                }
                e.Handled = true;
            }
        }

        private void TreeView_TouchUpEvent(Object sender, TouchEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject) as TreeViewItem;
            if (treeViewItem != null &&
                (treeViewItem.Header is DatabaseRootOrgItem || treeViewItem.Header is OriginOrgItem))
            {
                var element = (FrameworkElement)sender;
                e.Handled = true;
                e.TouchDevice.Capture(null);

                element.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(TreeView_TouchMoveEvent));
                element.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(TreeView_TouchUpEvent));

                if (_treeViewShadow != null)
                {
                    InkableScene inkableScene = this.FindParent<InkableScene>();
                    /*
                    if (DatabaseTableDropped != null)
                    {
                        Rct bounds = _treeViewShadow.GetBounds(inqScene);
                        DatabaseTableDropped(this, new DatabaseTableEventArgs(bounds,
                            SchemaViewModel, null, true));
                    }
                    */
                    inkableScene.Remove(_treeViewShadow);
                    _treeViewShadow = null;
                }
                else
                {
                    treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                }
            }
        }

        private void TreeView_TouchMoveEvent(Object sender, TouchEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject) as TreeViewItem;
            if (treeViewItem != null && treeViewItem.Header is DatabaseRootOrgItem)
            {
                InkableScene inkableScene = this.FindParent<InkableScene>();
                Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

                /*Vec v = _treeViewStartDrag - fromInkableScene;
                List<PathInfo> pathInfos = SchemaViewModel.OriginModel.CalculateRecursivePathInfos();
                if (v.Length > 10 && _treeViewShadow == null &&
                    pathInfos.Count > 1)
                {
                    ManipulationStart(fromInkableScene);
                }
                ManipulationMove(fromInkableScene);*/
            }
        }

        public void ManipulationStart(Point fromInqScene)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            if (inkableScene != null)
            {
                _treeViewStartDrag = fromInqScene;
                _treeViewShadow = new Border();
                _treeViewShadow.Width = 120;
                _treeViewShadow.Height = 40;
                _treeViewShadow.Background = new SolidColorBrush(Color.FromArgb(70, 125, 125, 125));
                _treeViewShadow.BorderBrush = Brushes.Black;
                Label l = new Label();
                l.HorizontalAlignment = HorizontalAlignment.Center;
                l.VerticalAlignment = VerticalAlignment.Center;
                l.FontWeight = FontWeights.Bold;
                l.Content = "Tables";
                _treeViewShadow.Child = l;

                _treeViewShadow.RenderTransform = new TranslateTransform(
                    fromInqScene.X - _treeViewShadow.Width / 2.0,
                    fromInqScene.Y - _treeViewShadow.Height);
                inkableScene.Add(_treeViewShadow);
            }
        }

        public void ManipulationMove(Point fromInqScene)
        {
            if (_treeViewShadow != null)
            {
                InkableScene inkableScene = this.FindParent<InkableScene>();
                _treeViewStartDrag = fromInqScene;
                _treeViewShadow.RenderTransform = new TranslateTransform(
                    fromInqScene.X - _treeViewShadow.Width / 2.0,
                    fromInqScene.Y - _treeViewShadow.Height);
                inkableScene.Add(_treeViewShadow);
            }
        }

    }
}
