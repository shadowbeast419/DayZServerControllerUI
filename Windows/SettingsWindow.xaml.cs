using System;
using System.Windows;
using System.Windows.Documents;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Remoting;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using DayZServerControllerUI.Annotations;
using DayZServerControllerUI.Settings;
using DayZServerControllerUI.UserControls;

namespace DayZServerControllerUI.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public sealed partial class SettingsWindow : IDisposable, INotifyPropertyChanged
    {
        private readonly ServerControlSettingsWrapper _settingsWrapper = new (DayzCtrlSettings.Default.SteamCredentialStorageName ?? "SteamCredentials");
        private readonly List<UserControlPathSetting> _pathUserControls;

        public ServerControlSettingsWrapper SettingsWrapper => _settingsWrapper;


        #region Properties for UI Bindings

        public bool SteamCmdEnabled
        {
            get => _settingsWrapper.UseSteamCmd;
            set => _settingsWrapper.UseSteamCmd = value;
        }

        public bool AllSettingsValid => _settingsWrapper.SettingsValid;

        public bool DiscordBotIsEnabled
        {
            // Inverted logic of DiscordBot-Enable applies better to UI
            get => !_settingsWrapper.MuteDiscordBot;
            set => _settingsWrapper.MuteDiscordBot = !value;
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsWindow()
        {
            InitializeComponent();

            DataContext = this;
            this.Hide();
            
            _pathUserControls = new List<UserControlPathSetting>();

            // Get all Path-UserControls by Reflection
            foreach (var member in this.GetType().GetMembers())
            {
                if (member is not FieldInfo)
                    continue;

                FieldInfo fieldInfo = (FieldInfo)member;

                if (fieldInfo.FieldType != typeof(UserControlPathSetting))
                    continue;

                object? pathUserCtrl = fieldInfo.GetValue(this);

                if(pathUserCtrl is UserControlPathSetting userControlPathSetting)
                {
                    _pathUserControls.Add(userControlPathSetting);
                    userControlPathSetting.PropertyChanged += UserControlPathSetting_PropertyChanged;
                }
            }

            foreach (UserControlPathSetting userCtrl in _pathUserControls)
            {
                userCtrl.PropertyChanged += UserCtrlPath_PropertyChanged;
            }
            
            ButtonSave.IsEnabled = _settingsWrapper.SettingsValid;

            ApplySettingsToUi();
        }


        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UserCtrlPath_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == null || sender is not UserControlPathSetting)
                return;

            if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName != "SelectedPath")
                return;

            // DayZ Server Path
            if (sender == UserControlDayzServerPath && !String.IsNullOrEmpty(UserControlDayzServerPath.SelectedPath))
            {
                _settingsWrapper.DayzServerExePath = new FileInfo(UserControlDayzServerPath.SelectedPath);
                return;
            }

            // DayZ Client Path
            if (sender == UserControlDayzClientPath && !String.IsNullOrEmpty(UserControlDayzClientPath.SelectedPath))
            {
                _settingsWrapper.DayzGameExePath = new FileInfo(UserControlDayzClientPath.SelectedPath);
                return;
            }

            // ModMapping.txt File-Path
            if (sender == UserControlModlistPath && !String.IsNullOrEmpty(UserControlModlistPath.SelectedPath))
            {
                _settingsWrapper.ModMappingFilePath = new FileInfo(UserControlModlistPath.SelectedPath);
                return;
            }

            // Steam CMD Path
            if (sender == UserControlSteamCmdPath && !String.IsNullOrEmpty(UserControlSteamCmdPath.SelectedPath))
            {
                _settingsWrapper.SteamCmdPath = new FileInfo(UserControlSteamCmdPath.SelectedPath);
                return;
            }

            // DiscordInfo.txt Path
            if (sender == UserControlDiscordFilePath && !String.IsNullOrEmpty(UserControlDiscordFilePath.SelectedPath))
            {
                _settingsWrapper.DiscordFilePath = new FileInfo(UserControlDiscordFilePath.SelectedPath);
                return;
            }

            // ServerLog.log Path
            if (sender == UserControlServerLogFilePath && !String.IsNullOrEmpty(UserControlServerLogFilePath.SelectedPath))
            {
                _settingsWrapper.DayzServerLogFilePath = new FileInfo(UserControlServerLogFilePath.SelectedPath);
            }
        }

        /// <summary>
        /// This event is called if one of the Path UserControls contain a new path (selected by user)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void UserControlPathSetting_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Notify the MainViewModel of a settings change
            OnPropertyChanged(nameof(AllSettingsValid));
        }

        private void ApplySettingsToUi()
        {
            CheckBoxMuteDiscord.IsChecked = _settingsWrapper.MuteDiscordBot;
            CheckBoxUseSteamCmd.IsChecked = _settingsWrapper.UseSteamCmd;

            if (_settingsWrapper.CredentialsValid && _settingsWrapper.SteamCredentials != null)
            {
                // Credentials found -> write to TextBoxes
                TextBoxSteamUser.Text = _settingsWrapper.SteamCredentials.Username;
                PasswordBoxSteamPassword.Password = _settingsWrapper.SteamCredentials.Password;
            }

            UserControlDayzServerPath.SelectedPath = _settingsWrapper.DayzServerExePath?.FullName;
            UserControlDayzClientPath.SelectedPath = _settingsWrapper.DayzGameExePath?.FullName;
            UserControlModlistPath.SelectedPath = _settingsWrapper.ModMappingFilePath?.FullName;
            UserControlSteamCmdPath.SelectedPath = _settingsWrapper.SteamCmdPath?.FullName;
            UserControlDiscordFilePath.SelectedPath = _settingsWrapper.DiscordFilePath?.FullName;
            UserControlServerLogFilePath.SelectedPath = _settingsWrapper.DayzServerLogFilePath?.FullName;

            OnPropertyChanged(nameof(AllSettingsValid));
        }

        /// <summary>
        /// If SteamCmd is used, disable/enable Controls accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBoxUseSteamCmd_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWrapper.UseSteamCmd == CheckBoxUseSteamCmd.IsChecked)
                return;

            _settingsWrapper.UseSteamCmd = CheckBoxUseSteamCmd.IsChecked ?? false;

            // Note:
            // Some IsEnabled-Assignments happen implicitly via Bindings to the UseSteamCmd-Property
            UserControlSteamCmdPath.Status = _settingsWrapper.UseSteamCmd ? UserControlPathSettingState.Disabled : UserControlPathSettingState.PathInvalid;
            OnPropertyChanged(nameof(SteamCmdEnabled));
        }

        private void CheckBoxMuteDiscord_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWrapper.MuteDiscordBot == CheckBoxMuteDiscord.IsChecked)
                return;

            _settingsWrapper.MuteDiscordBot = CheckBoxMuteDiscord.IsChecked ?? false;

            // Inform the UI about a possible change
            OnPropertyChanged(nameof(AllSettingsValid));
            OnPropertyChanged(nameof(DiscordBotIsEnabled));
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            _settingsWrapper.SaveSettingsToFile();
            Hide();
        }

        private void ButtonDiscard_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void TextBoxSteamUser_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(TextBoxSteamUser.Text) &&
                !String.IsNullOrEmpty(PasswordBoxSteamPassword.Password))
            {
                _settingsWrapper.SteamCredentials = new Credential(TextBoxSteamUser.Text, PasswordBoxSteamPassword.Password);
            }

            // Inform the UI about a possible change
            OnPropertyChanged(nameof(AllSettingsValid));
        }

        private void PasswordBoxSteamPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(TextBoxSteamUser.Text) &&
                !String.IsNullOrEmpty(PasswordBoxSteamPassword.Password))
            {
                _settingsWrapper.SteamCredentials = new Credential(TextBoxSteamUser.Text, PasswordBoxSteamPassword.Password);
            }

            // Inform the UI about a possible change
            OnPropertyChanged(nameof(AllSettingsValid));
        }

        // Hide Window instead of closing
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        public void Dispose()
        {
            // Unsubscribe from events (garbage collection waits for it!)
            foreach (UserControlPathSetting userCtrl in _pathUserControls)
            {
                userCtrl.PropertyChanged -= UserCtrlPath_PropertyChanged;
            }
        }
    }
}
