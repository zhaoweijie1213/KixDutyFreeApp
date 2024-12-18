using KixDutyFree.Shared.Manage;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class StartupService(Manager manager, CheckVersionService checkVersionService) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await checkVersionService.CheckForUpdateAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await manager.StopAsync();
        }
    }
}
