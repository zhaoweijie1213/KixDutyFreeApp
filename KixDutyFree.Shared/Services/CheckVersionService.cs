using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium.DevTools;
using QYQ.Base.Common.Extension;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    /// <summary>
    /// 版本检测
    /// </summary>
    public class CheckVersionService(ILogger<CheckVersionService> logger, IHttpClientFactory httpClientFactory) : ISingletonDependency
    {
        /// <summary>
        /// 所有者
        /// </summary>
        public const string owner = "zhaoweijie1213";

        /// <summary>
        /// 仓库名称
        /// </summary>

        public const string repo = "KixDutyFreeApp";

        /// <summary>
        /// 
        /// </summary>
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        private bool _IsNewVersionAvailable = false;

        /// <summary>
        /// 是否有新版本可用
        /// </summary>
        public bool NewVersionAvailable
        {
            get
            {
                return _IsNewVersionAvailable;
            }

            set
            {
                _IsNewVersionAvailable = value;
            }
        }

        /// <summary>
        /// 新版本可用事件
        /// </summary>
        public event EventHandler<GitHubRelease>? OnNewVersionAvailable;

        /// <summary>
        /// 检测更新
        /// </summary>
        /// <returns></returns>
        public async Task CheckForUpdateAsync()
        {
            string? currentVersion = GetCurrentVersion();
            if (string.IsNullOrEmpty(currentVersion)) return;

            logger.LogInformation("当前版本: {currentVersion}", currentVersion);
            GitHubReleaseResponse? latestRelease = await GetLatestReleaseAsync(owner, repo);
            if (latestRelease != null)
            {
                logger.LogInformation("最新版本: {latestRelease.TagName}", latestRelease.TagName);

                _IsNewVersionAvailable = IsNewVersionAvailable(currentVersion, latestRelease.TagName);

                if (_IsNewVersionAvailable)
                {
                    //// 下载第一个资产
                    //var asset = latestRelease.Assets.FirstOrDefault();
                    //if (asset != null)
                    //{
                    //    logger.LogInformation($"检测到新版本 {latestRelease.TagName}，准备下载 {asset.Name}...");
                    //    string downUrl = asset.BrowserDownloadUrl;
                    //}
                    GitHubRelease gitHubRelease = new GitHubRelease()
                    {
                        Name = latestRelease.Name ?? "",
                        Body = latestRelease.Body.Split("\r\n").ToList(),
                        BrowserDownloadUrl = latestRelease.Assets.FirstOrDefault()?.BrowserDownloadUrl ?? string.Empty
                    };
                    OnNewVersionAvailable?.Invoke(null, gitHubRelease);
                }
            }

        }

        /// <summary>
        /// 获取最新版本
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="repo"></param>
        /// <returns></returns>
        public async Task<GitHubReleaseResponse?> GetLatestReleaseAsync(string owner, string repo)
        {
            GitHubReleaseResponse? gitHubRelease = null;
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                var client = httpClientFactory.CreateClient("github");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                gitHubRelease = JsonConvert.DeserializeObject<GitHubReleaseResponse>(json) ?? new();
            }
            catch (Exception e)
            {
                logger.BaseErrorLog("GetLatestReleaseAsync", e);
            }
            return gitHubRelease;
        }


        /// <summary>
        /// 比较版本号
        /// </summary>
        /// <param name="currentVersion"></param>
        /// <param name="latestVersion"></param>
        /// <returns></returns>
        public bool IsNewVersionAvailable(string currentVersion, string latestVersion)
        {
            // 移除可能的前缀，如 'v'
            latestVersion = latestVersion.TrimStart('v', 'V');
            Version current = new(currentVersion);
            Version latest = new(latestVersion);
            return latest > current;
        }


        /// <summary>
        /// 获取当前应用程序的版本
        /// </summary>
        /// <returns></returns>
        public static string? GetCurrentVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        }

        public class GitHubReleaseResponse
        {
            [JsonProperty("url")]
            public string? Url { get; set; }

            [JsonProperty("assets_url")]
            public string? AssetsUrl { get; set; }

            [JsonProperty("upload_url")]
            public string? UploadUrl { get; set; }

            [JsonProperty("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("author")]
            public Author? Author { get; set; }

            [JsonProperty("node_id")]
            public string? NodeId { get; set; }

            [JsonProperty("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonProperty("target_commitish")]
            public string? TargetCommitish { get; set; }

            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("draft")]
            public bool Draft { get; set; }

            [JsonProperty("prerelease")]
            public bool Prerelease { get; set; }

            [JsonProperty("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonProperty("published_at")]
            public DateTime PublishedAt { get; set; }

            [JsonProperty("assets")]
            public List<Asset> Assets { get; set; } = [];

            [JsonProperty("tarball_url")]
            public string? TarballUrl { get; set; }

            [JsonProperty("zipball_url")]
            public string? ZipballUrl { get; set; }

            [JsonProperty("body")]
            public string Body { get; set; } = string.Empty;
        }

        public class Author
        {
            [JsonProperty("login")]
            public string? Login { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("node_id")]
            public string? NodeId { get; set; }

            [JsonProperty("avatar_url")]
            public string? AvatarUrl { get; set; }

            [JsonProperty("gravatar_id")]
            public string? GravatarId { get; set; }

            [JsonProperty("url")]
            public string? Url { get; set; }

            [JsonProperty("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonProperty("followers_url")]
            public string? FollowersUrl { get; set; }

            [JsonProperty("following_url")]
            public string? FollowingUrl { get; set; }

            [JsonProperty("gists_url")]
            public string? GistsUrl { get; set; }

            [JsonProperty("starred_url")]
            public string? StarredUrl { get; set; }

            [JsonProperty("subscriptions_url")]
            public string? SubscriptionsUrl { get; set; }

            [JsonProperty("organizations_url")]
            public string? OrganizationsUrl { get; set; }

            [JsonProperty("repos_url")]
            public string? ReposUrl { get; set; }

            [JsonProperty("events_url")]
            public string? EventsUrl { get; set; }

            [JsonProperty("received_events_url")]
            public string? ReceivedEventsUrl { get; set; }

            [JsonProperty("type")]
            public string? Type { get; set; }

            [JsonProperty("user_view_type")]
            public string? UserViewType { get; set; }

            [JsonProperty("site_admin")]
            public bool SiteAdmin { get; set; }
        }

        public class Asset
        {
            [JsonProperty("url")]
            public string? Url { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("node_id")]
            public string? NodeId { get; set; }

            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("label")]
            public string? Label { get; set; }

            [JsonProperty("uploader")]
            public Uploader? Uploader { get; set; }

            [JsonProperty("content_type")]
            public string? ContentType { get; set; }

            [JsonProperty("state")]
            public string? State { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }

            [JsonProperty("download_count")]
            public long DownloadCount { get; set; }

            [JsonProperty("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonProperty("updated_at")]
            public DateTime UpdatedAt { get; set; }

            [JsonProperty("browser_download_url")]
            public string BrowserDownloadUrl { get; set; } = string.Empty;
        }

        public class Uploader
        {
            [JsonProperty("login")]
            public string? Login { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("node_id")]
            public string? NodeId { get; set; }

            [JsonProperty("avatar_url")]
            public string? AvatarUrl { get; set; }

            [JsonProperty("gravatar_id")]
            public string? GravatarId { get; set; }

            [JsonProperty("url")]
            public string? Url { get; set; }

            [JsonProperty("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonProperty("followers_url")]
            public string? FollowersUrl { get; set; }

            [JsonProperty("following_url")]
            public string? FollowingUrl { get; set; }

            [JsonProperty("gists_url")]
            public string? GistsUrl { get; set; }

            [JsonProperty("starred_url")]
            public string? StarredUrl { get; set; }

            [JsonProperty("subscriptions_url")]
            public string? SubscriptionsUrl { get; set; }

            [JsonProperty("organizations_url")]
            public string? OrganizationsUrl { get; set; }

            [JsonProperty("repos_url")]
            public string? ReposUrl { get; set; }

            [JsonProperty("events_url")]
            public string? EventsUrl { get; set; }

            [JsonProperty("received_events_url")]
            public string? ReceivedEventsUrl { get; set; }

            [JsonProperty("type")]
            public string? Type { get; set; }

            [JsonProperty("user_view_type")]
            public string? UserViewType { get; set; }

            [JsonProperty("site_admin")]
            public bool SiteAdmin { get; set; }
        }
    }

    public class GitHubRelease
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public List<string> Body { get; set; } = [];

        /// <summary>
        /// 
        /// </summary>
        public string BrowserDownloadUrl { get; set; } = string.Empty;

    }
}
