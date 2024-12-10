using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Models.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Repository
{
    public class AccountRepository(ILogger<AccountRepository> logger, IConfiguration configuration)
        : BaseRepository<AccountEntity>(logger, configuration.GetConnectionString("DefaultConnection")!), ITransientDependency
    {
        /// <summary>
        /// 查询账号
        /// </summary>
        /// <returns></returns>
        public Task<List<AccountEntity>> QueryAsync()
        {
            return Db.Queryable<AccountEntity>().ToListAsync();
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public Task<int> InsertAsync(List<AccountEntity> list)
        {
            return Db.Insertable(list).ExecuteCommandAsync();
        }
    }
}
