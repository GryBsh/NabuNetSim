using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nabu.NetSim.UI.Services;
using System.Collections.Concurrent;

namespace Nabu.NetSim.UI;

public class AppLogProvider : ILoggerProvider
{
    readonly ConcurrentDictionary<string, AppLogger> Loggers = new(StringComparer.OrdinalIgnoreCase);
    LogService Logs { get; }
    readonly Settings Settings;
    private bool disposedValue;

    public AppLogProvider(
        Settings settings,
        LogService repository
    )
    {
        Settings = settings;
        Logs = repository;
    }

    public ILogger CreateLogger(string categoryName)
        => Loggers.GetOrAdd(
            categoryName,
            name => new AppLogger(name, Logs)
        );

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                
                Loggers.Clear();             
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
