using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Enum;

namespace Slamby.TAU.Model
{
    public class UpdateMessage
    {
        public UpdateMessage(UpdateType updateType, object parameter = null)
        {
            UpdateType = updateType;
            Parameter = parameter;
        }

        public object Parameter { get; set; }

        public UpdateType UpdateType { get; set; }
    }
}
