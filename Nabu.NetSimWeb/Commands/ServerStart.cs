﻿using Autofac.Extensions.DependencyInjection;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Nabu;
using Nabu.Cli;
using Nabu.NetSim.UI;
using Nabu.NetSim.UI.Services;
using Nabu.NetSim.UI.ViewModels;
using Nabu.NetSimWeb;
using Nabu.Network.NHACP.V0;
using Nabu.Network.NHACP.V01;
using Nabu.Network.RetroNet;
using Nabu.Network;
using Nabu.Services;
using Napa;
using NLog.Extensions.Logging;
using ReactiveUI;
using Spectre.Console;
using Spectre.Console.Cli;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Configuration;
using System.IO;
using Nabu.Packages;
using Nabu.JavaScript;
using Nabu.Network.NabuNetworkCom;
using Autofac.Core;

namespace Nabu.NetSimWeb;

public class ProtocolFailedToLoad : Protocol
{
    public ProtocolFailedToLoad(ILog logger, AdaptorSettings? settings = null) : base(logger, settings)
    {
    }

    public override byte[] Commands { get; } = Array.Empty<byte>();

    public override byte Version { get; } = 0;

    protected override Task Handle(byte unhandled, CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
}

public class ServerStartSettings : CommandSettings
{
    //[CommandOption("-p|--new-process")]
    //public bool NewProcess { get; set; }

    [CommandOption("-s|--noui")]
    public bool NoUI { get; set; } = true;
}

public class ServerStart : AsyncCommand<ServerStartSettings>
{
    public ServerStart(Settings settings)
    {
        Settings = settings;
    }

    public Settings Settings { get; }

    public static IServiceCollection AddNabu(IServiceCollection services, Settings settings)
    {
        services.AddAutofac();
        services.AddLogging(
            logging =>
                logging
                    .ClearProviders()
                    .AddNLog("nlog.config")
        );

        services.AddSingleton<INabuNetwork, NabuNetwork>();
        services.AddSingleton<SourceService>();
        services.AddSingleton<ISourceService, SourceService>();
        services.AddSingleton<StorageService>();
        services.AddSingleton<IPackageManager, PackageManager>();
        services.AddSingleton<PackageService>();
        services.AddSingleton<IFileCache, FileCache>();
        services.AddSingleton<IHttpCache, HttpCache>();
        services.AddSingleton<IJob, RefreshSourcesJob>();
        services.AddSingleton<IJob, RefreshPackagesJob>();
        services.AddHttpClient();

        services.AddTransient<ClassicNabuProtocol>();
        services.AddTransient<IProtocol, NHACPProtocol>();
        services.AddTransient<IProtocol, NHACPV01Protocol>();
        services.AddTransient<IProtocol, RetroNetProtocol>();
        services.AddTransient<IProtocol, HeadlessProtocol>();
        services.AddTransient<IProtocol, MenuProtocol>();

        services.AddSingleton(typeof(ILog<>), typeof(ConsoleLog<>));
        services.AddSingleton<ILoggerProvider, AnsiLogProvider>();

        services.AddSingleton<ISimulation, Simulation>()
                .AddHostedService<Simulation>();


        

        var factories = new IProtocolFactory[]
        {
            new JavaScriptFactory()
        };

        foreach (var factory in factories)
        {
            services.AddSingleton(factory);
        }

        foreach (var protocol in settings.Protocols)
        {
            if (factories.FirstOrDefault(f => f.Type == protocol.Type) is IProtocolFactory factory)
            {
                services.AddTransient(
                    sp =>
                    {
                        var logger = sp.GetRequiredService<ILog<PluginProtocol>>();
                        return factory.CreateProtocol(sp, logger, protocol);
                    }
                );
            }
        }

        return services;
    }

    static void AddStaticSources(Settings settings)
    {
        settings.Sources.Insert(0, new ProgramSource
        {
            Name = "Local NABU Files",
            Path = settings.LocalProgramPath
        });
        
    }

    public static async Task<int> Headless(Settings settings, string[] args)
    {       
        await Host.CreateDefaultBuilder(args).ConfigureServices(
            (context, services) =>
            {
                context.Configuration.Bind("Settings", settings);
                if (settings.DisableHeadless is false)
                {
                    foreach (var adaptor in settings.Adaptors.Serial.Concat<AdaptorSettings>(settings.Adaptors.TCP))
                    {
                        adaptor.Headless = true;
                        adaptor.HeadlessSource = settings.HeadlessSource;
                    }
                }
                AddStaticSources(settings);
                services.AddSingleton(settings);
                AddNabu(services, settings);
            }
        ).RunConsoleAsync();

        await Task.Delay(2000);
        return 0;
    }

    public static int WebServer(Settings settings, string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.Bind("Settings", settings);
        AddStaticSources(settings);
        builder.Services.AddSingleton(settings);
        var services = builder.Services;
        AddNabu(services, settings);
        builder.Services.AddLogging(l => l.AddDBLogger());
        services.UseMicrosoftDependencyResolver();
        var resolver = Locator.CurrentMutable;
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();

        services.AddDataProtection();
        services.AddLiteDb();

        services.AddSingleton<IJob, LogCleanupJob>();
        services.AddSingleton<IJob, GCJob>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IHeadlineService, HeadlineService>();
        services.AddSingleton<ProcessService>();

        services.AddScoped<MainLayoutViewModel>();
        services.AddScoped<HomeViewModel>();
        services.AddScoped<AdaptorSettingsViewModel>();
        services.AddScoped<StatusViewModel>();
        services.AddScoped<LogViewModel>();
        services.AddScoped<AdaptorViewModel>();
        services.AddScoped<ButtonTrayViewModel>();
        services.AddScoped<FilesViewModel>();
        services.AddScoped<PackagesViewModel>();
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<FolderListViewModel>();
        services.AddScoped<AvailablePackagesViewModel>();
        services.AddScoped<HeadlineViewModel>();
        services.AddScoped<EmulatorButtonViewModel>();

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
        app.Run();

        return 0;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ServerStartSettings settings)
    {
        var args = context.Remaining.Raw.ToArray();

        

        //if (settings.NewProcess)
        //    return Spawn(args);
        //else
        if (settings.NoUI)
            return await Headless(Settings, args);
        return WebServer(Settings, args);
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