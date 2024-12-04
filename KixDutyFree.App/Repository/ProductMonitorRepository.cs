using KixDutyFree.App.Models.Entity;
using QYQ.Base.Common.IOCExtensions;

namespace KixDutyFree.App.Repository
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
            return Db.Queryable<ProductMonitorEntity>().OrderByDescending(i => i.UpdateTime).FirstAsync(i => i.Account == account && i.ProductId == productId);
        }

        /// <summary>
        /// 修改为已完成状态
        /// </summary>
        /// <returns></returns>
        public Task<int> UpdateCompletedAsync()
        {
            return Db.Updateable<ProductMonitorEntity>().SetColumns(i => new ProductMonitorEntity()
            {
                Setup = OrderSetup.Completed
            }).Where(i => i.Setup == OrderSetup.OrderPlaced).ExecuteCommandAsync();
        }
    }
}
