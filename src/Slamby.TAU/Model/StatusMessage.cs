using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slamby.TAU.Model
{
    public class StatusMessage
    {
        public StatusMessage(string message, DateTime timestamp)
        {
            Message = message;
            Timestamp = timestamp;
        }

        public string Message { get; private set; }

        public DateTime Timestamp { get; private set; }
    }
}
