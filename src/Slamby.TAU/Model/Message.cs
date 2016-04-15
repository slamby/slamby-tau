using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slamby.TAU.Model
{
    public class Message
    {
        public Message(string text)
        {
            Text = text;
        }
        public string Text { get; set; }
    }
}
