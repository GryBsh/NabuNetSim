using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nabu.NetSim.UI.Models;
using Nabu.NetSim.UI.Services;
using Nabu.Services;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;

namespace Nabu.NetSim.UI;

public class AppLogConfiguration
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
public class AppLogProvider : ILoggerProvider
{
    readonly IOptionsMonitor<AppLogConfiguration> Config;
    readonly ConcurrentDictionary<string, AppLogger> Loggers = new(StringComparer.OrdinalIgnoreCase);
    LogService Logs { get; }
    readonly Settings Settings;
    public AppLogProvider(
        Settings settings,
        IOptionsMonitor<AppLogConfiguration> config,
        LogService repository
    )
    {
        Settings = settings;
        Config = config;
        Logs = repository;
    }

    public ILogger CreateLogger(string categoryName)
        => Loggers.GetOrAdd(
            categoryName,
            name => new AppLogger(name, this, Config.CurrentValue, Logs)
        );

    public void Dispose()
    {
        Loggers.Clear();
    }
}
