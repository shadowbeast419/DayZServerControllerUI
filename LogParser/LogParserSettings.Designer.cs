﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DayZServerControllerUI.LogParser {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.0.3.0")]
    internal sealed partial class LogParserSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static LogParserSettings defaultInstance = ((LogParserSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new LogParserSettings())));
        
        public static LogParserSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ServerLogDir {
            get {
                return ((string)(this["ServerLogDir"]));
            }
            set {
                this["ServerLogDir"] = value;
            }
        }
    }
}
