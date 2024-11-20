using AixDutyFreeCrawler.App.Services;
using AixDutyFreeCrawler.App.Components;
using QYQ.Base.Common.IOCExtensions;
using AixDutyFreeCrawler.App.Models;
using Quartz;
using Quartz.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMultipleService("^AixDutyFreeCrawler");
builder.Services.AddHostedService<WorkerService>();
builder.Services.Configure<List<AccountModel>>(builder.Configuration.GetSection("Accounts"));
builder.Services.AddQuartz().AddQuartzServer(options =>
{
    // when shutting down we want jobs to complete gracefully
    options.WaitForJobsToComplete = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
