using KixDutyFree.App.Models.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;

namespace KixDutyFree.App.Repository
{
    public class ProductInfoRepository(ILogger<ProductInfoRepository> logger, IConfiguration configuration)
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

        /// <summary>
        /// 查询商品
        /// </summary>
        /// <returns></returns>
        public Task<List<ProductInfoEntity>> QueryAsync()
        {
            return Db.Queryable<ProductInfoEntity>().OrderByDescending(i => i.UpdateTime).ToListAsync();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<int> DeleteAsync(string id)
        {
            return Db.Deleteable<ProductInfoEntity>().Where(i=>i.Id == id).ExecuteCommandAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public Task<int> UpdateQuantityAsync(string id,int quantity)
        {
            return Db.Updateable<ProductInfoEntity>().SetColumns(i => new ProductInfoEntity()
            {
                Quantity = quantity
            }).Where(i => i.Id == id).ExecuteCommandAsync();
        }
    }
}
