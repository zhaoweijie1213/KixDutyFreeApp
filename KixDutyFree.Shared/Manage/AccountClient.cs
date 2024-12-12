using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Config;
using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Models.Response;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.EventHandler;
using KixDutyFree.Shared.Manage;
using KixDutyFree.Shared.Models.Entity;
using KixDutyFree.Shared.Services;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Modules.Network;
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
    public class AccountClient(ILogger<AccountClient> logger, SeleniumService seleniumService, IConfiguration configuration, IHttpClientFactory httpClientFactory
        , IMemoryCache memoryCache, ProductMonitorRepository productMonitorRepository, ProductInfoRepository productInfoRepository, IOptionsMonitor<FlightInfoModel> flightInfoModel
        , ExcelProcess excelProcess, CacheManage cacheManage, ProductService productService, IMediator  mediator, OrderService orderService) : ITransientDependency
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        /// <summary>
        /// http客户端实例
        /// </summary>
        private HttpClient? _httpClient { get; set; }

        /// <summary>
        /// 浏览器实例
        /// </summary>
        private ChromeDriver? _driver { get; set; }

        /// <summary>
        /// 错误次数
        /// </summary>
        public int ErrorCount { get; set; }

        private readonly SemaphoreSlim _driverSemaphore = new(1, 1);

        /// <summary>
        /// 账号信息
        /// </summary>
        public AccountInfo Account { get; set; } = new AccountInfo();

        /// <summary>
        /// 是否成功登录
        /// </summary>
        public bool IsLoginSuccess { get; set; } = false;

        /// <summary>
        /// 正在加载
        /// </summary>
        private bool IsLoading { get; set; }

        /// <summary>
        /// 异步获取浏览器实例
        /// </summary>
        /// <returns></returns>
        public async Task<ChromeDriver?> GetDriverAsync()
        {
            await _driverSemaphore.WaitAsync();
            try
            {
                return _driver;
            }
            finally
            {
                _driverSemaphore.Release();
            }
        }

        /// <summary>
        /// 异步设置浏览器实例
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        public async Task SetDriverAsync(ChromeDriver? driver)
        {
            await _driverSemaphore.WaitAsync();
            try
            {
                _driver = driver;
            }
            finally
            {
                _driverSemaphore.Release();
            }
        }

        /// <summary>
        /// 异步执行浏览器操作，确保线程安全，并返回结果
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="action">浏览器操作委托，返回 Task&lt;T&gt;</param>
        /// <returns>Task&lt;T&gt; 结果</returns>
        public async Task<T> ExecuteDriverActionAsync<T>(Func<ChromeDriver, Task<T>> action)
        {
            await _driverSemaphore.WaitAsync();
            try
            {
                if (_driver == null)
                {
                    throw new InvalidOperationException("ChromeDriver 实例未初始化");
                }
                return await action(_driver);
            }
            finally
            {
                _driverSemaphore.Release();
            }
        }

        /// <summary>
        /// 异步执行浏览器操作，确保线程安全
        /// </summary>
        /// <param name="action">浏览器操作委托，返回 Task</param>
        /// <returns>Task</returns>
        public async Task ExecuteDriverActionAsync(Func<ChromeDriver, Task> action)
        {
            await _driverSemaphore.WaitAsync();
            try
            {
                if (_driver == null)
                {
                    throw new InvalidOperationException("ChromeDriver 实例未初始化");
                }
                await action(_driver);
            }
            finally
            {
                _driverSemaphore.Release();
            }
        }



        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<bool> InitAsync(AccountInfo? account)
        {
            try
            {
                bool headless = configuration.GetSection("Headless").Get<bool>();
                //_httpClient = httpClientFactory.CreateClient(account.Email);
                var res = await seleniumService.CreateInstancesAsync(account, headless);
                await SetDriverAsync(res.Item1);
                IsLoginSuccess = res.Item2;
                //设置httpClient Cookie
                _httpClient = httpClientFactory.CreateClient(account?.Email ?? "DefaultClient");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                await SyncCookies();
                logger.LogInformation("InitAsync.初始化完成");
                if (IsLoginSuccess && account != null)
                {
                    Account = account;
                    //发送登录成功事件
                    await mediator.Publish(new UserLoginStatusChangedNotification(account.Email,IsLoginSuccess));
                }
                ////开始监控商品
                //await StartMonitoringAsync();

            }
            catch (Exception e)
            {
                logger.BaseErrorLog("InitAsync", e);
            }
            ErrorCount = 0;
            return IsLoginSuccess;
        }

        /// <summary>
        /// 
        /// </summary>
        private string CookieHeader {  get; set; } = string.Empty;

        /// <summary>
        /// 同步cookie
        /// </summary>
        /// <exception cref="Exception"></exception>
        public async Task<string> SyncCookies()
        {
            if (!string.IsNullOrEmpty(CookieHeader)) return CookieHeader;
            if (_driver == null || _httpClient == null)
            {
                throw new Exception("driver 或者_httpClient 不能为 null");
            }
            CookieHeader = await ExecuteDriverActionAsync(async (driver) =>
            {
                // 从 Selenium WebDriver 获取 Cookie
                var seleniumCookies = driver.Manage().Cookies.AllCookies;
                return await Task.FromResult(string.Join("; ", seleniumCookies.Select(c => $"{c.Name}={c.Value}")));
            });
            return CookieHeader;
        }

        /// <summary>
        /// 检查登录状态
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckLoginStatusAsync()
        {
            bool status = false;
            if (!IsLoading)
            {
                status = await ExecuteDriverActionAsync(async (driver) =>
                {
                    return await seleniumService.IsLogin(driver);
                });
            }

            return status;
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
        /// 检查商品是否有货
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> CheckProductAvailabilityAsync(ProductInfoEntity product, CancellationToken cancellationToken)
        {
            bool isAvailable = false;
            try
            {
                if (_driver == null)
                {
                    throw new Exception("driver不能为null");
                }
                // 检查商品是否有货
                var res = await ProductVariationAsync(product.Id, 1, cancellationToken);
                isAvailable = res?.Product?.Availability?.Available ?? false;
                if (res != null && string.IsNullOrEmpty(product.Name))
                {
                    product.Name = res.Product?.ProductName ?? "";
                    product.Image = res.Product?.Images?.Large?.FirstOrDefault()?.AbsUrl ?? "";
                    product.UpdateTime = DateTime.Now;
                    await productService.ProductAddOrUpdateAync(product);
                }
                if (isAvailable)
                {
                    logger.LogInformation("商品:{Name}可用,最大定购数{MaxOrderQuantity}", product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                }
                else
                {
                    logger.LogInformation("商品:{Name}不可用,最大定购数{MaxOrderQuantity}", product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                }

                productService.UpdateStock(product.Id, isAvailable, res?.Product?.Availability?.MaxOrderQuantity ?? 0);
            }
            catch (Exception e)
            {
                logger.BaseErrorLog("CheckProductAvailabilityAsync", e);
            }

            return isAvailable;
        }


        /// <summary>
        /// 检查下单流程
        /// </summary>
        /// <param name="productAddress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<(bool, ProductVariationResponse?)> FullCheckProductAvailabilityAsync(ProductInfoEntity product, CancellationToken cancellationToken)
        {
            bool isAvailable = false;
            try
            {
                if (cancellationToken.IsCancellationRequested == true) return (isAvailable, null);
                if (_driver == null)
                {
                    throw new Exception("driver不能为null");
                }

                if (IsLoginSuccess && !string.IsNullOrEmpty(product?.Id))
                {
                    //查询最新订单,判断是否已经加入购物车
                    var productMonitor = await productMonitorRepository.QueryAsync(Account.Email, product.Id);
                    if (productMonitor != null && productMonitor.Setup == OrderSetup.OrderPlaced)
                    {
                        logger.LogInformation("{Account}\t{Name}已有订单,订单状态{Setup}", Account.Email, product.Name, productMonitor.Setup.ToString());
                        return (isAvailable, null);
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
                        isAvailable = res?.Product?.Availability?.Available ?? false;
                        if (isAvailable)
                        {
                            //var productConfig = await cacheManage.GetProductsAsync();
                            logger.LogInformation("账号:{Account}\t商品:{Name}可用,最大定购数{MaxOrderQuantity}", Account.Email, product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                            // 触发下单逻辑
                            await PlaceOrderAsync(res!.Product!, product.Quantity, cancellationToken);
                        }
                        else
                        {
                            logger.LogInformation("账号:{Account}\t商品:{Name}不可用,最大定购数{MaxOrderQuantity}", Account.Email, product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                            //ProductMonitorEntity prodicrMonitor = await productMonitorRepository.QueryAsync(Account.Email, product.Id);
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
                            return (isAvailable, res);
                        }
                    }
         
                }
     
            }
            catch (WebDriverTimeoutException ex)
            {
                logger.BaseErrorLog("CheckProductAvailabilityAsync.WebDriverTimeoutException", ex);
                await RelodAsync();
            }
            catch (Exception ex)
            {
                ErrorCount++;
                logger.LogError("CheckProductAvailabilityAsync.错误次数: {ErrorCount}", ErrorCount);
                // 记录异常日志
                logger.BaseErrorLog($"CheckProductAvailabilityAsync.检查商品时出错", ex);
            }
            return (isAvailable, null);
        }

        /// <summary>
        /// 下单逻辑
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public async Task PlaceOrderAsync(Product product,int quantity , CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (_driver == null)
                {
                    throw new Exception("driver不能为null");
                }

                if (!string.IsNullOrEmpty(product.Id))
                {
                    //确认商品数量
                    if (Account.Quantity > 0 && product.Availability?.MaxOrderQuantity > Account.Quantity)
                    {
                        quantity = Account.Quantity;
                    }
                    else if (quantity == 0 || quantity > product.Availability?.MaxOrderQuantity)
                    {
                        quantity = product.Availability?.MaxOrderQuantity ?? 1;
                    }
                    CartAddProductResponse? res = null;
                    var productMonitor = await cacheManage.GetProductMonitorAsync(Account.Email, product.Id);
                    if (productMonitor == null)
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
                    if (productMonitor.Setup == OrderSetup.None || productMonitor.Setup == OrderSetup.Completed)
                    {
                        //添加到购物车
                        res = await CartAddProductAsync(product.Id, quantity, cancellationToken);
                        if (res?.Error == false)
                        {
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
                        }
                    }
                    if (res?.Error == false || productMonitor.Setup == OrderSetup.AddedToCart || productMonitor.Setup == OrderSetup.FlightInfoSaved)
                    {
                        logger.LogInformation("PlaceOrderAsync.商品 {BrandName} 加入购物车成功！", product.BrandName);
            
                        //跳转到购物车页面
                        await ExecuteDriverActionAsync((driver) =>
                        {
                            return seleniumService.ToCartAsync(driver);
                        });
                        var date = Account.Date;
                        //获取航班信息
                        var flightInfo = await GetFlightDataAsync(Account.Date, cancellationToken);
                        if (flightInfo != null)
                        {

                            await ExecuteDriverActionAsync(async (driver) =>
                            {
                                try
                                {
                                    //保存航班信息
                                    FlightSaveInfoResponse? saveInfo = null;
                                    if (productMonitor.Setup == OrderSetup.AddedToCart)
                                    {
                                        // 查找csrf_token
                                        var csrfTokenElement = driver.FindElement(By.Name("csrf_token"));
                                        // 获取元素的 value 属性值
                                        string csrfToken = csrfTokenElement.GetDomAttribute("value");

                                        var flight = flightInfo.FirstOrDefault(i => i.AirlineName.Contains(Account.AirlineName) && i.Flightno2 == Account.FlightNo);
                                        string otherflightno = "";
                                        if (flight == null)
                                        {
                                            flight = flightInfo.First(i => i.AirlineName.Contains(Account.AirlineName));
                                            flight.Flightno2 = "other";
                                            otherflightno = Account.FlightNo;
                                        }
                                        //string airlinesNo = flightInfo.First().Flightno1;
                                        //string flightNo= flightInfo.First().Flightno2;

                                        saveInfo = await FlightSaveInfoAsync(
                                            csrfToken: csrfToken,
                                            calendarStartDate: DateTime.Now.ToString("yyyy/MM/dd"),
                                            departureDate: date.ToString("yyyy/MM/dd"),
                                            departureTime: date.ToString("HH:mm"),
                                            airlinesNo: flight.Flightno1,
                                            flightNo: flight.Flightno2,
                                            otherflightno: "",
                                            cancellationToken: cancellationToken,
                                            connectingFlight: "no");
                                        if(saveInfo?.AllowCheckout == true)
                                        {
                                            productMonitor.Setup = OrderSetup.FlightInfoSaved;
                                            productMonitor.UpdateTime = DateTime.Now;
                                        }
                                    }
                                    //判断是否可以下单
                                    if (saveInfo?.AllowCheckout == true || productMonitor.Setup == OrderSetup.FlightInfoSaved)
                                    {
                                        await productMonitorRepository.UpdateAsync(productMonitor);
                                        //调用下单逻辑,跳转结算界面
                                        await driver.Navigate().GoToUrlAsync("https://www.kixdutyfree.jp/cn/checkout");
                                        // 等待页面加载完成
                                        WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));
                                        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                                        var dwfrm_billing = driver.FindElement(By.Id("dwfrm_billing"));
                                        // 查找csrf_token
                                        var billingCsrfTokenElement = dwfrm_billing.FindElement(By.Name("csrf_token"));
                                        // 获取元素的 value 属性值
                                        string billingCsrfToken = billingCsrfTokenElement.GetDomAttribute("value");
                                        var submitPayment = await SubmitPaymentAsync(Account.Email, billingCsrfToken, cancellationToken);
                                        if (submitPayment?.Error == false || productMonitor.Setup == OrderSetup.PaymentSubmitted)
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
                                                //订单入库
                                                var order = new OrdersEntity()
                                                {
                                                    OrderId = productMonitor.OrderId,
                                                    ProductId = productMonitor.ProductId,
                                                    Account = Account.Email,
                                                    AirlineName = Account.AirlineName,
                                                    FlightDate = Account.Date,
                                                    FlightNo = Account.FlightNo,
                                                    CreateTime = productMonitor.UpdateTime
                                                };
                                                await orderService.AddOrderAsync(order);
                                                //信息导出到表格
                                                await excelProcess.OrderExportAsync(new Models.Excel.OrderExcel()
                                                {
                                                    OrderId = productMonitor.OrderId,
                                                    ProductId = productMonitor.ProductId,
                                                    ProductName = product.ProductName ?? "",
                                                    Account = Account.Email,
                                                    AirlineName = Account.AirlineName,
                                                    FlightDate = Account.Date,
                                                    FlightNo = Account.FlightNo,
                                                    CreateTime = productMonitor.UpdateTime
                                                });
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
                            });
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
            catch (WebDriverTimeoutException ex)
            {
                ErrorCount++;
                logger.BaseErrorLog("PlaceOrderAsync.WebDriverTimeoutException", ex);
                await RelodAsync();
            }
            catch (Exception ex)
            {
                ErrorCount++;
                logger.LogError("PlaceOrderAsync.错误次数: {ErrorCount}", ErrorCount);
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
            // 关闭并释放旧的 ChromeDriver 实例
            if (_driver != null)
            {
                try
                {
                    IsLoading = true;

                    _driver?.Quit();
                    logger.LogInformation("ReinitializeDriverAsync. 旧的 ChromeDriver 实例已关闭并释放。");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ReinitializeDriverAsync. 关闭旧的 ChromeDriver 实例时出错。");
                }
                finally
                {
                    IsLoading = false;
                }
            }
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
            HttpRequestMessage request = new(HttpMethod.Get, Cart.ProductVariation + $"?pid={productId}&quantity={quantity}");
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", CookieHeader);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("ProductVariationAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<ProductVariationResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
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
            request.Headers.Add("Cookie", await SyncCookies());
            // 使用 FormUrlEncodedContent 设置请求内容
            request.Content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<CartAddProductResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
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
            request.Headers.Add("Cookie", await SyncCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<CartUpdateQuantityResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
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
            request.Headers.Add("Cookie", await SyncCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<FlightGetInfoResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
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
            request.Headers.Add("Cookie", await SyncCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogDebug("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<FlightSaveInfoResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }
            else
            {
                ErrorCount++;
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
            request.Headers.Add("Cookie", await SyncCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("SubmitPaymentAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<SubmitPaymentResponse>(content);
            }
            else
            {
                ErrorCount++;
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
            request.Headers.Add("Cookie", await SyncCookies());
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("PlaceOrderAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<PlaceOrderResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }
            else
            {
                ErrorCount++;
            }
            return data;
        }

        #endregion
    }
}
