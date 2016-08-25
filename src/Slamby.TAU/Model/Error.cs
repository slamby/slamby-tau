using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.Model
{
    public class Error
    {
        public string Header { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public DateTime Date { get; set; }

        public static Error Convert(object errorObject)
        {
            if (errorObject is ClientResponse)
            {
                var error = (ClientResponse)errorObject;
                var header = error.ServerMessage.Length > 50 ? error.ServerMessage.Substring(0, 50) + "..." : error.ServerMessage;
                header = header.Replace(Environment.NewLine, " ");
                var details = string.Join(Environment.NewLine, error.Errors.Errors);
                return new Error { Header = header, Message = error.ServerMessage, Details = details, Date = DateTime.UtcNow };
            }
            if (errorObject is Exception)
            {
                var error = (Exception)errorObject;
                var header = error.Message.Length > 50 ? error.Message.Substring(0, 50) + "..." : error.Message;
                header = header.Replace(Environment.NewLine, " ");
                var details = error.InnerException == null ? "" : string.Join(Environment.NewLine, GetInnerExceptions(error));
                return new Error { Header = header, Message = error.Message, Details = details, Date = DateTime.UtcNow };
            }
            return null;
        }

        private static List<string> GetInnerExceptions(Exception exception)
        {
            var inners = new List<string> { exception.Message };
            if (exception.InnerException != null)
                inners.AddRange(GetInnerExceptions(exception.InnerException));
            return inners;
        }
    }
}