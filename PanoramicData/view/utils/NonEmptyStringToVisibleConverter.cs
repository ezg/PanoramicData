using System.Windows;

namespace PanoramicData.view.utils
{
    [System.Windows.Data.ValueConversion(typeof(string), typeof(System.Windows.Visibility))]
    public class NonEmptyStringToVisibleConverter : System.Windows.Data.IValueConverter
    {
        public virtual object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value as string))
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }
        public virtual object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
