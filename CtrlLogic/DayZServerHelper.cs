using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DayZServerControllerUI.CtrlLogic
{
    internal class DayZServerHelper
    {
        private readonly FileInfo? _dayzServerPath;
        private readonly string _dayZServerProcName;
        private readonly TimeSpan _restartInterval;
        private readonly System.Timers.Timer _restartTimer;
        private DateTime? _startTime;
        private bool _timerStoppedManually = false;

        private int ServerPort { get; set; } = 2302;
        private string ServerConfig { get; set; } = "serverDZ.cfg";
        private string ProfileFolderName { get; set; } = "Profiles";


        public bool IsRunning => ProcessHelper.IsRunning(_dayZServerProcName);

        public TimeSpan? TimeUntilNextRestart
        {
            get
            {
                DateTime? timeOfNextRestart = TimeOfNextRestart;

                if (!timeOfNextRestart.HasValue)
                    return null;

                if (TimeOfNextRestart != null)
                    return TimeOfNextRestart.Value - DateTime.Now;

                return null;
            }
        }

        public DateTime? TimeOfNextRestart
        {
            get
            {
                if (_restartTimer.Enabled == false || !_startTime.HasValue)
                    return null;

                return _startTime.Value + _restartInterval;
            }
        }

        public static readonly TimeSpan DefaultRestartInterval = TimeSpan.FromHours(4);

        public event Action? RestartTimerElapsed; 

        public DayZServerHelper(FileInfo? dayzServerPath, TimeSpan? restartInterval)
        {
            if (dayzServerPath == null || !dayzServerPath.Exists)
            {
                throw new ArgumentException($"No valid name for DayZServer-Executable or path not found! " +
                                            $"({(dayzServerPath != null ? dayzServerPath.FullName : String.Empty)})");
            }

            _dayzServerPath = dayzServerPath;
            _restartInterval = restartInterval ?? DefaultRestartInterval;

            // Process Name is the same as the filename without extension
            _dayZServerProcName = _dayzServerPath.Name.Replace(_dayzServerPath.Extension, String.Empty);

            if (restartInterval == null)
            {
                throw new ArgumentException($"Invalid Restart-Interval!");
            }

            _restartTimer = new System.Timers.Timer();
            _restartTimer.Elapsed += RestartTimer_Elapsed;
            _restartTimer.Interval = _restartInterval.TotalMilliseconds;
            _restartTimer.AutoReset = true;
        }

        public void StartServer(IEnumerable<string> modsToEnable)
        {
            List<string> cliArguments = new List<string>();

            cliArguments.Add($"-config={ServerConfig}");
            cliArguments.Add($"-port={ServerPort}");
            cliArguments.Add("-dologs");
            cliArguments.Add("-adminlog");
            cliArguments.Add("-netlog");
            cliArguments.Add("-freezecheck");

            List<string> modsAlreadyAdded = new List<string>();

            // Prioritize the mods where the sorting is relevant
            if (modsToEnable.Contains("@CF"))
                modsAlreadyAdded.Add("@CF");

            if(modsToEnable.Contains("@Dabs-Framework"))
                modsAlreadyAdded.Add("@Dabs-Framework");

            if (modsToEnable.Contains("@Community-Online-Tools"))
                modsAlreadyAdded.Add("@Community-Online-Tools");

            if (modsToEnable.Contains("@DayZ-Expansion-Licensed"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Licensed");

            if (modsToEnable.Contains("@DayZ-Expansion-Core"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Core");

            if (modsToEnable.Contains("@DayZ-Expansion"))
                modsAlreadyAdded.Add("@DayZ-Expansion");

            if (modsToEnable.Contains("@DayZ-Expansion-Book"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Book");

            if (modsToEnable.Contains("@DayZ-Expansion-Market"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Market");

            if (modsToEnable.Contains("@DayZ-Expansion-Vehicles"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Vehicles");

            // Add the rest
            foreach(string modToEnable in modsToEnable)
            {
                if (!modsAlreadyAdded.Contains(modToEnable))
                    modsAlreadyAdded.Add(modToEnable);
            }

            StringBuilder modStringBuilder = new StringBuilder();

            foreach(string modToAdd in modsAlreadyAdded)
            {
                modStringBuilder.Append(modToAdd + ";");
            }

            cliArguments.Add($"\"-mod={modStringBuilder}\"");
            cliArguments.Add($"\"-profiles={ProfileFolderName}\"");

            Console.WriteLine($"Starting DayZServer with generated CLI-Arguments.");
            Console.WriteLine($"Arguments: {String.Join(' ', cliArguments)}");

            ProcessHelper.Start(_dayzServerPath, cliArguments);
        }

        public void StartRestartTimer()
        {
            _restartTimer.Start();
            _startTime = DateTime.Now;
        }

        public void StopServer()
        {
            int killedProcs = ProcessHelper.Kill(_dayZServerProcName);
            Console.WriteLine($"Killed {killedProcs} DayZServer Processes");
        }

        public void StopRestartTimer()
        {
            _timerStoppedManually = true;
            _restartTimer.Stop();
            _timerStoppedManually = false;
        }

        private void RestartTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if(!_timerStoppedManually)
                RestartTimerElapsed?.Invoke();
        }
    }
}
