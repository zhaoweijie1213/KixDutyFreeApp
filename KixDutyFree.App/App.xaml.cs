using KixDutyFree.App.Models.Config;
using KixDutyFree.App.Models;
using KixDutyFree.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using QYQ.Base.Common.IOCExtensions;
using Serilog;
using System.Configuration;
using System.Data;
using System.Windows;
using Quartz.AspNetCore;
using System;
using Masa.Blazor;
using KixDutyFree.App.Service;
using Magicodes.ExporterAndImporter.Excel.Utility.TemplateExport;
using System.Reflection;
using System.Diagnostics;
using KixDutyFree.Shared.Services.Interface;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.Extension;

namespace KixDutyFree.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {

        private readonly IHost _host;

        private readonly ILogger<App> _logger;

        public App()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddWpfBlazorWebView();
            builder.Services.AddMasaBlazor(options =>
            {
                // new Locale(current, fallback);
                options.Locale = new Locale("zh-CN", "en-US");
            });
            var sink = new LogStore();
            builder.Services.AddSingleton(sink);
            builder.Services.AddSerilog(configureLogger =>
            {

                configureLogger.Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(builder.Configuration).WriteTo.Sink(sink);

            });
            // 注册 MediatR，并扫描当前程序集中的处理器
            builder.Services.AddMediatR(cfg =>
            {
                // 加载程序集
                var sharedAssembly = Assembly.Load("KixDutyFree.Shared");
                cfg.RegisterServicesFromAssembly(sharedAssembly);
            });
            builder.Services.AddMultipleService("^KixDutyFree");
            //builder.Services.AddHostedService<WorkerService>();
            builder.Services.AddHostedService<StartupService>();
            builder.Services.AddHostedService<CheckVersionStartupService>();
            builder.Services.Configure<List<AccountInfo>>(builder.Configuration.GetSection("Accounts"));
            builder.Services.Configure<ProductModel>(builder.Configuration.GetSection("Products"));
            builder.Services.Configure<FlightInfoModel>(builder.Configuration.GetSection("FlightInfo"));
            builder.Services.AddQuartz().AddQuartzServer(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;
            });
            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();

            builder.Services.AddQuartz(q =>
            {
                q.UseDefaultThreadPool(x => x.MaxConcurrency = 50);
                //q.UseInMemoryStore();
            });
            builder.Services.AddQuartzHostedService(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;
                options.AwaitApplicationStarted = true;
            });
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif
            //builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddSingleton<MainWindow>();
            _host = builder.Build();
            GobalObject.serviceProvider = _host.Services;
            _logger = GobalObject.serviceProvider.GetRequiredService<ILogger<App>>();
            // 订阅 RestartRequested 事件
            var restartService = GobalObject.serviceProvider.GetRequiredService<IRestartService>();
            restartService.RestartRequested += RestartApplication;
 
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                await _host.StartAsync();
            }
            catch (Exception ex)
            {
                // 记录异常或显示消息
                HandyControl.Controls.MessageBox.Show($"启动应用程序失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // 关闭当前应用程序
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 同步阻塞直到 _host.StopAsync() 执行完成
            _host.StopAsync().GetAwaiter().GetResult();
            base.OnExit(e);
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        public void RestartApplication()
        {
            try
            {

                _logger.LogInformation("RestartApplication:重新启动应用程序");

                /***
                 * MessageBox.Show 或 Application.Current.Shutdown 这类操作需要在 UI 线程上执行。如果这些操作在后台线程上调用，就会导致“调用线程无法访问此对象，因为另一个线程拥有该对象”的错误。
                 * 在您的场景中，RestartApplication 方法被 RestartRequested 事件触发，而该事件可能在后台线程（例如，HostedService 或其他业务逻辑线程）上被触发。这导致 RestartApplication 方法在非 UI 线程上执行，从而尝试访问 UI 元素时引发异常。
                 * 解决方案
                 * 要解决这个问题，您需要确保所有涉及 UI 操作的方法（如 MessageBox.Show 和 Application.Current.Shutdown）都在 UI 线程上执行。可以通过 WPF 的 Dispatcher 将这些操作调度到 UI 线程。
                 * 确保 RestartApplication 方法中的所有 UI 操作都在 UI 线程上执行。可以使用 Dispatcher.Invoke 或 Dispatcher.BeginInvoke 来实现这一点
                 * ***/

                // 使用 Dispatcher 确保在 UI 线程上执行
                Current.Dispatcher.Invoke(async () =>
                {
                    // 获取当前可执行文件的路径
                    string? exePath = Process.GetCurrentProcess().MainModule?.FileName;

                    if (!string.IsNullOrEmpty(exePath))
                    {
                        // 停止 Host
                        if (_host != null)
                        {
                            await _host.StopAsync();
                        }

                        // 启动新的应用程序实例
                        Process.Start(new ProcessStartInfo(exePath)
                        {
                            UseShellExecute = true
                        });

                        // 关闭当前应用程序
                        Current.Shutdown();
                    }
                });
               
            }
            catch (Exception ex)
            {
                _logger.BaseErrorLog("RestartApplication", ex);
                // 记录异常或显示消息
                HandyControl.Controls.MessageBox.Show($"无法重启应用程序：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
