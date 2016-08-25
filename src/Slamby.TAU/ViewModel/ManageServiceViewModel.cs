using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json.Linq;
using Slamby.SDK.Net.Managers.Interfaces;
using Slamby.SDK.Net.Models;
using Slamby.SDK.Net.Models.Enums;
using Slamby.SDK.Net.Models.Services;
using Slamby.TAU.Design;
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
    public class ManageServiceViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the ManageServiceViewModel class.
        /// </summary>
        public ManageServiceViewModel(IServiceManager serviceManager, IClassifierServiceManager classifierServiceManager, IPrcServiceManager prcServiceManager, IProcessManager processManager, DialogHandler dialogHandler)
        {
            Services = new ObservableCollection<ExtendedService>();
            _serviceManager = serviceManager;
            _classifierServiceManager = classifierServiceManager;
            _prcServiceManager = prcServiceManager;
            _processManager = processManager;
            _dialogHandler = dialogHandler;

            ModifyAliasCommand = new RelayCommand(async () =>
             {
                 if (SelectedServices == null || !SelectedServices.Any()) return;

                 var serviceToModify = SelectedServices.First();
                 var context = new CommonDialogViewModel
                 {
                     Buttons = ButtonsEnum.OkCancel,
                     Header = "Modify Alias",
                     Content = new JContent(serviceToModify.Alias)
                 };
                 var view = new CommonDialog { DataContext = context };
                 var canClose = false;
                 var result = await _dialogHandler.Show(view, "RootDialog",
                     async (object sender, DialogClosingEventArgs args) =>
                     {
                         if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                         {
                             args.Cancel();
                             args.Session.UpdateContent(new ProgressDialog());
                             var isSuccessful = false;
                             var errorMessage = "";
                             try
                             {
                                 serviceToModify.Alias = ((JContent)context.Content).GetJToken().ToObject<string>();
                                 var response = await _serviceManager.UpdateServiceAsync(serviceToModify.Id, serviceToModify);
                                 isSuccessful = response.IsSuccessFul;
                                 ResponseValidator.Validate(response, false);
                             }
                             catch (Exception exception)
                             {
                                 isSuccessful = false;
                                 errorMessage = exception.Message;

                             }
                             finally
                             {
                                 if (!isSuccessful)
                                 {
                                     context.ErrorMessage = errorMessage;
                                     context.ShowError = true;
                                     args.Session.UpdateContent(view);
                                 }
                                 else
                                 {
                                     canClose = true;
                                     args.Session.Close((CommonDialogResult)args.Parameter);
                                 }
                             }
                         }
                     });
                 if ((CommonDialogResult)result == CommonDialogResult.Ok)
                 {
                     var aliasMatches = Services.Where(s => s.Id != serviceToModify.Id && s.Alias == serviceToModify.Alias);
                     if (aliasMatches.Any())
                     {
                         foreach (var aliasMatch in aliasMatches)
                         {
                             aliasMatch.Alias = "";
                         }
                     }
                 }

             });

            LoadedCommand = new RelayCommand(async () =>
                {
                    Mouse.SetCursor(Cursors.Arrow);
                    SetGridSettings();
                    if (_loadedFirst && _serviceManager != null)
                    {
                        _loadedFirst = false;
                        await _dialogHandler.ShowProgress(null, async () =>
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => Services.Clear());
                            Log.Info(LogMessages.ManageDataLoadTags);
                            var response = await _serviceManager.GetServicesAsync();
                            ResponseValidator.Validate(response, false);
                            var tempServices = new ObservableCollection<ExtendedService>(response.ResponseObject.Select(s => new ExtendedService(s)));
                            response.ResponseObject.Where(s => s.Status == ServiceStatusEnum.Busy).ToList().ForEach(s => _busyServiceIds.Add(s.Id));
                            var activePrcServices = response.ResponseObject.Where(s => s.Status == ServiceStatusEnum.Active && s.Type == ServiceTypeEnum.Prc).ToList();
                            foreach (var service in activePrcServices)
                            {
                                var prcResponse = await _prcServiceManager.GetServiceAsync(service.Id);
                                ResponseValidator.Validate(prcResponse, false);
                                if (prcResponse.ResponseObject.IndexSettings != null)
                                {
                                    tempServices.First(s => s.Id == service.Id).IsIndexed = true;
                                }
                            }
                            Services = tempServices;
                        });
                        _timer = new System.Threading.Timer(par =>
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    foreach (var serviceId in _busyServiceIds.ToList())
                                    {
                                        var response = await _serviceManager.GetServiceAsync(serviceId);
                                        if (ResponseValidator.Validate(response))
                                        {
                                            if (response.ResponseObject.Status != ServiceStatusEnum.Busy && string.IsNullOrEmpty(response.ResponseObject.ActualProcessId))
                                            {
                                                var removed = serviceId;
                                                if (_busyServiceIds.TryTake(out removed))
                                                {
                                                    var updatedService = new ExtendedService(response.ResponseObject);
                                                    if (updatedService.Status == ServiceStatusEnum.Active &&
                                                        updatedService.Type == ServiceTypeEnum.Prc)
                                                    {
                                                        var prcResponse = await _prcServiceManager.GetServiceAsync(updatedService.Id);
                                                        ResponseValidator.Validate(prcResponse);
                                                        updatedService.IsIndexed = prcResponse.ResponseObject.IndexSettings != null;
                                                    }
                                                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                                    {
                                                        if (SelectedServices?.Count <= 0)
                                                        {
                                                            SelectedServices.Add(updatedService);
                                                        }
                                                        Services[Services.IndexOf(Services.First(se => se.Id == serviceId))] = updatedService;
                                                    });
                                                }
                                            }
                                        }
                                    }

                                }
                                catch (Exception exception)
                                {
                                    DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                                }
                            });
                        }, null, 500, 10000);
                    }
                });

            PrepareCommand = new RelayCommand(Prepare);
            ActivateCommand = new RelayCommand(Activate);
            ExportCommand = new RelayCommand(Export);
            RecommendCommand = new RelayCommand(Recommend);
            RecommendByIdCommand = new RelayCommand(RecommendById);
            CancelCommand = new RelayCommand(Cancel);
            DeactivateCommand = new RelayCommand(Deactivate);
            CreateCommand = new RelayCommand<ServiceTypeEnum>(Create);
            ModifyCommand = new RelayCommand(ShowDetails);
            ShowDetailsCommand = new RelayCommand(ShowDetails);
            DeleteCommand = new RelayCommand(Delete);
            IndexCommand = new RelayCommand(Index);
            IndexPartialCommand = new RelayCommand(IndexPartial);

        }

        private System.Threading.Timer _timer;

        private ConcurrentBag<string> _busyServiceIds = new ConcurrentBag<string>();

        private bool _loadedFirst = true;

        private IServiceManager _serviceManager;

        private IClassifierServiceManager _classifierServiceManager;

        private IPrcServiceManager _prcServiceManager;

        private IProcessManager _processManager;

        private DialogHandler _dialogHandler;

        public RelayCommand LoadedCommand { get; private set; }

        public RelayCommand ModifyAliasCommand { get; private set; }

        public RelayCommand<ServiceTypeEnum> CreateCommand { get; private set; }

        public RelayCommand DeleteCommand { get; private set; }

        public RelayCommand ModifyCommand { get; private set; }

        public RelayCommand ShowDetailsCommand { get; private set; }

        public RelayCommand PrepareCommand { get; private set; }
        public RelayCommand ActivateCommand { get; private set; }
        public RelayCommand RecommendCommand { get; private set; }
        public RelayCommand RecommendByIdCommand { get; private set; }
        public RelayCommand IndexCommand { get; private set; }
        public RelayCommand IndexPartialCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand DeactivateCommand { get; private set; }
        public RelayCommand ExportCommand { get; private set; }


        private ObservableCollection<ExtendedService> _services;

        public ObservableCollection<ExtendedService> Services
        {
            get { return _services; }
            set { Set(() => Services, ref _services, value); }
        }


        private ObservableCollection<ExtendedService> _selectedServices;

        public ObservableCollection<ExtendedService> SelectedServices
        {
            get { return _selectedServices; }
            set { Set(() => SelectedServices, ref _selectedServices, value); }
        }

        private async void Create(ServiceTypeEnum serviceType)
        {
            Log.Info(LogMessages.ManageServiceCreate);
            var context = new CommonDialogViewModel
            {
                Header = "Create New Service",
                Buttons = ButtonsEnum.OkCancel,
                Content = new Service { Type = serviceType }
            };
            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            ClientResponseWithObject<Service> response = null;
            var result = await _dialogHandler.Show(view, "RootDialog",
                async (object sender, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessful = false;
                        var errorMessage = "";
                        try
                        {
                            response = await _serviceManager.CreateServiceAsync((Service)context.Content);
                            isSuccessful = response.IsSuccessFul;
                            ResponseValidator.Validate(response, false);
                        }
                        catch (Exception exception)
                        {
                            isSuccessful = false;
                            errorMessage = exception.Message;

                        }
                        finally
                        {
                            if (!isSuccessful)
                            {
                                context.ErrorMessage = errorMessage;
                                context.ShowError = true;
                                args.Session.UpdateContent(view);
                            }
                            else
                            {
                                canClose = true;
                                args.Session.Close((CommonDialogResult)args.Parameter);
                            }
                        }
                    }
                });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                Services.Add(new ExtendedService(response.ResponseObject));
            }
        }

        private bool _servicesGridSettingsLadedFromFile = false;
        private DataGridSettings _servicesGridSettings;

        public DataGridSettings ServicesGridSettings
        {
            get { return _servicesGridSettings; }
            set
            {
                if (Set(() => ServicesGridSettings, ref _servicesGridSettings, value))
                {
                    if (value != null && !_servicesGridSettingsLadedFromFile)
                    {
                        GlobalStore.SaveGridSettings("ManageService_Services", "all", value);
                    }
                    _servicesGridSettingsLadedFromFile = false;
                }
            }
        }

        private void SetGridSettings()
        {
            var gridSettingsDict = GlobalStore.GridSettingsDictionary;
            if (gridSettingsDict != null && gridSettingsDict.Any())
            {
                if (gridSettingsDict.ContainsKey("ManageService_Services"))
                {
                    var tagsSettings = gridSettingsDict["ManageService_Services"];
                    if (tagsSettings.ContainsKey("all"))
                    {
                        _servicesGridSettingsLadedFromFile = true;
                        ServicesGridSettings = tagsSettings["all"];
                    }
                    else
                    {
                        ServicesGridSettings = null;
                    }
                }
                else
                {
                    ServicesGridSettings = null;
                }
            }
            else
            {
                ServicesGridSettings = null;
            }
        }


        private async void ShowDetails()
        {
            Log.Info(LogMessages.ManageServiceShowDetails);
            if (SelectedServices.Any())
            {
                var context = new CommonDialogViewModel
                {
                    Header = "Details",
                    Content = new JContent(new object()),
                    Buttons = ButtonsEnum.Ok
                };

                var getServiceIsSuccessful = true;
                await _dialogHandler.ShowProgress(null, async () =>
                {
                    try
                    {
                        var selectedService = SelectedServices.First();
                        Service service;
                        switch (selectedService.Type)
                        {
                            case ServiceTypeEnum.Classifier:
                                {
                                    var response = await _classifierServiceManager.GetServiceAsync(selectedService.Id);
                                    ResponseValidator.Validate(response, false);
                                    service = response.ResponseObject;
                                    break;
                                }
                            case ServiceTypeEnum.Prc:
                                {
                                    var response = await _prcServiceManager.GetServiceAsync(selectedService.Id);
                                    ResponseValidator.Validate(response, false);
                                    service = response.ResponseObject;
                                    break;
                                }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        Process process = new Process();
                        if (service != null && !string.IsNullOrEmpty(service.ActualProcessId))
                        {
                            var response = await _processManager.GetProcessAsync(service.ActualProcessId);
                            ResponseValidator.Validate(response, false);
                            process = response.ResponseObject;
                        }
                        context.Content = new JContent(new { Service = service, Process = process });
                    }
                    catch (Exception exception)
                    {
                        getServiceIsSuccessful = false;
                        Messenger.Default.Send(exception);
                    }

                });

                if (!getServiceIsSuccessful)
                    return;

                await _dialogHandler.Show(new CommonDialog { DataContext = context }, "RootDialog");
            }
        }

        private async void Delete()
        {
            Log.Info(LogMessages.ManageServiceDelete);
            if (SelectedServices != null && SelectedServices.Any())
            {
                var context = new CommonDialogViewModel
                {
                    Content = new Message(string.Format("Are you sure to delete {0} services", SelectedServices.Count)),
                    Buttons = ButtonsEnum.YesNoCancel
                };

                var view = new CommonDialog { DataContext = context };
                var result = await _dialogHandler.Show(view, "RootDialog");
                if ((CommonDialogResult)result == CommonDialogResult.Yes)
                {
                    var selectedServices = SelectedServices.ToList();
                    var cancellationToken = new CancellationTokenSource();
                    var status = new StatusDialogViewModel { Title = "Delete services", CancellationTokenSource = cancellationToken };
                    var deletedServices = new List<ExtendedService>();
                    await _dialogHandler.Show(new StatusDialog { DataContext = status }, "RootDialog", async (object sender, DialogOpenedEventArgs oa) =>
                    {
                        try
                        {
                            var all = selectedServices.Count;
                            await Task.Run(() =>
                            {
                                try
                                {
                                    for (var done = 0; done < all && !cancellationToken.IsCancellationRequested;)
                                    {
                                        var sevriceId = selectedServices[done].Id;
                                        try
                                        {
                                            var response = _serviceManager.DeleteServiceAsync(sevriceId).Result;
                                            ResponseValidator.Validate(response, false);
                                            deletedServices.Add(selectedServices[done]);
                                        }
                                        catch (Exception ex)
                                        {
                                            Messenger.Default.Send(new Exception(string.Format("Error during delete service with id: {0}", sevriceId), ex));
                                            status.ErrorCount++;
                                        }
                                        finally
                                        {
                                            done++;
                                            status.DoneCount = done;
                                            status.ProgressValue = (done / (double)all) * 100;
                                        }
                                    }

                                }
                                catch (OperationCanceledException)
                                {
                                    Log.Info(LogMessages.OperationCancelled);
                                }
                            });

                        }
                        catch (Exception exception)
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                        }
                        finally
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => deletedServices.ForEach(ds =>
                            {
                                SelectedServices.Remove(ds);
                                Services.Remove(ds);
                            }));
                            status.OperationIsFinished = true;
                        }
                    });
                }
            }
        }

        private async void Prepare()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();

            var context = new CommonDialogViewModel
            {
                Content = new JContent(new object()),
                Buttons = ButtonsEnum.OkCancel,
                Header = "Prepare Settings"
            };
            var getServiceIsSuccessful = true;
            await _dialogHandler.ShowProgress(null, async () =>
                {
                    try
                    {
                        switch (selected.Type)
                        {
                            case ServiceTypeEnum.Classifier:
                                {
                                    var getServiceResponse = await _classifierServiceManager.GetServiceAsync(selected.Id);
                                    ResponseValidator.Validate(getServiceResponse, false);
                                    var classifierService = getServiceResponse.ResponseObject;
                                    context.Content = new JContent(classifierService.PrepareSettings ?? new ClassifierPrepareSettings { NGramList = new List<int> { 1, 2, 3 } });
                                    break;
                                }
                            case ServiceTypeEnum.Prc:
                                {
                                    var getServiceResponse = await _prcServiceManager.GetServiceAsync(selected.Id);
                                    ResponseValidator.Validate(getServiceResponse, false);
                                    var prcService = getServiceResponse.ResponseObject;
                                    context.Content = new JContent(prcService.PrepareSettings ?? new PrcPrepareSettings());
                                    break;
                                }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception exception)
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                        getServiceIsSuccessful = false;
                    }

                });

            if (!getServiceIsSuccessful)
                return;

            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            ClientResponseWithObject<Process> clientResponse = null;
            var result = await _dialogHandler.Show(view, "RootDialog",
                async (object sender, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessful = false;
                        var errorMessage = "";
                        try
                        {
                            switch (selected.Type)
                            {
                                case ServiceTypeEnum.Classifier:
                                    {
                                        clientResponse = await _classifierServiceManager.PrepareServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ClassifierPrepareSettings>());
                                        break;
                                    }
                                case ServiceTypeEnum.Prc:
                                    {
                                        clientResponse = await _prcServiceManager.PrepareServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<PrcPrepareSettings>());
                                        break;
                                    }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            isSuccessful = clientResponse.IsSuccessFul;
                            ResponseValidator.Validate(clientResponse, false);
                        }
                        catch (Exception exception)
                        {
                            isSuccessful = false;
                            errorMessage = exception.Message;

                        }
                        finally
                        {
                            if (!isSuccessful)
                            {
                                context.ErrorMessage = errorMessage;
                                context.ShowError = true;
                                args.Session.UpdateContent(view);
                            }
                            else
                            {
                                canClose = true;
                                args.Session.Close((CommonDialogResult)args.Parameter);
                            }
                        }
                    }
                });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                selected.Status = ServiceStatusEnum.Busy;
                selected.ActualProcessId = clientResponse.ResponseObject.Id;
                DispatcherHelper.CheckBeginInvokeOnUI(() => Services = new ObservableCollection<ExtendedService>(Services));
                _busyServiceIds.Add(selected.Id);
                Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, clientResponse.ResponseObject));
            }
        }

        private async void Activate()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();
            var context = new CommonDialogViewModel
            {
                Content = new JContent(new object()),
                Buttons = ButtonsEnum.OkCancel,
                Header = "Activate Settings"
            };

            var getServiceIsSuccessful = true;
            await _dialogHandler.ShowProgress(null, async () =>
            {
                try
                {
                    switch (selected.Type)
                    {
                        case ServiceTypeEnum.Classifier:
                            {
                                var getServiceResponse = await _classifierServiceManager.GetServiceAsync(selected.Id);
                                ResponseValidator.Validate(getServiceResponse, false);
                                var classifierService = getServiceResponse.ResponseObject;
                                context.Content = new JContent(classifierService.ActivateSettings ?? new ClassifierActivateSettings { NGramList = new List<int> { 1, 2, 3 } });
                                break;
                            }
                        case ServiceTypeEnum.Prc:
                            {
                                var getServiceResponse = await _prcServiceManager.GetServiceAsync(selected.Id);
                                ResponseValidator.Validate(getServiceResponse, false);
                                var prcService = getServiceResponse.ResponseObject;
                                context.Content = new JContent(prcService.ActivateSettings ?? new PrcActivateSettings());
                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception exception)
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                    getServiceIsSuccessful = false;
                }

            });

            if (!getServiceIsSuccessful)
                return;

            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            ClientResponseWithObject<Process> clientResponse = null;
            var result = await _dialogHandler.Show(view, "RootDialog",
                async (object sender, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessful = false;
                        var errorMessage = "";
                        try
                        {
                            switch (selected.Type)
                            {
                                case ServiceTypeEnum.Classifier:
                                    {
                                        clientResponse = await _classifierServiceManager.ActivateServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ClassifierActivateSettings>());
                                        break;
                                    }
                                case ServiceTypeEnum.Prc:
                                    {
                                        clientResponse = await _prcServiceManager.ActivateServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<PrcActivateSettings>());
                                        break;
                                    }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            isSuccessful = clientResponse.IsSuccessFul;
                            ResponseValidator.Validate(clientResponse, false);
                        }
                        catch (Exception exception)
                        {
                            isSuccessful = false;
                            errorMessage = exception.Message;

                        }
                        finally
                        {
                            if (!isSuccessful)
                            {
                                context.ErrorMessage = errorMessage;
                                context.ShowError = true;
                                args.Session.UpdateContent(view);
                            }
                            else
                            {
                                canClose = true;
                                args.Session.Close((CommonDialogResult)args.Parameter);
                            }
                        }
                    }
                });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    selected.Status = ServiceStatusEnum.Busy;
                    selected.ActualProcessId = clientResponse.ResponseObject.Id;
                    _busyServiceIds.Add(selected.Id);
                    Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, clientResponse.ResponseObject));
                    Services = new ObservableCollection<ExtendedService>(Services);
                });
            }
        }

        private async void Export()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();
            var context = new CommonDialogViewModel
            {
                Content = new JContent(new ExportDictionariesSettings()),
                Buttons = ButtonsEnum.OkCancel,
                Header = "Export Settings"
            };


            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            ClientResponseWithObject<Process> response = null;
            var result = await _dialogHandler.Show(view, "RootDialog",
                async (object sender, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessful = false;
                        var errorMessage = "";
                        try
                        {
                            switch (selected.Type)
                            {
                                case ServiceTypeEnum.Classifier:
                                    {
                                        response = await _classifierServiceManager.ExportDictionariesAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ExportDictionariesSettings>());
                                        break;
                                    }
                                case ServiceTypeEnum.Prc:
                                    {
                                        response = await _prcServiceManager.ExportDictionariesAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ExportDictionariesSettings>());
                                        break;
                                    }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            isSuccessful = response.IsSuccessFul;
                            ResponseValidator.Validate(response, false);
                        }
                        catch (Exception exception)
                        {
                            isSuccessful = false;
                            errorMessage = exception.Message;

                        }
                        finally
                        {
                            if (!isSuccessful)
                            {
                                context.ErrorMessage = errorMessage;
                                context.ShowError = true;
                                args.Session.UpdateContent(view);
                            }
                            else
                            {
                                canClose = true;
                                args.Session.Close((CommonDialogResult)args.Parameter);
                            }
                        }
                    }
                });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, response.ResponseObject));
            }
        }

        private async void Cancel()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();
            if (string.IsNullOrEmpty(selected.ActualProcessId))
                return;
            await _dialogHandler.ShowProgress(null, async () =>
            {
                var clientResponse = await _processManager.CancelProcessAsync(selected.ActualProcessId);
                if (ResponseValidator.Validate(clientResponse))
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        selected.Status = null;
                        var removed = selected.Id;
                        Services = new ObservableCollection<ExtendedService>(Services);
                    });
                }
            });
        }

        private async void Deactivate()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();
            await _dialogHandler.ShowProgress(null, async () =>
            {
                ClientResponse clientResponse;
                switch (selected.Type)
                {
                    case ServiceTypeEnum.Classifier:
                        clientResponse = await _classifierServiceManager.DeactivateServiceAsync(selected.Id);
                        break;
                    case ServiceTypeEnum.Prc:
                        clientResponse = await _prcServiceManager.DeactivateServiceAsync(selected.Id);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                ResponseValidator.Validate(clientResponse, false);
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    selected.Status = ServiceStatusEnum.Prepared;
                    Services = new ObservableCollection<ExtendedService>(Services);
                });
            });
        }

        private async void Recommend()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();
            var context = new CommonDialogViewModel
            {
                Buttons = ButtonsEnum.OkCancel,
                Header = "Recommend"
            };
            var resultContext = new CommonDialogViewModel
            {
                Buttons = ButtonsEnum.Ok,
                Header = "Recommend"
            };
            switch (selected.Type)
            {
                case ServiceTypeEnum.Classifier:
                    context.Content = new JContent(new ClassifierRecommendationRequest());
                    break;
                case ServiceTypeEnum.Prc:
                    context.Content = new JContent(new PrcRecommendationRequest());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            var result = await _dialogHandler.Show(view, "RootDialog",
                async (object sender, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessful = false;
                        var errorMessage = "";
                        try
                        {
                            switch (selected.Type)
                            {
                                case ServiceTypeEnum.Classifier:
                                    var classifierClientResponse = await _classifierServiceManager.RecommendAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ClassifierRecommendationRequest>());
                                    isSuccessful = classifierClientResponse.IsSuccessFul;
                                    ResponseValidator.Validate(classifierClientResponse, false);
                                    resultContext.Content = new JContent(classifierClientResponse.ResponseObject);
                                    break;
                                case ServiceTypeEnum.Prc:
                                    var prcClientResponse = await _prcServiceManager.RecommendAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<PrcRecommendationRequest>());
                                    isSuccessful = prcClientResponse.IsSuccessFul;
                                    ResponseValidator.Validate(prcClientResponse, false);
                                    resultContext.Content = new JContent(prcClientResponse.ResponseObject);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        catch (Exception exception)
                        {
                            isSuccessful = false;
                            errorMessage = exception.Message;

                        }
                        finally
                        {
                            if (!isSuccessful)
                            {
                                context.ErrorMessage = errorMessage;
                                context.ShowError = true;
                                args.Session.UpdateContent(view);
                            }
                            else
                            {
                                canClose = true;
                                args.Session.Close((CommonDialogResult)args.Parameter);
                            }
                        }
                    }
                });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                await _dialogHandler.Show(new CommonDialog { DataContext = resultContext }, "RootDialog");
            }
        }

        private async void RecommendById()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();
            if (selected.Type != ServiceTypeEnum.Prc) return;
            var context = new CommonDialogViewModel
            {
                Buttons = ButtonsEnum.OkCancel,
                Header = "RecommendById",
                Content = new JContent(new PrcRecommendationByIdRequest())
            };
            var resultContext = new CommonDialogViewModel
            {
                Buttons = ButtonsEnum.Ok,
                Header = "RecommendById"
            };
            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            var result = await _dialogHandler.Show(view, "RootDialog",
                async (object sender, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessful = false;
                        var errorMessage = "";
                        try
                        {
                            var prcClientResponse = await _prcServiceManager.RecommendByIdAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<PrcRecommendationByIdRequest>());
                            isSuccessful = prcClientResponse.IsSuccessFul;
                            ResponseValidator.Validate(prcClientResponse, false);
                            resultContext.Content = new JContent(prcClientResponse.ResponseObject);
                        }
                        catch (Exception exception)
                        {
                            isSuccessful = false;
                            errorMessage = exception.Message;

                        }
                        finally
                        {
                            if (!isSuccessful)
                            {
                                context.ErrorMessage = errorMessage;
                                context.ShowError = true;
                                args.Session.UpdateContent(view);
                            }
                            else
                            {
                                canClose = true;
                                args.Session.Close((CommonDialogResult)args.Parameter);
                            }
                        }
                    }
                });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                await _dialogHandler.Show(new CommonDialog { DataContext = resultContext }, "RootDialog");
            }
        }

        private async void Index()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();
            if (selected.Type != ServiceTypeEnum.Prc)
                return;
            var context = new CommonDialogViewModel
            {
                Content = new JContent(new object()),
                Buttons = ButtonsEnum.OkCancel,
                Header = "Index Settings"
            };

            var getServiceIsSuccessful = true;
            await _dialogHandler.ShowProgress(null, async () =>
            {
                try
                {
                    var getServiceResponse = await _prcServiceManager.GetServiceAsync(selected.Id);
                    ResponseValidator.Validate(getServiceResponse, false);
                    var prcService = getServiceResponse.ResponseObject;
                    context.Content = new JContent(prcService.IndexSettings ?? new PrcIndexSettings());
                }
                catch (Exception exception)
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                    getServiceIsSuccessful = false;
                }

            });

            if (!getServiceIsSuccessful)
                return;

            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            ClientResponseWithObject<Process> clientResponse = null;
            var result = await _dialogHandler.Show(view, "RootDialog",
                async (object sender, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessful = false;
                        var errorMessage = "";
                        try
                        {
                            clientResponse = await _prcServiceManager.IndexAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<PrcIndexSettings>());
                            isSuccessful = clientResponse.IsSuccessFul;
                            ResponseValidator.Validate(clientResponse, false);
                        }
                        catch (Exception exception)
                        {
                            isSuccessful = false;
                            errorMessage = exception.Message;

                        }
                        finally
                        {
                            if (!isSuccessful)
                            {
                                context.ErrorMessage = errorMessage;
                                context.ShowError = true;
                                args.Session.UpdateContent(view);
                            }
                            else
                            {
                                canClose = true;
                                args.Session.Close((CommonDialogResult)args.Parameter);
                            }
                        }
                    }
                });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    selected.Status = ServiceStatusEnum.Busy;
                    selected.ActualProcessId = clientResponse.ResponseObject.Id;
                    _busyServiceIds.Add(selected.Id);
                    Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, clientResponse.ResponseObject));
                    Services = new ObservableCollection<ExtendedService>(Services);
                });
            }
        }

        private async void IndexPartial()
        {
            if (SelectedServices == null || !SelectedServices.Any())
                return;
            var selected = SelectedServices.First();
            if (selected.Type != ServiceTypeEnum.Prc)
                return;
            await _dialogHandler.ShowProgress(null, async () =>
            {
                var clientResponse = await _prcServiceManager.IndexPartialAsync(selected.Id);
                if (ResponseValidator.Validate(clientResponse))
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                        {
                            selected.Status = ServiceStatusEnum.Busy;
                            selected.ActualProcessId = clientResponse.ResponseObject.Id;
                            _busyServiceIds.Add(selected.Id);
                            Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, clientResponse.ResponseObject));
                            Services = new ObservableCollection<ExtendedService>(Services);
                        });
                    });
                }
            });
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public void Reload()
        {
            _loadedFirst = true;
        }
    }
}