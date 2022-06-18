using System;
using System.Collections.Generic;
using System.IO;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;
using Ookii.Dialogs.Wpf;

namespace DayZServerControllerUI.Settings
{
    public class ServerControlSettingsWrapper
    {
        private readonly string _steamCredentialsStorageName;
        private Credential? _steamCredentials;
        private readonly List<FileInfo?> _fileInfoList;
        private FileInfo? _dayzServerExePath;
        private FileInfo? _dayzGameExePath;
        private FileInfo? _modMappingFilePath;
        private FileInfo? _steamCmdPath;
        private FileInfo? _discordFilePath;
        private FileInfo? _dayzServerLogFilePath;

        public bool MuteDiscordBot { get; set; }
        public bool UseSteamCmd { get; set; }

        public FileInfo? DayzServerExePath
        {
            get => _dayzServerExePath;
            set => _dayzServerExePath = value;
        }

        public FileInfo? DayzGameExePath
        {
            get => _dayzGameExePath;
            set => _dayzGameExePath = value;
        }

        public FileInfo? ModMappingFilePath
        {
            get => _modMappingFilePath;
            set => _modMappingFilePath = value;
        }

        public FileInfo? SteamCmdPath
        {
            get => _steamCmdPath;
            set => _steamCmdPath = value;
        }

        public FileInfo? DiscordFilePath
        {
            get => _discordFilePath;
            set => _discordFilePath = value;
        }

        public FileInfo? DayzServerLogFilePath
        {
            get => _dayzServerLogFilePath;
            set => _dayzServerLogFilePath = value;
        }

        public Credential? SteamCredentials
        {
            get => _steamCredentials;
            set => _steamCredentials = value;
        }

        public bool SettingsValid
        {
            get
            {
                switch (UseSteamCmd)
                {
                    case true:
                        return PathsValid && CredentialsValid;

                    case false:
                        return PathsValid;
                }
            }
        }

        private bool PathsValid
        {
            get
            {
                switch (UseSteamCmd)
                {
                    case true:
                        // Take the SteamCmd path into account
                        foreach (FileInfo? fileInfo in _fileInfoList)
                        {
                            if (fileInfo == null)
                                return false;
                        }

                        return true;

                    case false:
                        // SteamCmd path is not necessary
                        foreach (FileInfo? fileInfo in _fileInfoList)
                        {
                            if (fileInfo != null && fileInfo == _steamCmdPath)
                                continue;

                            if (fileInfo == null)
                                return false;
                        }

                        return true;
                }
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

        public ServerControlSettingsWrapper(string steamCredentialsStorageName)
        {
            _steamCredentialsStorageName = steamCredentialsStorageName;
            _fileInfoList = new List<FileInfo?> { _dayzServerExePath, _dayzGameExePath , _modMappingFilePath , 
                _steamCmdPath, _discordFilePath , _dayzServerLogFilePath };

            // Load SettingsWrapper / Paths before getting the Property Values of this class instance
            LoadSettingsFromFile();
        }

        /// <summary>
        /// Loads credentials from Windows Credential Manager
        /// </summary>
        private void LoadSettingsFromFile()
        {
            MuteDiscordBot = DayzCtrlSettings.Default.MuteDiscordBot;
            UseSteamCmd = DayzCtrlSettings.Default.UseSteamCmd;

            LoadSteamCredentials();

            _dayzServerExePath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.DayzServerExePath)
                ? new FileInfo(DayzCtrlSettings.Default.DayzServerExePath)
                : null;

            _dayzGameExePath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.DayzGameExePath)
                ? new FileInfo(DayzCtrlSettings.Default.DayzGameExePath)
                : null;

            _modMappingFilePath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.ModMappingFilePath)
                ? new FileInfo(DayzCtrlSettings.Default.ModMappingFilePath)
                : null;

            _steamCmdPath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.SteamCmdPath)
                ? new FileInfo(DayzCtrlSettings.Default.SteamCmdPath)
                : null;

            _discordFilePath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.DiscordDataFilePath)
                ? new FileInfo(DayzCtrlSettings.Default.DiscordDataFilePath)
                : null;

            _dayzServerLogFilePath = !String.IsNullOrEmpty(DayzCtrlSettings.Default.DayzServerLogFilePath)
                ? new FileInfo(DayzCtrlSettings.Default.DayzServerLogFilePath)
                : null;
        }

        public bool SaveSettingsToFile()
        {
            if (!SettingsValid)
                return false;

            // Save new Credentials if valid
            if (UseSteamCmd && CredentialsValid)
            {
                SaveSteamCredentials();
                DayzCtrlSettings.Default.SteamCmdPath = SteamCmdPath?.FullName;
            }

            DayzCtrlSettings.Default.MuteDiscordBot = MuteDiscordBot;
            DayzCtrlSettings.Default.UseSteamCmd = UseSteamCmd;
            DayzCtrlSettings.Default.DayzServerExePath = DayzServerExePath?.FullName;
            DayzCtrlSettings.Default.DayzGameExePath = DayzGameExePath?.FullName;
            DayzCtrlSettings.Default.ModMappingFilePath = ModMappingFilePath?.FullName;
            DayzCtrlSettings.Default.DiscordDataFilePath = DiscordFilePath?.FullName;
            DayzCtrlSettings.Default.DayzServerLogFilePath = DayzServerLogFilePath?.FullName;
            DayzCtrlSettings.Default.Save();

            return true;
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
            if (SteamCredentials == null)
                return;

            if (!WindowsCredentials.SaveCredentials(SteamCredentials.Username, SteamCredentials.Password,
                    _steamCredentialsStorageName, out _))
                throw new CredentialException($"Could not store Steam Credentials {_steamCredentialsStorageName}");
        }
    }
}
