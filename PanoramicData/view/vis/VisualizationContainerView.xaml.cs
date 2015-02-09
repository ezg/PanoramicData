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
using PanoramicData.view.vis.render;
using PanoramicData.utils;
using PanoramicData.view.inq;

namespace PanoramicData.view.vis
{
    public partial class VisualizationContainerView : MovableElement, IScribbable
    {
        public static int WIDTH = 300;
        public static int HEIGHT = 200;

        private Resizer _front = new Resizer(true);
        private Resizer _back = new Resizer(false);
        private bool _isFrontShown = true;

        public VisualizationContainerView()
        {
            InitializeComponent();
            this.Type = MovableElementType.Rect;
            this.DataContextChanged += VisualizationContainerView_DataContextChanged;
        }
        
        void VisualizationContainerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                (e.OldValue as VisualizationViewModel).PropertyChanged -= VisualizationContainerView_PropertyChanged;
            }
            if (e.NewValue != null)
            {
                VisualizationViewModel model = (e.NewValue as VisualizationViewModel);
                model.PropertyChanged += VisualizationContainerView_PropertyChanged;
                visualizationTypeUpdated();
            }
        }

        void VisualizationContainerView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            VisualizationViewModel visualizationViewModel = (DataContext as VisualizationViewModel);
            if (e.PropertyName == visualizationViewModel.GetPropertyName(() => visualizationViewModel.VisualizationType))
            {
                visualizationTypeUpdated();
            }
        }

        void visualizationTypeUpdated()
        {
            VisualizationViewModel visualizationViewModel = (DataContext as VisualizationViewModel);
            rotationTransition.Duration = new Duration(TimeSpan.FromSeconds(0.05));
            _front.Content = new Front();
            _front.DataContext = visualizationViewModel;
            transitionPresenter.Content = _front;

            if (visualizationViewModel.VisualizationType != VisualizationType.Table)
            {
                _back.Content = new Back();
                _back.DataContext = visualizationViewModel;
            }

            rotationTransition.Duration = new Duration(TimeSpan.FromSeconds(0.75));

            if (visualizationViewModel.VisualizationType == VisualizationType.Bar)
            {
                FilterRenderer fRenderer = new BarChartRenderer();
                (_front.Content as Front).SetContent(fRenderer);
                _front.CreateBitmapForInteractions = true;
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Table)
            {
                FilterRenderer fRenderer = new TableRenderer();
                (_front.Content as Front).SetContent(fRenderer);
                _front.CreateBitmapForInteractions = true;
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Plot)
            {
                //PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                //(_front.Content as Front).SetContent(fRenderer);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Line)
            {
                //PlotFilterRenderer4 fRenderer = new PlotFilterRenderer4(false);
                //(_front.Content as Front).SetContent(fRenderer);
            }
            else if (visualizationViewModel.VisualizationType == VisualizationType.Map)
            {
                //MapFilterRenderer2 fRenderer = new MapFilterRenderer2(false);
                //(_front.Content as Front).SetContent(fRenderer);
            }
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

        public override void NotifyMove(Point delta)
        {
            base.NotifyMove(delta);

            (DataContext as VisualizationViewModel).Position = this.GetBounds().TopLeft.GetVec().GetWindowsPoint();
            (DataContext as VisualizationViewModel).Size = new Vector2(this.Width, this.Height);
        }

        public override void NotifyScale(Vector2 delta, Vector2 offset)
        {
            base.NotifyScale(delta, offset);

            (DataContext as VisualizationViewModel).Position = this.GetBounds().TopLeft.GetVec().GetWindowsPoint();
            (DataContext as VisualizationViewModel).Size = new Vector2(this.Width, this.Height);
        }

        public override void InitPostionAndDimension(Point pos, Vector2 dim)
        {
            base.InitPostionAndDimension(pos, dim);
        }

        public override Point GetPosition()
        {
            return (DataContext as VisualizationViewModel).Position;
        }

        public override void SetPosition(Point pos)
        {
            (DataContext as VisualizationViewModel).Position = pos;
        }

        public override void SetSize(Vector2 dim)
        {
            (DataContext as VisualizationViewModel).Size = dim;
        }

        public override Vector2 GetSize()
        {
            return (DataContext as VisualizationViewModel).Size;
        }

        public override Vector2 GetMinSize()
        {
            return new Vector2(this.MinWidth, this.MinHeight);
        }

        public override void NotifyInteraction()
        {
            base.NotifyInteraction();
        }

        public GeoAPI.Geometries.IGeometry Geometry
        {
            get
            {
                VisualizationViewModel model = this.DataContext as VisualizationViewModel;

                Rect bounds = new Rect(model.Position , new Size(model.Size.X, model.Size.Y));
                return bounds.GetPolygon();
            }
        }
    }
}
