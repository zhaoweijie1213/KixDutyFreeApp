using AixDutyFreeCrawler.App.Models.Entity;
using QYQ.Base.Common.IOCExtensions;

namespace AixDutyFreeCrawler.App.Repository
{
    public class ProductMonitorRepository(ILogger<ProductMonitorRepository> logger, IConfiguration configuration)
        : BaseRepository<ProductMonitorEntity>(logger, configuration.GetConnectionString("DefaultConnection")!), ITransientDependency
    {
        /// <summary>
        /// 查询商品监控信息
        /// </summary>
        /// <param name="account"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public Task<ProductMonitorEntity> QueryAsync(string account, string productId)
        {
            return Db.Queryable<ProductMonitorEntity>().FirstAsync(i => i.Account == account && i.ProductId == productId);
        }
    }
}
