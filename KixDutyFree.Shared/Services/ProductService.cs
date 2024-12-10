using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Models.Response;
using KixDutyFree.App.Quartz;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models;
using KixDutyFree.Shared.Models.Input;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OpenQA.Selenium.Support.UI;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class ProductService(ProductInfoRepository productInfoRepository, CacheManage cacheManage, QuartzManagement quartzManagement) : ISingletonDependency
    {
        private List<ProductMonitorInfo> _productStocks = [];

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        // 事件，用于通知订阅者库存变化
        public event Action? OnChange;

        // 获取当前库存
        public List<ProductMonitorInfo> Products
        {
            get
            {
                // 返回库存的副本，避免外部修改
                return new List<ProductMonitorInfo>(_productStocks);
            }
        }

        // 通知订阅者状态变化
        private void NotifyStateChanged() => OnChange?.Invoke();

        /// <summary>
        /// 获取商品
        /// </summary>
        /// <returns></returns>
        public async Task<List<ProductMonitorInfo>> GetProductsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                //var products = await cacheManage.GetProductsAsync();
                var list = await productInfoRepository.QueryAsync();
                foreach (var item in list)
                {
                    var data = _productStocks.FirstOrDefault(i => i.Id == item.Id);
                    if (data == null)
                    {
                        _productStocks.Add(new ProductMonitorInfo()
                        {
                            Id = item.Id,
                            Address = item.Address,
                            Name = item.Name,
                            IsAvailable = false,
                            MonitorStatus = false,
                            Image = item.Image,
                            Quantity = item.Quantity,
                            CreateTime = item.CreateTime
                        });
                        NotifyStateChanged();
                    }
                }
                return Products;
            }
            finally
            {
                _semaphore.Release();
            }

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
        /// 新增或修改商品
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<ProductInfoEntity> ProductAddOrUpdateAync(ProductInfoEntity entity)
        {
            var product = await productInfoRepository.FindAsync(entity.Id);
            if (product == null)
            {
                product = await productInfoRepository.InsertAsync(entity);
                _productStocks.Add(new ProductMonitorInfo()
                {
                    Id = product.Id,
                    Address = product.Address,
                    Name = product.Name,
                    Image = product.Image,
                    Quantity = product.Quantity,
                    CreateTime = product.CreateTime
                });
            }
            else
            {
                await productInfoRepository.UpdateAsync(entity);
                var productInfo = _productStocks.FirstOrDefault(i => i.Id == entity.Id);
                if (productInfo == null)
                {
                    _productStocks.Add(new ProductMonitorInfo()
                    {
                        Id = product.Id,
                        Address = product.Address,
                        Name = product.Name,
                        Image = product.Image,
                        Quantity = product.Quantity,
                        CreateTime = product.CreateTime
                    });
                }
                else
                {
                    productInfo.Name = product.Name;
                    productInfo.Image = product.Image;
                }
            }
            NotifyStateChanged();
            return product;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DelAsync(string id)
        {
            var count = await productInfoRepository.DeleteAsync(id);
            NotifyStateChanged();
            bool status = false;
            if (count > 0)
            {
                //取消商品监控任务
                status = await quartzManagement.CancelMonitorAsync(id);
                var product = _productStocks.FirstOrDefault(i => i.Id == id);
                if (product != null)
                {
                    _productStocks.Remove(product);
                    NotifyStateChanged();
                }
            }
            return status;
        }

        /// <summary>
        /// 查询商品
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<ProductInfoEntity> GetProductAsync(string address)
        {
           return await productInfoRepository.FindByAddressAsync(address);
        }

        // 更新某个商品的库存
        public void UpdateStock(string productId, int maxQuantity)
        {
            var product = _productStocks.FirstOrDefault(i => i.Id == productId);
            if (product != null)
            {
                product.MaxQuantity = maxQuantity;
                product.IsAvailable = true;
                NotifyStateChanged();
            }
            
        }
    }
}
