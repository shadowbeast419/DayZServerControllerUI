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
        private readonly List<string> _logEntries;
        private readonly FileInfo _logFile;
        private int _lastLogLineIndex;

        public LogParser(FileInfo serverLogFile)
        {
            _lastLogLineIndex = 0;
            _logEntries = new();

            if (!serverLogFile.Exists)
                throw new ArgumentException($"Server-Log File does not exist {serverLogFile.FullName}");

            _logFile = serverLogFile;
        }

        /// <summary>
        /// Adds new lines of the server-log file (if any) to the log entries list
        /// </summary>
        /// <returns>Number of new found lines</returns>
        private int UpdateLogEntries()
        {
            using (FileStream fs = new FileStream(_logFile.FullName, FileMode.Open, FileAccess.Read,
                   FileShare.ReadWrite))
            {
                using BufferedStream bs = new BufferedStream(fs);
                using StreamReader fileReader = new StreamReader(bs);

                while (!fileReader.EndOfStream)
                {
                    _logEntries.Add(fileReader.ReadLine() ?? String.Empty);
                }
            }

            return _logEntries.Count;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="logLineCount"></param>
        /// <returns></returns>
        public List<LogLine> ParseNew(int logLineCount = 1000)
        {
            int newLineCount = UpdateLogEntries();

            // Are we already at the end of the file?
            if (_lastLogLineIndex >= _logEntries.Count - 1 || newLineCount == 0)
                return new List<LogLine>();

            //try
            //{
            //    using (Stream s = new FileStream(_logFile.FullName, FileMode.Open, FileAccess.Read,
            //               FileShare.ReadWrite))
            //    {
            //        using (StreamReader fileReader = new StreamReader(s))
            //        {
            //            logFileContent = await fileReader.ReadLinesAsync();
            //        }
            //    }
            //}
            //catch (IOException ex)
            //{
            //    MessageBox.Show(ex.Message);

            //    return logLinesParsed;
            //}

            // Watch out that we are no reading too much
            int desiredLastIndex = _lastLogLineIndex + logLineCount;
            int lastIndexOfFile = _logEntries.Count - 1;
            int calculatedLogLineIndexStop = desiredLastIndex > lastIndexOfFile ? lastIndexOfFile : desiredLastIndex;

            List<string> logLinesBlock = _logEntries.GetRange(_lastLogLineIndex, calculatedLogLineIndexStop);
            int logLineIndexStart = _lastLogLineIndex;
            int lineCount = 0;

            _lastLogLineIndex += logLineCount;

            List<LogLine> logLinesParsed = new List<LogLine>();

            foreach (string logLineStr in logLinesBlock)
            {
                LogLine logLine = new LogLine();

                if (logLine.ParseLine(logLineStr, logLineIndexStart + lineCount++))
                {
                    // No PlayerName in ServerRestart Event
                    if (!logLine.Player.IsValid && logLine.EventType != LogEvent.ServerRestart)
                    {
                        _lastLogLineIndex++;
                        continue;
                    }

                    logLinesParsed.Add(logLine);
                }
            }

            if (logLinesParsed.Count > 0)
            {
                // Index from end expression (^1 -> last index)
                var timeStamp = logLinesParsed[^1].TimeStamp;
                if (timeStamp != null)
                {
                    DateTime logStartDate = timeStamp.Value;

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
                        var dateTime = logLinesParsed[i].TimeStamp;
                        var stamp = logLinesParsed[i + 1].TimeStamp;

                        if (stamp != null && dateTime != null && dateTime.Value.Hour > stamp.Value.Hour)
                        {
                            var time = logLinesParsed[i + 1].TimeStamp;

                            if (time != null)
                            {
                                DateTime newerDateTimeTemp = time.Value;
                                var fromDays = logLinesParsed[i].TimeStamp;

                                if (fromDays != null)
                                {
                                    DateTime currentDateTimeTemp = fromDays.Value;

                                    // If yes, subtract a day compared to the newer timestamp
                                    logLinesParsed[i].TimeStamp = new DateTime(newerDateTimeTemp.Year, newerDateTimeTemp.Month,
                                        newerDateTimeTemp.Day, currentDateTimeTemp.Hour, currentDateTimeTemp.Minute, currentDateTimeTemp.Second) - TimeSpan.FromDays(1);
                                }
                            }

                            continue;
                        }

                        var nextTimeStamp = logLinesParsed[i + 1].TimeStamp;

                        if (nextTimeStamp != null)
                        {
                            DateTime laterDateTime = nextTimeStamp.Value;
                            var currentDateTime = logLinesParsed[i].TimeStamp;
                            if (currentDateTime != null)
                            {
                                DateTime currentDateTimeValue = currentDateTime.Value;

                                // Use the date of the previous log line and the time of the current one
                                logLinesParsed[i].TimeStamp = new DateTime(laterDateTime.Year, laterDateTime.Month,
                                    laterDateTime.Day, currentDateTimeValue.Hour, currentDateTimeValue.Minute, currentDateTimeValue.Second);
                            }
                        }
                    }
                }
            }

            return logLinesParsed;
        }
    }
}
