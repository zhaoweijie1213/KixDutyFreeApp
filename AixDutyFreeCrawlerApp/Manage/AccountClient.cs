using AixDutyFreeCrawler.App.Models;
using AixDutyFreeCrawler.App.Models.Response;
using AixDutyFreeCrawler.App.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using QYQ.Base.Common.IOCExtensions;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace AixDutyFreeCrawler.App.Manage
{
    /// <summary>
    /// 账号客户端
    /// </summary>
    public class AccountClient(ILogger<AccountClient> logger, SeleniumService seleniumService, IOptionsMonitor<Products> products, IConfiguration configuration, IHttpClientFactory httpClientFactory
        , IMemoryCache memoryCache) : ITransientDependency
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        /// <summary>
        /// http客户端实例
        /// </summary>
        private HttpClient? _httpClient { get; set; }

        /// <summary>
        /// 浏览器实例
        /// </summary>
        private IWebDriver? driver;

        /// <summary>
        /// 异步令牌
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new();


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task InitAsync(AccountModel account)
        {
            bool headless = configuration.GetSection("Headless").Get<bool>();
            //_httpClient = httpClientFactory.CreateClient(account.Email);
            driver = await seleniumService.CreateInstancesAsync(account, headless);
            logger.LogInformation("InitAsync.初始化完成");
            //设置httpClient Cookie
            _httpClient = httpClientFactory.CreateClient(account.Email);
            ConfigureHttpClientWithCookies();
        }

        /// <summary>
        /// 设置Cookie
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void ConfigureHttpClientWithCookies()
        {
            if (driver == null || _httpClient == null)
            {
                throw new Exception("driver 或者_httpClient 不能为 null");
            }
            SyncCookies();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            logger.LogInformation("HttpClient 已配置完成，并同步了浏览器的 Cookie。");
        }

        /// <summary>
        /// 同步cookie
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void SyncCookies()
        {
            if (driver == null || _httpClient == null)
            {
                throw new Exception("driver 或者_httpClient 不能为 null");
            }
            // 从 Selenium WebDriver 获取 Cookie
            var seleniumCookies = driver.Manage().Cookies.AllCookies;
            // 将 Cookie 转换为字符串
            var cookieHeader = string.Join("; ", seleniumCookies.Select(c => $"{c.Name}={c.Value}"));
            _httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        }



        /// <summary>
        /// 开始监控
        /// </summary>
        public void StartMonitoring()
        {
            Task.Run(() => MonitorProductsAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void StopMonitoring()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// 监控商品的异步方法
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task MonitorProductsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var product in products.CurrentValue.Urls)
                {
                    await CheckProductAvailabilityAsync(product, cancellationToken);
                }

                // 设置检查间隔
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
        }


        /// <summary>
        /// 检查单个商品是否有货
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CheckProductAvailabilityAsync(string product, CancellationToken cancellationToken)
        {
            try
            {
                if (driver == null)
                {
                    throw new Exception("driver不能为null");
                }
                // 导航到商品页面
                driver.Navigate().GoToUrl(product);
                if (!memoryCache.TryGetValue(product, out string? productId))
                {
                    try
                    {
                        var productDetail = driver.FindElement(By.ClassName("product-detail"));
                        // 获取 data-pid 属性的值 得到商品id
                        productId = productDetail.GetAttribute("data-pid");
                        if(product != null)
                        {
                            memoryCache.Set(product, productId);
                        }
                    }
                    catch (NoSuchElementException e)
                    {
                        logger.LogWarning("TaskStartAsync:{Message}", e.Message);
                    } 
                }
       
                // 检查商品是否有货（需要根据页面元素进行判断）
                bool isAvailable = await IsProductAvailable(productId!);

                if (isAvailable)
                {
                    // 触发下单逻辑
                    await PlaceOrderAsync(product);
                }
            }
            catch (Exception ex)
            {
                // 记录异常日志
                logger.LogError($"检查商品时出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 判断商品是否有货
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> IsProductAvailable(string productId)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient");
            }

            try
            {
                var response = await _httpClient.GetAsync(Cart.ProductVariation + $"?pid={productId}&quantity=1");
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var data = JsonConvert.DeserializeObject<ProductVariationResponse>(content);
                    if (data?.Product?.Availability?.Available == true)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (WebDriverTimeoutException)
            {
                // 元素在指定时间内未出现，可能是无货状态
                return false;
            }
        }

        // 下单逻辑
        private async Task PlaceOrderAsync(string product)
        {
            try
            {
                if (driver == null)
                {
                    throw new Exception("driver不能为null");
                }
                // 添加商品到购物车
                var addToCartButton = driver.FindElement(By.Id("add-to-cart-button"));
                addToCartButton.Click();

                // 等待购物车页面加载
                await Task.Delay(TimeSpan.FromSeconds(2));

                // 进入结算页面
                driver.Navigate().GoToUrl("https://www.kixdutyfree.jp/cn/checkout/");

                // 填写订单信息，选择支付方式等
                // 需要根据页面实际情况进行元素定位和操作

                // 提交订单
                var placeOrderButton = driver.FindElement(By.Id("place-order-button"));
                placeOrderButton.Click();

                // 等待订单确认页面加载
                await Task.Delay(TimeSpan.FromSeconds(2));

                // 记录下单成功的信息
                logger.LogInformation($"商品 {product} 下单成功！");
            }
            catch (Exception ex)
            {
                // 记录异常日志
                logger.LogError($"下单时出错：{ex.Message}");
            }
        }


        /// <summary>
        /// 退出
        /// </summary>
        /// <returns></returns>
        public Task QuitAsync()
        {
            // 停止监控
            StopMonitoring();
            driver?.Quit();
            return Task.CompletedTask;
        }
    }
}
