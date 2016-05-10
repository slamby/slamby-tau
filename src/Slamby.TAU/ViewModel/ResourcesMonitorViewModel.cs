using System.Threading.Tasks;
using System.Timers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Helper;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ResourcesMonitorViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the ResourcesMonitorViewModel class.
        /// </summary>
        public ResourcesMonitorViewModel(StatusManager statusManager)
        {
            _statusManager = statusManager;
            ApplyIntervalSettingsCommand = new RelayCommand(() => _timer.Interval = RefreshInterval * 1000);
            _timer.Elapsed += (sender, e) =>
            {
                Task.Run(async () =>
                {
                    var statusResponse = await _statusManager.GetStatusAsync();
                    if (ResponseValidator.Validate(statusResponse))
                    {
                        Status = statusResponse.ResponseObject;
                    }
                });
            };
            _timer.Start();

        }

        private StatusManager _statusManager;

        private Timer _timer = new Timer { AutoReset = true, Interval = 20000, Enabled = true };

        private Status _status = new Status();


        public Status Status
        {
            get { return _status; }
            set { Set(() => Status, ref _status, value); }
        }


        private int _refreshInterval = 20;

        public int RefreshInterval
        {
            get { return _refreshInterval; }
            set { Set(() => RefreshInterval, ref _refreshInterval, value); }
        }

        public RelayCommand ApplyIntervalSettingsCommand { get; private set; }
    }
}