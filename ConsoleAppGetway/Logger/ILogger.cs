using System.Diagnostics;

namespace ConsoleAppGateway.Logger
{
    internal interface ILogger
    {
        void AddLog(string mes, EventLogEntryType type);
        void Clear();
    }
}
