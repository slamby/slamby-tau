using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Models;
using Slamby.SDK.Net.Models.Enums;
using Slamby.TAU.Enum;
using Slamby.TAU.Helper;
using Slamby.TAU.Logger;
using Slamby.TAU.Model;
using Slamby.TAU.Resources;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ManageProcessViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the ManageProcessViewModel class.
        /// </summary>
        public ManageProcessViewModel(IProcessManager processManager)
        {
            _processManager = processManager;

            Messenger.Default.Register<UpdateMessage>(this, message =>
            {
                switch (message.UpdateType)
                {
                    case UpdateType.EndPointUpdate:
                        DispatcherHelper.CheckBeginInvokeOnUI(() => Processes.Clear());
                        _loadedFirst = true;
                        break;
                    case UpdateType.NewProcessCreated:
                        DispatcherHelper.CheckBeginInvokeOnUI(() => Processes.Add((Process)message.Parameter));
                        break;
                }
            });

            LoadedCommand = new RelayCommand(async () =>
            {
                Mouse.SetCursor(Cursors.Arrow);
                if (_loadedFirst && _processManager != null)
                {
                    _loadedFirst = false;
                    await DialogHandler.ShowProgress(null, async () =>
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() => Processes.Clear());
                        Log.Info(LogMessages.ManageProcessLoadProcesses);
                        var response = await _processManager.GetProcessesAsync(true);
                        if (ResponseValidator.Validate(response))
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                Processes = new ObservableCollection<Process>(response.ResponseObject);
                                RaisePropertyChanged("Processes");
                            });
                        }
                    });
                }
            });

            RefreshProcessCommand = new RelayCommand<string>(async id =>
            {
                var processResponse = await _processManager.GetProcessAsync(id);
                if (ResponseValidator.Validate(processResponse))
                {
                    var selectedItem = Processes.FirstOrDefault(p => p.Id == id);
                    if (selectedItem != null)
                    {
                        Processes[Processes.IndexOf(selectedItem)] = processResponse.ResponseObject;
                        Processes = new ObservableCollection<Process>(Processes);
                    }
                }
            });

            CancelProcessCommand = new RelayCommand<Process>(async process =>
            {
                var processResponse = await _processManager.CancelProcessAsync(process.Id);
                if (ResponseValidator.Validate(processResponse))
                {
                    var selectedItem = Processes.FirstOrDefault(p => p.Id == process.Id);
                    if (selectedItem != null)
                    {
                        selectedItem.Status = ProcessStatusEnum.Cancelled;
                        Processes = new ObservableCollection<Process>(Processes);
                    }
                }
            });

        }

        private bool _loadedFirst = true;
        private IProcessManager _processManager;

        private ObservableCollection<Process> _processes = new ObservableCollection<Process>();

        public ObservableCollection<Process> Processes
        {
            get { return _processes; }
            set { Set(() => Processes, ref _processes, value); }
        }

        public RelayCommand LoadedCommand { get; private set; }

        public RelayCommand<string> RefreshProcessCommand { get; private set; }

        public RelayCommand<Process> CancelProcessCommand { get; private set; }
    }
}