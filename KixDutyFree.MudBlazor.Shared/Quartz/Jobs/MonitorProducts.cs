using KixDutyFree.App.Manage;
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
    /// 商品监控任务
    /// </summary>
    [DisallowConcurrentExecution]
    public class MonitorProducts(ILogger<MonitorProducts> logger, AccountClientFactory accountClientFactory, CacheManage cacheManage) : IJob, ITransientDependency
    {
        public async Task Execute(IJobExecutionContext context)
        {
            string guid = Guid.NewGuid().ToString();
            logger.LogInformation("Execute.标识符:{Guid}", guid);
            try
            {
                var id = context.JobDetail.JobDataMap.Get("id")?.ToString();
                if (id == null)
                {
                    return;
                }
                var defaultClient = await accountClientFactory.GetDefaultClientAsync();
                var product = await cacheManage.GetProductInfoAsync(id);
                if (product == null) return;
                bool isAvailable=  await defaultClient.CheckProductAvailabilityAsync(product, context.CancellationToken);
                if (isAvailable)
                {
                    List<Task> tasks = new List<Task>();
                    //下单
                    foreach (var client in accountClientFactory.Clients) 
                    {
                        tasks.Add(client.Value.FullCheckProductAvailabilityAsync(product, context.CancellationToken));
                    }
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception e)
            {
                logger.BaseErrorLog("Execute", e);
            }

        }
    }
}
