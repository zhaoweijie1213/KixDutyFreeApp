using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Excel;
using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;
using Microsoft.Extensions.Caching.Memory;
using QYQ.Base.Common.IOCExtensions;

namespace KixDutyFree.App.Manage
{
    public class CacheManage(ILogger<CacheManage> logger, IMemoryCache memoryCache) : ITransientDependency
    {

        /// <summary>
        /// 获取账号信息
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountModel>?> GetAccountAsync() 
        {
            string key = "AccountInfo";
            if (!memoryCache.TryGetValue(key, out List<AccountModel>? account))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Account.xlsx");
                var importer = new ExcelImporter();
                var result = await importer.Import<AccountModel>(path, null);
                if(result.Data.Count > 0)
                {
                    account = result.Data.ToList();
                    memoryCache.Set(key, account, TimeSpan.FromMinutes(1));
                }
            }
            return account;
        }
    }
}
