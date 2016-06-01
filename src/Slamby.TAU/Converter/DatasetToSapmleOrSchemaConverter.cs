using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Newtonsoft.Json;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.Converter
{
    public class DatasetToSapmleOrSchemaConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dataSet = value as DataSet;
            if (value == null) return "";
            return dataSet.SampleDocument!=null ? JsonConvert.SerializeObject(dataSet.SampleDocument, Formatting.Indented):
                JsonConvert.SerializeObject(dataSet.Schema, Formatting.Indented);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
