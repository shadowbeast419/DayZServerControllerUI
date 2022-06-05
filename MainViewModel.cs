using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Timers;
using CredentialManagement;
using DayZServerControllerUI.CtrlLogic;

// ReSharper disable All

namespace DayZServerControllerUI
{
    internal class MainViewModel
    {
        // SteamId necessary for Workshop-Folder Path
        private const string DayzSteamId = $"221100";

        private DiscordBot? _discordBot;
        private SteamCmdWrapper? _steamCmdWrapper;
        private ModManager? _modManager;
        private ModlistReader? _modlistReader;
        private Logging? _logger;
        private DayZServerHelper? _dayZServerHelper;
        private Timer _restartTimer;
        private Timer _modUpdateTimer;

        public bool IsInitialized { get; private set; }

        public bool IsServerRunning
        {
            get
            {
                if (!IsInitialized || _dayZServerHelper == null)
                    return false;

                return _dayZServerHelper.IsRunning;
            }
        }

        public event Action ModUpdateDetected;
        public event Action ServerRestarting;

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

            IsInitialized = false;
        }

        public async Task Initialize()
        {
            FileInfo? discordDataFileInfo = null;
            string dataFilePath = Settings.Default.DiscordDataFilePath ?? String.Empty;

            if (File.Exists(dataFilePath))
            {
                discordDataFileInfo = new FileInfo(dataFilePath);
            }

            // Should the Discord Bot be enabled?
            if (!Settings.Default.MuteDiscordBot && discordDataFileInfo != null)
            {
                _discordBot = new DiscordBot(new DiscordBotData(discordDataFileInfo));
                await _discordBot.Init();
            }

            // Check DayZ Client Path
            string dayzClientPath = Settings.Default.DayzGameExePath ?? String.Empty;

            if (!File.Exists(dayzClientPath))
                throw new IOException($"DayZ-Client Path not valid! ({dayzClientPath})");

            FileInfo? dayzClientInfo = new FileInfo(dayzClientPath);

            // Check DayZ Server Path
            string dayzServerPath = Settings.Default.DayzServerExePath ?? String.Empty;

            if (!File.Exists(dayzServerPath))
                throw new IOException($"DayZ-Server Path not valid! ({dayzServerPath ?? String.Empty})");

            FileInfo? dayzServerExePath = new FileInfo(dayzServerPath);

            // Check Modlist Path
            string modlistPath = Settings.Default.ModMappingFilePath ?? String.Empty;

            if (String.IsNullOrEmpty(modlistPath) || !File.Exists(modlistPath))
                throw new IOException($"Modlist.txt Path not valid! ({modlistPath})");

            // Should SteamCmd be used?
            bool useSteamCmdForUpdates = Settings.Default.UseSteamCmd;

            // Init SteamCmd Wrapper
            string steamCmdPath = Settings.Default.SteamCmdPath ?? String.Empty;

            if (!File.Exists(steamCmdPath))
                throw new IOException($"SteamCmdPath not valid! ({steamCmdPath})");

            if (dayzClientInfo.Directory != null && dayzServerExePath.Directory != null)
                _steamCmdWrapper = new SteamCmdWrapper(SteamCmdModeEnum.SteamCmdExe, new FileInfo(steamCmdPath),
                    dayzServerExePath.Directory, dayzClientInfo.Directory);

            // Init Mod-Checking-Logic
            _modlistReader = new ModlistReader(new FileInfo(modlistPath));
            _modManager = new ModManager(GetSteamWorkshopFolderFromGameExe(dayzClientInfo),
                dayzServerExePath, _modlistReader, _steamCmdWrapper);

            WindowsCredentials.TryGetExistingCredentials($"SteamCredentials2", out Credential? credentials);
            bool success = credentials != null;

            switch (success)
            {
                case true:
                    // Init SteamCmd, credentials are necessary
                    _steamCmdWrapper.Init(credentials);

                    break;

                case false:
                    // No credentials, without them SteamCmd-Mode is not possible
                    MessageBox.Show(
                        $"No Steam Credentials found in Windows Credential Storage. You can save them in the Settings Dialog. SteamCmd-Mode is disabled.",
                        $"Notification", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;
            }

            TimeSpan restartInterval;

            // Invalid Restart Interval -> Use default interval
            if (Settings.Default.ServerRestartPeriodMinutes <= 0)
                restartInterval = DayZServerHelper.DefaultRestartInterval;
            else
                restartInterval = TimeSpan.FromMinutes(Settings.Default.ServerRestartPeriodMinutes);

            // Init DayZ Server Helper
            _dayZServerHelper = new DayZServerHelper(dayzServerExePath, restartInterval);

            IsInitialized = true;
        }

        /// <summary>
        /// Attaches the used DiscordBot to the Logger given as parameter
        /// </summary>
        /// <param name="logger"></param>
        public void AttachDiscordBotToLogger(ref Logging logger)
        {
            if(_discordBot != null)
                logger.AttachDiscordBot(_discordBot);
        }

        public void StartTimers()
        {
            _restartTimer.Interval =
                TimeSpan.FromMinutes(Settings.Default.ServerRestartPeriodMinutes).TotalMilliseconds;

            _modUpdateTimer.Start();
            _restartTimer.Start();
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

            ModUpdateDetected?.Invoke();

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
    }
}
