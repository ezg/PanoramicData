using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DiagramDesigner;
using PanoramicDataModel;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using PixelLab.Common;
using starPadSDK.Inq;
using starPadSDK.WPFHelp;
using PanoramicData.model.view;
using PanoramicData.view.schema;
using PanoramicData.view.math;
using PanoramicData.view.other;
using PanoramicData.view.table;
using CombinedInputAPI;
using PanoramicData.model.view_new;
using PanoramicData.view.inq;

namespace PanoramicData.view.filter
{
    public partial class Resizer : UserControl
    {
        private IDisposable _filterModelDisposable = null;
        private IDisposable _tableModelDisposable = null;

        private Grid _headerGrid = null;
        private Border _headerBorder = null;
        private Polygon _resizeGrid = null;
        private Grid _contentGrid = null;
        private Grid _dotGrid = null;
        private Grid _optionGrid = null;
        private TextBlock _lblRowCount = null;

        private Grouper _grouper = null;
        private Colorer _colorer = null;
        private Styler _styler = null;
        
        private RowDefinition _headerRowDefinition = null;

        private Grid _frontGrid = null;
        private Canvas _backCanvas = null;

        private Grid _frozen = null;

        private Point _startDrag1 = new Point();
        private Point _current1 = new Point();
        private TouchDevice _dragDevice1 = null;

        private Point _startDrag2 = new Point();
        private Point _current2 = new Point();
        private TouchDevice _dragDevice2 = null;
        private Border _copyShadow = null;

        private bool _isFront = true;
       
        private bool _isRecommendationPanelOut = false;
        private bool _isRecommendationPanelAnimationRunning = false;
        private Storyboard _recommendationPanelStoryBoard = null;

        private bool _isTreeViewPanelOut = false;
        private bool _isTreeViewPanelAnimationRunning = false;
        private Storyboard _treeViewPanelStoryBoard = null;

        private Stopwatch _headerGridStopwatch = new Stopwatch();
        private Point _headerGridStartPoint = new Point();

        public static readonly DependencyProperty FilterModelProperty = DependencyProperty.Register("FilterModel",
            typeof (FilterModel), typeof (Resizer), new PropertyMetadata(OnFilterModelChanged));

        public FilterModel FilterModel
        {
            get
            {
                return (FilterModel) GetValue(FilterModelProperty);
            }
            set
            {
                SetValue(FilterModelProperty, value);
            }
        }

        private static void OnFilterModelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as Resizer).OnFilterModelChanged(args);
        }

        protected virtual void OnFilterModelChanged(DependencyPropertyChangedEventArgs args)
        {
            if (_filterModelDisposable != null)
            {
                _filterModelDisposable.Dispose();
            }
            if (_tableModelDisposable != null)
            {
                _tableModelDisposable.Dispose();
            }
            if (args.NewValue != null)
            {
                _filterModelDisposable = Observable.FromEventPattern<FilterModelUpdatedEventArgs>(
                    (FilterModel) args.NewValue, "FilterModelUpdated")
                    .Where(
                        arg =>
                            arg.EventArgs != null &&
                            (arg.EventArgs.Mode != UpdatedMode.UI || (arg.EventArgs.Mode == UpdatedMode.UI && (arg.EventArgs.SubMode == SubUpdatedMode.Color || arg.EventArgs.SubMode == SubUpdatedMode.RenderStyle))) &&
                            arg.EventArgs.Mode != UpdatedMode.FilteredItemsStatus)
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            if (arg.EventArgs.Mode == UpdatedMode.Structure ||
                                arg.EventArgs.Mode == UpdatedMode.Incoming ||
                                (arg.EventArgs.Mode == UpdatedMode.UI && (arg.EventArgs.SubMode == SubUpdatedMode.Color || arg.EventArgs.SubMode == SubUpdatedMode.RenderStyle)) ||
                                (arg.EventArgs.Mode == UpdatedMode.FilteredItemsChange &&
                                 (arg.EventArgs.Sender != this || arg.EventArgs.Sender == null)))
                            {
                                init();
                            }
                        }));
                    });

                init();
            }

        }

        public static readonly DependencyProperty WidthAnimationProperty = DependencyProperty.Register(
            "WidthAnimation", typeof (double),
            typeof (Resizer), new PropertyMetadata(OnWidthAnimationChanged));

        public double WidthAnimation
        {
            get
            {
                return (double) GetValue(WidthAnimationProperty);
            }
            set
            {
                SetValue(WidthAnimationProperty, value);
            }
        }

        private static void OnWidthAnimationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((Resizer) obj).OnWidthAnimationChanged(args);
        }

        private void OnWidthAnimationChanged(DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                MovableElement movableParent = this.FindParent<MovableElement>();
                movableParent.NotifyScale(new Vec((double) args.NewValue/movableParent.GetSize().X, 1.0), new Vec(0, 0));
            }
        }

        public Resizer(bool isFront, bool showToggle = true)
        {
            _isFront = isFront;
            _showToggle = showToggle;
            InitializeComponent();

            _frozen = new Grid();
            _frozen.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            _frozen.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            _frozen.Background = new SolidColorBrush(Colors.White);
        }

        private bool _showToggle = true;
        public bool ShowToggle
        {
            get
            {
                return _showToggle;
            }
            set
            {
                if (_backCanvas != null && _frontGrid != null)
                {
                    if (value)
                    {
                        _dotGrid.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        _dotGrid.Visibility = Visibility.Collapsed;
                    }
                }
                _showToggle = value;
            }
        }

        private bool _createBitmapForInteractions = true;
        public bool CreateBitmapForInteractions
        {
            get
            {
                return _createBitmapForInteractions;
            }
            set
            {
                _createBitmapForInteractions = value;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _headerGrid = (Grid) GetTemplateChild("headerGrid");
            _headerBorder = (Border)GetTemplateChild("headerBorder");
            _resizeGrid = (Polygon) GetTemplateChild("resizeGrid");
            _contentGrid = (Grid) GetTemplateChild("contentGrid");
            _dotGrid = (Grid) GetTemplateChild("dotGrid");
            _optionGrid = (Grid)GetTemplateChild("optionGrid");
            _frontGrid = (Grid) GetTemplateChild("frontGrid");
            _backCanvas = (Canvas) GetTemplateChild("backCanvas");
            _headerRowDefinition = (RowDefinition) GetTemplateChild("headerRowDefinition");
            _grouper = (Grouper)GetTemplateChild("grouper");
            _colorer = (Colorer)GetTemplateChild("colorer");
            _styler = (Styler)GetTemplateChild("styler");
            _lblRowCount = (TextBlock)GetTemplateChild("lblRowCount");
 
            if (!_isFront || !_showToggle)
            {
                ((Grid)GetTemplateChild("frontGrid")).Visibility = Visibility.Hidden;
            }
            if (_isFront || !_showToggle)
            {
                ((Canvas) GetTemplateChild("backCanvas")).Visibility = Visibility.Hidden;
            }

            if (_showToggle)
            {
                _dotGrid.Visibility = Visibility.Visible;
            }
            else
            {
                _dotGrid.Visibility = Visibility.Collapsed;
            }

            _headerGrid.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchDownEvent));
            _resizeGrid.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchDownEvent));
            _dotGrid.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(dotGrid_TouchDownEvent));
            _optionGrid.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(optionGrid_TouchDownEvent));

            if (Properties.Settings.Default.PanoramicDataEnableStable)
            {
                double s = Properties.Settings.Default.PanoramicDataStableScaleFactor;

                _dotGrid.Visibility = Visibility.Collapsed;
                _resizeGrid.RenderTransformOrigin = new Point(1,1);
                _resizeGrid.RenderTransform = new ScaleTransform(s, s);

                _optionGrid.RenderTransformOrigin = new Point(0, 0.5);
                _optionGrid.RenderTransform = new ScaleTransform(s, s);

                _headerRowDefinition.Height = new GridLength(28 * s);

                ((Label)GetTemplateChild("lblLabel")).FontSize = Properties.Settings.Default.PanoramicDataStableLabelFontSize;
            }

            init();
        }

        void plusCanvas_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

            CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo = new CalculatedColumnDescriptorInfo();
            calculatedColumnDescriptorInfo.Name = "Calculated Field " + FilterModel.TableModel.CalculatedColumnDescriptorInfos.Count;
            calculatedColumnDescriptorInfo.TableModel = FilterModel.TableModel;

            MathEditor me = new MathEditor(
                new ResizerMathEditorExecution(inkableScene, calculatedColumnDescriptorInfo), FilterModel, calculatedColumnDescriptorInfo);
            me.SetPosition(fromInkableScene.X - RadialControl.SIZE / 2,
                fromInkableScene.Y - RadialControl.SIZE / 2);
            inkableScene.Add(me);

            e.Handled = true;
        }

        private void init()
        {
            return;
            if (_grouper != null)
            {
                _grouper.FilterModel = FilterModel;
                _grouper.Init();
            }
            if (_colorer != null)
            {
                _colorer.FilterModel = FilterModel;
                _colorer.Init();
            }
            if (_styler != null)
            {
                if (FilterModel.FilterRendererType == FilterRendererType.Table)
                {
                    _styler.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _styler.FilterModel = FilterModel;
                    _styler.Init();
                }
            }

            if (_headerRowDefinition != null && (FilterModel as FilterHolderViewModel).NoChrome)
            {
                _headerRowDefinition.Height = new GridLength(0);
                _resizeGrid.Visibility = Visibility.Collapsed;
                //_headerGrid.BorderThickness = new Thickness(0);
            }
            if (_headerGrid != null &&
                (FilterModel.FilterRendererType == FilterRendererType.Pivot || FilterModel.FilterRendererType == FilterRendererType.Frozen))
            {
                _headerBorder.CornerRadius = new CornerRadius(20, 20, 0, 0);
                _optionGrid.Visibility = Visibility.Collapsed;
                _dotGrid.Visibility = Visibility.Collapsed;
            }

            if (_grouper != null && _styler != null && _colorer != null)
            {
                _grouper.Visibility = Visibility.Visible;
                _colorer.Visibility = Visibility.Visible;
                _styler.Visibility = Visibility.Visible;
                _lblRowCount.Visibility = Visibility.Collapsed;

                if (FilterModel.FilterRendererType == FilterRendererType.Table)
                {
                    _lblRowCount.Visibility = Visibility.Visible;
                }

                if (FilterModel.FilterRendererType == FilterRendererType.Pivot ||
                    FilterModel.FilterRendererType == FilterRendererType.Frozen ||
                    FilterModel.FilterRendererType == FilterRendererType.Map ||
                    FilterModel.FilterRendererType == FilterRendererType.Table ||
                    FilterModel.FilterRendererType == FilterRendererType.Slider)
                {
                    _styler.Visibility = Visibility.Collapsed;
                }
                if (FilterModel.FilterRendererType == FilterRendererType.Pivot ||
                    FilterModel.FilterRendererType == FilterRendererType.Frozen ||
                    FilterModel.FilterRendererType == FilterRendererType.Map ||
                    FilterModel.FilterRendererType == FilterRendererType.Slider ||
                    FilterModel.FilterRendererType == FilterRendererType.Pie)
                {
                    _grouper.Visibility = Visibility.Collapsed;
                }
                if (FilterModel.FilterRendererType == FilterRendererType.Pivot ||
                    FilterModel.FilterRendererType == FilterRendererType.Frozen ||
                    FilterModel.FilterRendererType == FilterRendererType.Map ||
                    FilterModel.FilterRendererType == FilterRendererType.Slider)
                {
                    _colorer.Visibility = Visibility.Collapsed;
                }

                if (!_isFront)
                {
                    _grouper.Visibility = Visibility.Collapsed;
                    _colorer.Visibility = Visibility.Collapsed;
                    _styler.Visibility = Visibility.Collapsed;
                    _lblRowCount.Visibility = Visibility.Collapsed;
                }
            }
            if (_contentGrid != null)
            {
                if (FilterModel.FilterRendererType == FilterRendererType.Table)
                {
                    _contentGrid.Margin= new Thickness(0, 0, 0, 30);
                }
                else
                {
                    _contentGrid.Margin = new Thickness(0, 0, 0, 0);
                }
            }
            if (_lblRowCount != null)
            {
                _lblRowCount.DataContext = FilterModel;
                Binding binding = new Binding
                {
                    Path = new PropertyPath("RowCount"),
                    StringFormat = "{0} " + (FilterModel.FilterRendererType == FilterRendererType.Table ? " Rows" : " Datapoints")
                };
                _lblRowCount.SetBinding(TextBlock.TextProperty, binding);
            }
        }

        void dotGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            e.Handled = true;
            _dotGrid.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(dotGrid_PointDrag));
            _dotGrid.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(dotGrid_PointUp));
        }

        void dotGrid_PointUp(Object sender, TouchEventArgs e)
        {
            e.Handled = true;
            _dotGrid.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(dotGrid_PointDrag));
            _dotGrid.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(dotGrid_PointUp));

            MovableElement movableParent = this.FindParent<MovableElement>();
            movableParent.FlipSides();
        }

        void dotGrid_PointDrag(Object sender, TouchEventArgs e)
        {
            e.Handled = true;
        }

        void optionGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            e.Handled = true;
            _optionGrid.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(optionGrid_PointDrag));
            _optionGrid.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(optionGrid_PointUp));
        }

        void optionGrid_PointUp(Object sender, TouchEventArgs e)
        {
            e.Handled = true;
            _optionGrid.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(optionGrid_PointDrag));
            _optionGrid.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(optionGrid_PointUp));

            InkableScene inkableScene = this.FindParent<InkableScene>();
            Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

            RadialMenuCommand root = new RadialMenuCommand();
            root.IsSelectable = false;

            RadialMenuCommand remove = new RadialMenuCommand();
            remove.Name = "Remove";
            remove.Data = FilterModel;
            remove.IsRemove = true;
            remove.IsSelectable = true;
            root.AddSubCommand(remove);

            RadialMenuCommand styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Change\nColor";
            styleCmd.IsSelectable = true;
            styleCmd.IsActivatable = false;
            styleCmd.Data = FilterModel;
            styleCmd.ActiveTriggered = (cmd) =>
            {
                FilterModel.SwitchToNewColor();
            };
            root.AddSubCommand(styleCmd);

            styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Create\nSnaphot";
            styleCmd.Data = FilterModel;
            styleCmd.AllowsDragging = true;
            styleCmd.IsSelectable = true;
            styleCmd.IsActivatable = false;
            styleCmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Line;
            root.AddSubCommand(styleCmd);

            styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Create\nCopy";
            styleCmd.Data = FilterModel;
            styleCmd.AllowsDragging = true;
            styleCmd.IsSelectable = true;
            styleCmd.IsActivatable = false;
            styleCmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Line;
            root.AddSubCommand(styleCmd);

            if (root.InnerCommands.Count > 0)
            {
                MovableElement movableElement = this.FindParent<MovableElement>();

                RadialControl rc = new RadialControl(root,
                    new ResizerRadialControlExecution(FilterModel, FilterModel.TableModel, movableElement, this, inkableScene));
                rc.SetPosition(fromInkableScene.X - RadialControl.SIZE / 2,
                    fromInkableScene.Y - RadialControl.SIZE / 2);
                inkableScene.Add(rc);
            }
            e.Handled = true;
        }

        void optionGrid_PointDrag(Object sender, TouchEventArgs e)
        {
            e.Handled = true;
        }

        bool fix = false;
        void headerGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();

            if (_dragDevice1 == null && _dragDevice2 == null)
            {
                _headerGridStopwatch.Restart();
                e.Handled = true;
                
                MovableElement movableParent = this.FindParent<MovableElement>();

                e.TouchDevice.Capture(_headerGrid);
                _startDrag1 = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement) inkableScene);
                _headerGridStartPoint = _startDrag1;
                _current1 = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);

                _headerGrid.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchMoveEvent));
                if (Mouse.RightButton != MouseButtonState.Pressed && !fix)
                {
                    _headerGrid.AddHandler(FrameworkElement.TouchUpEvent,
                        new EventHandler<TouchEventArgs>(headerGrid_TouchUpEvent));
                    fix = false;
                }
                else
                {
                    fix = true;
                    e.TouchDevice.Capture(null);
                }

                _dragDevice1 = e.TouchDevice;

                // notify content if needed
                this.preTransformation();

                movableParent.PreTransformation();
                movableParent.NotifyInteraction();
                movableParent.NotifyDragStart(_current1);
            }
            else if (_dragDevice2 == null)
            {
                e.Handled = true;
                _dragDevice2 = e.TouchDevice;
                e.TouchDevice.Capture(_headerGrid);
            }
        }

        void headerGrid_TouchUpEvent(Object sender, TouchEventArgs e)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            MovableElement movableParent = this.FindParent<MovableElement>();

            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;

                Pt pos = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);

                // notify content if needed
                this.PostTransformation();

                movableParent.PostTransformation();
                movableParent.NotifyInteraction();
                movableParent.NotifyDragEnd(pos);


                Point curDrag = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);
            }
            else if (e.TouchDevice == _dragDevice2)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice2 = null;

                if (_copyShadow != null)
                {
                    Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;
                    inkableScene.Add(_copyShadow);
                    _copyShadow = null;

                    RadialMenuCommand copy = new RadialMenuCommand();
                    copy.Name = "Create\nCopy";
                    copy.Data = FilterModel;
                    copy.AllowsDragging = true;
                    copy.IsSelectable = true;
                    copy.IsActivatable = false;
                    ResizerRadialControlExecution exec = new ResizerRadialControlExecution(FilterModel, FilterModel.TableModel, movableParent, this, inkableScene);
                    exec.Drop(null, copy, fromInkableScene);
                }
            }

            if (_dragDevice1 == null && _dragDevice2 == null)
            {
                _headerGrid.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchMoveEvent));
                _headerGrid.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchUpEvent));
            }
        }

        void headerGrid_TouchMoveEvent(Object sender, TouchEventArgs e)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            if (e.TouchDevice == _dragDevice1)
            {
                MovableElement movableParent = this.FindParent<MovableElement>();
                Point curDrag = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);

                Vector vec = curDrag - _startDrag1;
                Point dragDelta = new Point(vec.X, vec.Y);

                _startDrag1 = curDrag;
                _current1 = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);
                e.Handled = true;

                // notify content if needed

                movableParent.NotifyMove(dragDelta);
                movableParent.NotifyInteraction();
                movableParent.NotifyDragMove(_current1);
            }
            else if (e.TouchDevice == _dragDevice2)
            {
                e.Handled = true;
                if (inkableScene != null && _copyShadow == null)
                {
                    _startDrag2 = e.GetTouchPoint(inkableScene).Position;
                    _copyShadow = new Border();
                    _copyShadow.Width = 120;
                    _copyShadow.Height = 40;
                    _copyShadow.Background = new SolidColorBrush(Color.FromArgb(70, 125, 125, 125));
                    _copyShadow.BorderBrush = Brushes.Black;
                    Label l = new Label();
                    l.HorizontalAlignment = HorizontalAlignment.Center;
                    l.VerticalAlignment = VerticalAlignment.Center;
                    l.FontWeight = FontWeights.Bold;
                    l.Content = "Create Copy";
                    _copyShadow.Child = l;
                    inkableScene.Add(_copyShadow);
                }

                if (_copyShadow != null)
                {
                    _startDrag2 = e.GetTouchPoint(inkableScene).Position;
                    _copyShadow.RenderTransform = new TranslateTransform(
                        _startDrag2.X - _copyShadow.Width / 2.0,
                        _startDrag2.Y - _copyShadow.Height);
                }
            }
        }

        void resizeGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            //if (e.DeviceType != InputFramework.DeviceType.MultiTouch)
                //return;

            if (_dragDevice1 == null)
            {
                e.Handled = true;
                InkableScene inkableScene = this.FindParent<InkableScene>();
                MovableElement movableParent = this.FindParent<MovableElement>();

                e.TouchDevice.Capture(_resizeGrid);
                _startDrag1 = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);
                _current1 = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);

                _resizeGrid.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchMoveEvent));
                _resizeGrid.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchUpEvent));
                _dragDevice1 = e.TouchDevice;

                // notify content if needed
                this.preTransformation();

                movableParent.PreTransformation();
                movableParent.NotifyInteraction();
            }
        }

        void resizeGrid_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                _resizeGrid.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchMoveEvent));
                _resizeGrid.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchUpEvent));

                MovableElement movableParent = this.FindParent<MovableElement>();

                // notify content if needed
                this.PostTransformation();

                movableParent.PostTransformation();
                movableParent.NotifyInteraction();
            }

        }

        void resizeGrid_TouchMoveEvent(object sender, TouchEventArgs e)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            MovableElement movableParent = this.FindParent<MovableElement>();
            Point curDrag = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);

            if (e.TouchDevice == _dragDevice1)
            {
                Vec currentSize = movableParent.GetSize();
                Vec currentMinSize = movableParent.GetMinSize(); 
                Vec vec = curDrag - _startDrag1;
                Point dragDelta = new Point(vec.X, vec.Y);

                double deltaVertical = Math.Min(dragDelta.Y, currentSize.Y - currentMinSize.Y);
                double deltaHorizontal = Math.Min(dragDelta.X, currentSize.X - currentMinSize.X);

                _startDrag1 = curDrag;
                _current1 = movableParent.TranslatePoint(e.GetTouchPoint(movableParent).Position, (FrameworkElement)inkableScene);
                e.Handled = true;

                Vec newSize = new Vec(currentSize.X + dragDelta.X, currentSize.Y + dragDelta.Y);

                // notify content if needed
                Vec delta = new Vec(
                    Math.Max(newSize.X / currentSize.X, currentMinSize.X / currentSize.X),
                    Math.Max(newSize.Y / currentSize.Y, currentMinSize.Y / currentSize.Y));

                movableParent.NotifyScale(delta, new Vec(0, 0));
                movableParent.NotifyInteraction();
            }
        }

        public Image CreateImage()
        {
            Image img = FrostyFreeze.CreateImageFromControl(_contentGrid);
            img.Width = img.Height = double.NaN;
            //img.Stretch = Stretch.None;
            _frozen.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            _frozen.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            return img;
        }

        private void preTransformation()
        {
            if (CreateBitmapForInteractions)
            {
                Image img = this.CreateImage();

                /*RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)img.Source.Width,
                                                                           (int)img.Source.Height,
                                                                           100, 100, PixelFormats.Default);
            renderTargetBitmap.Render(img);
            JpegBitmapEncoder jpegBitmapEncoder = new JpegBitmapEncoder();
            jpegBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            using (FileStream fileStream = new FileStream(@"C:\temp\1.jpg", FileMode.Create))
            {
                jpegBitmapEncoder.Save(fileStream);
                fileStream.Flush();
                fileStream.Close();
            }*/

                _frozen.Children.Add(img);
                Grid.SetRow(_frozen, 1);
                Grid.SetColumn(_frozen, 1);

                foreach (var c in _contentGrid.Children)
                {
                    (c as FrameworkElement).Visibility = System.Windows.Visibility.Collapsed;
                }

                _frozen.Visibility = System.Windows.Visibility.Visible;
                if (!_contentGrid.Children.Contains(_frozen))
                {
                    _contentGrid.Children.Add(_frozen);
                }
            }
        }

        private void PostTransformation()
        {
            if (CreateBitmapForInteractions)
            {
                if (_frozen != null)
                {
                    _contentGrid.Children.Remove(_frozen);
                    _frozen.Children.Clear();

                    foreach (var c in _contentGrid.Children)
                    {
                        (c as FrameworkElement).Visibility = System.Windows.Visibility.Visible;
                    }
                }
                UpdateLayout();
            }
        }
    }

    public class DatabaseTableEventArgs : EventArgs
    {
        public Rct Bounds { get; set; }
        public TableModel TableModel { get; set; }
        public FilterModel FilterModel { get; set; }
        public bool DefaultSize { get; set; }

        public DatabaseTableEventArgs()
        {
        }

        public DatabaseTableEventArgs(Rct bounds, TableModel tm, FilterModel fm, bool defaultSize)
        {
            this.Bounds = bounds;
            this.TableModel = tm;
            this.FilterModel = fm;
            this.DefaultSize = defaultSize;
        }
    }

    public class ResizerRadialControlExecution : RadialControlExecution
    {
        private FilterModel _filterModel = null;
        private TableModel _tableModel = null;
        private MovableElement _movableElement = null;
        private Resizer _resizer = null;
        private InkableScene _inkableScene = null;

        public static event EventHandler<AttributeViewModelEventArgs> Dropped;

        public ResizerRadialControlExecution(FilterModel filterModel, TableModel tableModel, MovableElement movableElement, Resizer resizer, InkableScene inkableScene)
        {
            this._filterModel = filterModel;
            this._tableModel = tableModel;
            this._movableElement = movableElement;
            this._resizer = resizer;
            this._inkableScene = inkableScene;
        }

        public override void Remove(RadialControl sender, RadialMenuCommand cmd)
        {
            base.Remove(sender, cmd);

            if (_filterModel != null)
            {
                //foreach (var elem in _inkableScene.Elements.ToArray())
                {
                    //if (elem is VisualizationContainerView)
                    {
                        //if ((elem as VisualizationContainerView).FilterHolderViewModel == _filterModel)
                        {
                            //_inkableScene.Rem(elem);
                            //break;
                        }
                    }  
                }
            }
            else if (_filterModel != null)
            {
                _filterModel.RemoveColumnDescriptor(cmd.Data as PanoramicDataColumnDescriptor);
            }
        }

        public override void Drop(RadialControl sender, RadialMenuCommand cmd, Point fromInkableScene)
        {
            base.Dispose(sender);

            if (_inkableScene != null && sender != null)
            {
                //_inkableScene.Rem(sender as FrameworkElement);
            }

            if (Dropped != null)
            {
                Vec size = _movableElement.GetSize();
                Rct bounds = new Rct(new Pt(fromInkableScene.X - (size.X / 2.0), fromInkableScene.Y - (size.Y / 2.0)), _movableElement.GetSize());
                if (cmd.Name == "Create\nSnaphot")
                {
                    FilterHolderViewModel frozenModel = new FilterHolderViewModel();
                    frozenModel.TableModel = _filterModel.TableModel;
                    frozenModel.Label = "Snapshot";
                    frozenModel.Color = _filterModel.Color;
                    frozenModel.FilterRendererType = FilterRendererType.Frozen;
                    frozenModel.FrozenImage = _resizer.CreateImage();
                    /*Dropped(this,
                        new AttributeViewModelEventArgs(bounds, null, _tableModel,
                            frozenModel, false, AttributeViewModelEventArgType.Snapshot));*/
                }
                else if (cmd.Name == "Create\nCopy")
                {
                    /*Dropped(this,
                        new AttributeViewModelEventArgs(bounds, null, _tableModel,
                            _filterModel, false, AttributeViewModelEventArgType.Copy));*/
                }
            }
        }

        public override void Dispose(RadialControl sender)
        {
            base.Dispose(sender);

            if (_inkableScene != null)
            {
                //_inkableScene.Rem(sender as FrameworkElement);
            }
        }

        public override void ExecuteCommand(
            RadialControl sender, RadialMenuCommand cmd,
            string needle = null, StroqCollection stroqs = null)
        {
            // exectue Action
            if (cmd.ActiveTriggered != null)
            {
                cmd.ActiveTriggered(cmd);
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

    public class ResizerMathEditorExecution : MathEditorExecution
    {
        private InkableScene _inkableScene = null;
        private CalculatedColumnDescriptorInfo _calculatedColumnDescriptorInfo = null;

        public ResizerMathEditorExecution(InkableScene inkableScene, CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo)
        {
            this._inkableScene = inkableScene;
            this._calculatedColumnDescriptorInfo = calculatedColumnDescriptorInfo;
        }

        public override void Dispose(MathEditor sender)
        {
            base.Dispose(sender);
            if (_calculatedColumnDescriptorInfo.Stroqs.Count > 0)
            {
                _calculatedColumnDescriptorInfo.TableModel.UpdateCalculatedColumnDescriptorInfo(
                    _calculatedColumnDescriptorInfo);
            }
            if (_inkableScene != null)
            {
                //_inkableScene.Rem(sender as FrameworkElement);
            }
        }
    }
}
