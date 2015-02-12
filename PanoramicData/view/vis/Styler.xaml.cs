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
using PanoramicData.model.view;
using PanoramicData.view.other;
using CombinedInputAPI;
using PanoramicData.model.data;
using PanoramicData.model.view;
using PanoramicData.view.inq;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for Styler.xaml
    /// </summary>
    public partial class Styler : UserControl
    {
        public Styler()
        {
            InitializeComponent();
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(colorGrid_TouchDownEvent));
        }

        private void colorGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
            InkableScene inkableScene = this.FindParent<InkableScene>();
            Point fromInkableScene = e.GetTouchPoint(inkableScene).Position;

            RadialMenuCommand root = new RadialMenuCommand();
            root.IsSelectable = false;

            RadialMenuCommandGroup group = new RadialMenuCommandGroup("group", RadialMenuCommandComandGroupPolicy.DeactivateAndTriggerOthers);

            RadialMenuCommand styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Line";
            styleCmd.CommandGroup = group;
            styleCmd.Data = queryModel;
            styleCmd.IsSelectable = true;
            styleCmd.IsActive = queryModel.VisualizationType == VisualizationType.Line;
            styleCmd.ActiveTriggered = (cmd) =>
            {
                if (cmd.IsActive)
                {
                    QueryModel qm = cmd.Data as QueryModel;
                    qm.VisualizationType = VisualizationType.Line;
                }
                cmd.IsActive = queryModel.VisualizationType == VisualizationType.Line;
            };
            root.AddSubCommand(styleCmd);

            styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Bar";
            styleCmd.CommandGroup = group;
            styleCmd.Data = queryModel;
            styleCmd.IsSelectable = true;
            styleCmd.IsActive = queryModel.VisualizationType == VisualizationType.Bar;
            styleCmd.ActiveTriggered = (cmd) =>
            {
                if (cmd.IsActive)
                {
                    QueryModel qm = cmd.Data as QueryModel;
                    qm.VisualizationType = VisualizationType.Bar;
                }
                cmd.IsActive = queryModel.VisualizationType == VisualizationType.Bar;
            };
            root.AddSubCommand(styleCmd);

            styleCmd = new RadialMenuCommand();
            styleCmd.Name = "Point";
            styleCmd.CommandGroup = group;
            styleCmd.Data = queryModel;
            styleCmd.IsSelectable = true;
            styleCmd.IsActive = queryModel.VisualizationType == VisualizationType.Plot;
            styleCmd.ActiveTriggered = (cmd) =>
            {
                if (cmd.IsActive)
                {
                    QueryModel qm = cmd.Data as QueryModel;
                    qm.VisualizationType = VisualizationType.Plot;
                }
                cmd.IsActive = queryModel.VisualizationType == VisualizationType.Plot;
            };
            root.AddSubCommand(styleCmd);

            if (root.InnerCommands.Count > 0)
            {
                RadialControl rc = new RadialControl(root,
                    new StylerRadialControlExecution(inkableScene));
                rc.SetPosition(fromInkableScene.X - RadialControl.SIZE / 2,
                    fromInkableScene.Y - RadialControl.SIZE / 2);
                inkableScene.Add(rc);
            }
            e.Handled = true;
        }
    }

    public class StylerRadialControlExecution : RadialControlExecution
    {
        private InkableScene _inkableScene = null;

        public StylerRadialControlExecution(InkableScene inkableScene)
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
