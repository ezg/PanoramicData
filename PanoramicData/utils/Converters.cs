using PixelLab.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PanoramicData.utils
{
    public class StringToVisibilityConverter : SimpleValueConverter<string, Visibility>
    {
        protected override Visibility ConvertBase(string input)
        {
            return (input == null || input == "") ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
