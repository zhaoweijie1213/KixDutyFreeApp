using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Manage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using QYQ.Base.Common.Extension;
using QYQ.Base.Common.IOCExtensions;
using System.Text.RegularExpressions;

namespace KixDutyFree.Shared.Services
{

    public class SeleniumService(ILogger<SeleniumService> logger, ProductInfoRepository productInfoRepository, CacheManage cacheManage, ClientMonitor clientMonitor) : ITransientDependency
    {

        public const string Home = "https://www.kixdutyfree.jp/cn";

        public const string LoginUrl = "https://www.kixdutyfree.jp/cn/login";

        public const string AccountUrl = "https://www.kixdutyfree.jp/cn/account";

        /// <summary>
        /// 创建实例
        /// </summary>
        public async Task<(ChromeDriver, bool)> CreateInstancesAsync(AccountInfo? account, bool headless = false)
        {
            ChromeDriver? driver = null;
            bool isLogin = false;
            bool status = false;
            while (!status)
            {
                try
                {
                    // 在后台线程中实例化ChromeDriver
                    driver = await Task.Run(() =>
                    {
                        ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                        service.HideCommandPromptWindow = true;
                        service.SuppressInitialDiagnosticInformation = true;
                        service.InitializationTimeout = TimeSpan.FromMinutes(2);

                        var options = new ChromeOptions();
                        if (headless)
                        {
                            options.AddArgument("--headless");
                            options.AddArgument("--disable-gpu");
                            //options.AddArgument("--no-sandbox");
                            options.AddArgument("--disable-dev-shm-usage");
                        }

                        return new ChromeDriver(service, options,TimeSpan.FromMinutes(3));
                    });

                    // 设置页面加载超时时间为 120 秒
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(120);
                    //driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(120);
                    await driver.Navigate().GoToUrlAsync("https://www.kixdutyfree.jp/cn");
                    //获取标题
                    var title = driver.Title;
                    logger.LogInformation("Title:{title}", title);

                    await Confirm(driver);
                    //登录
                    if (account != null)
                    {
                        isLogin = await Login(account, driver);
                        status = isLogin;
                    }
                    else
                    {
                        status = true;
                    }
            
                }
                catch (Exception e)
                {
                    status = false;
                    isLogin = false;
                    if (driver != null)
                    {
                        driver.Quit();
                        driver.Dispose();
                    }
                    logger.BaseErrorLog("CreateInstancesAsync", e);
                    clientMonitor.AddError();
                }
            }
            return new(driver!, isLogin);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Login(AccountInfo account, ChromeDriver driver)
        {
            bool isLogin = false;
            //检测登录按钮
            try
            {
                await driver.Navigate().GoToUrlAsync("https://www.kixdutyfree.jp/cn/login/");
                //输入账号密码
                var email = driver.FindElement(By.Id("login-form-email"));
                email.SendKeys(account.Email);
                var pwd = driver.FindElement(By.Id("login-form-password"));
                pwd.SendKeys(account.Password);
                //点击登录按钮
                var loginSubmit = driver.FindElement(By.XPath("//div[contains(@class, 'login-submit-button') and contains(@class, 'pt-1')]//button[contains(@class, 'btn') and contains(@class, 'btn-block') and contains(@class, 'btn-primary') and contains(@class, 'btn-login')]"));
                loginSubmit.Click();
                logger.LogInformation("Login.登录:{email}", email);
                isLogin = await IsLogin(driver);
            }
            catch (NoSuchElementException e)
            {
                logger.LogWarning("Login:{Message}/r/n{StackTrace}", e.Message, e.StackTrace);
            }
            return isLogin;
        }

        /// <summary>
        /// 检测是否登录
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        public async Task<bool> IsLogin(ChromeDriver driver)
        {
            bool isLogin = false;

            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                if (driver.Url.Contains(AccountUrl))
                {
                    await driver.Navigate().GoToUrlAsync(Home);
                }
                else
                {
                    await driver.Navigate().GoToUrlAsync(AccountUrl);
                }
                // 尝试查找表示已登录状态的元素
                var accountInfo = wait.Until(driver =>
                {
                    try
                    {
                        // 查找登录后才会显示的元素，例如账户信息
                        var element = driver.FindElement(By.Id("myaccount"));
                        return element.Displayed ? element : null;
                    }
                    catch (NoSuchElementException)
                    {
                        return null;
                    }
                });

                if (accountInfo != null)
                {
                    isLogin = true;
                    var nameElement = accountInfo.FindElement(By.ClassName("name"));
                    logger.LogInformation("已登录: {name}", nameElement.Text);
                }
            }
            catch (WebDriverTimeoutException e)
            {
                isLogin = false;
                logger.BaseErrorLog("IsLogin.未检测到登录状态，超时未找到指定元素,请检查网络是否正常", e);
                clientMonitor.AddLoginError();
            }

            return isLogin;
        }

        /// <summary>
        /// 获取商品信息
        /// </summary>
        /// <param name="address"></param>
        /// <param name="driver"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public async Task<ProductInfoEntity?> GetProductIdAsync(string address, ChromeDriver driver, int quantity)
        {
            var product = await productInfoRepository.FindByAddressAsync(address);
            if (product == null)
            {
                try
                {
                    // 导航到商品页面
                    await driver.Navigate().GoToUrlAsync(address);
                    var productDetail = driver.FindElement(By.ClassName("product-detail"));
                    // 获取 data-pid 属性的值 得到商品id
                    string productId = productDetail.GetDomAttribute("data-pid");
                    if (productId != null)
                    {

                        product = new ProductInfoEntity()
                        {
                            Id = productId,
                            Address = address,
                            Quantity = quantity,
                            CreateTime = DateTime.Now,
                            UpdateTime = DateTime.Now
                        };
                        product = await productInfoRepository.InsertAsync(product);
                        //设置缓存
                        cacheManage.SetProductInfoByAddress(address, product);
                    }
                }
                catch (NoSuchElementException e)
                {
                    logger.BaseErrorLog("GetProductIdAsync", e);
                }
            }

            return product;
        }


        /// <summary>
        /// 下单
        /// </summary>
        /// <returns></returns>
        public async Task OrderAsync(ChromeDriver driver, string productAddress)
        {
            await driver.Navigate().GoToUrlAsync(productAddress);
            await Confirm(driver);
            // 显式等待，等待特定元素加载完成，最多等待 10 秒
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(30));
            bool targetElement = wait.Until(x => x.Url == productAddress);
            if (targetElement)
            {
                bool status = await CheckInventoryAsync(driver);
                if (status)
                {
                    int orderNum = 0;
                    try
                    {
                        try
                        {
                            // 使用 XPath 查找包含类名 "title warning-header" 并包含文本 "已达上限" 的 <div> 元素
                            IWebElement warningDiv = driver.FindElement(By.XPath("//div[contains(@class, 'title') and contains(@class, 'warning-header')]"));
                            if (warningDiv.Text == "已达上限")
                            {
                                return;
                            }
                        }
                        catch (NoSuchElementException e)
                        {
                            logger.LogWarning("OrderAsync:{Message}", e.Message);
                        }
                        //可购买最大数量
                        var availabilityText = driver.FindElement(By.ClassName("availability-text")).Text;
                        orderNum = ParseMaxOrder(availabilityText);
                    }
                    catch (NoSuchElementException e)
                    {
                        logger.LogWarning("OrderAsync:{Message}", e.Message);
                    }
                    if (orderNum <= 0)
                    {
                        orderNum = 20;
                    }
                    //输入数量
                    var productQuantity = driver.FindElement(By.Name("product-quantity"));
                    // 清空输入框的当前值
                    productQuantity.Clear();
                    productQuantity.SendKeys(orderNum.ToString());
                    //添加到购物车
                    var addToCart = driver.FindElement(By.ClassName("add-to-cart"));
                    addToCart.Click();
                    //去购物车结算
                    await ToCartAsync(driver);
                }
            }
        }

        /// <summary>
        /// 去购物车结算
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        public async Task ToCartAsync(ChromeDriver driver)
        {
            //去购物车结算
            await driver.Navigate().GoToUrlAsync("https://www.kixdutyfree.jp/cn/cart/");
            try
            {
                // 等待页面加载完成
                WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));
                wait.Until(d =>
                {
                    return ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete");
                });

                // 页面加载完成，添加日志
                logger.LogInformation("ToCartAsync:购物车页面已成功加载");
            }
            catch (WebDriverTimeoutException)
            {
                // 处理页面加载超时的情况
                logger.LogError("ToCartAsync:购物车页面加载超时");
                throw;
            }
            #region 交互

            //DateTime datetime = DateTime.Now.AddDays(15);
            ////填写日期和时间
            //var date = driver.FindElement(By.Id("departureDate"));
            //date.SendKeys(datetime.ToString("yyyy/MM/dd"));
            //var timeInput = driver.FindElement(By.Id("departureTime"));
            //// 使用 JavaScript 来设置输入框的值
            //string script = $"arguments[0].removeAttribute('readonly'); arguments[0].value = '{datetime:HH:ss}';";
            //((IJavaScriptExecutor)driver).ExecuteScript(script, timeInput);
            //logger.LogInformation("ToCartAsync.时间输入框的值已被设置为{datetime}", datetime.ToString("yyyy/MM/dd HH:ss"));
            ////选择航空公司
            //// 找到下拉框元素
            //IWebElement dropdownElement = driver.FindElement(By.Id("airlines"));
            //// 创建 SelectElement 对象
            //SelectElement select = new(dropdownElement);
            //foreach (var option in select.Options)
            //{
            //    if (option.Text.Contains("四川航空"))
            //    {
            //        select.SelectByText(option.Text);
            //        logger.LogInformation("ToCartAsync.已成功选择航空公司：{option}", option.Text);
            //        break;
            //    }
            //}
            ////选择航班号
            //var flightno = driver.FindElement(By.Id("flightno"));
            //// 创建 SelectElement 对象
            //SelectElement flightnoSelect = new(flightno);
            //flightnoSelect.SelectByIndex(1);  // 选择第1个选项
            ////是否转机
            ////try
            ////{
            ////    var radioYesButton = driver.FindElement(By.Id("connecting-flight-yes"));
            ////    var radioNoButton = driver.FindElement(By.Id("connecting-flight-no"));
            ////    radioYesButton.Click();
            ////    radioNoButton.Click();
            ////}
            ////catch (NoSuchElementException e)
            ////{
            ////    logger.LogWarning("ToCartAsync:{Message}", e.Message);
            ////}


            ////我已了解
            //var agreeCheckBox = driver.FindElement(By.Id("agree-products"));
            //// 检查复选框的值是否为 "yes"
            //string value = agreeCheckBox.GetAttribute("value");
            //string classAttribute = agreeCheckBox.GetAttribute("class");
            //if (value != "yes"||(classAttribute != null && classAttribute.Contains("is-invalid")))
            //{
            //    agreeCheckBox.Click();
            //}
            ////下一步
            //// 找到 name 为 "flight-form" 的表单
            //IWebElement formElement = driver.FindElement(By.Name("flight-form"));
            //// 在表单上下文中查找提交按钮
            //IWebElement submitButton = formElement.FindElement(By.CssSelector("button[type='submit']"));
            //// 点击提交按钮
            //submitButton.Click();
            //logger.LogInformation("ToCartAsync:提交按钮已被成功点击，等待跳转到结算页面...");
            //// 显式等待，直到 URL 变为目标结算页面的 URL
            //WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
            //bool urlIsCorrect = wait.Until(d =>
            //    d.Url.Contains("https://www.kixdutyfree.jp/cn/checkout"));
            //if (urlIsCorrect)
            //{
            //    logger.LogInformation("ToCartAsync:成功跳转到结算页面！");
            //    // 找到包含“到店支付”的 div 元素
            //    IWebElement instorePaymentDiv = driver.FindElement(By.XPath("//div[contains(@class, 'tab-link') and contains(@class, 'instore-payment-select')]"));
            //    // 点击该元素以选择“到店支付”
            //    instorePaymentDiv.Click();
            //    logger.LogInformation("ToCartAsync:“到店支付”选项已成功选中");
            //    //下一步
            //    try
            //    {
            //        // 使用 XPath 找到 card payment-form 下的 card-body，然后找到 submit-payment 按钮
            //        IWebElement nextButton = driver.FindElement(By.XPath("//div[contains(@class, 'card payment-form')]//div[contains(@class, 'card-body')]//button[contains(@class, 'submit-payment')]"));
            //        nextButton.Click();
            //    }
            //    catch (NoSuchElementException e)
            //    {
            //        logger.LogWarning("OrderAsync:{Message}", e.Message);
            //    }


            //}
            //else
            //{
            //    logger.LogInformation("ToCartAsync:未成功跳转到结算页面！");
            //}

            #endregion

        }

        /// <summary>
        /// 信息确认
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        public Task Confirm(ChromeDriver driver)
        {
            //查询是否有年龄确认dialog对话框
            try
            {
                var ageConfirmDialog = driver.FindElement(By.ClassName("modal-dialog"));
                var ageModalHeader = ageConfirmDialog.FindElement(By.ClassName("modal-header"));
                if (ageModalHeader.FindElement(By.TagName("span")).Text == "年龄认证")
                {
                    //年龄确认
                    var ageButton = ageConfirmDialog.FindElement(By.Id("age_yes"));
                    ageButton.Click();
                }
            }
            catch (NoSuchElementException e)
            {
                logger.BaseErrorLog("Confirm", e);
            }
            ////同意网站cookie
            //try
            //{
            //    var trustCookie = driver.FindElement(By.Id("onetrust-accept-btn-handler"));
            //    trustCookie.Click();
            //}
            //catch (NoSuchElementException e)
            //{
            //    logger.LogWarning("TaskStartAsync:{Message}", e.Message);
            //}

            return Task.CompletedTask;
        }

        /// <summary>
        /// 检测库存
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        public Task<bool> CheckInventoryAsync(ChromeDriver driver)
        {
            bool status = true;
            string productName = "";
            try
            {
                productName = driver.FindElement(By.ClassName("product-name")).Text;
            }
            catch (NoSuchElementException e)
            {
                logger.LogWarning("MonitorPagesAsync:{Message}", e.Message);
            }

            try
            {
                //查询是否有库存
                var text = driver.FindElement(By.XPath("//div[contains(@class, 'title') and contains(@class, 'availability-header')]"));
                if (text.Text == "暂无库存")
                {
                    status = false;
                    logger.LogInformation("MonitorPagesAsync:{productName}{Text}", productName, text.Text);
                }
                else
                {
                    logger.LogInformation("MonitorPagesAsync:{productName}{Text},{des}", productName, text.Text, driver.FindElement(By.ClassName("availability-text")).Text);
                }
            }
            catch (NoSuchElementException e)
            {
                logger.LogWarning("MonitorPagesAsync:{Message}", e.Message);
            }
            return Task.FromResult(status);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static int ParseMaxOrder(string input)
        {
            // 使用正则表达式提取文本中的数字
            string pattern = @"\d+";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                // 将提取到的数字转换为整数
                return int.Parse(match.Value);
            }
            else
            {
                return -1;
            }
        }
    }
}
