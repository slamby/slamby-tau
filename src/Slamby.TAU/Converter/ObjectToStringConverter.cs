using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Newtonsoft.Json;

namespace Slamby.TAU.Converter
{
    public class ObjectToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return JsonConvert.DeserializeObject(value.ToString());
        }
    }
}
