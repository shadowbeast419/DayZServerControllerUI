using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;
using CredentialManagement;

namespace DayZServerControllerUI.CtrlLogic
{
    internal class SteamCmdWrapper
    {
        private const string DayZGameId = "221100";
        private const string DayZServerGameId = "223350";

        private readonly FileInfo? _steamCmdPath;
        private bool _isInitialized;
        private Credential? _steamCredentials;

        // Only for PowerShell Mode
        private PSCredential? _psSteamCredentials;
        private PowerShell? _powerShell;
        private readonly DirectoryInfo? _dayzGameDir;
        private readonly DirectoryInfo? _dayzServerDir;

        private readonly List<string> _cliArguments = new();
        private readonly List<string> _defaultCliStartArguments = new();
        private readonly List<string> _defaultCliEndArguments = new();

        public int ModUpdateTasksCount { get; private set; }
        public SteamCmdModeEnum SteamCmdMode { get; }

        public bool IsInitialized => _isInitialized;

        public SteamCmdWrapper(SteamCmdModeEnum cmdMode = SteamCmdModeEnum.Disabled, FileInfo? steamCmdPath = null, 
            DirectoryInfo? dayzServerDir = null, DirectoryInfo? dayzGameDir = null)
        {
            SteamCmdMode = SteamCmdModeEnum.SteamCmdExe;

            _steamCmdPath = steamCmdPath;
            SteamCmdMode = cmdMode;
            _dayzGameDir = dayzGameDir;
            _dayzServerDir = dayzServerDir;
        }

        public bool Init(Credential? steamCredentials)
        {
            _isInitialized = false;

            if (SteamCmdMode == SteamCmdModeEnum.Disabled)
                return true;

            if (_dayzGameDir == null || _dayzServerDir == null)
                return false;

            if (_steamCmdPath == null || !_steamCmdPath.Exists && SteamCmdMode == SteamCmdModeEnum.SteamCmdExe)
                throw new ArgumentException($"SteamCMD Path not valid!");

            if ((!_dayzGameDir.Exists || !_dayzServerDir.Exists) && SteamCmdMode == SteamCmdModeEnum.SteamPowerShellWrapper)
                throw new ArgumentException($"DayZ game or server directory not valid!");

            if (steamCredentials == null)
                throw new ArgumentNullException($"No valid Steam Credentials could be read/stored.");

            _steamCredentials = steamCredentials;

            switch (SteamCmdMode)
            {
                // Only with direct SteamCMD Mode, each command is necessary to append for updating the mods
                case SteamCmdModeEnum.SteamCmdExe:
                    _defaultCliStartArguments.Clear();

                    // Add the default login command to the cli arguments
                    _defaultCliStartArguments.Add($"+login");
                    _defaultCliStartArguments.Add(_steamCredentials.Username);
                    _defaultCliStartArguments.Add(_steamCredentials.Password);
                    // _defaultCliStartArguments.Add($"+set_steam_guard_code {_steamGuardCode}");

                    _defaultCliEndArguments.Add("+quit");

                    break;

                case SteamCmdModeEnum.SteamPowerShellWrapper:
                    _psSteamCredentials =
                        new PSCredential(_steamCredentials.Username, _steamCredentials.SecurePassword);

                    break;
            }

            _isInitialized = true;

            return true;
        }

        public async Task UpdateDayZServer()
        {
            if (!_isInitialized)
                return;

            switch (SteamCmdMode)
            {
                case SteamCmdModeEnum.SteamCmdExe:
                    ResetUpdateTaskList();
                    AddUpdateGameTask(DayZServerGameId);
                    await ExecuteSteamCmdUpdate();

                    break;

                case SteamCmdModeEnum.SteamPowerShellWrapper:
                    _powerShell = PowerShell.Create();
                    _powerShell.AddCommand($"Update-SteamApp");
                    _powerShell.AddParameter("AppID", DayZServerGameId);
                    _powerShell.AddParameter("Path", _dayzServerDir?.Name);
                    _powerShell.AddParameter("Credential", _psSteamCredentials);

                    await _powerShell.InvokeAsync();

                    break;

                case SteamCmdModeEnum.Disabled:
                    return;
            }
        }

        private void AddUpdateGameTask(string gameID)
        {
            if (!_isInitialized || SteamCmdMode == SteamCmdModeEnum.Disabled)
                return;

            if (String.IsNullOrEmpty(gameID))
                throw new ArgumentException("SteamApiWrapper: GameID is invalid.");

            _cliArguments.Add($"\"+app_update");
            _cliArguments.Add($"{gameID}\"");
        }

        /// <summary>
        /// This function only makes sense in SteamCmdExe Mode since the PowerShell Wrapper does this automatically
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="modID"></param>
        /// <exception cref="ArgumentException"></exception>
        public void AddUpdateWorkshopItemTask(string gameID, string modID)
        {
            if (!_isInitialized || SteamCmdMode != SteamCmdModeEnum.SteamCmdExe)
                return;

            if (String.IsNullOrEmpty(gameID) || string.IsNullOrEmpty(modID))
                throw new ArgumentException("SteamApiWrapper: GameID or ModID is invalid.");

            _cliArguments.Add("\"+workshop_download_item");
            _cliArguments.Add(gameID);
            _cliArguments.Add($"{modID}\"");

            ModUpdateTasksCount++;
        }

        public async Task<bool> ExecuteSteamCmdUpdate()
        {
            if (!_isInitialized || SteamCmdMode == SteamCmdModeEnum.Disabled || _steamCmdPath == null)
                return true;

            switch (SteamCmdMode)
            {
                case SteamCmdModeEnum.SteamCmdExe:
                    // Insert first and last arguments which are always the same
                    _cliArguments.InsertRange(0, _defaultCliStartArguments);
                    _cliArguments.InsertRange(_cliArguments.Count, _defaultCliEndArguments);

                    await ProcessHelper.Start(_steamCmdPath, _cliArguments);

                    break;

                // PowerShell Wrapper takes all Mod-Updates into Account (however, not in the same directory as the raw SteamCmd-Mode)
                case SteamCmdModeEnum.SteamPowerShellWrapper:
                    _powerShell = PowerShell.Create();
                    _powerShell.AddCommand($"Update-SteamApp");
                    _powerShell.AddParameter("AppID", DayZGameId);
                    _powerShell.AddParameter("Path", _dayzGameDir?.Name);
                    _powerShell.AddParameter("Credential", _psSteamCredentials);
                    await _powerShell.InvokeAsync();

                    break;
            }

            return true;
        }

        public void ResetUpdateTaskList()
        {
            _cliArguments.Clear();
            ModUpdateTasksCount = 0;
        }
    }
}
