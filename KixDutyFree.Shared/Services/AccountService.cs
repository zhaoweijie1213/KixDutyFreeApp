using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models.Entity;
using KixDutyFree.Shared.Repository;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class AccountService(CacheManage cacheManage, AccountRepository accountRepository) : ISingletonDependency
    {
        /// <summary>
        /// 同步账号
        /// </summary>
        /// <returns></returns>
        public async Task SyncAccountAsync()
        {
            var data = await cacheManage.GetAccountAsync();
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
        public async Task<List<AccountEntity>> GetAccountAsync()
        {
            List<AccountEntity> list = await accountRepository.QueryAsync();
            return list;
        }
    }
}
