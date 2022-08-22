using System;
using System.Diagnostics;
using System.IO;

namespace ConsoleAppGateway.Logger
{
    internal class LoggerEvent : ILogger
    {
        private readonly string nameLog = "";

        public LoggerEvent(string nameLog)
        {
            this.nameLog = nameLog;
        }
        public void AddLog(string mes, EventLogEntryType type)
        {
            try
            {
                var el = GetLog();
                el.WriteEntry(mes, type);
            }
            catch (Exception ex)
            {
                var dirLog = Path.Combine(Directory.GetCurrentDirectory(), "Log");
                if (!Directory.Exists(dirLog))
                    Directory.CreateDirectory(dirLog);

                File.WriteAllText(Path.Combine(dirLog, $"{nameLog}-{DateTime.Now.ToString("ddMMyyyyHHmmss")}-log.txt"), $"{Enum.GetName(typeof(EventLogEntryType), type)}:: {mes}\n{ex.Message}\n=======================");
            }
        }

        private EventLog GetLog()
        {
            if (!EventLog.SourceExists(nameLog))
            {
                EventLog.CreateEventSource(nameLog, nameLog);
            }
            return new EventLog { Source = nameLog };
        }

        public void Clear()
        {
            var EventLog = GetLog();
            EventLog.Clear();
        }

    }
}
