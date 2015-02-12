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
using PanoramicDataModel;
using PixelLab.Common;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using PanoramicData.view.other;
using PanoramicData.model.view;
using PanoramicData.view.table;
using CombinedInputAPI;
using PanoramicData.model.view;
using PanoramicData.model.data;
using PanoramicData.view.inq;
using System.Reactive.Linq;
using System.Collections.Specialized;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for Colorer.xaml
    /// </summary>
    public partial class Colorer : UserControl, AttributeViewModelEventHandler
    {
        private Point _colorerStartDrag = new Point(0, 0);
        private Border _colorerShadow = null;
        private IDisposable _observableDisposable = null;

        public Colorer()
        {
            InitializeComponent();
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchDownEvent));
            this.DataContextChanged += Colorer_DataContextChanged;
        }

        void Colorer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                if (_observableDisposable != null)
                {
                    _observableDisposable.Dispose();
                }
            }
            if (e.NewValue != null)
            {
                if (_observableDisposable != null)
                {
                    _observableDisposable.Dispose();
                }
                _observableDisposable = Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(
                    (e.NewValue as VisualizationViewModel).QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.Color), "CollectionChanged")
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            populate();
                        }));
                    });

                populate();
            }
        }

        public void populate()
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;

            if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Color).Count == 0)
            {
                colorGridP1.Fill = Brushes.LightGray;
                colorGridP2.Fill = Brushes.LightGray;
                colorGridP3.Fill = Brushes.LightGray;
            }
            else
            {
                colorGridP1.Fill = (DataContext as VisualizationViewModel).Brush;
                colorGridP2.Fill = (DataContext as VisualizationViewModel).Brush;
                colorGridP3.Fill = (DataContext as VisualizationViewModel).Brush;
            }
        }

        private void colorGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;

            if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Color).Count > 0)
            {
                InkableScene inkableScene = this.FindParent<InkableScene>();

                e.TouchDevice.Capture(this);

                _colorerStartDrag = e.GetTouchPoint(inkableScene).Position;

                this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchMoveEvent));
                this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchUpEvent));
            }
            e.Handled = true;
        }

        private void colorGrid_TouchUpEvent(Object sender, TouchEventArgs e)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;

            var element = (FrameworkElement)sender;
            e.Handled = true;
            e.TouchDevice.Capture(null);

            element.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchMoveEvent));
            element.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchUpEvent));

            if (_colorerShadow != null)
            {
                InkableScene inkableScene = this.FindParent<InkableScene>();

               /* if (ColorerDropped != null)
                {
                    Rct bounds = _colorerShadow.GetBounds(inkableScene);
                    //ColorerDropped(this, new DatabaseTableEventArgs(bounds,
                     //   FilterModel.TableModel, FilterModel, true));
                }*/

                inkableScene.Remove(_colorerShadow);
                _colorerShadow = null;
            }
            else
            {
                InkableScene inkableScene = this.FindParent<InkableScene>();
                Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

                RadialMenuCommand root = new RadialMenuCommand();
                root.IsSelectable = false;

                List<AttributeOperationModel> attributeOperationModels = new List<AttributeOperationModel>();

                // field level colorings
                attributeOperationModels.AddRange(queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Color));

                foreach (var attributeOperationModel in attributeOperationModels)
                {
                    RadialMenuCommand rmc = new RadialMenuCommand();
                    rmc.Name = attributeOperationModel.AttributeModel.Name.Replace(" ", "\n");
                    rmc.Data = attributeOperationModel;
                    rmc.IsSelectable = true;
                    rmc.IsActive = queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Color).Contains(attributeOperationModel);
                    rmc.ActiveTriggered = (cmd) =>
                    {
                        AttributeOperationModel aom = cmd.Data as AttributeOperationModel;
                        togglecoloring(aom);
                    };
                    root.AddSubCommand(rmc);
                }

                if (root.InnerCommands.Count > 0)
                {
                    RadialControl rc = new RadialControl(root,
                        new colorGridRadialControlExecution(inkableScene));
                    rc.SetPosition(fromInkableScene.X - RadialControl.SIZE / 2,
                        fromInkableScene.Y - RadialControl.SIZE / 2);
                    inkableScene.Add(rc);
                }
            }
        }

        private void colorGrid_TouchMoveEvent(Object sender, TouchEventArgs e)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

            Vec v = _colorerStartDrag - fromInkableScene;
            if (v.Length > 10 && _colorerShadow == null)
            {
                ManipulationStart(fromInkableScene);
            }
            ManipulationMove(fromInkableScene);
        }

        public void ManipulationStart(Point fromInkableScene)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            if (inkableScene != null)
            {
                _colorerStartDrag = fromInkableScene;
                _colorerShadow = new Border();
                _colorerShadow.Width = 120;
                _colorerShadow.Height = 40;
                _colorerShadow.Background = new SolidColorBrush(Color.FromArgb(70, 125, 125, 125));
                _colorerShadow.BorderBrush = Brushes.Black;
                Label l = new Label();
                l.HorizontalAlignment = HorizontalAlignment.Center;
                l.VerticalAlignment = VerticalAlignment.Center;
                l.FontWeight = FontWeights.Bold;
                l.Content = "Legend";
                _colorerShadow.Child = l;

                _colorerShadow.RenderTransform = new TranslateTransform(
                    fromInkableScene.X - _colorerShadow.Width / 2.0,
                    fromInkableScene.Y - _colorerShadow.Height);
                inkableScene.Add(_colorerShadow);
            }
        }

        public void ManipulationMove(Point fromInkableScene)
        {
            if (_colorerShadow != null)
            {
                InkableScene inkableScene = this.FindParent<InkableScene>();
                _colorerStartDrag = fromInkableScene;
                _colorerShadow.RenderTransform = new TranslateTransform(
                    fromInkableScene.X - _colorerShadow.Width / 2.0,
                    fromInkableScene.Y - _colorerShadow.Height);
                inkableScene.Add(_colorerShadow);
            }
        }

        private void togglecoloring(AttributeOperationModel attributeOperationModel)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
            if (!queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Color).Contains(attributeOperationModel))
            {
                if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Count > 0)
                {
                    attributeOperationModel.IsGrouped = true;
                }
                //queryModel.AddFunctionAttributeOperationModel(AttributeFunction.Group, attributeOperationModel);
                queryModel.AddFunctionAttributeOperationModel(AttributeFunction.Color, attributeOperationModel);
            }
            else
            {
                //queryModel.RemoveFunctionAttributeOperationModel(AttributeFunction.Group, attributeOperationModel);
                queryModel.RemoveFunctionAttributeOperationModel(AttributeFunction.Color, attributeOperationModel);
            }
        }

        public void AttributeViewModelMoved(AttributeViewModel sender, AttributeViewModelEventArgs e, bool overElement)
        {
            if (overElement)
            {
                colorGridRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                colorGridRectangle.Visibility = Visibility.Collapsed;
            }
        }

        public void AttributeViewModelDropped(AttributeViewModel sender, AttributeViewModelEventArgs e)
        {
            togglecoloring(e.AttributeOperationModel);
            colorGridRectangle.Visibility = Visibility.Collapsed;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return new GeometryHitTestResult(this, IntersectionDetail.Intersects);
        }
    }

    public class colorGridRadialControlExecution : RadialControlExecution
    {
        private InkableScene _inkableScene = null;

        public colorGridRadialControlExecution(InkableScene inkableScene)
        {
            this._inkableScene = inkableScene;
        }

        public override void Remove(RadialControl sender, RadialMenuCommand cmd)
        {
            base.Remove(sender, cmd);
        }

        public override void Dispose(RadialControl sender)
        {
            base.Dispose(sender);

            if (_inkableScene != null)
            {
                _inkableScene.Remove(sender as FrameworkElement);
            }
        }

        public override void ExecuteCommand(
            RadialControl sender, RadialMenuCommand cmd,
            string needle = null, List<InkStroke> stroqs = null)
        {
            // exectue Action
            if (cmd.ActiveTriggered != null)
            {
                cmd.ActiveTriggered(cmd);
            }
        }
    }
}
