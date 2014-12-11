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
using starPadSDK.Inq;
using starPadSDK.WPFHelp;
using PanoramicData.view.other;
using PanoramicData.model.view;
using PanoramicData.view.table;
using CombinedInputAPI;

namespace PanoramicData.view.filter
{
    /// <summary>
    /// Interaction logic for Colorer.xaml
    /// </summary>
    public partial class Colorer : UserControl, ColumnHeaderEventHandler
    {
        public static event EventHandler<DatabaseTableEventArgs> ColorerDropped;

        private Point _colorerStartDrag = new Point(0, 0);
        private Border _colorerShadow = null;

        public FilterModel FilterModel { get; set; }

        public Colorer()
        {
            InitializeComponent();
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchDownEvent));
        }

        public void Init()
        {
            if (FilterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count == 0 /*&&
                !FilterModel.TableModel.CalculateRecursivePathInfos().Any(pp => pp.Path.Count > 0)*/)
            {
                colorGridP1.Fill = Brushes.LightGray;
                colorGridP2.Fill = Brushes.LightGray;
                colorGridP3.Fill = Brushes.LightGray;
            }
            else
            {
                colorGridP1.Fill = FilterModel.Brush;
                colorGridP2.Fill = FilterModel.Brush;
                colorGridP3.Fill = FilterModel.Brush;
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
                    column.IsGrouped = true;
                    if (!descriptors.Contains(column))
                    {
                        descriptors.Add(column);
                    }
                }
            }
            return descriptors;
        } 

        private void colorGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if (FilterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count > 0)
            {
                InqScene inqScene = this.FindParent<InqScene>();

                e.TouchDevice.Capture(this);

                _colorerStartDrag = e.GetTouchPoint(inqScene).Position;

                this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchMoveEvent));
                this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchUpEvent));
            }
            e.Handled = true;
        }

        private void colorGrid_TouchUpEvent(Object sender, TouchEventArgs e)
        {
            var element = (FrameworkElement)sender;
            e.Handled = true;
            e.TouchDevice.Capture(null);

            element.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchMoveEvent));
            element.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchUpEvent));

            if (_colorerShadow != null)
            {
                InqScene inqScene = this.FindParent<InqScene>();

                if (ColorerDropped != null)
                {
                    Rct bounds = _colorerShadow.GetBounds(inqScene);
                    ColorerDropped(this, new DatabaseTableEventArgs(bounds,
                        FilterModel.TableModel, FilterModel, true));
                }

                inqScene.Rem(_colorerShadow);
                _colorerShadow = null;
            }
            else
            {
                InqScene inqScene = this.FindParent<InqScene>();
                Point fromInqScene = e.GetTouchPoint(inqScene).Position;

                RadialMenuCommand root = new RadialMenuCommand();
                root.IsSelectable = false;

                List<PanoramicDataColumnDescriptor> descriptors = new List<PanoramicDataColumnDescriptor>();
                //descriptors.AddRange(topLevelGroupings());

                // field level colorings
                descriptors.AddRange(FilterModel.GetColumnDescriptorsForOption(Option.ColorBy));

                foreach (var descriptor in descriptors)
                {
                    RadialMenuCommand rmc = new RadialMenuCommand();
                    rmc.Name = descriptor.GetSimpleLabel().Replace(" ", "\n");
                    rmc.Data = descriptor;
                    rmc.IsSelectable = true;
                    rmc.IsActive = FilterModel.GetColumnDescriptorsForOption(Option.ColorBy).Contains(descriptor);
                    rmc.ActiveTriggered = (cmd) =>
                    {
                        PanoramicDataColumnDescriptor columnDescriptor = cmd.Data as PanoramicDataColumnDescriptor;
                        togglecoloring(columnDescriptor);
                    };
                    root.AddSubCommand(rmc);
                }

                if (root.InnerCommands.Count > 0)
                {
                    RadialControl rc = new RadialControl(root,
                        new colorGridRadialControlExecution(FilterModel, FilterModel.TableModel, inqScene));
                    rc.SetPosition(fromInqScene.X - RadialControl.SIZE / 2,
                        fromInqScene.Y - RadialControl.SIZE / 2);
                    inqScene.AddNoUndo(rc);
                }
            }
        }

        private void colorGrid_TouchMoveEvent(Object sender, TouchEventArgs e)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            Point fromInqScene = e.GetTouchPoint(inqScene).Position;

            Vec v = _colorerStartDrag - fromInqScene;
            List<PathInfo> pathInfos = FilterModel.TableModel.CalculateRecursivePathInfos();
            if (v.Length > 10 && _colorerShadow == null)
            {
                ManipulationStart(fromInqScene);
            }
            ManipulationMove(fromInqScene);
        }

        public void ManipulationStart(Point fromInqScene)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            if (inqScene != null)
            {
                _colorerStartDrag = fromInqScene;
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
                    fromInqScene.X - _colorerShadow.Width / 2.0,
                    fromInqScene.Y - _colorerShadow.Height);
                inqScene.AddNoUndo(_colorerShadow);
            }
        }

        public void ManipulationMove(Point fromInqScene)
        {
            if (_colorerShadow != null)
            {
                InqScene inqScene = this.FindParent<InqScene>();
                _colorerStartDrag = fromInqScene;
                _colorerShadow.RenderTransform = new TranslateTransform(
                    fromInqScene.X - _colorerShadow.Width / 2.0,
                    fromInqScene.Y - _colorerShadow.Height);
                inqScene.AddNoUndo(_colorerShadow);
            }
        }

        private void togglecoloring(PanoramicDataColumnDescriptor columnDescriptor)
        {
            if (!FilterModel.GetColumnDescriptorsForOption(Option.ColorBy).Contains(columnDescriptor))
            {
                if (FilterModel.FilterRendererType != FilterRendererType.Pie)
                {
                    if (FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count > 0)
                    {
                        columnDescriptor.IsGrouped = true;
                    }
                    FilterModel.AddOptionColumnDescriptor(Option.ColorBy, columnDescriptor);
                }
                else
                {
                    //FilterModel.AddOptionColumnDescriptor(Option.ColorBy, columnDescriptor);
                    //var clone = (PanoramicDataColumnDescriptor) columnDescriptor.Clone();
                    //clone.IsGrouped = true;
                    //FilterModel.AddOptionColumnDescriptor(Option.GroupBy, clone);
                    //if (FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Count > 0)
                    {
                        columnDescriptor.IsGrouped = true;
                    }
                    FilterModel.AddOptionColumnDescriptor(Option.ColorBy, columnDescriptor);
                }
            }
            else
            {
                if (FilterModel.FilterRendererType != FilterRendererType.Pie)
                {
                    FilterModel.RemoveOptionColumnDescriptor(Option.ColorBy, columnDescriptor);
                }
                else
                {
                    //FilterModel.RemoveOptionColumnDescriptor(Option.ColorBy, columnDescriptor);
                    //var clones = FilterModel.GetColumnDescriptorsForOption(Option.GroupBy).Where(cd => cd.MatchSimple(columnDescriptor));
                    //foreach (var clone in clones)
                    //{
                    //    FilterModel.RemoveOptionColumnDescriptor(Option.GroupBy, clone);
                    //}
                    FilterModel.RemoveOptionColumnDescriptor(Option.ColorBy, columnDescriptor);
                }
            }
        }

        public void ColumnHeaderMoved(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e, bool overElement)
        {
            if (overElement)
            {
                //if (FilterModel.FilterRendererType != FilterRendererType.Table)
                {
                    colorGridRectangle.Visibility = Visibility.Visible;
                }
            }
            else
            {
                colorGridRectangle.Visibility = Visibility.Collapsed;
            }
        }

        public void ColumnHeaderDropped(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e)
        {
            var clone = (PanoramicDataColumnDescriptor)e.ColumnDescriptor.SimpleClone();
            togglecoloring(clone);
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
        private FilterModel _filterModel = null;
        private TableModel _tableModel = null;
        private InqScene _inqScene = null;

        public colorGridRadialControlExecution(FilterModel filterModel, TableModel tableModel, InqScene inqScene)
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
        }
    }

    public class ColorerTopLevelPair
    {
        public FieldInfo FieldInfo { get; set; }
        public PathInfo PathInfo { get; set; }

        public ColorerTopLevelPair(FieldInfo fi, PathInfo pi)
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
