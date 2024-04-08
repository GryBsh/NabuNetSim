using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nabu.NetSim.UI;
/*
public class AppLogProvider(
    ILogService repository
    ) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, AppLogger> Loggers = new(StringComparer.OrdinalIgnoreCase);
    private bool disposedValue;

    public ILogger CreateLogger(string categoryName)
        => Loggers.GetOrAdd(
            categoryName,
            name => new AppLogger(name, repository)
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
}*/