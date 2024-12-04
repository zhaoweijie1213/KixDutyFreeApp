using KixDutyFree.App.Services;
using KixDutyFree.App.Components;
using QYQ.Base.Common.IOCExtensions;
using KixDutyFree.App.Models;
using Quartz;
using Quartz.AspNetCore;
using KixDutyFree.App.Models.Config;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseElectron(args);
//builder.Services.AddElectron();

builder.Services.AddSerilog(configureLogger =>
{
    configureLogger.Enrich.WithMachineName()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(builder.Configuration);
});
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

