﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Slamby.TAU.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsProviderAttribute(typeof(SlambySettingsProvider))]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("s3cr3t")]
        public string EndpointSecret {
            get {
                return ((string)(this["EndpointSecret"]));
            }
            set {
                this["EndpointSecret"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsProviderAttribute(typeof(SlambySettingsProvider))]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1000")]
        public int BulkSize {
            get {
                return ((int)(this["BulkSize"]));
            }
            set {
                this["BulkSize"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsProviderAttribute(typeof(SlambySettingsProvider))]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://tautest/")]
        public string EndpointUri {
            get {
                return ((string)(this["EndpointUri"]));
            }
            set {
                this["EndpointUri"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UpdateSettings {
            get {
                return ((bool)(this["UpdateSettings"]));
            }
            set {
                this["UpdateSettings"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://tau.slamby.com/install/")]
        public string UpdateFeed {
            get {
                return ((string)(this["UpdateFeed"]));
            }
            set {
                this["UpdateFeed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsProviderAttribute(typeof(SlambySettingsProvider))]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("{}")]
        public string GridSettingsDictionary {
            get {
                return ((string)(this["GridSettingsDictionary"]));
            }
            set {
                this["GridSettingsDictionary"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsProviderAttribute(typeof(SlambySettingsProvider))]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[{\"ApiBaseEndpoint\":\"https://api.slamby.com/demo-api/\",  \"ApiSecret\": \"s3cr3t\",  " +
            " \"Timeout\": \"00:05:00\",   \"ParallelLimit\": 1000}]")]
        public string Endpoints {
            get {
                return ((string)(this["Endpoints"]));
            }
            set {
                this["Endpoints"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsProviderAttribute(typeof(SlambySettingsProvider))]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("{\"ApiBaseEndpoint\":\"https://api.slamby.com/demo-api/\",  \"ApiSecret\": \"s3cr3t\",   " +
            "\"Timeout\": \"00:05:00\",   \"ParallelLimit\": 1000}")]
        public string SelectedEndpoint {
            get {
                return ((string)(this["SelectedEndpoint"]));
            }
            set {
                this["SelectedEndpoint"] = value;
            }
        }
    }
}
