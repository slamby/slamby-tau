using Slamby.SDK.Net.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Slamby.SDK.Net.Models.Services;

namespace Slamby.TAU.Converter
{
    public class IListToObservableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return value;
            if(((IList) value).Count == 0) return Activator.CreateInstance(targetType);
            if (value.GetType() == typeof(ObservableCollection<Tag>))
                return new ObservableCollection<object>((ObservableCollection<Tag>)value);
            if (value.GetType() == typeof(ObservableCollection<Service>))
                return new ObservableCollection<object>((ObservableCollection<Service>)value);
            return new ObservableCollection<object>((ObservableCollection<object>)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return value;
            if (((IList)value).Count == 0) return Activator.CreateInstance(targetType);
            if (((IList)value)[0].GetType() == typeof(Tag))
                return new ObservableCollection<Tag>(((IList)value).Cast<Tag>());
            if (((IList)value)[0].GetType() == typeof(Service) || ((IList)value)[0].GetType().BaseType == typeof(Service))
                return new ObservableCollection<Service>(((IList)value).Cast<Service>());
            return new ObservableCollection<object>(((IList)value).Cast<object>());
        }
    }
}
