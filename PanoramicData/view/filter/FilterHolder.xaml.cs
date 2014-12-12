using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PanoramicDataModel;
using PixelLab.Common;
using starPadSDK.AppLib;
using System.Reactive.Linq;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using PanoramicData.model.view;
using PanoramicData.view.vis;
using PanoramicData.view.table;
using PanoramicData.controller.view;

namespace PanoramicData.view.filter
{
    public partial class FilterHolder : MovableElement
    {
        public static int WIDTH = 300;
        public static int HEIGHT = 200;

        private Resizer _front = new Resizer(true);
        private Resizer _back = new Resizer(false);
        private bool _isFrontShown = true;
        private IDisposable _filterModelDisposable = null;

        private FilterModelAttachment _filterAttachment = null;
        private FilterModelAttachment _brushAttachment = null;

        public static readonly DependencyProperty FilterHolderViewModelProperty = DependencyProperty.Register("FilterHolderViewModel", typeof(FilterHolderViewModel), typeof(FilterHolder), new PropertyMetadata(OnFilterHolderViewModelChanged));

        public FilterHolderViewModel FilterHolderViewModel
        {
            get
            {
                return (FilterHolderViewModel)GetValue(FilterHolderViewModelProperty);
            }
            set
            {
                SetValue(FilterHolderViewModelProperty, value);
            }
        }

        static void OnFilterHolderViewModelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((FilterHolder) obj).OnFilterHolderViewModelChanged(args);
            ((FilterHolder)obj).DataContext = args.NewValue;
        }

        private void OnFilterHolderViewModelChanged(DependencyPropertyChangedEventArgs args)
        {
            if (_filterModelDisposable != null)
            {
                _filterModelDisposable.Dispose();
            }
            _filterModelDisposable = Observable.FromEventPattern<FilterModelUpdatedEventArgs>((FilterModel)args.NewValue, "FilterModelUpdated").
                Where(arg2 => arg2.EventArgs.Mode != UpdatedMode.UI && arg2.EventArgs.Mode != UpdatedMode.FilteredItemsChange)
                    .Throttle(TimeSpan.FromMilliseconds(50))
                          .Subscribe((arg) =>
                          {
                              Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                              {
                                init(arg.EventArgs);
                              }));
                          });

            init(null);
        }

        public FilterHolder()
        {
            InitializeComponent();
            this.Type = MovableElementType.Rect;
            this.HasEnclosedAnchor = false;

            _filterAttachment = new FilterModelAttachment(FilteringType.Filter);
            MainViewController.Instance.InkableScene.Add(_filterAttachment);

            _brushAttachment = new FilterModelAttachment(FilteringType.Brush);
            MainViewController.Instance.InkableScene.Add(_brushAttachment);
        }

        ~FilterHolder()
        {
            
        }

        void changeIncomingFilterModel()
        {
            if (_filterAttachment != null)
            {
                _filterAttachment.Destination = FilterHolderViewModel;
                _filterAttachment.Sources.Clear();
                foreach (var m in FilterHolderViewModel.GetIncomingFilterModels(FilteringType.Filter))
                {
                    _filterAttachment.Sources.Add(m as FilterHolderViewModel);
                }
            }
            if (_brushAttachment != null)
            {
                _brushAttachment.Sources.Clear();
                _brushAttachment.Destination = FilterHolderViewModel;
                foreach (var m in FilterHolderViewModel.GetIncomingFilterModels(FilteringType.Brush))
                {
                    _brushAttachment.Sources.Add(m as FilterHolderViewModel);
                }
            }
        }

        private void init(FilterModelUpdatedEventArgs e)
        {
            if (e != null)
            {
                if (e.Mode == UpdatedMode.Incoming)
                {
                    changeIncomingFilterModel();
                }
                else if (e.Mode == UpdatedMode.UI)
                {
                    //foreach (var edge in _filterHolderInEdges.Values)
                    //{
                    //    edge.update();
                    //}
                }
            }
            else
            {
                rotationTransition.Duration = new Duration(TimeSpan.FromSeconds(0.05));
                _front.Content = new Front();
                _front.FilterModel = FilterHolderViewModel;
                transitionPresenter.Content = _front;

                if (FilterHolderViewModel.FilterRendererType != FilterRendererType.Table ||
                    FilterHolderViewModel.FilterRendererType != FilterRendererType.Pivot)
                {
                    _back.Content = new Back();
                    _back.FilterModel = FilterHolderViewModel;
                    (_back.Content as Back).FilterModel = FilterHolderViewModel;
                }

                rotationTransition.Duration = new Duration(TimeSpan.FromSeconds(0.75));

                if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Pie)
                {
                    PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                    fRenderer.FilterModel = FilterHolderViewModel;
                    (_front.Content as Front).SetContent(fRenderer);
                    _front.ShowToggle = FilterHolderViewModel.ShowSettings;
                }
                else if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Histogram)
                {
                    PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                    fRenderer.FilterModel = FilterHolderViewModel;
                    (_front.Content as Front).SetContent(fRenderer);
                    _front.ShowToggle = FilterHolderViewModel.ShowSettings;
                }
                else if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Table)
                {
                    FilterRenderer fRenderer = new TableFilterRenderer();
                    fRenderer.FilterModel = FilterHolderViewModel;
                    (_front.Content as Front).SetContent(fRenderer);
                    _front.ShowToggle = FilterHolderViewModel.ShowSettings;
                    _front.CreateBitmapForInteractions = true;
                }
                else if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Pivot)
                {
                    FilterRenderer fRenderer = new PivotFilterRenderer();
                    fRenderer.FilterModel = FilterHolderViewModel;
                    (_front.Content as Front).SetContent(fRenderer);
                    _front.ShowToggle = false;
                    _front.CreateBitmapForInteractions = false;
                    MainViewController.Instance.InkableScene.Remove(_brushAttachment);
                }
                else if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Slider)
                {
                    SliderFilterRenderer fRenderer = new SliderFilterRenderer();
                    fRenderer.FilterModel = FilterHolderViewModel;
                    (_front.Content as Front).SetContent(fRenderer);
                }
                else if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Plot)
                {
                    PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                    fRenderer.FilterModel = FilterHolderViewModel;
                    (_front.Content as Front).SetContent(fRenderer);
                    _front.ShowToggle = FilterHolderViewModel.ShowSettings;
                }
                else if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Line)
                {
                    PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                    fRenderer.FilterModel = FilterHolderViewModel;
                    (_front.Content as Front).SetContent(fRenderer);
                    _front.ShowToggle = FilterHolderViewModel.ShowSettings;
                }
                else if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Map)
                {
                    MapFilterRenderer2 fRenderer = new MapFilterRenderer2(false);
                    fRenderer.FilterModel = FilterHolderViewModel;
                    (_front.Content as Front).SetContent(fRenderer);
                    _front.ShowToggle = FilterHolderViewModel.ShowSettings;
                }
                else if (FilterHolderViewModel.FilterRendererType == FilterRendererType.Frozen)
                {
                    (_front.Content as Front).SetContent(FilterHolderViewModel.FrozenImage);
                    (_front.Content as Front).HorizontalContentAlignment = HorizontalAlignment.Left;
                    (_front.Content as Front).VerticalContentAlignment = VerticalAlignment.Top;
                    _front.ShowToggle = false;
                    _front.CreateBitmapForInteractions = false;
                }

                changeIncomingFilterModel();
            }

            //
            /*if (contentGrid != null)
            {
                _filter = new data.filter.Filter();
                _filter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                _filter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                _filter.FilterModel = FilterHolderViewModel;

                contentGrid.Children.Clear();
                contentGrid.Children.Add(_filter);
            }*/
        }

        public override void FlipSides()
        {
            if (_isFrontShown)
            {
                transitionPresenter.Content = _back;
                if (FilterHolderViewModel != null)
                {
                    (_back.Content as Back).SetStroqs(FilterHolderViewModel.NameStroqs);
                }
            }
            else
            {
                transitionPresenter.Content = _front;
                if (FilterHolderViewModel != null)
                {
                    if ((_back.Content as Back).Stroqs != null)
                    {
                        FilterHolderViewModel.NameStroqs = (_back.Content as Back).Stroqs;
                        FilterHolderViewModel.Name = (_back.Content as Back).Name.Trim();
                    }
                }
            }
            _isFrontShown = !_isFrontShown;
        }

        public byte[] CreateImage()
        {
            FilterRenderer renderer = _front.VisualDescendentsOfType<FilterRenderer>().First();
            return renderer.CreateImage();
        }

        public override void NotifyMove(Pt delta)
        {
            CombinedFilterHolder combinedFilterHolder = this.FindParent<CombinedFilterHolder>();
            if (combinedFilterHolder != null && combinedFilterHolder.FilterHolderViewModel == this.FilterHolderViewModel)
            {
                combinedFilterHolder.NotifyMove(delta);
            }
            else
            {
                base.NotifyMove(delta);

                FilterHolderViewModel.Center = this.GetBounds().Center.GetVec().GetWindowsPoint();
                FilterHolderViewModel.Dimension = new Vec(this.Width, this.Height);
            }
        }

        public override void NotifyScale(Vec delta, Vec offset)
        {
            CombinedFilterHolder combinedFilterHolder = this.FindParent<CombinedFilterHolder>();
            if (combinedFilterHolder != null && combinedFilterHolder.FilterHolderViewModel == this.FilterHolderViewModel)
            {
                combinedFilterHolder.NotifyScale(delta, offset);
            }
            else
            {
                base.NotifyScale(delta, offset);

                FilterHolderViewModel.Center = this.GetBounds().Center.GetVec().GetWindowsPoint();
                FilterHolderViewModel.Dimension = new Vec(this.Width, this.Height);
            }
        }

        public override void InitPostionAndDimension(Pt pos, Vec dim)
        {
            base.InitPostionAndDimension(pos, dim);

            FilterHolderViewModel.Center = new Pt(pos.X + dim.X / 2.0, pos.Y + dim.Y / 2.0);
            FilterHolderViewModel.Dimension = new Vec(this.Width, this.Height);
        }

        public override void SetCenter(Pt pos)
        {
            base.SetCenter(pos);
            FilterHolderViewModel.Center = pos;
        }

        public override void SetDimension(Vec dim)
        {
            base.SetDimension(dim);
            FilterHolderViewModel.Dimension = new Vec(this.Width, this.Height);
        }

        public override void NotifyInteraction()
        {
            base.NotifyInteraction();
        }
    }
}
