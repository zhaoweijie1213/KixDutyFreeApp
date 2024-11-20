using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using QYQ.Base.Common.IOCExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AixDutyFreeCrawler.App.Models;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace AixDutyFreeCrawler.App.Services
{

    public class SeleniumService(ILogger<SeleniumService> logger) : ISingletonDependency
    {
        /// <summary>
        /// driver实例
        /// </summary>
        private readonly List<IWebDriver> drivers = new();

        /// <summary>
        /// 
        /// </summary>
        private readonly IWebDriver monitorDriver = new ChromeDriver();

        /// <summary>
        /// 开始任务
        /// </summary>
        public async Task CreateInstancesAsync(AccountModel account)
        {
            IWebDriver driver = new ChromeDriver();
            driver.Navigate().GoToUrl("https://www.kixdutyfree.jp/cn/login/");
            //获取标题
            var title = driver.Title;
            logger.LogInformation("Title:{title}", title);

            await Confirm(driver);

            ////查询是否有年龄确认dialog对话框
            //try
            //{
            //    var ageConfirmDialog = driver.FindElement(By.ClassName("modal-dialog"));
            //    var ageModalHeader = ageConfirmDialog.FindElement(By.ClassName("modal-header"));
            //    if(ageModalHeader.FindElement(By.TagName("span")).Text == "年龄认证")
            //    {
            //        //年龄确认
            //        var ageButton = ageConfirmDialog.FindElement(By.Id("age_yes"));
            //        ageButton.Click();
            //    }
            //}
            //catch (NoSuchElementException e)
            //{
            //    logger.LogWarning("TaskStartAsync:{Message}", e.Message);
            //}
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
            //检测登录按钮
            try
            {
                //输入账号密码
                var email = driver.FindElement(By.Id("login-form-email"));
                email.SendKeys(account.Email);
                var pwd = driver.FindElement(By.Id("login-form-password"));
                pwd.SendKeys(account.Password);
                //点击登录按钮
                var loginSubmit = driver.FindElement(By.XPath("//div[contains(@class, 'login-submit-button') and contains(@class, 'pt-1')]//button[contains(@class, 'btn') and contains(@class, 'btn-block') and contains(@class, 'btn-primary') and contains(@class, 'btn-login')]"));
                loginSubmit.Click();
                logger.LogInformation("TaskStartAsync.登录:{email}",email);
            }
            catch (NoSuchElementException e)
            {
                logger.LogWarning("TaskStartAsync:{Message}", e.Message);
            }
            //保存实例到集合
            drivers.Add(driver);
        }

        ///// <summary>
        ///// 商品页面库存监控(不登录)
        ///// </summary>
        ///// <param name="productAddress"></param>
        ///// <returns></returns>
        //public async Task<bool> MonitorPagesAsync(string productAddress)
        //{
        //    bool status = true;
        //    monitorDriver.Navigate().GoToUrl(productAddress);
        //    await Confirm(monitorDriver);
        //    return await CheckInventoryAsync(monitorDriver);
        //}



        /// <summary>
        /// 下单
        /// </summary>
        /// <returns></returns>
        public async Task OrderAsync(IWebDriver driver,string productAddress)
        {
            bool status = await CheckInventoryAsync(driver);
            if (status)
            {
                int orderNum = 0;
                try
                {
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
                productQuantity.SendKeys(orderNum.ToString());
                //添加到购物车
                var addToCart = driver.FindElement(By.ClassName("add-to-cart"));
                addToCart.Click();
                //去购物车结算
                driver.Navigate().GoToUrl("https://www.kixdutyfree.jp/cn/cart/");
            }
        }

        ///// <summary>
        ///// 多页面监控
        ///// </summary>
        ///// <param name="urls"></param>
        ///// <param name="credentials"></param>
        ///// <returns></returns>
        //public async Task MonitorPagesWithMultipleAccounts(List<string> urls)
        //{
        //    var tasks = new List<Task>();
        //    foreach (var item in drivers)
        //    {

        //    }
        //    for (int i = 0; i < urls.Count; i++)
        //    {
        //        string url = urls[i];
        //        var credential = credentials[i];

        //        tasks.Add(TaskStartAsync(accounts));
        //    }

        //    await Task.WhenAll(tasks);
        //}

        /// <summary>
        /// 信息确认
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        public Task Confirm(IWebDriver driver)
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
                logger.LogWarning("TaskStartAsync:{Message}", e.Message);
            }
            //同意网站cookie
            try
            {
                var trustCookie = driver.FindElement(By.Id("onetrust-accept-btn-handler"));
                trustCookie.Click();
            }
            catch (NoSuchElementException e)
            {
                logger.LogWarning("TaskStartAsync:{Message}", e.Message);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 检测库存
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        public Task<bool> CheckInventoryAsync(IWebDriver driver)
        {
            bool status = true;
            string productName = "";
            try
            {
                productName = monitorDriver.FindElement(By.ClassName("product-name")).Text;
            }
            catch (NoSuchElementException e)
            {
                logger.LogWarning("MonitorPagesAsync:{Message}", e.Message);
            }

            try
            {
                //查询是否有库存
                var text = monitorDriver.FindElement(By.XPath("//div[contains(@class, 'title') and contains(@class, 'availability-header')]"));
                if (text.Text == "暂无库存")
                {
                    status = false;
                    logger.LogInformation("MonitorPagesAsync:{productName}{Text}", productName, text.Text);
                }
                else
                {
                    logger.LogInformation("MonitorPagesAsync:{productName}{Text},{des}", productName, text.Text, monitorDriver.FindElement(By.ClassName("availability-text")).Text);
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
