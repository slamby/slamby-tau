using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Helper;
using Slamby.TAU.Model;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class AssignTagDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the AssignTagDialogViewModel class.
        /// </summary>
        public AssignTagDialogViewModel(ObservableCollection<Tag> tags, ObservableCollection<Tag> selectedTags, DataGridSettings settings)
        {
            Tags = tags;
            SelectedTags = selectedTags;
            Settings = settings;
        }

        private ObservableCollection<Tag> _tags;

        public ObservableCollection<Tag> Tags
        {
            get { return _tags; }
            set { Set(() => Tags, ref _tags, value); }
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

        private DataGridSettings _settings;

        public DataGridSettings Settings
        {
            get { return _settings; }
            set { Set(() => Settings, ref _settings, value); }
        }

    }
}