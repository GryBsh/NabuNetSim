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
//using LiteDb.Extensions.Caching;
using Nabu.Network.RetroNet;
using Nabu.Services;
using Nabu.NetSim.UI.Services;
//using NeoSmart.Caching.Sqlite;
//using Microsoft.EntityFrameworkCore;
using Nabu.NetSimWeb;
using LiteDb.Extensions.Caching;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind("Settings", settings);

ConfigureServices(builder.Services, settings);

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


void ConfigureServices(IServiceCollection services, Settings settings)
{
    services.UseMicrosoftDependencyResolver();
    var resolver = Locator.CurrentMutable;
    resolver.InitializeSplat();
    resolver.InitializeReactiveUI();
    services.AddLogging(
        logging => {
            logging.ClearProviders()
                   .AddInMemoryLogger()
                   .AddNLog("nlog.config");
        }
    );
    services.AddTransient(typeof(IConsole<>), typeof(LoggingConsole<>));
    services.AddDataProtection();
    //services.AddSqliteCache(options => {
     //    options.CleanupInterval = TimeSpan.FromMinutes(15);
    //     options.CachePath = settings.CacheDatabasePath;
    //});
    
    services.AddSingleton(settings);
    services.AddSingleton(settings.Sources);
    
    services.AddHttpClient();
    services.AddSingleton(typeof(IRepository<>), typeof(LiteDBRepository<>));
    services.AddLiteDbCache(
        options =>
        {
            options.Connection = LiteDB.ConnectionType.Shared;
            options.CachePath = settings.CacheDatabasePath;
        }
    );

    services.AddSingleton<IJob, LogCleanupJob>();
    services.AddSingleton<IJob, GCJob>();
    //services.AddSqlite<AppData>("Data Source=data.db");
    services.AddSingleton<LogService>();
    Simulation.Register(services, settings);

    services.AddRazorPages();
    services.AddServerSideBlazor();
    services
        .AddBlazorise(
            options => {
                options.Immediate = true;
            }
        ).AddBootstrap5Components()
        .AddBootstrap5Providers()
        .AddFontAwesomeIcons();

    services.AddScoped<MainLayoutViewModel>();
    services.AddScoped<HomeViewModel>();
    services.AddScoped<MenuViewModel>();
    services.AddScoped<StatusViewModel>();
    services.AddScoped<LogViewModel>();
}
