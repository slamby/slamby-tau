using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Logger;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slamby.TAU.Helper
{
    public static class ResponseValidator
    {
        public static bool Validate(ClientResponse response, bool handleException = true)
        {
            if (!response.IsSuccessful)
            {
                var exception = response.Errors != null ? new Exception(string.Format("HttpStatusCode: {0}, ServerMessage: {1}{2}Errors: {3}", response.HttpStatusCode, response.ServerMessage, Environment.NewLine, string.Join(Environment.NewLine, response.Errors.Errors))) :
                    new Exception(string.Format("HttpStatusCode: {0}, ServerMessage: {1}", response.HttpStatusCode, response.ServerMessage));
                Log.Error(exception);
                if (GlobalStore.IsInTestMode || !handleException)
                    throw exception;
                DispatcherHelper.CheckBeginInvokeOnUI(() => Messenger.Default.Send(response));
            }
            return response.IsSuccessful;
        }
    }
}
