using Microsoft.Extensions.Logging;
using Nabu.NetSim.UI.Services;
using Nabu.Models;
using Nabu.Services;
using System.Collections.Concurrent;
using System.Reactive.Linq;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;

namespace Nabu.NetSim.UI;

public class AppLogger : DisposableBase, ILogger
{
    private readonly string FullName;
    private readonly string Name;
    SemaphoreSlim Lock { get; } = new(1, 1);
    public AppLogger(
        string name,
        LogService log
        //IRepository<LogEntry> repository
    )
    {
        FullName = name;
        Name = name.Split('.')[^1];
        Logs = log;
        //LogEntries = repository;
        Disposables.AddInterval(TimeSpan.FromSeconds(10), _ => CommitBatch());
        Disposables.Add(Lock);
    }

    //IRepository<LogEntry> LogEntries { get; }
    LogService Logs { get; }
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => default!;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= LogLevel.Information &&
           (FullName.StartsWith("Microsoft.AspNetCore") is false);

    
    ConcurrentQueue<LogEntry> Batch { get; } = new();

    async void CommitBatch()
    {
        if (Batch.IsEmpty) return;
        await Lock.WaitAsync();
        var batch = Batch.ToArray();
        Batch.Clear();
        Lock.Release();
        Logs.BulkInsert(batch);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel)) 
            return;

        if (eventId.Name is not null) 
            return;

        Task.Run(async () => {
            await Lock.WaitAsync();
            Batch.Enqueue(
                new LogEntry(
                    Guid.NewGuid(),
                    DateTime.Now,
                    logLevel.ToString(),
                    Name,
                    formatter(state, exception)
                )
            );
            Lock.Release();
        });

    }
}

