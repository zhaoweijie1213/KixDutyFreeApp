using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Manage;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    /// <summary>
    /// 商品监控
    /// </summary>
    public class ProductMonitorService(ProductMonitorRepository productMonitorRepository, CacheManage cacheManage) : ITransientDependency
    {
        /// <summary>
        /// 新增监控信息
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<ProductMonitorEntity> InsertProductMonitorAsync(ProductMonitorEntity entity)
        {
            entity = await productMonitorRepository.InsertAsync(entity);
            //更新缓存
            cacheManage.SetProductMonitorAsync(entity);
            return entity;
        }

        /// <summary>
        /// 修改监控信息
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task UpdateProductMonitorAsync(ProductMonitorEntity entity)
        {
            bool status = await productMonitorRepository.UpdateAsync(entity);
            if (status)
            {
                cacheManage.SetProductMonitorAsync(entity);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task UpdateCancelAsync(string email, string productId)
        {
            int command = await productMonitorRepository.UpdateCancelAsync(email, productId);
            if (command > 0) 
            {

            }
        }
    }
}
