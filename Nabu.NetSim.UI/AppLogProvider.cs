using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Nabu.NetSim.UI;

public class AppLogConfiguration
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
public class AppLogProvider : ILoggerProvider
{
    readonly IOptionsMonitor<AppLogConfiguration> Config;
    readonly ConcurrentDictionary<string, AppLogger> Loggers = new(StringComparer.OrdinalIgnoreCase);
    readonly AppLog AppLog;
    public AppLogProvider(
        AppLog appLog,
        IOptionsMonitor<AppLogConfiguration> config
    )
    {
        AppLog = appLog;
        Config = config;
    }

    public ILogger CreateLogger(string categoryName)
        => Loggers.GetOrAdd(
            categoryName,
            name => new AppLogger(name, AppLog, this, Config.CurrentValue)
        );

    public void Dispose()
    {
        foreach (var logger in Loggers)
        {
            Loggers.Clear();
        }
    }
}
