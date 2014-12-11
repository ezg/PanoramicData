using System.Reactive.Linq;
using PanoramicDataModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using PixelLab.Common;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using starPadSDK.Inq;
using PanoramicData.model.view;
using PanoramicData.view.other;
using PanoramicData.controller.data;
using PanoramicData.view.math;
using CombinedInputAPI;

namespace PanoramicData.view.table
{
    /// <summary>
    /// Interaction logic for SimpleGridViewColumnHeader.xaml
    /// </summary>
    public partial class SimpleGridViewColumnHeader : UserControl
    {
        public static readonly DependencyProperty IsInteractiveProperty = 
            DependencyProperty.Register("IsInteractive", typeof(bool), 
            typeof(SimpleGridViewColumnHeader),
            new PropertyMetadata(true, OnIsInteractiveChanged));

        public bool IsInteractive
        {
            get
            {
                return (bool)GetValue(IsInteractiveProperty);
            }
            set
            {
                SetValue(IsInteractiveProperty, value);
            }
        }

        static void OnIsInteractiveChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as SimpleGridViewColumnHeader).OnIsInteractiveChanged(args);
        }

        protected void OnIsInteractiveChanged(DependencyPropertyChangedEventArgs args)
        {
            this.MouseDown -= SimpleGridViewColumnHeader_MouseDown;
            this.RemoveHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDownEvent));

            if ((bool)args.NewValue)
            {
                this.MouseDown += SimpleGridViewColumnHeader_MouseDown;
                this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDownEvent));
            }
        }

        public static readonly DependencyProperty IsSimpleRenderingProperty =
            DependencyProperty.Register("IsSimpleRendering", typeof(bool),
            typeof(SimpleGridViewColumnHeader),
            new PropertyMetadata(false, OnIsSimpleRenderingChanged));

        public bool IsSimpleRendering
        {
            get
            {
                return (bool)GetValue(IsSimpleRenderingProperty);
            }
            set
            {
                SetValue(IsSimpleRenderingProperty, value);
            }
        }

        static void OnIsSimpleRenderingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as SimpleGridViewColumnHeader).OnIsSimpleRenderingChanged(args);
        }

        protected void OnIsSimpleRenderingChanged(DependencyPropertyChangedEventArgs args)
        {
            if ((bool) args.NewValue)
            {
                subTB.Visibility = Visibility.Collapsed;
                filterGrid.Visibility = Visibility.Collapsed;
                downArrow.Visibility = Visibility.Collapsed;
                upArrow.Visibility = Visibility.Collapsed;
                mainTB.SetValue(Grid.RowSpanProperty, 2);
                mainTB.TextAlignment = TextAlignment.Left;
                mainTB.FontWeight = FontWeights.Normal;
                this.MinHeight = 20;
                HeaderBorder.Background = Brushes.White;
                mainGrid.Margin = new Thickness(0, 2, 0, 2);

                this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDownEvent));
            }
        }

        public static readonly DependencyProperty EnableRadialMenuProperty =
            DependencyProperty.Register("EnableRadialMenu", typeof(bool),
            typeof(SimpleGridViewColumnHeader),
            new PropertyMetadata(true, OnEnableRadialMenuChanged));

        public bool EnableRadialMenu
        {
            get
            {
                return (bool)GetValue(EnableRadialMenuProperty);
            }
            set
            {
                SetValue(EnableRadialMenuProperty, value);
            }
        }

        static void OnEnableRadialMenuChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as SimpleGridViewColumnHeader).OnEnableRadialMenuChanged(args);
        }

        protected void OnEnableRadialMenuChanged(DependencyPropertyChangedEventArgs args)
        {
        }

        public static readonly DependencyProperty EnableRemoveOptionInRadialMenuProperty =
            DependencyProperty.Register("EnableRemoveOptionInRadialMenu", typeof(bool),
            typeof(SimpleGridViewColumnHeader),
            new PropertyMetadata(true, OnEnableRemoveOptionInRadialMenuChanged));

        public bool EnableRemoveOptionInRadialMenu
        {
            get
            {
                return (bool)GetValue(EnableRemoveOptionInRadialMenuProperty);
            }
            set
            {
                SetValue(EnableRemoveOptionInRadialMenuProperty, value);
            }
        }

        static void OnEnableRemoveOptionInRadialMenuChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as SimpleGridViewColumnHeader).OnEnableRemoveOptionInRadialMenuChanged(args);
        }

        protected void OnEnableRemoveOptionInRadialMenuChanged(DependencyPropertyChangedEventArgs args)
        {
        }

        public static readonly DependencyProperty EnableScaleFunctionInRadialMenuProperty =
            DependencyProperty.Register("EnableScaleFunctionInRadialMenu", typeof(bool),
            typeof(SimpleGridViewColumnHeader),
            new PropertyMetadata(true, OnEnableRunningTotaInRadialMenuChanged));

        public bool EnableScaleFunctionInRadialMenu
        {
            get
            {
                return (bool)GetValue(EnableScaleFunctionInRadialMenuProperty);
            }
            set
            {
                SetValue(EnableScaleFunctionInRadialMenuProperty, value);
            }
        }

        static void OnEnableRunningTotaInRadialMenuChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as SimpleGridViewColumnHeader).OnEnableScaleFunctionInRadialMenuChanged(args);
        }

        protected void OnEnableScaleFunctionInRadialMenuChanged(DependencyPropertyChangedEventArgs args)
        {
        }

        public static readonly DependencyProperty EnableMoveByPenProperty =
           DependencyProperty.Register("EnableMoveByPen", typeof(bool),
           typeof(SimpleGridViewColumnHeader),
           new PropertyMetadata(false, OnEnableMoveByPenChanged));

        public bool EnableMoveByPen
        {
            get
            {
                return (bool)GetValue(EnableMoveByPenProperty);
            }
            set
            {
                SetValue(EnableMoveByPenProperty, value);
            }
        }

        static void OnEnableMoveByPenChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as SimpleGridViewColumnHeader).OnEnableMoveByPenChanged(args);
        }

        protected void OnEnableMoveByPenChanged(DependencyPropertyChangedEventArgs args)
        {
        }

        public static readonly DependencyProperty FilterModelProperty =
            DependencyProperty.Register("FilterModel", typeof(FilterModel),
            typeof(SimpleGridViewColumnHeader),
            new PropertyMetadata());

        public FilterModel FilterModel
        {
            get
            {
                return (FilterModel)GetValue(FilterModelProperty);
            }
            set
            {
                SetValue(FilterModelProperty, value);
            }
        }

        public static readonly DependencyProperty TableModelProperty =
            DependencyProperty.Register("TableModel", typeof(TableModel),
            typeof(SimpleGridViewColumnHeader),
            new PropertyMetadata());

        public TableModel TableModel
        {
            get
            {
                return (TableModel)GetValue(TableModelProperty);
            }
            set
            {
                SetValue(TableModelProperty, value);
            }
        }
      
        public static event EventHandler<ColumnHeaderEventArgs> Moved;
        public static event EventHandler<ColumnHeaderEventArgs> Dropped;

        public void FireMoved(SimpleGridViewColumnHeader shadow)
        {
            if (Moved != null)
            {
                InqScene inqScene = this.FindParent<InqScene>();
                Rct bounds = shadow.GetBounds(inqScene);
                Moved(this, new ColumnHeaderEventArgs(bounds, DataContext as PanoramicDataColumnDescriptor, TableModel, FilterModel, true));
            }
        }

        public void FireDropped(SimpleGridViewColumnHeader shadow, FilterModel linkFromFilterModel = null)
        {
            if (Dropped != null)
            {
                InqScene inqScene = this.FindParent<InqScene>();
                Rct bounds = shadow.GetBounds(inqScene);

                Dropped(this, new ColumnHeaderEventArgs(bounds, DataContext as PanoramicDataColumnDescriptor, TableModel, FilterModel, !_boundsChanged, ColumnHeaderEventArgsCommand.None, linkFromFilterModel));
            }
        }

        private bool _isShadow = false;
        private SimpleGridViewColumnHeader _shadow = null; 
        private long _manipulationStartTime = 0;
        private Point _startDrag = new Point(0, 0);
        private Point _currentFromInqScene = new Point(0, 0);
        private bool _boundsChanged = false;


        public SimpleGridViewColumnHeader(): this(false) 
        {
        }

        public SimpleGridViewColumnHeader(bool isShadow, bool renderDark = false)
        {
            InitializeComponent();

            this._isShadow = isShadow;

            if (_isShadow)
            {
                HeaderBorder.Background = new SolidColorBrush(Color.FromArgb(70, 125, 125, 125));
                HeaderBorder.HorizontalAlignment = HorizontalAlignment.Center;
                HeaderBorder.VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                HeaderBorder.Background = renderDark ? Brushes.DarkGray : Brushes.White;//Brushes.DarkGray;
                rectHighlight.Visibility = Visibility.Collapsed;
            }
            if (Properties.Settings.Default.PanoramicDataEnableStable)
            {
                double s = Properties.Settings.Default.PanoramicDataStableScaleFactor;
                mainTB.FontSize = Properties.Settings.Default.PanoramicDataStableLabelFontSize;
                this.MinHeight = 20 * s;
            }
            this.MouseDown += SimpleGridViewColumnHeader_MouseDown;
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDownEvent));
            SetBinding(MyDataContextProperty, new Binding());
        }

        public void ShortcutGesture(string recog)
        {
            if (EnableRadialMenu)
            {
                PanoramicDataColumnDescriptor cd = (DataContext as PanoramicDataColumnDescriptor);
                if (cd.DataType == DataTypeConstants.FLOAT ||
                    cd.DataType == DataTypeConstants.INT ||
                    cd.DataType == DataTypeConstants.BIT)
                {
                    if (recog.Equals("a"))
                    {
                        if (cd.AggregateFunction == AggregateFunction.Avg)
                        {
                            cd.AggregateFunction = AggregateFunction.None;
                        }
                        else
                        {
                            cd.AggregateFunction = AggregateFunction.Avg;
                        }
                    }
                    else if (recog.Equals("s"))
                    {
                        if (cd.AggregateFunction == AggregateFunction.Sum)
                        {
                            cd.AggregateFunction = AggregateFunction.None;
                        }
                        else
                        {
                            cd.AggregateFunction = AggregateFunction.Sum;
                        }
                    }
                }
                if (FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count > 0 &&
                    FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count(cd2 => cd2.IsBinned && cd.MatchSimple(cd)) > 0)
                {
                    if (recog.Equals("b"))
                    {
                        if (cd.AggregateFunction == AggregateFunction.Bin)
                        {
                            cd.AggregateFunction = AggregateFunction.None;
                        }
                        else
                        {
                            cd.AggregateFunction = AggregateFunction.Bin;
                        }
                    }
                }
                if (recog.Equals("c"))
                {
                    if (cd.AggregateFunction == AggregateFunction.Count)
                    {
                        cd.AggregateFunction = AggregateFunction.None;
                    }
                    else
                    {
                        cd.AggregateFunction = AggregateFunction.Count;
                    }
                }
                if (recog.Equals("g"))
                {
                    var clone = (PanoramicDataColumnDescriptor)cd.SimpleClone();
                    clone.IsGrouped = true;

                    if (FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Contains(clone))
                    {
                        var remove =
                            FilterModel.GetColumnDescriptorsForOption(Option.GroupBy)
                                .Where(cd2 => clone.MatchComplete(cd2)).ToList();
                        if (remove.Count > 0)
                        {
                            FilterModel.RemoveGrouping(remove[0]);
                        }
                    }
                    else
                    {
                        FilterModel.AddGrouping(clone);
                    }
                }
            }
        }

        private TouchDevice _dragDevice1 = null;
        private void SimpleGridViewColumnHeader_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if (((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus) || EnableMoveByPen)
            {
                if (_dragDevice1 == null)
                {
                    e.Handled = true;
                    e.TouchDevice.Capture(this);
                    InqScene inqScene = this.FindParent<InqScene>();
                    Point fromInqScene = e.GetTouchPoint(inqScene).Position;

                    _manipulationStartTime = DateTime.Now.Ticks;
                    _startDrag = fromInqScene;

                    this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDragEvent));
                    this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchUpEvent));
                    _dragDevice1 = e.TouchDevice;
                }
            }
        }

        void SimpleGridViewColumnHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                e.Handled = true;
                e.MouseDevice.Capture(this);
                InqScene inqScene = this.FindParent<InqScene>();
                Point fromInqScene = e.GetPosition(inqScene);

                _manipulationStartTime = DateTime.Now.Ticks;
                _startDrag = fromInqScene;

                this.MouseMove += SimpleGridViewColumnHeader_MouseMove;
                this.MouseUp += SimpleGridViewColumnHeader_MouseUp;
            }
        }

        public void ManipulationStart(Point fromInqScene)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            if (inqScene != null)
            {
                _currentFromInqScene = fromInqScene;
                _shadow = new SimpleGridViewColumnHeader(true);
                _shadow.DataContext = this.DataContext;
                //_shadow.Width = this.ActualWidth;
                //_shadow.Height = 40;// this.ActualHeight;
                //_shadow.HeaderBorder.Width = this.ActualWidth;
                //_shadow.HeaderBorder.Height = 40;// this.ActualHeight;

                _shadow.Measure(new Size(double.PositiveInfinity,
                                         double.PositiveInfinity));

                double add = IsSimpleRendering ? 30 : 0;
                _shadow.Width = this.ActualWidth + add;
                _shadow.HeaderBorder.Width = this.ActualWidth + add;
                _shadow.Height = _shadow.DesiredSize.Height;
                _shadow.HeaderBorder.Height = _shadow.DesiredSize.Height;

                _shadow.RenderTransform = new TranslateTransform(
                    fromInqScene.X - _shadow.Width / 2.0,
                    fromInqScene.Y - _shadow.Height);
                inqScene.AddNoUndo(_shadow);

                FireMoved(_shadow);
            }
        }

        private void SimpleGridViewColumnHeader_TouchDragEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                InqScene inqScene = this.FindParent<InqScene>();
                Point fromInqScene = e.GetTouchPoint(inqScene).Position;

                Vec v = _startDrag - fromInqScene;
                if (v.Length > 10 && _shadow == null)
                {
                    ManipulationStart(fromInqScene);
                }
                ManipulationMove(fromInqScene);
            }
        }

        void SimpleGridViewColumnHeader_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            InqScene inqScene = this.FindParent<InqScene>();
            Point fromInqScene = e.GetPosition(inqScene);

            Vec v = _startDrag - fromInqScene;
            if (v.Length > 10 && _shadow == null)
            {
                ManipulationStart(fromInqScene);
            }
            ManipulationMove(fromInqScene);
        }

        public void ManipulationMove(Point fromInqScene)
        {
            if (_shadow != null)
            {
                InqScene inqScene = this.FindParent<InqScene>();
                _currentFromInqScene = fromInqScene;
                _shadow.RenderTransform = new TranslateTransform(
                    fromInqScene.X - _shadow.Width / 2.0,
                    fromInqScene.Y - _shadow.Height);
                if (inqScene != null)
                {
                    inqScene.AddNoUndo(_shadow);

                    FireMoved(_shadow);
                }
            }
        }

        private void SimpleGridViewColumnHeader_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                InqScene inqScene = this.FindParent<InqScene>();
                Point fromInqScene = e.GetTouchPoint(inqScene).Position;

                if (_shadow == null && 
                    _manipulationStartTime + TimeSpan.FromSeconds(0.5).Ticks > DateTime.Now.Ticks)
                {
                    if (EnableRadialMenu)
                    {
                        DisplayRadialControl(fromInqScene);
                    }
                    else
                    {
                        if (DataContext is CalculatedColumnDescriptor)
                        {
                            CalculatedColumnDescriptorInfo info = (DataContext as CalculatedColumnDescriptor).CalculatedColumnDescriptorInfo;

                            if (info != null)
                            {
                                MathEditor me = new MathEditor(
                                    new SimpleGridViewColumnHeaderMathEditorExecution(inqScene, info), FilterModel,
                                    info);
                                me.SetPosition(fromInqScene.X - RadialControl.SIZE/2,
                                    fromInqScene.Y - RadialControl.SIZE/2);
                                inqScene.AddNoUndo(me);
                            }
                        }
                    }
                }

                ManipulationEnd(fromInqScene);

                _manipulationStartTime = 0;

                this.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDragEvent));
                this.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchUpEvent));   
            }
        }

        void SimpleGridViewColumnHeader_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Mouse.Capture(null);
            InqScene inqScene = this.FindParent<InqScene>();
            Point fromInqScene = e.GetPosition(inqScene);

            if (_shadow == null && 
                _manipulationStartTime + TimeSpan.FromSeconds(0.5).Ticks > DateTime.Now.Ticks &&
                EnableRadialMenu)
            {
                DisplayRadialControl(fromInqScene);
            }

            ManipulationEnd(fromInqScene);

            this.MouseMove -= SimpleGridViewColumnHeader_MouseMove;
            this.MouseUp -= SimpleGridViewColumnHeader_MouseUp;
            _manipulationStartTime = 0;
        }

        public void ManipulationEnd(Point fromInqScene)
        {
            if (_shadow != null)
            {
                InqScene inqScene = this.FindParent<InqScene>();

                FireDropped(_shadow);

                inqScene.Rem(_shadow);
                _shadow = null;
            }
        }

        public void DisplayRadialControl(Point fromInqScene)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            if (inqScene != null)
            {
                RadialControl rc = new RadialControl(setupRadialCommands(DataContext as PanoramicDataColumnDescriptor),
                    new ColumnHeaderRadialControlExecution(FilterModel, TableModel, inqScene));
                rc.SetPosition(fromInqScene.X - RadialControl.SIZE / 2,
                    fromInqScene.Y - RadialControl.SIZE / 2);
                inqScene.AddNoUndo(rc);
            }
        }

        public static readonly DependencyProperty MyDataContextProperty =
        DependencyProperty.Register("MyDataContext",
                                    typeof(Object),
                                    typeof(SimpleGridViewColumnHeader),
                                    new PropertyMetadata(MyDataContextChanged));

        private static void MyDataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            PanoramicDataColumnDescriptor columnDescriptor = e.NewValue as PanoramicDataColumnDescriptor;
            SimpleGridViewColumnHeader header = sender as SimpleGridViewColumnHeader;
            if (columnDescriptor != null)
            {
                string mainLabel = "";
                string subLabel = "";
                columnDescriptor.GetLabels(out mainLabel, out subLabel);

                header.mainTB.Text = mainLabel;
                header.subTB.Text = subLabel;

                if (columnDescriptor.FilterStroqs != null && columnDescriptor.FilterStroqs.Count > 0)
                {
                    header.filterGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    header.filterGrid.Visibility = Visibility.Collapsed;
                }

                header.upArrow.Visibility = Visibility.Collapsed;
                header.downArrow.Visibility = Visibility.Collapsed;
                if (!header.IsSimpleRendering)
                {
                    if (columnDescriptor.SortMode == SortMode.Asc)
                    {
                        header.upArrow.Visibility = Visibility.Visible;
                    }
                    else if (columnDescriptor.SortMode == SortMode.Desc)
                    {
                        header.downArrow.Visibility = Visibility.Visible;
                    }
                }
            }
            SimpleGridViewColumnHeader myControl = (SimpleGridViewColumnHeader)sender;
        }

        RadialMenuCommand setupRadialCommands(PanoramicDataColumnDescriptor column)
        {
            RadialMenuCommand root = new RadialMenuCommand();
            root.Data = column;
            root.IsSelectable = false;

            if (EnableRemoveOptionInRadialMenu)
            {
                RadialMenuCommand remove = new RadialMenuCommand();
                remove.Name = "Remove";
                remove.Data = column;
                remove.IsRemove = true;
                remove.IsSelectable = true;
                root.AddSubCommand(remove);
            }

            if (EnableScaleFunctionInRadialMenu)
            {
                RadialMenuCommandGroup scaleGroup = new RadialMenuCommandGroup("scaleGroup", RadialMenuCommandComandGroupPolicy.DeactivateOthers);
                RadialMenuCommand scale = new RadialMenuCommand();
                scale.Name = "Scale\nFunction";
                scale.Data = column;
                scale.IsActive = column.ScaleFunction != ScaleFunction.None;
                root.AddSubCommand(scale);

                RadialMenuCommand log = new RadialMenuCommand();
                log.Name = "Log";
                log.CommandGroup = scaleGroup;
                log.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                log.Data = column;
                log.IsSelectable = true;
                log.IsActive = column.ScaleFunction == ScaleFunction.Log;
                log.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.ScaleFunction = cmd.IsActive ? ScaleFunction.Log : ScaleFunction.None;
                };
                scale.AddSubCommand(log);

                RadialMenuCommand norm = new RadialMenuCommand();
                norm.Name = "Normalized";
                norm.CommandGroup = scaleGroup;
                norm.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                norm.Data = column;
                norm.IsSelectable = true;
                norm.IsActive = column.ScaleFunction == ScaleFunction.Normalize;
                norm.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.ScaleFunction = cmd.IsActive ? ScaleFunction.Normalize : ScaleFunction.None;
                };
                scale.AddSubCommand(norm);

                RadialMenuCommand rt = new RadialMenuCommand();
                rt.Name = "Running\nTotal";
                rt.CommandGroup = scaleGroup;
                rt.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                rt.Data = column;
                rt.IsSelectable = true;
                rt.IsActive = column.ScaleFunction == ScaleFunction.RunningTotal;
                rt.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.ScaleFunction = cmd.IsActive ? ScaleFunction.RunningTotal : ScaleFunction.None;
                };
                scale.AddSubCommand(rt);

                RadialMenuCommand rtNorm = new RadialMenuCommand();
                rtNorm.Name = "Running Total\nNormalized";
                rtNorm.CommandGroup = scaleGroup;
                rtNorm.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                rtNorm.Data = column;
                rtNorm.IsSelectable = true;
                rtNorm.IsActive = column.ScaleFunction == ScaleFunction.RunningTotalNormalized;
                rtNorm.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.ScaleFunction = cmd.IsActive ? ScaleFunction.RunningTotalNormalized : ScaleFunction.None;
                };
                scale.AddSubCommand(rtNorm);
            }

            RadialMenuCommand sort = new RadialMenuCommand();
            sort.Name = "Sort";
            sort.Data = column;
            sort.IsActive = column.SortMode != SortMode.None;
            sort.IsSelectable = false;
            root.AddSubCommand(sort);

            RadialMenuCommandGroup sortGroup = new RadialMenuCommandGroup("sortGroup", RadialMenuCommandComandGroupPolicy.DeactivateOthers);
            RadialMenuCommand sortAsc = new RadialMenuCommand();
            sortAsc.Name = "Asc";
            sortAsc.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
            sortAsc.CommandGroup = sortGroup;
            sortAsc.Data = column;
            sortAsc.IsActive = column.SortMode == SortMode.Asc;
            sortAsc.IsSelectable = true;
            sortAsc.ActiveTriggered = (cmd) => 
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.SortMode = cmd.IsActive ? SortMode.Asc : SortMode.None;
            };
            sort.AddSubCommand(sortAsc);

            RadialMenuCommand sortDesc = new RadialMenuCommand();
            sortDesc.Name = "Desc";
            sortAsc.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
            sortDesc.CommandGroup = sortGroup;
            sortDesc.Data = column;
            sortDesc.IsActive = column.SortMode == SortMode.Desc;
            sortDesc.IsSelectable = true;
            sortDesc.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.SortMode = cmd.IsActive ? SortMode.Desc : SortMode.None;
            };
            sort.AddSubCommand(sortDesc);

            /*RadialMenuCommand group = new RadialMenuCommand();
            group.Name = "Group";
            group.Data = column;
            group.IsActive = column.IsAnyGroupingOperationApplied();
            group.IsSelectable = false;
            

            RadialMenuCommandGroup groupGroup = new RadialMenuCommandGroup("groupGroup", RadialMenuCommandComandGroupPolicy.DeactivateAndTriggerOthers);
            RadialMenuCommand distinct = new RadialMenuCommand();
            distinct.Name = "Distinct";
            distinct.CommandGroup = groupGroup;
            distinct.Data = column;
            distinct.IsActive = column.IsGrouped;
            distinct.IsSelectable = true;
            distinct.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.IsGrouped = cmd.IsActive ? true : false;
            };
            group.AddSubCommand(distinct);

            RadialMenuCommand tile = new RadialMenuCommand();
            tile.Name = "Tile";
            tile.CommandGroup = groupGroup;
            tile.Data = column;
            tile.IsActive = column.IsTiled;
            tile.AllowsNumericInput = true;
            tile.MinNumericValue = 1;
            tile.MaxNumericValue = 30;
            tile.UpperNumericValue = column.NumberOfTiles;
            tile.IsSelectable = true;
            tile.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.IsTiled = cmd.IsActive ? true : false;
                cd.NumberOfTiles = (int) cmd.UpperNumericValue;
            };
            //group.AddSubCommand(tile); // ez: tmp removed
             

            if (column.DataType == DataTypeConstants.INT ||
                column.DataType == DataTypeConstants.FLOAT)
            {
                RadialMenuCommand bin = new RadialMenuCommand();
                bin.Name = "Bin";
                bin.CommandGroup = groupGroup;
                bin.Data = column;
                bin.IsActive = column.IsBinned;
                bin.IsSelectable = true;
                bin.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.IsBinned = cmd.IsActive ? true : false;
                };
                group.AddSubCommand(bin);

                RadialMenuCommand binSize = new RadialMenuCommand();
                binSize.Name = "Bin Size";
                binSize.Data = column;
                binSize.AllowsNumericInput = true;
                binSize.IsRangeNumericInput = false;
                binSize.MaxNumericValue = column.MaxValue.HasValue ? column.MaxValue.Value : 100;
                binSize.MinNumericValue = 1;
                binSize.UpperNumericValue = column.BinSize;
                binSize.IsSelectable = false;
                binSize.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.BinSize = cmd.UpperNumericValue;
                };
                bin.AddSubCommand(binSize);

                RadialMenuCommand binRange = new RadialMenuCommand();
                binRange.Name = "Bin Range";
                binRange.Data = column;
                binRange.AllowsNumericInput = true;
                binRange.IsRangeNumericInput = true;
                binRange.MaxNumericValue = column.MaxValue.HasValue ? column.MaxValue.Value : 100;
                binRange.MinNumericValue = column.MinValue.HasValue ? column.MinValue.Value : 0;
                binRange.UpperNumericValue = column.BinUpperBound;
                binRange.LowerNumericValue = column.BinLowerBound;
                binRange.IsSelectable = false;
                binRange.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.BinUpperBound = cmd.UpperNumericValue;
                    cd.BinLowerBound = cmd.LowerNumericValue;
                };
                bin.AddSubCommand(binRange);

                root.AddSubCommand(group);
            }
            else
            {
                root.AddSubCommand(distinct);
            }
            */
            RadialMenuCommand vis = new RadialMenuCommand();
            vis.Name = "Vis";
            vis.Data = column;
            vis.IsSelectable = true;
            vis.IsActive = column.IsVisualization;
            vis.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.IsVisualization = cmd.IsActive;
            };
            // ez: tmp removed root.AddSubCommand(vis);

            RadialMenuCommandGroup aggGroup = new RadialMenuCommandGroup("aggGroup", RadialMenuCommandComandGroupPolicy.DeactivateOthers);
            RadialMenuCommand aggregate = new RadialMenuCommand();
            aggregate.Name = "Transform";
            aggregate.Data = column;
            aggregate.IsActive = column.AggregateFunction != AggregateFunction.None;
            root.AddSubCommand(aggregate);

            if (column.DataType == DataTypeConstants.INT ||
                column.DataType == DataTypeConstants.FLOAT)
            {
                RadialMenuCommand sum = new RadialMenuCommand();
                sum.Name = "Sum";
                sum.CommandGroup = aggGroup;
                sum.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                sum.Data = column;
                sum.IsSelectable = true;
                sum.IsActive = column.AggregateFunction == AggregateFunction.Sum;
                sum.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Sum : AggregateFunction.None;
                };
                aggregate.AddSubCommand(sum);

                RadialMenuCommand avg = new RadialMenuCommand();
                avg.Name = "Avg";
                avg.CommandGroup = aggGroup;
                avg.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                avg.Data = column;
                avg.IsSelectable = true;
                avg.IsActive = column.AggregateFunction == AggregateFunction.Avg;
                avg.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Avg : AggregateFunction.None;
                };
                aggregate.AddSubCommand(avg);
            }
            if (FilterModel != null &&
                FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count > 0 &&
                FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count(cd => cd.IsBinned && cd.MatchSimple(column)) > 0)
            {
                RadialMenuCommand binRange = new RadialMenuCommand();
                binRange.Name = "Bin Range";
                binRange.CommandGroup = aggGroup;
                binRange.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                binRange.Data = column;
                binRange.IsSelectable = true;
                binRange.IsActive = column.AggregateFunction == AggregateFunction.Bin;
                binRange.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Bin : AggregateFunction.None;
                };
                aggregate.AddSubCommand(binRange);
            }

            RadialMenuCommand max = new RadialMenuCommand();
            max.Name = "Max";
            max.CommandGroup = aggGroup;
            max.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
            max.Data = column;
            max.IsSelectable = true;
            max.IsActive = column.AggregateFunction == AggregateFunction.Max;
            max.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Max : AggregateFunction.None;
            };
            aggregate.AddSubCommand(max);

            RadialMenuCommand min = new RadialMenuCommand();
            min.Name = "Min";
            min.CommandGroup = aggGroup;
            min.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
            min.Data = column;
            min.IsSelectable = true;
            min.IsActive = column.AggregateFunction == AggregateFunction.Min;
            min.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Min : AggregateFunction.None;
            };
            aggregate.AddSubCommand(min);

            RadialMenuCommand count = new RadialMenuCommand();
            count.Name = "Count";
            count.CommandGroup = aggGroup;
            count.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
            count.Data = column;
            count.IsSelectable = true;
            count.IsActive = column.AggregateFunction == AggregateFunction.Count;
            count.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Count : AggregateFunction.None;
            };
            aggregate.AddSubCommand(count);

            /*RadialMenuCommand concat = new RadialMenuCommand();
            concat.Name = "Concat";
            concat.CommandGroup = aggGroup;
            concat.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
            concat.Data = column;
            concat.IsSelectable = true;
            concat.IsActive = column.AggregateFunction == AggregateFunction.Concat;
            concat.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Concat : AggregateFunction.None;
            };
            aggregate.AddSubCommand(concat);*/

            if (FilterModel != null)
            {
                RadialMenuCommand filter = new RadialMenuCommand();
                filter.Name = "Filter";
                filter.Data = column;
                filter.AllowsStroqInput = true;
                filter.IsSelectable = false;

                RadialMenuCommand actualFilter = new RadialMenuCommand();
                actualFilter.Name = "Dummy Command";
                filter.AddSubCommand(actualFilter);

                root.AddSubCommand(filter);
            }


            return root;
        }
    }

    public interface ColumnHeaderEventHandler 
    {
        void ColumnHeaderMoved(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e, bool overElement);
        void ColumnHeaderDropped(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e);
    }

    public class ColumnHeaderRadialControlExecution : RadialControlExecution
    {
        private FilterModel _filterModel = null;
        private TableModel _tableModel = null;
        private InqScene _inqScene = null;

        public ColumnHeaderRadialControlExecution(FilterModel filterModel, TableModel tableModel, InqScene inqScene)
        {
            this._filterModel = filterModel;
            this._tableModel = tableModel;
            this._inqScene = inqScene;
        }

        public override void Remove(RadialControl sender, RadialMenuCommand cmd)
        {
            base.Remove(sender, cmd);

            if (_tableModel != null)
            {
                _tableModel.RemoveColumnDescriptor(cmd.Data as PanoramicDataColumnDescriptor);
            }
            else if (_filterModel != null)
            {
                _filterModel.RemoveColumnDescriptor(cmd.Data as PanoramicDataColumnDescriptor);
            }
        }

        public override void Dispose(RadialControl sender)
        {
            base.Dispose(sender);

            if (_inqScene != null)
            {
                _inqScene.Rem(sender as FrameworkElement);
            }
        }

        public override void ExecuteCommand(RadialControl sender, RadialMenuCommand cmd, string needle = null, StroqCollection stroqs = null)
        {
            base.ExecuteCommand(sender, cmd, needle, stroqs);

            PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;

            if (needle != null)
            {
                FilteredItem fi = null;

                if (needle != "")
                {
                    if (cd.DataType == DataTypeConstants.NVARCHAR || 
                        cd.DataType == DataTypeConstants.GEOGRAPHY)
                    {
                        fi = new FilteredItem();
                        fi.IsHandwrittenFilter = true;
                        PanoramicDataValue val = new PanoramicDataValue();
                        val.Value = needle;
                        val.DataType = DataTypeConstants.NVARCHAR;
                        fi.ColumnComparisonValues.Add(cd, new PanoramicDataValueComparison(val, Predicate.LIKE));
                    }
                    else if (cd.DataType == DataTypeConstants.FLOAT ||
                                cd.DataType == DataTypeConstants.INT)
                    {

                        double d = 0;
                        fi = new FilteredItem();
                        Predicate pred = Predicate.LIKE;
                        if (needle.StartsWith(">"))
                        {
                            if (double.TryParse(needle.Substring(1, needle.Length - 1).Trim(), out d))
                            {

                                pred = Predicate.GREATER_THAN;
                            }
                            else
                            {
                                fi = null;
                            }
                        }
                        else if (needle.StartsWith("<"))
                        {
                            if (double.TryParse(needle.Substring(1, needle.Length - 1).Trim(), out d))
                            {

                                pred = Predicate.LESS_THAN;
                            }
                            else
                            {
                                fi = null;
                            }
                        }
                        else
                        {
                            if (double.TryParse(needle.Trim(), out d))
                            {
                                pred = Predicate.EQUALS;
                            }
                            else
                            {
                                fi = null;
                            }
                        }
                        if (fi != null)
                        {
                            fi.IsHandwrittenFilter = true;
                            PanoramicDataValue val = new PanoramicDataValue();
                            val.Value = d;
                            val.DataType = DataTypeConstants.FLOAT;
                            fi.ColumnComparisonValues.Add(cd, new PanoramicDataValueComparison(val, pred));
                        }

                    }
                }
                if (_filterModel != null)
                {
                    FilteredItem old = null;
                    foreach (var cFi in _filterModel.EmbeddedFilteredItems)
                    {
                        if (cFi.ColumnComparisonValues.ContainsKey(cd) && cFi.IsHandwrittenFilter)
                        {
                            old = cFi;
                        }
                    }
                    if (old != null)
                    {
                        _filterModel.RemoveEmbeddedFilteredItem(old);
                    }
                    if (fi != null)
                    {
                        cd.FilterStroqs = stroqs;
                        _filterModel.AddEmbeddedFilteredItem(fi);
                    }
                }
            }

            // exectue Action
            if (cmd.ActiveTriggered != null)
            {
                cmd.ActiveTriggered(cmd);
            }

            // check parent policy
            if (cmd.ParentPolicy == RadialMenuCommandParentPolicy.ActivateParentWhenActive)
            {
                if (cmd.CommandGroup == null)
                {
                    cmd.Parent.IsActive = cmd.IsActive;
                }
                else
                {
                    bool anyInGroupActive = false;
                    foreach (var c in cmd.Parent.InnerCommands)
                    {
                        if (c.CommandGroup == cmd.CommandGroup)
                        {
                            anyInGroupActive = anyInGroupActive || c.IsActive;
                        }
                    }
                    cmd.Parent.IsActive = anyInGroupActive;
                    if (cmd.Parent.ActiveTriggered != null)
                    {
                        cmd.Parent.ActiveTriggered(cmd);
                    }
                }
            }

            // check group policy
            if (cmd.CommandGroup != null)
            {
                if (cmd.CommandGroup.GroupPolicy == RadialMenuCommandComandGroupPolicy.MultiActive)
                {
                    // do nothing
                }
                else 
                {
                    if (cmd.IsActive)
                    {
                        foreach (var c in cmd.Parent.InnerCommands)
                        {
                            if (c != cmd && c.CommandGroup == cmd.CommandGroup)
                            {
                                c.IsActive = false;

                                if (cmd.CommandGroup.GroupPolicy == RadialMenuCommandComandGroupPolicy.DeactivateAndTriggerOthers &&
                                    c.ActiveTriggered != null)
                                {
                                    c.ActiveTriggered(c);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class ColumnHeaderEventArgs : EventArgs
    {
        public Rct Bounds { get; set; }
        public PanoramicDataColumnDescriptor ColumnDescriptor { get; set; }
        public TableModel TableModel { get; set; }
        public FilterModel FilterModel { get; set; }
        public FilterModel LinkFromFilterModel { get; set; }
        public bool DefaultSize { get; set; }
        public ColumnHeaderEventArgsCommand Command { get; set; }

        public ColumnHeaderEventArgs() { }
        public ColumnHeaderEventArgs(Rct bounds, PanoramicDataColumnDescriptor cd, TableModel tm, FilterModel fm, bool defaultSize, ColumnHeaderEventArgsCommand cmd = ColumnHeaderEventArgsCommand.None, FilterModel linkFromFilterModel = null)
        {
            this.Bounds = bounds;
            this.ColumnDescriptor = cd;
            this.TableModel = tm;
            this.FilterModel = fm;
            this.DefaultSize = defaultSize;
            this.LinkFromFilterModel = linkFromFilterModel;
            this.Command = cmd;
        }
    }

    public enum ColumnHeaderEventArgsCommand
    {
        None,
        Copy,
        Snapshot
    }

    public class DataContextChangeEventHelper
    {
        public DataContextChangeEventHelper(FrameworkElement frameworkElement)
        {
            FrameworkElement = frameworkElement;
            DataContext = FrameworkElement.DataContext;
        }

        public FrameworkElement FrameworkElement { get; private set; }

        private Object dataContext;

        public Object DataContext
        {
            get
            {
                return dataContext;
            }
            set
            {
                dataContext = value;
                if (DataContextChanged != null)
                {
                    DataContextChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler DataContextChanged;

        public void Bind()
        {
            var binding = new Binding("DataContext")
            {
                Mode = BindingMode.TwoWay,
                Source = this
            };
            FrameworkElement.SetBinding(TextBlock.DataContextProperty, binding);
        }

    }

    public class SimpleGridViewColumnHeaderMathEditorExecution : MathEditorExecution
    {
        private InqScene _inqScene = null;
        private CalculatedColumnDescriptorInfo _calculatedColumnDescriptorInfo = null;

        public SimpleGridViewColumnHeaderMathEditorExecution(InqScene inqScene, CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo)
        {
            this._inqScene = inqScene;
            this._calculatedColumnDescriptorInfo = calculatedColumnDescriptorInfo;
        }

        public override void Dispose(MathEditor sender)
        {
            base.Dispose(sender);
            _calculatedColumnDescriptorInfo.TableModel.UpdateCalculatedColumnDescriptorInfo(_calculatedColumnDescriptorInfo);

            if (_inqScene != null)
            {
                _inqScene.Rem(sender as FrameworkElement);
            }
        }
    }
}
