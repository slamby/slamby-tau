using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight.Threading;

namespace Slamby.TAU.Control
{
    public class CustomListView : ListView
    {
        public CustomListView()
        {
            this.SelectionChanged += CustomListView_SelectionChanged;
            this.Loaded += CustomListView_Loaded;
        }

        void CustomListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.SelectedItems.Count > 0)
                this.SelectedItemsList = new ObservableCollection<object>(this.SelectedItems.Cast<object>());
            else
            {
                this.SelectedItemsList = new ObservableCollection<object>();
            }
        }

        private async void CustomListView_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                Mouse.SetCursor(Cursors.Wait);
                if (SelectedItemsList != null)
                {
                    foreach (var item in SelectedItemsList)
                    {
                        this.SelectedItems.Add(item);
                    }
                }
                Mouse.SetCursor(Cursors.Arrow);
            }));
        }

        public ObservableCollection<object> SelectedItemsList
        {
            get { return (ObservableCollection<object>)GetValue(SelectedItemsListProperty); }
            set
            {
                SetValue(SelectedItemsListProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for SelectedItemsList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register("SelectedItemsList", typeof(ObservableCollection<object>), typeof(CustomListView), new PropertyMetadata(new ObservableCollection<object>()));

    }
}