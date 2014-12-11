using System.Windows;
using System.Windows.Controls;

namespace PanoramicData.view.table
{
    /// <summary>
    /// Interaction logic for SimpleDataGridDragFeedback.xaml
    /// </summary>
    public partial class SimpleDataGridDragFeedback : UserControl
    {
        public SimpleDataGridDragFeedback()
        {
            InitializeComponent();
            DataContextChanged += SimpleDataGridDragFeedback_DataContextChanged;
        }

        void SimpleDataGridDragFeedback_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is string)
            {
                content.Content = e.NewValue;
            }
        }
    }
}
