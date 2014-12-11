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
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using PanoramicData.model.view;
using PanoramicData.view.table;
using PanoramicData.view.filter;

namespace PanoramicData.view.schema
{
    /// <summary>
    /// Interaction logic for ColumnTreeView.xaml
    /// </summary>
    public partial class ColumnTreeView : UserControl, ColumnHeaderEventHandler
    {
        public static event EventHandler<DatabaseTableEventArgs> DatabaseTableDropped;

        public FilterModel FilterModel { get; set; }

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

        public void InitTree(FilterModel filterModel)
        {
            FilterModel = filterModel;
            tree.Items.Clear();

            DatabaseRootOrgItem databaseRoot = new DatabaseRootOrgItem(FilterModel, "Tables");
            TreeViewItem item = new TreeViewItem();
            item.Header = databaseRoot;
            item.HeaderTemplate = FindResource("DatabaseRootTemplate") as DataTemplate;
            item.Items.Add(null);
            tree.Items.Add(item);

            if (FilterModel.TableModel.NamedFilterModels.Count > 0)
            {
                CustomFieldsRootOrgItem customFieldsRoot = new CustomFieldsRootOrgItem(FilterModel.TableModel,
                    "Custom Groups");
                item = new TreeViewItem();
                item.Header = customFieldsRoot;
                item.HeaderTemplate = FindResource("CustomFieldsRootTemplate") as DataTemplate;
                item.Items.Add(null);
                tree.Items.Add(item);
            }
            if (FilterModel.TableModel.CalculatedColumnDescriptorInfos.Count > 0)
            {
                CaclculatedFieldsRootOrgItem customFieldsRoot = new CaclculatedFieldsRootOrgItem(FilterModel.TableModel,
                    "Caclculated Fields");
                item = new TreeViewItem();
                item.Header = customFieldsRoot;
                item.HeaderTemplate = FindResource("CalculatedFieldsRootTemplate") as DataTemplate;
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
                    if (oi is TableInfoOrgItem)
                    {
                        subItem.HeaderTemplate = FindResource("TableInfoTemplate") as DataTemplate;
                        subItem.Items.Add(null);
                    }
                    if (oi is CustomFieldOrgItem)
                    {
                        FilterModel filterModel = (FilterModel)oi.Data;
                        TableModel tableModel = filterModel.TableModel;
                        PanoramicDataColumnDescriptor cd = new NamedFilterModelColumnDescriptor(tableModel, filterModel);

                        DataTemplate template = new DataTemplate();
                        FrameworkElementFactory tbFactory = new FrameworkElementFactory(typeof(SimpleGridViewColumnHeader));
                        tbFactory.SetValue(SimpleGridViewColumnHeader.IsInteractiveProperty, true);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.FilterModelProperty, FilterModel);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.TableModelProperty, null);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.DataContextProperty, cd);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.IsSimpleRenderingProperty, true);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.EnableRadialMenuProperty, false);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.EnableMoveByPenProperty, true);
                        template.VisualTree = tbFactory;
                        subItem.HeaderTemplate = template;
                    }
                    if (oi is CalculatedFieldOrgItem)
                    {
                        CalculatedColumnDescriptorInfo descriptorInfo = (CalculatedColumnDescriptorInfo)oi.Data;
                        PanoramicDataColumnDescriptor cd = new CalculatedColumnDescriptor(descriptorInfo.TableModel, descriptorInfo);

                        DataTemplate template = new DataTemplate();
                        FrameworkElementFactory tbFactory = new FrameworkElementFactory(typeof(SimpleGridViewColumnHeader));
                        tbFactory.SetValue(SimpleGridViewColumnHeader.IsInteractiveProperty, true);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.FilterModelProperty, FilterModel);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.TableModelProperty, null);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.DataContextProperty, cd);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.IsSimpleRenderingProperty, true);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.EnableRadialMenuProperty, false);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.EnableMoveByPenProperty, true);
                        template.VisualTree = tbFactory;
                        subItem.HeaderTemplate = template;
                    }
                    else if (oi is FieldInfoOrgItem)
                    {
                        FieldInfo fieldInfo = (FieldInfo)oi.Data;
                        PathInfo pathInfo = oi.Parent.Data as PathInfo;
                        PanoramicDataColumnDescriptor cd = new DatabaseColumnDescriptor(fieldInfo, pathInfo);

                        DataTemplate template = new DataTemplate();
                        FrameworkElementFactory tbFactory = new FrameworkElementFactory(typeof(SimpleGridViewColumnHeader));
                        tbFactory.SetValue(SimpleGridViewColumnHeader.IsInteractiveProperty, true);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.FilterModelProperty, FilterModel);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.TableModelProperty, null);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.DataContextProperty, cd);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.IsSimpleRenderingProperty, true);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.EnableRadialMenuProperty, false);
                        tbFactory.SetValue(SimpleGridViewColumnHeader.EnableMoveByPenProperty, true);
                        template.VisualTree = tbFactory;
                        subItem.HeaderTemplate = template;

                    }
                    item.Items.Add(subItem);

                }
            }
        }

        public void ColumnHeaderMoved(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e, bool overElement)
        {
        }

        public void ColumnHeaderDropped(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e)
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
                if (treeViewItem.Header is DatabaseRootOrgItem || treeViewItem.Header is TableInfoOrgItem)
                {
                    var element = (FrameworkElement)sender;
                    e.TouchDevice.Capture(element);

                    InqScene inqScene = this.FindParent<InqScene>();
                    _treeViewStartDrag = e.GetTouchPoint(inqScene).Position;

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
                (treeViewItem.Header is DatabaseRootOrgItem || treeViewItem.Header is TableInfoOrgItem))
            {
                var element = (FrameworkElement)sender;
                e.Handled = true;
                e.TouchDevice.Capture(null);

                element.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(TreeView_TouchMoveEvent));
                element.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(TreeView_TouchUpEvent));

                if (_treeViewShadow != null)
                {
                    InqScene inqScene = this.FindParent<InqScene>();

                    if (DatabaseTableDropped != null)
                    {
                        Rct bounds = _treeViewShadow.GetBounds(inqScene);
                        DatabaseTableDropped(this, new DatabaseTableEventArgs(bounds,
                            FilterModel.TableModel, FilterModel, true));
                    }

                    inqScene.Rem(_treeViewShadow);
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
                InqScene inqScene = this.FindParent<InqScene>();
                Point fromInqScene = e.GetTouchPoint(inqScene).Position;

                Vec v = _treeViewStartDrag - fromInqScene;
                List<PathInfo> pathInfos = FilterModel.TableModel.CalculateRecursivePathInfos();
                if (v.Length > 10 && _treeViewShadow == null &&
                    pathInfos.Count > 1)
                {
                    ManipulationStart(fromInqScene);
                }
                ManipulationMove(fromInqScene);
            }
        }

        public void ManipulationStart(Point fromInqScene)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            if (inqScene != null)
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
                inqScene.AddNoUndo(_treeViewShadow);
            }
        }

        public void ManipulationMove(Point fromInqScene)
        {
            if (_treeViewShadow != null)
            {
                InqScene inqScene = this.FindParent<InqScene>();
                _treeViewStartDrag = fromInqScene;
                _treeViewShadow.RenderTransform = new TranslateTransform(
                    fromInqScene.X - _treeViewShadow.Width / 2.0,
                    fromInqScene.Y - _treeViewShadow.Height);
                inqScene.AddNoUndo(_treeViewShadow);
            }
        }

    }
}
