using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Models.Response;
using KixDutyFree.App.Quartz;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models;
using KixDutyFree.Shared.Models.Entity;
using KixDutyFree.Shared.Models.Input;
using KixDutyFree.Shared.Quartz.Jobs;
using KixDutyFree.Shared.Repository;
using log4net.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OpenQA.Selenium.Support.UI;
using Quartz;
using QYQ.Base.Common.Extension;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class ProductService(ILogger<ProductService> logger,ProductInfoRepository productInfoRepository, ISchedulerFactory schedulerFactory, IConfiguration configuration, SeleniumService seleniumService
        , CacheManage cacheManage) : ISingletonDependency
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
        /// 同步账号
        /// </summary>
        /// <returns></returns>
        public async Task SyncProductAsync()
        {
            var data = await cacheManage.GetExcelProductsAsync();
            var accounts = await productInfoRepository.QueryAsync();
            if (accounts.Count <= 0)
            {
                if (data != null)
                {
                    foreach (var config in data) 
                    {
                        await StartMonitorAsync(new AddProductInput()
                        {
                            Address = config.Address,
                            Quantity = config.Quantity
                        });
                    }
                }
            }
        }

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
        /// 新增商品监控任务
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> StartMonitorAsync(AddProductInput input)
        {
            bool status = false;
            try
            {
                //开始商品监控任务
                bool headless = configuration.GetSection("Headless").Get<bool>();
                //创建浏览器实例
                var res = await seleniumService.CreateInstancesAsync(null, headless);

                var info = await seleniumService.GetProductIdAsync(input.Address, res.Item1);

                if (info != null)
                {
                    var scheduler = await schedulerFactory.GetScheduler();
                    var job = JobBuilder.Create<MonitorProducts>()
                        .UsingJobData("id", info.Id)
                        .WithIdentity($"monitor_job_{info.Id}", "monitor")
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity($"monitor_trigger_{info.Id}", "monitor")
                        .StartNow()
                        .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(10)
                        .RepeatForever())
                        .Build();
                    await scheduler.ScheduleJob(job, trigger);
                    logger.LogInformation("StartMonitorAsync.添加监控任务:{address}", input.Address);
                    UpdateMonitorStatus(info.Id, true);

                    status = true;
                }
                else
                {
                    status = false;
                    logger.LogWarning("StartMonitorAsync.未获取到商品信息:{address}", input.Address);
                }
                res.Item1.Quit();
                res.Item1.Dispose();
            }
            catch (Exception e)
            {
                logger.BaseErrorLog("StartMonitorAsync", e);
            }
            return status;
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
                var scheduler = await schedulerFactory.GetScheduler();
                status = await scheduler.UnscheduleJob(new TriggerKey($"monitor_trigger_{id}", "monitor"));
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

        /// <summary>
        /// 更新某个商品的库存
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="isAvailable"></param>
        /// <param name="maxQuantity"></param>
        public void UpdateStock(string productId, bool isAvailable, int maxQuantity)
        {
            var product = _productStocks.FirstOrDefault(i => i.Id == productId);
            if (product != null)
            {
                product.MaxQuantity = maxQuantity;
                product.IsAvailable = isAvailable;
                NotifyStateChanged();
            }

        }

        /// <summary>
        /// 更新商品可用数量
        /// </summary>
        /// <param name="address"></param>
        /// <param name="quantity"></param>
        public async Task UpdateMonitorStatusAndQuantityAsync(string id, int quantity)
        {
            await productInfoRepository.UpdateQuantityAsync(id, quantity);
            var product = _productStocks.FirstOrDefault(i => i.Id == id);
            if (product != null)
            {
                product.Quantity = quantity;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 更新监控状态
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="monitorStatus"></param>
        public void UpdateMonitorStatus(string productId, bool monitorStatus)
        {
            var product = _productStocks.FirstOrDefault(i => i.Id == productId);
            if (product != null)
            {
                product.MonitorStatus = monitorStatus;
                NotifyStateChanged();
            }
     
        }
    }
}
