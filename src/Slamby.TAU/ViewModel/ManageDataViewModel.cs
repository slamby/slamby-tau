using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json.Linq;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Design;
using Slamby.TAU.Enum;
using Slamby.TAU.Helper;
using Slamby.TAU.Model;
using System;
using System.Drawing.Text;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Slamby.TAU.Logger;
using Slamby.TAU.Resources;
using GalaSoft.MvvmLight.Threading;
using System.Windows.Input;
using Microsoft.Practices.ServiceLocation;
using NuGet;
using Slamby.SDK.Net.Managers.Interfaces;
using Slamby.SDK.Net.Models.Enums;
using Slamby.TAU.View;
using Clipboard = System.Windows.Clipboard;
using CommonDialog = Slamby.TAU.View.CommonDialog;
using Cursors = System.Windows.Input.Cursors;
using Message = Slamby.TAU.Model.Message;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ManageDataViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the ManageDataViewModel class.
        /// </summary>
        public ManageDataViewModel(DataSet dataSet, DialogHandler dialogHandler)
        {

            _documentManager = new DocumentManager(GlobalStore.SelectedEndpoint, dataSet.Name);
            _tagManager = new TagManager(GlobalStore.SelectedEndpoint, dataSet.Name);
            _currentDataSet = dataSet;
            _dialogHandler = dialogHandler;

            Tags = new ObservableCollection<Tag>();
            Documents = new ObservableCollection<object>();

            LoadMoreCommand = new RelayCommand(async () =>
            {
                await _dialogHandler.ShowProgress(null, async () =>
                {
                    await ScrollDocuments();
                });
            }, () => !string.IsNullOrEmpty(_filterScrollId));
            LoadedCommand = new RelayCommand(async () =>
            {
                Mouse.SetCursor(Cursors.Arrow);
                SetGridSettings();
                if (_loadedFirst && _tagManager != null && _documentManager != null)
                {
                    _loadedFirst = false;
                    await _dialogHandler.ShowProgress(null, async () =>
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() => Tags.Clear());
                        Log.Info(LogMessages.ManageDataLoadTags);
                        var response = await _tagManager.GetTagsAsync(true);
                        if (ResponseValidator.Validate(response))
                        {
                            DispatcherHelper.CheckBeginInvokeOnUI(() => Tags = new ObservableCollection<Tag>(response.ResponseObject));
                        }
                        DocumentProperties = new ObservableCollection<string>(_currentDataSet.SampleDocument != null ? ((JObject)_currentDataSet.SampleDocument).Properties().ToList().Select(p => p.Name) :
                            ((JObject)((JObject)_currentDataSet.Schema)["properties"]).Properties().ToList().Select(p => p.Name));
                        _activeSource = ActiveSourceEnum.Filter;
                        var filterSettings = new DocumentFilterSettings
                        {
                            Filter = new Filter(),
                            Pagination = new Pagination
                            {
                                Limit = ScrollSize,
                            }
                        };
                        await LoadDocuments(filterSettings);
                    });
                }
            });

            CopyToClipboardCommand = new RelayCommand(() =>
            {
                Clipboard.SetText("[ " + string.Join(", ", SelectedTags.Select(t => $"\"{t.Id}\"")) + " ]");
            });

            AddTagCommand = new RelayCommand(async () => await AddTag());
            RemoveTagCommand = new RelayCommand(RemoveTag);
            ModifyTagTagCommand = new RelayCommand(ModifyTag);
            ExportWordsCommand = new RelayCommand(async () => await ExportWords(), () => SelectedTags != null && SelectedTags.Any());

            AddDocumentCommand = new RelayCommand(async () => await AddDocument());
            DeleteDocumentCommand = new RelayCommand(DeleteDocument);
            AssignTagCommand = new RelayCommand(AssignTag);
            RemoveAssignedTagCommand = new RelayCommand(RemoveTags);
            ClearTagListCommand = new RelayCommand(ClearTags);
            CopyToCommand = new RelayCommand(CopyTo);
            MoveToCommand = new RelayCommand(MoveTo);
            CopyAllToCommand = new RelayCommand(CopyAllTo);
            MoveAllToCommand = new RelayCommand(MoveAllTo);
            ModifyDocumentCommand = new RelayCommand(ModifyDocument);

            SelectTagsForSampleCommand = new RelayCommand(SelectSampleTags);
            GetSampleCommand = new RelayCommand(GetSample);
            SelectTagsForFilterCommand = new RelayCommand(SelectFilterTags);
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ApplyFieldsCommand = new RelayCommand(() =>
              {
                  if (_activeSource == ActiveSourceEnum.Filter)
                      ApplyFilter();
                  else
                      GetSample();
              });
        }

        #region GridSettings

        private void SetGridSettings()
        {
            var gridSettingsDict = GlobalStore.GridSettingsDictionary;
            if (gridSettingsDict != null && gridSettingsDict.Any())
            {
                if (gridSettingsDict.ContainsKey("ManageData_Tags"))
                {
                    var tagsSettings = gridSettingsDict["ManageData_Tags"];
                    if (tagsSettings.ContainsKey(_currentDataSet.Name))
                    {
                        _tagGridSettingsLadedFromFile = true;
                        TagsGridSettings = tagsSettings[_currentDataSet.Name];
                    }
                    else
                    {
                        TagsGridSettings = null;
                    }
                }
                else
                {
                    TagsGridSettings = null;
                }

                if (gridSettingsDict.ContainsKey("ManageData_Documents"))
                {
                    var documentsSettings = gridSettingsDict["ManageData_Documents"];
                    if (documentsSettings.ContainsKey(_currentDataSet.Name))
                    {
                        _documentGridSettingsLadedFromFile = true;
                        DocumetsGridSettings = documentsSettings[_currentDataSet.Name];
                    }
                    else
                    {
                        DocumetsGridSettings = null;
                    }
                }
                else
                {
                    DocumetsGridSettings = null;
                }
            }
            else
            {
                TagsGridSettings = null;
                DocumetsGridSettings = null;
            }
        }

        private bool _documentGridSettingsLadedFromFile = false;
        private DataGridSettings _documetsGridSettings;

        public DataGridSettings DocumetsGridSettings
        {
            get
            {
                return _documetsGridSettings;
            }
            set
            {
                if (Set(() => DocumetsGridSettings, ref _documetsGridSettings, value))
                {
                    if (value != null && !_documentGridSettingsLadedFromFile)
                    {
                        GlobalStore.SaveGridSettings("ManageData_Documents", _currentDataSet.Name, value);
                    }
                    _documentGridSettingsLadedFromFile = false;
                }
            }
        }

        private bool _tagGridSettingsLadedFromFile = false;
        private DataGridSettings _tagsGridSettings;

        public DataGridSettings TagsGridSettings
        {
            get { return _tagsGridSettings; }
            set
            {
                if (Set(() => TagsGridSettings, ref _tagsGridSettings, value))
                {
                    if (value != null && !_tagGridSettingsLadedFromFile)
                    {
                        GlobalStore.SaveGridSettings("ManageData_Tags", _currentDataSet.Name, value);
                    }
                    _tagGridSettingsLadedFromFile = false;
                }
            }
        }

        #endregion

        public RelayCommand CopyToClipboardCommand { get; private set; }

        public RelayCommand LoadedCommand { get; private set; }

        public RelayCommand LoadMoreCommand { get; private set; }

        private bool _loadedFirst = true;

        private ITagManager _tagManager;
        private IDocumentManager _documentManager;
        private DataSet _currentDataSet;
        private DialogHandler _dialogHandler;


        private ObservableCollection<Tag> _tags;

        public ObservableCollection<Tag> Tags
        {
            get { return _tags; }
            set { Set(() => Tags, ref _tags, value); }
        }


        private ObservableCollection<object> _documents;

        public ObservableCollection<object> Documents
        {
            get { return _documents; }
            set { Set(() => Documents, ref _documents, value); }
        }


        private ObservableCollection<Tag> _selectedTags;

        public ObservableCollection<Tag> SelectedTags
        {
            get { return _selectedTags; }
            set
            {
                if (Set(() => SelectedTags, ref _selectedTags, value))
                {
                    ExportWordsCommand.RaiseCanExecuteChanged();
                }
            }
        }


        private ObservableCollection<object> _selectedDocuments;

        public ObservableCollection<object> SelectedDocuments
        {
            get { return _selectedDocuments; }
            set { Set(() => SelectedDocuments, ref _selectedDocuments, value); }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set { Set(() => IsBusy, ref _isBusy, value); }
        }



        #region Document filtering and sampling

        private ObservableCollection<object> _selectedDocumentProperties = new ObservableCollection<object>();

        public ObservableCollection<object> SelectedDocumentProperties
        {
            get { return _selectedDocumentProperties; }
            set { Set(() => SelectedDocumentProperties, ref _selectedDocumentProperties, value); }
        }

        private ObservableCollection<string> _documentProperties;

        public ObservableCollection<string> DocumentProperties
        {
            get { return _documentProperties; }
            set { Set(() => DocumentProperties, ref _documentProperties, value); }
        }

        private ActiveSourceEnum _activeSource = ActiveSourceEnum.Filter;

        private ObservableCollection<Tag> _selectedTagsForSample = new ObservableCollection<Tag>();

        private string _sampleSettingsId;

        private string _selectedLabelForSample = "All";

        public string SelectedLabelForSample
        {
            get { return _selectedLabelForSample; }
            set { Set(() => SelectedLabelForSample, ref _selectedLabelForSample, value); }
        }


        private int _sizeText = 100;

        public int SizeText
        {
            get { return _sizeText; }
            set { Set(() => SizeText, ref _sizeText, value); }
        }


        private bool _stratified;

        public bool Stratified
        {
            get { return _stratified; }
            set { Set(() => Stratified, ref _stratified, value); }
        }



        private bool _isFixSizeSample = true;

        public bool IsFixSizeSample
        {
            get { return _isFixSizeSample; }
            set { Set(() => IsFixSizeSample, ref _isFixSizeSample, value); }
        }


        public RelayCommand SelectTagsForSampleCommand { get; private set; }
        public RelayCommand GetSampleCommand { get; private set; }
        public RelayCommand ApplyFieldsCommand { get; private set; }


        private string _filterScrollId = null;


        private int _scrollSize = 50;

        public int ScrollSize
        {
            get { return _scrollSize; }
            set { Set(() => ScrollSize, ref _scrollSize, value); }
        }

        private ObservableCollection<Tag> _selectedTagsForFilter = new ObservableCollection<Tag>();

        private string _selectedLabelForFilter = "All";

        public string SelectedLabelForFilter
        {
            get { return _selectedLabelForFilter; }
            set { Set(() => SelectedLabelForFilter, ref _selectedLabelForFilter, value); }
        }

        private string _filter = "";

        public string Filter
        {
            get { return _filter; }
            set
            {
                Set(() => Filter, ref _filter, value);
            }
        }

        public RelayCommand SelectTagsForFilterCommand { get; private set; }

        public RelayCommand ApplyFilterCommand { get; private set; }

        private async void ApplyFilter()
        {
            Log.Info(LogMessages.ManageDataFilterApply);
            await _dialogHandler.ShowProgress(null, async () =>
            {
                _activeSource = ActiveSourceEnum.Filter;
                var filterSettings = new DocumentFilterSettings
                {
                    Filter = new Filter
                    {
                        TagIds = _selectedTagsForFilter?.Select(t => t.Id).ToList() ?? new List<string>(),
                        Query = Filter
                    },
                    Pagination = new Pagination { Limit = ScrollSize },
                    Order = new Order { OrderByField = _currentDataSet.IdField, OrderDirection = OrderDirectionEnum.Asc }
                };
                filterSettings.Fields = SelectedDocumentProperties.Count == DocumentProperties.Count
                    ? new List<string> { "*" }
                    : SelectedDocumentProperties.Count == 0 ? null : SelectedDocumentProperties.Select(o => o.ToString()).ToList();
                await LoadDocuments(filterSettings);
            });
        }

        private async void GetSample()
        {
            Log.Info(LogMessages.ManageDataSampleGet);
            await _dialogHandler.ShowProgress(null, async () =>
            {
                _activeSource = ActiveSourceEnum.Sample;
                _filterScrollId = null;
                DispatcherHelper.CheckBeginInvokeOnUI(() => LoadMoreCommand.RaiseCanExecuteChanged());
                _sampleSettingsId = Guid.NewGuid().ToString();
                var sampleSettings = new DocumentSampleSettings
                {
                    Id = _sampleSettingsId,
                    TagIds = _selectedTagsForSample?.Select(t => t.Id).ToList() ?? new List<string>(),
                    Percent = IsFixSizeSample ? 0 : SizeText,
                    Size = IsFixSizeSample ? SizeText : 0,
                    IsStratified = Stratified
                };
                sampleSettings.Fields = SelectedDocumentProperties.Count == DocumentProperties.Count
                    ? new List<string> { "*" }
                    : SelectedDocumentProperties.Count == 0 ? null : SelectedDocumentProperties.Select(o => o.ToString()).ToList();
                await LoadDocuments(sampleSettings);
            });
        }

        private async Task ScrollDocuments()
        {
            Log.Info(LogMessages.ManageDataLoadDocuments);
            var response = await _documentManager.GetFilteredDocumentsAsync(null, _filterScrollId);
            try
            {
                ResponseValidator.Validate(response, false);
                _filterScrollId = response.ResponseObject.Count == 0 ? null : response.ResponseObject.ScrollId;
                DispatcherHelper.CheckBeginInvokeOnUI(() => LoadMoreCommand.RaiseCanExecuteChanged());
                DispatcherHelper.CheckBeginInvokeOnUI(() => Documents.AddRange(response.ResponseObject.Items));
            }
            catch (Exception exception)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
            }
        }

        private async Task LoadDocuments(DocumentFilterSettings filterSettings)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => Documents.Clear());
            Log.Info(LogMessages.ManageDataLoadDocuments);
            var response = await _documentManager.GetFilteredDocumentsAsync(filterSettings, null);
            try
            {
                ResponseValidator.Validate(response, false);
                _filterScrollId = response.ResponseObject.Count == 0 ? null : response.ResponseObject.ScrollId;
                DispatcherHelper.CheckBeginInvokeOnUI(() => LoadMoreCommand.RaiseCanExecuteChanged());
                DispatcherHelper.CheckBeginInvokeOnUI(() => Documents = new ObservableCollection<object>(response.ResponseObject.Items));
            }
            catch (Exception exception)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() => Documents = new ObservableCollection<object>());
                DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
            }
        }

        private async Task LoadDocuments(DocumentSampleSettings sampleSettings)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => Documents.Clear());
            Log.Info(LogMessages.ManageDataLoadDocuments);
            var response = await _documentManager.GetSampleDocumentsAsync(sampleSettings);
            try
            {
                ResponseValidator.Validate(response, false);
                DispatcherHelper.CheckBeginInvokeOnUI(() => Documents = new ObservableCollection<object>(response.ResponseObject.Items));
            }
            catch (Exception exception)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(exception));
            }
        }

        #endregion

        #region Tag related commands and methods

        public RelayCommand AddTagCommand { get; private set; }

        public RelayCommand RemoveTagCommand { get; private set; }

        public RelayCommand ModifyTagTagCommand { get; private set; }

        public RelayCommand ExportWordsCommand { get; private set; }

        public async Task AddTag()
        {
            Log.Info(LogMessages.ManageDataTagAdd);

            var context = new CommonDialogViewModel
            {
                Header = "Add Tag",
                Buttons = ButtonsEnum.OkCancel,
                Content = new JContent(new Tag { Properties = new TagProperties() })
            };
            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            ClientResponseWithObject<Tag> response = null;
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
                            var newTag = ((JContent)context.Content).GetJToken().ToObject<Tag>();
                            response = await _tagManager.CreateTagAsync(newTag);
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
                Tags.Add(response.ResponseObject);
            }
        }

        private async void ModifyTag()
        {
            Log.Info(LogMessages.ManageDataTagModify);
            if (SelectedTags != null && SelectedTags.Any())
            {
                var selectedTag = SelectedTags.First();
                var id = selectedTag.Id;

                var context = new CommonDialogViewModel
                {
                    Header = "Modify Tag",
                    Buttons = ButtonsEnum.OkCancel,
                    Content = new JContent(selectedTag)
                };
                var view = new CommonDialog { DataContext = context };
                var canClose = false;
                ClientResponse response = null;
                Tag newTag = null;
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
                                newTag = ((JContent)context.Content).GetJToken().ToObject<Tag>();
                                response = await _tagManager.UpdateTagAsync(id, newTag);
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
                    var selectedList = SelectedTags.ToList();
                    Tags[Tags.IndexOf(selectedTag)] = newTag;
                    SelectedTags = new ObservableCollection<Tag>(selectedList);
                }
            }
        }

        private async void RemoveTag()
        {
            Log.Info(LogMessages.ManageDataTagRemove);
            if (SelectedTags != null && SelectedTags.Any())
            {
                var tagsToRemove = SelectedTags.ToList();
                var context = new CommonDialogViewModel
                {
                    Content = new Message("Would you like to clean the related documents tag list?"),
                    Buttons = ButtonsEnum.YesNoCancel
                };
                var view = new CommonDialog { DataContext = context };
                var result = await _dialogHandler.Show(view, "RootDialog");
                if ((CommonDialogResult)result != CommonDialogResult.Cancel)
                {
                    var cancellationToken = new CancellationTokenSource();
                    var status = new StatusDialogViewModel { Title = "Remove Tags", CancellationTokenSource = cancellationToken };
                    await _dialogHandler.Show(new StatusDialog { DataContext = status }, "RootDialog", async (object sender, DialogOpenedEventArgs oa) =>
                    {
                        try
                        {
                            var all = tagsToRemove.Count;
                            await Task.Run(() =>
                            {
                                try
                                {
                                    for (var done = 0; done < all && !cancellationToken.IsCancellationRequested;)
                                    {
                                        try
                                        {
                                            var response = _tagManager.DeleteTagAsync(tagsToRemove[done].Id, true, (CommonDialogResult)result == CommonDialogResult.Yes).Result;
                                            if (ResponseValidator.Validate(response, false))
                                            {
                                                var currentTag = tagsToRemove[done];
                                                DispatcherHelper.CheckBeginInvokeOnUI(() => Tags.Remove(currentTag));
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Messenger.Default.Send(new Exception(string.Format("Error during remove tag: name: {0} id: {1}", tagsToRemove[done].Name, tagsToRemove[done].Id), ex));
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
                            status.OperationIsFinished = true;
                        }
                    });

                }
            }
        }

        public async Task ExportWords()
        {
            Log.Info(LogMessages.ManageDataTagExportWords);

            var context = new CommonDialogViewModel
            {
                Header = "Export Words",
                Buttons = ButtonsEnum.OkCancel,
                Content = new JContent(new TagsExportWordsSettings { TagIdList = SelectedTags.Select(t => t.Id).ToList() })
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
                            var settings = ((JContent)context.Content).GetJToken().ToObject<TagsExportWordsSettings>();
                            response = await _tagManager.WordsExportAsync(settings);
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

        #endregion

        #region Document related commands and methods

        public RelayCommand AddDocumentCommand { get; private set; }
        public RelayCommand DeleteDocumentCommand { get; private set; }
        public RelayCommand AssignTagCommand { get; private set; }
        public RelayCommand RemoveAssignedTagCommand { get; private set; }
        public RelayCommand ClearTagListCommand { get; private set; }
        public RelayCommand CopyToCommand { get; private set; }
        public RelayCommand MoveToCommand { get; private set; }
        public RelayCommand CopyAllToCommand { get; private set; }
        public RelayCommand MoveAllToCommand { get; private set; }
        public RelayCommand ModifyDocumentCommand { get; set; }

        public async Task AddDocument()
        {
            Log.Info(LogMessages.ManageDataDocumentAdd);

            var context = new CommonDialogViewModel
            {
                Header = "Add Document",
                Buttons = ButtonsEnum.OkCancel,
                Content = new JContent(_currentDataSet.SampleDocument ?? new object())
            };
            var view = new CommonDialog { DataContext = context };
            var canClose = false;
            object newDocument = null;
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
                            newDocument = ((JContent)context.Content).GetJToken().ToObject<object>();
                            var response = await _documentManager.CreateDocumentAsync(newDocument);
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
                Documents.Add(newDocument);
            }

        }


        private async void ModifyDocument()
        {

            Log.Info(LogMessages.ManageDataDocumentModify);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                var selectedDocument = SelectedDocuments.First();
                var docId = ((JObject)selectedDocument)[_currentDataSet.IdField].ToString();
                await _dialogHandler.ShowProgress(null, async () =>
                {
                    var getFullDocumentResponse = await _documentManager.GetDocumentAsync(docId);
                    ResponseValidator.Validate(getFullDocumentResponse, false);
                    selectedDocument = getFullDocumentResponse.ResponseObject;
                });
                var context = new CommonDialogViewModel
                {
                    Header = "Modify Document",
                    Buttons = ButtonsEnum.OkCancel,
                    Content = new JContent(selectedDocument)
                };

                var view = new CommonDialog { DataContext = context };
                var canClose = false;
                ClientResponseWithObject<object> response = null;
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
                                var modified = ((JContent)context.Content).GetJToken().ToObject<object>();
                                response = (ClientResponseWithObject<object>)await _documentManager.UpdateDocumentAsync(docId, modified);
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
                    var selectedList = SelectedDocuments.ToList();
                    Documents[Documents.IndexOf(selectedDocument)] = response.ResponseObject;
                    selectedList.Remove(selectedDocument);
                    selectedList.Add(response.ResponseObject);
                    SelectedDocuments = new ObservableCollection<object>(selectedList);
                    Documents = new ObservableCollection<object>(Documents);
                }
            }
        }

        private async void DeleteDocument()
        {
            Log.Info(LogMessages.ManageDataDocumentRemove);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                var documentsToRemove = SelectedDocuments.ToList();
                var context = new CommonDialogViewModel
                {
                    Content = new Message(string.Format("Are you sure to delete {0} documents", SelectedDocuments.Count)),
                    Buttons = ButtonsEnum.YesNoCancel
                };

                var view = new CommonDialog { DataContext = context };
                var result = await _dialogHandler.Show(view, "RootDialog");
                if ((CommonDialogResult)result == CommonDialogResult.Yes)
                {
                    var cancellationToken = new CancellationTokenSource();
                    var status = new StatusDialogViewModel { Title = "Remove Documents", CancellationTokenSource = cancellationToken };
                    await _dialogHandler.Show(new StatusDialog { DataContext = status }, "RootDialog", async (object sender, DialogOpenedEventArgs oa) =>
                    {
                        try
                        {
                            var all = documentsToRemove.Count;
                            await Task.Run(() =>
                            {
                                try
                                {
                                    for (var done = 0; done < all && !cancellationToken.IsCancellationRequested;)
                                    {
                                        var docId = string.Empty;
                                        try
                                        {
                                            docId = ((JObject)documentsToRemove[done])[_currentDataSet.IdField].ToString();
                                            var response = _documentManager.DeleteDocumentAsync(docId).Result;
                                            if (ResponseValidator.Validate(response, false))
                                            {
                                                var currentDocument = documentsToRemove[done];
                                                DispatcherHelper.CheckBeginInvokeOnUI(() => Documents.Remove(currentDocument));
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Messenger.Default.Send(new Exception(string.Format("Error during remove document with id: {0}", docId), ex));
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
                            status.OperationIsFinished = true;
                        }
                    });

                }
            }
        }

        private async void AssignTag()
        {
            Log.Info(LogMessages.ManageDataDocumentAddTag);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                var context = new AssignTagDialogViewModel(Tags, new ObservableCollection<Tag>(), TagsGridSettings);
                var view = new AssignTagDialog { DataContext = context };
                if ((bool)await _dialogHandler.Show(view, "RootDialog") && context.SelectedTags != null && context.SelectedTags.Any())
                {
                    if (context.SelectedTags.Count > 1)
                    {
                        if (!SelectedDocuments.All(d => ((JObject)d)[_currentDataSet.TagField] is JArray))
                        {
                            await _dialogHandler.Show(new CommonDialog { DataContext = new CommonDialogViewModel { Header = "Warning", Content = new Message("Category field is not array"), Buttons = ButtonsEnum.Ok } }, "RootDialog");
                            return;
                        }
                    }


                    var selectedDocs = SelectedDocuments.ToList();
                    var cancellationToken = new CancellationTokenSource();
                    var status = new StatusDialogViewModel { Title = "Assign tags", CancellationTokenSource = cancellationToken };
                    await _dialogHandler.Show(new StatusDialog { DataContext = status }, "RootDialog", async (object sender, DialogOpenedEventArgs oa) =>
                    {
                        try
                        {
                            var all = selectedDocs.Count;
                            await Task.Run(() =>
                            {
                                try
                                {
                                    for (var done = 0; done < all && !cancellationToken.IsCancellationRequested;)
                                    {
                                        var docObject = (JObject)selectedDocs[done];
                                        var docId = docObject[_currentDataSet.IdField].ToString();
                                        try
                                        {
                                            if (docObject[_currentDataSet.TagField] is JArray)
                                            {
                                                var tags = (JArray)docObject[_currentDataSet.TagField];
                                                var originalTags = tags.ToObject<JArray>();
                                                context.SelectedTags.ToList().ForEach(t =>
                                                {
                                                    if (!tags.Any(j => j.ToString().Equals(t.Id.ToString())))
                                                        tags.Add(t.Id);
                                                });
                                                var modifiedTags = new JObject();
                                                modifiedTags.Add(_currentDataSet.TagField, tags);
                                                var modified = modifiedTags.ToObject<object>();
                                                var response = _documentManager.UpdateDocumentAsync(docId, modified).Result;
                                                if (!response.IsSuccessFul)
                                                {
                                                    tags = originalTags;
                                                    ResponseValidator.Validate(response, false);
                                                }
                                            }
                                            else
                                            {
                                                var modifiedTags = new JObject();
                                                modifiedTags.Add(_currentDataSet.TagField, context.SelectedTags.First().Id);
                                                var modified = modifiedTags.ToObject<object>();
                                                var response = _documentManager.UpdateDocumentAsync(docId, modified).Result;
                                                ResponseValidator.Validate(response, false);
                                                docObject[_currentDataSet.TagField] = context.SelectedTags.First().Id;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Messenger.Default.Send(new Exception(string.Format("Error during modify document with id: {0}", docId), ex));
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
                            Documents = new ObservableCollection<object>(Documents);
                            status.OperationIsFinished = true;
                        }
                    });

                }
            }
        }

        private async void RemoveTags()
        {
            Log.Info(LogMessages.ManageDataDocumentRemoveTags);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                var commonTags = new HashSet<string>();
                var firstDoc = SelectedDocuments.First();
                if (((JObject)firstDoc)[_currentDataSet.TagField] is JArray)
                {
                    var tags = (JArray)(((JObject)firstDoc)[_currentDataSet.TagField]);
                    tags.ToList().ForEach(t => commonTags.Add(t.ToObject<string>()));
                }
                else
                {
                    commonTags.Add(((JObject)firstDoc)[_currentDataSet.TagField].ToObject<string>());
                }
                foreach (var d in SelectedDocuments)
                {
                    if (((JObject)d)[_currentDataSet.TagField] is JArray)
                    {
                        var tags = (JArray)(((JObject)d)[_currentDataSet.TagField]);
                        commonTags = new HashSet<string>(tags.Select(t => t.ToString()).Intersect(commonTags));
                    }
                    else
                    {
                        var tag = ((JObject)d)[_currentDataSet.TagField].ToObject<string>();
                        if (string.IsNullOrEmpty(tag))
                        {
                            commonTags = new HashSet<string>();
                            break;
                        }
                        if (!commonTags.Contains(tag))
                        {
                            commonTags = new HashSet<string>();
                            break;
                        }
                    }
                }

                var context = new AssignTagDialogViewModel(new ObservableCollection<Tag>(Tags.Where(t => commonTags.Contains(t.Id))), new ObservableCollection<Tag>(), TagsGridSettings);
                var view = new AssignTagDialog { DataContext = context };
                if ((bool)await _dialogHandler.Show(view, "RootDialog") && context.SelectedTags != null && context.SelectedTags.Any())
                {
                    var selectedDocs = SelectedDocuments.ToList();
                    var cancellationToken = new CancellationTokenSource();
                    var status = new StatusDialogViewModel { Title = "Remove Tags", CancellationTokenSource = cancellationToken };
                    await _dialogHandler.Show(new StatusDialog { DataContext = status }, "RootDialog", async (object sender, DialogOpenedEventArgs oa) =>
                    {
                        try
                        {
                            var all = selectedDocs.Count;
                            await Task.Run(() =>
                            {
                                try
                                {
                                    for (var done = 0; done < all && !cancellationToken.IsCancellationRequested;)
                                    {
                                        var docObject = (JObject)selectedDocs[done];
                                        var docId = docObject[_currentDataSet.IdField].ToString();
                                        try
                                        {
                                            if (docObject[_currentDataSet.TagField] is JArray)
                                            {
                                                var tags = (JArray)docObject[_currentDataSet.TagField];
                                                var originalTags = tags.ToObject<JArray>();
                                                context.SelectedTags.ToList().ForEach(t =>
                                                {
                                                    var match = tags.FirstOrDefault(j => j.ToString().Equals(t.Id));
                                                    if (match != null)
                                                        tags.Remove(match);
                                                });
                                                var modifiedTags = new JObject();
                                                modifiedTags.Add(_currentDataSet.TagField, tags);
                                                var modified = modifiedTags.ToObject<object>();
                                                var response = _documentManager.UpdateDocumentAsync(docId, modified).Result;
                                                if (!response.IsSuccessFul)
                                                {
                                                    tags = originalTags;
                                                    ResponseValidator.Validate(response, false);
                                                }
                                            }
                                            else
                                            {
                                                var modifiedTags = new JObject();
                                                modifiedTags.Add(_currentDataSet.TagField, "");
                                                var modified = modifiedTags.ToObject<object>();
                                                var response = _documentManager.UpdateDocumentAsync(docId, modified).Result;
                                                ResponseValidator.Validate(response, false);
                                                docObject[_currentDataSet.TagField] = "";
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Messenger.Default.Send(new Exception(string.Format("Error during modify document with id: {0}", docId), ex));
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
                            Documents = new ObservableCollection<object>(Documents);
                            status.OperationIsFinished = true;
                        }
                    });
                }
            }
        }

        private async void ClearTags()
        {
            Log.Info(LogMessages.ManageDataDocumentRemoveTags);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                var context = new CommonDialogViewModel
                {
                    Content = new Message("Are you sure to clear tags?"),
                    Buttons = ButtonsEnum.YesNoCancel
                };
                var view = new CommonDialog { DataContext = context };
                var result = await _dialogHandler.Show(view, "RootDialog");
                if ((CommonDialogResult)result == CommonDialogResult.Yes)
                {
                    var selectedDocs = SelectedDocuments.ToList();
                    var cancellationToken = new CancellationTokenSource();
                    var status = new StatusDialogViewModel { Title = "Clear Tags", CancellationTokenSource = cancellationToken };
                    await _dialogHandler.Show(new StatusDialog { DataContext = status }, "RootDialog", async (object sender, DialogOpenedEventArgs oa) =>
                    {
                        try
                        {
                            var all = selectedDocs.Count;
                            await Task.Run(() =>
                            {
                                try
                                {
                                    for (var done = 0; done < all && !cancellationToken.IsCancellationRequested;)
                                    {
                                        var docObject = (JObject)selectedDocs[done];
                                        var docId = docObject[_currentDataSet.IdField].ToString();
                                        try
                                        {
                                            if (docObject[_currentDataSet.TagField] is JArray)
                                            {
                                                var tags = (JArray)docObject[_currentDataSet.TagField];
                                                var originalTags = tags.ToObject<JArray>();
                                                tags.RemoveAll();
                                                var modifiedTags = new JObject();
                                                modifiedTags.Add(_currentDataSet.TagField, tags);
                                                var modified = modifiedTags.ToObject<object>();
                                                var response = _documentManager.UpdateDocumentAsync(docId, modified).Result;
                                                if (!response.IsSuccessFul)
                                                {
                                                    tags = originalTags;
                                                    ResponseValidator.Validate(response, false);
                                                }
                                            }
                                            else
                                            {
                                                var modifiedTags = new JObject();
                                                modifiedTags.Add(_currentDataSet.TagField, "");
                                                var modified = modifiedTags.ToObject<object>();
                                                var response = _documentManager.UpdateDocumentAsync(docId, modified).Result;
                                                ResponseValidator.Validate(response, false);
                                                docObject[_currentDataSet.TagField] = "";
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Messenger.Default.Send(new Exception(string.Format("Error during modify document with id: {0}", docId), ex));
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
                            Documents = new ObservableCollection<object>(Documents);
                            status.OperationIsFinished = true;
                        }
                    });
                }
            }
        }

        private async void CopyTo()
        {
            Log.Info(LogMessages.ManageDataDocumentCopyTo);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                IEnumerable<DataSet> datasets = ServiceLocator.Current.GetInstance<ManageDataSetViewModel>().DataSets;

                var context = new DataSetSelectorViewModel { DataSets = new ObservableCollection<DataSet>(datasets.Where(ds => ds.Name != _currentDataSet.Name)) };
                var view = new DataSetSelector { DataContext = context };
                if ((bool)await _dialogHandler.Show(view, "RootDialog"))
                {
                    await _dialogHandler.ShowProgress(null, async () =>
                    {
                        await CopyDocuments(SelectedDocuments.Select(d => ((JObject)d)[_currentDataSet.IdField].ToString()).ToList(), context.SelectedDataSet.Name);
                    });
                }
            }
        }

        private async void MoveTo()
        {
            Log.Info(LogMessages.ManageDataDocumentMoveTo);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                IEnumerable<DataSet> datasets = ServiceLocator.Current.GetInstance<ManageDataSetViewModel>().DataSets;

                var context = new DataSetSelectorViewModel { DataSets = new ObservableCollection<DataSet>(datasets.Where(ds => ds.Name != _currentDataSet.Name)) };
                var view = new DataSetSelector { DataContext = context };
                if ((bool)await _dialogHandler.Show(view, "RootDialog"))
                {
                    await _dialogHandler.ShowProgress(null, async () =>
                    {
                        await MoveDocuments(SelectedDocuments.Select(d => ((JObject)d)[_currentDataSet.IdField].ToString()).ToList(), context.SelectedDataSet.Name);
                    });
                }
            }
        }

        private async void CopyAllTo()
        {
            Log.Info(LogMessages.ManageDataDocumentCopyAllTo);

            IEnumerable<DataSet> datasets = ServiceLocator.Current.GetInstance<ManageDataSetViewModel>().DataSets;

            var context = new DataSetSelectorViewModel { DataSets = new ObservableCollection<DataSet>(datasets.Where(ds => ds.Name != _currentDataSet.Name)) };
            var view = new DataSetSelector { DataContext = context };
            if ((bool)await _dialogHandler.Show(view, "RootDialog"))
            {
                await _dialogHandler.ShowProgress(null, async () =>
                {
                    var documentIds = await GetAllDocumentIdsByCurrentSettings();
                    await CopyDocuments(documentIds, context.SelectedDataSet.Name);
                });
            }
        }

        private async void MoveAllTo()
        {
            IEnumerable<DataSet> datasets = ServiceLocator.Current.GetInstance<ManageDataSetViewModel>().DataSets;

            var context = new DataSetSelectorViewModel { DataSets = new ObservableCollection<DataSet>(datasets.Where(ds => ds.Name != _currentDataSet.Name)) };
            var view = new DataSetSelector { DataContext = context };
            if ((bool)await _dialogHandler.Show(view, "RootDialog"))
            {
                await _dialogHandler.ShowProgress(null, async () =>
                {
                    var documentIds = await GetAllDocumentIdsByCurrentSettings();
                    await MoveDocuments(documentIds, context.SelectedDataSet.Name);
                });
            }
        }

        private async Task<List<string>> GetAllDocumentIdsByCurrentSettings()
        {
            //get documents
            var documentIds = new List<string>();

            ClientResponseWithObject<PaginatedList<object>> response = null;
            if (_activeSource == ActiveSourceEnum.Filter)
            {
                var filterSettings = new DocumentFilterSettings
                {
                    Filter = new Filter
                    {
                        TagIds = _selectedTagsForFilter?.Select(t => t.Id).ToList() ?? new List<string>(),
                        Query = Filter
                    },
                    Pagination = new Pagination { Limit = -1 },
                    Fields = new List<string> { _currentDataSet.IdField }
                };
                response = await _documentManager.GetFilteredDocumentsAsync(filterSettings, null);
            }
            else if (_activeSource == ActiveSourceEnum.Sample)
            {
                var sampleSettings = new DocumentSampleSettings
                {
                    Id = _sampleSettingsId,
                    TagIds = _selectedTagsForSample?.Select(t => t.Id).ToList() ?? new List<string>(),
                    Percent = IsFixSizeSample ? 0 : SizeText,
                    Size = IsFixSizeSample ? SizeText : 0,
                    IsStratified = Stratified,
                    Fields = new List<string> { _currentDataSet.IdField }
                };
                response = await _documentManager.GetSampleDocumentsAsync(sampleSettings);
            }
            ResponseValidator.Validate(response, false);
            documentIds = response.ResponseObject.Items.Select(d => ((JObject)d)[_currentDataSet.IdField].ToString()).ToList();
            return documentIds;
        }

        private async Task CopyDocuments(List<string> docIdList, string targetDataSetName)
        {
            var response = await _documentManager.CopyDocumentsToAsync(new DocumentCopySettings
            {
                Ids = docIdList,
                TargetDataSetName = targetDataSetName
            });
            ResponseValidator.Validate(response, false);
        }

        private async Task MoveDocuments(List<string> docIdList, string targetDataSetName)
        {
            var response = await _documentManager.MoveDocumentsToAsync(new DocumentMoveSettings
            {
                Ids = docIdList,
                TargetDataSetName = targetDataSetName
            });
            ResponseValidator.Validate(response, false);
            if (_activeSource == ActiveSourceEnum.Filter)
            {
                var filterSettings = new DocumentFilterSettings
                {
                    Filter = new Filter
                    {
                        TagIds = _selectedTagsForFilter?.Select(t => t.Id).ToList() ?? new List<string>(),
                        Query = Filter
                    },
                    Pagination = new Pagination { Limit = ScrollSize },
                    Order = new Order { OrderByField = _currentDataSet.IdField, OrderDirection = OrderDirectionEnum.Asc }
                };
                await LoadDocuments(filterSettings);
            }
            else
            {
                Documents = new ObservableCollection<object>();
            }
        }

        private async void SelectSampleTags()
        {
            Log.Info(LogMessages.ManageDataSampleTagSelect);
            var context = new AssignTagDialogViewModel(Tags, new ObservableCollection<Tag>(_selectedTagsForSample), TagsGridSettings);
            context.SelectedTags = _selectedTagsForSample;
            var view = new AssignTagDialog { DataContext = context };
            if ((bool)await _dialogHandler.Show(view, "RootDialog"))
            {
                _selectedTagsForSample = context.SelectedTags;
                SelectedLabelForSample = _selectedTagsForSample == null || !_selectedTagsForSample.Any()
                    ? "All"
                    : _selectedTagsForSample.Count == Tags.Count ? "All" : _selectedTagsForSample.Count.ToString();
            }
        }

        private async void SelectFilterTags()
        {
            Log.Info(LogMessages.ManageDataFilterTagSelect);
            var context = new AssignTagDialogViewModel(Tags, new ObservableCollection<Tag>(_selectedTagsForFilter), TagsGridSettings);
            context.SelectedTags = _selectedTagsForFilter;
            var view = new AssignTagDialog { DataContext = context };
            if ((bool)await _dialogHandler.Show(view, "RootDialog"))
            {
                _selectedTagsForFilter = context.SelectedTags;
                SelectedLabelForFilter = _selectedTagsForFilter == null || !_selectedTagsForFilter.Any()
                    ? "All"
                    : _selectedTagsForFilter.Count == Tags.Count ? "All" : _selectedTagsForFilter.Count.ToString();
            }
        }

        #endregion
    }
}