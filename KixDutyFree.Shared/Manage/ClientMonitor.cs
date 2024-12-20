
using KixDutyFree.Shared.Services;
using KixDutyFree.Shared.Services.Interface;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Manage
{
    /// <summary>
    /// 客户端活动监控
    /// </summary>
    public class ClientMonitor(ILogger<ClientMonitor> logger, IRestartService restartService) : ISingletonDependency
    {
        /// <summary>
        /// 登录错误次数
        /// </summary>
        private int LoginError { get; set; }

        private readonly object _lock = new();

        /// <summary>
        /// 
        /// </summary>
        private bool IsRestarting {  get; set; } = false;

        /// <summary>
        /// 初始化错误
        /// </summary>
        private int InitError { get; set; }

        /// <summary>
        /// 新增登录错误
        /// </summary>
        public void AddLoginError()
        {
            LoginError++;
            logger.LogWarning("AddLoginError.登录错误次数：{LoginError}", LoginError);
            if (LoginError >= 1)
            {
                lock (_lock)
                {
                    if (!IsRestarting)
                    {
                        IsRestarting = true;
                        logger.LogError("AddLoginError.登录错误次数达到{LoginError}次，准备重启应用程序。", LoginError);
                        restartService.RequestRestart();
                    }
                 
                }
          
            }
        }

        /// <summary>
        /// 新增初始化错误
        /// </summary>
        public void AddError()
        {
            InitError++;
            logger.LogWarning("AddError.初始化错误次数：{InitError}", InitError);

            if (InitError >= 3)
            {
                lock (_lock)
                {
                    if (!IsRestarting)
                    {
                        logger.LogError("AddError.初始化错误次数达到{InitError}次，准备重启应用程序。", InitError);
                        restartService.RequestRestart();
                    }
                }
            }
        }
    }
}
