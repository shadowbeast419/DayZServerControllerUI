using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DayZServerControllerUI.CtrlLogic
{
    internal class ModManager
    {
        public readonly int DayZGameId = 221100;

        private readonly DirectoryInfo _workshopModFolder;
        private readonly DirectoryInfo _dayzServerFolder;
        private Dictionary<long, string> _modListDict = new();

        // Stores the Workshop Mod Directories as keys and the DayZ-Server Mod Directories as values
        private readonly Dictionary<DirectoryInfo, DirectoryInfo> _workshopServerModFolderDir;
        private readonly ModlistReader _modlistReader;
        private MultipleFileWatchers? _modFileWatchers;
        private readonly SteamCmdWrapper? _steamCmdWrapper;

        /// <summary>
        /// Names of all Mod Folders in DayZ-Server Directory (from Modlist)
        /// </summary>
        public IEnumerable<string> ServerFolderModDirectoryNames
        {
            get
            {
                return _workshopServerModFolderDir.Values.Select(x => x.Name);
            }
        }

        public bool ModUpdateAvailable
        {
            get
            {
                foreach (var sourceDestTuple in _workshopServerModFolderDir)
                {
                    if (!MultipleFileWatchers.CheckIfDirectoryContentsAreEqual(sourceDestTuple.Key, sourceDestTuple.Value))
                        return true;
                }

                return false;
            }
        }

        public ModManager(DirectoryInfo workshopModFolder, FileInfo dayzServerExeInfo, ModlistReader modlistReader, SteamCmdWrapper? steamCmdWrapper)
        {
            if (!workshopModFolder.Exists)
            {
               throw new ArgumentException($"Mod Source-Directory not found! ({workshopModFolder.FullName})");
            }

            if (!dayzServerExeInfo.Exists)
            {
                throw new ArgumentException($"DayZServer-Exe directory not found! ({dayzServerExeInfo.FullName})");
            }

            _workshopModFolder = workshopModFolder;

            if (dayzServerExeInfo.DirectoryName != null)
                _dayzServerFolder = new DirectoryInfo(dayzServerExeInfo.DirectoryName);

            _modlistReader = modlistReader;
            _workshopServerModFolderDir = new Dictionary<DirectoryInfo, DirectoryInfo>();
            _steamCmdWrapper = steamCmdWrapper;
        }

        /// <summary>
        /// Updates the mods from the Modlist
        /// </summary>
        /// <returns></returns>
        public async Task UpdateModDirsFromModlistAsync()
        {
            Console.WriteLine("Fetching Mods from Modlist...");
            _modListDict = await _modlistReader.GetModsFromFile() ?? new Dictionary<long, string>();

            Console.WriteLine($"{_modListDict.Count} Mods found in file.");

            _workshopServerModFolderDir.Clear();

            // Get all ModFolders from the WorkshopFolder (with IDs as names)
            IEnumerable<string> allModDirectories = _workshopModFolder.GetDirectories().Select(x => x.Name);

            // Check if all Mods from the Modlist are present inside the Mod Directory
            foreach (var modKeyValuePair in _modListDict)
            {
                // Is the mod missing?
                var modDirectories = allModDirectories.ToList();

                if (!modDirectories.Contains(modKeyValuePair.Key.ToString()))
                {
                    Console.WriteLine($"WARNING: Mod {modKeyValuePair.Value} with ID {modKeyValuePair.Key} missing in Mod-Directory!");
                }
                else
                {
                    string pathToWorkshopModFolder = Path.Combine(_workshopModFolder.FullName, modKeyValuePair.Key.ToString());
                    string pathToServerModFolder = Path.Combine(_dayzServerFolder.FullName, modKeyValuePair.Value);

                    _workshopServerModFolderDir.Add(new DirectoryInfo(pathToWorkshopModFolder), new DirectoryInfo(pathToServerModFolder));
                }
            }
        }

        /// <summary>
        /// Function calls SteamCMD which checks for updates of all mods, FileWatchers detect changes
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        public async Task<int> DownloadModUpdatesViaSteamAsync()
        {
            if (_steamCmdWrapper == null || _steamCmdWrapper.SteamCmdMode == SteamCmdModeEnum.Disabled)
                return 0;

            // Watch the Workshop Mod Directories for changes
            _modFileWatchers = new MultipleFileWatchers(_workshopServerModFolderDir.Keys);

            // Start observing all necessary mod folders
            _modFileWatchers.StartWatching();

            if (_steamCmdWrapper.SteamCmdMode == SteamCmdModeEnum.SteamCmdExe)
            {
                _steamCmdWrapper.ResetUpdateTaskList();

                foreach (var modKeyValuePair in _modListDict)
                {
                    Console.WriteLine($"Adding task for checking for updates of Mod {modKeyValuePair.Value}...");
                    _steamCmdWrapper.AddUpdateWorkshopItemTask(DayZGameId.ToString(), modKeyValuePair.Key.ToString());
                }

                Console.WriteLine($"Executing steamCMD-process with {_steamCmdWrapper.ModUpdateTasksCount} ModUpdate-Tasks...");
            }

            if (_steamCmdWrapper.SteamCmdMode != SteamCmdModeEnum.Disabled)
            {
                // Force Update either via SteamCMD directly or via PowerShell
                bool success = await _steamCmdWrapper.ExecuteSteamCmdUpdate();
                Console.WriteLine($"Closed SteamAPI-process. Success: {success}.");
            }

            IList<DirectoryInfo> changedModList = _modFileWatchers.EndWatching();
            List<DirectoryInfo> modsToCopy = new List<DirectoryInfo>();

            foreach(DirectoryInfo modPath in changedModList)
            {
                if (!modsToCopy.Contains(modPath))
                    modsToCopy.Add(modPath);
            }

            Console.WriteLine($"{changedModList.Count} Mods have been updated by SteamAPI.");

            return changedModList.Count;
        }

        /// <summary>
        /// Syncs Mod Folder contents locally. 
        /// </summary>
        /// <param></param>
        /// <returns></returns>
        public async Task<int> SyncWorkshopWithServerModsAsync()
        {
            int modsChanged = 0;

            foreach (var sourceDestTuple in _workshopServerModFolderDir)
            {
                if (!MultipleFileWatchers.CheckIfDirectoryContentsAreEqual(sourceDestTuple.Key, sourceDestTuple.Value))
                {
                    // Copy the content of the updated mod to the server mod folder
                    Console.WriteLine($"Mod {sourceDestTuple.Value.Name} has a different file! " +
                                      $"Starting to copy to server mod folder.");

                    using (var copyWorker = new DirectoryCopyWorker(sourceDestTuple.Key, sourceDestTuple.Value))
                    {
                        await copyWorker.CopyDirectory();
                    }

                    modsChanged++;
                }
            }

            return modsChanged;
        }
    }
}
