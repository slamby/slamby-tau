using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slamby.SDK.Net;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Properties;

namespace Slamby.TAU.Helper
{
    public static class GlobalStore
    {
        private static Configuration _endpointConfiguration = new Configuration
        {
            ApiBaseEndpoint = new Uri(Settings.Default["EndpointUri"].ToString()),
            ApiSecret = Settings.Default["EndpointSecret"].ToString()
        };
        public static Configuration EndpointConfiguration
        {
            get
            {
                return _endpointConfiguration;
            }
            set
            {
                _endpointConfiguration = value;
                Settings.Default["EndpointUri"] = value.ApiBaseEndpoint.ToString();
                Settings.Default["EndpointSecret"] = value.ApiSecret.ToString();
                if (!IsInTestMode)
                    Settings.Default.Save();
            }
        }

        private static int _bulkSize = Int32.Parse(Settings.Default["BulkSize"].ToString());

        public static int BulkSize
        {
            get
            {
                return _bulkSize;
            }
            set
            {
                _bulkSize = value;
                Settings.Default["BulkSize"] = value;
                if (!IsInTestMode)
                    Settings.Default.Save();
            }
        }

        public static DataSet CurrentDataset { get; set; }

        public static bool IsInTestMode { get; set; } = false;

        public static bool DialogIsOpen { get; set; } = false;
    }
}
