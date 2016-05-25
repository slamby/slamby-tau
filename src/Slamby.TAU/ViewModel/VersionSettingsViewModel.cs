using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Slamby.TAU.Logger;
using Slamby.TAU.Properties;
using Squirrel;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class VersionSettingsViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the VersionSettingsViewModel class.
        /// </summary>
        public VersionSettingsViewModel()
        {
            ApplyReleaseCommand = new RelayCommand(async () =>
            {
                var selectedVersion = $"{SelectedRelease.Major}.{SelectedRelease.Minor}.{SelectedRelease.Build}";
                Uri baseUri = new Uri(Settings.Default.UpdateFeed);
                Uri uri = new Uri(baseUri, selectedVersion);
                using (var mgr = new UpdateManager(uri.ToString(), "SlambyTau"))
                {
                    Directory.Delete(Path.Combine(mgr.RootAppDirectory, "packages"), true);
                    await mgr.FullInstall();
                }
                Application.Current.Shutdown();
            });
            LoadedCommand = new RelayCommand(() =>
            {
                String content = "";
                using (var client = new WebClient())
                {
                    Stream stream = client.OpenRead(Settings.Default.UpdateFeed + "RELEASES");
                    using (var reader = new StreamReader(stream))
                    {
                        content = reader.ReadToEnd();
                    }
                }

                AvailableReleases =
                    new ObservableCollection<Version>(content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(v => new Version(v)));
                CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            });
        }

        private ObservableCollection<Version> _availableReleases = new ObservableCollection<Version>();

        public ObservableCollection<Version> AvailableReleases
        {
            get { return _availableReleases; }
            set { Set(() => AvailableReleases, ref _availableReleases, value); }
        }

        private Version _currentVersion;

        public Version CurrentVersion
        {
            get { return _currentVersion; }
            set { Set(() => CurrentVersion, ref _currentVersion, value); }
        }

        private Version _selectedRelease;

        public Version SelectedRelease
        {
            get { return _selectedRelease; }
            set { Set(() => SelectedRelease, ref _selectedRelease, value); }
        }

        public RelayCommand ApplyReleaseCommand { get; private set; }

        public RelayCommand LoadedCommand { get; private set; }

    }
}