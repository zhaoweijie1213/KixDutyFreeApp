using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models.Input;
using KixDutyFree.Shared.Quartz.Jobs;
using KixDutyFree.Shared.Services;
using log4net.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using QYQ.Base.Common.IOCExtensions;
using static Quartz.Logging.OperationName;

namespace KixDutyFree.App.Quartz
{
    public class QuartzManagement(ILogger<QuartzManagement> logger, ISchedulerFactory schedulerFactory, AccountClientFactory accountClientFactory
        , ProductInfoRepository productInfoRepository, ProductService productService) : ISingletonDependency
    {
        /// <summary>
        /// 开始监控
        /// </summary>
        /// <returns></returns>
        public async Task StartMonitorAsync()
        {

            var products = await productInfoRepository.QueryAsync();

            if (products != null && products.Count > 0)
            {
                foreach (var product in products)
                {
                    await productService.StartMonitorAsync(new AddProductInput()
                    {
                        Address = product.Address,
                        Quantity = product.Quantity
                    });
                }
            }
        }

        /// <summary>
        /// 检查登录状态
        /// </summary>
        /// <returns></returns>
        public async Task StartLoginCheckAsync()
        {
            foreach (var client in accountClientFactory.Clients)
            {
                if (client.Value != null)
                {
                    var scheduler = await schedulerFactory.GetScheduler();
                    var job = JobBuilder.Create<CheckLoginJob>()
                        .UsingJobData("email", client.Key)
                        .WithIdentity($"login_check_job_{client.Key}", "login_check")
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity($"login_check_trigger_{client.Key}", "login_check")
                        .StartNow()
                        .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(10)
                        .RepeatForever())
                        .Build();
                    await scheduler.ScheduleJob(job, trigger);
                    logger.LogInformation("StartMonitorAsync.添加{email}登录检测任务", client.Key);
                }
            }
        }

        /// <summary>
        /// 检查错误次数
        /// </summary>
        /// <returns></returns>
        public async Task StartErrorCheckAsync()
        {

            var scheduler = await schedulerFactory.GetScheduler();
            var job = JobBuilder.Create<CheckErrorJob>()
                .WithIdentity($"error_check_job", "error_check")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"erroe_check_trigger", "error_check")
                .StartNow()
                .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(5)
                .RepeatForever())
                .Build();
            await scheduler.ScheduleJob(job, trigger);
            logger.LogInformation("StartMonitorAsync.添加错误检测任务");


        }

    }
}
