using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using InputFramework;
using InputFramework.WPFDevices;

namespace starPadSDK.AppLib {
    public partial class SelectionToolbar {
        InqScene _ican;

        void copy_Click(object sender, RoutedEventArgs e) {
            SelectionFeedback feedback = (sender as Button).Tag as SelectionFeedback; 
            Clipboard.SetDataObject(feedback.Selection);
        }
        void cut_Click (object sender, RoutedEventArgs e) {
            SelectionFeedback feedback = (sender as Button).Tag as SelectionFeedback;
            Clipboard.SetDataObject(feedback.Selection);

            _ican.UndoRedo.Add(new DeleteAction(feedback.Selection, _ican));
            _ican.SetSelection(feedback.Selection.Device,new SelectionObj());  // update the selection
        }
        void lookup_Click(object sender, RoutedEventArgs e)
        {
            SelectionFeedback feedback = (sender as Button).Tag as SelectionFeedback;
            Clipboard.SetDataObject(feedback.Selection);

            if (LookupEvent != null)
                LookupEvent(feedback, null);
            _ican.SetSelection(feedback.Selection.Device, new SelectionObj());  // update the selection
            e.Handled = true;
        }
        void lookup_Touch(object sender, RoutedPointEventArgs e)
        {
            lookup_Click(sender, e);
        }

        public static event EventHandler LookupEvent;

        public SelectionToolbar(InqScene ican, SelectionFeedback feedback) {
            _ican = ican;

            this.InitializeComponent();

            // An alternative to creating buttons w/ Expression Blend is to create them programmatically, as:
            Button cut = new Button();
            cut.Content = "Cut";
            cut.Tag = feedback;
            cut.Click += new RoutedEventHandler(cut_Click);
            toolbar.Items.Add(cut);

            // An alternative to creating buttons w/ Expression Blend is to create them programmatically, as:
            Button lookup = new Button();
            lookup.Content = "Lookup";
            lookup.Tag = feedback;
            lookup.Click += new RoutedEventHandler(lookup_Click);
            toolbar.Items.Add(lookup);
            lookup.AddHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(lookup_Touch));

            this.Copy.Tag = feedback;
        }
        public ToolBar ToolBar { get { return toolbar; } }
    }
}