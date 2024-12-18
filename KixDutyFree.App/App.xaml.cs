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

namespace KixDutyFree.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {

        private IHost _host;

        public App()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddWpfBlazorWebView();
            builder.Services.AddMasaBlazor(options => {
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
            });
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif
            //builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddSingleton<MainWindow>();
            _host = builder.Build();
            GobalObject.serviceProvider = _host.Services;
            Task.Run(async () =>
            {
                await _host.StartAsync();
            });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
        
            //// 获取日志服务并重定向 Console
            //var loggingService = _host.Services.GetRequiredService<ILoggingService>();
            //Console.SetOut(new ConsoleLogger(loggingService));

            //var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            //mainWindow.Show();

            base.OnStartup(e);
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
                // 获取当前可执行文件的路径
                string exePath = Assembly.GetExecutingAssembly().Location;

                // 启动新的应用程序实例
                Process.Start(new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true
                });

                // 关闭当前应用程序
                Current.Shutdown();
            }
            catch (Exception ex)
            {
                // 记录异常或显示消息
                HandyControl.Controls.MessageBox.Show($"无法重启应用程序：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
