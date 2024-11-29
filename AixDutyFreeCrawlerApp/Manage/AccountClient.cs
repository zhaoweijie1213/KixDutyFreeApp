using AixDutyFreeCrawler.App.Models;
using AixDutyFreeCrawler.App.Models.Entity;
using AixDutyFreeCrawler.App.Models.Response;
using AixDutyFreeCrawler.App.Repository;
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
        , IMemoryCache memoryCache, ProductMonitorRepository productMonitorRepository, ProductInfoRepository productInfoRepository) : ITransientDependency
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
        /// <param name="productAddress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CheckProductAvailabilityAsync(string productAddress, CancellationToken cancellationToken)
        {
            try
            {
                if (driver == null)
                {
                    throw new Exception("driver不能为null");
                }
                
                // 导航到商品页面
                driver.Navigate().GoToUrl(productAddress);
                if (!memoryCache.TryGetValue(productAddress, out ProductInfoEntity? product))
                {
                    product = await productInfoRepository.FindByAddressAsync(productAddress);
                    if (product == null || product?.UpdateTime <= DateTime.Now.AddDays(-7))
                    {
                        try
                        {
                            var productDetail = driver.FindElement(By.ClassName("product-detail"));
                            // 获取 data-pid 属性的值 得到商品id
                            string productId = productDetail.GetAttribute("data-pid");
                            if (productId != null)
                            {
                                product = new ProductInfoEntity()
                                {
                                    Id = productId,
                                    Address = productAddress,
                                    CreateTime = DateTime.Now,
                                    UpdateTime = DateTime.Now
                                };
                                product = await productInfoRepository.InsertAsync(product);
                            }
                        }
                        catch (NoSuchElementException e)
                        {
                            logger.LogWarning("TaskStartAsync:{Message}", e.Message);
                        }
                    }
                    //else
                    //{

                    //}
                    memoryCache.Set(productAddress, product, TimeSpan.FromMinutes(5));
                }

                if (IsLoginSuccess && !string.IsNullOrEmpty(product?.Id))
                {
                    //判断是否已经加入购物车
                    var prodicrMonitor = await productMonitorRepository.QueryAsync(Account.Email, product.Id);
                    if (prodicrMonitor != null && prodicrMonitor.Setup == OrderSetup.AddedToCart)
                    {
                        logger.LogInformation("{Account}\t{Name}已加入购物车，不需要检测", Account.Email, product.Name);
                        return;
                    }
                    else
                    {
                        // 检查商品是否有货（需要根据页面元素进行判断）
                        var res = await ProductVariationAsync(product.Id, 1);
                        if (res != null && string.IsNullOrEmpty(product.Name))
                        {
                            product.Name = res.Product?.ProductName ?? "";
                            await productInfoRepository.UpdateAsync(product);
                        }
                        bool isAvailable = res?.Product?.Availability?.Available ?? false;
                        if (isAvailable)
                        {
                            logger.LogInformation("账号:{Account}\t商品:{Name}可用,最大定购数{MaxOrderQuantity}", Account.Email, product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                            // 触发下单逻辑
                            await PlaceOrderAsync(res!.Product!);
                        }
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
                        //跳转到购物车页面
                        await seleniumService.ToCartAsync(driver);
                        var date = configuration.GetSection("FlightDate").Get<DateTime>();
                        //获取航班信息
                        var flightInfo = await GetFlightDataAsync(date);
                        if (flightInfo != null) 
                        {
                            try
                            {
                                // 查找csrf_token
                                var csrfTokenElement = driver.FindElement(By.Name("csrf_token"));
                                // 获取元素的 value 属性值
                                string csrfToken = csrfTokenElement.GetAttribute("value");
                                //保存航班信息
                                var saveInfo = await FlightSaveInfoAsync(
                                    csrfToken: csrfToken,
                                    calendarStartDate: DateTime.Now.ToString("yyyy/MM/dd"),
                                    departureDate: date.ToString("yyyy/MM/dd"),
                                    departureTime: date.ToString("HH:mm"),
                                    airlinesNo: flightInfo.First().Flightno1,
                                    flightNo: flightInfo.First().Flightno2,
                                    otherflightno: "",
                                    connectingFlight: "no");
                                //判断是否可以下单
                                if (saveInfo?.AllowCheckout == true)
                                {
                                    //调用下单逻辑,跳转结算界面
                                    driver.Navigate().GoToUrl("https://www.kixdutyfree.jp" + saveInfo.RedirectUrl);
                                    var dwfrm_billing = driver.FindElement(By.Id("driver"));
                                    // 查找csrf_token
                                    var billingCsrfTokenElement = dwfrm_billing.FindElement(By.Name("csrf_token"));
                                    // 获取元素的 value 属性值
                                    string billingCsrfToken = csrfTokenElement.GetAttribute("value");
                                    var submitPayment = await SubmitPaymentAsync(billingCsrfToken, csrfToken);
                                    if (submitPayment?.Error == false)
                                    {
                                        //最终确认下单
                                        var placeOrder = await PlaceOrderAsync();
                                        if (placeOrder?.Error == false)
                                        {
                                            logger.LogInformation("PlaceOrderAsync.下单成功");
                                        }
                                        else
                                        {
                                            logger.LogWarning("PlaceOrderAsync.下单失败");
                                        }
                                    }
                                    else
                                    {
                                        logger.LogWarning("PlaceOrderAsync.提交付款信息失败");
                                    }
                                }
                                else
                                {
                                    logger.LogWarning("PlaceOrderAsync.无法下单，请检查航班信息是否正确!");
                                }
                            }
                            catch (NoSuchElementException)
                            {
                                // 未找到元素，处理异常
                                logger.LogError("PlaceOrderAsync.未能找到csrf_token的输入元素");
                            }
                        }
                        else
                        {
                            logger.LogWarning("PlaceOrderAsync.未获取到{date}航班信息", date.ToString("D"));
                        }

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
        /// 获取航班信息
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<List<FlightData>?> GetFlightDataAsync(DateTime date)
        {
            string key = $"FlightData_{date:yyyy/MM/dd HH:mm}";
            if(!memoryCache.TryGetValue(key, out List<FlightData>? data))
            {
                //获取航班信息
                var flightInfo = await FlightGetInfoAsync(date);
                data = flightInfo?.FlightData;
                if (data != null)
                {
                    memoryCache.Set(key, data, TimeSpan.FromHours(1));
                }
            }
            return data;
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
        public async Task<FlightGetInfoResponse?> FlightGetInfoAsync(DateTime dateTime)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            FlightGetInfoResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Get, Cart.FlightGetInfo + $"?date={dateTime:yyyy/MM/dd}&time={dateTime:HH:mm}");
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
