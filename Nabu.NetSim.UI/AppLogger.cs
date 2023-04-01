using DynamicData.Binding;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.ObjectModel;
using System.Linq;
using Blazorise;
using Microsoft.Extensions.Logging;
using System;

namespace Nabu.NetSim.UI;



public class AppLogger : ILogger
{
    private readonly string Name;
    private readonly AppLogProvider Provider;
    private readonly AppLogConfiguration Settings;

    public AppLogger(
        string name,
        AppLogProvider provider,
        AppLogConfiguration settings,
        IRepository<LogEntry> repository
    )
    {
        Name = name.Split('.')[^1];
        Provider = provider;
        Settings = settings;
        LogEntries = repository;
    }

    IRepository<LogEntry> LogEntries { get; }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => default!;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= Settings.LogLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel)) return;

        if (eventId.Name?.StartsWith("System.Net.Http") is false) return;

        Task.Run(() =>
            LogEntries.Insert(
                new LogEntry(
                    Guid.NewGuid(),
                    DateTime.Now,
                    logLevel,
                    eventId,
                    Name,
                    formatter(state, exception),
                    exception
                )
            )
        );
    }
}

