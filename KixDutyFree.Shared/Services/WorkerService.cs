﻿using KixDutyFree.App.Models;
using KixDutyFree.App.Quartz;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QYQ.Base.Common.Extension;
using System.Diagnostics;

namespace KixDutyFree.Shared.Services
{
    public class WorkerService(ILogger<WorkerService> logger, IServiceProvider services) : BackgroundService
    {

        //public override async Task StartAsync(CancellationToken cancellationToken)
        //{
        //    await base.StartAsync(cancellationToken);
        //}

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await manager.InitDataAsync();
            ////商品监控
            //await quartzManagement.StartMonitorAsync();
            ////错误检查
            //await quartzManagement.StartErrorCheckAsync();
            var serverAddresses = services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>().Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            var address = "http://localhost:5128";
            if (address != null)
            {
                // 打开浏览器
                OpenBrowser(address);
            }
            ////加载客户端
            //await manager.InitClientAsync();
            return Task.CompletedTask;
        }

        private void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                logger.BaseErrorLog($"OpenBrowser", ex);
            }
        }
    }
}
