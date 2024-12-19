using KixDutyFree.App.Manage;
using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Response;
using KixDutyFree.App.Quartz;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models;
using KixDutyFree.Shared.Models.Entity;
using KixDutyFree.Shared.Quartz.Jobs;
using KixDutyFree.Shared.Repository;
using log4net.Core;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using OpenQA.Selenium.BiDi.Modules.Log;
using Quartz;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class AccountService(ILogger<AccountService> logger, CacheManage cacheManage, AccountRepository accountRepository, IServiceProvider serviceProvider, AccountClientFactory accountClientFactory
        , ISchedulerFactory schedulerFactory) : ISingletonDependency
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
            await StartLoginCheckAsync(account.Email);
        }


        /// <summary>
        /// 检查登录状态
        /// </summary>
        /// <returns></returns>
        public async Task StartLoginCheckAsync(string email)
        {

            var scheduler = await schedulerFactory.GetScheduler();
            var job = JobBuilder.Create<CheckLoginJob>()
                .UsingJobData("email", email)
                .WithIdentity($"login_check_job_{email}", "login_check")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"login_check_trigger_{email}", "login_check")
                .StartAt(DateTime.Now.AddMinutes(5))
                .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(5)
                .RepeatForever())
                .Build();
            await scheduler.ScheduleJob(job, trigger);
            logger.LogInformation("StartMonitorAsync.添加{email}登录检测任务", email);


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
                //修改客户端账号信息
                if(accountClientFactory.Clients.TryGetValue(entity.Email, out AccountClient? accountClient))
                {
                    accountClient.Account = account;
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
            //取消任务
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.UnscheduleJob(new TriggerKey($"login_check_trigger_{email}", "login_check"));
            //移除客户端
            if (accountClientFactory.Clients.TryGetValue(email, out AccountClient? accountClient))
            {
                if (accountClient != null)
                {
                    await accountClient.QuitAsync();
                    accountClientFactory.Clients.TryRemove(email, out _);
                }
            }
            var account = _accounts.FirstOrDefault(i => i.Email == email);
            if (account != null)
            {
                _accounts.Remove(account);
                NotifyStateChanged();
            }

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
