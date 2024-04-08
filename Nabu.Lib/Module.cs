using Lgc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Jobs;
using Nabu.Network;
using Nabu.Protocols.Classic;
using Nabu.Protocols;
using Nabu.Sources;
using Napa;
using Autofac.Extensions.DependencyInjection;
using Nabu.Settings;
using Nabu.Protocols.RetroNet;
using Gry.Jobs;
using Gry.Protocols;
using NHACP.V0;
using NHACP.V01;
using Gry.Settings;

namespace Nabu;

public class NabuModule : Module;

public class ModuleBuilder : IRegister
{
    void LoggingBuilder(ILoggingBuilder builder)
    {
        builder.ClearProviders();
        var builders = Runtime.GetImplementationsOf<ILoggerBuilder>()
                              .Select(b => Runtime.Activate<ILoggerBuilder>(b));
        foreach (var logBuilder in builders)
            logBuilder?.Build(builder);
    }

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutofac();
        services.AddLogging(LoggingBuilder);

        services.AddSingleton<INabuNetwork, NabuNetwork>();
        services.AddSingleton<SourceService>();
        services.AddSingleton<ISourceService, SourceService>();
        services.AddSingleton<StorageService>();
        services.AddSingleton<IPackageManager, PackageManager>();
        services.AddSingleton<PackageService>();

        services.AddSingleton<IJob, RefreshSourcesJob>();
        services.AddSingleton<IJob, ServiceStarterJob>();
        services.AddHttpClient();

        services.AddTransient<ClassicNabuProtocol>();
        services.AddTransient<IProtocol<AdaptorSettings>, NHACPProtocol>();
        services.AddTransient<IProtocol<AdaptorSettings>, NHACPV01Protocol<AdaptorSettings>>();
        services.AddTransient<IProtocol<AdaptorSettings>, RetroNetProtocol>();        services.AddTransient<IProtocol<AdaptorSettings>, RetroNetHeadlessProtocol>();
        services.AddTransient<IProtocol<AdaptorSettings>, MenuProtocol>();
        services.AddTransient<IProtocol<AdaptorSettings>, ControlProtocol>();

        services.AddSingleton(typeof(Logs.ILogger<>), typeof(Logs.ConsoleLog<>));

        services.AddSingleton<ISimulation, NabuSimulation>();
        //services.AddSingleton<ISimulation, Simulation>()
        //        .AddHostedService<Simulation>();

        services.AddSingleton<SettingsProvider>();
        
        
    }
}
