using KixDutyFree.Shared.Manage;
using Microsoft.Extensions.Logging;
using Quartz;
using QYQ.Base.Common.Extension;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Quartz.Jobs
{
    /// <summary>
    /// 检测错误任务
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="accountClientFactory"></param>
    [DisallowConcurrentExecution]
    public class CheckErrorJob(ILogger<CheckErrorJob> logger, AccountClientFactory accountClientFactory) : IJob, ITransientDependency
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                foreach (var client in accountClientFactory.Clients)
                {
                    if (client.Value.ErrorCount >= 5)
                    {
                        await client.Value.RelodAsync();
                    }
                }
            }
            catch (Exception e)
            {
                logger.BaseErrorLog("Execute", e);
            }
        }
    }
}
