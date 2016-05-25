using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public class ManageServiceViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the ManageServiceViewModel class.
        /// </summary>
        public ManageServiceViewModel(IServiceManager serviceManager, IClassifierServiceManager classifierServiceManager, IPrcServiceManager prcServiceManager, IProcessManager processManager, DialogHandler dialogHandler)
        {
            Services = new ObservableCollection<Service>();
            _serviceManager = serviceManager;
            _classifierServiceManager = classifierServiceManager;
            _prcServiceManager = prcServiceManager;
            _processManager = processManager;
            _dialogHandler = dialogHandler;

            LoadedCommand = new RelayCommand(async () =>
                {
                    Mouse.SetCursor(Cursors.Arrow);
                    if (_loadedFirst && _serviceManager != null)
                    {
                        _loadedFirst = false;
                        await _dialogHandler.ShowProgress(null, async () =>
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => Services.Clear());
                            Log.Info(LogMessages.ManageDataLoadTags);
                            var response = await _serviceManager.GetServicesAsync();
                            if (ResponseValidator.Validate(response))
                            {
                                Services = new ObservableCollection<Service>(response.ResponseObject);
                                response.ResponseObject.Where(s => s.Status == ServiceStatusEnum.Busy).ToList().ForEach(s => _busyServiceIds.Add(s.Id));
                            }
                        });

                        _timer.Elapsed += async (s, e) =>
                        {
                            await Task.Run(() =>
                            {
                                try
                                {
                                    foreach (var serviceId in _busyServiceIds.ToList())
                                    {
                                        var response = _serviceManager.GetServiceAsync(serviceId).Result;
                                        if (response.ResponseObject.Status != ServiceStatusEnum.Busy)
                                        {
                                            var removed = serviceId;
                                            _busyServiceIds.TryTake(out removed);
                                            DispatcherHelper.CheckBeginInvokeOnUI(() => Services[Services.IndexOf(Services.First(se => se.Id == serviceId))] = response.ResponseObject);
                                        }
                                    }
                                    var selectedServices = SelectedServices?.ToList() ?? new List<Service>();
                                    Services = new ObservableCollection<Service>(Services);
                                    SelectedServices = new ObservableCollection<Service>(selectedServices);

                                }
                                catch (Exception exception)
                                {
                                    DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                                }
                            });
                        };
                        _timer.Start();
                    }
                });

            PrepareCommand = new RelayCommand(Prepare);
            ActivateCommand = new RelayCommand(Activate);
            ExportCommand = new RelayCommand(Export);
            RecommendCommand = new RelayCommand(Recommend);
            CancelCommand = new RelayCommand(Cancel);
            DeactivateCommand = new RelayCommand(Deactivate);
            CreateCommand = new RelayCommand<ServiceTypeEnum>(Create);
            ModifyCommand = new RelayCommand(ShowDetails);
            ShowDetailsCommand = new RelayCommand(ShowDetails);
            DeleteCommand = new RelayCommand(Delete);
        }

        private Timer _timer = new Timer { Interval = 10000, AutoReset = true, Enabled = true };

        private ConcurrentBag<string> _busyServiceIds = new ConcurrentBag<string>();

        private bool _loadedFirst = true;

        private IServiceManager _serviceManager;

        private IClassifierServiceManager _classifierServiceManager;

        private IPrcServiceManager _prcServiceManager;

        private IProcessManager _processManager;

        private DialogHandler _dialogHandler;

        public RelayCommand LoadedCommand { get; private set; }

        public RelayCommand<ServiceTypeEnum> CreateCommand { get; private set; }

        public RelayCommand DeleteCommand { get; private set; }

        public RelayCommand ModifyCommand { get; private set; }

        public RelayCommand ShowDetailsCommand { get; private set; }

        public RelayCommand PrepareCommand { get; private set; }
        public RelayCommand ActivateCommand { get; private set; }
        public RelayCommand RecommendCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand DeactivateCommand { get; private set; }
        public RelayCommand ExportCommand { get; private set; }


        private ObservableCollection<Service> _services;

        public ObservableCollection<Service> Services
        {
            get { return _services; }
            set { Set(() => Services, ref _services, value); }
        }


        private ObservableCollection<Service> _selectedServices;

        public ObservableCollection<Service> SelectedServices
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
            var result = await _dialogHandler.Show(new CommonDialog { DataContext = context }, "RootDialog");
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                await _dialogHandler.ShowProgress(null,
                    async () =>
                    {
                        var response = await _serviceManager.CreateServiceAsync((Service)context.Content);
                        if (ResponseValidator.Validate(response))
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => Services.Add(response.ResponseObject));
                        }
                    });
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
                await DialogHost.Show(new CommonDialog { DataContext = context }, "RootDialog",
                    async (object s, DialogOpenedEventArgs oa) =>
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
                                        ResponseValidator.Validate(response);
                                        service = response.ResponseObject;
                                        break;
                                    }
                                case ServiceTypeEnum.Prc:
                                    {
                                        var response = await _prcServiceManager.GetServiceAsync(selectedService.Id);
                                        ResponseValidator.Validate(response);
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
                                ResponseValidator.Validate(response);
                                process = response.ResponseObject;
                            }
                            context.Content = new JContent(new { Service = service, Process = process });
                        }
                        catch (Exception exception)
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                            oa.Session.Close();
                        }
                    });
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
                    await _dialogHandler.ShowProgress(null, async () =>
                    {
                        var deletedServices = new List<Service>();
                        foreach (var selectedService in SelectedServices)
                        {
                            var response = await _serviceManager.DeleteServiceAsync(selectedService.Id);
                            if (ResponseValidator.Validate(response))
                            {
                                deletedServices.Add(selectedService);
                            }
                        }
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            deletedServices.ForEach(ds =>
                            {
                                SelectedServices.Remove(ds);
                                Services.Remove(ds);
                            })
                        );
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
            var result = await DialogHost.Show(new CommonDialog { DataContext = context }, "RootDialog",
                async (object s, DialogOpenedEventArgs oa) =>
                {
                    try
                    {
                        switch (selected.Type)
                        {
                            case ServiceTypeEnum.Classifier:
                                {
                                    var getServiceResponse = await _classifierServiceManager.GetServiceAsync(selected.Id);
                                    ResponseValidator.Validate(getServiceResponse);
                                    var classifierService = getServiceResponse.ResponseObject;
                                    context.Content = new JContent(classifierService.PrepareSettings ?? new ClassifierPrepareSettings());
                                    break;
                                }
                            case ServiceTypeEnum.Prc:
                                {
                                    var getServiceResponse = await _prcServiceManager.GetServiceAsync(selected.Id);
                                    ResponseValidator.Validate(getServiceResponse);
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
                    }

                });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                await _dialogHandler.ShowProgress(null,
                        async () =>
                        {
                            ClientResponseWithObject<Process> clientResponse;
                            switch (selected.Type)
                            {
                                case ServiceTypeEnum.Classifier:
                                    {
                                        clientResponse = await _classifierServiceManager.PrepareServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ClassifierPrepareSettings>());
                                        if (ResponseValidator.Validate(clientResponse))
                                        {
                                            selected.Status = ServiceStatusEnum.Busy;
                                            selected.ActualProcessId = clientResponse.ResponseObject.Id;
                                            DispatcherHelper.CheckBeginInvokeOnUI(() => Services = new ObservableCollection<Service>(Services));
                                            _busyServiceIds.Add(selected.Id);
                                            Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, clientResponse.ResponseObject));
                                        }
                                        break;
                                    }
                                case ServiceTypeEnum.Prc:
                                    {
                                        clientResponse = await _prcServiceManager.PrepareServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<PrcPrepareSettings>());
                                        if (ResponseValidator.Validate(clientResponse))
                                        {
                                            selected.Status = ServiceStatusEnum.Busy;
                                            selected.ActualProcessId = clientResponse.ResponseObject.Id;
                                            DispatcherHelper.CheckBeginInvokeOnUI(() => Services = new ObservableCollection<Service>(Services));
                                            _busyServiceIds.Add(selected.Id);
                                            Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, clientResponse.ResponseObject));
                                        }
                                        break;
                                    }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        });
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
            var result = await DialogHost.Show(new CommonDialog { DataContext = context }, "RootDialog", async (object s, DialogOpenedEventArgs oa) =>
              {
                  try
                  {
                      switch (selected.Type)
                      {
                          case ServiceTypeEnum.Classifier:
                              {
                                  var getServiceResponse = await _classifierServiceManager.GetServiceAsync(selected.Id);
                                  ResponseValidator.Validate(getServiceResponse);
                                  var classifierService = getServiceResponse.ResponseObject;
                                  context.Content = new JContent(classifierService.ActivateSettings ?? new ClassifierActivateSettings());
                                  break;
                              }
                          case ServiceTypeEnum.Prc:
                              {
                                  var getServiceResponse = await _prcServiceManager.GetServiceAsync(selected.Id);
                                  ResponseValidator.Validate(getServiceResponse);
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
                  }
              });
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                await _dialogHandler.ShowProgress(null, async () =>
                {
                    switch (selected.Type)
                    {
                        case ServiceTypeEnum.Classifier:
                            {
                                var clientResponse = await _classifierServiceManager.ActivateServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ClassifierActivateSettings>());
                                if (ResponseValidator.Validate(clientResponse))
                                {
                                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                    {
                                        selected.Status = ServiceStatusEnum.Active;
                                        Services = new ObservableCollection<Service>(Services);
                                    });
                                }
                                break;
                            }
                        case ServiceTypeEnum.Prc:
                            {
                                var clientResponse = await _prcServiceManager.ActivateServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<PrcActivateSettings>());
                                if (ResponseValidator.Validate(clientResponse))
                                {
                                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                    {
                                        selected.Status = ServiceStatusEnum.Active;
                                        Services = new ObservableCollection<Service>(Services);
                                    });
                                }
                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
            var result = await DialogHost.Show(new CommonDialog { DataContext = context }, "RootDialog");
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                await _dialogHandler.ShowProgress(null, async () =>
                {
                    switch (selected.Type)
                    {
                        case ServiceTypeEnum.Classifier:
                            {
                                var clientResponse = await _classifierServiceManager.ExportDictionariesAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ExportDictionariesSettings>());
                                if (ResponseValidator.Validate(clientResponse))
                                {
                                    Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, clientResponse.ResponseObject));
                                }
                                break;
                            }
                        case ServiceTypeEnum.Prc:
                            {
                                var clientResponse = await _prcServiceManager.ExportDictionariesAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ExportDictionariesSettings>());
                                if (ResponseValidator.Validate(clientResponse))
                                {
                                    Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, clientResponse.ResponseObject));
                                }
                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
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
                        selected.Status = ServiceStatusEnum.New;
                        var removed = selected.Id;
                        _busyServiceIds.TryTake(out removed);
                        Services = new ObservableCollection<Service>(Services);
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
                if (ResponseValidator.Validate(clientResponse))
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        selected.Status = ServiceStatusEnum.Prepared;
                        Services = new ObservableCollection<Service>(Services);
                    });
                }
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
            var result = await _dialogHandler.Show(new CommonDialog { DataContext = context }, "RootDialog");
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                var recommendResult = await DialogHost.Show(new ProgressDialog(), "RootDialog", async (object s, DialogOpenedEventArgs oa) =>
                {
                    try
                    {
                        switch (selected.Type)
                        {
                            case ServiceTypeEnum.Classifier:
                                var classifierClientResponse = await _classifierServiceManager.RecommendServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<ClassifierRecommendationRequest>());
                                if (ResponseValidator.Validate(classifierClientResponse))
                                {
                                    oa.Session.Close(classifierClientResponse.ResponseObject);
                                }
                                else
                                {
                                    oa.Session.Close();
                                }
                                break;
                            case ServiceTypeEnum.Prc:
                                var prcClientResponse = await _prcServiceManager.RecommendServiceAsync(selected.Id, ((JContent)context.Content).GetJToken().ToObject<PrcRecommendationRequest>());
                                if (ResponseValidator.Validate(prcClientResponse))
                                {
                                    oa.Session.Close(prcClientResponse.ResponseObject);
                                }
                                else
                                {
                                    oa.Session.Close();
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception exception)
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                        oa.Session.Close();
                    }
                });
                if (recommendResult != null)
                {
                    JContent recommendationToken;
                    switch (selected.Type)
                    {
                        case ServiceTypeEnum.Classifier:
                            recommendationToken = new JContent((IEnumerable<ClassifierRecommendationResult>)recommendResult);
                            break;
                        case ServiceTypeEnum.Prc:
                            recommendationToken = new JContent((IEnumerable<PrcRecommendationResult>)recommendResult);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    var recommendationContext = new CommonDialogViewModel
                    {
                        Content = recommendationToken,
                        Buttons = ButtonsEnum.Ok,
                        Header = "Recommendation Result"
                    };
                    await _dialogHandler.Show(new CommonDialog { DataContext = recommendationContext }, "RootDialog");
                }
            }
        }
    }
}