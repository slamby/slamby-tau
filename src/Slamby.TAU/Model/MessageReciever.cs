using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace Slamby.TAU.Model
{
    public class MessageReciever : ObservableObject
    {
        public MessageReciever()
        {
            //Messenger.Default.Register<StatusMessage>(this, m => { Messages += string.Format("{0}{1} - {2}", Environment.NewLine, m.Timestamp.ToString("yyyy-MM-dd hh:mm:ss"), m.Message); });
            Messenger.Default.Register<StatusMessage>(this, m => { ErrorMessages.Add(new ErrorMessage(string.Format("{0}{1} - {2}", Environment.NewLine, m.Timestamp.ToString("yyyy-MM-dd hh:mm:ss"), m.Message))); });
        }


        string _messages = "";

        public string Messages
        {
            get { return _messages; }
            set { Set(() => Messages, ref _messages, value); }
        }

        public ObservableCollection<ErrorMessage> ErrorMessages { get; set; }
    }

    public class ErrorMessage
    {
        public ErrorMessage(string message)
        {
            Text = message;
            Header = message.Length > 200 ? message.Substring(0, 200) + "..." : message;
        }

        public string Header { get; set; }

        public string Text { get; set; }
    }
}
