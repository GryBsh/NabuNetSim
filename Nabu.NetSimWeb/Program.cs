using Nabu;
using Nabu.Adaptor;
using Nabu.Network;
using Nabu.NetSimWeb;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Nabu.NetSimWeb.ViewModels;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Blazorise.LoadingIndicator;
using Blazorise.RichTextEdit;
using NLog.Extensions.Logging;

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
builder.Services.AddSingleton<AppLog>();
builder.Services.AddTransient<MainLayoutViewModel>();
builder.Services.AddTransient<HomeViewModel>();
Simulation.Register(builder.Services);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services
    .AddBlazorise(options => {
        options.Immediate = true;
    }).AddLoadingIndicator()
    .AddBlazoriseRichTextEdit()
    .AddBootstrap5Components()
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
