using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DayZServerControllerUI.LogParser
{
    public class LogParserViewModel : INotifyPropertyChanged
    {
        private DayZServerControllerUI.LogParser.LogParser _logParser;
        private System.Timers.Timer _refreshTimer;
        private readonly List<DayZPlayer> _playerList;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<LogLine> LogLines { get; set; }
        public ObservableCollection<PlayerStatistics> OnlineStatistics { get; set; }

        public LogParserViewModel()
        {
            _playerList = new List<DayZPlayer>();

            LogLines = new ObservableCollectionListSource<LogLine>();
            OnlineStatistics = new ObservableCollection<PlayerStatistics>();
        }

        public void Init()
        {
            _logParser = new DayZServerControllerUI.LogParser.LogParser();

            _refreshTimer = new System.Timers.Timer()
            {
                Interval = 30000.0,
                AutoReset = true
            };

            _refreshTimer.Elapsed += RefreshTimer_Elapsed;
            _refreshTimer.Start();

            // Manual call at startup
            RefreshTimer_Elapsed(null, null);
        }

        private async void RefreshTimer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            List<LogLine> newLogLines = await _logParser.GetNewLogLines();

            if (newLogLines.Count == 0)
                return;

            List<DayZPlayer> playerListBefore;
            List<LogLine> logLinesBefore;
            List<DayZPlayer> playersToAdd;
            List<LogLine> logLinesToAdd;

            await using (var db = new LoggingDbContext())
            {
                playerListBefore = await db.Players.AsNoTracking().ToListAsync(CancellationToken.None);
                logLinesBefore = await db.LogLines.AsNoTracking().ToListAsync(CancellationToken.None);

                playersToAdd = new List<DayZPlayer>();
                logLinesToAdd = new List<LogLine>();

                foreach (LogLine logLine in newLogLines)
                {
                    if (!playerListBefore.Select(x => x.Name).Contains(logLine.Player.Name) &&
                        !playersToAdd.Select(x => x.Name).Contains(logLine.Player.Name))
                    {
                        playersToAdd.Add(logLine.Player);
                    }

                    if (!logLinesBefore.Contains(logLine) && !logLinesToAdd.Contains(logLine))
                    {
                        logLinesToAdd.Add(logLine);
                    }
                }

                var players = db.Set<DayZPlayer>();
                await players.AddRangeAsync(playersToAdd);

                await db.SaveChangesAsync(CancellationToken.None);

                var logLines = db.Set<LogLine>();
                await logLines.AddRangeAsync(logLinesToAdd);

                await db.SaveChangesAsync(CancellationToken.None);
            }

            // Save the new player list into the models's collection
            _playerList.Clear();
            _playerList.AddRange(playerListBefore);
            _playerList.AddRange(playersToAdd);

            // Remove player with empty name (server restart event)
            _playerList.RemoveAll(x => String.IsNullOrEmpty(x.Name) || x.Name.StartsWith("Survivor"));

            LogLines.Clear();
            logLinesBefore.ForEach(x => LogLines.Add(x));
            logLinesToAdd.ForEach(x => LogLines.Add(x));

            // Calculate statistics for each player
            foreach (DayZPlayer player in _playerList)
            {
                OnlineStatistics.Add(new PlayerStatistics(player, LogLines.Where(x => x.PlayerName == player.Name).ToList()));
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OnlineStatistics)));
        }
    }
}
