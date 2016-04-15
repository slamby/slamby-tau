using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Slamby.TAU.Model;
using GalaSoft.MvvmLight.Threading;

namespace Slamby.TAU.Helper
{
    public static class StatusHelper
    {
        public static void LogStatus(StatusMessage message)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(message));
        }
    }
}
