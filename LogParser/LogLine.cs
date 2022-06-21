using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents.DocumentStructures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayZServerControllerUI.LogParser
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; }
        protected Entity(Guid id) => Id = id;
        protected Entity() : this(Guid.Empty) { }
        public bool IsNewEntity => Id == Guid.Empty;
    }

    public enum LogEvent : int
    {
        PlayerConnected = 0,
        PlayerDisconnected,
        PlayerDied,
        PlayerKicked,
        PlayerKickedUnstableConnection,
        ServerRestart,
        None
    }

    [Index(nameof(Name), nameof(SteamID))]
    [ComplexType]
    public class DayZPlayer : Entity
    {
        [Key]
        public string Name { get; set; }
        public string SteamID { get; set; }

        public DayZPlayer()
        {
            Name = string.Empty;
            SteamID = string.Empty;
        }

        public DayZPlayer(string name, string steamId)
        {
            Name = name;
            SteamID = steamId;
        }

        [NotMapped]
        public bool IsValid => !string.IsNullOrEmpty(Name);

        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==(DayZPlayer left, DayZPlayer right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(DayZPlayer left, DayZPlayer right)
        {
            return !(left == right);
        }

        protected bool Equals(DayZPlayer other)
        {
            return Name == other.Name && SteamID == other.SteamID;
        }
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DayZPlayer)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, SteamID);
        }
    }

    [ComplexType]
    [Index(nameof(PlayerName), nameof(Message))]
    public class LogLine
    {
        [Column("EventType")]
        public LogEvent EventType { get; set; }

        [NotMapped]
        public DayZPlayer Player { get; set; }

        public DateTime? TimeStamp { get; set; }

        private string _playerName;
        public string PlayerName
        {
            get
            {
                _playerName = Player.Name;
                return _playerName;
            }
            set => _playerName = value;
        }

        private int _primaryKey = 0;

        [Key]
        public int PrimaryKey
        {
            get
            {
                _primaryKey = GetHashCode();
                return _primaryKey;
            }
            set => _primaryKey = value;
        }

        public string? Message { get; set; }

        public LogLine()
        {
            _playerName = string.Empty;

            EventType = LogEvent.None;
            Player = new DayZPlayer();
            TimeStamp = null;
        }

        public LogLine(LogEvent eventType, string playerName, string steamID, DateTime timeStamp)
        {
            _playerName = string.Empty;

            EventType = eventType;
            Player = new DayZPlayer(playerName, steamID);
            TimeStamp = timeStamp;
        }

        public bool ParseLine(string logLine, int id)
        {
            if (string.IsNullOrEmpty(logLine))
                return false;

            // Player connects
            if (logLine.Contains("Player") && logLine.Contains("connected") && logLine.Contains(" is ") && !logLine.Contains("disconnected"))
            {
                DateTime? timeStamp = GetDateTimeFromLogLine(logLine);

                if (!timeStamp.HasValue)
                    return false;

                TimeStamp = timeStamp;

                // "moglef" is connected (steamID=76561198067078615)
                List<string> playerStringParts = logLine.Split(' ').ToList();
                int indexFront = playerStringParts.IndexOf("Player");
                int indexAfter = playerStringParts.IndexOf("is");

                StringBuilder strBuilder = new StringBuilder();

                for(int i = 0; i < playerStringParts.Count; i++)
                {
                    // Player name can consist of more than 1 word
                    if (i > indexFront && i < indexAfter)
                        strBuilder.Append(playerStringParts[i]);
                }

                string playerString = strBuilder.ToString().Trim('"');

                // (steamID=76561198067078615)
                Player = new DayZPlayer(playerString, playerStringParts.Last().Remove(0, 9).TrimEnd(')'));
                EventType = LogEvent.PlayerConnected;
                Message = logLine;

                return true;
            }

            // Player got kicked (because of wrong mods etc.. )
            if (logLine.Contains("[StateMachine]: Kick player"))
            {
                DateTime? timeStamp = GetDateTimeFromLogLine(logLine);

                if (!timeStamp.HasValue)
                    return false;

                TimeStamp = timeStamp;

                // 19:22:37 [StateMachine]: Kick player Survivor (dpnid 178538990 uid ) State AuthPlayerLoginState
                List<string> kickStringSplitted = logLine.Split(' ').ToList();

                int indexFront = kickStringSplitted.IndexOf("player");
                int indexAfter = kickStringSplitted.IndexOf(kickStringSplitted.FirstOrDefault(x => x.StartsWith("(")));

                StringBuilder strBuilder = new StringBuilder();

                for (int i = 0; i < kickStringSplitted.Count; i++)
                {
                    // Player name can consist of more than 1 word
                    if (i > indexFront && i < indexAfter)
                        strBuilder.Append(kickStringSplitted[i]);
                }

                Player = new DayZPlayer(strBuilder.ToString().Trim(), string.Empty);
                EventType = LogEvent.PlayerKicked;

                Message = logLine;

                return true;
            }

            // Player gets kicked because of unstable connection
            if (logLine.Contains("unstable connection"))
            {
                DateTime? timeStamp = GetDateTimeFromLogLine(logLine);

                if (!timeStamp.HasValue)
                    return false;

                TimeStamp = timeStamp;

                //  9:03:08 Player moglef (20582534) kicked from server: 10 (Possible speedhack or very unstable connection.)
                List<string> kickStringSplitted = logLine.Split(' ').ToList();

                int indexFront = kickStringSplitted.IndexOf("Player");
                int indexAfter = kickStringSplitted.IndexOf(kickStringSplitted.FirstOrDefault(x => x.StartsWith("(") && x.EndsWith(")")));

                StringBuilder strBuilder = new StringBuilder();

                for (int i = 0; i < kickStringSplitted.Count; i++)
                {
                    // Player name can consist of more than 1 word
                    if (i > indexFront && i < indexAfter)
                        strBuilder.Append(kickStringSplitted[i]);
                }

                Player = new DayZPlayer(strBuilder.ToString().Trim(), string.Empty);
                EventType = LogEvent.PlayerKickedUnstableConnection;
                Message = logLine;

                return true;
            }

            // Player disconnects
            if (logLine.Contains("Player") && logLine.Contains("disconnected") && !logLine.Contains("BattlEye"))
            {
                DateTime? timeStamp = GetDateTimeFromLogLine(logLine);

                if (!timeStamp.HasValue)
                    return false;

                TimeStamp = timeStamp;

                // 15:52:07 Player Chris Toffel disconnected.
                List<string> disconnectStringParts = logLine.Split(' ').ToList();
                int indexFront = disconnectStringParts.IndexOf("Player");
                int indexAfter = disconnectStringParts.IndexOf("disconnected.");

                StringBuilder strBuilder = new StringBuilder();

                for (int i = 0; i < disconnectStringParts.Count; i++)
                {
                    // Player name can consist of more than 1 word
                    if (i > indexFront && i < indexAfter)
                        strBuilder.Append(disconnectStringParts[i]);
                }

                Player = new DayZPlayer(strBuilder.ToString(), string.Empty);
                EventType = LogEvent.PlayerDisconnected;

                Message = logLine;

                return true;
            }

            // Server restart
            if (logLine.Contains("Reading mission ..."))
            {
                DateTime? timeStamp = GetDateTimeFromLogLine(logLine);

                if (!timeStamp.HasValue)
                    return false;

                TimeStamp = timeStamp;
                EventType = LogEvent.ServerRestart;
                Message = logLine;

                return true;
            }

            return false;
        }

        private DateTime? GetDateTimeFromLogLine(string logLine)
        {
            DateTime? dateTime = null;

            if (DateTime.TryParse(logLine.Substring(0, 8), out DateTime logLineDate))
            {
                dateTime = logLineDate;
            }

            return dateTime;
        }

        protected bool Equals(LogLine other)
        {
            return EventType == other.EventType && Player.Equals(other.Player) && Nullable.Equals(TimeStamp, other.TimeStamp);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((LogLine)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)EventType, Player, TimeStamp, Message);
        }

    }
}
