using KixDutyFree.App.Models;
using KixDutyFree.App.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QYQ.Base.Common.IOCExtensions;
using System.Collections.Concurrent;

namespace KixDutyFree.App.Manage
{
    public class Manager(ILogger<Manager> logger, IServiceProvider serviceProvider, CacheManage cacheManage) : ISingletonDependency
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
            var accounts = await cacheManage.GetAccountAsync();
            if (accounts == null) return;
            //初始化各个账号的实例
            List<Task> tasks = [];
            foreach (var account in accounts)
            {
                var accountClient = serviceProvider.GetService<AccountClient>()!;
                //tasks.Add(accountClient.InitAsync(account));
                if (Clients.TryAdd(account.Email, accountClient))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await accountClient.InitAsync(account);
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
