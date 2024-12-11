using QYQ.Base.Common.IOCExtensions;
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

        // 事件，当新的 LogEvent 被添加时触发
        public event Action? OnLogAdded;

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

            // 触发事件通知
            OnLogAdded?.Invoke();
        }

        // 获取当前的日志事件列表
        public IEnumerable<LogEvent> GetLogEvents()
        {
            return _logEvents.ToArray();
        }
    }
}
