using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using QYQ.Base.Common.IOCExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AixDutyFreeCrawler.App.Models;

namespace AixDutyFreeCrawler.App.Services
{

    public class SeleniumService(ILogger<SeleniumService> logger,IOptionsMonitor<List<AccountModel>> accounts) : ITransientDependency
    {
        /// <summary>
        /// 开始任务
        /// </summary>
        public Task TaskStartAsync()
        {
            var account = accounts.CurrentValue.First();
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
            return Task.CompletedTask;
        }

        /// <summary>
        /// 多页面监控
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public async Task MonitorPagesWithMultipleAccounts(List<string> urls, List<(string Username, string Password)> credentials)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < urls.Count; i++)
            {
                string url = urls[i];
                var credential = credentials[i];

                tasks.Add(Task.Run(() =>
                {
                    //var options = new ChromeOptions();
                    //options.AddArgument("--headless");
                    //options.AddArgument("--disable-gpu");
                    //options.AddArgument("--no-sandbox");
                    //options.AddArgument($"--user-data-dir=/path/to/chrome-profile-{i}");
                    //options.AddArgument("--incognito");

                    //IWebDriver driver = new ChromeDriver(options);
                    IWebDriver driver = new ChromeDriver();

                    try
                    {
                        driver.Navigate().GoToUrl(url);

                        // 登录步骤
                        driver.FindElement(By.Name("username")).SendKeys(credential.Username);
                        driver.FindElement(By.Name("password")).SendKeys(credential.Password);
                        driver.FindElement(By.Name("login")).Click();

                        // 登录后操作
                        Console.WriteLine($"User {credential.Username} monitoring page: {url}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error for user {credential.Username} on {url}: {ex.Message}");
                    }
                    finally
                    {
                        driver.Quit();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

    }
}
