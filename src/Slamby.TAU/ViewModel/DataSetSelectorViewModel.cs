using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MaterialDesignThemes.Wpf;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class DataSetSelectorViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the DataSetSelectorViewModel class.
        /// </summary>
        public DataSetSelectorViewModel()
        {
            DoubleClickCommand = new RelayCommand(() =>
              {
                  DialogHost.CloseDialogCommand.Execute(true, null);
              });
        }


        private ObservableCollection<DataSet> _dataSets;

        public ObservableCollection<DataSet> DataSets
        {
            get { return _dataSets; }
            set { Set(() => DataSets, ref _dataSets, value); }
        }


        private DataSet _selectedDataSet;

        public DataSet SelectedDataSet
        {
            get { return _selectedDataSet; }
            set { Set(() => SelectedDataSet, ref _selectedDataSet, value); }
        }

        public RelayCommand DoubleClickCommand { get; private set; }

    }
}