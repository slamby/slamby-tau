using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Slamby.TAU.Converter
{
    public class ErrorCountToBackgrounColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int)) return new SolidColorBrush(Colors.Transparent);
            if (((int)value) > 0) return new SolidColorBrush(Colors.Red);
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}