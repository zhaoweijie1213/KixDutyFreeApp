using AixDutyFreeCrawler.App.Services;
using AixDutyFreeCrawler.App.Components;
using QYQ.Base.Common.IOCExtensions;
using AixDutyFreeCrawler.App.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMultipleService("^AixDutyFreeCrawler");
builder.Services.AddHostedService<WorkerService>();
builder.Services.Configure<List<AccountModel>>(builder.Configuration.GetSection("Accounts"));
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
