using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Slamby.TAU.Logger;
using Slamby.TAU.Resources;
using System.Threading;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class StatusDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the StatusDialogViewModel class.
        /// </summary>
        public StatusDialogViewModel()
        {
            CancelCommand = new RelayCommand(() =>
              {
                  Log.Info(string.Format(LogMessages.StatusDialogCancel, Title));
                  CancellationTokenSource.Cancel();
              });
        }


        private double _progressValue;

        public double ProgressValue
        {
            get { return _progressValue; }
            set { Set(() => ProgressValue, ref _progressValue, value); }
        }


        private string _title;

        public string Title
        {
            get { return _title; }
            set { Set(() => Title, ref _title, value); }
        }


        private int _errorCount;

        public int ErrorCount
        {
            get { return _errorCount; }
            set
            {
                if (Set(() => ErrorCount, ref _errorCount, value))
                {
                    HasError = ErrorCount > 0;
                }
            }
        }


        private int _doneCount;

        public int DoneCount
        {
            get { return _doneCount; }
            set { Set(() => DoneCount, ref _doneCount, value); }
        }

        private bool _hasError;

        public bool HasError
        {
            get { return _hasError; }
            set { Set(() => HasError, ref _hasError, value); }
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public RelayCommand CancelCommand { get; private set; }
    }
}