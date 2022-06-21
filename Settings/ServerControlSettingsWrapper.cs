using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;
using Ookii.Dialogs.Wpf;

namespace DayZServerControllerUI.Settings
{
    public class ServerControlSettingsWrapper
    {
        private enum SelectablePaths
        {
            DayzServerExe,
            DayzGameExe,
            ModMappingFile,
            SteamCmd,
            DiscordFile,
            DayzServerLogFile
        }

        private readonly string _steamCredentialsStorageName;
        private Credential? _steamCredentials;

        // Stores all by the user selected paths 
        private readonly Dictionary<SelectablePaths, FileInfo?> _fileInfoList;
        private FileInfo? _dayzServerExePath;
        private FileInfo? _dayzGameExePath;
        private FileInfo? _modMappingFilePath;
        private FileInfo? _steamCmdPath;
        private FileInfo? _discordFilePath;
        private FileInfo? _dayzServerLogFilePath;
        private TimeSpan? _restartInterval;

        public TimeSpan RestartInterval
        {
            get
            {
                // Return default value of 4hours if no value has been set
                if (!_restartInterval.HasValue)
                    return new TimeSpan(0, 4, 0, 0);

                return _restartInterval.Value;
            }
            set => _restartInterval = value;
        }

        public bool MuteDiscordBot { get; set; }
        public bool UseSteamCmd { get; set; }

        public FileInfo? DayzServerExePath
        {
            get => _dayzServerExePath;
            set
            {
                if (value == null || !value.Exists)
                    return;

                _fileInfoList[SelectablePaths.DayzServerExe] = value;
                _dayzServerExePath = value;
            }
        }

        public FileInfo? DayzGameExePath
        {
            get => _dayzGameExePath;
            set
            {
                if (value == null || !value.Exists)
                    return;

                _fileInfoList[SelectablePaths.DayzGameExe] = value;
                _dayzGameExePath = value;
            }
        }

        public FileInfo? ModMappingFilePath
        {
            get => _modMappingFilePath;
            set
            {
                if (value == null || !value.Exists)
                    return;

                _fileInfoList[SelectablePaths.ModMappingFile] = value;
                _modMappingFilePath = value;
            } 
        }

        public FileInfo? SteamCmdPath
        {
            get => _steamCmdPath;
            set
            {
                if (value == null || !value.Exists)
                    return;

                _fileInfoList[SelectablePaths.SteamCmd] = value;
                _steamCmdPath = value;
            }
        }

        public FileInfo? DiscordFilePath
        {
            get => _discordFilePath;
            set
            {
                if (value == null || !value.Exists)
                    return;

                _fileInfoList[SelectablePaths.DiscordFile] = value;
                _discordFilePath = value;
            }
        }

        public FileInfo? DayzServerLogFilePath
        {
            get => _dayzServerLogFilePath;
            set
            {
                if (value == null || !value.Exists)
                    return;

                _fileInfoList[SelectablePaths.DayzServerLogFile] = value;
                _dayzServerLogFilePath = value;
            }
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
                        // Check all paths (including SteamCmd Path)
                        return _fileInfoList.Values.All(x => x != null && x.Exists);

                    case false:

                        // Check all paths except SteamCmd path
                        return _fileInfoList.Where(keyValuePair => keyValuePair.Key != SelectablePaths.SteamCmd).ToList().Select(keyValuePair => keyValuePair.Value)
                            .All(fileInfo => fileInfo != null && fileInfo.Exists);
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
            _fileInfoList = new();

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
            RestartInterval = DayzCtrlSettings.Default.ServerRestartPeriod;

            LoadSteamCredentials();

            // Store the FileInfos from the SettingsFile to a Dictionary with all possible SelectablePaths 
            foreach (SelectablePaths selectablePath in Enum.GetValues(typeof(SelectablePaths)))
            {
                switch (selectablePath)
                {
                    case SelectablePaths.DayzServerExe:
                        DayzServerExePath = GetPathFromSettings(selectablePath);

                        break;
                    case SelectablePaths.DayzGameExe:
                        DayzGameExePath = GetPathFromSettings(selectablePath);

                        break;
                    case SelectablePaths.ModMappingFile:
                        ModMappingFilePath = GetPathFromSettings(selectablePath);

                        break;
                    case SelectablePaths.SteamCmd:
                        SteamCmdPath = GetPathFromSettings(selectablePath);

                        break;
                    case SelectablePaths.DiscordFile:
                        DiscordFilePath = GetPathFromSettings(selectablePath);

                        break;
                    case SelectablePaths.DayzServerLogFile:
                        DayzServerLogFilePath = GetPathFromSettings(selectablePath);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedPath"></param>
        /// <param name="settingsString"></param>
        private FileInfo? GetPathFromSettings(SelectablePaths selectedPath)
        {
            switch (selectedPath)
            {
                case SelectablePaths.DayzServerExe:
                    return !String.IsNullOrEmpty(DayzCtrlSettings.Default.DayzServerExePath)
                        ? new FileInfo(DayzCtrlSettings.Default.DayzServerExePath)
                        : null;

                case SelectablePaths.DayzGameExe:
                    return !String.IsNullOrEmpty(DayzCtrlSettings.Default.DayzGameExePath)
                        ? new FileInfo(DayzCtrlSettings.Default.DayzGameExePath)
                        : null;

                case SelectablePaths.ModMappingFile:
                    return !String.IsNullOrEmpty(DayzCtrlSettings.Default.ModMappingFilePath)
                        ? new FileInfo(DayzCtrlSettings.Default.ModMappingFilePath)
                        : null;

                case SelectablePaths.SteamCmd:
                    return !String.IsNullOrEmpty(DayzCtrlSettings.Default.SteamCmdPath)
                        ? new FileInfo(DayzCtrlSettings.Default.SteamCmdPath)
                        : null;

                case SelectablePaths.DiscordFile:
                    return !String.IsNullOrEmpty(DayzCtrlSettings.Default.DiscordDataFilePath)
                        ? new FileInfo(DayzCtrlSettings.Default.DiscordDataFilePath)
                        : null;

                case SelectablePaths.DayzServerLogFile:
                    return !String.IsNullOrEmpty(DayzCtrlSettings.Default.DayzServerLogFilePath)
                        ? new FileInfo(DayzCtrlSettings.Default.DayzServerLogFilePath)
                        : null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selectedPath), selectedPath, null);
            }
        }

        /// <summary>
        /// Maps the path to the correct path-setting
        /// </summary>
        /// <param name="selectablePath"></param>
        /// <param name="pathToStore"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void StorePathToSettings(SelectablePaths selectablePath, FileInfo? pathToStore)
        {
            string pathString = pathToStore != null && pathToStore.Exists ? pathToStore.FullName : String.Empty;

            switch (selectablePath)
            {
                case SelectablePaths.DayzServerExe:
                    DayzCtrlSettings.Default.DayzServerExePath = pathString;

                    break;
                case SelectablePaths.DayzGameExe:
                    DayzCtrlSettings.Default.DayzGameExePath = pathString;

                    break;
                case SelectablePaths.ModMappingFile:
                    DayzCtrlSettings.Default.ModMappingFilePath = pathString;

                    break;
                case SelectablePaths.SteamCmd:
                    DayzCtrlSettings.Default.SteamCmdPath = pathString;

                    break;
                case SelectablePaths.DiscordFile:
                    DayzCtrlSettings.Default.DiscordDataFilePath = pathString;

                    break;
                case SelectablePaths.DayzServerLogFile:
                    DayzCtrlSettings.Default.DayzServerLogFilePath = pathString;

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selectablePath), selectablePath, null);
            }

            DayzCtrlSettings.Default.Save();
        }

        public void SaveSettingsToFile()
        {
            if (!SettingsValid)
                return;

            // Save new Credentials if valid
            if (UseSteamCmd && CredentialsValid)
            {
                SaveSteamCredentials();
                DayzCtrlSettings.Default.SteamCmdPath = SteamCmdPath?.FullName;
            }

            DayzCtrlSettings.Default.MuteDiscordBot = MuteDiscordBot;
            DayzCtrlSettings.Default.UseSteamCmd = UseSteamCmd;
            DayzCtrlSettings.Default.ServerRestartPeriod = RestartInterval;

            // Store the FileInfos in the Settings File
            foreach (SelectablePaths selectablePath in Enum.GetValues(typeof(SelectablePaths)))
            {
                if(_fileInfoList[selectablePath] == null)
                    continue;

                #pragma warning disable CS8604
                StorePathToSettings(selectablePath, _fileInfoList[selectablePath]);
                #pragma warning restore CS8604
            }
        }

        /// <summary>
        /// Clears all paths in the settings file
        /// </summary>
        public void ClearPaths()
        {
            foreach (SelectablePaths selectablePath in Enum.GetValues(typeof(SelectablePaths)))
            {
                if (_fileInfoList[selectablePath] == null)
                    continue;

                #pragma warning disable CS8604
                StorePathToSettings(selectablePath, null);
                #pragma warning restore CS8604
            }
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
