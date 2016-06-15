using System;
using System.Collections.Generic;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.Model
{
    public class Error
    {
        public string Message { get; set; }
        public string Details { get; set; }
        public DateTime Date { get; set; }

        public static Error Convert(object errorObject)
        {
            if (errorObject is ClientResponse)
            {
                var error = (ClientResponse)errorObject;
                return new Error { Message = error.ServerMessage, Details = string.Join(Environment.NewLine, error.Errors), Date = DateTime.UtcNow };
            }
            if (errorObject is Exception)
            {
                var error = (Exception)errorObject;
                return new Error { Message = error.Message, Details = string.Join(Environment.NewLine, GetInnerExceptions(error)), Date = DateTime.UtcNow};
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