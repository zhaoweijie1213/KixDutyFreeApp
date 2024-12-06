using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Models.Excel;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Manage;
using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;

namespace KixDutyFree.App.Manage
{
    public class CacheManage(ILogger<CacheManage> logger, IMemoryCache memoryCache,ProductInfoRepository productInfoRepository) : ITransientDependency
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
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "账号.xlsx");
                var importer = new ExcelImporter();
                var result = await importer.Import<AccountModel>(path, null);
                if(result.Data.Count > 0)
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
        public async Task<List<ProductModel>?> GetProductsAsync()
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
                    memoryCache.Set(key, productInfo, TimeSpan.FromMinutes(30));
                }
            }
            return productInfo;
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
                    memoryCache.Set(key, productInfo, TimeSpan.FromMinutes(30));
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
            memoryCache.Set(key, product, TimeSpan.FromMinutes(30));
        }
    }
}
