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
using Dragablz;
using Newtonsoft.Json;
using Slamby.SDK.Net.Managers.Interfaces;
using Slamby.TAU.Properties;
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

        public ManageDataSetViewModel(IDataSetManager dataSetManager, DialogHandler dialogHandler)
        {
            _dataSetManager = dataSetManager;
            _dialogHandler = dialogHandler;

            AddCommand = new RelayCommand(async () => await Add());
            CloneDatasetCommand = new RelayCommand(async () => await Add(SelectedDataSet));
            ImportDocumentCommand = new RelayCommand(ImportJson<object>);
            ImportTagCommand = new RelayCommand(ImportJson<Tag>);
            ImportDocumentCsvCommand = new RelayCommand(ImportCsv<object>);
            ImportTagCsvCommand = new RelayCommand(ImportCsv<Tag>);
            DoubleClickCommand = new RelayCommand(() =>
                {
                    if (SelectedDataSet != null)
                        Messenger.Default.Send(new UpdateMessage(UpdateType.OpenNewTab,
                          new HeaderedItemViewModel(SelectedDataSet.Name + " -Data",
                              new ManageData { DataContext = new ManageDataViewModel(SelectedDataSet, _dialogHandler) }, true)));
                });
            DeleteCommand = new RelayCommand(Delete);
            RenameCommand = new RelayCommand(Rename);
            if (_loadedFirst)
            {
                DataSets.Clear();
                try
                {
                    Task.Run(async () =>
                                    {
                                        var response = await _dataSetManager.GetDataSetsAsync();
                                        if (ResponseValidator.Validate(response))
                                        {
                                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                            {
                                                response.ResponseObject.ToList().ForEach(ds => DataSets.Add(ds));
                                            });
                                        }
                                        _loadedFirst = false;
                                    }).Wait();
                }
                catch (Exception exception)
                {
                    Messenger.Default.Send(exception);
                }

            }
        }

        private bool _loadedFirst = true;
        private IDataSetManager _dataSetManager;
        private DialogHandler _dialogHandler;

        private ObservableCollection<DataSet> _dataSets = new ObservableCollection<DataSet>();

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
        public RelayCommand LoadedCommand { get; private set; }
        public RelayCommand AddCommand { get; private set; }
        public RelayCommand CloneDatasetCommand { get; private set; }
        public RelayCommand DoubleClickCommand { get; private set; }
        public RelayCommand RenameCommand { get; private set; }
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
                },
                Schema = JsonConvert.DeserializeObject("{\"type\": \"object\", \"properties\": { \"id\": { \"type\": \"integer\" }, \"title\": { \"type\": \"string\" }, \"desc\": { \"type\": \"string\" }, \"tag\": { \"type\": \"array\", \"items\": { \"type\": \"string\"}}}}")
            } : new DataSet
            {
                NGramCount = SelectedDataSet.NGramCount,
                IdField = SelectedDataSet.IdField,
                TagField = SelectedDataSet.TagField,
                InterpretedFields = SelectedDataSet.InterpretedFields,
                SampleDocument = SelectedDataSet.SampleDocument,
                Schema = selectedDataSet.Schema
            };
            var view = new AddDataSetDialog { DataContext = new AddDataSetDialogViewModel(newDataSet) };
            var isAccepted = await _dialogHandler.Show(view, "RootDialog");
            if ((bool)isAccepted)
            {
                await _dialogHandler.ShowProgress(null, async () =>
                {

                    var response = newDataSet.Schema == null ? await _dataSetManager.CreateDataSetAsync(newDataSet) : await _dataSetManager.CreateDataSetSchemaAsync(newDataSet);
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
            var ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "CSV|*.csv"
            };
            if (ofd.ShowDialog() == true)
            {
                var importSettings = new CsvImportSettings { Delimiter = ",", Force = true };
                var dialogResult = await _dialogHandler.Show(
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
                await _dialogHandler.Show(new StatusDialog { DataContext = status }, "RootDialog",
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
                                        response = await new TagManager(GlobalStore.SelectedEndpoint, SelectedDataSet.Name).BulkTagsAsync(settings);
                                        ResponseValidator.Validate(response);
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add(string.Format("Error during bulk process:{0}{1}", Environment.NewLine, ex.Message));
                                        status.ErrorCount += importResult.Tokens.Count;
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
                                        response = await new DocumentManager(GlobalStore.SelectedEndpoint, SelectedDataSet.Name).BulkDocumentsAsync(settings);
                                        if (!ResponseValidator.Validate(response))
                                        {
                                            return;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Messenger.Default.Send(ex);
                                        status.ErrorCount += GlobalStore.SelectedEndpoint.BulkSize;
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
                            status.OperationIsFinished = true;
                        }
                    });

                if (invalidRows.Any())
                    Messenger.Default.Send(new Exception($"Invalid rows:{Environment.NewLine}{string.Join(", ", invalidRows)}"));
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
                await _dialogHandler.ShowProgress(null, async () =>
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
            await _dialogHandler.Show(new StatusDialog { DataContext = status }, "RootDialog", async (object sender, DialogOpenedEventArgs oa) =>
            {
                try
                {
                    var all = tokens.Count;
                    var done = 0;
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (typeof(T) == typeof(Tag))
                            {
                                var response = new ClientResponseWithObject<BulkResults>();
                                try
                                {
                                    var settings = new TagBulkSettings();
                                    settings.Tags = tokens.Select(t => t.ToObject<Tag>()).ToList();
                                    response = new TagManager(GlobalStore.SelectedEndpoint, SelectedDataSet.Name).BulkTagsAsync(settings).Result;
                                    ResponseValidator.Validate(response, false);
                                }
                                catch (Exception ex)
                                {
                                    Messenger.Default.Send(ex);
                                    status.ErrorCount += tokens.Count;
                                }
                                finally
                                {
                                    var bulkErrors = response.ResponseObject?.Results.Where(br => br.StatusCode != (int)HttpStatusCode.OK).ToList();
                                    if (bulkErrors != null && bulkErrors.Any())
                                    {
                                        foreach (var error in bulkErrors)
                                        {
                                            Messenger.Default.Send(new Exception(string.Format("Id: {0}, error: {1}", error.Id, error.Error)));
                                            status.ErrorCount++;
                                        }

                                    }
                                    status.DoneCount += tokens.Count;
                                    status.ProgressValue = 100;
                                }
                            }
                            else
                            {
                                var remaining = tokens.Count;
                                while ((remaining - GlobalStore.SelectedEndpoint.BulkSize) > 0 && !cancellationToken.IsCancellationRequested)
                                {
                                    var response = new ClientResponseWithObject<BulkResults>();
                                    try
                                    {
                                        var settings = new DocumentBulkSettings();
                                        settings.Documents = tokens.Take(GlobalStore.SelectedEndpoint.BulkSize).Select(t => t.ToObject<object>()).ToList();
                                        response = new DocumentManager(GlobalStore.SelectedEndpoint, SelectedDataSet.Name).BulkDocumentsAsync(settings).Result;
                                        ResponseValidator.Validate(response, false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Messenger.Default.Send(new Exception(string.Format("Error during bulk process at range [{0}-{1}]", done, done + remaining), ex));
                                        status.ErrorCount += GlobalStore.SelectedEndpoint.BulkSize;
                                    }
                                    finally
                                    {
                                        var bulkErrors = response.ResponseObject.Results.Where(br => br.StatusCode != (int)HttpStatusCode.OK).ToList();
                                        if (bulkErrors.Any())
                                        {
                                            foreach (var error in bulkErrors)
                                            {
                                                Messenger.Default.Send(new Exception(string.Format("Id: {0}, error: {1}", error.Id, error.Error)));
                                                status.ErrorCount++;
                                            }

                                        }
                                        done += GlobalStore.SelectedEndpoint.BulkSize;
                                        status.DoneCount = done;
                                        status.ProgressValue = (done / (double)all) * 100;
                                        remaining -= GlobalStore.SelectedEndpoint.BulkSize;
                                        tokens = tokens.Skip(GlobalStore.SelectedEndpoint.BulkSize).ToList();
                                    }
                                }
                                if (remaining > 0 && !cancellationToken.IsCancellationRequested)
                                {
                                    var response = new ClientResponseWithObject<BulkResults>();
                                    try
                                    {
                                        var settings = new DocumentBulkSettings();
                                        settings.Documents = tokens.Take(remaining).Select(t => t.ToObject<object>()).ToList();
                                        response = new DocumentManager(GlobalStore.SelectedEndpoint, SelectedDataSet.Name).BulkDocumentsAsync(settings).Result;
                                        ResponseValidator.Validate(response, false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Messenger.Default.Send(new Exception(string.Format("Error during bulk process at range [{0}-{1}]", done, done + remaining), ex));
                                        status.ErrorCount += remaining;
                                    }
                                    finally
                                    {
                                        var bulkErrors = response.ResponseObject.Results.Where(br => br.StatusCode != (int)HttpStatusCode.OK).ToList();
                                        if (bulkErrors.Any())
                                        {
                                            foreach (var error in bulkErrors)
                                            {
                                                Messenger.Default.Send(new Exception(string.Format("Id: {0}, error: {1}", error.Id, error.Error)));
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
                        }
                    });

                }
                catch (Exception exception)
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
                }
                finally
                {
                    status.OperationIsFinished = true;
                }
            });
        }

        private async void Rename()
        {
            Log.Info(LogMessages.ManageDataSetRenameCommand);
            if (SelectedDataSet == null) return;
            var originalName = SelectedDataSet.Name;
            var context = new CommonDialogViewModel
            {
                Header = "Rename Dataset",
                Content = new JContent(originalName),
                Buttons = ButtonsEnum.OkCancel
            };
            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            await _dialogHandler.Show(view, "RootDialog", async (object s, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Ok)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessFul = true;
                        var errorMessage = "";
                        var newName = "";
                        try
                        {
                            newName = ((JContent)context.Content).GetJToken().ToString();
                            var response = await _dataSetManager.UpdateDataSetAsync(originalName, new DataSetUpdate { Name = newName });
                            ResponseValidator.Validate(response, false);
                        }
                        catch (Exception exception)
                        {
                            isSuccessFul = false;
                            errorMessage = exception.Message;
                        }
                        finally
                        {
                            if (!isSuccessFul)
                            {
                                context.ErrorMessage = errorMessage;
                                context.ShowError = true;
                                args.Session.UpdateContent(view);
                            }
                            else
                            {
                                var selectedIndex = DataSets.IndexOf(SelectedDataSet);
                                DataSets[selectedIndex].Name = newName;
                                DataSets = new ObservableCollection<DataSet>(DataSets);
                                Messenger.Default.Send(new UpdateMessage(UpdateType.DatasetRename, originalName));
                                canClose = true;
                                args.Session.Close((CommonDialogResult)args.Parameter);
                            }
                        }
                    }

                });
        }

        private async void Delete()
        {
            Log.Info(LogMessages.ManageDataSetDeleteCommand);
            if (SelectedDataSet == null) return;
            var context = new CommonDialogViewModel
            {
                Buttons = ButtonsEnum.YesNo,
                Header = "Delete dataset",
                Content = new JContent("Are you sure to remove the following data set: " + SelectedDataSet.Name)
            };
            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            var result = await _dialogHandler.Show(view, "RootDialog",
                async (object sender, DialogClosingEventArgs args) =>
                {
                    if (!canClose && (CommonDialogResult)args.Parameter == CommonDialogResult.Yes)
                    {
                        args.Cancel();
                        args.Session.UpdateContent(new ProgressDialog());
                        var isSuccessful = false;
                        var errorMessage = "";
                        try
                        {
                            var response = await _dataSetManager.DeleteDataSetAsync(SelectedDataSet.Name);
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
            if ((CommonDialogResult)result == CommonDialogResult.Yes)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() => DataSets.Remove(SelectedDataSet));
            }
        }

    }
}