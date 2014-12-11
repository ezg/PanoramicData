using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;

namespace starPadSDK.SurfaceLib
{
    /// <summary>
    /// Interaction logic for PalmPrintCustomize.xaml
    /// </summary>
    public partial class PalmPrintCustomize : SurfaceUserControl
    {
        public PalmPrintCustomize()
        {
            InitializeComponent();

            _CustomizeToolbox = new PalmCustomize();
            _CustomizeToolbox.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Add(_CustomizeToolbox);
            _CustomizeToolbox.RenderTransform = new TranslateTransform(260, -250);

            Contacts.AddPreviewContactUpHandler(btnCustomize, btnCustomize_ContactUp);
        }


        protected PalmCustomize _CustomizeToolbox = null;

        private void btnCustomize_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btnCustomize_ContactUp(object sender, ContactEventArgs e)
        {
            _CustomizeToolbox.Show();
        }

        public void Dismiss()
        {
            _CustomizeToolbox.Hide();
        }
    }

}
