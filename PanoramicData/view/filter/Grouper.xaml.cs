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
using PanoramicDataModel;
using PixelLab.Common;
using Recognizer.NDollar;
using starPadSDK.AppLib;
using starPadSDK.Inq;
using PanoramicData.view.other;
using PanoramicData.view.table;
using PanoramicData.model.view;
using PanoramicData.model.view_new;

namespace PanoramicData.view.filter
{
    /// <summary>
    /// Interaction logic for Grouper.xaml
    /// </summary>
    public partial class Grouper : UserControl, AttributeViewModelEventHandler
    {
        public FilterModel FilterModel { get; set; }

        public Grouper()
        {
            InitializeComponent();
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(GroupGrid_TouchDownEvent));
        }

        public void Init()
        {
            if (!(FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count(cd => true/*!cd.IsHidden*/) > 0) &&
                !FilterModel.TableModel.CalculateRecursivePathInfos().Any(pp => pp.Path.Count > 0))
            {
                groupGridP1.Fill = Brushes.LightGray;
                groupGridP2.Fill = Brushes.LightGray;
                groupGridP3.Fill = Brushes.LightGray;
            }
            else
            {
                groupGridP1.Fill = FilterModel.Brush;
                groupGridP2.Fill = FilterModel.Brush;
                groupGridP3.Fill = FilterModel.Brush;
            }
        }

        private Dictionary<GrouperTopLevelPair, DatabaseColumnDescriptor> _topLevelLookup = new Dictionary<GrouperTopLevelPair, DatabaseColumnDescriptor>();
        private List<PanoramicDataColumnDescriptor> topLevelGroupings()
        {
            List<PanoramicDataColumnDescriptor> descriptors = new List<PanoramicDataColumnDescriptor>();
            List<PathInfo> pathInfos = FilterModel.TableModel.CalculateRecursivePathInfos();
            foreach (var pp in pathInfos)
            {
                if (pp.Path.Count > 0)
                {
                    DatabaseColumnDescriptor column = null;
                    GrouperTopLevelPair pair = new GrouperTopLevelPair(pp.TableInfo.PrimaryKeyFieldInfo, pp);
                    if (!_topLevelLookup.ContainsKey(pair))
                    {
                        _topLevelLookup.Add(pair, new DatabaseColumnDescriptor(pp.TableInfo.PrimaryKeyFieldInfo, pp));
                    }
                    column = _topLevelLookup[pair];
                    if (!descriptors.Contains(column))
                    {
                        column.IsGrouped = FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Contains(column);
                        descriptors.Add(column);
                    }
                }
            }
            return descriptors;
        } 

        private void GroupGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            Point fromInqScene = e.GetTouchPoint(inqScene).Position;

            RadialMenuCommand root = new RadialMenuCommand();
            root.IsSelectable = false;

            List<PanoramicDataColumnDescriptor> descriptors = new List<PanoramicDataColumnDescriptor>();
            descriptors.AddRange(topLevelGroupings());

            // field level groupings
            descriptors.AddRange(FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Where(cd => !cd.IsPrimaryKey/* && !cd.IsHidden*/));

            foreach (var descriptor in descriptors)
            {
                if (!(descriptor.DataType == DataTypeConstants.INT ||
                      descriptor.DataType == DataTypeConstants.FLOAT) || descriptor.IsPrimaryKey)
                {
                    RadialMenuCommand groupDistinct = new RadialMenuCommand();
                    groupDistinct.Name = descriptor.GetSimpleLabel().Replace(" ", "\n");
                    groupDistinct.Data = descriptor;
                    groupDistinct.IsSelectable = true;
                    groupDistinct.IsActive = descriptor.IsGrouped;
                    groupDistinct.ActiveTriggered = (cmd) =>
                    {
                        PanoramicDataColumnDescriptor columnDescriptor = cmd.Data as PanoramicDataColumnDescriptor;
                        columnDescriptor.IsGrouped = true;
                        if (cmd.IsActive)
                            addGrouping(columnDescriptor);
                        else
                            removeGrouping(columnDescriptor);
                    };
                    root.AddSubCommand(groupDistinct);
                }
                else
                {
                    PanoramicDataColumnDescriptor binDescriptor = null;
                    PanoramicDataColumnDescriptor groupDescriptor = null;
                    if (descriptor.IsGrouped)
                    {
                        groupDescriptor = descriptor;
                        binDescriptor = (PanoramicDataColumnDescriptor) descriptor.Clone();
                    }
                    else
                    {
                        groupDescriptor = (PanoramicDataColumnDescriptor) descriptor.Clone();
                        binDescriptor = descriptor;
                    }

                    RadialMenuCommand group = new RadialMenuCommand();
                    group.Name = groupDescriptor.GetSimpleLabel().Replace(" ", "\n");
                    group.Data = groupDescriptor;
                    group.IsActive = groupDescriptor.IsAnyGroupingOperationApplied();
                    group.IsSelectable = false;

                    RadialMenuCommandGroup groupGroup = new RadialMenuCommandGroup("groupGroup",
                        RadialMenuCommandComandGroupPolicy.DeactivateAndTriggerOthers);

                    RadialMenuCommand groupDistinct = new RadialMenuCommand();
                    groupDistinct.Name = "Distinct";
                    groupDistinct.Data = groupDescriptor;
                    groupDistinct.CommandGroup = groupGroup;
                    groupDistinct.IsSelectable = true;
                    groupDistinct.IsActive = groupDescriptor.IsGrouped;
                    groupDistinct.ActiveTriggered = (cmd) =>
                    {
                        PanoramicDataColumnDescriptor columnDescriptor = cmd.Data as PanoramicDataColumnDescriptor;
                        columnDescriptor.IsGrouped = true;
                        if (cmd.IsActive)
                            addGrouping(columnDescriptor);
                        else
                            removeGrouping(columnDescriptor);
                    };
                    group.AddSubCommand(groupDistinct);


                    RadialMenuCommand bin = new RadialMenuCommand();
                    bin.Name = "Bin";
                    bin.CommandGroup = groupGroup;
                    bin.Data = binDescriptor;
                    bin.IsActive = binDescriptor.IsBinned;
                    bin.IsSelectable = true;
                    bin.ActiveTriggered = (cmd) =>
                    {
                        PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                        cd.IsBinned = true;
                        if (cmd.IsActive)
                            addGrouping(cd);
                        else
                            removeGrouping(cd);
                    };
                    group.AddSubCommand(bin);

                    RadialMenuCommand binSize = new RadialMenuCommand();
                    binSize.Name = "Bin Size";
                    binSize.Data = binDescriptor;
                    binSize.AllowsNumericInput = true;
                    binSize.IsRangeNumericInput = false;
                    binSize.MaxNumericValue = binDescriptor.MaxValue.HasValue ? binDescriptor.MaxValue.Value : 100;
                    binSize.MinNumericValue = 1;
                    binSize.UpperNumericValue = binDescriptor.BinSize;
                    binSize.IsSelectable = false;
                    binSize.ActiveTriggered = (cmd) =>
                    {
                        PanoramicDataColumnDescriptor cd = cmd.Data as PanoramicDataColumnDescriptor;
                        cd.BinSize = cmd.UpperNumericValue;
                    };
                    bin.AddSubCommand(binSize);

                    RadialMenuCommand binRange = new RadialMenuCommand();
                    binRange.Name = "Bin Range";
                    binRange.Data = binDescriptor;
                    binRange.AllowsNumericInput = true;
                    binRange.IsRangeNumericInput = true;
                    binRange.MaxNumericValue = binDescriptor.MaxValue.HasValue ? binDescriptor.MaxValue.Value : 100;
                    binRange.MinNumericValue = binDescriptor.MinValue.HasValue ? binDescriptor.MinValue.Value : 0;
                    binRange.UpperNumericValue = binDescriptor.BinUpperBound;
                    binRange.LowerNumericValue = binDescriptor.BinLowerBound;
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
            }

            if (root.InnerCommands.Count > 0)
            {
                RadialControl rc = new RadialControl(root,
                    new GroupGridRadialControlExecution(FilterModel, FilterModel.TableModel, inqScene));
                rc.SetPosition(fromInqScene.X - RadialControl.SIZE / 2,
                    fromInqScene.Y - RadialControl.SIZE / 2);
                inqScene.AddNoUndo(rc);
            }
            e.Handled = true;
        }

        private void removeGrouping(PanoramicDataColumnDescriptor columnDescriptor)
        {
            if (FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Contains(columnDescriptor))
            {
                FilterModel.RemoveGrouping(columnDescriptor);
            }
        }

        private void addGrouping(PanoramicDataColumnDescriptor columnDescriptor)
        {
            if (!FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Contains(columnDescriptor))
            {
                FilterModel.AddGrouping(columnDescriptor);
            }
        }

        public void AttributeViewModelMoved(AttributeView sender, AttributeViewModelEventArgs e, bool overElement)
        {
            if (overElement)
            {
                //if (FilterModel.FilterRendererType != FilterRendererType.Table)
                {
                    groupGridRectangle.Visibility = Visibility.Visible;
                }
            }
            else
            {
                groupGridRectangle.Visibility = Visibility.Collapsed;
            }
        }

        public void AttributeViewModelDropped(AttributeView sender, AttributeViewModelEventArgs e)
        {
            /*var clone = (PanoramicDataColumnDescriptor)e.ColumnDescriptor.SimpleClone();
            clone.IsGrouped = true;
            addGrouping(clone);
            groupGridRectangle.Visibility = Visibility.Collapsed;*/
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
        private FilterModel _filterModel = null;
        private TableModel _tableModel = null;
        private InqScene _inqScene = null;

        public GroupGridRadialControlExecution(FilterModel filterModel, TableModel tableModel, InqScene inqScene)
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

    public class GrouperTopLevelPair
    {
        public FieldInfo FieldInfo { get; set; }
        public PathInfo PathInfo { get; set; }

        public GrouperTopLevelPair(FieldInfo fi, PathInfo pi)
        {
            FieldInfo = fi;
            PathInfo = pi;
        }
        public override int GetHashCode()
        {
            int code = 0;
            code ^= FieldInfo.GetHashCode();
            code ^= PathInfo.GetHashCode();
            return code;
        }

        public override bool Equals(object obj)
        {
            if (obj is GrouperTopLevelPair)
            {
                GrouperTopLevelPair that = obj as GrouperTopLevelPair;

                return this.FieldInfo.Equals(that.FieldInfo) && this.PathInfo.Equals(that.PathInfo);
            }
            return false;
        }
    }
}
