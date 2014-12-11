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
using PanoramicData.model.view;
using CombinedInputAPI;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for PivotFilterItemRenderer.xaml
    /// </summary>
    public partial class PivotFilterItemRenderer : UserControl
    {
        public bool IsAnySelected
        {
            get { return (bool)GetValue(IsAnySelectedProperty); }
            set { SetValue(IsAnySelectedProperty, value); }
        }

        public static readonly DependencyProperty IsAnySelectedProperty =
            DependencyProperty.Register("IsAnySelected", typeof(bool), typeof(PivotFilterItemRenderer), new UIPropertyMetadata(false));
        

        public PivotFilterItemRenderer()
        {
            InitializeComponent();
            this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(pointDownEvent));
        }

        private void pointDownEvent(object sender, TouchEventArgs e)
        {
            e.Handled = true;
            (this.DataContext as Pivot).Selected = !(this.DataContext as Pivot).Selected;
        }
    }
}
