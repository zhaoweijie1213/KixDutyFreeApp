
using AixDutyFreeCrawler.App.Models;
using Microsoft.Extensions.Options;

namespace AixDutyFreeCrawler.App.Services
{
    public class WorkerService(SeleniumService seleniumService, IOptionsMonitor<List<AccountModel>> accounts,IOptionsMonitor<Products> products) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //初始化各个账号的实例
            List<Task> tasks = [];
            foreach (var account in accounts.CurrentValue)
            {
                tasks.Add(seleniumService.CreateInstancesAsync(account));
            }
            await Task.WhenAll(tasks);

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    await Task.Delay(1000);
            //}
            //下单
            await seleniumService.OrderAsync(seleniumService.drivers.First(), products.CurrentValue.Urls.First());
        }
    }
}
