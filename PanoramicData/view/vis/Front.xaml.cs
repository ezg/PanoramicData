using System.Windows;
using System.Windows.Controls;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for Front.xaml
    /// </summary>
    public partial class Front : UserControl
    {
        public Front()
        {
            InitializeComponent();
        }

        public void SetContent(FrameworkElement content)
        {
            contentGrid.Children.Clear();
            contentGrid.Children.Add(content);
        }
    }
}
