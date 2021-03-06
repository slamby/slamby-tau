﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Practices.ServiceLocation;
using Slamby.TAU.Helper;
using Slamby.TAU.Model;
using Cursors = System.Windows.Input.Cursors;
using DataGrid = System.Windows.Controls.DataGrid;

namespace Slamby.TAU.Control
{
    public class CustomDataGrid : DataGrid
    {

        public CustomDataGrid()
        {
            this.LoadingRow += OnLoadingRow;
            this.EnableRowVirtualization = true;
            this.SelectionChanged += CustomDataGrid_SelectionChanged;
            this.Loaded += CustomDataGrid_Loaded;
            this.AutoGeneratedColumns += CustomDataGrid_AutoGeneratedColumns;
            this.Sorting += CustomDataGrid_Sorting;
        }

        private void CustomDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.Header is string)
            {
                var oldSettings = GridSettings == null ? new DataGridSettings() : (DataGridSettings)GridSettings;
                var sortDesc = this.Items.SortDescriptions;
                SortDescription oldSortDescForProperty;
                if (sortDesc.Any(sd => sd.PropertyName == e.Column.Header.ToString()))
                {
                    oldSortDescForProperty = sortDesc.First(sd => sd.PropertyName == e.Column.Header.ToString());
                    oldSortDescForProperty = new SortDescription
                    {
                        Direction = oldSortDescForProperty.Direction == ListSortDirection.Ascending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending,
                        PropertyName = e.Column.Header.ToString()
                    };
                }
                else
                {
                    oldSortDescForProperty = new SortDescription
                    {
                        Direction = ListSortDirection.Ascending,
                        PropertyName = e.Column.Header.ToString()
                    };
                }
                oldSettings.SortDescription = oldSortDescForProperty;
                GridSettings = null;
                GridSettings = oldSettings;
            }
        }

        private void CustomDataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            Columns[Columns.Count - 1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }

        private async void CustomDataGrid_Loaded(object sender, RoutedEventArgs e)
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
            if (GridSettings?.SortDescription != null)
            {
                this.Items?.SortDescriptions.Add(GridSettings.SortDescription.Value);
            }
        }

        private void OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (IsIndexed)
                e.Row.Header = e.Row.GetIndex() + 1;
            if (_isFirstRow)
            {
                if (GridSettings?.Columns != null)
                {
                    //Column setting
                    var isColumnMissmatch = GridSettings.Columns.Any(c => !Columns.Where(col => col.Header != null && col.Header is string).Select(col => col.Header.ToString()).Contains(c));
                    if (GridSettings.Columns != null && !isColumnMissmatch)
                    {
                        var i = 0;
                        GridSettings.Columns.ForEach(c =>
                        {
                            var currentCol = Columns.Where(col => col.Header != null && col.Header is string).FirstOrDefault(col => col.Header.ToString() == c);
                            currentCol.DisplayIndex = i++;
                        });
                    }
                    else
                    {
                        var oldSettings = (DataGridSettings)GridSettings;
                        oldSettings.Columns = this.Columns.ToList().Select(c => c.Header.ToString()).ToList();
                        GridSettings = oldSettings;
                    }
                }
                _isFirstRow = false;
            }
        }

        void CustomDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.SelectedItems.Count > 0)
                this.SelectedItemsList = new ObservableCollection<object>(this.SelectedItems.Cast<object>());
            else
            {
                this.SelectedItemsList = new ObservableCollection<object>();
            }
        }

        protected override void OnColumnReordered(DataGridColumnEventArgs e)
        {
            base.OnColumnReordered(e);
            var cols = new Dictionary<string, int>();
            this.Columns.Where(c => c.Header != null && c.Header is string).ToList().ForEach(c => cols.Add(c.Header.ToString(), c.DisplayIndex));
            var oldSettings = GridSettings == null ? new DataGridSettings() : (DataGridSettings)GridSettings;
            oldSettings.Columns = Columns.Where(c => c.Header != null && c.Header is string).ToList().OrderBy(c => c.DisplayIndex).Select(c => c.Header.ToString()).ToList();
            GridSettings = null;
            GridSettings = oldSettings;
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
            DependencyProperty.Register("SelectedItemsList", typeof(ObservableCollection<object>), typeof(CustomDataGrid), new PropertyMetadata(new ObservableCollection<object>()));


        public DataGridSettings GridSettings
        {
            get { return (DataGridSettings)GetValue(GridSettingsProperty); }
            set
            {
                SetValue(GridSettingsProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for GridSettings.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GridSettingsProperty =
            DependencyProperty.Register("GridSettings", typeof(DataGridSettings), typeof(CustomDataGrid), new PropertyMetadata());


        private bool _isFirstRow = true;

        private bool _isindexed;

        public bool IsIndexed
        {
            get { return _isindexed; }
            set
            {
                _isindexed = value;
            }
        }
    }
}
