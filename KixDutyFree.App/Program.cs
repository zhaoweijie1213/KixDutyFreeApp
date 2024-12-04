using KixDutyFree.App.Services;
using KixDutyFree.App.Components;
using QYQ.Base.Common.IOCExtensions;
using KixDutyFree.App.Models;
using Quartz;
using Quartz.AspNetCore;
using KixDutyFree.App.Models.Config;
using ElectronNET.API;
using ElectronNET.API.Entities;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseElectron(args);

builder.Logging.AddLog4Net();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMultipleService("^KixDutyFree");
builder.Services.AddHostedService<WorkerService>();
builder.Services.Configure<List<AccountModel>>(builder.Configuration.GetSection("Accounts"));
builder.Services.Configure<ProductModel>(builder.Configuration.GetSection("Products"));
builder.Services.Configure<FlightInfoModel>(builder.Configuration.GetSection("FlightInfo"));
builder.Services.AddQuartz().AddQuartzServer(options =>
{
    // when shutting down we want jobs to complete gracefully
    options.WaitForJobsToComplete = true;
});
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

if (HybridSupport.IsElectronActive)
{
    CreateElectronWindow();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<KixDutyFree.App.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();


 async void CreateElectronWindow()
{
    var window = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
    {
        Width = 1152,
        Height = 864,
        Show = false // 先隐藏窗口
    });

    window.OnReadyToShow += () => window.Show();

    // 在这里添加托盘图标和菜单
    SetupTrayIcon(window);
}

 void SetupTrayIcon(BrowserWindow window)
{
    var menuItems = new MenuItem[]
    {
        new MenuItem
        {
            Label = "打开页面",
            Click =  () =>  window.Show()
        },
        new MenuItem
        {
            Label = "退出",
            Click = () => Electron.App.Exit()
        }
    };

    var trayMenu = new MenuItem[] { new MenuItem { Label = "菜单", Submenu = menuItems } };

    Electron.Tray.Show("icon.png", trayMenu);

    // 监听窗口关闭事件
    window.OnClose += () =>
    {
        // 取消关闭操作，隐藏窗口
         window.Hide();
    };

    // 监听托盘图标的双击事件
    Electron.Tray.OnDoubleClick += (e, r) =>
    {
        window.Show();
    };
}

