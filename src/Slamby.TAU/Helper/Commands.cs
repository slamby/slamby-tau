using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;

namespace Slamby.TAU.Helper
{
    public class Commands
    {
        public static RelayCommand<object> RemoveColumnCommand { get; } = new RelayCommand<object>(column =>
        {
            ;
        });
    }
}