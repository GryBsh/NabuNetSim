using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;

namespace Nabu.NetSim.UI;

public sealed class AppLog
{
    // This is some Blazor Server quirk, it only works right on both sides
    // Like this...
    static SourceList<LogEntry> LogEntries = new() { };
    static SemaphoreSlim LogLock = new(1, 1);
    public SourceList<LogEntry> Entries => LogEntries;
    
    static IEnumerable<LogEntry> RemovalStaging { get; set; } = Array.Empty<LogEntry>();
    static IEnumerable<LogEntry> PendingRemoval => RemovalStaging.ToArray();
    public static int Interval { get; } = 30;

    public AppLog(Settings settings)
    {
        LogEntries.Connect()
                  .Bind(out var pending)
                  .Subscribe();

        RemovalStaging = pending.Where(RemovalFilter);

        Observable.Interval(TimeSpan.FromMinutes(Interval))
                  .ObserveOn(ThreadPoolScheduler.Instance)
                  .SubscribeOn(RxApp.MainThreadScheduler)
                  .Subscribe(_ => LogCycle());
    }

    bool RemovalFilter(LogEntry entry) 
        => entry.Timestamp < DateTime.Now.AddMinutes(-Interval);

    void LogCycle()
    {
        Add(
            Guid.NewGuid(),
            DateTime.Now,
            LogLevel.Warning,
            new EventId(0, "Log Cycle"),
            "UI Log Cycle",
            $"Removing {RemovalStaging.Count()} aged log entries",
            null
        );

        lock (LogLock)
        {
            LogEntries.RemoveMany(PendingRemoval);
        }

        GC.Collect();
    }

    public void Add(LogEntry entry)
    {
        lock (LogLock) 
            LogEntries.Add(entry);
    }

    public void Add(
        Guid guid,
        DateTime timestamp,
        LogLevel logLevel,
        EventId eventId,
        string name,
        string message,
        Exception? exception
    )
    {
        Add(new(guid, timestamp, logLevel, eventId, name, message, exception));
    }

}
