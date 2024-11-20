
namespace AixDutyFreeCrawler.App.Services
{
    public class WorkerService(SeleniumService seleniumService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await seleniumService.TaskStartAsync();
        }
    }
}
