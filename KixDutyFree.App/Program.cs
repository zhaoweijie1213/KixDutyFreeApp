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
        Show = false // �����ش���
    });

    window.OnReadyToShow += () => window.Show();

    // �������������ͼ��Ͳ˵�
    SetupTrayIcon(window);
}

 void SetupTrayIcon(BrowserWindow window)
{
    var menuItems = new MenuItem[]
    {
        new MenuItem
        {
            Label = "��ҳ��",
            Click =  () =>  window.Show()
        },
        new MenuItem
        {
            Label = "�˳�",
            Click = () => Electron.App.Exit()
        }
    };

    var trayMenu = new MenuItem[] { new MenuItem { Label = "�˵�", Submenu = menuItems } };

    Electron.Tray.Show("icon.png", trayMenu);

    // �������ڹر��¼�
    window.OnClose += () =>
    {
        // ȡ���رղ��������ش���
         window.Hide();
    };

    // ��������ͼ���˫���¼�
    Electron.Tray.OnDoubleClick += (e, r) =>
    {
        window.Show();
    };
}

