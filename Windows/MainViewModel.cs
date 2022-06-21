using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;
using DayZServerControllerUI.Settings;

namespace DayZServerControllerUI.Windows
{
    internal sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        // SteamId necessary for Workshop-Folder Path
        private const string DayzSteamId = $"221100";

        private readonly SettingsWindow _settingsWindow;
        private DiscordBot? _discordBot;
        private SteamCmdWrapper? _steamCmdWrapper;
        private ModManager? _modManager;
        private ModlistReader? _modlistReader;
        private readonly Logging? _logger;
        private DayZServerHelper? _dayZServerHelper;
        private readonly Timer _restartTimer;
        private readonly Timer _modUpdateTimer;
        private bool _isInitialized;
        private double _restartPeriodProgress;
        private bool _modUpdateInProgress;

        public ServerControlSettingsWrapper ServerSettings => _settingsWindow.SettingsWrapper;

        #region Properties for UI Bindings

        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                if(value)
                    Initialized?.Invoke();

                _isInitialized = value;
            }
        }

        public bool SettingsWindowVisible
        {
            get => _settingsWindow.IsVisible;
            set
            {
                switch (value)
                {
                    case true:
                        _settingsWindow.Show();

                        return;

                    case false:
                        _settingsWindow.Hide();

                        return;
                }
            }
        }

        public bool IsServerRunning
        {
            get
            {
                if (!IsInitialized || _dayZServerHelper == null)
                    return false;

                return _dayZServerHelper.IsRunning;
            }
        }
        /// <summary>
        /// Range from 0 - 100
        /// </summary>
        public double RestartPeriodProgress
        {
            get => _restartPeriodProgress;
            set
            {
                if (double.IsNaN(value) || value < 0.0d || value > 100.0d)
                    return;

                _restartPeriodProgress = value;
                OnPropertyChanged(nameof(RestartPeriodProgress));
            }
        }

        /// <summary>
        /// Is true if the ModUpdate Timer detects an update
        /// </summary>
        public bool ModUpdateInProgress => _modUpdateInProgress;

        #endregion

        public event Action? Initialized;
        public event Action? ServerRestarting;
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel(ref Logging logger)
        {
            _logger = logger;

            _restartTimer = new Timer()
            {
                AutoReset = true
            };

            _restartTimer.Elapsed += RestartTimer_Elapsed;

            _modUpdateTimer = new Timer()
            {
                Interval = TimeSpan.FromMinutes(30).TotalMilliseconds,
                AutoReset = true
            };

            _modUpdateTimer.Elapsed += ModUpdateTimer_Elapsed;

            // Create SettingsWrapper Window, but it is hidden at first
            _settingsWindow = new SettingsWindow();
        }

        #region Server Control Logic

        private async Task ValidateSettings()
        {
            // TODO: Create property in SettingsWrapper for FirstStart

            // Open SettingsWrapper Window if first startup or if settings have changed and are not valid
            if (DayzCtrlSettings.Default.FirstStart || !ServerSettings.SettingsValid)
            {
                _isInitialized = false;

                _settingsWindow.SaveButtonClicked += SettingsWindow_SaveButtonClicked;
                _settingsWindow.Show();

                return;
            }

            // Settings are valid, we can start initializing
            await InitializeServerLogicAsync();

            _isInitialized = true;
            Initialized?.Invoke();
        }

        private async Task InitializeServerLogicAsync()
        {
            // Should the Discord Bot be enabled?
            if (!ServerSettings.MuteDiscordBot && ServerSettings.DiscordFilePath != null)
            {
                if (ServerSettings.DiscordFilePath == null)
                    throw new IOException($"Discord-SecretFile Path is empty!");

                _discordBot = new DiscordBot(new DiscordBotData(ServerSettings.DiscordFilePath));
                await _discordBot.Init();
            }

            if (ServerSettings.DayzGameExePath == null)
                throw new IOException($"DayZ-Client Path is empty!");

            if (ServerSettings.DayzServerExePath == null)
                throw new IOException($"DayZ-Server Path is empty!");

            if (ServerSettings.ModMappingFilePath == null)
                throw new IOException($"Modlist.txt Path is empty!");

            // Should SteamCmd be used?
            if (ServerSettings.UseSteamCmd)
            {
                if (ServerSettings.SteamCmdPath == null)
                    throw new IOException($"SteamCmdPath is empty!");

                // TODO: Refactor DirectoryInfo to FileInfo
                // Init SteamCmd Wrapper
                //_steamCmdWrapper = new SteamCmdWrapper(SteamCmdModeEnum.SteamCmdExe, ServerSettings.SteamCmdPath,
                //    ServerSettings.DayzServerExePath, ServerSettings.DayzGameExePath);

                WindowsCredentials.TryGetExistingCredentials($"SteamCredentials2", out Credential? credentials);
                bool success = credentials != null && _steamCmdWrapper != null;

                switch (success)
                {
                    case true:
                        // Init SteamCmd, credentials are necessary
                        if (_steamCmdWrapper == null)
                            throw new ArgumentNullException($"SteamCmdWrapper is null where it shouldn't be null.");

                        _steamCmdWrapper.Init(credentials);

                        break;

                    case false:
                        // No credentials, without them SteamCmd-Mode is not possible
                        MessageBox.Show(
                            $"No Steam Credentials found in Windows Credential Storage. You can save them in the SettingsWrapper Dialog. SteamCmd-Mode is disabled.",
                            $"Notification", MessageBoxButton.OK, MessageBoxImage.Information);

                        break;
                }
            }

            // Init Mod-Checking-Logic
            _modlistReader = new ModlistReader(ServerSettings.ModMappingFilePath);
            _modManager = new ModManager(GetSteamWorkshopFolderFromGameExe(ServerSettings.DayzGameExePath),
                ServerSettings.DayzServerExePath, _modlistReader, _steamCmdWrapper);

            // Init DayZ Server Helper
            _dayZServerHelper = new DayZServerHelper(ServerSettings.DayzServerExePath, ServerSettings.RestartInterval);

            IsInitialized = true;
            Initialized?.Invoke();
        }

        public void StartTimers()
        {
            _restartTimer.Interval = ServerSettings.RestartInterval.TotalMilliseconds;

            _modUpdateTimer.Start();
            _restartTimer.Start();
        }

        #endregion

        #region Helper Functions

        public async Task StartInitializingAsync()
        {
            await ValidateSettings();
        }

        /// <summary>
        /// Attaches the used DiscordBot to the Logger given as parameter
        /// </summary>
        /// <param name="logger"></param>
        public void AttachDiscordBotToLogger(ref Logging logger)
        {
            if (_discordBot != null)
                logger.AttachDiscordBot(_discordBot);
        }

        public void ClearPaths()
        {
            ServerSettings.ClearPaths();
        }

        private DirectoryInfo GetSteamWorkshopFolderFromGameExe(FileInfo? dayzClientExePath)
        {
            string dayzClientPath = dayzClientExePath != null ? dayzClientExePath.FullName : String.Empty;

            if (dayzClientExePath == null || !File.Exists(dayzClientPath))
                throw new IOException($"Steam-Client Path is not valid! ({dayzClientPath})");

            if (dayzClientExePath.Directory?.Parent?.Exists == false)
                throw new IOException($"\"/steamapps/common\" Folder does not exist!");

            // steamapps/common
            DirectoryInfo? commonFolder = dayzClientExePath.Directory?.Parent;

            if (commonFolder?.Exists != true)
                throw new IOException($"Steam-Common Folder not found! ({commonFolder?.Name ?? String.Empty})");

            // Steam/steamapps
            DirectoryInfo? steamAppsFolder = commonFolder.Parent;
            string dayzWorkshopPath = steamAppsFolder != null
                ? Path.Combine(steamAppsFolder.FullName, $"workshop/content/{DayzSteamId}/") : String.Empty;

            if (String.IsNullOrEmpty(dayzWorkshopPath) || !Directory.Exists(dayzWorkshopPath))
                throw new IOException(
                    $"Steam-Workshop folder not found in assumed path! ({dayzWorkshopPath ?? String.Empty})");

            return new DirectoryInfo(dayzWorkshopPath);
        }

        #endregion

        #region Events

        /// <summary>
        /// User has saved new (valid) settings, re-initialize the ServerController
        /// </summary>
        private async void SettingsWindow_SaveButtonClicked()
        {
            await InitializeServerLogicAsync();

            _isInitialized = true;
            Initialized?.Invoke();
        }

        private async void ModUpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_modManager == null)
                throw new NullReferenceException($"ModUpdateTimer: Mod-Manager Object is null!");

            if (_logger == null)
                throw new NullReferenceException($"ModUpdateTimer: Logger Object is null!");

            if (_dayZServerHelper == null)
                throw new NullReferenceException($"ModUpdateTimer: Dayz-Server-Helper Object is null!");

            // Is an update available / are the local directories out of sync?
            if (!_modManager.ModUpdateAvailable)
                return;

            _modUpdateInProgress = true;
            OnPropertyChanged(nameof(ModUpdateInProgress));

            await _logger.WriteLineAsync($"Mods need an update. Restarting in 5 Minutes!");

            await Task.Delay(TimeSpan.FromMinutes(5));

            await _logger.WriteLineAsync("Stopping server now...");
            _dayZServerHelper.StopServer();
            _dayZServerHelper.StopRestartTimer();

            await Task.Delay(TimeSpan.FromSeconds(20));

            await _logger.WriteLineAsync("Syncing Workshop-Mod-Folder with Server-Mod-Folder...", false);

            int syncedModsLocal = await _modManager.SyncWorkshopWithServerModsAsync();
            await _logger.WriteLineAsync($"Synced {syncedModsLocal} Mod(s) locally");

            await _logger.WriteLineAsync($"Restarting server now.");
            _dayZServerHelper.StartServer(_modManager.ServerFolderModDirectoryNames);
            _dayZServerHelper.StartRestartTimer();

            await _logger.WriteLineAsync($"Server started! Next restart scheduled at " +
                                         $"{(_dayZServerHelper.TimeOfNextRestart.HasValue ? _dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");

            _modUpdateInProgress = false;
            OnPropertyChanged(nameof(ModUpdateInProgress));
        }

        private async void RestartTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_modManager == null)
                throw new NullReferenceException($"RestartTimer: Mod-Manager Object is null!");

            if (_logger == null)
                throw new NullReferenceException($"RestartTimer: Logger Object is null!");

            if (_dayZServerHelper == null)
                throw new NullReferenceException($"RestartTimer: Dayz-Server-Helper Object is null!");

            ServerRestarting?.Invoke();

            await _logger.WriteLineAsync("Server Restart-Timer Elapsed, restarting now.");
            await _logger.WriteLineAsync($"Stopping server now.", false);

            _dayZServerHelper.StopServer();
            _dayZServerHelper.StopRestartTimer();
            await Task.Delay(TimeSpan.FromSeconds(20));

            // await logger.WriteLineAsync($"Checking for DayZServer Updates...", false);
            // await steamApiWrapper.UpdateDayZServer();

            await _logger.WriteLineAsync($"Restarting server now.", false);
            _dayZServerHelper.StartServer(_modManager.ServerFolderModDirectoryNames);
            _dayZServerHelper.StartRestartTimer();

            await _logger.WriteLineAsync($"Server started! Next restart scheduled: " +
                                         $"{(_dayZServerHelper.TimeOfNextRestart.HasValue ? _dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public void Dispose()
        {
            _settingsWindow.Close();
        }
    }
}
