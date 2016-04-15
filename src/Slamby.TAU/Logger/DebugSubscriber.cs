using Slamby.SDK.Net.Helpers;
using Slamby.TAU.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slamby.TAU.Logger
{
    public class DebugSubscriber : IRawMessageSubscriber
    {
        public void OnRawDataPublish(object sender, RawMessageEventArgs args)
        {
            StatusHelper.LogStatus(new Model.StatusMessage(args.Message, DateTime.UtcNow));
        }
    }
}
