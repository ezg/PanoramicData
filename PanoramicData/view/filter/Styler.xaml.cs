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
using starPadSDK.Inq;
using PanoramicData.model.view;
using PanoramicData.view.other;
using CombinedInputAPI;

namespace PanoramicData.view.filter
{
    /// <summary>
    /// Interaction logic for Styler.xaml
    /// </summary>
    public partial class Styler : UserControl
    {
        public FilterModel FilterModel { get; set; }

        public Styler()
        {
            InitializeComponent();
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchDownEvent));
        }

        public void Init()
        {
            /*if (FilterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count == 0)
            {
                stylerGridP1.Fill = Brushes.LightGray;
            }
            else
            {
                stylerGridP1.Fill = FilterModel.Brush;
            }*/
        }

        private void colorGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            Point fromInqScene = e.GetTouchPoint(inqScene).Position;

            RadialMenuCommand root = new RadialMenuCommand();
            root.IsSelectable = false;

            RadialMenuCommandGroup group = new RadialMenuCommandGroup("group", RadialMenuCommandComandGroupPolicy.DeactivateAndTriggerOthers);

            RadialMenuCommand styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Pie";
            styleCmd.CommandGroup = group;
            styleCmd.Data = FilterModel;
            styleCmd.IsSelectable = true;
            styleCmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Pie;
            styleCmd.ActiveTriggered = (cmd) =>
            {
                if (cmd.IsActive)
                {
                    FilterModel fm = cmd.Data as FilterModel;
                    fm.FilterRendererType = FilterRendererType.Pie;
                }
                cmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Pie;
            };
            root.AddSubCommand(styleCmd);

            styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Line";
            styleCmd.CommandGroup = group;
            styleCmd.Data = FilterModel;
            styleCmd.IsSelectable = true;
            styleCmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Line;
            styleCmd.ActiveTriggered = (cmd) =>
            {
                if (cmd.IsActive)
                {
                    FilterModel fm = cmd.Data as FilterModel;
                    fm.FilterRendererType = FilterRendererType.Line;
                }
                cmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Line;
            };
            root.AddSubCommand(styleCmd);

            styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Bar";
            styleCmd.CommandGroup = group;
            styleCmd.Data = FilterModel;
            styleCmd.IsSelectable = true;
            styleCmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Histogram;
            styleCmd.ActiveTriggered = (cmd) =>
            {
                if (cmd.IsActive)
                {
                    FilterModel fm = cmd.Data as FilterModel;
                    fm.FilterRendererType = FilterRendererType.Histogram;
                }
                cmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Histogram;
            };
            root.AddSubCommand(styleCmd);

            styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Point";
            styleCmd.CommandGroup = group;
            styleCmd.Data = FilterModel;
            styleCmd.IsSelectable = true;
            styleCmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Plot;
            styleCmd.ActiveTriggered = (cmd) =>
            {
                if (cmd.IsActive)
                {
                    FilterModel fm = cmd.Data as FilterModel;
                    fm.FilterRendererType = FilterRendererType.Plot;
                }
                cmd.IsActive = FilterModel.FilterRendererType == FilterRendererType.Plot;
            };
            root.AddSubCommand(styleCmd);

            if (root.InnerCommands.Count > 0)
            {
                RadialControl rc = new RadialControl(root,
                    new StylerRadialControlExecution(FilterModel, FilterModel.TableModel, inqScene));
                rc.SetPosition(fromInqScene.X - RadialControl.SIZE / 2,
                    fromInqScene.Y - RadialControl.SIZE / 2);
                inqScene.AddNoUndo(rc);
            }
            e.Handled = true;
        }
    }

    public class StylerRadialControlExecution : RadialControlExecution
    {
        private FilterModel _filterModel = null;
        private TableModel _tableModel = null;
        private InqScene _inqScene = null;

        public StylerRadialControlExecution(FilterModel filterModel, TableModel tableModel, InqScene inqScene)
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
}
