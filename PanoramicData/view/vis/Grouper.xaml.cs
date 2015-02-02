using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Cryptography;
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
using PixelLab.Common;
using Recognizer.NDollar;
using starPadSDK.AppLib;
using starPadSDK.Inq;
using PanoramicData.view.other;
using PanoramicData.view.table;
using PanoramicData.model.view;
using PanoramicData.model.view_new;
using System.Reactive.Linq;
using System.Collections.Specialized;
using PanoramicData.model.data;
using PanoramicData.view.inq;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for Grouper.xaml
    /// </summary>
    public partial class Grouper : UserControl, AttributeViewModelEventHandler
    {
        private IDisposable _observableDisposable = null;

        public Grouper()
        {
            InitializeComponent();
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(GroupGrid_TouchDownEvent));
            this.DataContextChanged += Grouper_DataContextChanged;
        }

        void Grouper_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
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
                    (e.NewValue as VisualizationViewModel).QueryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group), "CollectionChanged")
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

            if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Count == 0)
            {
                groupGridP1.Fill = Brushes.LightGray;
                groupGridP2.Fill = Brushes.LightGray;
                groupGridP3.Fill = Brushes.LightGray;
            }
            else
            {
                groupGridP1.Fill = (DataContext as VisualizationViewModel).Brush;
                groupGridP2.Fill = (DataContext as VisualizationViewModel).Brush;
                groupGridP3.Fill = (DataContext as VisualizationViewModel).Brush;
            }
        }
        
        private void GroupGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            Point fromInqScene = e.GetTouchPoint(inkableScene).Position;

            RadialMenuCommand root = new RadialMenuCommand();
            root.IsSelectable = false;

            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;

            foreach (var attributeOperationModel in queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group))
            {
                if (!(attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.INT ||
                      attributeOperationModel.AttributeModel.AttributeDataType == AttributeDataTypeConstants.FLOAT))
                {
                    RadialMenuCommand groupDistinct = new RadialMenuCommand();
                    groupDistinct.Name = attributeOperationModel.AttributeModel.Name.Replace(" ", "\n");
                    groupDistinct.Data = attributeOperationModel;
                    groupDistinct.IsSelectable = true;
                    groupDistinct.IsActive = attributeOperationModel.IsGrouped;
                    groupDistinct.ActiveTriggered = (cmd) =>
                    {
                        AttributeOperationModel model = cmd.Data as AttributeOperationModel;
                        model.IsGrouped = true;
                        if (cmd.IsActive)
                            addGrouping(model);
                        else
                            removeGrouping(model);
                    };
                    root.AddSubCommand(groupDistinct);
                }
                else
                {
                    AttributeOperationModel binModel = null;
                    AttributeOperationModel groupModel = null;
                    if (attributeOperationModel.IsGrouped)
                    {
                        groupModel = attributeOperationModel;
                        binModel = attributeOperationModel;
                    }
                    else
                    {
                        groupModel = attributeOperationModel;
                        binModel = attributeOperationModel;
                    }

                    RadialMenuCommand group = new RadialMenuCommand();
                    group.Name = groupModel.AttributeModel.Name.Replace(" ", "\n");
                    group.Data = groupModel;
                    group.IsActive = groupModel.IsGrouped || groupModel.IsBinned;
                    group.IsSelectable = false;

                    RadialMenuCommandGroup groupGroup = new RadialMenuCommandGroup("groupGroup",
                        RadialMenuCommandComandGroupPolicy.DeactivateAndTriggerOthers);

                    RadialMenuCommand groupDistinct = new RadialMenuCommand();
                    groupDistinct.Name = "Distinct";
                    groupDistinct.Data = groupModel;
                    groupDistinct.CommandGroup = groupGroup;
                    groupDistinct.IsSelectable = true;
                    groupDistinct.IsActive = groupModel.IsGrouped;
                    groupDistinct.ActiveTriggered = (cmd) =>
                    {
                        AttributeOperationModel model = cmd.Data as AttributeOperationModel;
                        model.IsGrouped = true;
                        if (cmd.IsActive)
                            addGrouping(model);
                        else
                            removeGrouping(model);
                    };
                    group.AddSubCommand(groupDistinct);


                    RadialMenuCommand bin = new RadialMenuCommand();
                    bin.Name = "Bin";
                    bin.CommandGroup = groupGroup;
                    bin.Data = binModel;
                    bin.IsActive = binModel.IsBinned;
                    bin.IsSelectable = true;
                    bin.ActiveTriggered = (cmd) =>
                    {
                        AttributeOperationModel model = cmd.Data as AttributeOperationModel;
                        model.IsBinned = true;
                        if (cmd.IsActive)
                            addGrouping(model);
                        else
                            removeGrouping(model);
                    };
                    group.AddSubCommand(bin);

                    RadialMenuCommand binSize = new RadialMenuCommand();
                    binSize.Name = "Bin Size";
                    binSize.Data = binModel;
                    binSize.AllowsNumericInput = true;
                    binSize.IsRangeNumericInput = false;
                    binSize.MaxNumericValue = binModel.MaxBinSize;
                    binSize.MinNumericValue = 1;
                    binSize.UpperNumericValue = binModel.BinSize;
                    binSize.IsSelectable = false;
                    binSize.ActiveTriggered = (cmd) =>
                    {
                        AttributeOperationModel model = cmd.Data as AttributeOperationModel;
                        model.BinSize = cmd.UpperNumericValue;
                    };
                    bin.AddSubCommand(binSize);
                    root.AddSubCommand(group);
                }
            }

            if (root.InnerCommands.Count > 0)
            {
                RadialControl rc = new RadialControl(root,
                    new GroupGridRadialControlExecution(queryModel, inkableScene));
                rc.SetPosition(fromInqScene.X - RadialControl.SIZE / 2,
                    fromInqScene.Y - RadialControl.SIZE / 2);
                inkableScene.Add(rc);
            }
            e.Handled = true;
        }

        private void removeGrouping(AttributeOperationModel attributeOperationModel)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
            if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Contains(attributeOperationModel))
            {
                queryModel.RemoveFunctionAttributeOperationModel(AttributeFunction.Group, attributeOperationModel);
                //queryModel.RemoveGrouping(attributeOperationModel);
            }
        }

        private void addGrouping(AttributeOperationModel attributeOperationModel)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
            if (!queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Group).Contains(attributeOperationModel))
            {
                queryModel.AddFunctionAttributeOperationModel(AttributeFunction.Group, attributeOperationModel);
                //queryModel.AddGrouping(columnDescriptor);
            }
        }

        public void AttributeViewModelMoved(AttributeViewModel sender, AttributeViewModelEventArgs e, bool overElement)
        {
            if (overElement)
            {
                groupGridRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                groupGridRectangle.Visibility = Visibility.Collapsed;
            }
        }

        public void AttributeViewModelDropped(AttributeViewModel sender, AttributeViewModelEventArgs e)
        {
            e.AttributeOperationModel.IsGrouped = true;
            addGrouping(e.AttributeOperationModel);
            groupGridRectangle.Visibility = Visibility.Collapsed;
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

    public class GroupGridRadialControlExecution : RadialControlExecution
    {
        private QueryModel _queryModel = null;
        private InkableScene _inkableScene = null;

        public GroupGridRadialControlExecution(QueryModel queryModel, InkableScene inkableScene)
        {
            this._queryModel = queryModel;
            this._inkableScene = inkableScene;
        }

        public override void Remove(RadialControl sender, RadialMenuCommand cmd)
        {
            base.Remove(sender, cmd);

            if (_queryModel != null)
            {
                _queryModel.RemoveAttributeOperationModel((cmd.Data as AttributeViewModel).AttributeOperationModel);
            }
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
}
