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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataGridExtensions;

namespace Slamby.TAU.View
{
    /// <summary>
    /// Interaction logic for ManageData.xaml
    /// </summary>
    public partial class ManageData : UserControl
    {
        public ManageData()
        {
            InitializeComponent();
        }

        private void ContextButtons_OnClick(object sender, RoutedEventArgs e)
        {
            DocumentsContextMenu.IsOpen = false;
        }
    }
}
