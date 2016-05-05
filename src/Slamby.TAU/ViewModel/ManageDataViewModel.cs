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
using Slamby.TAU.Logger;
using Slamby.TAU.Resources;
using GalaSoft.MvvmLight.Threading;
using System.Windows.Input;
using Microsoft.Practices.ServiceLocation;
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
        public ManageDataViewModel(IDocumentManager documentManager, ITagManager tagManager, DialogHandler dialogHandler)
        {

            _documentManager = documentManager;
            _tagManager = tagManager;
            _currentDataSet = GlobalStore.CurrentDataset;
            _dialogHandler = dialogHandler;

            Tags = new ObservableCollection<Tag>();
            Documents = new ObservableCollection<object>();

            LoadedCommand = new RelayCommand(async () =>
            {
                Mouse.SetCursor(Cursors.Arrow);
                if (_loadedFirst && _tagManager != null && _documentManager != null)
                {
                    _currentDataSet = GlobalStore.CurrentDataset;
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
                        _activeSource = ActiveSourceEnum.Filter;
                        _filterSettings = new DocumentFilterSettings
                        {
                            Filter = new Filter(),
                            Pagination = new Pagination
                            {
                                Limit = 50,
                                OrderByField = _currentDataSet.IdField
                            }
                        };
                        _sampleSettings = new DocumentSampleSettings
                        {
                            Id = Guid.NewGuid().ToString(),
                            Pagination = new Pagination
                            {
                                Limit = 50
                            }
                        };
                        await LoadDocuments();
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
            ExportWordsCommand = new RelayCommand(async () => await ExportWords());

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

            DocumentPaginatorViewModel = new PaginatorViewModel
            {
                LoadCommand = new RelayCommand<Pagination>(async p =>
                {
                    if (_activeSource == ActiveSourceEnum.Filter)
                        _filterSettings.Pagination = p;
                    else if (_activeSource == ActiveSourceEnum.Sample)
                        _sampleSettings.Pagination = p;
                    await _dialogHandler.ShowProgress(null, async () =>
                    {
                        await LoadDocuments();
                    });
                }),
                PaginatedList = new PaginatedList<object> { Pagination = new Pagination { Limit = 50 } }
            };
        }

        public RelayCommand CopyToClipboardCommand { get; private set; }

        public RelayCommand LoadedCommand { get; private set; }

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
                Set(() => SelectedTags, ref _selectedTags, value);
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



        #region Document filtering and paging

        private ActiveSourceEnum _activeSource = ActiveSourceEnum.Filter;

        private PaginatorViewModel _documentPaginatorViewModel;

        public PaginatorViewModel DocumentPaginatorViewModel
        {
            get { return _documentPaginatorViewModel; }
            set { Set(() => DocumentPaginatorViewModel, ref _documentPaginatorViewModel, value); }
        }

        private DocumentSampleSettings _sampleSettings;

        private ObservableCollection<Tag> _selectedTagsForSample = new ObservableCollection<Tag>();

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


        private DocumentFilterSettings _filterSettings;

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
            _activeSource = ActiveSourceEnum.Filter;
            await _dialogHandler.ShowProgress(null, async () =>
            {
                _filterSettings.Filter.Query = Filter;
                if (_selectedTagsForFilter != null)
                    _filterSettings.Filter.TagIds = _selectedTagsForFilter.Select(t => t.Id).ToList();
                _filterSettings.Pagination.Offset = 0;
                await LoadDocuments();
            });
        }

        private async void GetSample()
        {
            Log.Info(LogMessages.ManageDataSampleGet);
            _activeSource = ActiveSourceEnum.Sample;
            await _dialogHandler.ShowProgress(null, async () =>
            {
                _sampleSettings.Id = Guid.NewGuid().ToString();
                _sampleSettings.Pagination.Offset = 0;
                _sampleSettings.IsStratified = Stratified;
                if (IsFixSizeSample)
                {
                    _sampleSettings.Percent = 0;
                    _sampleSettings.Size = SizeText;
                }
                else
                {
                    _sampleSettings.Percent = SizeText;
                    _sampleSettings.Size = 0;
                }

                if (_selectedTagsForSample != null)
                    _sampleSettings.TagIds = _selectedTagsForSample.Select(t => t.Id).ToList();
                await LoadDocuments();
            });
        }

        private async Task LoadDocuments()
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => Documents.Clear());
            Log.Info(LogMessages.ManageDataLoadDocuments);
            if (_activeSource == ActiveSourceEnum.Filter)
            {
                var response = await _documentManager.GetFilteredDocumentsAsync(_filterSettings);
                if (ResponseValidator.Validate(response))
                {
                    _filterSettings.Pagination = response.ResponseObject.Count == 0 ? new Pagination { Limit = 50 } : response.ResponseObject.Pagination;
                    DocumentPaginatorViewModel.PaginatedList = response.ResponseObject.Count == 0 ? new PaginatedList<object> { Pagination = new Pagination { Limit = 50 } } : response.ResponseObject;
                    Documents = new ObservableCollection<object>(response.ResponseObject.Items);
                }
                else
                {
                    DocumentPaginatorViewModel.PaginatedList = new PaginatedList<object> { Pagination = new Pagination { Limit = 50 } };
                }
            }
            else if (_activeSource == ActiveSourceEnum.Sample)
            {
                var response = await _documentManager.GetSampleDocumentsAsync(_sampleSettings);
                if (ResponseValidator.Validate(response))
                {
                    _sampleSettings.Pagination = response.ResponseObject.Count == 0 ? new Pagination { Limit = 50 } : response.ResponseObject.Pagination;
                    DocumentPaginatorViewModel.PaginatedList = response.ResponseObject.Count == 0 ? new PaginatedList<object> { Pagination = new Pagination { Limit = 50 } } : response.ResponseObject;
                    Documents = new ObservableCollection<object>(response.ResponseObject.Items);
                }
                else
                {
                    DocumentPaginatorViewModel.PaginatedList = new PaginatedList<object> { Pagination = new Pagination { Limit = 50 } };
                }
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
            var result = await _dialogHandler.Show(new CommonDialog { DataContext = context }, "RootDialog");
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                var newTag = ((JContent)context.Content).GetJToken().ToObject<Tag>();
                var response = await _tagManager.CreateTagAsync(newTag);
                if (ResponseValidator.Validate(response))
                {
                    Tags.Add(response.ResponseObject);
                }
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
                var result = await _dialogHandler.Show(new CommonDialog { DataContext = context }, "RootDialog");
                if ((CommonDialogResult)result == CommonDialogResult.Ok)
                {
                    var newTag = ((JContent)context.Content).GetJToken().ToObject<Tag>();
                    var response = await _tagManager.UpdateTagAsync(id, newTag);
                    if (ResponseValidator.Validate(response))
                    {
                        var selectedList = SelectedTags.ToList();
                        Tags[Tags.IndexOf(selectedTag)] = newTag;
                        SelectedTags = new ObservableCollection<Tag>(selectedList);
                    }
                }
            }
        }

        private async void RemoveTag()
        {

            Log.Info(LogMessages.ManageDataTagRemove);
            if (SelectedTags != null && SelectedTags.Any())
            {
                var context = new CommonDialogViewModel
                {
                    Content = new Model.Message("Would you like to clean the related documents tag list?"),
                    Buttons = ButtonsEnum.YesNoCancel
                };
                var view = new CommonDialog { DataContext = context };
                var result = await _dialogHandler.Show(view, "RootDialog");
                if ((CommonDialogResult)result != CommonDialogResult.Cancel)
                {
                    await _dialogHandler.ShowProgress(null,
                        async () =>
                        {
                            foreach (var selectedTag in SelectedTags)
                            {
                                var response = await _tagManager.DeleteTagAsync(selectedTag.Id, true, (CommonDialogResult)result == CommonDialogResult.Yes);
                                if (ResponseValidator.Validate(response))
                                {
                                    DispatcherHelper.CheckBeginInvokeOnUI(() => Tags.Remove(selectedTag));
                                }
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
            var result = await _dialogHandler.Show(new CommonDialog { DataContext = context }, "RootDialog");
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                var settings = ((JContent)context.Content).GetJToken().ToObject<TagsExportWordsSettings>();
                var response = await _tagManager.WordsExportAsync(settings);
                if (ResponseValidator.Validate(response))
                {
                    Messenger.Default.Send(new UpdateMessage(UpdateType.NewProcessCreated, response.ResponseObject));
                }
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
                Content = new JContent(_currentDataSet.SampleDocument)
            };
            var result = await _dialogHandler.Show(new CommonDialog { DataContext = context }, "RootDialog");
            if ((CommonDialogResult)result == CommonDialogResult.Ok)
            {
                await _dialogHandler.ShowProgress(null, async () =>
                 {
                     var newDocument = ((JContent)context.Content).GetJToken().ToObject<object>();
                     var response = await _documentManager.CreateDocumentAsync(newDocument);
                     if (ResponseValidator.Validate(response))
                     {
                         DispatcherHelper.CheckBeginInvokeOnUI(() => Documents.Add(newDocument));
                     }
                 });
            }

        }


        private async void ModifyDocument()
        {

            Log.Info(LogMessages.ManageDataDocumentModify);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                var selectedDocument = SelectedDocuments.First();
                var docId = ((JObject)selectedDocument)[_currentDataSet.IdField].ToString();
                var context = new CommonDialogViewModel
                {
                    Header = "Modify Document",
                    Buttons = ButtonsEnum.OkCancel,
                    Content = new JContent(selectedDocument)
                };
                var result = await _dialogHandler.Show(new CommonDialog { DataContext = context }, "RootDialog");
                if ((CommonDialogResult)result == CommonDialogResult.Ok)
                {
                    var newDocument = ((JContent)context.Content).GetJToken().ToObject<object>();
                    var response = await _documentManager.UpdateDocumentAsync(docId, newDocument);
                    if (ResponseValidator.Validate(response))
                    {
                        var selectedList = SelectedDocuments.ToList();
                        Documents[Documents.IndexOf(selectedDocument)] = newDocument;
                        selectedList.Remove(selectedDocument);
                        selectedList.Add(newDocument);
                        SelectedDocuments = new ObservableCollection<object>(selectedList);
                        Documents = new ObservableCollection<object>(Documents);
                    }
                }
            }
        }

        private async void DeleteDocument()
        {
            Log.Info(LogMessages.ManageDataDocumentRemove);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                var context = new CommonDialogViewModel
                {
                    Content = new Message(string.Format("Are you sure to delete {0} documents", SelectedDocuments.Count)),
                    Buttons = ButtonsEnum.YesNoCancel
                };
                var view = new CommonDialog { DataContext = context };
                var result = await _dialogHandler.Show(view, "RootDialog");
                if ((CommonDialogResult)result == CommonDialogResult.Yes)
                {
                    await _dialogHandler.ShowProgress(null,
                        async () =>
                        {
                            var deletedDocs = new List<object>();
                            foreach (var selectedDocument in SelectedDocuments)
                            {
                                var docId = ((JObject)selectedDocument)[_currentDataSet.IdField].ToString();
                                var response = await _documentManager.DeleteDocumentAsync(docId);
                                if (ResponseValidator.Validate(response))
                                {
                                    deletedDocs.Add(selectedDocument);
                                }
                            }
                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                deletedDocs.ForEach(dd =>
                                {
                                    SelectedDocuments.Remove(dd);
                                    Documents.Remove(dd);
                                })
                            );
                        });
                }
            }
        }

        private async void AssignTag()
        {
            Log.Info(LogMessages.ManageDataDocumentAddTag);
            if (SelectedDocuments != null && SelectedDocuments.Any())
            {
                var context = new AssignTagDialogViewModel(Tags, new ObservableCollection<Tag>());
                var view = new AssignTagDialog { DataContext = context };
                if ((bool)await _dialogHandler.Show(view, "RootDialog") && context.SelectedTags != null && context.SelectedTags.Any())
                {
                    if (!(((JObject)SelectedDocuments.First())[_currentDataSet.TagField] is JArray) &&
                        context.SelectedTags.Count > 1)
                    {
                        await _dialogHandler.Show(new CommonDialog { DataContext = new CommonDialogViewModel { Header = "Warning", Content = new Message("Category field is not array"), Buttons = ButtonsEnum.Ok } }, "RootDialog");
                        return;
                    }
                    SelectedDocuments.ToList().ForEach(async d =>
                    {
                        var docObject = (JObject)d;
                        var docId = docObject[_currentDataSet.IdField].ToString();
                        if (docObject[_currentDataSet.TagField] is JArray)
                        {
                            var tags = (JArray)docObject[_currentDataSet.TagField];
                            context.SelectedTags.ToList().ForEach(t =>
                            {
                                if (!tags.Any(j => j.ToString().Equals(t.Id.ToString())))
                                    tags.Add(t.Id);
                            });
                            var newDocument = JObject.Parse(d.ToString()).ToObject<object>();
                            var response = await _documentManager.UpdateDocumentAsync(docId, newDocument);
                            if (ResponseValidator.Validate(response))
                            {
                                Documents = new ObservableCollection<object>(Documents);
                            }
                        }
                        else
                        {
                            docObject[_currentDataSet.TagField] = context.SelectedTags.First().Id;
                            var newDocument = docObject.ToObject<object>();
                            var response = await _documentManager.UpdateDocumentAsync(docId, newDocument);
                            if (ResponseValidator.Validate(response))
                            {
                                Documents = new ObservableCollection<object>(Documents);
                            }
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
                SelectedDocuments.ToList().ForEach(d =>
                {
                    if (((JObject)d)[_currentDataSet.TagField] is JArray)
                    {
                        var tags = (JArray)(((JObject)d)[_currentDataSet.TagField]);
                        tags.ToList().ForEach(t => commonTags.Add(t.ToObject<string>()));
                    }
                    else
                    {
                        commonTags.Add(((JObject)d)[_currentDataSet.TagField].ToObject<string>());
                    }
                });
                var context = new AssignTagDialogViewModel(new ObservableCollection<Tag>(Tags.Where(t => commonTags.Contains(t.Id))), new ObservableCollection<Tag>());
                var view = new AssignTagDialog { DataContext = context };
                if ((bool)await _dialogHandler.Show(view, "RootDialog") && context.SelectedTags != null && context.SelectedTags.Any())
                {
                    SelectedDocuments.ToList().ForEach(async d =>
                    {
                        var docObject = (JObject)d;
                        var docId = docObject[_currentDataSet.IdField].ToString();
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
                            var newDocument = JObject.Parse(d.ToString()).ToObject<object>();
                            var response = await _documentManager.UpdateDocumentAsync(docId, newDocument);
                            if (ResponseValidator.Validate(response))
                            {
                                Documents = new ObservableCollection<object>(Documents);
                            }
                            else
                            {
                                tags = originalTags;
                            }
                        }
                        else
                        {
                            docObject[_currentDataSet.TagField] = "";
                            var newDocument = docObject.ToObject<object>();
                            var response = await _documentManager.UpdateDocumentAsync(docId, newDocument);
                            if (ResponseValidator.Validate(response))
                            {
                                Documents = new ObservableCollection<object>(Documents);
                            }
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
                    var selectedList = SelectedDocuments.ToList();
                    SelectedDocuments.ToList().ForEach(async d =>
                    {
                        var docObject = (JObject)d;
                        var docId = docObject[_currentDataSet.IdField].ToString();
                        if (docObject[_currentDataSet.TagField] is JArray)
                        {
                            var tags = (JArray)docObject[_currentDataSet.TagField];
                            var originalTags = tags.ToObject<JArray>();
                            tags.RemoveAll();
                            var newDocument = JObject.Parse(d.ToString()).ToObject<object>();
                            var response = await _documentManager.UpdateDocumentAsync(docId, newDocument);
                            if (ResponseValidator.Validate(response))
                            {
                                Documents = new ObservableCollection<object>(Documents);
                            }
                            else
                            {
                                tags = originalTags;
                            }
                        }
                        else
                        {
                            docObject[_currentDataSet.TagField] = "";
                            var newDocument = docObject.ToObject<object>();
                            var response = await _documentManager.UpdateDocumentAsync(docId, newDocument);
                            if (ResponseValidator.Validate(response))
                            {
                                Documents = new ObservableCollection<object>(Documents);
                            }
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
                    await CopyDocuments(SelectedDocuments.Select(d => ((JObject)d)[_currentDataSet.IdField].ToString()).ToList(), context.SelectedDataSet.Name);
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
                    await MoveDocuments(SelectedDocuments.Select(d => ((JObject)d)[_currentDataSet.IdField].ToString()).ToList(), context.SelectedDataSet.Name);
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
                var documentIds = await GetAllDocumentIdsByCurrentSettings();
                await CopyDocuments(documentIds, context.SelectedDataSet.Name);
            }
        }

        private async void MoveAllTo()
        {
            IEnumerable<DataSet> datasets = ServiceLocator.Current.GetInstance<ManageDataSetViewModel>().DataSets;

            var context = new DataSetSelectorViewModel { DataSets = new ObservableCollection<DataSet>(datasets.Where(ds => ds.Name != _currentDataSet.Name)) };
            var view = new DataSetSelector { DataContext = context };
            if ((bool)await _dialogHandler.Show(view, "RootDialog"))
            {
                var documentIds = await GetAllDocumentIdsByCurrentSettings();
                await MoveDocuments(documentIds, context.SelectedDataSet.Name);
            }
        }

        private async Task<List<string>> GetAllDocumentIdsByCurrentSettings()
        {
            //get documents
            var documentIds = new List<string>();
            await _dialogHandler.ShowProgress(null, async () =>
            {
                ClientResponseWithObject<PaginatedList<object>> response = null;
                if (_activeSource == ActiveSourceEnum.Filter)
                {
                    var allFilterSettings = new DocumentFilterSettings()
                    {
                        Pagination = new Pagination
                        {
                            Limit = -1,
                            Offset = 0,
                            OrderByField = _currentDataSet.IdField
                        },
                        Filter = _filterSettings.Filter,
                        IdsOnly = true
                    };
                    response = await _documentManager.GetFilteredDocumentsAsync(allFilterSettings);
                }
                else if (_activeSource == ActiveSourceEnum.Sample)
                {
                    var allSampleSettings = new DocumentSampleSettings()
                    {
                        Pagination = new Pagination
                        {
                            Limit = -1,
                            Offset = 0,
                            OrderByField = _currentDataSet.IdField
                        },
                        TagIds = _sampleSettings.TagIds,
                        Id = _sampleSettings.Id,
                        IsStratified = _sampleSettings.IsStratified,
                        Percent = _sampleSettings.Percent,
                        Size = _sampleSettings.Size,
                        IdsOnly = true
                    };
                    response = await _documentManager.GetSampleDocumentsAsync(allSampleSettings);
                }
                if (ResponseValidator.Validate(response))
                {
                    documentIds = response.ResponseObject.Items.Select(d => ((JObject)d)[_currentDataSet.IdField].ToString()).ToList();
                }
            });
            return documentIds;
        }

        private async Task CopyDocuments(List<string> docIdList, string targetDataSetName)
        {
            await _dialogHandler.ShowProgress(null,
                    async () =>
                    {
                        var response = await _documentManager.CopyDocumentsToAsync(new DocumentCopySettings
                        {
                            Ids = docIdList,
                            TargetDataSetName = targetDataSetName
                        });
                        ResponseValidator.Validate(response);
                    });
        }

        private async Task MoveDocuments(List<string> docIdList, string targetDataSetName)
        {
            await _dialogHandler.ShowProgress(null,
                        async () =>
                        {
                            var response = await _documentManager.MoveDocumentsToAsync(new DocumentMoveSettings
                            {
                                Ids = docIdList,
                                TargetDataSetName = targetDataSetName
                            });
                            if (ResponseValidator.Validate(response))
                            {
                                await LoadDocuments();
                            }
                        });
        }

        private async void SelectSampleTags()
        {
            Log.Info(LogMessages.ManageDataSampleTagSelect);
            var context = new AssignTagDialogViewModel(Tags, new ObservableCollection<Tag>(_selectedTagsForSample));
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
            var context = new AssignTagDialogViewModel(Tags, new ObservableCollection<Tag>(_selectedTagsForFilter));
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