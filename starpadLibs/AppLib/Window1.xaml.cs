using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using StarPadSDK.Points;
using StarPadSDK.Utils;
using StarPadSDK.Stroq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Shapes;
using StarPadSDKTests.BobsCusps;
using Points;
using System.IO;
using MathExpr;

namespace AJournal {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window {
        public Window1() {
            InitializeComponent();
            this.Pages.SelectionChanged += new SelectionChangedEventHandler(Pages_SelectionChanged);
            createNewPage(0).Background = Brushes.White;
        }

        Page createNewPage(int where) {
            MenuItem delete = new MenuItem();
            delete.Header = "Delete";
            delete.Click += new RoutedEventHandler(delete_Click);

            TabItem ti = new TabItem();
            ti.ContextMenu = new ContextMenu();
            ti.ContextMenu.Items.Add(delete);
            delete.Tag = ti;

            Page gc = new Page();
            gc.Tag = ti;
            gc.Background = Brushes.White;
            gc.NameChangedEvent += new EventHandler(tabNameChanged);
            gc.HorizontalAlignment = HorizontalAlignment.Stretch;
            gc.VerticalAlignment = VerticalAlignment.Stretch;
            ti.Content = gc;
            ti.Header = gc.PageName;
            Pages.Items.Insert(where < 0 ? Pages.Items.Count + where: where, ti);
            Pages.SelectedItem = ti;

            return gc;
        }

        void delete_Click(object sender, RoutedEventArgs e) {
            TabItem ti = (TabItem)((MenuItem)sender).Tag;
            Pages.SelectedItem = Pages.Items[Pages.Items.Count - 1];
            Pages.Items.Remove(ti);
        }
        void Pages_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Pages.SelectedItem is TabItem) {
                object head = (Pages.SelectedItem as TabItem).Header;
                if (head as String == "+")
                    createNewPage(-1);
            }
        }

        void tabNameChanged(object sender, EventArgs e) {
            Page p = (Page)sender;
            TabItem ti = (TabItem)p.Tag;
            ti.Header = p.PageName;
        }
    }
}
