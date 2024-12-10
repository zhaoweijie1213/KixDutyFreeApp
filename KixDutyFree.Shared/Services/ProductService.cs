using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Quartz;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models.Input;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class ProductService(ProductInfoRepository productInfoRepository, CacheManage cacheManage, QuartzManagement quartzManagement) : ITransientDependency
    {

        /// <summary>
        /// 获取商品
        /// </summary>
        /// <returns></returns>
        public async Task<List<ProductInfoEntity>> GetProductsAsync()
        {
            var products = await cacheManage.GetProductsAsync();
            var list = await productInfoRepository.QueryAsync();
            if (products != null)
            {
                list = list.Where(i => products.Any(x => x.Address == i.Address)).ToList();
            }
            return list;
        }

        /// <summary>
        /// 新增商品任务
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task AddProduct(AddProductInput input)
        {
            //开始商品监控任务
            await quartzManagement.StartMonitorAsync(input);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DelAsync(string id)
        {
            bool status = false;
           var count = await productInfoRepository.DeleteAsync(id);
            if (count > 0)
            {
                //取消商品监控任务
                await quartzManagement.CancelMonitorAsync(id);
            }
            status = count > 0;
            return status;
        }
    }
}
