using System;
using System.Collections.Generic;
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
            Messenger.Default.Register<StatusMessage>(this, m => { Messages += string.Format("{0}{1} - {2}", Environment.NewLine, m.Timestamp.ToString("yyyy-MM-dd hh:mm:ss"), m.Message);; });
        }


        string _messages = "";

        public string Messages
        {
            get { return _messages; }
            set { Set(() => Messages, ref _messages, value); }
        }
    }
}
