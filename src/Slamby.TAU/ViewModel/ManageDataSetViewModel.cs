using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
using Slamby.TAU.Logger;
using Slamby.TAU.Resources;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using GalaSoft.MvvmLight.Threading;
using System.Windows.Input;
using Slamby.TAU.View;
using CommonDialog = Slamby.TAU.View.CommonDialog;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ManageDataSetViewModel : ViewModelBase
    {

        public ManageDataSetViewModel(IDataSetManager dataSetManager, ObservableCollection<DataSet> datasets)
        {
            _dataSetManager = IsInDesignModeStatic ? new DesignDataSetManager() : dataSetManager;

            DataSets = datasets;
            if (DataSets != null && DataSets.Any())
                SelectedDataSet = DataSets[0];
            AddCommand = new RelayCommand(async ()=> await Add());
            CloneDatasetCommand = new RelayCommand(async () => await Add(SelectedDataSet));
            SeletcToWorkCommand = new RelayCommand(async () => await SelectToWork());
            ImportDocumentCommand = new RelayCommand(ImportJson<object>);
            ImportTagCommand = new RelayCommand(ImportJson<Tag>);
            ImportDocumentCsvCommand = new RelayCommand(ImportCsv<object>);
            ImportTagCsvCommand = new RelayCommand(ImportCsv<Tag>);
            DoubleClickCommand = new RelayCommand(async () => await DoubleClick());
            DeleteCommand = new RelayCommand(Delete);
        }

        private IDataSetManager _dataSetManager;

        private ObservableCollection<DataSet> _dataSets;

        public ObservableCollection<DataSet> DataSets
        {
            get { return _dataSets; }
            set { Set(() => DataSets, ref _dataSets, value); }
        }

        DataSet _selectedDataSet;

        public DataSet SelectedDataSet
        {
            get { return _selectedDataSet; }
            set { Set(() => SelectedDataSet, ref _selectedDataSet, value); }
        }

        public RelayCommand LoadedCommand { get; private set; } = new RelayCommand(() => { Mouse.SetCursor(Cursors.Arrow); });

        public RelayCommand AddCommand { get; private set; }
        public RelayCommand CloneDatasetCommand { get; private set; }

        //public RelayCommand SetAsDefaultCommand { get; private set; }

        public RelayCommand SeletcToWorkCommand { get; private set; }

        public RelayCommand DoubleClickCommand { get; private set; }

        //public RelayCommand OptimizeCommand { get; private set; }

        public RelayCommand DeleteCommand { get; private set; }
        public RelayCommand ImportDocumentCommand { get; private set; }
        public RelayCommand ImportTagCommand { get; private set; }
        public RelayCommand ImportDocumentCsvCommand { get; private set; }
        public RelayCommand ImportTagCsvCommand { get; private set; }

        private async Task Add(DataSet selectedDataSet = null)
        {
            Log.Info(LogMessages.ManageDataSetAddCommand);
            var newDataSet = selectedDataSet == null ? new DataSet
                                {
                                    NGramCount = 3,
                                    IdField = "id",
                                    TagField = "tags",
                                    InterpretedFields = new List<string> { "title", "desc" },
                                    SampleDocument = new
                                    {
                                        id = 10,
                                        title = "thisisthetitle",
                                        desc = "thisisthedesc",
                                        tags = new[] { "tag1" }
                                    }
                                } 
                                : new DataSet
                                    {
                                        NGramCount = SelectedDataSet.NGramCount,
                                        IdField = SelectedDataSet.IdField,
                                        TagField = SelectedDataSet.TagField,
                                        InterpretedFields = SelectedDataSet.InterpretedFields,
                                        SampleDocument = SelectedDataSet.SampleDocument
                                    };
            var view = new AddDataSetDialog { DataContext = new AddDataSetDialogViewModel(newDataSet) };
            var isAccepted = await DialogHandler.Show(view, "RootDialog");
            if ((bool)isAccepted)
            {
                await DialogHandler.ShowProgress(null, async () =>
                {
                    var response = await _dataSetManager.CreateDataSetAsync(newDataSet);
                    if (ResponseValidator.Validate(response))
                    {
                        var createdResponse = await _dataSetManager.GetDataSetAsync(newDataSet.Name);
                        if (ResponseValidator.Validate(createdResponse))
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => DataSets.Add(createdResponse.ResponseObject));
                        }
                    }
                });
            }
        }

        private async void ImportCsv<T>()
        {
            var ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "CSV|*.csv";
            if (ofd.ShowDialog() == true)
            {
                var importSettings = new CsvImportSettings { Delimiter = ",", Force = true };
                var dialogResult = await DialogHandler.Show(
                        new CommonDialog
                        {
                            DataContext =
                                new CommonDialogViewModel
                                {
                                    Header = "Csv Import Settings",
                                    Content = importSettings,
                                    Buttons = ButtonsEnum.OkCancel
                                }
                        }, "RootDialog");
                if ((CommonDialogResult)dialogResult == CommonDialogResult.Cancel)
                    return;
                var invalidRows = new List<int>();
                var errors = new ConcurrentBag<string>();
                var cancellationToken = new CancellationTokenSource();
                var status = new StatusDialogViewModel { ProgressValue = 0, Title = "Importing documents", CancellationTokenSource = cancellationToken };
                await DialogHost.Show(new StatusDialog { DataContext = status }, "RootDialog",
                    async (object sender, DialogOpenedEventArgs oa) =>
                    {
                        FileStream stream = null;
                        try
                        {
                            stream = new FileStream(ofd.FileName, FileMode.Open);
                            stream.Position = 0;
                            var importResult = new CsvImportResult();
                            importSettings.CsvReader = CsvProcesser.GetCsvReader(stream, importSettings);
                            while (importResult.CanContinue && !cancellationToken.IsCancellationRequested)
                            {
                                importResult = await CsvProcesser.GetTokens(importSettings, typeof(T) == typeof(Tag));
                                invalidRows.AddRange(importResult.InvalidRows);
                                status.ErrorCount += importResult.InvalidRows.Count;

                                if (typeof(T) == typeof(Tag))
                                {
                                    var response = new ClientResponseWithObject<BulkResults>();
                                    try
                                    {
                                        var settings = new TagBulkSettings();
                                        settings.Tags = importResult.Tokens.Select(t => t.ToObject<Tag>()).ToList();
                                        response = await new TagManager(GlobalStore.EndpointConfiguration, SelectedDataSet.Name).BulkTagsAsync(settings);
                                        if (!ResponseValidator.Validate(response))
                                        {
                                            return;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add(string.Format("Error during bulk process:{0}{1}", Environment.NewLine, ex.Message));
                                        status.ErrorCount += GlobalStore.BulkSize;
                                    }
                                    finally
                                    {
                                        var bulkErrors = response.ResponseObject.Results.Where(br => br.StatusCode != (int)HttpStatusCode.OK).ToList();
                                        if (bulkErrors.Any())
                                        {
                                            foreach (var error in bulkErrors)
                                            {
                                                errors.Add(string.Format("Id: {0}, error: {1}", error.Id, error.Error));
                                                status.ErrorCount++;
                                            }

                                        }
                                        status.DoneCount += (importResult.Tokens.Count + importResult.InvalidRows.Count);
                                        status.ProgressValue = (stream.Position / (double)stream.Length) * 100;
                                        importResult.Tokens = null;
                                        importResult.InvalidRows = null;
                                    }
                                }
                                else
                                {
                                    var response = new ClientResponseWithObject<BulkResults>();
                                    try
                                    {
                                        var settings = new DocumentBulkSettings();
                                        settings.Documents = importResult.Tokens.Select(t => t.ToObject<object>()).ToList();
                                        response = await new DocumentManager(GlobalStore.EndpointConfiguration, SelectedDataSet.Name).BulkDocumentsAsync(settings);
                                        if (!ResponseValidator.Validate(response))
                                        {
                                            return;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add(string.Format("Error during bulk process:{0}{1}", Environment.NewLine, ex.Message));
                                        status.ErrorCount += GlobalStore.BulkSize;
                                    }
                                    finally
                                    {
                                        var bulkErrors = response.ResponseObject.Results.Where(br => br.StatusCode != (int)HttpStatusCode.OK).ToList();
                                        if (bulkErrors.Any())
                                        {
                                            foreach (var error in bulkErrors)
                                            {
                                                errors.Add(string.Format("Id: {0}, error: {1}", error.Id, error.Error));
                                                status.ErrorCount++;
                                            }

                                        }
                                        status.DoneCount += (importResult.Tokens.Count + importResult.InvalidRows.Count);
                                        status.ProgressValue = (stream.Position / (double)stream.Length) * 100;
                                        importResult.Tokens = null;
                                        importResult.InvalidRows = null;
                                    }
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                        }
                        finally
                        {
                            stream?.Close();
                            oa.Session.Close();
                        }
                    });

                if (invalidRows.Any())
                    await DialogHandler.Show(new CommonDialog { DataContext = new CommonDialogViewModel { Header = "One or more invalid row found", Content = $"Invalid rows:{Environment.NewLine}{string.Join(", ", invalidRows)}", Buttons = ButtonsEnum.Ok } }, "RootDialog");
                if (errors.Any())
                    await DialogHandler.Show(new CommonDialog { DataContext = new CommonDialogViewModel { Header = "One or more error occured", Content = $"Errors:{Environment.NewLine}{string.Join(", ", errors)}", Buttons = ButtonsEnum.Ok } }, "RootDialog");

            }
        }

        private async void ImportJson<T>()
        {
            var ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "JSON|*.json";
            if (ofd.ShowDialog() == true)
            {
                var tokens = new List<JToken>();
                await DialogHandler.ShowProgress(null, async () =>
                  {
                      tokens = await JsonProcesser.GetTokens(ofd.FileName);
                  });

                await Import<T>(tokens);
            }

        }

        private async Task Import<T>(List<JToken> tokens)
        {
            var cancellationToken = new CancellationTokenSource();
            var status = new StatusDialogViewModel { Title = "Importing documents", CancellationTokenSource = cancellationToken };
            await DialogHost.Show(new StatusDialog { DataContext = status }, "RootDialog", async (object sender, DialogOpenedEventArgs oa) =>
            {
                try
                {
                    var errors = new ConcurrentBag<string>();
                    var all = tokens.Count;
                    var done = 0;
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (typeof(T) == typeof(Tag))
                            {
                                var result = Parallel.ForEach(tokens,
                                    new ParallelOptions
                                    {
                                        MaxDegreeOfParallelism = 8,
                                        CancellationToken = cancellationToken.Token
                                    }, token =>
                                    {
                                        ClientResponse response = new ClientResponse();
                                        try
                                        {

                                            var newDocument = token.ToObject<Tag>();
                                            response =
                                                new TagManager(GlobalStore.EndpointConfiguration,
                                                    SelectedDataSet.Name).CreateTagAsync(newDocument).Result;
                                        }
                                        catch (Exception ex)
                                        {
                                            errors.Add(string.Format("Id: {0}, error: {1}", token["Id"], ex.Message));
                                            status.ErrorCount++;
                                        }
                                        finally
                                        {
                                            if (!response.IsSuccessFul)
                                            {
                                                errors.Add(string.Format("Id: {0}, error: {1}", token["Id"], response.ServerMessage));
                                                status.ErrorCount++;
                                            }
                                            done++;
                                            status.DoneCount++;
                                            status.ProgressValue = (done / (double)all) * 100;
                                        }
                                    });
                                if (!result.IsCompleted || errors.Any())
                                {
                                    var errorResponse = new ClientResponse()
                                    {
                                        IsSuccessFul = false,
                                        HttpStatusCode = System.Net.HttpStatusCode.InternalServerError,
                                        Errors = new ErrorsModel { Errors = errors },
                                        ServerMessage = "One or more error occurred during import."
                                    };
                                    ResponseValidator.Validate(errorResponse);
                                }
                            }
                            else
                            {
                                var remaining = tokens.Count;
                                while ((remaining - GlobalStore.BulkSize) > 0 && !cancellationToken.IsCancellationRequested)
                                {
                                    var response = new ClientResponseWithObject<BulkResults>();
                                    try
                                    {
                                        var settings = new DocumentBulkSettings();
                                        settings.Documents = tokens.Take(GlobalStore.BulkSize).Select(t => t.ToObject<object>()).ToList();
                                        response = new DocumentManager(GlobalStore.EndpointConfiguration, SelectedDataSet.Name).BulkDocumentsAsync(settings).Result;
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add(string.Format("Error during bulk process at range [{0}-{1}]{2}{3}", done, done + GlobalStore.BulkSize, Environment.NewLine, ex.Message));
                                        status.ErrorCount += GlobalStore.BulkSize;
                                    }
                                    finally
                                    {
                                        var bulkErrors = response.ResponseObject.Results.Where(br => br.StatusCode != (int)HttpStatusCode.OK).ToList();
                                        if (bulkErrors.Any())
                                        {
                                            foreach (var error in bulkErrors)
                                            {
                                                errors.Add(string.Format("Id: {0}, error: {1}", error.Id, error.Error));
                                                status.ErrorCount++;
                                            }

                                        }
                                        done += GlobalStore.BulkSize;
                                        status.DoneCount = done;
                                        status.ProgressValue = (done / (double)all) * 100;
                                        remaining -= GlobalStore.BulkSize;
                                        tokens = tokens.Skip(GlobalStore.BulkSize).ToList();
                                    }
                                }
                                if (remaining > 0 && !cancellationToken.IsCancellationRequested)
                                {
                                    var response = new ClientResponseWithObject<BulkResults>();
                                    try
                                    {
                                        var settings = new DocumentBulkSettings();
                                        settings.Documents = tokens.Take(remaining).Select(t => t.ToObject<object>()).ToList();
                                        response = new DocumentManager(GlobalStore.EndpointConfiguration, SelectedDataSet.Name).BulkDocumentsAsync(settings).Result;
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add(string.Format("Error during bulk process at range [{0}-{1}]{2}{3}", done, done + remaining, Environment.NewLine, ex.Message));
                                        status.ErrorCount += remaining;
                                    }
                                    finally
                                    {
                                        var bulkErrors = response.ResponseObject.Results.Where(br => br.StatusCode != (int)HttpStatusCode.OK).ToList();
                                        if (bulkErrors.Any())
                                        {
                                            foreach (var error in bulkErrors)
                                            {
                                                errors.Add(string.Format("Id: {0}, error: {1}", error.Id, error.Error));
                                                status.ErrorCount++;
                                            }

                                        }
                                        done += remaining;
                                        status.DoneCount = done;
                                        status.ProgressValue = (done / (int)all) * 100;
                                        remaining -= remaining;
                                    }
                                }
                            }

                        }
                        catch (OperationCanceledException)
                        {
                            Log.Info(LogMessages.OperationCancelled);
                            if (errors.Any())
                            {
                                var errorResponse = new ClientResponse() { IsSuccessFul = false, HttpStatusCode = System.Net.HttpStatusCode.InternalServerError, Errors = new ErrorsModel { Errors = errors }, ServerMessage = "One or more error occurred during import." };
                                ResponseValidator.Validate(errorResponse);
                            }
                        }
                    });

                }
                catch (Exception exception)
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                }
                finally
                {
                    oa.Session.Close();
                }
            });
        }

        //private void SetAsDefaultEventHandler(object sender, DialogOpenedEventArgs eventArgs)
        //{
        //    StatusHelper.LogStatus(new StatusMessage("test message", DateTime.UtcNow));
        //    eventArgs.Session.Close();
        //}

        private async Task SelectToWork()
        {
            await DialogHandler.ShowProgress(null, () => Messenger.Default.Send(new UpdateMessage(UpdateType.SelectedDataSetChange, SelectedDataSet)));
        }

        private async Task DoubleClick()
        {
            await DialogHandler.ShowProgress(null, () =>
            {
                Messenger.Default.Send(new UpdateMessage(UpdateType.SelectedDataSetChange, SelectedDataSet));
                Messenger.Default.Send(new UpdateMessage(UpdateType.SelectedMenuItemChange, "Data"));
            });
        }

        //private async void OptimizeEventHandler(object sender, DialogOpenedEventArgs eventArgs)
        //{
        //    var response = await _dataSetManager.OptimizeDataSetAsync(SelectedDataSet.Name);
        //    if (ResponseValidator.Validate(response))
        //    {
        //        SelectedDataSet.Statistics.SegmentsCount = 0;
        //        SelectedDataSet.Statistics.ShadowDocumentsCount = 0;
        //        //reset binding to update the changed property on the view
        //        var originalSelected = SelectedDataSet;
        //        SelectedDataSet = null;
        //        SelectedDataSet = originalSelected;
        //        eventArgs.Session.Close();
        //    }
        //}

        private async void Delete()
        {
            Log.Info(LogMessages.ManageDataSetDeleteCommand);
            var context = new CommonDialogViewModel
            {
                Content = new Message("Are you sure to remove the following data set: " + SelectedDataSet.Name),
                Buttons = ButtonsEnum.YesNoCancel
            };
            var view = new CommonDialog { DataContext = context };
            var result = await DialogHandler.Show(view, "RootDialog");
            if ((CommonDialogResult)result == CommonDialogResult.Yes)
            {
                await DialogHost.Show(new ProgressDialog(), "RootDialog", async (object s, DialogOpenedEventArgs oa) =>
                {
                    try
                    {
                        var response = await _dataSetManager.DeleteDataSetAsync(SelectedDataSet.Name);
                        if (ResponseValidator.Validate(response))
                        {
                            DataSets.Remove(SelectedDataSet);
                            SelectedDataSet = null;
                            SelectedDataSet = DataSets.FirstOrDefault();
                        }
                    }
                    catch (Exception exception)
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                    }
                    finally
                    {
                        oa.Session.Close();
                    }
                });
            }
        }

    }
}