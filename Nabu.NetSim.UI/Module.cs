using Gry;
using Gry.Adapters;
using Gry.Jobs;
using Lgc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Nabu.NetSim.UI.Services;
using Nabu.NetSim.UI.ViewModels;
using Nabu.Network;
using Nabu.Settings;

namespace Nabu.NetSim.UI;

public class UIModule : Module
{
}
/*
public class DBLogBuilder : ILoggerBuilder
{
    public ILoggingBuilder Build(ILoggingBuilder builder)
    {
        builder.AddConfiguration();
        //builder.Services.AddLiteDb();
        //builder.Services.AddSingleton<ILogService, LogService>();
        //builder.Services.AddSingleton<ILoggerProvider, AppLogProvider>();
        return builder;
    }
}*/



public class ModuleBuilder : IRegister 
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        //services.AddSingleton<IJob, LogCleanupJob>();
        services.AddSingleton<IJob, GCJob>();
        
        //services.AddSingleton<IHeadlineService, HeadlineService>();
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

        services.AddSingleton<IJob,LogTailCleanupJob>();

       
    }
}
