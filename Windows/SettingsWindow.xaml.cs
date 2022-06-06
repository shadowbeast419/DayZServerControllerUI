using System;
using System.Windows;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;

namespace DayZServerControllerUI.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private bool _muteDiscordBot;
        private bool _useSteamCmd;
        private string _steamCredentialsStorageName;

        public SettingsWindow()
        {
            InitializeComponent();

            _muteDiscordBot = DayzCtrlSettings.Default.MuteDiscordBot;
            _useSteamCmd = DayzCtrlSettings.Default.UseSteamCmd;

            _steamCredentialsStorageName = DayzCtrlSettings.Default.SteamCredentialStorageName ?? "SteamCredentials";

            ApplySettingsFromFile();
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
                TextBoxSteamPassword.Password = steamCredentials.Password;
            }

            UserControlDayzServerPath.SelectedPath = DayzCtrlSettings.Default.DayzServerExePath;
            UserControlDayzClientPath.SelectedPath = DayzCtrlSettings.Default.DayzGameExePath;
            UserControlModlistPath.SelectedPath = DayzCtrlSettings.Default.ModMappingFilePath;
            UserControlSteamCmdPath.SelectedPath = DayzCtrlSettings.Default.SteamCmdPath;
        }

        private bool SaveSettingsToFile()
        {
            bool isSuccess = true;
            bool steamCmdEnabled = CheckBoxUseSteamCmd.IsChecked ?? false;

            // Save new Credentials if valid
            if (steamCmdEnabled && !String.IsNullOrEmpty(TextBoxSteamUser.Text) && !String.IsNullOrEmpty(TextBoxSteamPassword.Password))
            {
                isSuccess = WindowsCredentials.SaveCredentials(TextBoxSteamUser.Text, TextBoxSteamPassword.Password,
                    _steamCredentialsStorageName, out _);
            }

            DayzCtrlSettings.Default.MuteDiscordBot = CheckBoxMuteDiscord.IsChecked ?? false;
            DayzCtrlSettings.Default.UseSteamCmd = CheckBoxUseSteamCmd.IsChecked ?? false;

            DayzCtrlSettings.Default.Save();

            return isSuccess;
        }

        private void CheckBoxUseSteamCmd_Click(object sender, RoutedEventArgs e)
        {
            if (_useSteamCmd == CheckBoxUseSteamCmd.IsChecked)
                return;

            _useSteamCmd = CheckBoxUseSteamCmd.IsChecked ?? false;
        }

        private void CheckBoxMuteDiscord_Click(object sender, RoutedEventArgs e)
        {
            if (_muteDiscordBot == CheckBoxMuteDiscord.IsChecked)
                return;

            _muteDiscordBot = CheckBoxMuteDiscord.IsChecked ?? false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            bool success = SaveSettingsToFile();

            if (!success)
                MessageBox.Show($"Error while trying to save Steam Credentials to Windows Credential Storage.");

            Close();
        }

        private void ButtonDiscard_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonServerPath_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonClientPath_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonModlistPath_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ButtonSteamCmdPath_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
