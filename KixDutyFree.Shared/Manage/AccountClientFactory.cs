using KixDutyFree.Shared.Manage.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Manage
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="serviceProvider"></param>
    public class AccountClientFactory(ILogger<AccountClientFactory> logger, IServiceProvider serviceProvider) : ISingletonDependency
    {
        /// <summary>
        /// 客户端
        /// </summary>
        public ConcurrentDictionary<string, AccountClient> Clients { get; set; } = new();

        /// <summary>
        /// 默认客户端
        /// </summary>
        public AccountClient? DefaultClient { get; set; }

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// 获取客户端
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public  AccountClient? GetClient(string account)
        {
            Clients.TryGetValue(account, out AccountClient? client);
            return client;
        }

        /// <summary>
        /// 获取默认客户端
        /// </summary>
        /// <returns></returns>
        public async Task<AccountClient> GetDefaultClientAsync()
        {
            logger.LogInformation("GetDefaultClientAsync.获取默认客户端");
            if (DefaultClient == null)
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (DefaultClient == null)
                    {
                        var accountClient = serviceProvider.GetService<AccountClient>()!;
                        await accountClient.InitAsync(null); // 确保 InitAsync 能处理 null 参数
                        DefaultClient = accountClient;
                        logger.LogInformation("GetDefaultClientAsync.默认客户端初始化完成");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GetDefaultClientAsync.初始化默认客户端时出错");
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return DefaultClient;
        }
    }
}
