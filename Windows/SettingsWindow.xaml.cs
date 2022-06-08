using System;
using System.Windows;
using System.Windows.Documents;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using DayZServerControllerUI.UserControls;

namespace DayZServerControllerUI.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private bool _muteDiscordBot;
        private bool _useSteamCmd;
        private readonly string _steamCredentialsStorageName;
        private readonly List<UserControlPathSetting> _pathUserControls;
        private bool _selectedPathsValid;
        private bool _steamCredentialsValid;

        public bool SettingsValid
        {
            get
            {
                if (_selectedPathsValid && !_useSteamCmd)
                    return true;

                // Take SteamCredentials Controls into account if SteamCMD Mode is enabled
                return _useSteamCmd && _steamCredentialsValid && _selectedPathsValid;
            }
        }

        public SettingsWindow()
        {
            InitializeComponent();

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
                    userControlPathSetting.ValidPathSelected += UserControlPathSetting_ValidPathSelected;
                }
            }

            _muteDiscordBot = DayzCtrlSettings.Default.MuteDiscordBot;
            _useSteamCmd = DayzCtrlSettings.Default.UseSteamCmd;

            _steamCredentialsStorageName = DayzCtrlSettings.Default.SteamCredentialStorageName ?? "SteamCredentials";

            ApplySettingsFromFile();
        }

        private void UserControlPathSetting_ValidPathSelected()
        {
            // Are all necessary paths valid?
            switch (_useSteamCmd)
            {
                case true:
                    // With SteamCmd enabled the path to SteamCmd.exe must also be valid
                    _selectedPathsValid = _pathUserControls.All(x => x.SelectionValid);

                    return;

                case false:
                    _selectedPathsValid = _pathUserControls.Where(x => x != UserControlSteamCmdPath).All(x => x.SelectionValid);

                    return;
            }
        }

        private void ApplySettingsFromFile()
        {
            CheckBoxMuteDiscord.IsChecked = DayzCtrlSettings.Default.MuteDiscordBot;
            CheckBoxUseSteamCmd.IsChecked = DayzCtrlSettings.Default.UseSteamCmd;

            // Load credentials from Windows Credential Manager
            WindowsCredentials.TryGetExistingCredentials(_steamCredentialsStorageName,
                out Credential? steamCredentials);

            if (steamCredentials != null)
            {
                // Credentials found -> write to TextBoxes
                TextBoxSteamUser.Text = steamCredentials.Username;
                PasswordBoxSteamPassword.Password = steamCredentials.Password;
            }

            UserControlDayzServerPath.SelectedPath = DayzCtrlSettings.Default.DayzServerExePath;
            UserControlDayzClientPath.SelectedPath = DayzCtrlSettings.Default.DayzGameExePath;
            UserControlModlistPath.SelectedPath = DayzCtrlSettings.Default.ModMappingFilePath;
            UserControlSteamCmdPath.SelectedPath = DayzCtrlSettings.Default.SteamCmdPath;

            ButtonSave.IsEnabled = SettingsValid;
        }

        private void SaveSettingsToFile()
        {
            // Save new Credentials if valid
            if (_useSteamCmd && _steamCredentialsValid)
            {
                bool success = WindowsCredentials.SaveCredentials(TextBoxSteamUser.Text, PasswordBoxSteamPassword.Password,
                    _steamCredentialsStorageName, out _);

                if (!success)
                {
                    _steamCredentialsValid = false;
                    CheckBoxUseSteamCmd.IsChecked = false;

                    MessageBox.Show($"Unable to store Steam-Credentials to Windows Credential Storage.", $"Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }
            }

            DayzCtrlSettings.Default.MuteDiscordBot = CheckBoxMuteDiscord.IsChecked ?? false;
            DayzCtrlSettings.Default.UseSteamCmd = CheckBoxUseSteamCmd.IsChecked ?? false;

            DayzCtrlSettings.Default.Save();
        }

        private void CheckBoxUseSteamCmd_Click(object sender, RoutedEventArgs e)
        {
            if (_useSteamCmd == CheckBoxUseSteamCmd.IsChecked)
                return;

            _useSteamCmd = CheckBoxUseSteamCmd.IsChecked ?? false;

            TextBoxSteamUser.IsEnabled = _useSteamCmd;
            PasswordBoxSteamPassword.IsEnabled = _useSteamCmd;
        }

        private void CheckBoxMuteDiscord_Click(object sender, RoutedEventArgs e)
        {
            if (_muteDiscordBot == CheckBoxMuteDiscord.IsChecked)
                return;

            _muteDiscordBot = CheckBoxMuteDiscord.IsChecked ?? false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettingsToFile();
            Hide();
        }

        private void ButtonDiscard_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void TextBoxSteamUser_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _steamCredentialsValid = !String.IsNullOrEmpty(TextBoxSteamUser.Text) &&
                                     !String.IsNullOrEmpty(PasswordBoxSteamPassword.Password);

            ButtonSave.IsEnabled = SettingsValid;
        }

        private void PasswordBoxSteamPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            _steamCredentialsValid = !String.IsNullOrEmpty(TextBoxSteamUser.Text) &&
                                     !String.IsNullOrEmpty(PasswordBoxSteamPassword.Password);

            ButtonSave.IsEnabled = SettingsValid;
        }

        // Hide Window instead of closing
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
