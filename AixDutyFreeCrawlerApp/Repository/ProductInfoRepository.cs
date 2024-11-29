using AixDutyFreeCrawler.App.Models.Entity;
using QYQ.Base.Common.IOCExtensions;

namespace AixDutyFreeCrawler.App.Repository
{
    public class ProductInfoRepository(Logger<ProductInfoRepository> logger, IConfiguration configuration)
        : BaseRepository<ProductInfoEntity>(logger, configuration.GetConnectionString("DefaultConnection")!), ITransientDependency
    {
        /// <summary>
        /// 查询商品
        /// </summary>
        /// <param name="productAddress"></param>
        /// <returns></returns>
        public Task<ProductInfoEntity> FindByAddressAsync(string productAddress)
        {
            return Db.Queryable<ProductInfoEntity>().FirstAsync(i => i.Address == productAddress);
        }

        /// <summary>
        /// 查询商品
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<ProductInfoEntity> FindAsync(string id)
        {
            return Db.Queryable<ProductInfoEntity>().FirstAsync(i => i.Id == id);
        }
    }
}
