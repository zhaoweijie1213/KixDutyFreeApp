using MudBlazor.Services;
using KixDutyFreeMud.App.Components;
using Serilog;
using KixDutyFree.App.Models.Config;
using KixDutyFree.App.Models;
using Quartz;
using QYQ.Base.Common.IOCExtensions;
using Quartz.AspNetCore;
using KixDutyFree.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(configureLogger =>
{
    configureLogger.Enrich.WithMachineName()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(builder.Configuration);
});
// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMultipleService("^KixDutyFree");
builder.Services.AddHostedService<WorkerService>();
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
