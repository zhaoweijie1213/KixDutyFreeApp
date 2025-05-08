using KixDutyFree.App.Manage;
using KixDutyFree.App.Models;
using KixDutyFree.App.Quartz;
using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Repository;
using KixDutyFree.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QYQ.Base.Common.IOCExtensions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Management;
using System.Runtime.InteropServices;

namespace KixDutyFree.Shared.Manage
{
    public class Manager(ILogger<Manager> logger, ProductMonitorRepository productMonitorRepository, AccountClientFactory accountClientFactory, QuartzManagement quartzManagement, AccountService accountService 
        , AppConfigRepository appConfigRepository) : ISingletonDependency
    {
        /// <summary>
        /// 加载数据
        /// </summary>
        /// <returns></returns>
        public async Task InitDataAsync()
        {
            logger.LogInformation("加载数据");
            var appConfig = await appConfigRepository.FindAsync();
            if (appConfig == null)
            {
                appConfig = new Models.Entity.AppConfigEntity
                {
                    ReOrderOnRestart = true
                };
                await appConfigRepository.InsertAsync(appConfig);
            }
            ////同步商品数据
            //await productService.SyncProductAsync();
            ////同步账号数据
            //await accountService.SyncAccountAsync();
        }

        /// <summary>
        /// 初始化客户端
        /// </summary>
        /// <returns></returns>
        public async Task InitClientAsync()
        {
            var appConfig = await appConfigRepository.FindAsync();
            if (appConfig?.ReOrderOnRestart == true)
            {
                //将订单标记为已完成
                await productMonitorRepository.UpdateCompletedAsync();
            }
            //账号
            var accounts = await accountService.GetAccountInfoAsync();
            if (accounts == null) return;
            //初始化各个账号的实例
            List<Task> tasks = [];
            foreach (var account in accounts)
            {
                tasks.Add(accountService.CreateClientAsync(account));
            }
            await Task.WhenAll(tasks);
            //检查登录状态任务
            await quartzManagement.StartLoginCheckAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly string _root = Path.Combine(Path.GetTempPath(), "selenium");

        /// <summary>
        /// 停止
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            List<Task> tasks = [];
            foreach (var client in accountClientFactory.Clients.Values)
            {
                tasks.Add(client.QuitAsync());
            }
            if (accountClientFactory.DefaultClient != null)
            {
                tasks.Add(accountClientFactory.DefaultClient.QuitAsync());
            }
            await Task.WhenAll(tasks);

            var drivers = Process.GetProcessesByName("chromedriver");
            foreach (var p in drivers)
            {
                try 
                { 
                    p.Kill();
                }
                catch
                {
                    /* ignore */
                }
            }
            var tempRoot = Path.Combine(Path.GetTempPath(), "selenium");
            KillChromeWithTempRoot(tempRoot);

            await SeleniumTempCleanupAsync();
        }

        /// <summary>
        /// 杀掉所有命令行里含有 tempRoot 的 chrome.exe 进程
        /// </summary>
        public void KillChromeWithTempRoot(string tempRoot)
        {
            // 1. 用 WMI 查询出所有 name=chrome.exe 且 CommandLine 包含 tempRoot 的进程 ID
            string wmiQuery =
                $"SELECT ProcessId FROM Win32_Process " +
                $"WHERE Name='chrome.exe' AND CommandLine LIKE '%{tempRoot.Replace("\\", "\\\\")}%'";
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogInformation("当前非 Windows 平台，跳过 Chrome 进程清理。");
                return;
            }
            using var searcher = new ManagementObjectSearcher(wmiQuery);
            foreach (ManagementObject mo in searcher.Get())
            {
                var pidObj = mo["ProcessId"];
                if (pidObj != null && int.TryParse(pidObj.ToString(), out int pid))
                {
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        proc.Kill();
                        logger.LogInformation("Killed chrome.exe (PID={pid})", pid);
                    }
                    catch
                    {
                        // 可能已经退出，忽略
                    }
                }
            }
        }



        /// <summary>
        /// 删除临时文件
        /// </summary>
        public Task SeleniumTempCleanupAsync()
        {
            try
            {
                if (!Directory.Exists(_root))
                {
                    logger.LogInformation("Selenium Temp 目录不存在，无需清理。");

                }

                var dirs = Directory.GetDirectories(_root);
                foreach (var dir in dirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        logger.LogInformation("已删除临时 Selenium Profile：{dir}", dir);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "删除临时目录失败：{dir}", dir);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "清理 Selenium Temp 目录时发生错误");
            }
            return Task.CompletedTask;
        }


    }
}
