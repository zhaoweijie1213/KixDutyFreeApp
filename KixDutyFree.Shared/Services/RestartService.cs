using KixDutyFree.Shared.Services.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class RestartService(ILogger<RestartService> logger) : IRestartService
    {
        public event Action? RestartRequested;

        private int _restartCount = 0;

        private const int MaxRestartCount = 3;

        public void RequestRestart()
        {
            if (_restartCount < MaxRestartCount)
            {
                _restartCount++;
                RestartRequested?.Invoke();
            }

            else
            {
                // 达到最大重启次数，记录日志或通知用户
                logger.LogError("应用程序已达到最大重启次数，停止重启。");
            }
        }
    }
}
