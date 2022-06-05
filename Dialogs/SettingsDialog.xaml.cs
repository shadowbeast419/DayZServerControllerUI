using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;

namespace DayZServerControllerUI.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private bool _muteDiscordBot;
        private bool _useSteamCmd;
        private string _steamCredentialsStorageName;

        public SettingsDialog()
        {
            InitializeComponent();

            _muteDiscordBot = Settings.Default.MuteDiscordBot;
            _useSteamCmd = Settings.Default.UseSteamCmd;

            _steamCredentialsStorageName = Settings.Default.SteamCredentialStorageName ?? "SteamCredentials";

            ApplySettingsFromFile();
        }

        private void ApplySettingsFromFile()
        {
            CheckBoxMuteDiscord.IsChecked = Settings.Default.MuteDiscordBot;
            CheckBoxUseSteamCmd.IsChecked = Settings.Default.UseSteamCmd;

            // Load credentials from Windows Credential Manager
            WindowsCredentials.TryGetExistingCredentials(_steamCredentialsStorageName,
                out Credential? steamCredentials);

            if (steamCredentials != null)
            {
                // Credentials found -> write to TextBoxes
                TextBoxSteamUser.Text = steamCredentials.Username;
                TextBoxSteamPassword.Password = steamCredentials.Password;
            }
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

            Settings.Default.MuteDiscordBot = CheckBoxMuteDiscord.IsChecked ?? false;
            Settings.Default.UseSteamCmd = CheckBoxUseSteamCmd.IsChecked ?? false;

            Settings.Default.Save();

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
    }
}
