using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Managers.Interfaces;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Helper;
using Slamby.TAU.Logger;
using Slamby.TAU.Model;
using Slamby.TAU.View;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ResourcesMonitorViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the ResourcesMonitorViewModel class.
        /// </summary>
        public ResourcesMonitorViewModel(IStatusManager statusManager, DialogHandler dialogHandler)
        {
            _dialogHandler = dialogHandler;
            _statusManager = statusManager;

            Messenger.Default.Register<ClientResponse>(this, response => DispatcherHelper.CheckBeginInvokeOnUI(() => ErrorHandler(response)));
            Messenger.Default.Register<Exception>(this, exception => DispatcherHelper.CheckBeginInvokeOnUI(() => ErrorHandler(exception)));
            ApplyIntervalSettingsCommand = new RelayCommand(() => _timer.Change(500, RefreshInterval * 1000));
            ErrorCountResetCommand = new RelayCommand(() => ErrorCount = 0);
            _timer = new System.Threading.Timer(par =>
            {
                Task.Run(async () =>
                {
                    var statusResponse = await _statusManager.GetStatusAsync();
                    if (statusResponse.IsSuccessFul)
                    {
                        GlobalStore.EndPointIsAlive = true;
                        EndPointStatus.IsAlive = true;
                        EndPointStatus.Status = statusResponse.ResponseObject;
                    }
                    else
                    {
                        GlobalStore.EndPointIsAlive = false;
                        EndPointStatus.IsAlive = false;
                        EndPointStatus.Status = null;
                    }
                });
            }, null, 500, RefreshInterval * 1000);

            ShowErrorDetailsCommand = new RelayCommand<Error>((error) =>
            {
                if (error == null) return;
                var edw = new ErrorDetailsWindow(error.Message + Environment.NewLine + error.Details);
                edw.Show();
            });

        }

        private IStatusManager _statusManager;

        private System.Threading.Timer _timer;

        private int _refreshInterval = 20;

        public int RefreshInterval
        {
            get { return _refreshInterval; }
            set { Set(() => RefreshInterval, ref _refreshInterval, value); }
        }

        private EndPointStatus _endPointStatus = new EndPointStatus();

        public EndPointStatus EndPointStatus
        {
            get { return _endPointStatus; }
            set { Set(() => EndPointStatus, ref _endPointStatus, value); }
        }

        public RelayCommand ApplyIntervalSettingsCommand { get; private set; }

        public RelayCommand<Error> ShowErrorDetailsCommand { get; private set; }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private ObservableCollection<Error> _errors = new ObservableCollection<Error>();

        public ObservableCollection<Error> Errors
        {
            get { return _errors; }
            set { Set(() => Errors, ref _errors, value); }
        }

        private int _errorCount = 0;

        public int ErrorCount
        {
            get { return _errorCount; }
            set { Set(() => ErrorCount, ref _errorCount, value); }
        }

        public RelayCommand ErrorCountResetCommand { get; private set; }

        private DialogHandler _dialogHandler;

        private void ErrorHandler(object errorObject)
        {
            if (errorObject is ClientResponse || errorObject is Exception)
            {
                Errors.Add(Error.Convert(errorObject));
                ErrorCount++;
            }


            //if (Errors.Count == 1)
            //{
            //    while (Errors.Any())
            //    {
            //        var context = errorObject is ClientResponse ? new ErrorViewModel((ClientResponse)Errors.First()) : new ErrorViewModel((Exception)Errors.First());
            //        try
            //        {
            //            await _dialogHandler.Show(new ErrorDialog { DataContext = context }, "ErrorDialog");
            //            object current;
            //            while (!_errors.TryDequeue(out current)) ;

            //        }
            //        catch (Exception)
            //        {
            //            Log.Debug("Show error failed!");
            //        }
            //    }
            //}
        }
    }
}