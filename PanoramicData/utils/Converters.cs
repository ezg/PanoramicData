using PanoramicData.model.data;
using PanoramicData.model.view_new;
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

    public class BooleanToVisibilityConverter : SimpleValueConverter<bool, Visibility>
    {
        protected override Visibility ConvertBase(bool input)
        {
            return input ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class IsTableVisualizationTypeConverter : SimpleValueConverter<VisualizationType, bool>
    {
        protected override bool ConvertBase(VisualizationType input)
        {
            return input == VisualizationType.Table;
        }
    }
    
    public class InverseBooleanToVisibilityConverter : SimpleValueConverter<bool, Visibility>
    {
        protected override Visibility ConvertBase(bool input)
        {
            return input ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public class NullToVisibilityConverter : SimpleValueConverter<object, Visibility>
    {
        protected override Visibility ConvertBase(object input)
        {
            return input == null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public class SortModeAscToVisibilityConvertor : SimpleValueConverter<SortMode, Visibility>
    {
        protected override Visibility ConvertBase(SortMode input)
        {
            return input == SortMode.Asc ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class SortModeDescToVisibilityConvertor : SimpleValueConverter<SortMode, Visibility>
    {
        protected override Visibility ConvertBase(SortMode input)
        {
            return input == SortMode.Desc ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
