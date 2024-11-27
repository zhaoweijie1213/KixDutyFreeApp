using AixDutyFreeCrawler.App.Models;
using AixDutyFreeCrawler.App.Models.Response;
using AixDutyFreeCrawler.App.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using QYQ.Base.Common.Extension;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Concurrent;
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
        private ChromeDriver? driver;

        /// <summary>
        /// 异步令牌
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// 
        /// </summary>
        private AccountModel Account {  get; set; } = new AccountModel();

        /// <summary>
        /// 是否成功登录
        /// </summary>
        private bool IsLoginSuccess { get; set; } = false;

        private ConcurrentDictionary<string, ProductMonitorInfo> ProductsMonitor { get; set; } = new();


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> InitAsync(AccountModel account)
        {
            Account = account;
            bool headless = configuration.GetSection("Headless").Get<bool>();
            //_httpClient = httpClientFactory.CreateClient(account.Email);
            var res = await seleniumService.CreateInstancesAsync(account, headless);
            driver = res.Item1;
            IsLoginSuccess = res.Item2;
            if (IsLoginSuccess)
            {
                logger.LogInformation("InitAsync.初始化完成");
                //设置httpClient Cookie
                _httpClient = httpClientFactory.CreateClient(account.Email);
                ConfigureHttpClientWithCookies();
            }
            else
            {
                logger.LogError("InitAsync:{account}登录失败", account.Email);
            }
            return IsLoginSuccess;
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
        public Task StartMonitoringAsync()
        {
            return Task.Run(() => MonitorProductsAsync(_cancellationTokenSource.Token));
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
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
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
                        if (productId != null)
                        {
                            memoryCache.Set(product, productId);
                        }
                    }
                    catch (NoSuchElementException e)
                    {
                        logger.LogWarning("TaskStartAsync:{Message}", e.Message);
                    }
                }

                if (IsLoginSuccess)
                {
                    // 检查商品是否有货（需要根据页面元素进行判断）
                    var res = await ProductVariationAsync(productId!, 1);
                    bool isAvailable = res?.Product?.Availability?.Available ?? false;
                    if (isAvailable)
                    {
                        logger.LogInformation("账号:{Account}\t商品:{brandName}可用,最大定购数{MaxOrderQuantity}", Account.Email, res?.Product?.BrandName, res?.Product?.Availability?.MaxOrderQuantity);
                        // 触发下单逻辑
                        await PlaceOrderAsync(res!.Product!);
                    }
                }
     
            }
            catch (Exception ex)
            { 
                // 记录异常日志
                logger.LogError($"CheckProductAvailabilityAsync.检查商品时出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 下单逻辑
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        private async Task PlaceOrderAsync(Product product)
        {
            try
            {
                if (driver == null)
                {
                    throw new Exception("driver不能为null");
                }
               
                if (!string.IsNullOrEmpty(product.Id))
                {
                    var res = await CartAddProductAsync(product.Id, product.Availability?.MaxOrderQuantity ?? 10);
                    if (res?.Error == false)
                    {
                        logger.LogInformation("PlaceOrderAsync.商品 {BrandName} 加入购物车成功！", product.BrandName);

                    }
                }
                else
                {
                    logger.LogWarning("PlaceOrderAsync.找不到商品Id");
                }
            }
            catch (Exception ex)
            {
                // 记录异常日志
                logger.BaseErrorLog("PlaceOrderAsync", ex);
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

        #region API请求

        /// <summary>
        /// 商品数量变化
        /// </summary>
        /// <returns></returns>
        public async Task<ProductVariationResponse?> ProductVariationAsync(string productId, int quantity)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            ProductVariationResponse? data = null;
            var response = await _httpClient.GetAsync(Cart.ProductVariation + $"?pid={productId}&quantity={quantity}");
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("ProductVariationAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<ProductVariationResponse>(content);
            }
            return data;
        }

        /// <summary>
        /// 添加到购物车
        /// </summary>
        /// <returns></returns>
        public async Task<CartAddProductResponse?> CartAddProductAsync(string productId, int quantity)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            CartAddProductResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Post, Cart.AddProduct);
            // 构建表单数据
            var formData = new List<KeyValuePair<string, string>>
            {
                new("pid", productId),
                new("quantity", quantity.ToString())
            };
            // 使用 FormUrlEncodedContent 设置请求内容
            request.Content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<CartAddProductResponse>(content);
            }
            return data;
        }

        /// <summary>
        /// 修改购物车商品数量
        /// </summary>
        public async Task<CartUpdateQuantityResponse?> CartUpdateQuantityAsync(string productId, int quantity, string uuid)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            CartUpdateQuantityResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Get, Cart.UpdateQuantity + $"?pid={productId}&quantity={quantity}&uuid={uuid}");
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<CartUpdateQuantityResponse>(content);
            }
            return data;
        }

        /// <summary>
        /// 航班信息
        /// </summary>
        /// <param name="date">yyyy/MM/dd</param>
        /// <param name="time">HH:mm</param>
        /// <returns></returns>
        public async Task<FlightGetInfoResponse?> FlightGetInfoAsync(string date, string time)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            FlightGetInfoResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Get, Cart.FlightGetInfo + $"?date={date}&time={time}");
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<FlightGetInfoResponse>(content);
            }
            return data;
        }

        /// <summary>
        /// 保存航班信息
        /// </summary>
        /// <param name="csrfToken"></param>
        /// <param name="calendarStartDate"></param>
        /// <param name="departureDate"></param>
        /// <param name="departureTime"></param>
        /// <param name="airlinesNo">航空公司</param>
        /// <param name="flightNo">航班号  其他航班号:other</param>
        /// <param name="otherflightno">其他航班号</param>
        /// <param name="connectingFlight">是否转机 yes/no</param>
        /// <param name="agreeProductLimits"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<FlightSaveInfoResponse?> FlightSaveInfoAsync(string csrfToken, string calendarStartDate, string departureDate, string departureTime, string airlinesNo, string flightNo, string otherflightno
            , string connectingFlight, string agreeProductLimits = "yes")
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            FlightSaveInfoResponse? data = null;
            // 构建表单数据
            var formData = new List<KeyValuePair<string, string>>
            {
                new("csrf_token", csrfToken),
                new("midNightFlight", "no"),
                new("calendarStartDate", calendarStartDate),
                new("departureDate", departureDate),
                new("departureTime", departureTime),
                new("airlinesNo", airlinesNo),
                new("flightNo", flightNo),
                new("otherflightno", otherflightno),
                new("connectingFlight", connectingFlight),
                new("agreeProductLimits", agreeProductLimits)
            };
            var request = new HttpRequestMessage(HttpMethod.Post, Cart.FlightSaveInfo)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<FlightSaveInfoResponse>(content);
            }
            return data;
        }

        /// <summary>
        /// 提交付款信息
        /// </summary>
        /// <returns></returns>
        public async Task<SubmitPaymentResponse?> SubmitPaymentAsync(string email,string csrfToken)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            SubmitPaymentResponse? data = null;
            // 构建表单数据
            var formData = new List<KeyValuePair<string, string>>
            {
                //new ("hidCardYear", "undefined"),
                //new ("hidCardMonth", "undefined"),
                //new ("hidCardNo", ""),
                //new ("hidCardType", ""),
                //new ("addressSelector", "new"),
                //new ("dwfrm_billing_addressFields_firstName", ""),
                //new ("dwfrm_billing_addressFields_lastName", ""),
                //new ("dwfrm_billing_addressFields_address1", ""),
                //new ("dwfrm_billing_addressFields_address2", ""),
                //new ("dwfrm_billing_addressFields_country", ""),
                //new ("dwfrm_billing_addressFields_city", ""),
                //new ("dwfrm_billing_addressFields_postalCode", ""),
                new ("csrf_token", csrfToken),
                //new ("localizedNewAddressTitle", "新地址"),
                new ("dwfrm_billing_contactInfoFields_email", email),
                //new ("dwfrm_billing_contactInfoFields_phone", ""),
                new ("dwfrm_billing_paymentMethod", "Instore_Payment") //到店支付
            };

            var request = new HttpRequestMessage(HttpMethod.Post, CheckoutServices.SubmitPayment)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("SubmitPaymentAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<SubmitPaymentResponse>(content);
            }
            return data;
        }

        /// <summary>
        /// 下单
        /// </summary>
        /// <returns></returns>
        public async Task<PlaceOrderResponse?> PlaceOrderAsync()
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            PlaceOrderResponse? data = null;
            var response = await _httpClient.PostAsync(CheckoutServices.PlaceOrder, null);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("PlaceOrderAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<PlaceOrderResponse>(content);
            }
            return data;
        }

        #endregion
    }
}
