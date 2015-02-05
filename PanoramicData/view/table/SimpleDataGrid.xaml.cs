using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FarseerPhysics;
using CombinedInputAPI;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using PixelLab.Common;
using System.Globalization;
using PanoramicDataModel;
using Border = System.Windows.Controls.Border;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using TextAlignment = System.Windows.TextAlignment;
using PanoramicData.controller.data;
using PanoramicData.utils.inq;
using PanoramicData.view.utils;
using PanoramicData.view.vis;
using PanoramicData.model.view_new;
using PanoramicData.view.vis.render;
using System.Collections.Specialized;
using PanoramicData.model.data;
using PanoramicData.view.inq;
using System.Reactive.Linq;
using PanoramicData.utils;

namespace PanoramicData.view.table
{
    /// <summary>
    /// Interaction logic for SimpleDataGrid.xaml
    /// </summary>
    public partial class SimpleDataGrid : AttributeViewModelEventHandler, StroqListener
    {
        public delegate void CellDroppedOutsideHandler(object sender, PanoramicDataColumnDescriptor column, PanoramicDataRow row, Point position);
        public event CellDroppedOutsideHandler CellDroppedOutside;

        private IDisposable _observableDisposable = null;
        private GridView _gridView = null;

        public bool CanReorder { get; set; }
        public bool CanResize { get; set; }
        public bool CanDrag { get; set; }
        public bool CanExplore { get; set; }

        private Point _startDrag1 = new Point();
        private long _manipulationStartTime = 0;
        private PanoramicDataColumnDescriptor _headerColumnDescriptor1 = null;

        private TouchDevice _dragDevice1 = null;
        private TouchDevice _dragDevice2 = null;

        private bool _isResizing = false;
        private bool _isTwoFingerExploringGraphs = true;
        private VisualizationContainerView _twoFingerExploreFeedback = null;
        private VisualizationContainerView _explorerFeedback = null;
        private PanoramicDataColumnDescriptor _explorerFeedbackColumnDescriptor = null;
        private bool _isSimpleGridViewColumnHeaderMoveFeedbackShown = false;
        private List<MappingEntry> _mapping = new List<MappingEntry>();

        private GridViewColumn _checkBoxColumn = null;

        private DateTime _last = DateTime.MinValue;
        
        public SimpleDataGrid()
        {
            InitializeComponent();
            this.DataContextChanged += SimpleDataGrid_DataContextChanged;
        }

        void SimpleDataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                if (_observableDisposable != null)
                {
                    _observableDisposable.Dispose();
                }
                (e.OldValue as VisualizationViewModel).QueryModel.QueryResultModel.PropertyChanged -= QueryResultModel_PropertyChanged;
            }
            if (e.NewValue != null)
            {
                if (_observableDisposable != null)
                {
                    _observableDisposable.Dispose();
                }
                _observableDisposable = Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(
                    (e.NewValue as VisualizationViewModel).QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.X), "CollectionChanged")
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            populateTableHeaders();
                        }));
                    });

                QueryResultModel resultModel = (DataContext as VisualizationViewModel).QueryModel.QueryResultModel;
                resultModel.PropertyChanged += QueryResultModel_PropertyChanged;
                if (resultModel.QueryResultItemModels != null)
                {
                    CollectionChangedEventManager.AddHandler(resultModel.QueryResultItemModels, QueryResultItemModels_CollectionChanged);
                    populateData();
                }
                populateTableHeaders();
            }
        }

        void QueryResultModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
             QueryResultModel resultModel = (DataContext as VisualizationViewModel).QueryModel.QueryResultModel;
             if (e.PropertyName == resultModel.GetPropertyName(() => resultModel.QueryResultItemModels))
             {
                 CollectionChangedEventManager.AddHandler(resultModel.QueryResultItemModels, QueryResultItemModels_CollectionChanged);
                 populateData();
             }
        }
        void QueryResultItemModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
        }

        private void populateData()
        {
            QueryResultModel resultModel = (DataContext as VisualizationViewModel).QueryModel.QueryResultModel;

            listView.ItemsSource = resultModel.QueryResultItemModels;
        }

        private void populateTableHeaders()
        {
            _dragDevice1 = null;
            _dragDevice2 = null;

            VisualizationViewModel model = (DataContext as VisualizationViewModel);
            List<AttributeOperationModel> attributeOperationModels = model.QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).ToList();

            _gridView = new GridView();
            _gridView.AllowsColumnReorder = false;

            // selection / checkbox column 
            _checkBoxColumn = new GridViewColumn();
            _checkBoxColumn.Width = 35;
            _checkBoxColumn.Header = "";
            DataTemplate template = new DataTemplate();
            FrameworkElementFactory checkboxFactory = new FrameworkElementFactory(typeof(CheckBox));
            checkboxFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding("Data.IsSelected"));
            checkboxFactory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            checkboxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkboxFactory.SetValue(CheckBox.IsEnabledProperty, true);
            checkboxFactory.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(checkboxFactory_CheckedEvent));
            checkboxFactory.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(checkboxFactory_UncheckedEvent));

            checkboxFactory.AddHandler(CheckBox.TouchDownEvent, new EventHandler<TouchEventArgs>(checkboxFactory_TouchDownEvent));
            checkboxFactory.AddHandler(CheckBox.TouchUpEvent, new EventHandler<TouchEventArgs>(checkboxFactory_TouchUpEvent));
            
            template.VisualTree = checkboxFactory;
            _checkBoxColumn.CellTemplate = template;
            _gridView.Columns.Add(_checkBoxColumn);

            /*if (_filterModel.GetIncomingFilterModels(FilteringType.Brush).Count > 0) //_tableModel != null)
            {
                GridViewColumn special = new GridViewColumn();
                special.Width = 35;
                special.Header = "";
                template = new DataTemplate();
                FrameworkElementFactory imgFactory = new FrameworkElementFactory(typeof(Image));
                Binding bimg = new Binding("Data");
                bimg.Converter = new FilterHighlightImageConverter(_filterModel);
                imgFactory.SetValue(Image.MarginProperty, new Thickness(0));
                imgFactory.SetBinding(Image.SourceProperty, bimg);
                imgFactory.SetValue(Image.StretchProperty, Stretch.Uniform);

                template.VisualTree = imgFactory;
                special.CellTemplate = template;
                gridView.Columns.Add(special);
            }*/
            /*if (_filterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count > 0) //_tableModel != null)
            {
                GridViewColumn special = new GridViewColumn();
                special.Width = 35;
                special.Header = "";
                template = new DataTemplate();
                FrameworkElementFactory imgFactory = new FrameworkElementFactory(typeof(Image));
                Binding bimg = new Binding("Data");
                bimg.Converter = new ColorByImageConverter(_filterModel);
                imgFactory.SetValue(Image.MarginProperty, new Thickness(0));
                imgFactory.SetBinding(Image.SourceProperty, bimg);
                imgFactory.SetValue(Image.StretchProperty, Stretch.Uniform);

                template.VisualTree = imgFactory;
                special.CellTemplate = template;
                gridView.Columns.Add(special);
            }*/

            List<MappingEntry> newMapping = new List<MappingEntry>();

            int fieldsIndex = 0;
            foreach (var attributeOperationModel in attributeOperationModels)
            {
                // loop over the current mapping and see if any fields match. 
                // this makes sure any reordering and adjusted widths are preserved

                if (_mapping.Where(me => me.AttributeOperationModel == attributeOperationModel).Count() > 0)
                {
                    MappingEntry mappingEntry = _mapping.Single(me => me.AttributeOperationModel == attributeOperationModel);
                    GridViewColumn gvc = createGridViewColumn(mappingEntry.AttributeOperationModel, attributeOperationModels.IndexOf(mappingEntry.AttributeOperationModel), mappingEntry.GridViewColumn);
                    _gridView.Columns.Add(gvc);
                    mappingEntry.GridViewColumn = gvc;
                    newMapping.Add(mappingEntry);
                }
                else
                {
                    GridViewColumn gvc = createGridViewColumn(attributeOperationModel, fieldsIndex, null);
                    if (attributeOperationModels.Count == 1)
                    {
                        gvc.Width = 200;
                    }
                    _gridView.Columns.Add(gvc);
                    MappingEntry me = new MappingEntry();
                    me.AttributeOperationModel = attributeOperationModel;
                    me.GridViewColumn = gvc;
                    me.FieldsIndex = fieldsIndex;
                    newMapping.Add(me);
                }

                fieldsIndex++;
            }

            _mapping.Clear();
            _mapping = newMapping;

            if (_gridView.Columns.Count() > 0)
            {
                GridViewColumnResize.SetWidth(_gridView.Columns.Last(), "*");
                listView.View = _gridView;
            }
        }
        
        GridViewColumn createGridViewColumn(AttributeOperationModel attributeOperationModel, int index, GridViewColumn oldColumn)
        {
            GridViewColumn gvc = new GridViewColumn();

            DataTemplate template = new DataTemplate();
            FrameworkElementFactory tbFactory = new FrameworkElementFactory(typeof(AttributeView));
            tbFactory.SetValue(AttributeView.DataContextProperty, new AttributeViewModel(DataContext as VisualizationViewModel, attributeOperationModel)
                {
                    IsRemoveEnabled = true
                });
            /*tbFactory.SetValue(AttributeView.Is, false);
            tbFactory.SetValue(AttributeView.FilterModelProperty, _filterModel);
            tbFactory.SetValue(AttributeView.TableModelProperty, _tableModel);*/

            template.VisualTree = tbFactory;
            gvc.HeaderTemplate = template;

            if (oldColumn != null)
            {
                gvc.Width = oldColumn.ActualWidth;
            }
            else
            {
                gvc.Width = 100;
            }

            template = new DataTemplate();
            /*if ((columnDescriptor.AggregateFunction == AggregateFunction.Concat ||
                      columnDescriptor.AggregateFunction == AggregateFunction.None) &&
                     ((_filterModel.GetColumnDescriptorsForOption(Option.GroupBy)
                            .Count(cd => !cd.MatchSimple(columnDescriptor)) > 0 &&
                         _filterModel.GetColumnDescriptorsForOption(Option.GroupBy)
                            .Count(cd => cd.MatchSimple(columnDescriptor)) == 0) ||
                         (_filterModel.GetColumnDescriptorsForOption(Option.X).Count(cd => cd.AggregateFunctionSetByUser && cd.AggregateFunction != AggregateFunction.None) > 0 &&
                          _filterModel.GetColumnDescriptorsForOption(Option.GroupBy)
                            .Count(cd => cd.MatchSimple(columnDescriptor)) == 0)) )
            {
                tbFactory = new FrameworkElementFactory(typeof(TextBlock));
                tbFactory.SetValue(TextBlock.TextProperty, "...");
                tbFactory.SetValue(TextBlock.BackgroundProperty, new SolidColorBrush(Color.FromArgb(1,0,0,0)));
                tbFactory.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(tbFactory_TouchDownEvent));
                tbFactory.SetValue(TextBlock.TagProperty, index);
                //tbFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow);
                tbFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                tbFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                tbFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                tbFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
                template.VisualTree = tbFactory;
            }
            else if (!columnDescriptor.IsVisualization)*/
            {
                tbFactory = new FrameworkElementFactory(typeof(TextBlock));
                Binding valueBinding = new Binding("Data");
                valueBinding.Converter = new TextValueConverter(attributeOperationModel);

                tbFactory.SetBinding(TextBlock.TextProperty, valueBinding);
                tbFactory.SetValue(TextBlock.TagProperty, index);
                //tbFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow);
                /*string dataType = FilterModel.GetDataTypeOfPanoramicDataColumnDescriptor(columnDescriptor, true);
                if (dataType == AttributeDataTypeConstants.NVARCHAR ||
                    dataType == AttributeDataTypeConstants.GEOGRAPHY)
                {
                    tbFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Left);
                }
                else
                {
                    tbFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                }*/
                tbFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                tbFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                tbFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
                template.VisualTree = tbFactory;
            }
            /*else
            {
                FrameworkElementFactory gFactory = new FrameworkElementFactory(typeof(Grid));
                gFactory.SetValue(Grid.ColumnProperty, 0);
                gFactory.SetValue(Grid.BackgroundProperty, Brushes.White);
                gFactory.SetValue(Grid.HeightProperty, 25.0);

                FrameworkElementFactory imgFactory = new FrameworkElementFactory(typeof(Image));
                MultiBinding bimg = new MultiBinding();
                Binding b = new Binding("ActualWidth");
                b.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Grid), 1);
                bimg.Bindings.Add(b);
                b = new Binding("ActualHeight");
                b.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Grid), 1);
                bimg.Bindings.Add(b);
                bimg.Bindings.Add(new Binding("Data.Values.Values[" + index + "].StringValue"));
                bimg.Converter = new MultiValueImageConverter();
                imgFactory.SetBinding(Image.SourceProperty, bimg);

                imgFactory.SetValue(TextBlock.MarginProperty, new Thickness(1));
                imgFactory.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                imgFactory.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Stretch);
                imgFactory.SetValue(Image.StretchProperty, Stretch.None);

                gFactory.AppendChild(imgFactory);
                template.VisualTree = gFactory;
            }*/
            gvc.CellTemplate = template;


            return gvc;
        }

        void tbFactory_TouchDownEvent(object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;
        }

        void checkboxFactory_TouchDownEvent(object sender, TouchEventArgs e)
        {
            //e.TouchDevice.Capture(sender as FrameworkElement);
            //e.Handled = true;
        }

        void checkboxFactory_TouchUpEvent(object sender, TouchEventArgs e)
        {
           // e.TouchDevice.Capture(null);
            //e.Handled = true;
        }

        void checkboxFactory_CheckedEvent(object sender, RoutedEventArgs e)
        {
            VisualizationViewModel model = (DataContext as VisualizationViewModel);
            var item = ((sender as FrameworkElement).DataContext as DataWrapper<QueryResultItemModel>).Data;
            FilterModel fi = new FilterModel(item);
            if (!model.QueryModel.FilterModels.Contains(fi))
            {
                model.QueryModel.AddFilterItem(fi, this);
            }
        }
        void checkboxFactory_UncheckedEvent(object sender, RoutedEventArgs e)
        {
            VisualizationViewModel model = (DataContext as VisualizationViewModel);
            var item = ((sender as FrameworkElement).DataContext as DataWrapper<QueryResultItemModel>).Data;
            FilterModel fi = new FilterModel(item);
            if (model.QueryModel.FilterModels.Contains(fi))
            {
                model.QueryModel.RemoveFilterItem(fi, this);
            }
        }

        void toggleSelection(List<QueryResultItemModel> queryResultItemModels)
        {
            VisualizationViewModel model = (DataContext as VisualizationViewModel);

            if (queryResultItemModels.Any(r => r.IsSelected))
            {
                foreach (var item in queryResultItemModels)
                {
                    item.IsSelected = false;
                    FilterModel fi = new FilterModel(item);
                    if (model.QueryModel.FilterModels.Contains(fi))
                    {
                        model.QueryModel.RemoveFilterItem(fi, this);
                    }
                }
            }
            else
            {
                foreach (var item in queryResultItemModels)
                {
                    item.IsSelected = true;
                    FilterModel fi = new FilterModel(item);
                    if (!model.QueryModel.FilterModels.Contains(fi))
                    {
                        model.QueryModel.AddFilterItem(fi, this);
                    }
                }
            }
        }

        void ColumnHeader_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            if ((e.Device is StylusDevice || (e.Device is MouseTouchDevice && (e.Device as MouseTouchDevice).IsStylus)))
            {
                return;
            }
            (sender as FrameworkElement).ManipulationDelta += ColumnHeader_ManipulationDelta;
            (sender as FrameworkElement).ManipulationCompleted += ColumnHeader_ManipulationCompleted;

            var header = (sender as FrameworkElement).FindParent<GridViewColumnHeader>();
            _manipulationStartTime = DateTime.Now.Ticks;

            Point pos = e.Manipulators.First().GetPosition(header);
            if (CanResize && header.Column != null &&
                pos.X > header.Column.ActualWidth - 15 &&
                pos.X < header.Column.ActualWidth + 15)
            {
                _isResizing = true;
            }
            else
            {
                _isResizing = false;
            }
        }

        void ColumnHeader_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var header = (sender as FrameworkElement).FindParent<GridViewColumnHeader>();

            if (CanResize && _isResizing && header != null)
            {
                header.Column.Width = Math.Max(35, header.Column.ActualWidth + e.DeltaManipulation.Translation.X);
                //e.Handled = true;
            }
        }

        void ColumnHeader_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            (sender as FrameworkElement).ManipulationDelta -= ColumnHeader_ManipulationDelta;
            (sender as FrameworkElement).ManipulationCompleted -= ColumnHeader_ManipulationCompleted;

            _isResizing = false;
            _manipulationStartTime = 0;
        }


        public void AttributeViewModelMoved(AttributeViewModel sender, AttributeViewModelEventArgs e, bool overElement)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
         
            // hide cloumn header reorder drop highlights 
            hideColumnReorderFeedbacks();

            if (overElement)
            {
                Point fromThis = inkableScene.TranslatePoint(e.Bounds.Center, this);

                if (CanReorder)
                {
                    IEnumerable<GridViewColumnHeader> headers = listView.VisualDescendentsOfType<GridViewColumnHeader>();
                    FrameworkElement highlight = null;

                    // find closest header reorder drop highlight 
                    GridViewColumnHeader closestHeader = findClosestColumnHeader(e.Bounds.Center);
                    highlight = closestHeader.FirstVisualDescendentByName("dragHighlight");
                    highlight.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        public void AttributeViewModelDropped(AttributeViewModel sender, AttributeViewModelEventArgs e)
        {            
            // hide cloumn header reorder drop highlights 
            hideColumnReorderFeedbacks();

            VisualizationViewModel model = (DataContext as VisualizationViewModel);
            if (model.QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).Count == 0)
            {
                model.QueryModel.AddFunctionAttributeOperationModel(AttributeFunction.X, sender.AttributeOperationModel);
                return;
            }

            InkableScene inkableScene = this.FindParent<InkableScene>();
            Point fromThis = inkableScene.TranslatePoint(e.Bounds.Center, this);

            // find closest header reorder drop highlight 
            GridViewColumnHeader closestHeader = findClosestColumnHeader(e.Bounds.Center);
            GridViewColumnCollection columns = ((GridView)listView.View).Columns;

            if (!_isResizing && (CanReorder || CanDrag) &&
                model.QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).Any(aom => object.ReferenceEquals(aom, sender.AttributeOperationModel)))
            {
                model.QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).Remove(sender.AttributeOperationModel);
                
            }
            AttributeOperationModel clone = e.AttributeOperationModel;
            if (closestHeader.Column == null)
            {
                model.QueryModel.AddFunctionAttributeOperationModel(AttributeFunction.X, clone);
            }
            else if ((closestHeader.Column.Header as string) == "")
            {
                model.QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).Insert(0, clone);
                model.QueryModel.FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
            }
            else
            {
                MappingEntry mapClosest = _mapping.Single(me => me.GridViewColumn == closestHeader.Column);
                int indexMap = _mapping.IndexOf(mapClosest);
                model.QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).Insert(indexMap, clone);
                model.QueryModel.FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
            }
        }

        private void hideColumnReorderFeedbacks()
        {
            IEnumerable<GridViewColumnHeader> headers = listView.VisualDescendentsOfType<GridViewColumnHeader>();
            FrameworkElement highlight = null;
            foreach (var h in headers)
            {
                if (h.ActualWidth != 0.0)
                {
                    highlight = h.FirstVisualDescendentByName("dragHighlight");
                    highlight.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
        private GridViewColumnHeader findClosestColumnHeader(Point fromInqScene)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            IEnumerable<GridViewColumnHeader> headers = listView.VisualDescendentsOfType<GridViewColumnHeader>();

            // find closest header reorder drop highlight 
            GridViewColumnHeader closestHeader = null;
            double closestXDist = double.MaxValue;
            foreach (var h in headers)
            {
                if (h.ActualWidth != 0.0)
                {
                    Point p = inkableScene.TranslatePoint(fromInqScene, h);
                    if (Math.Abs(p.X) < closestXDist)
                    {
                        closestXDist = Math.Abs(p.X);
                        closestHeader = h;
                    }
                }
            }
            return closestHeader;
        }
        private GridViewColumnHeader findOverColumnHeader(Point fromInqScene)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            IEnumerable<GridViewColumnHeader> headers = listView.VisualDescendentsOfType<GridViewColumnHeader>();

            foreach (var h in headers)
            {
                if (h.ActualWidth != 0.0)
                {
                    Point p = inkableScene.TranslatePoint(fromInqScene, h);
                    if (p.X > 0 && p.X < h.ActualWidth)
                    {
                        return h;
                    }
                }
            }
            return null;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return new GeometryHitTestResult(this, IntersectionDetail.Intersects);
        }
        
        public void NotifyStroqAdded(starPadSDK.Inq.Stroq s)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            InqAnalyzer inqAnalyzer = new InqAnalyzer();
            inqAnalyzer.AddStroke(s);
            inqAnalyzer.Analyze();
            string recog = inqAnalyzer.GetRecognizedString().ToLower();

            if (recog.Equals("a"))
            {

            }
            else
            {
                GridViewColumnHeader header = findOverColumnHeader(s[0]);
                if(header != null) {
                    if (header.Column == _checkBoxColumn)
                    {
                        Rect bounds = s.GetBounds();
                        GeoAPI.Geometries.IGeometry geom = new Pt[]
                        {
                            inkableScene.TranslatePoint(new Point(bounds.Left, bounds.Top), this),
                            inkableScene.TranslatePoint(new Point(bounds.Right, bounds.Top), this),
                            inkableScene.TranslatePoint(new Point(bounds.Right, bounds.Bottom), this),
                            inkableScene.TranslatePoint(new Point(bounds.Left, bounds.Bottom), this)
                        }.GetPolygon();
                        var rowPresenters = this.GetIntersectedTypesRecursive<GridViewRowPresenter>(geom.GetBounds());
                        List<PanoramicDataRow> rows = new List<PanoramicDataRow>();
                        foreach (var rowPresenter in rowPresenters)
                        {
                            if ((DataWrapper<PanoramicDataRow>) rowPresenter.DataContext != null)
                            {
                                PanoramicDataRow row = ((DataWrapper<PanoramicDataRow>) rowPresenter.DataContext).Data;
                                rows.Add(row);
                            }
                        }

                        //if (rows.Count > 0 && RowsSelected != null)
                        {
                            //RowsSelected(this, rows);
                        }
                    }
                    else
                    {
                        MappingEntry map = null;
                        IEnumerable<MappingEntry> mapEntries = _mapping.Where(me => me.GridViewColumn == header.Column);
                        if (mapEntries.Count() > 0)
                        {
                            map = mapEntries.First();

                            if (s.Count > 2)
                            {
                                if (s.Cusps().Length == 2 && Math.Abs(s[0].X - s[-1].X) < 50 &&
                                    s.Cusps().outSeg(0).Length > 30)
                                {

                                    bool downStroq = (s[-1].Y - s[0].Y) > 0;

                                    //EZ: if ((map.ColumnDescriptor.SortMode == SortMode.None ||
                                    /*     map.ColumnDescriptor.SortMode == SortMode.Asc) && downStroq)
                                    {
                                        map.ColumnDescriptor.SortMode = SortMode.Desc;
                                    }
                                    else if ((map.ColumnDescriptor.SortMode == SortMode.None ||
                                              map.ColumnDescriptor.SortMode == SortMode.Desc) && !downStroq)
                                    {
                                        map.ColumnDescriptor.SortMode = SortMode.Asc;
                                    }
                                    else if (map.ColumnDescriptor.SortMode == SortMode.Desc && downStroq)
                                    {
                                        map.ColumnDescriptor.SortMode = SortMode.None;
                                    }
                                    else if (map.ColumnDescriptor.SortMode == SortMode.Asc && !downStroq)
                                    {
                                        map.ColumnDescriptor.SortMode = SortMode.None;
                                    }*/
                                }
                            }
                        }
                    }
                }
            }

            //inkableScene.Rem(s);
        }

        public void NotifyStroqRemoved(starPadSDK.Inq.Stroq s)
        {
        }

        public void NotifyStroqsRemoved(starPadSDK.Inq.StroqCollection sc)
        {
        }

        public void NotifyStroqsAdded(starPadSDK.Inq.StroqCollection sc)
        {
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).IsManipulationEnabled = true;
            (sender as FrameworkElement).AddHandler(FrameworkElement.ManipulationStartedEvent, new EventHandler<ManipulationStartedEventArgs>(ColumnHeader_ManipulationStarted));
        }

        private void FrameworkElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).RemoveHandler(FrameworkElement.ManipulationStartedEvent, new EventHandler<ManipulationStartedEventArgs>(ColumnHeader_ManipulationStarted));
        }
    }
   
    public class TextValueConverter : IValueConverter
    {
        private AttributeOperationModel _attributeOperationModel = null;

        public TextValueConverter(AttributeOperationModel attributeOperationModel)
        {
            _attributeOperationModel = attributeOperationModel;
        }
        public object Convert(object value, Type targetType, object parameter,
           CultureInfo culture)
        {
            if (value != null)
            {
                QueryResultItemModel model = (value as QueryResultItemModel);
                if (model.Values.ContainsKey(_attributeOperationModel))
                {
                    return model.Values[_attributeOperationModel].ShortStringValue;
                }
                return "";
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MappingEntry
    {
        public GridViewColumn GridViewColumn { get; set; }
        public AttributeOperationModel AttributeOperationModel { get; set; }
        public int FieldsIndex { get; set; }
    }
}
