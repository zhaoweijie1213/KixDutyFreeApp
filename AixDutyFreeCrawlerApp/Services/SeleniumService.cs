using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using QYQ.Base.Common.IOCExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AixDutyFreeCrawler.App.Models;

namespace AixDutyFreeCrawler.App.Services
{

    public class SeleniumService(ILogger<SeleniumService> logger) : ISingletonDependency
    {
        /// <summary>
        /// driver实例
        /// </summary>
        private readonly List<IWebDriver> drivers = new();

        /// <summary>
        /// 开始任务
        /// </summary>
        public Task CreateInstancesAsync(AccountModel account)
        {
            IWebDriver driver = new ChromeDriver();
            driver.Navigate().GoToUrl("https://www.kixdutyfree.jp/cn/login/");
            //获取标题
            var title = driver.Title;
            logger.LogInformation("Title:{title}", title);
            //查询是否有年龄确认dialog对话框
            try
            {
                var ageConfirmDialog = driver.FindElement(By.ClassName("modal-dialog"));
                var ageModalHeader = ageConfirmDialog.FindElement(By.ClassName("modal-header"));
                if(ageModalHeader.FindElement(By.TagName("span")).Text == "年龄认证")
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
            return Task.CompletedTask;
        }

        /// <summary>
        /// 页面监控
        /// </summary>
        /// <returns></returns>
        public Task MonitorPages()
        {

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

    }
}
