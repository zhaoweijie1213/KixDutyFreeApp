using KixDutyFree.Shared.Manage.Client;
using KixDutyFree.Shared.Manage.Client.Interface;
using KixDutyFree.Shared.Models.Enum;
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
        public ConcurrentDictionary<string, IAccountClient> Clients { get; set; } = new();

        /// <summary>
        /// 商品监控客户端
        /// </summary>
        public ConcurrentDictionary<string, IAccountClient> ProductClients { get; set; } = new();

        ///// <summary>
        ///// 默认客户端
        ///// </summary>
        //public IAccountClient? DefaultClient { get; set; }

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// 获取客户端
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public IAccountClient? GetClient(string account)
        {
            Clients.TryGetValue(account, out IAccountClient? client);
            return client;
        }

        /// <summary>
        /// 获取默认客户端
        /// </summary>
        /// <returns></returns>
        public async Task<IAccountClient> GetDefaultClientAsync(string productId, ClientType clientType = ClientType.Http)
        {
            logger.LogInformation("GetDefaultClientAsync.获取默认客户端 {productId}", productId);

            if (!ProductClients.TryGetValue(productId, out IAccountClient? client))
            {
                await _semaphore.WaitAsync();

                try
                {
                    //IAccountClient accountClient;
                    switch (clientType)
                    {
                        case ClientType.Selenium:
                            client = serviceProvider.GetRequiredService<AccountClient>();
                            break;

                        default:
                            client = serviceProvider.GetRequiredService<HttpAccountClient>();
                            break;
                    }

                    await client.InitAsync(null); // 确保 InitAsync 能处理 null 参数
                                                  //DefaultClient = accountClient;
                    logger.LogInformation("GetDefaultClientAsync.默认客户端{productId}初始化完成", productId);

                    ProductClients.TryAdd(productId, client);
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

            return client;

            //if (DefaultClient == null)
            //{
            //    await _semaphore.WaitAsync();
            //    try
            //    {
            //        if (DefaultClient == null)
            //        {
                       
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.LogError(ex, "GetDefaultClientAsync.初始化默认客户端时出错");
            //        throw;
            //    }
            //    finally
            //    {
            //        _semaphore.Release();
            //    }
            //}
            //return DefaultClient;
        }
    }
}
