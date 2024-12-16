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
    public class AppConfigRepository(ILogger<AppConfigRepository> logger, IConfiguration configuration)
        : BaseRepository<AppConfigEntity>(logger, configuration.GetConnectionString("DefaultConnection")!), ITransientDependency
    {
        /// <summary>
        /// 获取
        /// </summary>
        /// <returns></returns>
        public Task<AppConfigEntity> FindAsync()
        {
            return Db.Queryable<AppConfigEntity>().FirstAsync();
        }

        /// <summary>
        /// 修改重新下单配置
        /// </summary>
        /// <param name="reOrderOnRestart"></param>
        /// <returns></returns>
        public Task<int> UpdateReOrderAsync(bool reOrderOnRestart)
        {
            return Db.Updateable<AppConfigEntity>().SetColumns(i => new AppConfigEntity()
            {
                ReOrderOnRestart = reOrderOnRestart
            }).Where(i => i.Id != 0).ExecuteCommandAsync();
        }
    }
}
