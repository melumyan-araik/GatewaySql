using System;
using System.Diagnostics;

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
            catch
            {
                // ignored
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
