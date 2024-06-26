﻿using Autofac.Extensions.DependencyInjection;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Nabu.Cli;
using Nabu.NetSim.UI;
using Nabu.NetSim.UI.Services;
using Nabu.NetSim.UI.ViewModels;
using Napa;
using NLog.Extensions.Logging;
using ReactiveUI;
using Spectre.Console.Cli;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using Nabu.Settings;
using Nabu.Sources;
using Lgc;
using Gry.Jobs;
using Gry;
using NHACP;
using Gry.Serialization;
using Spectre.Console;
using YamlDotNet.Serialization;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using Gry.Settings;
using Microsoft.AspNetCore.Mvc.Routing;
using Nabu.Network;
using Autofac.Core;
using Microsoft.Extensions.Configuration;

namespace Nabu.NetSimWeb;



public class ServerStartSettings : CommandSettings
{
    //[CommandOption("-p|--new-process")]
    //public bool NewProcess { get; set; }

    [CommandOption("-s|--noui")]
    public bool NoUI { get; set; } = true;

    [CommandOption("--setup")]
    public bool Setup { get; set; }
}

public class ServerStart : AsyncCommand<ServerStartSettings>
{
    public ServerStart(GlobalSettings settings, SettingsProvider settingsProvider, ILocationService location)
    {
        Settings = settings;
        SettingsProvider = settingsProvider;        Location = location;        Runtime.AddModule<GryModule>();
        Runtime.AddModule<NabuModule>();
        Runtime.AddModule<UIModule>();
        Runtime.AddModule<NHACPModule>();
    }

    GlobalSettings Settings { get; }
    SettingsProvider SettingsProvider { get;}
    ILocationService Location { get; }

    static void AddGlobal(        GlobalSettings settings,        ILocationService location,        IConfiguration configuration,         IServiceCollection services,         Action<GlobalSettings>? setter = null    )
    {        settings = new();        configuration.Bind("Settings", settings);
        services.Configure<NapaOptions>(configuration.GetSection("Packages"));
        setter?.Invoke(settings);
        services.AddSingleton(settings);
    }    static ILocationService Config(        GlobalSettings settings,         ILocationService location,         IHostBuilder? builder = null,        ConfigurationManager? config = null)    {        if (settings.UseHome)        {            location =  new LocationService(useClassic: false);            if (builder is not null)                builder.ConfigureHostConfiguration(b => b.AddJsonFile(location.FromHome.SettingsPath));            else            {                config?.AddJsonFile(location.FromHome.SettingsPath);            }        }        return location;    }

    public static async Task<int> Headless(        GlobalSettings settings,         ILocationService location,        string[] args    )
    {        Runtime.DisableRegistrar<NetSim.UI.ModuleBuilder>();
        var builder = Host.CreateDefaultBuilder(args);        location = Config(settings, location, builder);
        builder.ConfigureServices(
            (builder,services) => { 
                services.AddSingleton<ProcessService>();
                AddGlobal(settings, location, builder.Configuration, services, s => s.ForceHeadless = true);
            }
        );

        await Runtime.Build(builder).RunConsoleAsync();
        await Task.Delay(2000);
        return 0;
    }

    public static int WebServer(GlobalSettings settings, ILocationService location, string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);                builder.Configuration.AddJsonFile(location.FromHome.SettingsPath, true);        location = Config(settings, location, config: builder.Configuration);
        var services = builder.Services;
        AddGlobal(settings, location, builder.Configuration, services);

        //services.AddSingleton<ILoggerProvider, AnsiLogProvider>();
        services.UseMicrosoftDependencyResolver();
        var resolver = Locator.CurrentMutable;
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();

        services.AddControllers();
        services.AddRouting();
        services.AddDataProtection();
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services
            .AddBlazorise(
                options =>
                {
                    options.Immediate = true;
                }
            ).AddBootstrap5Components()
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();

        Runtime.Register(builder.Services, builder.Configuration);
        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
        app.MapControllers();
        app.Run();

        return 0;
    }


    public override async Task<int> ExecuteAsync(CommandContext context, ServerStartSettings settings)
    {
        var args = context.Remaining.Raw.ToArray();

        if (settings.NoUI)
            return await Headless(Settings, Location, args);
        return WebServer(Settings, Location, args);
    }

    /*
    private int Spawn(string[] args)
    {
        var directory = Path.GetDirectoryName(GetType().Assembly.Location);
        var exe = OperatingSystem.IsWindows() ? "nns.exe" : "nns";
        var self = Path.Combine(directory!, exe);
        var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = self,
                Arguments = "server start " + (string.Join(' ', args)),
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = directory,
                UseShellExecute = true
            }
        );

        var id = process?.Id ?? 0;

        if (id > 0)
        {
            AnsiConsole.WriteLine(id);
            return 0;
        }
        return -1;
    }
    */
}