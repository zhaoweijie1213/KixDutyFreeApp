using Quartz;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Quartz.Jobs
{
    /// <summary>
    /// 商品监控任务
    /// </summary>
    [DisallowConcurrentExecution]
    public class MonitorProducts : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
