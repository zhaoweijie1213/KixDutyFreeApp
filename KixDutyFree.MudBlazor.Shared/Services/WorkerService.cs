
using KixDutyFree.App.Manage;
using KixDutyFree.App.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QYQ.Base.Common.Extension;
using System.Diagnostics;

namespace KixDutyFree.App.Services
{
    public class WorkerService(ILogger<WorkerService> logger, Manager manager, IServiceProvider services) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var serverAddresses = services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>().Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            var address = serverAddresses?.Addresses.FirstOrDefault();
            if (address != null)
            {
                // 打开浏览器
                OpenBrowser(address);
            }
            //加载客户端
            await manager.InitClientAsync();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await manager.StopAsync();
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
