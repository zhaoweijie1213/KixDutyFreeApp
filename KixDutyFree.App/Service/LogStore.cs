using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.App.Service
{
    public class LogStore : ILogEventSink
    {
        public readonly ConcurrentQueue<LogEvent> _logEvents = new();
        private readonly int _maxLogEvents;

        public LogStore()
        {
            _maxLogEvents = 1500;
        }

        public void Emit(LogEvent logEvent)
        {
            _logEvents.Enqueue(logEvent);
            while (_logEvents.Count > _maxLogEvents && _logEvents.TryDequeue(out _)) 
            { 

            }
        }
    }
}
