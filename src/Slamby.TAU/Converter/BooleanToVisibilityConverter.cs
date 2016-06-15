using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Slamby.TAU.Converter
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = (bool?)value;
            if (parameter?.ToString() == "invert")
            {
                boolValue = !boolValue.Value;
            }
            return boolValue.HasValue ? boolValue.Value ? Visibility.Visible : Visibility.Collapsed : Visibility.Collapsed;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null) && ((Visibility)value == Visibility.Visible);
        }
    }
}