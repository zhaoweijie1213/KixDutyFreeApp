using KixDutyFree.App.Quartz;
using KixDutyFree.Shared.Manage;
using log4net.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.Extension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class StartupService(ILogger<StartupService> logger, Manager manager, QuartzManagement quartzManagement) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await manager.InitDataAsync();
            //商品监控
            await quartzManagement.StartMonitorAsync();
            //错误检查
            await quartzManagement.StartErrorCheckAsync();
            //加载客户端
            await manager.InitClientAsync();

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await manager.StopAsync();
        }

        public void OpenBrowser(string url)
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

    public class CheckVersionStartupService(CheckVersionService checkVersionService) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(3000, cancellationToken);
            await checkVersionService.CheckForUpdateAsync();

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
