using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;

namespace DayZServerControllerUI
{
    internal class ServerControlSettings
    {
        private readonly string _steamCredentialsStorageName;
        private Credential? _steamCredentials;
        private readonly List<FileInfo?> _fileInfos;

        public bool MuteDiscordBot { get; set; }
        public bool UseSteamCmd { get; set; }
        public FileInfo? DayzServerExePath { get; set; }
        public FileInfo? DayzGameExePath { get; set; }
        public FileInfo? ModMappingFilePath { get; set; }
        public FileInfo? SteamCmdPath { get; set; }

        public Credential? SteamCredentials
        {
            get => _steamCredentials;
            set => _steamCredentials = value;
        }

        public bool SettingsValid
        {
            get
            {
                if (!UseSteamCmd && PathsValid)
                    return true;

                return UseSteamCmd && PathsValid && CredentialsValid;
            }
        }

        public bool PathsValid
        {
            get
            {
                return _fileInfos.All(x => x != null && x.Exists);
            }
        }

        public bool CredentialsValid
        {
            get
            {
                return _steamCredentials != null && !String.IsNullOrEmpty(_steamCredentials.Username) &&
                       !String.IsNullOrEmpty(_steamCredentials.Password);
            }
        }

        public ServerControlSettings(string steamCredentialsStorageName)
        {
            _steamCredentialsStorageName = steamCredentialsStorageName;
            _fileInfos = new List<FileInfo?>();

            // Get all FileInfos of SettingsClass by Reflection
            foreach (var member in this.GetType().GetMembers())
            {
                if(member is not FieldInfo)
                    continue;

                FieldInfo fieldInfo = (FieldInfo) member;

                if(fieldInfo.FieldType != typeof(FileInfo))
                    continue;

                object? fileInfoObj = fieldInfo.GetValue(this);

                if (fileInfoObj is FileInfo fileInfo)
                {
                    _fileInfos.Add(fileInfo);
                }
            }

            LoadSettingsFromFile();
        }

        public void SaveSettingsToFile()
        {

        }

        private void LoadSettingsFromFile()
        {
            MuteDiscordBot = DayzCtrlSettings.Default.MuteDiscordBot;
            UseSteamCmd = DayzCtrlSettings.Default.UseSteamCmd;

            LoadSteamCredentials();

            DayzServerExePath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.DayzServerExePath)
                ? new FileInfo(DayzCtrlSettings.Default.DayzServerExePath)
                : null;

            DayzGameExePath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.DayzGameExePath)
                ? new FileInfo(DayzCtrlSettings.Default.DayzGameExePath)
                : null;

            ModMappingFilePath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.ModMappingFilePath)
                ? new FileInfo(DayzCtrlSettings.Default.ModMappingFilePath)
                : null;

            SteamCmdPath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.SteamCmdPath)
                ? new FileInfo(DayzCtrlSettings.Default.SteamCmdPath)
                : null;
        }

        private void LoadSteamCredentials()
        {
            if (String.IsNullOrEmpty(_steamCredentialsStorageName))
            {
                WindowsCredentials.TryGetExistingCredentials(_steamCredentialsStorageName,
                    out _steamCredentials);
            }
        }

        private void SaveSteamCredentials()
        {
            //bool success = WindowsCredentials.SaveCredentials(TextBoxSteamUser.Text, PasswordBoxSteamPassword.Password,
            //    _steamCredentialsStorageName, out _);

            //if (!success)
            //{
            //    _steamCredentialsValid = false;
            //    CheckBoxUseSteamCmd.IsChecked = false;

            //    MessageBox.Show($"Unable to store Steam-Credentials to Windows Credential Storage.", $"Error",
            //        MessageBoxButton.OK, MessageBoxImage.Error);

            //    return;
            //}
        }
    }
}
