﻿using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Services;
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
    public class MonitorProducts(ILogger<MonitorProducts> logger, AccountClientFactory accountClientFactory, ProductService productService, ProductInfoRepository productInfoRepository) : IJob, ITransientDependency
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

                productService.UpdateMonitorStatus(id, true);

                var defaultClient = await accountClientFactory.GetDefaultClientAsync(id);
                //var product = await cacheManage.GetProductInfoAsync(id);
                var product = await productInfoRepository.FindAsync(id);
                if (product == null) return;
                bool isAvailable=  await defaultClient.CheckProductAvailabilityAsync(product, context.CancellationToken);
                if (isAvailable)
                {
                    List<Task> tasks = [];
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
