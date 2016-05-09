using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MaterialDesignThemes.Wpf;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Design;
using Slamby.TAU.Enum;
using Slamby.TAU.Helper;
using Slamby.TAU.Model;
using System.Reflection;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Controls;
using Slamby.TAU.Logger;
using System.Windows.Input;
using System.Windows.Media;
using Dragablz;
using Dragablz.Dockablz;
using Slamby.TAU.Resources;
using GalaSoft.MvvmLight.Threading;
using Slamby.TAU.View;
using FontAwesome.WPF;
using Microsoft.Practices.ServiceLocation;
using Slamby.SDK.Net.Helpers;
using Slamby.SDK.Net.Models.Enums;
using MenuItem = Slamby.TAU.Model.MenuItem;
using Process = Slamby.SDK.Net.Models.Process;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IProcessManager processManager, DialogHandler dialogHandler)
        {
            IsEnable = false;
            Mouse.SetCursor(Cursors.Wait);
            _processManager = processManager;
            _dialogHandler = dialogHandler;
            if (Application.Current != null)
                Application.Current.DispatcherUnhandledException += DispatcherUnhandledException;


            if (Properties.Settings.Default.UpdateSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                Properties.Settings.Default.Save();
            }

            Messenger.Default.Register<StatusMessage>(this, sm => { Messages += $"{sm.Timestamp.ToString("yy.MM.dd. HH:mm:ss")} -- {sm.Message}{Environment.NewLine}"; });
            Messenger.Default.Register<UpdateMessage>(this, message =>
            {
                switch (message.UpdateType)
                {
                    case UpdateType.NewProcessCreated:
                        DispatcherHelper.CheckBeginInvokeOnUI(() => ActiveProcessesList.Add((Process)message.Parameter));
                        break;
                    case UpdateType.OpenNewTab:
                        Tabs.Add((HeaderedItemViewModel)message.Parameter);
                        break;
                }
            });
            Messenger.Default.Register<ClientResponse>(this, response => ErrorHandler(response));
            Messenger.Default.Register<Exception>(this, exception => ErrorHandler(exception));
            IsInSettingsMode = false;
            ChangeSettingsModeCommand = new RelayCommand(() => IsInSettingsMode = !IsInSettingsMode);
            RefreshCommand = new RelayCommand(() =>
            {
                Log.Info(LogMessages.MainRefreshCommand);
                Messenger.Default.Send(new UpdateMessage(UpdateType.EndPointUpdate));
            });
            AboutCommand = new RelayCommand(async () =>
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var sdkVersion = Assembly.LoadFrom("Slamby.SDK.Net.dll").GetName().Version;
                await _dialogHandler.Show(new CommonDialog { DataContext = new CommonDialogViewModel { Header = "About", Content = $"Tau version: {version.Major}.{version.Minor}.{version.Build}{Environment.NewLine}SDK version: {sdkVersion.Major}.{sdkVersion.Minor}.{sdkVersion.Build}", Buttons = ButtonsEnum.Ok } }, "RootDialog");
            });
            HelpCommand = new RelayCommand(() =>
            {
                System.Diagnostics.Process.Start("http://developers.slamby.com");
            });

            SizeChangedCommand = new RelayCommand(() =>
              {
                  if (LogWindowIsOpen)
                  {
                      LogWindowIsOpen = false;
                      LogWindowIsOpen = true;
                  }
              });
            DoubleClickCommand = new RelayCommand(() =>
            {
                object content = null;
                switch (SelectedMenuItem.Name.ToLower())
                {
                    case "datasets":
                        content = new ManageDataSet();
                        break;
                    case "services":
                        content = new ManageService();
                        break;
                    case "processes":
                        content = new ManageProcess();
                        break;
                }
                Messenger.Default.Send(new UpdateMessage(UpdateType.OpenNewTab,
                    new HeaderedItemViewModel(SelectedMenuItem.Name, content, true)));
            });
            RefreshProcessCommand = new RelayCommand<string>(async id =>
              {
                  var processResponse = await _processManager.GetProcessAsync(id);
                  if (ResponseValidator.Validate(processResponse))
                  {
                      var selectedItem = ActiveProcessesList.FirstOrDefault(p => p.Id == id);
                      if (selectedItem != null)
                      {
                          ActiveProcessesList[ActiveProcessesList.IndexOf(selectedItem)] = processResponse.ResponseObject;
                          ActiveProcessesList = new ObservableCollection<Process>(ActiveProcessesList);
                          if (processResponse.ResponseObject.Status != ProcessStatusEnum.InProgress)
                          {
                              Task.Run(async () =>
                              {
                                  await Task.Delay(20000);
                                  DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                  {
                                      var itemToRemove = ActiveProcessesList.FirstOrDefault(p => p.Id == id);
                                      if (itemToRemove != null)
                                          ActiveProcessesList.Remove(itemToRemove);
                                  });
                              });
                          }
                      }
                  }
              });

            CancelProcessCommand = new RelayCommand<Process>(async process =>
            {
                var processResponse = await _processManager.CancelProcessAsync(process.Id);
                if (ResponseValidator.Validate(processResponse))
                {
                    var selectedItem = ActiveProcessesList.FirstOrDefault(p => p.Id == process.Id);
                    if (selectedItem != null)
                    {
                        selectedItem.Status = ProcessStatusEnum.Cancelled;
                        ActiveProcessesList = new ObservableCollection<Process>(ActiveProcessesList);
                        Task.Run(async () =>
                        {
                            await Task.Delay(20000);
                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                var itemToRemove = ActiveProcessesList.FirstOrDefault(p => p.Id == process.Id);
                                if (itemToRemove != null)
                                    ActiveProcessesList.Remove(itemToRemove);
                            });
                        });
                    }
                }
            });

            SelectionChangedCommand = new RelayCommand(() =>
            {
                Mouse.SetCursor(Cursors.Wait);
            });
            InitData();
            Tabs = new ObservableCollection<HeaderedItemViewModel> { new HeaderedItemViewModel("DataSet", new ManageDataSet(), true) };
        }

        private void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(e.Exception));
            e.Handled = true;
        }

        public RelayCommand SelectionChangedCommand { get; private set; }
        public RelayCommand DoubleClickCommand { get; private set; }

        public RelayCommand<string> RefreshProcessCommand { get; private set; }
        public RelayCommand<Process> CancelProcessCommand { get; private set; }

        private bool _isEnable;

        public bool IsEnable
        {
            get { return _isEnable; }
            set { Set(() => IsEnable, ref _isEnable, value); }
        }

        private ObservableCollection<Process> _activeProcessesList = new ObservableCollection<Process>();

        public ObservableCollection<Process> ActiveProcessesList
        {
            get { return _activeProcessesList; }
            set { Set(() => ActiveProcessesList, ref _activeProcessesList, value); }
        }


        private async Task InitData()
        {
            try
            {
                var processResponse = await _processManager.GetProcessesAsync();
                if (ResponseValidator.Validate(processResponse))
                {
                    ActiveProcessesList =
                        new ObservableCollection<Process>(
                            processResponse.ResponseObject.Where(p => p.Status == ProcessStatusEnum.InProgress));
                }
                MenuItems = new ObservableCollection<MenuItem>();
                InitMenuItems();
                DataSets = ServiceLocator.Current.GetInstance<ManageDataSetViewModel>().DataSets;
            }
            catch (Exception exception)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
                IsEnable = true;
            }
        }

        private void InitMenuItems()
        {
            MenuItems.Clear();
            MenuItems.Add(new MenuItem
            {
                Name = "DataSets",
                Icon = ImageAwesome.CreateImageSource(FontAwesomeIcon.Database, Brushes.WhiteSmoke),
                Content = new ManageDataSet()
            });
            //MenuItems.Add(new MenuItem
            //{
            //    Name = "Data",
            //    Icon = ImageAwesome.CreateImageSource(FontAwesomeIcon.FilesOutline, Brushes.WhiteSmoke),
            //    Content = new ManageData()
            //});
            MenuItems.Add(new MenuItem
            {
                Name = "Services",
                Icon = ImageAwesome.CreateImageSource(FontAwesomeIcon.Tasks, Brushes.WhiteSmoke),
                Content = new ManageService()
            });
            MenuItems.Add(new MenuItem
            {
                Name = "Processes",
                Icon = ImageAwesome.CreateImageSource(FontAwesomeIcon.Spinner, Brushes.WhiteSmoke),
                Content = new ManageProcess()
            });
        }

        private async void ErrorHandler(object errorObject)
        {
            if (errorObject is ClientResponse || errorObject is Exception)
                _errors.Enqueue(errorObject);
            if (_errors.Count == 1)
            {
                while (_errors.Any())
                {
                    var context = errorObject is ClientResponse ? new ErrorViewModel((ClientResponse)_errors.First()) : new ErrorViewModel((Exception)_errors.First());
                    try
                    {
                        await _dialogHandler.Show(new ErrorDialog {DataContext = context}, "ErrorDialog");
                        object current;
                        while (!_errors.TryDequeue(out current)) ;
                    }
                    catch (Exception)
                    {
                        Log.Debug("Show error failed!");
                    }
                }
            }
        }

        private IProcessManager _processManager;
        private IDataSetManager _dataSetManager;
        private DialogHandler _dialogHandler;

        private static ConcurrentQueue<object> _errors = new ConcurrentQueue<object>();

        private ObservableCollection<MenuItem> _menuItems;

        public ObservableCollection<MenuItem> MenuItems
        {
            get { return _menuItems; }
            set { Set(() => MenuItems, ref _menuItems, value); }
        }


        private MenuItem _selectedMenuItem;

        public MenuItem SelectedMenuItem
        {
            get { return _selectedMenuItem; }
            set { Set(() => SelectedMenuItem, ref _selectedMenuItem, value); }
        }

        private ObservableCollection<DataSet> _dataSets;

        public ObservableCollection<DataSet> DataSets
        {
            get { return _dataSets; }
            set { Set(() => DataSets, ref _dataSets, value); }
        }

        private bool _isInSettingsMode;

        public bool IsInSettingsMode
        {
            get { return _isInSettingsMode; }
            set { Set(() => IsInSettingsMode, ref _isInSettingsMode, value); }
        }

        public RelayCommand ChangeSettingsModeCommand { get; private set; }

        public RelayCommand RefreshCommand { get; private set; }

        public RelayCommand AboutCommand { get; private set; }

        public RelayCommand HelpCommand { get; private set; }


        private bool _logWindowIsOpen;

        public bool LogWindowIsOpen
        {
            get { return _logWindowIsOpen; }
            set
            {
                if (Set(() => LogWindowIsOpen, ref _logWindowIsOpen, value))
                {
                    if (value)
                    {
                        RawMessagePublisher.Instance.AddSubscriber(_debugSubscriber);
                    }
                    else
                    {
                        RawMessagePublisher.Instance.RemoveSubscriber(_debugSubscriber);
                    }
                }
            }
        }

        private Logger.DebugSubscriber _debugSubscriber = new Logger.DebugSubscriber();

        private string _messages = "";

        public string Messages
        {
            get { return _messages; }
            set { Set(() => Messages, ref _messages, value); }
        }

        public RelayCommand SizeChangedCommand { get; private set; }


        private IInterTabClient _interTabClient = new InterTabClient();

        public IInterTabClient InterTabClient
        {
            get { return _interTabClient; }
            set { Set(() => InterTabClient, ref _interTabClient, value); }
        }

        public ItemActionCallback ClosingTabItemHandler => ClosingTabItemHandlerImpl;

        private static void ClosingTabItemHandlerImpl(ItemActionCallbackArgs<TabablzControl> args)
        {

            //here's your view model:
            var viewModel = args.DragablzItem.DataContext as HeaderedItemViewModel;
            Debug.Assert(viewModel != null);
        }

        public ClosingFloatingItemCallback ClosingFloatingItemHandler
        {
            get { return ClosingFloatingItemHandlerImpl; }
        }

        /// <summary>
        /// Callback to handle floating toolbar/MDI window closing.
        /// </summary>        
        private static void ClosingFloatingItemHandlerImpl(ItemActionCallbackArgs<Layout> args)
        {

            //here's your view model: 
            var disposable = args.DragablzItem.DataContext as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }

        public ObservableCollection<HeaderedItemViewModel> Tabs { get; set; }
    }
}