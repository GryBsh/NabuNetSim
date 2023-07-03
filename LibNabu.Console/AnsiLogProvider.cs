using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Nabu.Cli;


public class AnsiLogProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, AnsiLogger> Loggers = new(StringComparer.OrdinalIgnoreCase);

    private bool disposedValue;
    
    public AnsiLogProvider(
      
    )
    {
      
    }

    public ILogger CreateLogger(string categoryName)
        => Loggers.GetOrAdd(
            categoryName,
            name => new AnsiLogger(name)
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
