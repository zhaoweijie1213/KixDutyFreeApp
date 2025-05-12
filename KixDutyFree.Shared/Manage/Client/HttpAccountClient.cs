using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using KixDutyFree.App.Manage;
using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Models.Response;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.EventHandler;
using KixDutyFree.Shared.Manage.Client.Interface;
using KixDutyFree.Shared.Models;
using KixDutyFree.Shared.Models.Entity;
using KixDutyFree.Shared.Services;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.DevTools;
using QYQ.Base.Common.Extension;
using QYQ.Base.Common.IOCExtensions;


namespace KixDutyFree.Shared.Manage.Client
{
    /// <summary>
    /// 专用于 HTML+HTTP 的账号客户端，实现 IAccountClient。
    /// 使用 IHttpClientFactory + HtmlAgilityPack 完成登录及后续操作。
    /// 与原 Selenium 驱动的 AccountClient 并存。
    /// </summary>
    public class HttpAccountClient(ILogger<HttpAccountClient> logger,IHttpClientFactory httpClientFactory, KixDutyFreeApiService kixDutyFreeApiService, ProductService productService
        , CacheManage cacheManage, ProductInfoRepository productInfoRepository, ProductMonitorService productMonitorService,IMemoryCache memoryCache, OrderService orderService
        , ExcelProcess excelProcess, IMediator mediator) 
        : IAccountClient, ITransientDependency
    {
        private HttpClient _httpClient = null!;

        private readonly CookieContainer _cookieJar = new();


        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        /// <summary>
        /// 是否成功登录
        /// </summary>
        public bool IsLoginSuccess { get; set; } = false;

        /// <summary>
        /// 是否正在下单
        /// </summary>
        public bool IsPlaceOrdering { get; set; }

        /// <summary>
        /// 下单锁，保证同一时刻只有一个 PlaceOrderAsync 在跑
        /// </summary>
        private readonly SemaphoreSlim _placeOrderLock = new(1, 1);

        /// <summary>
        /// 账号信息
        /// </summary>
        public AccountInfo Account { get; set; } = new();

        /// <summary>
        /// 错误次数
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 检测登录状态
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckLoginStatusAsync()
        {
            // 1. 获取账户页 HTML
            var html = await _httpClient.GetStringAsync("/cn/account");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 2. 找到 menu-container-loggedin 区块
            var menuDiv = doc.DocumentNode
                .SelectSingleNode("//div[contains(@class,'menu-container-loggedin')]");

            // 3. 检测是否含有“登录/注册”
            bool isLoggedOut = menuDiv != null
                && menuDiv.InnerText.Contains("登录/注册", StringComparison.Ordinal);

            if (!isLoggedOut)
            {
                // 没看到“登录/注册”，
                logger.LogInformation("CheckLoginStatusAsync:账号已登录");
                return true;
            }

            IsLoginSuccess = false;

            // 4. 否则重新登录一次
            logger.LogInformation("CheckLoginStatusAsync:登录过期，尝试重新登录……");

            await LoginAsync(Account);  // 复用 LoginAsync 登录

            if (!IsLoginSuccess)
            {
                logger.LogError("CheckLoginStatusAsync:重新登录失败！");
            }
            return IsLoginSuccess;
        }

        /// <inheritdoc />
        public async Task<bool> InitAsync(AccountInfo? account)
        {
            try
            {
                if (_httpClient == null)
                {
                    _httpClient = httpClientFactory.CreateClient(account?.Email ?? "DefaultClient");
                    _httpClient.BaseAddress = new Uri("https://www.kixdutyfree.jp");
                    _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                    //var handler = new HttpClientHandler
                    //{
                    //    CookieContainer = _cookieJar,
                    //    UseCookies = true,
                    //    AllowAutoRedirect = true
                    //};
                    //_httpClient = new HttpClient(handler)
                    //{
                    //    BaseAddress = new Uri("https://www.kixdutyfree.jp")
                    //};
                    //_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                }

                if (account != null)
                {
                    await LoginAsync(account);
                }
                else
                {
                    await _httpClient.GetAsync("/cn");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "HttpAccountClient.InitAsync 异常");
            }

            return IsLoginSuccess;
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoginAsync(AccountInfo account)
        {
            // 1. 获取登录页面并解析 HTML
            var html = await _httpClient.GetStringAsync("/cn/login/");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            // 精确定位登录表单内的 csrf_token
            var tokenNode = doc.DocumentNode.SelectSingleNode("//form[@name='login-form']//input[@name='csrf_token']");
            var csrf = tokenNode?.GetAttributeValue("value", string.Empty);
            if (string.IsNullOrEmpty(csrf))
            {
                logger.LogError("HttpAccountClient: 无法获取登录表单的 csrf_token");
                return false;
            }

            var res = await kixDutyFreeApiService.LoginAsync(_httpClient, account.Email, account.Password, csrf);
            if (res?.Success == true)
            {
                html = await _httpClient.GetStringAsync("/cn/account");
                // 一定要重新加载新 HTML
                var accountDoc = new HtmlDocument();
                accountDoc.LoadHtml(html);
                var nameElement = accountDoc.DocumentNode.SelectSingleNode("//span[@class='name']");
                if (nameElement != null)
                {
                    logger.LogInformation("{Email}已登录: {name}", account.Email, nameElement.InnerText.Trim());
                }
                else
                {
                    logger.LogWarning("InitAsync: 登录后未找到用户名");
                }
                IsLoginSuccess = true;
                Account = account;
                //发送登录成功事件
                await mediator.Publish(new UserLoginStatusChangedNotification(account.Email, IsLoginSuccess));
            }

            return IsLoginSuccess;
        }

        /// <summary>
        /// 退出
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task QuitAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 重置
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<bool> RelodAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// 检查商品是否有货
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> CheckProductAvailabilityAsync(ProductInfoEntity product, CancellationToken cancellationToken)
        {
            bool isAvailable = false;
            try
            {
                // 检查商品是否有货
                var res = await kixDutyFreeApiService.ProductVariationAsync(_httpClient, product.Id, 1, cancellationToken);
                //isAvailable = res?.Product?.Availability?.Available ?? false;
                if (res != null && string.IsNullOrEmpty(product.Name))
                {
                    product.Name = res.Product?.ProductName ?? "";
                    product.Image = res.Product?.Images?.Large?.FirstOrDefault()?.AbsUrl ?? "";
                    product.UpdateTime = DateTime.Now;
                    await productService.ProductAddOrUpdateAync(product);
                }
                //商品可用状态判断
                if (res?.Product?.Available == false || res?.Product?.Availability?.Available == false || res?.Product?.ReadyToOrder == false || res?.Product?.Online == false)
                {
                    isAvailable = false;
                    logger.LogInformation("商品:{Name}不可用,最大定购数{MaxOrderQuantity}", product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                }
                else
                {
                    isAvailable = true;
                    logger.LogInformation("商品:{Name}可用,最大定购数{MaxOrderQuantity}", product.Name, res?.Product?.Availability?.MaxOrderQuantity);
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
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<(bool, ProductVariationResponse?)> FullCheckProductAvailabilityAsync(ProductInfoEntity product, CancellationToken cancellationToken)
        {
            bool isAvailable = false;
            try
            {
                if (IsLoginSuccess && !string.IsNullOrEmpty(product?.Id))
                {
                    //查询最新订单,判断当前流程信息
                    var productMonitor = await cacheManage.GetProductMonitorAsync(Account.Email, product.Id);
                    if (productMonitor != null && productMonitor.Setup == OrderSetup.OrderPlaced && !string.IsNullOrEmpty(productMonitor.OrderId))
                    {
                        logger.LogInformation("FullCheckProductAvailabilityAsync:{Account}\t{Name}已有订单,订单状态{Setup}", Account.Email, product.Name, productMonitor.Setup.ToString());
                        return (isAvailable, null);
                    }
                    else
                    {
                        // 检查商品是否有货（需要根据页面元素进行判断）
                        var res = await kixDutyFreeApiService.ProductVariationAsync(_httpClient, product.Id, 1, cancellationToken);
                        if (res != null && string.IsNullOrEmpty(product.Name))
                        {
                            product.Name = res.Product?.ProductName ?? "";
                            product.Image = res.Product?.Images?.Large?.FirstOrDefault()?.AbsUrl ?? "";
                            await productInfoRepository.UpdateAsync(product);
                        }
                        isAvailable = res?.Product?.Availability?.Available ?? false;
                        //if (res?.Product?.Available == false || res?.Product?.Availability?.Available == false || res?.Product?.ReadyToOrder == false || res?.Product?.Online == false)
                        //{

                        //}
                        //else
                        //{

                        //}
                        //可用或者数量有限
                        if (isAvailable || res?.Product?.Availability?.Status == "QUANTITY_LIMITED")
                        {
                            //var productConfig = await cacheManage.GetProductsAsync();
                            logger.LogInformation("FullCheckProductAvailabilityAsync.账号:{Account}\t商品:{Name}可用,最大定购数{MaxOrderQuantity}", Account.Email, product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                            // 添加购物车并下单
                            await AddCartAsync(res!.Product!, product.Quantity, cancellationToken);
                        }
                        else
                        {
                            logger.LogInformation("FullCheckProductAvailabilityAsync.账号:{Account}\t商品:{Name}不可用,最大定购数{MaxOrderQuantity}", Account.Email, product.Name, res?.Product?.Availability?.MaxOrderQuantity);
                            //ProductMonitorEntity prodicrMonitor = await productMonitorRepository.QueryAsync(Account.Email, product.Id);
                            if (productMonitor != null)
                            {
                                productMonitor.Setup = OrderSetup.None;
                                productMonitor.UpdateTime = DateTime.Now;
                                await productMonitorService.UpdateProductMonitorAsync(productMonitor);
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
                                await productMonitorService.InsertProductMonitorAsync(productMonitor);
                            }
                            return (isAvailable, res);
                        }
                    }

                }
                else
                {
                    logger.LogWarning("FullCheckProductAvailabilityAsync:{Account}未登录或者商品Id为null", Account.Email);
                }
            }
            catch (Exception ex)
            {
                ErrorCount++;
                logger.LogError("FullCheckProductAvailabilityAsync.错误次数: {ErrorCount}", ErrorCount);
                // 记录异常日志
                logger.BaseErrorLog($"FullCheckProductAvailabilityAsync.检查商品时出错", ex);
            }
            return (isAvailable, null);
        }

        /// <summary>
        /// 添加到购物车
        /// </summary>
        /// <param name="product"></param>
        /// <param name="quantity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddCartAsync(Product product, int quantity, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
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
                        productMonitor = await productMonitorService.InsertProductMonitorAsync(productMonitor);
                    }
                    //查看购物车是否有商品

                    // 1. 请求购物车页面 HTML
                    var html = await _httpClient.GetStringAsync("/cn/cart", CancellationToken.None);
                    var cartDoc = new HtmlDocument();
                    cartDoc.LoadHtml(html);
                    // 2. 找到所有商品卡片
                    var cardNodes = cartDoc.DocumentNode.SelectNodes("//div[contains(@class, 'card') and contains(@class, 'product-info')]");
                    List<CartProductModel> cartProducts = [];
                    if (cardNodes?.Count > 0)
                    {
                        foreach (var card in cardNodes)
                        {
                            // 3.a 读取 pid/uuid（从移除按钮上的 data- 属性）
                            var removeBtn = card.SelectSingleNode(
                                ".//button[contains(@class,'krs-cart-remove-product')]")!;
                            string pid = removeBtn.GetAttributeValue("data-pid", "");
                            string uuid = removeBtn.GetAttributeValue("data-uuid", "");
                            cartProducts.Add(new CartProductModel { Pid = pid, Uuid = uuid });

                            // 3.b 读取当前数量
                            var qtyInput = card.SelectSingleNode(".//input[@name='product-quantity']")!;
                            string rawQty = qtyInput.GetAttributeValue("value", "0");
                            int quantityValue = int.TryParse(rawQty, out var qv) ? qv : 0;

                            // 3.c 看是否有“售罄”提示
                            var restrictNode = card.SelectSingleNode(".//div[contains(@class,'lineitem-availability-restriction')]");
                            string restriction = restrictNode?.InnerText.Trim() ?? "";

                            // 4. 如果数量 <=0 或者出现售罄关键词，就调用“移除”接口
                            if (quantityValue <= 0 || restriction.Contains("该商品在您预订完成前已售罄"))
                            {
                                //移除该商品
                                var removeRes = await kixDutyFreeApiService.RemoveProductLineItemAsync(_httpClient, pid, uuid, cancellationToken);
     
                                logger.LogInformation("AddCartAsync.移除不可用商品:{pid}", removeRes?.UpdatedPid);
                            }
                            if (pid == product.Id && quantityValue != quantity)
                            {
                                //修正购物车数量
                                var updateQuantity = await kixDutyFreeApiService.CartUpdateQuantityAsync(_httpClient, pid, quantity, uuid, cancellationToken);
                                logger.LogInformation("AddCartAsync.修正购物车数量:账号 {Account},Id {UpdatedPid},数量 {UpdatedQty}", Account.Email, updateQuantity?.UpdatedPid, updateQuantity?.UpdatedQty);
                            }
                            if (productMonitor != null && productMonitor.Setup != OrderSetup.Completed)
                            {
                                productMonitor.Setup = OrderSetup.AddedToCart;
                                productMonitor.UpdateTime = DateTime.Now;
                                await productMonitorService.UpdateProductMonitorAsync(productMonitor);
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
                                productMonitor = await productMonitorService.InsertProductMonitorAsync(productMonitor);
                            }
                        }
                    }
                    else if (cartProducts == null || cartProducts.Any(x => x.Pid == product.Id) == false)
                    {
                        //添加到购物车
                        res = await kixDutyFreeApiService.CartAddProductAsync(_httpClient, product.Id, quantity, cancellationToken);
                        if (res?.Error == false)
                        {
                            if (productMonitor != null && productMonitor.Setup != OrderSetup.Completed)
                            {
                                productMonitor.Setup = OrderSetup.AddedToCart;
                                productMonitor.UpdateTime = DateTime.Now;
                                await productMonitorService.UpdateProductMonitorAsync(productMonitor);
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
                                productMonitor = await productMonitorService.InsertProductMonitorAsync(productMonitor);
                            }
                            logger.LogInformation("PlaceOrderAsync.商品 {ProductName} 加入购物车成功！", product.ProductName);
                        }
                        else
                        {
                            logger.LogWarning("PlaceOrderAsync.商品 {ProductName} 加入购物车失败！{Message}", product.ProductName, res?.Message);
                        }
                    }
                    //下单
                    await PlaceOrderAsync(productMonitor, product?.ProductName ?? "", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ErrorCount++;
                logger.LogError("AddCartAsync.错误次数: {ErrorCount}", ErrorCount);
                // 记录异常日志
                logger.BaseErrorLog("AddCartAsync", ex);
            }
        }


        /// <summary>
        /// 下单
        /// </summary>
        /// <param name="productMonitor"></param>
        /// <param name="productName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task PlaceOrderAsync(ProductMonitorEntity productMonitor, string productName, CancellationToken cancellationToken)
        {
            try
            {
                // 等待锁：如果已有线程在下单，就在这里排队
                await _placeOrderLock.WaitAsync(cancellationToken);
                IsPlaceOrdering = true;

                if (cancellationToken.IsCancellationRequested) return;
                var date = Account.Date;
                //获取航班信息
                var flightInfo = await GetFlightDataAsync(Account.Date, cancellationToken);

                if (flightInfo == null)
                {
                    logger.LogWarning("PlaceOrderAsync.未获取到 {date} 航班信息", Account.Date.ToString("D"));
                    return;
                }
                //保存航班信息
                FlightSaveInfoResponse? saveInfo = null;
                if (productMonitor.Setup == OrderSetup.AddedToCart)
                {
                    // 查找csrf_token

                    //跳转到购物车界面
                    var html = await _httpClient.GetStringAsync("/cn/cart");
                    var cartDoc = new HtmlDocument();
                    cartDoc.LoadHtml(html);
                    // 精确定位登录表单内 flight-form 的 csrf_token  
                    var csrfTokenElement = cartDoc.DocumentNode.SelectSingleNode("//form[@name='flight-form']//input[@name='csrf_token']");
                    // 获取元素的 value 属性值
                    var csrfToken = csrfTokenElement?.GetAttributeValue("value", "");
                    if (string.IsNullOrEmpty(csrfToken))
                    {
                        logger.LogWarning("PlaceOrderAsync.未获取到购物车界面 csrfToken");
                        return;
                    }

                    var flight = flightInfo.FirstOrDefault(i => i.AirlineName.Contains(Account.AirlineName) && i.Flightno2 == Account.FlightNo);
                    string otherflightno = "";
                    if (flight == null)
                    {
                        flight = flightInfo.First(i => i.AirlineName.Contains(Account.AirlineName));
                        flight.Flightno2 = "other";
                        otherflightno = Account.FlightNo;
                    }
                    saveInfo = await kixDutyFreeApiService.FlightSaveInfoAsync(_httpClient,
                        csrfToken: csrfToken,
                        calendarStartDate: DateTime.Now.ToString("yyyy/MM/dd"),
                        departureDate: date.ToString("yyyy/MM/dd"),
                        departureTime: date.ToString("HH:mm"),
                        airlinesNo: flight.Flightno1,
                        flightNo: flight.Flightno2,
                        otherflightno: "",
                        cancellationToken: cancellationToken,
                        connectingFlight: "no");
                    if (saveInfo?.AllowCheckout == true)
                    {
                        productMonitor.Setup = OrderSetup.FlightInfoSaved;
                        productMonitor.UpdateTime = DateTime.Now;
                    }
                }
                //判断是否可以下单
                if (saveInfo?.AllowCheckout == true || productMonitor.Setup == OrderSetup.FlightInfoSaved)
                {
                    await productMonitorService.UpdateProductMonitorAsync(productMonitor);
                    //调用下单逻辑,跳转结算界面
                    // GET checkout 以拿到新的 csrf_token
                    var html = await _httpClient.GetStringAsync("/cn/checkout", cancellationToken);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // 查找csrf_token
                    var billingCsrfTokenElement = doc.DocumentNode.SelectSingleNode("//form[@id='dwfrm_billing']//input[@name='csrf_token']") ?? throw new InvalidOperationException("无法解析支付页 csrf_token");
                    // 获取元素的 value 属性值
                    string billingCsrfToken = billingCsrfTokenElement.GetAttributeValue("value", "");
                    if (string.IsNullOrEmpty(billingCsrfToken))
                        throw new InvalidOperationException("无法解析支付页 csrf_token");

                    // 调用 API 提交到店支付
                    var submitPayment = await kixDutyFreeApiService.SubmitPaymentAsync(_httpClient, Account.Email, billingCsrfToken, cancellationToken);
                    if (submitPayment?.Error == false || productMonitor.Setup == OrderSetup.PaymentSubmitted)
                    {
                        productMonitor.Setup = OrderSetup.PaymentSubmitted;
                        productMonitor.UpdateTime = DateTime.Now;
                        await productMonitorService.UpdateProductMonitorAsync(productMonitor);
                        //最终确认下单
                        var placeOrder = await kixDutyFreeApiService.PlaceOrderAsync(_httpClient, cancellationToken);
                        if (placeOrder?.Error == false)
                        {
                            productMonitor.Setup = OrderSetup.OrderPlaced;
                            productMonitor.UpdateTime = DateTime.Now;
                            productMonitor.OrderId = placeOrder.OrderID;
                            productMonitor.OrderToken = placeOrder.OrderToken;
                            await productMonitorService.UpdateProductMonitorAsync(productMonitor);
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
                            await excelProcess.OrderExportAsync(new App.Models.Excel.OrderExcel()
                            {
                                OrderId = productMonitor.OrderId,
                                ProductId = productMonitor.ProductId,
                                ProductName = productName,
                                Account = Account.Email,
                                AirlineName = Account.AirlineName,
                                FlightDate = Account.Date,
                                FlightNo = Account.FlightNo,
                                CreateTime = productMonitor.UpdateTime
                            });
                        }
                        else if (placeOrder?.Error == true && placeOrder.ErrorMessage?.Contains("您购物车中的一件或多件商品超过了可购买范围，请减少购买数量或移除该商品") == true)
                        {
                            logger.LogWarning("PlaceOrderAsync.下单失败:{Message}", placeOrder.ErrorMessage);
                            //移除不可用的商品

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
            catch (Exception ex)
            {
                ErrorCount++;
                logger.LogError("PlaceOrderAsync.错误次数: {ErrorCount}", ErrorCount);
                // 记录异常日志
                logger.BaseErrorLog("PlaceOrderAsync", ex);
            }
            finally
            {
                _placeOrderLock.Release();
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
                var flightInfo = await kixDutyFreeApiService.FlightGetInfoAsync(_httpClient, date, cancellationToken);
                data = flightInfo?.FlightData;
                if (data != null)
                {
                    memoryCache.Set(key, data, TimeSpan.FromHours(1));
                }
            }
            return data;
        }

    }
}
