using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Config;
using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Models.Response;
using KixDutyFree.App.Repository;
using KixDutyFree.App.Services;
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

namespace KixDutyFree.App.Manage
{
    /// <summary>
    /// 账号客户端
    /// </summary>
    public class AccountClient(ILogger<AccountClient> logger, SeleniumService seleniumService, IOptionsMonitor<Products> products, IConfiguration configuration, IHttpClientFactory httpClientFactory
        , IMemoryCache memoryCache, ProductMonitorRepository productMonitorRepository, ProductInfoRepository productInfoRepository,IOptionsMonitor<FlightInfoModel> flightInfoModel) : ITransientDependency
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        /// <summary>
        /// http客户端实例
        /// </summary>
        private HttpClient? _httpClient { get; set; }

        /// <summary>
        /// 浏览器实例后备字段
        /// </summary>
        private ChromeDriver? _driver;

        private readonly object _driverLock = new object();

        /// <summary>
        /// 错误次数
        /// </summary>
        private int ErrorCount { get; set; } = 0;

        /// <summary>
        /// 浏览器实例
        /// </summary>
        private ChromeDriver? driver
        {
            get
            {
                lock (_driverLock)
                {
                    return _driver;
                }
            }
            set
            {
                lock (_driverLock)
                {
                    _driver = value;
                }
            }
        }

        /// <summary>
        /// 封装的方法
        /// </summary>
        /// <param name="action"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void PerformDriverAction(Action<ChromeDriver> action)
        {
            lock (_driverLock)
            {
                if (driver != null)
                {
                    action(driver);
                }
                else
                {
                    throw new InvalidOperationException("driver 未初始化");
                }
            }
        }

        /// <summary>
        /// 异步令牌
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource { get; set; } = new();

        ///// <summary>
        ///// 监控状态
        ///// </summary>
        //private bool MonitorStatus { get; set; } = false;

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
        /// <param name="account"></param>
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
                _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                //ConfigureHttpClientWithCookies();
            }
            else
            {
                logger.LogError("InitAsync:{account}登录失败", account.Email);
            }
            //开始监控商品
            await StartMonitoringAsync();
            return IsLoginSuccess;
        }

        /// <summary>
        /// 同步cookie
        /// </summary>
        /// <exception cref="Exception"></exception>
        private string GetCookies()
        {
            if (driver == null || _httpClient == null)
            {
                throw new Exception("driver 或者_httpClient 不能为 null");
            }

            lock (_driverLock)
            {
                // 从 Selenium WebDriver 获取 Cookie
                var seleniumCookies = driver.Manage().Cookies.AllCookies;
                // 将 Cookie 转换为字符串
                var cookieHeader = string.Join("; ", seleniumCookies.Select(c => $"{c.Name}={c.Value}"));
                //_httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
                return cookieHeader;
            }
      
        }



        /// <summary>
        /// 开始监控
        /// </summary>
        public Task StartMonitoringAsync()
        {
            //MonitorStatus = true;

            // 如果已有取消令牌源，先取消并释放
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            // 创建新的取消令牌源
            _cancellationTokenSource = new CancellationTokenSource();
            return Task.Run(() => MonitorProductsAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void StopMonitoring()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            //_cancellationTokenSource = null;
        }

        /// <summary>
        /// 监控商品的异步方法
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task MonitorProductsAsync(CancellationToken cancellationToken)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (ErrorCount > 10 || driver == null)
                {
                    await RelodAsync();
                    break;
                }
                else
                {
                    foreach (var product in products.CurrentValue.Urls)
                    {
                        await CheckProductAvailabilityAsync(product, cancellationToken);
                    }
                }

                bool status = await seleniumService.IsLogin(driver);
                if (!status) 
                {
                    await RelodAsync();
                }
                // 设置检查间隔
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }

        /// <summary>
        /// 重置客户端
        /// </summary>
        /// <returns></returns>
        public async Task RelodAsync()
        {
            logger.LogInformation("MonitorProductsAsync.重新初始化客户端: {Email}", Account.Email);
            await QuitAsync();
            await InitAsync(Account);
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
                if (cancellationToken.IsCancellationRequested) return;
                if (driver == null)
                {
                    throw new Exception("driver不能为null");
                }
                
                // 导航到商品页面
                driver.Navigate().GoToUrl(productAddress);
                if (!memoryCache.TryGetValue(productAddress, out ProductInfoEntity? product))
                {
                    product = await productInfoRepository.FindByAddressAsync(productAddress);
                    if (product == null)
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
                    memoryCache.Set(productAddress, product, TimeSpan.FromMinutes(5));
                }

                if (IsLoginSuccess && !string.IsNullOrEmpty(product?.Id))
                {
                    //查询最新订单,判断是否已经加入购物车
                    var prodicrMonitor = await productMonitorRepository.QueryAsync(Account.Email, product.Id);
                    if (prodicrMonitor != null && prodicrMonitor.Setup != OrderSetup.Completed)
                    {
                        logger.LogInformation("{Account}\t{Name}已有订单,订单状态{Setup}", Account.Email, product.Name, prodicrMonitor.Setup.ToString());
                        return;
                    }
                    else
                    {
                        // 检查商品是否有货（需要根据页面元素进行判断）
                        var res = await ProductVariationAsync(product.Id, 1, cancellationToken);
                        if (res != null && string.IsNullOrEmpty(product.Name))
                        {
                            product.Name = res.Product?.ProductName ?? "";
                            product.Image = res.Product?.Images?.Large?.FirstOrDefault()?.AbsUrl ?? "";
                            await productInfoRepository.UpdateAsync(product);
                        }
                        bool isAvailable = res?.Product?.Availability?.Available ?? false;
                        if (isAvailable)
                        {
                            logger.LogInformation("账号:{Account}\t商品:{Name}可用,最大定购数{MaxOrderQuantity}", Account.Email, product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                            // 触发下单逻辑
                            await PlaceOrderAsync(res!.Product!, cancellationToken);
                        }
                        else
                        {
                            logger.LogInformation("账号:{Account}\t商品:{Name}不可用,最大定购数{MaxOrderQuantity}", Account.Email, product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                            ProductMonitorEntity productMonitor = await productMonitorRepository.QueryAsync(Account.Email, product.Id);
                            if (productMonitor != null)
                            {
                                productMonitor.Setup = OrderSetup.None;
                                productMonitor.UpdateTime = DateTime.Now;
                                await productMonitorRepository.UpdateAsync(productMonitor);
                            }
                            else
                            {
                                productMonitor = new ProductMonitorEntity()
                                {
                                    ProductId = product.Id,
                                    Account = Account.Email,
                                    CreateTime = DateTime.Now,
                                    UpdateTime = DateTime.Now,
                                    Setup = OrderSetup.None
                                };
                                productMonitor = await productMonitorRepository.InsertAsync(productMonitor);
                            }
                        }
                    }
         
                }
     
            }
            catch (Exception ex)
            {
                // 记录异常日志
                logger.BaseErrorLog($"CheckProductAvailabilityAsync.检查商品时出错", ex);
            }
        }

        /// <summary>
        /// 下单逻辑
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        private async Task PlaceOrderAsync(Product product, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (driver == null)
                {
                    throw new Exception("driver不能为null");
                }

                if (!string.IsNullOrEmpty(product.Id))
                {
                    int quantity = 0;
                    if (Account.Quantity >=0)
                    {
                        quantity = Account.Quantity;
                    }
                    else
                    {
                        quantity = product.Availability?.MaxOrderQuantity ?? 1;
                    }
                    //添加到购物车
                    var res = await CartAddProductAsync(product.Id, quantity, cancellationToken);

                    if (res?.Error == false)
                    {
                        logger.LogInformation("PlaceOrderAsync.商品 {BrandName} 加入购物车成功！", product.BrandName);
                        ProductMonitorEntity productMonitor = await productMonitorRepository.QueryAsync(Account.Email, product.Id);
                        if (productMonitor != null && productMonitor.Setup != OrderSetup.Completed)
                        {
                            productMonitor.Setup = OrderSetup.AddedToCart;
                            productMonitor.UpdateTime = DateTime.Now;
                            await productMonitorRepository.UpdateAsync(productMonitor);
                        }
                        else
                        {
                            productMonitor = new ProductMonitorEntity()
                            {
                                ProductId = product.Id,
                                Account = Account.Email,
                                CreateTime = DateTime.Now,
                                UpdateTime = DateTime.Now,
                                Setup = OrderSetup.AddedToCart
                            };
                            productMonitor = await productMonitorRepository.InsertAsync(productMonitor);
                        }
                        //跳转到购物车页面
                        await seleniumService.ToCartAsync(driver);
                        var date = flightInfoModel.CurrentValue.Date;
                        //获取航班信息
                        var flightInfo = await GetFlightDataAsync(Account.Date, cancellationToken);
                        if (flightInfo != null)
                        {
                            try
                            {
                                // 查找csrf_token
                                var csrfTokenElement = driver.FindElement(By.Name("csrf_token"));
                                // 获取元素的 value 属性值
                                string csrfToken = csrfTokenElement.GetAttribute("value");

                                var flight = flightInfo.First(i => i.AirlineName.Contains(Account.AirlineName) && i.Flightno2 == Account.FlightNo);

                                //string airlinesNo = flightInfo.First().Flightno1;
                                //string flightNo= flightInfo.First().Flightno2;
                                //保存航班信息
                                var saveInfo = await FlightSaveInfoAsync(
                                    csrfToken: csrfToken,
                                    calendarStartDate: DateTime.Now.ToString("yyyy/MM/dd"),
                                    departureDate: date.ToString("yyyy/MM/dd"),
                                    departureTime: date.ToString("HH:mm"),
                                    airlinesNo: flight.Flightno1,
                                    flightNo: flight.Flightno2,
                                    otherflightno: "",
                                    cancellationToken: cancellationToken,
                                    connectingFlight: "no");
                                //判断是否可以下单
                                if (saveInfo?.AllowCheckout == true)
                                {
                                    productMonitor.Setup = OrderSetup.FlightInfoSaved;
                                    productMonitor.UpdateTime = DateTime.Now;
                                    await productMonitorRepository.UpdateAsync(productMonitor);
                                    //调用下单逻辑,跳转结算界面
                                    driver.Navigate().GoToUrl("https://www.kixdutyfree.jp" + saveInfo.RedirectUrl);
                                    // 等待页面加载完成
                                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
                                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                                    var dwfrm_billing = driver.FindElement(By.Id("dwfrm_billing"));
                                    // 查找csrf_token
                                    var billingCsrfTokenElement = dwfrm_billing.FindElement(By.Name("csrf_token"));
                                    // 获取元素的 value 属性值
                                    string billingCsrfToken = billingCsrfTokenElement.GetAttribute("value");
                                    var submitPayment = await SubmitPaymentAsync(Account.Email, billingCsrfToken, cancellationToken);
                                    if (submitPayment?.Error == false)
                                    {
                                        productMonitor.Setup = OrderSetup.PaymentSubmitted;
                                        productMonitor.UpdateTime = DateTime.Now;
                                        await productMonitorRepository.UpdateAsync(productMonitor);
                                        //最终确认下单
                                        var placeOrder = await PlaceOrderAsync(cancellationToken);
                                        if (placeOrder?.Error == false)
                                        {
                                            productMonitor.Setup = OrderSetup.OrderPlaced;
                                            productMonitor.UpdateTime = DateTime.Now;
                                            productMonitor.OrderId = placeOrder.OrderID;
                                            productMonitor.OrderToken = placeOrder.OrderToken;
                                            await productMonitorRepository.UpdateAsync(productMonitor);
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
                ErrorCount++;
                // 记录异常日志
                logger.BaseErrorLog("PlaceOrderAsync", ex);
            }
        }

        /// <summary>
        /// 获取航班信息
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<List<FlightData>?> GetFlightDataAsync(DateTime date, CancellationToken cancellationToken)
        {
            string key = $"FlightData_{date:yyyy/MM/dd HH:mm}";
            if (!memoryCache.TryGetValue(key, out List<FlightData>? data))
            {
                //获取航班信息
                var flightInfo = await FlightGetInfoAsync(date, cancellationToken);
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
            logger.LogInformation("QuitAsync.停止实例:{email}", Account.Email);
            return Task.CompletedTask;
        }

        #region API请求

        /// <summary>
        /// 商品数量变化
        /// </summary>
        /// <returns></returns>
        public async Task<ProductVariationResponse?> ProductVariationAsync(string productId, int quantity, CancellationToken cancellationToken)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            ProductVariationResponse? data = null;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Cart.ProductVariation + $"?pid={productId}&quantity={quantity}");
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", GetCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
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
        public async Task<CartAddProductResponse?> CartAddProductAsync(string productId, int quantity, CancellationToken cancellationToken)
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
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", GetCookies());
            // 使用 FormUrlEncodedContent 设置请求内容
            request.Content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.SendAsync(request, cancellationToken);
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
        public async Task<CartUpdateQuantityResponse?> CartUpdateQuantityAsync(string productId, int quantity, string uuid, CancellationToken cancellationToken)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            CartUpdateQuantityResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Get, Cart.UpdateQuantity + $"?pid={productId}&quantity={quantity}&uuid={uuid}");
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", GetCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
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
        public async Task<FlightGetInfoResponse?> FlightGetInfoAsync(DateTime dateTime, CancellationToken cancellationToken)
        {
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            FlightGetInfoResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Get, Cart.FlightGetInfo + $"?date={dateTime:yyyy/MM/dd}&time={dateTime:HH:mm}");
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", GetCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
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
            , string connectingFlight, CancellationToken cancellationToken, string agreeProductLimits = "yes")
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
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", GetCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
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
        public async Task<SubmitPaymentResponse?> SubmitPaymentAsync(string email,string csrfToken, CancellationToken cancellationToken)
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
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", GetCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
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
        public async Task<PlaceOrderResponse?> PlaceOrderAsync(CancellationToken cancellationToken)
        { 
            if (_httpClient == null)
            {
                throw new Exception("_httpClient未初始化");
            }
            PlaceOrderResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Post, CheckoutServices.PlaceOrder);
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", GetCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
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
