using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace DayZServerControllerUI.LogParser
{
    public class PlayerStatistics
    {
        public DayZPlayer Player { get; }
        public Dictionary<DateTime, TimeSpan> OnlineTimePerDay { get; }

        public PlayerStatistics(DayZPlayer player, List<LogLine> logLinesOfPlayer)
        {
            Player = player;
            OnlineTimePerDay = CalculateOnlineTime(logLinesOfPlayer);
        }

        private Dictionary<DateTime, TimeSpan> CalculateOnlineTime(List<LogLine> logLines)
        {
            Dictionary<DateTime, TimeSpan> onlineTimeDict = new Dictionary<DateTime, TimeSpan>();

            if (logLines == null || logLines.Count == 0)
                return onlineTimeDict;

            DateTime currentDate = DateTime.MinValue;
            DateTime onlineStartTime = DateTime.Today;
            bool playerIsCurrentlyOnline = false;

            // Sort by TimeStamp ascendingly (first entry is the oldest one)
            List<LogLine> sortedLogLines = logLines.OrderBy(x => x.TimeStamp).ToList();

            foreach (LogLine logLine in sortedLogLines)
            {
                if ((logLine.PlayerName != Player.Name && logLine.EventType != LogEvent.ServerRestart) || !logLine.TimeStamp.HasValue)
                    continue;

                // Player connected
                if (logLine.EventType == LogEvent.PlayerConnected)
                {
                    currentDate = logLine.TimeStamp.Value.Date;

                    if (!onlineTimeDict.ContainsKey(currentDate))
                    {
                        onlineTimeDict[currentDate] = TimeSpan.Zero;
                    }

                    onlineStartTime = logLine.TimeStamp.Value;
                    playerIsCurrentlyOnline = true;

                    continue;
                }

                // Player disconnected
                if (playerIsCurrentlyOnline &&
                    (logLine.EventType == LogEvent.PlayerDisconnected || 
                     logLine.EventType == LogEvent.PlayerKickedUnstableConnection || 
                     logLine.EventType == LogEvent.PlayerKicked))
                {
                    DateTime disconnectTime = logLine.TimeStamp.Value;

                    // Player finished playing, is it already the next day?
                    if (onlineStartTime.Date != disconnectTime.Date)
                    {
                        // Assign the difference until midnight to the previous day and the rest to next one
                        onlineTimeDict[currentDate] += TimeSpan.FromHours(24) - onlineStartTime.TimeOfDay;

                        DateTime nextDayDate = disconnectTime.Date;

                        if(!onlineTimeDict.ContainsKey(nextDayDate))
                            onlineTimeDict.Add(nextDayDate, TimeSpan.Zero);
                        
                        onlineTimeDict[nextDayDate] += disconnectTime.TimeOfDay;
                    }
                    else
                    {
                        // Calculate the time difference
                        onlineTimeDict[currentDate] += logLine.TimeStamp.Value - onlineStartTime;
                    }
                }

                // Server restart -> Disconnect player
                if (playerIsCurrentlyOnline && logLine.EventType == LogEvent.ServerRestart)
                {
                    DateTime disconnectTime = logLine.TimeStamp.Value;

                    // Calculate the time difference
                    onlineTimeDict[currentDate] += disconnectTime - onlineStartTime;
                }
            }

            return onlineTimeDict;
        }

        public PlayerStatisticsDataItem ToDataItem(bool timeInHours = true)
        {
            double totalOnlineTime = 0.0d;
            double maxOnlineTime = 0.0d;

            foreach (var statsPair in OnlineTimePerDay)
            {
                switch (timeInHours)
                {
                    case true:
                        totalOnlineTime += statsPair.Value.TotalHours;

                        if (statsPair.Value.TotalHours > maxOnlineTime)
                            maxOnlineTime = statsPair.Value.TotalHours;

                        break;
                    case false:
                        totalOnlineTime += statsPair.Value.TotalMinutes;

                        if (statsPair.Value.TotalMinutes > maxOnlineTime)
                            maxOnlineTime = statsPair.Value.TotalMinutes;

                        break;
                }
            }

            return new PlayerStatisticsDataItem(Player.ToString(), totalOnlineTime, maxOnlineTime);
        }
    }
}
