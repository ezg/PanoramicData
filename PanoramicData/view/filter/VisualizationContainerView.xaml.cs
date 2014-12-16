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
using PanoramicData.model.view_new;

namespace PanoramicData.view.filter
{
    public partial class VisualizationContainerView : MovableElement
    {
        public static int WIDTH = 300;
        public static int HEIGHT = 200;

        private Resizer _front = new Resizer(true);
        private Resizer _back = new Resizer(false);
        private bool _isFrontShown = true;
    
        private IDisposable _visualizationViewModelDisposable = null;

        private FilterModelAttachment _filterAttachment = null;
        private FilterModelAttachment _brushAttachment = null;

        public VisualizationContainerView()
        {
            InitializeComponent();
            this.Type = MovableElementType.Rect;
            this.HasEnclosedAnchor = false;

            _filterAttachment = new FilterModelAttachment(FilteringType.Filter);
            MainViewController.Instance.InkableScene.Add(_filterAttachment);

            _brushAttachment = new FilterModelAttachment(FilteringType.Brush);
            MainViewController.Instance.InkableScene.Add(_brushAttachment);

            this.DataContextChanged += VisualizationContainerView_DataContextChanged;
        }
        
        ~VisualizationContainerView()
        {

        }

        void VisualizationContainerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                (e.OldValue as VisualizationViewModel).PropertyChanged -= VisualizationContainerView_PropertyChanged;
                if (_visualizationViewModelDisposable != null)
                {
                    _visualizationViewModelDisposable.Dispose();
                }
            }
            if (e.NewValue != null)
            {
                VisualizationViewModel model = (e.NewValue as VisualizationViewModel);
                model.PropertyChanged += VisualizationContainerView_PropertyChanged;

                _visualizationViewModelDisposable = Observable.FromEventPattern<VisualizationViewModelUpdatedEventArgs>(
                    model, "VisualizationViewModelUpdated")
                    .Where(
                        arg => arg.EventArgs != null)
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            this.updateRendering();
                        }));
                    });

                this.updateRendering();
            }
        }

        private void updateRendering()
        {
            VisualizationViewModel visualizationViewModel = (DataContext as VisualizationViewModel);

            rotationTransition.Duration = new Duration(TimeSpan.FromSeconds(0.05));
            _front.Content = new Front();
            _front.DataContext = visualizationViewModel;
            transitionPresenter.Content = _front;

            if (visualizationViewModel.VisualizationType != VisualizationType.Table ||
                visualizationViewModel.VisualizationType != VisualizationType.Pivot)
            {
                _back.Content = new Back();
                _back.DataContext = visualizationViewModel;
            }

            rotationTransition.Duration = new Duration(TimeSpan.FromSeconds(0.75));

            if (visualizationViewModel.VisualizationType == VisualizationType.Pie)
            {
                PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                (_front.Content as Front).SetContent(fRenderer);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Histogram)
            {
                PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                (_front.Content as Front).SetContent(fRenderer);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Table)
            {
                FilterRenderer fRenderer = new TableFilterRenderer();
                (_front.Content as Front).SetContent(fRenderer);
                _front.CreateBitmapForInteractions = true;
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Pivot)
            {
                FilterRenderer fRenderer = new PivotFilterRenderer();
                (_front.Content as Front).SetContent(fRenderer);
                _front.ShowToggle = false;
                _front.CreateBitmapForInteractions = false;
                MainViewController.Instance.InkableScene.Remove(_brushAttachment);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Slider)
            {
                SliderFilterRenderer fRenderer = new SliderFilterRenderer();
                (_front.Content as Front).SetContent(fRenderer);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Plot)
            {
                PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                (_front.Content as Front).SetContent(fRenderer);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Line)
            {
                PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                (_front.Content as Front).SetContent(fRenderer);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Map)
            {
                MapFilterRenderer2 fRenderer = new MapFilterRenderer2(false);
                (_front.Content as Front).SetContent(fRenderer);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Frozen)
            {
                /*(_front.Content as Front).SetContent(FilterHolderViewModel.FrozenImage);
                (_front.Content as Front).HorizontalContentAlignment = HorizontalAlignment.Left;
                (_front.Content as Front).VerticalContentAlignment = VerticalAlignment.Top;
                _front.ShowToggle = false;
                _front.CreateBitmapForInteractions = false;*/
            }

            changeIncomingFilterModel();
        }

        void VisualizationContainerView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
        }

        void changeIncomingFilterModel()
        {
            /*if (_filterAttachment != null)
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
            }*/
        }

        public override void FlipSides()
        {
            /*if (_isFrontShown)
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
            _isFrontShown = !_isFrontShown;*/
        }

        public override void NotifyMove(Pt delta)
        {
            base.NotifyMove(delta);

            (DataContext as VisualizationViewModel).Position = this.GetBounds().TopLeft.GetVec().GetWindowsPoint();
            (DataContext as VisualizationViewModel).Size = new Vec(this.Width, this.Height);
        }

        public override void NotifyScale(Vec delta, Vec offset)
        {
            base.NotifyScale(delta, offset);

            (DataContext as VisualizationViewModel).Position = this.GetBounds().TopLeft.GetVec().GetWindowsPoint();
            (DataContext as VisualizationViewModel).Size = new Vec(this.Width, this.Height);
        }

        public override void InitPostionAndDimension(Pt pos, Vec dim)
        {
            base.InitPostionAndDimension(pos, dim);
        }

        public override Pt GetPosition()
        {
            return (DataContext as VisualizationViewModel).Position;
        }

        public override void SetPosition(Pt pos)
        {
            (DataContext as VisualizationViewModel).Position = pos;
        }

        public override void SetSize(Vec dim)
        {
            (DataContext as VisualizationViewModel).Size = dim;
        }

        public override Vec GetSize()
        {
            return (DataContext as VisualizationViewModel).Size;
        }

        public override Vec GetMinSize()
        {
            return new Vec(this.MinWidth, this.MinHeight);
        }

        public override void NotifyInteraction()
        {
            base.NotifyInteraction();
        }
    }
}
