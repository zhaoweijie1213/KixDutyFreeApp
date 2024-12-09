using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models.Entity;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class AccountService(CacheManage cacheManage) : ISingletonDependency
    {
        /// <summary>
        /// 获取账号
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountEntity>> GetAccountAsync()
        {
            List<AccountEntity> list = new();
            var data = await cacheManage.GetAccountAsync();
            if (data != null)
            {
                list = data.Select(i => new AccountEntity()
                {
                    Email = i.Email,
                    Password = i.Password,
                    AirlineName = i.AirlineName,
                    FlightNo = i.FlightNo,
                    Date = i.Date,
                    Quantity = i.Quantity
                }).ToList();
            }
          
            return list;
        }
    }
}
