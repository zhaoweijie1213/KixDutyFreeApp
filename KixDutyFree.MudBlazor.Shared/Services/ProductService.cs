using KixDutyFree.App.Manage;
using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Repository;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class ProductService(ProductInfoRepository productInfoRepository, CacheManage cacheManage) : ITransientDependency
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
            else
            {
                list = [];
            }
            return list;
        }
    }
}
