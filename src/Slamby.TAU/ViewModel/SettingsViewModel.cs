using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Slamby.SDK.Net;
using Slamby.TAU.Enum;
using Slamby.TAU.Helper;
using Slamby.TAU.Model;
using Slamby.TAU.Logger;
using Slamby.TAU.Resources;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the SettingsViewModel class.
        /// </summary>
        public SettingsViewModel()
        {
            EndPointUri = GlobalStore.EndpointConfiguration.ApiBaseEndpoint.AbsoluteUri;
            EndPointSecret = GlobalStore.EndpointConfiguration.ApiSecret;
            BulkSize = GlobalStore.BulkSize;
            ApplyEndpointSettingsCommand = new RelayCommand(() =>
            {
                Log.Info(LogMessages.SettingsApplyEndpointCommand);
                GlobalStore.EndpointConfiguration = new Configuration
                {
                    ApiBaseEndpoint = new Uri(EndPointUri),
                    ApiSecret = EndPointSecret
                };
                GlobalStore.BulkSize = BulkSize;
                Messenger.Default.Send(new UpdateMessage(UpdateType.EndPointUpdate));
            });
        }


        private string _endPointUri;

        public string EndPointUri
        {
            get { return _endPointUri; }
            set { Set(() => EndPointUri, ref _endPointUri, value); }
        }


        private string _endPointSecret;

        public string EndPointSecret
        {
            get { return _endPointSecret; }
            set { Set(() => EndPointSecret, ref _endPointSecret, value); }
        }


        private int _bulkSize;

        public int BulkSize
        {
            get { return _bulkSize; }
            set { Set(() => BulkSize, ref _bulkSize, value); }
        }

        public RelayCommand ApplyEndpointSettingsCommand { get; private set; }
    }
}