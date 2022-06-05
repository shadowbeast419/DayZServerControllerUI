using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerControllerUI.LogParser
{
    public class PlayerStatisticsDataItem
    {
        public string PlayerName { get; set; }
        public double TotalOnlineTime { get; set; }
        public double MaxOnlineTime { get; set; }

        public PlayerStatisticsDataItem(string playerName, double totalTime, double maxTime)
        {
            PlayerName = playerName;
            TotalOnlineTime = totalTime;
            MaxOnlineTime = maxTime;
        }
    }
}
