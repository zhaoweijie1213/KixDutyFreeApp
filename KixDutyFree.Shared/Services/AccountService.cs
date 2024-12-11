using KixDutyFree.App.Manage;
using KixDutyFree.App.Models;
using KixDutyFree.App.Quartz;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models;
using KixDutyFree.Shared.Models.Entity;
using KixDutyFree.Shared.Repository;
using log4net.Core;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.BiDi.Modules.Log;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class AccountService(ILogger<AccountService> logger, CacheManage cacheManage, AccountRepository accountRepository, IServiceProvider serviceProvider, AccountClientFactory accountClientFactory) : ISingletonDependency
    {
        private List<AccountInfo> _accounts = [];

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        // 事件，用于通知订阅者库存变化
        public event Action? OnChange;

        // 获取当前库存
        public List<AccountInfo> Accounts
        {
            get
            {
                // 返回库存的副本，避免外部修改
                return new List<AccountInfo>(_accounts);
            }
        }

        // 通知订阅者状态变化
        private void NotifyStateChanged() => OnChange?.Invoke();

        /// <summary>
        /// 同步账号
        /// </summary>
        /// <returns></returns>
        public async Task SyncAccountAsync()
        {
            var data = await cacheManage.GetExcelAccountAsync();
            var accounts = await accountRepository.QueryAsync();
            if (accounts.Count <= 0)
            {
                if (data != null)
                {
                    accounts = data.Select(i => new AccountEntity()
                    {
                        Email = i.Email,
                        Password = i.Password,
                        AirlineName = i.AirlineName,
                        FlightNo = i.FlightNo,
                        Date = i.Date,
                        Quantity = i.Quantity
                    }).ToList();
                    await accountRepository.InsertAsync(accounts);
                }
            }
        }


        /// <summary>
        /// 获取账号
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountInfo>> GetAccountInfoAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                List<AccountEntity> list = await accountRepository.QueryAsync();
                foreach (var item in list)
                {
                    var data = _accounts.FirstOrDefault(i => i.Email == item.Email);
                    if (data == null)
                    {
                        _accounts.Add(new AccountInfo()
                        {
                            Email = item.Email,
                            Password=item.Password,
                            AirlineName = item.AirlineName,
                            FlightNo = item.FlightNo,
                            Date = item.Date,
                            Quantity = item.Quantity
                        });
                        NotifyStateChanged();
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        
            return Accounts;
        }

        /// <summary>
        /// 新增账号
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task AddAccountAsync(AccountEntity entity)
        {
            entity = await accountRepository.InsertAsync(entity);
       
            var account = _accounts.FirstOrDefault(i => i.Email == entity.Email);
            if (account == null)
            {
                account = entity.Adapt<AccountInfo>();
                _accounts.Add(account);
                NotifyStateChanged();
            }
            //初始化账号client
            await CreateClientAsync(account);
        }

        /// <summary>
        /// 修改账号
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task UpdateAccountAsync(AccountEntity entity)
        {
           var status = await accountRepository.UpdateAsync(entity);
            if (status)
            {
                var account = _accounts.FirstOrDefault(i => i.Email == entity.Email);
                if (account == null)
                {
                    account = entity.Adapt<AccountInfo>();
                    _accounts.Add(account);
                }
                else
                {
                    account.Password = entity.Password;
                    account.AirlineName = entity.AirlineName;
                    account.FlightNo = entity.FlightNo;
                    account.Date = entity.Date;
                    account.Quantity = entity.Quantity;
                }
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 删除账号
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task DelAccountAsync(string email)
        {
            await accountRepository.DelAsync(email);
        }

        /// <summary>
        /// 创建客户端
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateClientAsync(AccountInfo account)
        {
            bool status = false;
            var accountClient = serviceProvider.GetService<AccountClient>()!;
            //tasks.Add(accountClient.InitAsync(account));
            if (accountClientFactory.Clients.TryAdd(account.Email, accountClient))
            {
                status = await accountClient.InitAsync(account);
            }
            else
            {
                // 处理添加失败的情况
                logger.LogWarning("账户 {Email} 已存在于客户端集合中。", account.Email);
            }
            return status;
        }

        /// <summary>
        /// 修改客户端登录状态
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task UpdateLoginStatusAsync(string email,bool isLogin)
        {
            var account = _accounts.FirstOrDefault(i => i.Email == email);
            if (account != null)
            {
                account.IsLogin = isLogin;
                NotifyStateChanged();
            }

            return Task.CompletedTask;
        }
    }

 
}
