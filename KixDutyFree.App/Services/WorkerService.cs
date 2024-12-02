
using KixDutyFree.App.Manage;
using KixDutyFree.App.Models;
using Microsoft.Extensions.Options;

namespace KixDutyFree.App.Services
{
    public class WorkerService(Manager manager) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await manager.InitClientAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await manager.StopAsync();
        }
    }
}
