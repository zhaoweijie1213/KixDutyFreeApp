using AixDutyFreeCrawler.App.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QYQ.Base.Common.IOCExtensions;
using System.Collections.Concurrent;

namespace AixDutyFreeCrawler.App.Manage
{
    public class Manager(ILogger<Manager> logger, IServiceProvider serviceProvider, IOptionsMonitor<List<AccountModel>> accounts) : ISingletonDependency
    {
        /// <summary>
        /// 
        /// </summary>
        public ConcurrentDictionary<string, AccountClient> Clients { get; set; } = new();

        /// <summary>
        /// 初始化客户端
        /// </summary>
        /// <returns></returns>
        public async Task InitClientAsync() 
        {
            //初始化各个账号的实例
            List<Task> tasks = [];
            foreach (var account in accounts.CurrentValue)
            {
                var accountClient = serviceProvider.GetService<AccountClient>()!;
                //tasks.Add(accountClient.InitAsync(account));
                if (Clients.TryAdd(account.Email, accountClient))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var status = await accountClient.InitAsync(account);
                            if (!status)
                            {
                                //账号登录失败
                            }
                            else
                            {
                                //开始监控商品
                                await accountClient.StartMonitoringAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            // 记录日志或处理异常
                            logger.LogError(ex, "初始化账户 {Email} 时发生错误。", account.Email);
                        }

                    }));
                }
                else
                {
                    // 处理添加失败的情况
                    logger.LogWarning("账户 {Email} 已存在于客户端集合中。", account.Email);
                }
            }
            await Task.WhenAll(tasks);


        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            foreach (var client in Clients.Values)
            {
                await client.QuitAsync();
            }
        }
    }
}
