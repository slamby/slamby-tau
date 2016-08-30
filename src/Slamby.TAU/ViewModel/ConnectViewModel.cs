using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using Slamby.SDK.Net;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Enum;
using Slamby.TAU.Helper;
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
    public class ConnectViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the ConnectViewModel class.
        /// </summary>
        public ConnectViewModel()
        {
            _dialogHandler = new DialogHandler();
            Endpoints = new ObservableCollection<ConfigurationWithId>(GlobalStore.Endpoints);
            SelectedIndex = Endpoints.IndexOf(Endpoints.FirstOrDefault(ep => ep.Equals(GlobalStore.SelectedEndpoint)));
            SelectCommand = new RelayCommand(async () =>
            {
                if (SelectedIndex < 0) return;
                ClientResponseWithObject<Status> response = null;
                bool IsSuccessFul = false;
                await
                    _dialogHandler.Show(new ProgressDialog(), "ConnectDialog",
                        async (object s, DialogOpenedEventArgs arg) =>
                        {
                            try
                            {
                                var statusManager = new StatusManager(Endpoints[SelectedIndex]);
                                response = await statusManager.GetStatusAsync();
                                IsSuccessFul = response.IsSuccessful;
                            }
                            catch (Exception exception)
                            {
                                IsSuccessFul = false;
                            }
                            finally
                            {
                                arg.Session.Close();
                            }
                        });


                if (IsSuccessFul)
                {
                    var tauVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    var apiVersion = Version.Parse(response.ApiVersion);
                    var isVersionMismatch = tauVersion.Major != apiVersion.Major || tauVersion.Minor != apiVersion.Minor;
                    object result = null;
                    if (isVersionMismatch)
                    {
                        result = await _dialogHandler.Show(new CommonDialog
                        {
                            DataContext =
                                    new CommonDialogViewModel
                                    {
                                        Buttons = ButtonsEnum.YesNo,
                                        Header = "Warning! Version mismatch.",
                                        Content = new Message($"Api version: {apiVersion}{Environment.NewLine}Tau version:{tauVersion}.{Environment.NewLine}Would you like to continue?")
                                    }
                        }, "ConnectDialog");
                    }
                    if (!isVersionMismatch || (CommonDialogResult)result == CommonDialogResult.Yes)
                    {
                        GlobalStore.SelectedEndpoint = Endpoints[SelectedIndex];
                        await ((ViewModelLocator)App.Current.Resources["Locator"]).EndpointUpdate();
                        var mainVindow = new MainWindow();
                        var connectWindow = App.Current.MainWindow;
                        App.Current.MainWindow = mainVindow;
                        mainVindow.Show();
                        connectWindow.Close();
                    }
                }
                else
                {
                    await
                        _dialogHandler.Show(
                            new CommonDialog
                            {
                                DataContext =
                                    new CommonDialogViewModel
                                    {
                                        Header = "Warning!",
                                        Content = new Message($"Faild to connect to selected endpoint.{Environment.NewLine}{string.Join(Environment.NewLine, response.Errors.Errors)}"),
                                        Buttons = ButtonsEnum.Ok
                                    }
                            }, "ConnectDialog");
                }
            });
            NewCommand = new RelayCommand(async () =>
              {
                  var context = new CommonDialogViewModel
                  {
                      Buttons = ButtonsEnum.OkCancel,
                      Header = "Create new endpoint",
                      Content = new JContent(new { ApiBaseEndpoint = new Uri("https://europe.slamby.com/"), ApiSecret = "", ParallelLimit = 0, BulkSize = 1000 })
                  };
                  var view = new CommonDialog { DataContext = context };
                  var canClose = false;
                  var result = await _dialogHandler.Show(view, "ConnectDialog",
                      async (object sender, DialogClosingEventArgs args) =>
                      {
                          if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                          {
                              args.Cancel();
                              args.Session.UpdateContent(new ProgressDialog());
                              var IsSuccessFul = false;
                              var errorMessage = "";
                              try
                              {
                                  var statusManager = new StatusManager(((JContent)context.Content).GetJToken().ToObject<Configuration>());
                                  var response = await statusManager.GetStatusAsync();
                                  IsSuccessFul = response.IsSuccessful;
                              }
                              catch (Exception exception)
                              {
                                  IsSuccessFul = false;
                                  errorMessage = exception is JsonReaderException ? exception.Message : "Faild to connect to selected endpoint!";
                              }
                              finally
                              {
                                  if (!IsSuccessFul)
                                  {
                                      context.ErrorMessage = string.IsNullOrEmpty(errorMessage) ? "Faild to connect to selected endpoint!" : errorMessage;
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
                      var newEndpoint = ((JContent)context.Content).GetJToken().ToObject<ConfigurationWithId>();
                      Endpoints.Add(newEndpoint);
                      GlobalStore.Endpoints = Endpoints.ToList();
                  }
              });
            EditCommand = new RelayCommand(async () =>
            {
                if (SelectedIndex < 0) return;
                var isSelectedInUse = Endpoints[SelectedIndex].Equals(GlobalStore.SelectedEndpoint);
                var context = new CommonDialogViewModel
                {
                    Buttons = ButtonsEnum.OkCancel,
                    Header = "Create new endpoint",
                    Content = new JContent(new { Endpoints[SelectedIndex].ApiBaseEndpoint, Endpoints[SelectedIndex].ApiSecret, Endpoints[SelectedIndex].ParallelLimit, Endpoints[SelectedIndex].BulkSize })
                };
                var view = new CommonDialog { DataContext = context };
                var canClose = false;
                var result = await _dialogHandler.Show(view, "ConnectDialog",
                    async (object sender, DialogClosingEventArgs args) =>
                    {
                        if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                        {
                            args.Cancel();
                            args.Session.UpdateContent(new ProgressDialog());
                            var IsSuccessFul = false;
                            try
                            {
                                var statusManager = new StatusManager(((JContent)context.Content).GetJToken().ToObject<Configuration>());
                                var response = await statusManager.GetStatusAsync();
                                IsSuccessFul = response.IsSuccessful;
                            }
                            catch (Exception)
                            {
                                IsSuccessFul = false;
                            }
                            finally
                            {
                                if (!IsSuccessFul)
                                {
                                    context.ErrorMessage = "Faild to connect to selected endpoint!";
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
                    var modifiedEndpoint = ((JContent)context.Content).GetJToken().ToObject<ConfigurationWithId>();
                    modifiedEndpoint.Id = Endpoints[SelectedIndex].Id;
                    Endpoints[SelectedIndex] = modifiedEndpoint;
                    Endpoints = new ObservableCollection<ConfigurationWithId>(Endpoints);
                    SelectedIndex = Endpoints.IndexOf(modifiedEndpoint);
                    GlobalStore.Endpoints = Endpoints.ToList();
                    if (isSelectedInUse)
                    {
                        GlobalStore.SelectedEndpoint = Endpoints[SelectedIndex];
                        await ((ViewModelLocator)App.Current.Resources["Locator"]).EndpointUpdate();
                        var mainVindow = new MainWindow();
                        var connectWindow = App.Current.MainWindow;
                        App.Current.MainWindow = mainVindow;
                        mainVindow.Show();
                        connectWindow.Close();
                    }
                }
            });
            DeleteCommand = new RelayCommand(async () =>
             {
                 if (SelectedIndex < 0) return;
                 var context = new CommonDialogViewModel
                 {
                     Buttons = ButtonsEnum.YesNo,
                     Header = "Delete endpoint",
                     Content = new Message("Are you sure to delete the selected endpoint?")
                 };
                 var result = await _dialogHandler.Show(new CommonDialog { DataContext = context }, "ConnectDialog");
                 if ((CommonDialogResult)result == CommonDialogResult.Yes)
                 {
                     Endpoints.RemoveAt(SelectedIndex);
                     SelectedIndex = 0;
                     GlobalStore.Endpoints = Endpoints.ToList();
                 }
             });
        }

        private DialogHandler _dialogHandler;

        private ObservableCollection<ConfigurationWithId> _endpoints;

        public ObservableCollection<ConfigurationWithId> Endpoints
        {
            get { return _endpoints; }
            set { Set(() => Endpoints, ref _endpoints, value); }
        }

        private int _selectedIndex;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { Set(() => SelectedIndex, ref _selectedIndex, value); }
        }

        public RelayCommand NewCommand { get; private set; }
        public RelayCommand DeleteCommand { get; private set; }
        public RelayCommand EditCommand { get; private set; }
        public RelayCommand SelectCommand { get; private set; }

        public RelayCommand LoadCommand { get; private set; }
    }
}