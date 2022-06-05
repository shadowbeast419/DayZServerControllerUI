using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DayZServerControllerUI.LogParser
{
    internal class LogParser
    {
        private int _lastLogLineIndex;

        public LogParser()
        {
            _lastLogLineIndex = 0;
        }

        public async Task<List<LogLine>> GetNewLogLines()
        {
            string logFileContent;
            List<LogLine> logLinesParsed = new List<LogLine>();

            try
            {
                using (Stream s = new FileStream(LogParserSettings.Default.ServerLogDir, FileMode.Open, FileAccess.Read,
                           FileShare.ReadWrite))
                {
                    using (StreamReader fileReader = new StreamReader(s))
                    {
                        logFileContent = await fileReader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                LogParserSettings.Default.ServerLogDir = string.Empty;
                LogParserSettings.Default.Save();

                return logLinesParsed;
            }

            string[] logLines = logFileContent.Split(new[] { '\r', '\n' });

            while (_lastLogLineIndex < logLines.Length)
            {
                LogLine lineParsed = new LogLine();

                if (lineParsed.ParseLine(logLines[_lastLogLineIndex], _lastLogLineIndex))
                {
                    // No PlayerName in ServerRestart Event
                    if (!lineParsed.Player.IsValid && lineParsed.EventType != LogEvent.ServerRestart)
                    {
                        _lastLogLineIndex++;
                        continue;
                    }

                    logLinesParsed.Add(lineParsed);
                }

                _lastLogLineIndex++;
            }

            if (logLinesParsed.Count > 0)
            {
                DateTime logStartDate = logLinesParsed[logLinesParsed.Count - 1].TimeStamp.Value;

                // Use the current date and the time from the logfile as start
                DateTime calcStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                    logStartDate.Hour, logStartDate.Minute, logStartDate.Second);

                // Set date times from the beginning
                for (int i = logLinesParsed.Count - 1; i >= 0; i--)
                {
                    if (i == logLinesParsed.Count - 1)
                    {
                        // Last added logLines are the latest, subtract the time difference
                        logLinesParsed[i].TimeStamp = calcStartDate;

                        continue;
                    }

                    // Check if a new day has started (older entry has an higher hour value than the newer one)
                    if (logLinesParsed[i].TimeStamp.Value.Hour > logLinesParsed[i + 1].TimeStamp.Value.Hour)
                    {
                        DateTime newerDateTimeTemp = logLinesParsed[i + 1].TimeStamp.Value;
                        DateTime currentDateTimeTemp = logLinesParsed[i].TimeStamp.Value;

                        // If yes, subtract a day compared to the newer timestamp
                        logLinesParsed[i].TimeStamp = new DateTime(newerDateTimeTemp.Year, newerDateTimeTemp.Month,
                            newerDateTimeTemp.Day, currentDateTimeTemp.Hour, currentDateTimeTemp.Minute, currentDateTimeTemp.Second) - TimeSpan.FromDays(1);

                        continue;
                    }

                    DateTime newerDateTime = logLinesParsed[i + 1].TimeStamp.Value;
                    DateTime currentDateTime = logLinesParsed[i].TimeStamp.Value;

                    // Use the date of the previous log line and the time of the current one
                    logLinesParsed[i].TimeStamp = new DateTime(newerDateTime.Year, newerDateTime.Month,
                        newerDateTime.Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);
                }
            }

            return logLinesParsed;
        }
    }
}
