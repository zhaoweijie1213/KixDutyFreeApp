using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HtmlAgilityPack;
using KixDutyFree.App.Models;
using KixDutyFree.Shared.Manage.Client.Interface;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;


namespace KixDutyFree.Shared.Manage.Client
{
    /// <summary>
    /// 专用于 HTML+HTTP 的账号客户端，实现 IAccountClient。
    /// 使用 IHttpClientFactory + HtmlAgilityPack 完成登录及后续操作。
    /// 与原 Selenium 驱动的 AccountClient 并存。
    /// </summary>
    public class HttpAccountClient : IAccountClient, ITransientDependency
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<HttpAccountClient> _logger;
        private HttpClient _httpClient = null!;

        public HttpAccountClient(IHttpClientFactory clientFactory, ILogger<HttpAccountClient> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> InitAsync(AccountInfo? account)
        {
            // 使用账号 Email 作为命名客户端，隔离 CookieContainer
            _httpClient = _clientFactory.CreateClient(account?.Email ?? "DefaultClient");
            if (_httpClient.BaseAddress == null)
                _httpClient.BaseAddress = new Uri("https://www.kixdutyfree.jp");
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            try
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
                    _logger.LogError("HttpAccountClient: 无法获取登录表单的 csrf_token");
                    return false;
                }

                // 2. 提交登录表单
                var data = new Dictionary<string, string>
                {
                    ["loginEmail"] = account.Email,
                    ["loginPassword"] = account.Password,
                    ["loginRememberMe"] = "true",
                    ["csrf_token"] = csrf
                };
                using var content = new FormUrlEncodedContent(data);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                var resp = await _httpClient.PostAsync("/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Account-Login?rurl=1", content);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("HttpAccountClient: 登录失败，状态码 {StatusCode}", resp.StatusCode);
                    return false;
                }

                // 3. 验证登录
                var accountHtml = await _httpClient.GetStringAsync("/cn/account");
                if (accountHtml.Contains("退出") || accountHtml.Contains("logout"))
                {
                    _logger.LogInformation("HttpAccountClient: 登录成功 {Email}", account.Email);
                    return true;
                }

                _logger.LogWarning("HttpAccountClient: 登录验证未通过");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HttpAccountClient.InitAsync 异常");
                return false;
            }
        }
    }
}
