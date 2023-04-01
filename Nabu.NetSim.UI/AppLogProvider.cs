using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    IRepository<LogEntry> Repository { get; }
    readonly Settings Settings;
    public AppLogProvider(
        Settings settings,
        IOptionsMonitor<AppLogConfiguration> config,
        IRepository<LogEntry> repository
    )
    {
        Settings = settings;
        Config = config;
        Repository = repository;

        
    }

    public ILogger CreateLogger(string categoryName)
        => Loggers.GetOrAdd(
            categoryName,
            name => new AppLogger(name, this, Config.CurrentValue, Repository)
        );

    public void Dispose()
    {
        Loggers.Clear();
    }
}
