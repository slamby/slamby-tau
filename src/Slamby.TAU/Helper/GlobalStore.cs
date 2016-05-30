using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Slamby.SDK.Net;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Model;
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

        private static Dictionary<string, Dictionary<string, DataGridSettings>> _gridSettingsDictionary = JObject.Parse(Settings.Default["GridSettingsDictionary"].ToString()).ToObject<Dictionary<string, Dictionary<string, DataGridSettings>>>();

        public static Dictionary<string, Dictionary<string, DataGridSettings>> GridSettingsDictionary
        {
            get
            {
                return _gridSettingsDictionary;
            }
            set
            {
                _gridSettingsDictionary = value;
                Settings.Default["GridSettingsDictionary"] = JObject.FromObject(value).ToString();
                if (!IsInTestMode)
                    Settings.Default.Save();
            }
        }

        public static void SaveGridSettings(string gridId, string dataSetId, DataGridSettings settings)
        {
            var settingsToSave = (Dictionary<string, Dictionary<string, DataGridSettings>>)GridSettingsDictionary;
            if (settingsToSave != null && settingsToSave.Any())
            {
                if (settingsToSave.ContainsKey(gridId))
                {
                    var tagsSettings = settingsToSave[gridId];
                    if (tagsSettings.ContainsKey(dataSetId))
                    {
                        settingsToSave[gridId][dataSetId] = settings;
                    }
                    else
                    {
                        settingsToSave[gridId].Add(dataSetId, settings);
                    }
                }
                else
                {
                    settingsToSave.Add(gridId, new Dictionary<string, DataGridSettings> { { dataSetId, settings } });
                }
            }
            else
            {
                var settingsDict = new Dictionary<string, Dictionary<string, DataGridSettings>>();
                settingsDict.Add(gridId, new Dictionary<string, DataGridSettings> { { dataSetId, settings } });
                settingsToSave = settingsDict;
            }
            GridSettingsDictionary = settingsToSave;
        }

        public static bool IsInTestMode { get; set; } = false;

        public static bool DialogIsOpen { get; set; } = false;
    }
}
