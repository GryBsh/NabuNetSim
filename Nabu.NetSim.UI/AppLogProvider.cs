using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI;

public class AppLogConfiguration
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
public class AppLogProvider : ILoggerProvider
{
    readonly IOptionsMonitor<AppLogConfiguration> Config;
    readonly ConcurrentDictionary<string, AppLogger> Loggers = new(StringComparer.OrdinalIgnoreCase);
    IRepository Repository { get; }
    readonly Settings Settings;
    public AppLogProvider(
        Settings settings,
        IOptionsMonitor<AppLogConfiguration> config,
        IRepository repository
    )
    {
        Settings = settings;
        Config = config;
        Repository = repository;

        Observable.Interval(
            TimeSpan.FromHours(Settings.LogCleanupIntervalHours), 
            ThreadPoolScheduler.Instance
        ).Subscribe(_ =>
        {
            var cutoff = DateTime.Now.AddDays(-Settings.MaxLogEntryAgeDays);
            Repository.Collection<LogEntry>().DeleteMany(e => e.Timestamp < cutoff);
            GC.Collect();
        });
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

