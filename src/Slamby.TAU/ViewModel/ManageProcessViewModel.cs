using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Slamby.SDK.Net.Managers.Interfaces;
using Slamby.SDK.Net.Models;
using Slamby.SDK.Net.Models.Enums;
using Slamby.TAU.Enum;
using Slamby.TAU.Helper;
using Slamby.TAU.Logger;
using Slamby.TAU.Model;
using Slamby.TAU.Resources;
using Slamby.TAU.View;

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
        public ManageProcessViewModel(IProcessManager processManager, DialogHandler dialogHandler)
        {
            _processManager = processManager;
            _dialogHandler = dialogHandler;

            Messenger.Default.Register<UpdateMessage>(this, message =>
            {
                switch (message.UpdateType)
                {
                    case UpdateType.NewProcessCreated:
                        var processes = Processes.ToList();
                        var currentProcess = (Process)message.Parameter;
                        currentProcess.Start = currentProcess.Start.ToLocalTime();
                        currentProcess.End = currentProcess.End.ToLocalTime();
                        processes.Add(currentProcess);
                        DispatcherHelper.CheckBeginInvokeOnUI(() => Processes = new ObservableCollection<Process>(processes.OrderByDescending(p => p.Start)));
                        break;
                }
            });

            LoadedCommand = new RelayCommand(async () =>
            {
                try
                {
                    Mouse.SetCursor(Cursors.Arrow);
                    SetGridSettings();
                    if (_loadedFirst && _processManager != null)
                    {
                        _loadedFirst = false;
                        await _dialogHandler.ShowProgress(null, async () =>
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => Processes.Clear());
                            Log.Info(LogMessages.ManageProcessLoadProcesses);
                            var response = await _processManager.GetProcessesAsync(true);
                            if (ResponseValidator.Validate(response, false))
                            {
                                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                {
                                    if (response.ResponseObject.Any())
                                    {
                                        var ordered = response.ResponseObject.OrderByDescending(p => p.Start);
                                        Processes = new ObservableCollection<Process>(ordered.Select(p =>
                                        {
                                            p.Start = p.Start.ToLocalTime();
                                            p.End = p.End.ToLocalTime();
                                            return p;
                                        }));
                                        RaisePropertyChanged("Processes");
                                    }
                                });
                            }
                        });
                    }
                }
                catch (Exception exception)
                {
                    Messenger.Default.Send(exception);
                }

            });

            RefreshProcessCommand = new RelayCommand<string>(async id =>
            {
                try
                {
                    var processResponse = await _processManager.GetProcessAsync(id);
                    if (ResponseValidator.Validate(processResponse, false))
                    {
                        var selectedItem = Processes.FirstOrDefault(p => p.Id == id);
                        if (selectedItem != null)
                        {
                            Processes[Processes.IndexOf(selectedItem)] = processResponse.ResponseObject;
                            Processes = new ObservableCollection<Process>(Processes);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Messenger.Default.Send(exception);
                }

            });

            CancelProcessCommand = new RelayCommand<Process>(async process =>
            {
                try
                {
                    var processResponse = await _processManager.CancelProcessAsync(process.Id);
                    if (ResponseValidator.Validate(processResponse, false))
                    {
                        var selectedItem = Processes.FirstOrDefault(p => p.Id == process.Id);
                        if (selectedItem != null)
                        {
                            selectedItem.Status = ProcessStatusEnum.Cancelled;
                            Processes = new ObservableCollection<Process>(Processes);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Messenger.Default.Send(exception);
                }

            });

            ShowProcessDetailsCommand= new RelayCommand<Process>(async process =>
            {
                if(process==null) return;
                var context = new CommonDialogViewModel
                {
                    Header = "Process details",
                    Buttons = ButtonsEnum.Ok,
                    Content = new JContent(process)
                };
                var view = new CommonDialog { DataContext = context };
                await _dialogHandler.Show(view);
            });

        }

        private bool _loadedFirst = true;
        private IProcessManager _processManager;
        private DialogHandler _dialogHandler;

        private ObservableCollection<Process> _processes = new ObservableCollection<Process>();

        public ObservableCollection<Process> Processes
        {
            get { return _processes; }
            set { Set(() => Processes, ref _processes, value); }
        }

        public RelayCommand LoadedCommand { get; private set; }

        public RelayCommand<string> RefreshProcessCommand { get; private set; }

        public RelayCommand<Process> CancelProcessCommand { get; private set; }

        public RelayCommand<Process> ShowProcessDetailsCommand { get; private set; }

        private bool _processesGridSettingsLadedFromFile = false;
        private DataGridSettings _processesGridSettings;

        public DataGridSettings ProcessesGridSettings
        {
            get { return _processesGridSettings; }
            set
            {
                if (Set(() => ProcessesGridSettings, ref _processesGridSettings, value))
                {
                    if (value != null && !_processesGridSettingsLadedFromFile)
                    {
                        GlobalStore.SaveGridSettings("ManageProcess_Processes", "all", value);
                    }
                    _processesGridSettingsLadedFromFile = false;
                }
            }
        }

        private void SetGridSettings()
        {
            var gridSettingsDict = GlobalStore.GridSettingsDictionary;
            if (gridSettingsDict != null && gridSettingsDict.Any())
            {
                if (gridSettingsDict.ContainsKey("ManageProcess_Processes"))
                {
                    var tagsSettings = gridSettingsDict["ManageProcess_Processes"];
                    if (tagsSettings.ContainsKey("all"))
                    {
                        _processesGridSettingsLadedFromFile = true;
                        ProcessesGridSettings = tagsSettings["all"];
                    }
                    else
                    {
                        ProcessesGridSettings = null;
                    }
                }
                else
                {
                    ProcessesGridSettings = null;
                }
            }
            else
            {
                ProcessesGridSettings = null;
            }
        }
    }
}