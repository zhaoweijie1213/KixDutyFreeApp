using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Models.Excel;
using KixDutyFree.App.Repository;
using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;

namespace KixDutyFree.Shared.Manage
{
    public class CacheManage(ILogger<CacheManage> logger, IMemoryCache memoryCache, ProductInfoRepository productInfoRepository, ProductMonitorRepository productMonitorRepository) : ITransientDependency
    {
        /// <summary>
        /// 获取表格账号信息
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountInfo>?> GetExcelAccountAsync()
        {
            string key = "AccountInfo";
            if (!memoryCache.TryGetValue(key, out List<AccountInfo>? account))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "账号.xlsx");
                var importer = new ExcelImporter();
                var result = await importer.Import<AccountInfo>(path, null);
                if (result.Data.Count > 0)
                {
                    account = result.Data.ToList();
                    memoryCache.Set(key, account);
                }
            }
            return account;
        }

        /// <summary>
        /// 获取商品信息
        /// </summary>
        /// <returns></returns>
        public async Task<List<ProductModel>?> GetExcelProductsAsync()
        {
            string key = "ProductsInfo";
            if (!memoryCache.TryGetValue(key, out List<ProductModel>? products))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "商品.xlsx");
                var importer = new ExcelImporter();
                var result = await importer.Import<ProductModel>(path, null);
                if (result.Data.Count > 0)
                {
                    products = result.Data.ToList();
                    memoryCache.Set(key, products);
                }
            }
            return products;
        }

        /// <summary>
        /// 获取商品信息
        /// </summary>
        /// <returns></returns>
        public async Task<ProductInfoEntity?> GetProductInfoAsync(string id)
        {
            string key = CustomCacheKeys.ProductInfo(id);
            if (!memoryCache.TryGetValue(key, out ProductInfoEntity? productInfo))
            {
                productInfo = await productInfoRepository.FindAsync(id);
                if (productInfo != null)
                {
                    memoryCache.Set(key, productInfo, TimeSpan.FromMinutes(5));
                }
            }
            return productInfo;
        }

        /// <summary>
        /// 设置商品信息
        /// </summary>
        /// <returns></returns>
        public void SetProductInfoAsync(string id, ProductInfoEntity entity)
        {
            string key = CustomCacheKeys.ProductInfo(id);

            memoryCache.Set(key, entity, TimeSpan.FromMinutes(5));
             
        }

        /// <summary>
        /// 获取商品信息
        /// </summary>
        /// <returns></returns>
        public async Task<ProductInfoEntity?> GetProductInfoByAddressAsync(string address)
        {
            string key = CustomCacheKeys.ProductInfoByAddress(address);
            if (!memoryCache.TryGetValue(key, out ProductInfoEntity? productInfo))
            {
                productInfo = await productInfoRepository.FindByAddressAsync(address);
                if (productInfo != null)
                {
                    memoryCache.Set(key, productInfo, TimeSpan.FromMinutes(5));
                }
            }
            return productInfo;
        }

        /// <summary>
        /// 设置商品信息
        /// </summary>
        /// <returns></returns>
        public void SetProductInfoByAddress(string address, ProductInfoEntity product)
        {
            string key = CustomCacheKeys.ProductInfoByAddress(address);
            memoryCache.Set(key, product, TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// 删除商品信息
        /// </summary>
        /// <returns></returns>
        public void RemoveProductInfoByAddress(string address)
        {
            string key = CustomCacheKeys.ProductInfoByAddress(address);
            memoryCache.Remove(key);
        }

        /// <summary>
        /// 获取监控信息
        /// </summary>
        /// <param name="email"></param>
        /// <param name="productid"></param>
        /// <returns></returns>
        public async Task<ProductMonitorEntity?> GetProductMonitorAsync(string email, string productId)
        {
            string key = CustomCacheKeys.ProductMonitor(email, productId);
            if (!memoryCache.TryGetValue(key, out ProductMonitorEntity? productInfo))
            {
                productInfo = await productMonitorRepository.QueryAsync(email, productId);
                if (productInfo != null)
                {
                    memoryCache.Set(key, productInfo, TimeSpan.FromMinutes(30));
                }
            }
            return productInfo;
        }
    }
}
