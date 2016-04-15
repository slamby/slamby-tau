using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Slamby.TAU.Converter
{
    public class StringListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Join(", ", (List<string>)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value).Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        }
    }
}
