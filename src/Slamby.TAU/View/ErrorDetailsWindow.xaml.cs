using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Slamby.TAU.View
{
    /// <summary>
    /// Interaction logic for ErrorDetailsWindow.xaml
    /// </summary>
    public partial class ErrorDetailsWindow : Window
    {
        public ErrorDetailsWindow(string error)
        {
            InitializeComponent();
            Error = error;
        }

        public string Error
        {
            get { return (string)GetValue(ErrorProperty); }
            set { SetValue(ErrorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Error.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorProperty =
            DependencyProperty.Register("Error", typeof(string), typeof(ErrorDetailsWindow), new PropertyMetadata(string.Empty));


    }
}
