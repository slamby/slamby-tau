using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Slamby.TAU.Converter
{
    public class StringToCellTemplateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new TextBlock();
            var regexp = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var match = regexp.Matches(value.ToString());
            if (match.Count > 0)
            {
                Label linkLabel = new Label {FlowDirection = FlowDirection.LeftToRight};
                Run linkText = new Run(value.ToString());
                Hyperlink link = new Hyperlink(linkText) {FlowDirection = FlowDirection.LeftToRight};

                link.NavigateUri = new Uri(match[0].ToString());

                link.RequestNavigate += new RequestNavigateEventHandler(delegate (object sender, RequestNavigateEventArgs e) {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                    e.Handled = true;
                });

                linkLabel.Content = link;

                return linkLabel;
            }
            return new TextBlock { Text = value.ToString(), FlowDirection = FlowDirection.LeftToRight};
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
