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
    /// 检查登录状态任务
    /// </summary>
    [DisallowConcurrentExecution]
    public class CheckLoginJob(ILogger<CheckLoginJob> logger, AccountClientFactory accountClientFactory) : IJob, ITransientDependency
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var email = context.JobDetail.JobDataMap.Get("email")?.ToString();
                if (!string.IsNullOrEmpty(email))
                {
                    var client = accountClientFactory.GetClient(email);
                    if (client == null) return;
                    bool status = await client.CheckLoginStatusAsync();
                    if (!status)
                    {
                        await client.RelodAsync();
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
