using System;
using System.Security.Cryptography;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Slamby.TAU.Model;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MessageWindowViewModel : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the MessageWindowViewModel class.
        /// </summary>
        public MessageWindowViewModel()
        {
            Messenger.Default.Register<StatusMessage>(this, m => { Messages += string.Format("{0}{1} - {2}", Environment.NewLine, m.Timestamp.ToString("u"), m.Message); });
        }


        string _messages = "";

        public string Messages
        {
            get { return _messages; }
            set { Set(() => Messages, ref _messages, value); }
        }

    }
}