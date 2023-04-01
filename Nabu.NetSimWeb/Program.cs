using Nabu;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Nabu.NetSim.UI;
using Nabu.NetSim.UI.ViewModels;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using NLog.Extensions.Logging;
using LiteDb.Extensions.Caching;
using Nabu.Network.RetroNet;
using Nabu.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var settings = new Settings();
builder.Configuration.Bind("Settings", settings);
builder.Services.AddLogging(
    logging => {
        logging.ClearProviders()
               .AddInMemoryLogger()
               .AddNLog("nlog.config");
    }
);
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(settings.Sources);
builder.Services.AddHttpClient();
builder.Services.AddScoped<MainLayoutViewModel>();
builder.Services.AddScoped<HomeViewModel>();
builder.Services.AddSingleton(typeof(IRepository<>), typeof(LiteDBRepository<>));
builder.Services.AddLiteDbCache(
                    options =>
                    {
                        options.Connection = LiteDB.ConnectionType.Shared;
                        options.CachePath = settings.CacheDatabasePath;
                    }
                );
builder.Services.AddSingleton<IJob, LogCleanupJob>();
Simulation.Register(builder.Services, settings);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services
    .AddBlazorise(options => {
        options.Immediate = true;
    }).AddBootstrap5Components()
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons();

builder.Services.UseMicrosoftDependencyResolver();
var resolver = Locator.CurrentMutable;
    resolver.InitializeSplat();
    resolver.InitializeReactiveUI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
