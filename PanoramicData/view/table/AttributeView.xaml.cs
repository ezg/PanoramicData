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
using PanoramicData.view.inq;
using PanoramicData.model.view_new;

namespace PanoramicData.view.table
{
    /// <summary>
    /// Interaction logic for AttributeView.xaml
    /// </summary>
    public partial class AttributeView : UserControl
    {
        private bool _isShadow = false;
        private AttributeView _shadow = null;
        private long _manipulationStartTime = 0;
        private Point _startDrag = new Point(0, 0);
        private Point _currentFromInkableScene = new Point(0, 0);
        private bool _boundsChanged = false;
        private TouchDevice _dragDevice1 = null;

        public AttributeView()
        {
            InitializeComponent();
        }

       /* public AttributeView(bool isShadow, bool renderDark = false)
        {
           

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

            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDownEvent));
            SetBinding(MyDataContextProperty, new Binding());
        }*/
        
        /*public void ShortcutGesture(string recog)
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
        }*/

        private void SimpleGridViewColumnHeader_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if (((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus) || (DataContext as AttributeViewModel).IsDraggableByPen)
            {
                if (_dragDevice1 == null)
                {
                    e.Handled = true;
                    e.TouchDevice.Capture(this);
                    InkableScene inkableScene = this.FindParent<InkableScene>();
                    Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

                    _manipulationStartTime = DateTime.Now.Ticks;
                    _startDrag = fromInkableScene;

                    this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDragEvent));
                    this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchUpEvent));
                    _dragDevice1 = e.TouchDevice;
                }
            }
        }

        public void ManipulationStart(Point fromInkableScene)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            if (inkableScene != null)
            {
                _currentFromInkableScene = fromInkableScene;
                _shadow = new AttributeView();
                _shadow.DataContext = new AttributeViewModel(DataContext as AttributeViewModel);

                _shadow.Measure(new Size(double.PositiveInfinity,
                                         double.PositiveInfinity));

                double add = (DataContext as AttributeViewModel).IsNoChrome ? 30 : 0;
                _shadow.Width = this.ActualWidth + add;
                _shadow.HeaderBorder.Width = this.ActualWidth + add;
                _shadow.Height = _shadow.DesiredSize.Height;
                _shadow.HeaderBorder.Height = _shadow.DesiredSize.Height;

                _shadow.RenderTransform = new TranslateTransform(
                    fromInkableScene.X - _shadow.Width / 2.0,
                    fromInkableScene.Y - _shadow.Height);
                inkableScene.Add(_shadow);

                Rct bounds = _shadow.GetBounds(inkableScene);
                (DataContext as AttributeViewModel).FireMoved(bounds, AttributeViewModelEventArgType.Default);
            }
        }

        private void SimpleGridViewColumnHeader_TouchDragEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                InkableScene inkableScene = this.FindParent<InkableScene>();
                Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

                Vec v = _startDrag - fromInkableScene;
                if (v.Length > 10 && _shadow == null)
                {
                    ManipulationStart(fromInkableScene);
                }
                ManipulationMove(fromInkableScene);
            }
        }

        public void ManipulationMove(Point fromInkableScene)
        {
            if (_shadow != null)
            {
                InkableScene inkableScene = this.FindParent<InkableScene>();
                _currentFromInkableScene = fromInkableScene;
                _shadow.RenderTransform = new TranslateTransform(
                    fromInkableScene.X - _shadow.Width / 2.0,
                    fromInkableScene.Y - _shadow.Height);
                if (inkableScene != null)
                {
                    inkableScene.Add(_shadow);

                    Rct bounds = _shadow.GetBounds(inkableScene);
                    (DataContext as AttributeViewModel).FireMoved(bounds, AttributeViewModelEventArgType.Default);
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
                InkableScene inkableScene = this.FindParent<InkableScene>();
                Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

                if (_shadow == null && 
                    _manipulationStartTime + TimeSpan.FromSeconds(0.5).Ticks > DateTime.Now.Ticks)
                {
                    if ((DataContext as AttributeViewModel).IsMenuEnabled)
                    {
                        DisplayRadialControl(fromInkableScene);
                    }
                    else
                    {
                        if (DataContext is CalculatedColumnDescriptor)
                        {
                            CalculatedColumnDescriptorInfo info = (DataContext as CalculatedColumnDescriptor).CalculatedColumnDescriptorInfo;

                            if (info != null)
                            {
                                /*MathEditor me = new MathEditor(
                                    new SimpleGridViewColumnHeaderMathEditorExecution(inkableScene, info), FilterModel,
                                    info);
                                me.SetPosition(fromInkableScene.X - RadialControl.SIZE/2,
                                    fromInkableScene.Y - RadialControl.SIZE/2);
                                inkableScene.Add(me);*/
                            }
                        }
                    }
                }

                ManipulationEnd(fromInkableScene);

                _manipulationStartTime = 0;

                this.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchDragEvent));
                this.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(SimpleGridViewColumnHeader_TouchUpEvent));   
            }
        }

        public void ManipulationEnd(Point fromInkableScene)
        {
            if (_shadow != null)
            {
                InkableScene inkableScene = this.FindParent<InkableScene>();

                Rct bounds = _shadow.GetBounds(inkableScene);
                (DataContext as AttributeViewModel).FireDropped(bounds, AttributeViewModelEventArgType.Default);

                inkableScene.Remove(_shadow);
                _shadow = null;
            }
        }

        public void DisplayRadialControl(Point fromInkableScene)
        {
            InkableScene inkableScene = this.FindParent<InkableScene>();
            if (inkableScene != null)
            {
                /*RadialControl rc = new RadialControl(setupRadialCommands(DataContext as PanoramicDataColumnDescriptor),
                    new ColumnHeaderRadialControlExecution(FilterModel, TableModel, inkableScene));
                rc.SetPosition(fromInkableScene.X - RadialControl.SIZE / 2,
                    fromInkableScene.Y - RadialControl.SIZE / 2);
                inkableScene.Add(rc);*/
            }
        }

        /*RadialMenuCommand setupRadialCommands(AttributeViewModel atrributeViewModel)
        {
            RadialMenuCommand root = new RadialMenuCommand();
            root.Data = atrributeViewModel;
            root.IsSelectable = false;

            if (atrributeViewModel.IsRemoveEnabled)
            {
                RadialMenuCommand remove = new RadialMenuCommand();
                remove.Name = "Remove";
                remove.Data = atrributeViewModel;
                remove.IsRemove = true;
                remove.IsSelectable = true;
                root.AddSubCommand(remove);
            }

            if (EnableScaleFunctionInRadialMenu)
            {
                RadialMenuCommandGroup scaleGroup = new RadialMenuCommandGroup("scaleGroup", RadialMenuCommandComandGroupPolicy.DeactivateOthers);
                RadialMenuCommand scale = new RadialMenuCommand();
                scale.Name = "Scale\nFunction";
                scale.Data = atrributeViewModel;
                scale.IsActive = atrributeViewModel.ScaleFunction != ScaleFunction.None;
                root.AddSubCommand(scale);

                RadialMenuCommand log = new RadialMenuCommand();
                log.Name = "Log";
                log.CommandGroup = scaleGroup;
                log.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                log.Data = atrributeViewModel;
                log.IsSelectable = true;
                log.IsActive = atrributeViewModel.ScaleFunction == ScaleFunction.Log;
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
                norm.Data = atrributeViewModel;
                norm.IsSelectable = true;
                norm.IsActive = atrributeViewModel.ScaleFunction == ScaleFunction.Normalize;
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
                rt.Data = atrributeViewModel;
                rt.IsSelectable = true;
                rt.IsActive = atrributeViewModel.ScaleFunction == ScaleFunction.RunningTotal;
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
                rtNorm.Data = atrributeViewModel;
                rtNorm.IsSelectable = true;
                rtNorm.IsActive = atrributeViewModel.ScaleFunction == ScaleFunction.RunningTotalNormalized;
                rtNorm.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.ScaleFunction = cmd.IsActive ? ScaleFunction.RunningTotalNormalized : ScaleFunction.None;
                };
                scale.AddSubCommand(rtNorm);
            }

            RadialMenuCommand sort = new RadialMenuCommand();
            sort.Name = "Sort";
            sort.Data = atrributeViewModel;
            sort.IsActive = atrributeViewModel.SortMode != SortMode.None;
            sort.IsSelectable = false;
            root.AddSubCommand(sort);

            RadialMenuCommandGroup sortGroup = new RadialMenuCommandGroup("sortGroup", RadialMenuCommandComandGroupPolicy.DeactivateOthers);
            RadialMenuCommand sortAsc = new RadialMenuCommand();
            sortAsc.Name = "Asc";
            sortAsc.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
            sortAsc.CommandGroup = sortGroup;
            sortAsc.Data = atrributeViewModel;
            sortAsc.IsActive = atrributeViewModel.SortMode == SortMode.Asc;
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
            sortDesc.Data = atrributeViewModel;
            sortDesc.IsActive = atrributeViewModel.SortMode == SortMode.Desc;
            sortDesc.IsSelectable = true;
            sortDesc.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.SortMode = cmd.IsActive ? SortMode.Desc : SortMode.None;
            };
            sort.AddSubCommand(sortDesc);

            RadialMenuCommand vis = new RadialMenuCommand();
            vis.Name = "Vis";
            vis.Data = atrributeViewModel;
            vis.IsSelectable = true;
            vis.IsActive = atrributeViewModel.IsVisualization;
            vis.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.IsVisualization = cmd.IsActive;
            };
            // ez: tmp removed root.AddSubCommand(vis);

            RadialMenuCommandGroup aggGroup = new RadialMenuCommandGroup("aggGroup", RadialMenuCommandComandGroupPolicy.DeactivateOthers);
            RadialMenuCommand aggregate = new RadialMenuCommand();
            aggregate.Name = "Transform";
            aggregate.Data = atrributeViewModel;
            aggregate.IsActive = atrributeViewModel.AggregateFunction != AggregateFunction.None;
            root.AddSubCommand(aggregate);

            if (atrributeViewModel.DataType == DataTypeConstants.INT ||
                atrributeViewModel.DataType == DataTypeConstants.FLOAT)
            {
                RadialMenuCommand sum = new RadialMenuCommand();
                sum.Name = "Sum";
                sum.CommandGroup = aggGroup;
                sum.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                sum.Data = atrributeViewModel;
                sum.IsSelectable = true;
                sum.IsActive = atrributeViewModel.AggregateFunction == AggregateFunction.Sum;
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
                avg.Data = atrributeViewModel;
                avg.IsSelectable = true;
                avg.IsActive = atrributeViewModel.AggregateFunction == AggregateFunction.Avg;
                avg.ActiveTriggered = (cmd) =>
                {
                    PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                    cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Avg : AggregateFunction.None;
                };
                aggregate.AddSubCommand(avg);
            }
            if (FilterModel != null &&
                FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count > 0 &&
                FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count(cd => cd.IsBinned && cd.MatchSimple(atrributeViewModel)) > 0)
            {
                RadialMenuCommand binRange = new RadialMenuCommand();
                binRange.Name = "Bin Range";
                binRange.CommandGroup = aggGroup;
                binRange.ParentPolicy = RadialMenuCommandParentPolicy.ActivateParentWhenActive;
                binRange.Data = atrributeViewModel;
                binRange.IsSelectable = true;
                binRange.IsActive = atrributeViewModel.AggregateFunction == AggregateFunction.Bin;
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
            max.Data = atrributeViewModel;
            max.IsSelectable = true;
            max.IsActive = atrributeViewModel.AggregateFunction == AggregateFunction.Max;
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
            min.Data = atrributeViewModel;
            min.IsSelectable = true;
            min.IsActive = atrributeViewModel.AggregateFunction == AggregateFunction.Min;
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
            count.Data = atrributeViewModel;
            count.IsSelectable = true;
            count.IsActive = atrributeViewModel.AggregateFunction == AggregateFunction.Count;
            count.ActiveTriggered = (cmd) =>
            {
                PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                cd.AggregateFunction = cmd.IsActive ? AggregateFunction.Count : AggregateFunction.None;
            };
            aggregate.AddSubCommand(count);

            if (FilterModel != null)
            {
                RadialMenuCommand filter = new RadialMenuCommand();
                filter.Name = "Filter";
                filter.Data = atrributeViewModel;
                filter.AllowsStroqInput = true;
                filter.IsSelectable = false;

                RadialMenuCommand actualFilter = new RadialMenuCommand();
                actualFilter.Name = "Dummy Command";
                filter.AddSubCommand(actualFilter);

                root.AddSubCommand(filter);
            }

            return root;
        }*/
    }

    public class ColumnHeaderRadialControlExecution : RadialControlExecution
    {
        private FilterModel _filterModel = null;
        private TableModel _tableModel = null;
        private InkableScene _inkableScene = null;

        public ColumnHeaderRadialControlExecution(FilterModel filterModel, TableModel tableModel, InkableScene inkableScene)
        {
            this._filterModel = filterModel;
            this._tableModel = tableModel;
            this._inkableScene = inkableScene;
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

            if (_inkableScene != null)
            {
                _inkableScene.Remove(sender as FrameworkElement);
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
    

    public class SimpleGridViewColumnHeaderMathEditorExecution : MathEditorExecution
    {
        private InkableScene _inkableScene = null;
        private CalculatedColumnDescriptorInfo _calculatedColumnDescriptorInfo = null;

        public SimpleGridViewColumnHeaderMathEditorExecution(InkableScene inkableScene, CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo)
        {
            this._inkableScene = inkableScene;
            this._calculatedColumnDescriptorInfo = calculatedColumnDescriptorInfo;
        }

        public override void Dispose(MathEditor sender)
        {
            base.Dispose(sender);
            _calculatedColumnDescriptorInfo.TableModel.UpdateCalculatedColumnDescriptorInfo(_calculatedColumnDescriptorInfo);

            if (_inkableScene != null)
            {
                _inkableScene.Remove(sender as FrameworkElement);
            }
        }
    }
}
