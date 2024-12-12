using KixDutyFree.App.Manage;
using KixDutyFree.App.Models;
using KixDutyFree.App.Quartz;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QYQ.Base.Common.IOCExtensions;
using System.Collections.Concurrent;

namespace KixDutyFree.Shared.Manage
{
    public class Manager(ILogger<Manager> logger, ProductMonitorRepository productMonitorRepository, IConfiguration configuration
        , AccountClientFactory accountClientFactory, QuartzManagement quartzManagement, AccountService accountService,ProductService productService) : ISingletonDependency
    {
        /// <summary>
        /// 加载数据
        /// </summary>
        /// <returns></returns>
        public async Task InitDataAsync()
        {
            //同步商品数据
            await productService.SyncProductAsync();
            //同步账号数据
            await accountService.SyncAccountAsync();
        }

        /// <summary>
        /// 初始化客户端
        /// </summary>
        /// <returns></returns>
        public async Task InitClientAsync()
        {
            if (configuration.GetSection("ReloadOnRestart").Get<bool>())
            {
                //将订单标记为已完成
                await productMonitorRepository.UpdateCompletedAsync();
            }
            //账号
            var accounts = await accountService.GetAccountInfoAsync();
            if (accounts == null) return;
            //初始化各个账号的实例
            List<Task> tasks = [];
            foreach (var account in accounts)
            {
                tasks.Add(accountService.CreateClientAsync(account));
            }
            await Task.WhenAll(tasks);
            //检查登录状态任务
            await quartzManagement.StartLoginCheckAsync();
        }


        /// <summary>
        /// 停止
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            foreach (var client in accountClientFactory.Clients.Values)
            {
                await client.QuitAsync();
            }
            if (accountClientFactory.DefaultClient != null)
            {
                await accountClientFactory.DefaultClient.QuitAsync();
            }
        }
    }
}
