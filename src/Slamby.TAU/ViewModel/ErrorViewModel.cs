using GalaSoft.MvvmLight;
using Slamby.SDK.Net.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ErrorViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the ErrorViewModel class.
        /// </summary>
        public ErrorViewModel(ClientResponse response)
        {
            Message = response.ServerMessage;
            StatusCode = response.HttpStatusCode;
            Errors = response.Errors == null ? new List<string>() : response.Errors.Errors;
            Source = "BackEnd";
        }

        public ErrorViewModel(Exception exception)
        {
            Message = exception.Message;
            Errors = exception.InnerException != null ? new List<string> { string.Format("[InnerException]: {0} [StackTrace]: {1}", exception.InnerException.InnerException, exception.InnerException.StackTrace) } : new List<string>();
            Source = "FrontEnd";
        }

        private string _message;

        public string Message
        {
            get { return _message; }
            set { Set(() => Message, ref _message, value); }
        }



        private HttpStatusCode _statusCode;

        public HttpStatusCode StatusCode
        {
            get { return _statusCode; }
            set { Set(() => StatusCode, ref _statusCode, value); }
        }



        private IEnumerable _errors;

        public IEnumerable Errors
        {
            get { return _errors; }
            set { Set(() => Errors, ref _errors, value); }
        }


        private string _source;

        public string Source
        {
            get { return _source; }
            set { Set(() => Source, ref _source, value); }
        }

    }
}