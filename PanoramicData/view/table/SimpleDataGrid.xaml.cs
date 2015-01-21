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
using PanoramicData.model.view;
using PanoramicData.controller.data;
using PanoramicData.utils.inq;
using PanoramicData.view.utils;
using PanoramicData.view.vis;
using PanoramicData.model.view_new;
using PanoramicData.view.vis.render;

namespace PanoramicData.view.table
{
    /// <summary>
    /// Interaction logic for SimpleDataGrid.xaml
    /// </summary>
    public partial class SimpleDataGrid : AttributeViewModelEventHandler, StroqListener
    {
        public static readonly DependencyProperty TestProperty =
            DependencyProperty.Register("Test", typeof(bool), typeof(SimpleDataGrid),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        private DelayedEventExecution _eventExecution = new DelayedEventExecution();
        private DelayedEventExecution _eventExecution2 = new DelayedEventExecution();

        public delegate void RowsSelectedHandler(object sender, List<PanoramicDataRow> rows);
        public event RowsSelectedHandler RowsSelected;

        public delegate void CellDroppedOutsideHandler(object sender, PanoramicDataColumnDescriptor column, PanoramicDataRow row, Point position);
        public event CellDroppedOutsideHandler CellDroppedOutside;

        public bool CanReorder { get; set; }
        public bool CanResize { get; set; }
        public bool CanDrag { get; set; }
        public bool CanExplore { get; set; }
        public FilterModel FilterModel { get; set; }

        private TableModel _tableModel = null;
        private FilterModel _filterModel = null;

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
        private SimpleDataGridDragFeedback _cellDragFeedback = null;
        private bool _isSimpleGridViewColumnHeaderMoveFeedbackShown = false;
        private List<MappingEntry> _mapping = new List<MappingEntry>();
        private AsyncVirtualizingCollection<PanoramicDataRow> _dataValues = null;

        private GridViewColumn _checkBoxColumn = null;

        private DateTime _last = DateTime.MinValue;
        
        public SimpleDataGrid()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            listView.Items.Refresh();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (DateTime.Now.Subtract(_last).TotalMilliseconds > 33)
            {
                base.OnRender(drawingContext);

                _eventExecution.ProcessAction();
                _eventExecution2.ProcessAction();
                _last = DateTime.Now;
            }
        }

        public void PopulateData(
            AsyncVirtualizingCollection<PanoramicDataRow> dataValues,
            TableModel tableModel, FilterModel filterModel)
        {
            _dragDevice1 = null;
            _dragDevice2 = null;

            _tableModel = tableModel;
            _filterModel = filterModel;

            IList<PanoramicDataColumnDescriptor> columnDescriptors = null;

            if (_filterModel != null)
            {
                columnDescriptors = _filterModel.GetColumnDescriptorsForOption(Option.X);
                FilterModel = _filterModel;
            }

            var gridView = new GridView();
            gridView.AllowsColumnReorder = false;

            // selection / checkbox column 
            _checkBoxColumn = new GridViewColumn();
            _checkBoxColumn.Width = 35;
            _checkBoxColumn.Header = "";
            DataTemplate template = new DataTemplate();
            FrameworkElementFactory checkboxFactory = new FrameworkElementFactory(typeof(CheckBox));
            checkboxFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding("Data.IsHighligthed"));
            checkboxFactory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            checkboxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkboxFactory.SetValue(CheckBox.IsEnabledProperty, false);
            checkboxFactory.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(checkboxFactory_TouchDownEvent));
            template.VisualTree = checkboxFactory;
            _checkBoxColumn.CellTemplate = template;
            gridView.Columns.Add(_checkBoxColumn);

            if (_filterModel.GetIncomingFilterModels(FilteringType.Brush).Count > 0) //_tableModel != null)
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
            }
            else if (_filterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count > 0) //_tableModel != null)
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
            }

            List<MappingEntry> newMapping = new List<MappingEntry>();

            int fieldsIndex = 0;
            foreach (var columnDescriptor in columnDescriptors.OrderBy(cd => cd.Order))
            {
                // loop over the current mapping and see if any fields match. 
                // this makes sure any reordering and adjusted widths are preserved

                if (_mapping.Where(me => me.ColumnDescriptor == columnDescriptor).Count() > 0)
                {
                    MappingEntry mappingEntry = _mapping.Single(me => me.ColumnDescriptor == columnDescriptor);
                    GridViewColumn gvc = createGridViewColumn(mappingEntry.ColumnDescriptor, _filterModel.ColumnDescriptors.IndexOf(mappingEntry.ColumnDescriptor), mappingEntry.GridViewColumn);
                    gridView.Columns.Add(gvc);
                    mappingEntry.GridViewColumn = gvc;
                    newMapping.Add(mappingEntry);
                }
                else 
                {
                    GridViewColumn gvc = createGridViewColumn(columnDescriptor, fieldsIndex, null);
                    if (columnDescriptors.Count == 1)
                    {
                        gvc.Width = 200;
                    }
                    gridView.Columns.Add(gvc);
                    MappingEntry me = new MappingEntry();
                    me.ColumnDescriptor = columnDescriptor;
                    me.GridViewColumn = gvc;
                    me.FieldsIndex = fieldsIndex;
                    newMapping.Add(me);
                }

                fieldsIndex++;
            }

            _mapping.Clear();
            _mapping = newMapping;

            if (gridView.Columns.Count() > 0)
            {
                GridViewColumnResize.SetWidth(gridView.Columns.Last(), "*");
                listView.View = gridView;
                if (_dataValues != null)
                {
                    _dataValues.PropertyChanged -= DataValues_PropertyChanged;
                }
                _dataValues = dataValues;
                listView.ItemsSource = _dataValues;
                _dataValues.PropertyChanged += DataValues_PropertyChanged;
            }
        }

        GridViewColumn createGridViewColumn(PanoramicDataColumnDescriptor columnDescriptor, int index, GridViewColumn oldColumn)
        {
            GridViewColumn gvc = new GridViewColumn();

            DataTemplate template = new DataTemplate();
            FrameworkElementFactory tbFactory = new FrameworkElementFactory(typeof(AttributeView));
            tbFactory.SetValue(AttributeView.DataContextProperty, columnDescriptor);
            /*tbFactory.SetValue(AttributeView.IsInteractiveProperty, false);
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
            if ((columnDescriptor.AggregateFunction == AggregateFunction.Concat ||
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
            else if (!columnDescriptor.IsVisualization)
            {
                tbFactory = new FrameworkElementFactory(typeof(TextBlock));
                tbFactory.SetBinding(TextBlock.TextProperty, new Binding("Data.Values.[" + index + "].ShortStringValue"));
                tbFactory.SetValue(TextBlock.TagProperty, index);
                //tbFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow);
                string dataType = FilterModel.GetDataTypeOfPanoramicDataColumnDescriptor(columnDescriptor, true);
                if (dataType == AttributeDataTypeConstants.NVARCHAR ||
                    dataType == AttributeDataTypeConstants.GEOGRAPHY)
                {
                    tbFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Left);
                }
                else
                {
                    tbFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                }
                tbFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                tbFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                tbFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
                template.VisualTree = tbFactory;
            }
            else
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
                bimg.Bindings.Add(new Binding("Data.Values.[" + index + "].StringValue"));
                bimg.Converter = new MultiValueImageConverter();
                imgFactory.SetBinding(Image.SourceProperty, bimg);

                imgFactory.SetValue(TextBlock.MarginProperty, new Thickness(1));
                imgFactory.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                imgFactory.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Stretch);
                imgFactory.SetValue(Image.StretchProperty, Stretch.None);

                gFactory.AppendChild(imgFactory);
                template.VisualTree = gFactory;
            }
            gvc.CellTemplate = template;


            return gvc;
        }

        void DataValues_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (listView.ItemsSource != null && listView.ItemsSource is AsyncVirtualizingCollection<PanoramicDataRow>)
            {
                AsyncVirtualizingCollection<PanoramicDataRow> dataValues = (AsyncVirtualizingCollection<PanoramicDataRow>)listView.ItemsSource;
                FilterModel.RowCount = dataValues.Count;

                if (dataValues.IsInitializing || dataValues.IsLoading)
                {
                    //loadingAnim.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    //loadingAnim.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        void tbFactory_TouchDownEvent(object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;
        }

        void checkboxFactory_TouchDownEvent(object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;

            CheckBox b = (CheckBox)sender;
            if ((DataWrapper<PanoramicDataRow>)b.DataContext != null)
            {
                PanoramicDataRow row = ((DataWrapper<PanoramicDataRow>)b.DataContext).Data;

                if (RowsSelected != null)
                {
                    RowsSelected(this, new List<PanoramicDataRow>(new PanoramicDataRow[] { row }));
                }
            }
        }

        void GridViewRowPresenter_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var elem = sender as FrameworkElement;
            if (elem != null)
            {
                e.Handled = true;
                e.MouseDevice.Capture(elem);

                Point position = e.GetPosition((FrameworkElement)elem);
                _startDrag1 = position;

                elem.MouseMove += GridViewRowPresenter_MouseMove;
                elem.MouseUp += GridViewRowPresenter_MouseUp;
            }
        }
        void GridViewRowPresenter_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;

            var elem = sender as FrameworkElement;
            if (elem != null)
            {
                Point position = e.GetPosition((FrameworkElement)elem);
                Vector vec = position - _startDrag1;
                Point dragDelta = new Point(vec.X, vec.Y);

                if (vec.Length > 10 && _cellDragFeedback == null)
                {
                    _cellDragFeedback = new SimpleDataGridDragFeedback();
                    _cellDragFeedback.Width = elem.ActualWidth + 20;
                    _cellDragFeedback.Height = elem.ActualHeight * 2;
                    _cellDragFeedback.DataContext = (elem as TextBlock).Text;
                    InqScene inqScene = this.FindParent<InqScene>();

                    if (inqScene != null)
                    {
                        inqScene.AddNoUndo(_cellDragFeedback);
                    }
                }
                // move filter drag feedback
                if (_cellDragFeedback != null)
                {
                    InqScene inqScene = this.FindParent<InqScene>();
                    Point trans = elem.TranslatePoint(position, inqScene);
                    _cellDragFeedback.RenderTransform = new TranslateTransform(trans.X - _cellDragFeedback.Width / 2.0, trans.Y - _cellDragFeedback.Height);
                }
            }

        }
        void GridViewRowPresenter_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Mouse.Capture(null);
            var elem = sender as FrameworkElement;

            elem.MouseMove -= GridViewRowPresenter_MouseMove;
            elem.MouseUp -= GridViewRowPresenter_MouseUp;

            if (elem != null && _cellDragFeedback != null)
            {
                Point position = e.GetPosition(elem);
                InqScene inqScene = this.FindParent<InqScene>();
                Point trans = elem.TranslatePoint(position, inqScene);

                inqScene.Rem(_cellDragFeedback);
                _cellDragFeedback = null;

                GridViewColumn column = this.FindParent<GridViewColumn>();

                if (CellDroppedOutside != null)
                {
                    PanoramicDataRow row = ((DataWrapper<PanoramicDataRow>)elem.DataContext).Data;
                    //CellDroppedOutside(this, row.ColumnValues[(int)elem.Tag], row, trans);
                }
            }
        }

        void ColumnHeader_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;
            var header = (sender as FrameworkElement).FindParent<GridViewColumnHeader>();
            PanoramicDataColumnDescriptor column = null;
            if (_mapping.Where(me => me.GridViewColumn == header.Column).Count() > 0)
            {
                MappingEntry map = _mapping.Where(me => me.GridViewColumn == header.Column).First();
                column = map.ColumnDescriptor;
            }

            _eventExecution.ProcessAction();

            if (header.Column != null)
            {
                e.Handled = true;
                e.TouchDevice.Capture(header);

                manipulationStart(header, e.GetTouchPoint((FrameworkElement)header).Position);

                header.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(ColumnHeader_PointDrag));
                header.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(ColumnHeader_PointUp));
            }
        }
        void ColumnHeader_PointDrag(Object sender, TouchEventArgs e)
        {
            var header = (sender as FrameworkElement).FindParent<GridViewColumnHeader>();

            Point position = e.GetTouchPoint((FrameworkElement)header).Position;

            _eventExecution.SubmitWorkItem(new System.Action(() =>
                    manipulationMove(header, position)), this);

            e.Handled = true;
        }
        void ColumnHeader_PointUp(Object sender, TouchEventArgs e)
        {
            _eventExecution.ProcessAction();

            var header = (sender as FrameworkElement).FindParent<GridViewColumnHeader>();
            e.Handled = true;
            e.TouchDevice.Capture(null);

            Point position = e.GetTouchPoint((FrameworkElement)header).Position;
            manipulationEnd(header, position);

            header.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(ColumnHeader_PointDrag));
            header.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(ColumnHeader_PointUp));
        }

        void ColumnHeader_TouchDown(object sender, TouchEventArgs e)
        {
            _eventExecution.ProcessAction();
            _eventExecution2.ProcessAction();

            var header = sender as GridViewColumnHeader;
            PanoramicDataColumnDescriptor column = null;
            if (_mapping.Where(me => me.GridViewColumn == header.Column).Count() > 0)
            {
                MappingEntry map = _mapping.Where(me => me.GridViewColumn == header.Column).First();
                column = map.ColumnDescriptor;
            }

            if (_dragDevice1 == null && _dragDevice2 == null)
            {
                if (header.Column != null && (header.Column.Header as string) != "")
                {
                    MappingEntry fieldMapping = _mapping.Where(me => me.GridViewColumn == header.Column).First();
                    _headerColumnDescriptor1 = fieldMapping.ColumnDescriptor;
                    e.Handled = true;
                    e.TouchDevice.Capture(header);
                    _dragDevice1 = e.TouchDevice;

                    manipulationStart(header, e.GetTouchPoint((FrameworkElement)header).Position);
                }
            }
            else if (_dragDevice2 == null)
            {
                if (_headerColumnDescriptor1 != null && header.Column != null)
                {
                    _isTwoFingerExploringGraphs = true;
                    e.TouchDevice.Capture(header);
                    _dragDevice2 = e.TouchDevice;

                    // remove any other feedback that might have been triggered so far
                    InqScene inqScene = this.FindParent<InqScene>();
                    inqScene.Rem(_explorerFeedback);
                    _explorerFeedback = null;
                    _explorerFeedbackColumnDescriptor = null;
                    hideColumnReorderFeedbacks();

                    // create a new filter
                    Point curDrag = e.GetTouchPoint((FrameworkElement)header).Position;
                    Point transInqScene = header.TranslatePoint(curDrag, inqScene);
                    Point thisOffset = this.TranslatePoint(new Point(), inqScene);

                    MappingEntry fieldMapping2 = _mapping.Where(me => me.GridViewColumn == header.Column).First();

                    _twoFingerExploreFeedback = new VisualizationContainerView();
                    FilterHolderViewModel filterHolderViewModel =
                        FilterHolderViewModel.CreateDefault(_headerColumnDescriptor1, fieldMapping2.ColumnDescriptor, _filterModel.TableModel,
                        FilterRendererType.Plot);
                    _explorerFeedbackColumnDescriptor = fieldMapping2.ColumnDescriptor;
                    filterHolderViewModel.Temporary = true;
                    //_twoFingerExploreFeedback.FilterHolderViewModel = filterHolderViewModel;

                    _twoFingerExploreFeedback.SetPosition(new Pt(transInqScene.X - VisualizationContainerView.WIDTH / 2.0, thisOffset.Y - VisualizationContainerView.HEIGHT)); 
                    //_twoFingerExploreFeedback.SetDimension(new Vec(VisualizationContainerView.WIDTH, VisualizationContainerView.HEIGHT));

                    inqScene.AddNoUndo(_twoFingerExploreFeedback);
                }
            }
        }
        void ColumnHeader_TouchMove(object sender, TouchEventArgs e)
        {
            var header = sender as GridViewColumnHeader;

            if (_dragDevice1 == e.TouchDevice)
            {
                Point position = e.GetTouchPoint((FrameworkElement)header).Position;
                //manipulationMove(header, position);
                _eventExecution.SubmitWorkItem(new System.Action(() =>
                    manipulationMove(header, position)), this);

                e.Handled = true;
            }
            else if (_dragDevice2 == e.TouchDevice && _twoFingerExploreFeedback != null)
            {
                Point curDrag = e.GetTouchPoint((FrameworkElement)header).Position;
                _eventExecution2.SubmitWorkItem(new System.Action(() =>
                    manipulationMoveTwo(header, curDrag)), this);

                e.Handled = true;
            }
        }
        void ColumnHeader_TouchUp(object sender, TouchEventArgs e)
        {
            _eventExecution.ProcessAction();
            _eventExecution2.ProcessAction();

            if (_dragDevice1 == e.TouchDevice)
            {
                var header = sender as GridViewColumnHeader;
                e.Handled = true;
                e.TouchDevice.Capture(null);

                Point position = e.GetTouchPoint((FrameworkElement)header).Position;
                manipulationEnd(header, position);

                _dragDevice1 = null;
            }
            else if (_dragDevice2 == e.TouchDevice)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice2 = null;

                //if (_twoFingerExploreFeedback.FilterHolderViewModel.Temporary)
                {
                    InqScene inqScene = this.FindParent<InqScene>();
                    inqScene.Rem(_twoFingerExploreFeedback);
                }
                //else
                {
                    //_twoFingerExploreFeedback.FilterHolderViewModel.TableModel.AddIncomingFilter(_twoFingerExploreFeedback.FilterHolderViewModel);
                    _twoFingerExploreFeedback.InitPostionAndDimension(_twoFingerExploreFeedback.GetPosition(), _twoFingerExploreFeedback.GetSize());
                }
                _twoFingerExploreFeedback = null;
                _explorerFeedbackColumnDescriptor = null;
            }
        }

        private void manipulationMoveTwo(GridViewColumnHeader header, Point curDrag)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            Point trans = header.TranslatePoint(curDrag, inqScene);

            if (((double)_twoFingerExploreFeedback.GetValue(Canvas.TopProperty)) - trans.Y > -200 ||
                ((double)_twoFingerExploreFeedback.GetValue(Canvas.TopProperty)) - trans.Y < -460)
            {
                //_twoFingerExploreFeedback.FilterHolderViewModel.Temporary = false;
            }

            //if (_twoFingerExploreFeedback.FilterHolderViewModel.Temporary)
            {
                // move filter
                _twoFingerExploreFeedback.SetValue(Canvas.LeftProperty, trans.X - VisualizationContainerView.WIDTH / 2.0);

                GridViewColumnHeader overHeader = findOverColumnHeader(trans);

                if (_headerColumnDescriptor1 != null && overHeader != null && overHeader.Column != null)
                {
                    MappingEntry fieldMapping2 = _mapping.Where(me => me.GridViewColumn == overHeader.Column).First();
                    if (fieldMapping2.ColumnDescriptor != _explorerFeedbackColumnDescriptor)
                    {
                        _explorerFeedbackColumnDescriptor = fieldMapping2.ColumnDescriptor;

                        FilterHolderViewModel filterHolderViewModel =
                        FilterHolderViewModel.CreateDefault(_headerColumnDescriptor1, fieldMapping2.ColumnDescriptor, _filterModel.TableModel, FilterRendererType.Plot);
                        _explorerFeedbackColumnDescriptor = fieldMapping2.ColumnDescriptor;
                        filterHolderViewModel.Temporary = true;
                        //_twoFingerExploreFeedback.FilterHolderViewModel = filterHolderViewModel;
                    }
                }
            }
            //else
            {
                _twoFingerExploreFeedback.SetValue(Canvas.TopProperty, trans.Y - VisualizationContainerView.HEIGHT);
                _twoFingerExploreFeedback.SetValue(Canvas.LeftProperty, trans.X - VisualizationContainerView.WIDTH / 2.0);
                //_twoFingerExploreFeedback.FilterHolderViewModel.Center = new Point((trans.X - VisualizationContainerView.WIDTH / 2.0) + VisualizationContainerView.WIDTH / 2.0, (trans.Y - VisualizationContainerView.HEIGHT) + VisualizationContainerView.HEIGHT / 2.0);
            }
        }

        private void manipulationStart(GridViewColumnHeader header, Point position)
        {
            _manipulationStartTime = DateTime.Now.Ticks;
            _startDrag1 = position;
            _isTwoFingerExploringGraphs = false;

            if (CanResize &&
                _startDrag1.X > header.Column.ActualWidth - 15 &&
                _startDrag1.X < header.Column.ActualWidth + 15)
            {
                _isResizing = true;
            }
            else
            {
                _isResizing = false;
            }
        }
        private void manipulationMove(GridViewColumnHeader header, Point position)
        {
            Point curDrag = position;
            Vector vec = curDrag - _startDrag1;
            Point dragDelta = new Point(vec.X, vec.Y);
            InqScene inqScene = this.FindParent<InqScene>();

            if (!_isTwoFingerExploringGraphs)
            {
                if (CanResize && _isResizing)
                {
                    header.Column.Width = Math.Max(35, header.Column.ActualWidth + vec.X);
                    _startDrag1 = curDrag;
                }
                if ((CanReorder || CanDrag) && _explorerFeedback == null && !_isResizing)
                {
                    Point trans = header.TranslatePoint(curDrag, inqScene);

                    if (header.VisualDescendentsOfType<AttributeView>().Count() > 0)
                    {
                        AttributeView simpleGridViewColumnHeader = header.VisualDescendentsOfType<AttributeView>().First();

                        // create drag feedback
                        if (vec.Length > 10 && !_isSimpleGridViewColumnHeaderMoveFeedbackShown)
                        {
                            simpleGridViewColumnHeader.ManipulationStart(trans);
                            _isSimpleGridViewColumnHeaderMoveFeedbackShown = true;

                            _isTwoFingerExploringGraphs = false;
                        }
                        // move filter drag feedback
                        if (_isSimpleGridViewColumnHeaderMoveFeedbackShown)
                        {
                            simpleGridViewColumnHeader.ManipulationMove(trans);
                        }
                    }
                }
                if (CanExplore && !_isSimpleGridViewColumnHeaderMoveFeedbackShown && !_isResizing)
                {
                    Point trans = header.TranslatePoint(curDrag, inqScene);
                    Point thisOffset = this.TranslatePoint(new Point(), inqScene);

                    // create explorer feedback
                    if (!(_manipulationStartTime + TimeSpan.FromSeconds(1.0).Ticks > DateTime.Now.Ticks) && _explorerFeedback == null)
                    {
                        createExplorerDragFeedback(header.TranslatePoint(curDrag, inqScene), header);
                        _isTwoFingerExploringGraphs = false;
                    }

                    // move filter drag feedback
                    if (_explorerFeedback != null)
                    {
                        if (((double)_explorerFeedback.GetValue(Canvas.TopProperty)) - trans.Y > -200 ||
                            ((double)_explorerFeedback.GetValue(Canvas.TopProperty)) - trans.Y < -460)
                        {
                            //_explorerFeedback.FilterHolderViewModel.Temporary = false;
                        }

                       // if (_explorerFeedback.FilterHolderViewModel.Temporary)
                        {
                            // move filter
                            _explorerFeedback.SetValue(Canvas.LeftProperty, trans.X - VisualizationContainerView.WIDTH / 2.0);
                            _explorerFeedback.SetValue(Canvas.TopProperty, thisOffset.Y - VisualizationContainerView.HEIGHT);


                            Console.WriteLine((trans.X - VisualizationContainerView.WIDTH / 2.0) + " " + (thisOffset.Y - VisualizationContainerView.HEIGHT));
                            GridViewColumnHeader overHeader = findOverColumnHeader(trans);

                            if (overHeader != null && overHeader.Column != null && (overHeader.Column.Header as string) != "")
                            {
                                MappingEntry fieldMapping = _mapping.Where(me => me.GridViewColumn == overHeader.Column).First();
                                if (fieldMapping.ColumnDescriptor != _explorerFeedbackColumnDescriptor)
                                {
                                    FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateDefault(fieldMapping.ColumnDescriptor, _filterModel.TableModel);
                                    filterHolderViewModel.Temporary = true;
                                    //filterHolderViewModel.Color = _explorerFeedback.FilterHolderViewModel.Color;

                                    //_explorerFeedback.FilterHolderViewModel = filterHolderViewModel;
                                    _explorerFeedbackColumnDescriptor = fieldMapping.ColumnDescriptor;
                                }
                            }
                        }
                        //else
                        {
                            _explorerFeedback.SetValue(Canvas.TopProperty, trans.Y - VisualizationContainerView.HEIGHT);
                            _explorerFeedback.SetValue(Canvas.LeftProperty, trans.X - VisualizationContainerView.WIDTH / 2.0);
                            //_explorerFeedback.FilterHolderViewModel.Center = new Point((trans.X - VisualizationContainerView.WIDTH / 2.0) + VisualizationContainerView.WIDTH / 2.0, (trans.Y - VisualizationContainerView.HEIGHT) + VisualizationContainerView.HEIGHT / 2.0);
                        }
                    }
                }
            }
        }
        private void manipulationEnd(GridViewColumnHeader header, Point position)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            Point curDrag = position;
            Point fromInqScene = header.TranslatePoint(curDrag, inqScene);
            Point fromThis = header.TranslatePoint(curDrag, this);

            if (header.VisualDescendentsOfType<AttributeView>().Count() > 0)
            {
                AttributeView simpleGridViewColumnHeader = header.VisualDescendentsOfType<AttributeView>().First();

                if (_isSimpleGridViewColumnHeaderMoveFeedbackShown)
                {
                    simpleGridViewColumnHeader.ManipulationEnd(fromInqScene);
                    _isSimpleGridViewColumnHeaderMoveFeedbackShown = false;
                }
                else if (_explorerFeedback != null)
                {
                   // if (_explorerFeedback.FilterHolderViewModel.Temporary)
                    {
                        inqScene.Rem(_explorerFeedback);
                    }
                    //else
                    {
                        //_explorerFeedback.FilterHolderViewModel.TableModel.AddIncomingFilter(_explorerFeedback.FilterHolderViewModel);
                        _explorerFeedback.InitPostionAndDimension(_explorerFeedback.GetPosition(), _explorerFeedback.GetSize());
                    }
                    _explorerFeedback = null;
                    _explorerFeedbackColumnDescriptor = null;
                }
                else
                {
                    if (!_isTwoFingerExploringGraphs && _manipulationStartTime + TimeSpan.FromSeconds(0.5).Ticks > DateTime.Now.Ticks)
                    {
                        int count = _mapping.Where(me => me.GridViewColumn == header.Column).Count();
                        if (!_isResizing && header.Column != null && count > 0)
                        {
                            simpleGridViewColumnHeader.DisplayRadialControl(fromInqScene);
                        }
                    }
                }
            }
            _isResizing = false;
            _manipulationStartTime = 0;
        }

        public void AttributeViewModelMoved(AttributeViewModel sender, AttributeViewModelEventArgs e, bool overElement)
        {
            /*SimpleDataGrid parentGrid = sender.FindParent<SimpleDataGrid>();
            InqScene inqScene = this.FindParent<InqScene>();
         
            // hide cloumn header reorder drop highlights 
            hideColumnReorderFeedbacks();

            if (overElement &&
                (_tableModel == null || _tableModel == e.TableModel))
            {
                Point fromThis = inqScene.TranslatePoint(e.Bounds.Center, this);

                if (CanReorder)
                {
                    IEnumerable<GridViewColumnHeader> headers = listView.VisualDescendentsOfType<GridViewColumnHeader>();
                    FrameworkElement highlight = null;

                    // find closest header reorder drop highlight 
                    GridViewColumnHeader closestHeader = findClosestColumnHeader(e.Bounds.Center);
                    highlight = closestHeader.FirstVisualDescendentByName("dragHighlight");
                    highlight.Visibility = System.Windows.Visibility.Visible;
                }
            }*/
        }

        public void AttributeViewModelDropped(AttributeViewModel sender, AttributeViewModelEventArgs e)
        {
            /*SimpleDataGrid parentGrid = sender.FindParent<SimpleDataGrid>();

            if (_filterModel != null && _filterModel.GetColumnDescriptorsForOption(Option.X).Count == 0)
            {
                _filterModel.TableModel = e.TableModel;
                _filterModel.AddOptionColumnDescriptor(Option.X, e.ColumnDescriptor.Clone() as PanoramicDataColumnDescriptor);
                return;
            }

            if (!_isResizing && (CanReorder || CanDrag) &&
                (_tableModel == null || _tableModel == e.TableModel))
            {
                InqScene inqScene = this.FindParent<InqScene>();

                Point fromThis = inqScene.TranslatePoint(e.Bounds.Center, this);
                GridViewColumnHeader header = (sender as FrameworkElement).FindParent<GridViewColumnHeader>();

                // hide cloumn header reorder drop highlights 
                hideColumnReorderFeedbacks();

                if (CanReorder)
                {
                    // find closest header reorder drop highlight 
                    GridViewColumnHeader closestHeader = findClosestColumnHeader(e.Bounds.Center);
                    GridViewColumnCollection columns = ((GridView)listView.View).Columns;

                    if (parentGrid == this) 
                    {
                        MappingEntry map = null;
                        IEnumerable<MappingEntry> mapEntries = _mapping.Where(me => me.GridViewColumn == header.Column);
                        if (mapEntries.Count() > 0)
                        {
                            map = mapEntries.First();
                        }

                        // column already exists within this datagrid
                        if (map != null)
                        {
                            if (closestHeader.Column == null)
                            {
                                columns.Remove(header.Column);
                                columns.Add(header.Column);
                                _mapping.Remove(map);
                                _mapping.Add(map);
                            }
                            else if ((closestHeader.Column.Header as string) == "")
                            {
                                columns.Remove(header.Column);
                                columns.Insert(1, header.Column);
                                _mapping.Remove(map);
                                _mapping.Insert(1, map);
                            }
                            else
                            {
                                MappingEntry mapClosest =
                                    _mapping.Single(me => me.GridViewColumn == closestHeader.Column);

                                if (map.GridViewColumn != mapClosest.GridViewColumn)
                                {
                                    columns.Remove(header.Column);
                                    _mapping.Remove(map);

                                    int index = columns.IndexOf(closestHeader.Column);
                                    if (index != -1)
                                    {
                                        columns.Insert(index, header.Column);
                                    }

                                    int indexMap = _mapping.IndexOf(mapClosest);
                                    if (indexMap != -1)
                                    {
                                        _mapping.Insert(indexMap, map);
                                    }
                                }
                            }

                            for (int i = 0; i < _mapping.Count; i++)
                            {
                                _mapping[i].ColumnDescriptor.Order = i;
                            }
                        }
                    }
                    // new column
                    else
                    {
                        PanoramicDataColumnDescriptor clone =
                                   (PanoramicDataColumnDescriptor)e.ColumnDescriptor.Clone();
                        if (closestHeader.Column == null)
                        {
                            int i = 0;
                            foreach(var me in _mapping) 
                            {
                                me.ColumnDescriptor.Order = i;
                                i++;
                            }
                            clone.Order = i;
                        }
                        else if ((closestHeader.Column.Header as string) == "")
                        {
                            clone.Order = 0;
                            int i = 1;
                            foreach (var me in _mapping)
                            {
                                me.ColumnDescriptor.Order = i;
                                i++;
                            }
                        }
                        else
                        {
                            MappingEntry mapClosest = _mapping.Single(me => me.GridViewColumn == closestHeader.Column);
                            
                            int indexMap = _mapping.IndexOf(mapClosest);
                            if (indexMap != -1)
                            {
                                int i = 0;
                                for(i = 0; i < indexMap; i++)
                                {
                                    _mapping[i].ColumnDescriptor.Order = i;
                                }
                                clone.Order = i;

                                for (int k = i; k < _mapping.Count; k++)
                                {
                                    _mapping[k].ColumnDescriptor.Order = k+1;
                                }
                            }
                        }
                        _filterModel.AddOptionColumnDescriptor(Option.X, clone);
                    }
                }
            }*/
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return new GeometryHitTestResult(this, IntersectionDetail.Intersects);
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
            InqScene inqScene = this.FindParent<InqScene>();
            IEnumerable<GridViewColumnHeader> headers = listView.VisualDescendentsOfType<GridViewColumnHeader>();

            // find closest header reorder drop highlight 
            GridViewColumnHeader closestHeader = null;
            double closestXDist = double.MaxValue;
            foreach (var h in headers)
            {
                if (h.ActualWidth != 0.0)
                {
                    Point p = inqScene.TranslatePoint(fromInqScene, h);
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
            InqScene inqScene = this.FindParent<InqScene>();
            IEnumerable<GridViewColumnHeader> headers = listView.VisualDescendentsOfType<GridViewColumnHeader>();

            foreach (var h in headers)
            {
                if (h.ActualWidth != 0.0)
                {
                    Point p = inqScene.TranslatePoint(fromInqScene, h);
                    if (p.X > 0 && p.X < h.ActualWidth)
                    {
                        return h;
                    }
                }
            }
            return null;
        }
        private void createExplorerDragFeedback(Point offset, GridViewColumnHeader gridViewColumnHeader)
        {
            IEnumerable<MappingEntry> maps = _mapping.Where(me => me.GridViewColumn == gridViewColumnHeader.Column);
            if (maps.Count() == 1)
            {
                MappingEntry map = maps.First();
                InqScene inqScene = this.FindParent<InqScene>();

                _explorerFeedback = new VisualizationContainerView();
                _explorerFeedbackColumnDescriptor = map.ColumnDescriptor;
                FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateDefault(map.ColumnDescriptor, _filterModel.TableModel);
                filterHolderViewModel.Temporary = true;

                //_explorerFeedback.FilterHolderViewModel = filterHolderViewModel;
                //_explorerFeedback.SetDimension(new Vec(VisualizationContainerView.WIDTH, VisualizationContainerView.HEIGHT));
                //filterHolderViewModel.AddIncomingFilter(_filterModel, FilteringType.Filter);
                //_explorerFeedback.InitPostionAndDimension(offset, new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT));
                
                inqScene.AddNoUndo(_explorerFeedback);
                //_explorerFeedback = null;
                //FilterHolder filter = new FilterHolder(inqScene);
                //filterHolderViewModel = FilterHolderViewModel.CreateDefault(map.ColumnDescriptor, _filterModel.TableModel);
                //filter.FilterHolderViewModel = filterHolderViewModel;
                //filterHolderViewModel.Center = new Point();
                //filter.InitPostionAndDimension(offset, new Vec(FilterHolder.WIDTH, FilterHolder.HEIGHT));
            }
        }

        public void NotifyStroqAdded(starPadSDK.Inq.Stroq s)
        {
            InqScene inqScene = this.FindParent<InqScene>();
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
                            inqScene.TranslatePoint(new Point(bounds.Left, bounds.Top), this),
                            inqScene.TranslatePoint(new Point(bounds.Right, bounds.Top), this),
                            inqScene.TranslatePoint(new Point(bounds.Right, bounds.Bottom), this),
                            inqScene.TranslatePoint(new Point(bounds.Left, bounds.Bottom), this)
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

                        if (rows.Count > 0 && RowsSelected != null)
                        {
                            RowsSelected(this, rows);
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

                                    if ((map.ColumnDescriptor.SortMode == SortMode.None ||
                                         map.ColumnDescriptor.SortMode == SortMode.Asc) && downStroq)
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
                                    }
                                }
                            }
                        }
                    }
                }
            }

            inqScene.Rem(s);
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

            (sender as FrameworkElement).AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(ColumnHeader_TouchDownEvent));
        }

        private void FrameworkElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).RemoveHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(ColumnHeader_TouchDownEvent));
        }
    }
    
    public class FilterHighlightImageConverter : IValueConverter
    {
        private FilterModel _filterModel = null;
        private List<FilterModel> _filterModels = null;
        private float _width = 0.0f;
        private float _dim = 23;

        public FilterHighlightImageConverter(FilterModel filterModel)
        {
            _filterModel = filterModel;
            _filterModels = _filterModel.GetIncomingFilterModels(FilteringType.Brush).ToList();
            _width = (float)(_dim / (double)_filterModels.Count);
        }

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            System.Drawing.Bitmap pg = new System.Drawing.Bitmap((int)_dim, (int)_dim);
            if (value != null)
            {
                PanoramicDataRow row = (PanoramicDataRow)value;
                System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(pg);
                if (_filterModels.Count() > 0)
                {
                    for (int c = 0; c < _filterModels.Count; c++)
                    {
                        FilterModel fm = _filterModels[c];
                        System.Drawing.Color fmColor = System.Drawing.Color.FromArgb(fm.Color.A, fm.Color.R,
                            fm.Color.G, fm.Color.B);
                        System.Drawing.Brush fmBrush = new System.Drawing.SolidBrush(fmColor);

                        System.Drawing.Color fmColorTrans = System.Drawing.Color.FromArgb(50, fm.Color.R,
                                fm.Color.G, fm.Color.B);
                        System.Drawing.Brush fmBrushTrans = new System.Drawing.SolidBrush(fmColorTrans);

                        gr.FillRectangle(fmBrushTrans,
                            (float)(c * _width), (float)(0),
                            _width, (float)_dim);

                        double percantage = 0.0;
                        if (row.PassesFilterModel.ContainsKey(fm) && fm.Selected)
                        {
                            percantage = row.PassesFilterModel[fm];
                        }

                        gr.FillRectangle(fmBrush,
                            (float)(c * _width), (float)((1.0 - percantage) * _dim),
                            _width, (float)(percantage * _dim));
                    }
                }

                System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 125, 125, 125));
                gr.DrawRectangle(pen, 0, 0, _dim - 1, _dim - 1);
            }

            return pg.LoadImage();
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ColorByImageConverter : IValueConverter
    {
        private FilterModel _filterModel = null;
        private float _dim = 23;

        public ColorByImageConverter(FilterModel filterModel)
        {
            _filterModel = filterModel;
        }

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            System.Drawing.Bitmap pg = new System.Drawing.Bitmap((int)_dim, (int)_dim);
            if (value != null)
            {
                PanoramicDataRow row = (PanoramicDataRow)value;
                List<PanoramicDataValue> dataValues = new List<PanoramicDataValue>();
                List<PanoramicDataValue> groupedDataValues = new List<PanoramicDataValue>();
                string grouping = "";
                if (_filterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count > 0)
                {
                    List<string> groupingList = new List<string>();
                    foreach (var columnDescriptor in _filterModel.GetColumnDescriptorsForOption(Option.ColorBy))
                    {
                        PanoramicDataValue dataValue = row.GetValue(columnDescriptor);
                        string main = "";
                        string sub = "";
                        columnDescriptor.GetLabels(out main, out sub, false);

                        groupingList.Add(main + ":" + sub + ":" + dataValue.StringValue);
                    }
                    grouping = string.Join(":", groupingList);
                }

                System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(pg);
                if (grouping != "")
                {
                        Color c = RendererResources.GetGroupingColor(grouping);
                        System.Drawing.Color fmColor = System.Drawing.Color.FromArgb(c.A, c.R,
                            c.G, c.B);
                        System.Drawing.Brush fmBrush = new System.Drawing.SolidBrush(fmColor);

                        gr.FillRectangle(fmBrush,
                            (float)(0), (float)(0),
                            (float)_dim, (float)_dim);
                }

                System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 125, 125, 125));
                gr.DrawRectangle(pen, 0, 0, _dim - 1, _dim - 1);
            }

            return pg.LoadImage();
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
        public PanoramicDataColumnDescriptor ColumnDescriptor { get; set; }
        public int FieldsIndex { get; set; }
    }

    public class SettingsMapping
    {
        public PanoramicDataColumnDescriptor ColumnDescriptor { get; set; }
        public StackPanel StackPanel { get; set; }
        public Border Border { get; set; }
    }

    public class DelayedEventExecution
    {
        int submitCount = 0;
        int execCount = 0;
        private System.Action _nextAction;
        private System.Windows.Threading.DispatcherPriority _priority = System.Windows.Threading.DispatcherPriority.Render;
        public DelayedEventExecution(System.Windows.Threading.DispatcherPriority priority = System.Windows.Threading.DispatcherPriority.Render)
        {
            _priority = priority;
        }

        public void SubmitWorkItem(System.Action action, SimpleDataGrid grid)
        {
            grid.SetValue(SimpleDataGrid.TestProperty, !((bool)grid.GetValue(SimpleDataGrid.TestProperty)));
            _nextAction = action;
            submitCount++;
            //Application.Current.Dispatcher.BeginInvoke(new System.Action(() => ProcessAction(action)), _priority);
        }

        public void ProcessAction()
        {
            if (_nextAction != null)
            {
                _nextAction.Invoke();
                _nextAction = null;
                execCount++;
            }
        }
    }

    public class MultiValueImageConverter : IMultiValueConverter
    {
        System.Drawing.Color red = System.Drawing.Color.FromArgb(0xff, 0xff, 0, 0);
        System.Drawing.Brush redBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
        System.Drawing.Brush whiteBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0xff, 0xff, 0xff, 0xff));
        System.Drawing.Pen gray = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0xff, 0xcc, 0xcc, 0xcc), 1);
        System.Drawing.Pen black = new System.Drawing.Pen(System.Drawing.Color.Black, 1);

        public object Convert(object[] values, Type targetType, object parameter,
            CultureInfo culture)
        {
            int w = (int)Math.Max(1, (double)values[0]);
            int h = (int)Math.Max(1, (double)values[1]);

            System.Drawing.Bitmap pg = new System.Drawing.Bitmap(w, h);
            System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(pg);

            int padding = (int) Math.Ceiling(h * 0.3);
            //gr.FillRectangle(whiteBrush, 0, 0, w, h);
            gr.DrawRectangle(gray, padding, padding, w - padding * 2, h - padding * 2);

            if (values[2] != DependencyProperty.UnsetValue &&
                (string)values[2] != "")
            {
                double l = w - padding * 2;
                string[] entries = ((string)values[2]).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                double min = double.Parse(entries[0]);
                double max = double.Parse(entries[1]);

                for (int i = 2; i < entries.Count(); i++)
                {
                    double x = ((double.Parse(entries[i]) - min) / (max - min)) * l + padding;
                    gr.DrawLine(black, (int)x, padding, (int)x, h - padding);
                }
            }

            return pg.LoadImage();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
