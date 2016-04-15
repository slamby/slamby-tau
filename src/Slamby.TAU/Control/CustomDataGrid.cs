using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Slamby.TAU.Control
{
    public class CustomDataGrid : DataGrid
    {
        public CustomDataGrid()
        {
            this.SelectionChanged += CustomDataGrid_SelectionChanged;
        }

        private void OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        void CustomDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.SelectedItems.Count > 0)
                this.SelectedItemsList = new ObservableCollection<object>(this.SelectedItems.Cast<object>());
            else
            {
                this.SelectedItemsList?.Clear();
            }
        }

        public ObservableCollection<object> SelectedItemsList
        {
            get { return (ObservableCollection<object>)GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItemsList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register("SelectedItemsList", typeof(ObservableCollection<object>), typeof(CustomDataGrid), new PropertyMetadata(new ObservableCollection<object>(), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var dataGridrid = sender as CustomDataGrid;
            if (dataGridrid != null && e.NewValue != null)
            {
                var newValue = ((ObservableCollection<object>)e.NewValue).ToList();
                newValue.ForEach(item => dataGridrid.SelectedItems.Add(item));
            }
        }

        private bool _isindexed;

        public bool IsIndexed
        {
            get { return _isindexed; }
            set
            {
                _isindexed = value;
                if (value)
                {
                    this.LoadingRow += OnLoadingRow;
                }
                else
                {
                    this.LoadingRow -= OnLoadingRow;
                }
            }
        }
    }
}
