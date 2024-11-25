using OpenQA.Selenium;
using System.Net;

namespace AixDutyFreeCrawler.App
{
    public class HttpInfoSyncTool
    {
        private static CookieContainer cookieContainer = new CookieContainer();

        // 从 Selenium 获取最新的 Cookie，并同步到 HttpClient
        public static void SyncCookiesFromSeleniumToHttpClient(IWebDriver driver)
        {
            foreach (OpenQA.Selenium.Cookie seleniumCookie in driver.Manage().Cookies.AllCookies)
            {
                System.Net.Cookie cookie = new System.Net.Cookie
                {
                    Name = seleniumCookie.Name,
                    Value = seleniumCookie.Value,
                    Domain = seleniumCookie.Domain.StartsWith(".") ? seleniumCookie.Domain : "." + seleniumCookie.Domain,
                    Path = seleniumCookie.Path,
                    Secure = seleniumCookie.Secure,
                    HttpOnly = seleniumCookie.IsHttpOnly,
                    Expires = seleniumCookie.Expiry ?? DateTime.MinValue
                };
                cookieContainer.Add(cookie);
            }
            Console.WriteLine("Cookies 已成功同步到 HttpClient");
        }

        /// <summary>
        /// 从 HttpClient 的 CookieContainer 获取 Cookie 并同步到 Selenium
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="domain"></param>
        public static void SyncCookiesFromHttpClientToSelenium(IWebDriver driver, CookieContainer cookieContainer, string domain)
        {
            Uri targetUri = new(domain);
            foreach (System.Net.Cookie httpCookie in cookieContainer.GetCookies(targetUri))
            {
                OpenQA.Selenium.Cookie seleniumCookie = new(
                    httpCookie.Name,
                    httpCookie.Value,
                    httpCookie.Domain,
                    httpCookie.Path,
                    httpCookie.Expires == DateTime.MinValue ? null : (DateTime?)httpCookie.Expires
                );

                // 在 Selenium 中添加或更新 Cookie
                driver.Manage().Cookies.AddCookie(seleniumCookie);
            }
            Console.WriteLine("Cookies 已成功同步到 Selenium");
        }
    }
}
