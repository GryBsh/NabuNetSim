using Microsoft.Extensions.Logging;
using Nabu.NetSim.UI.Services;
using Nabu.Models;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;

namespace Nabu.NetSim.UI;
/*
public class AppDataFactory : IDesignTimeDbContextFactory<AppData>
{
    public AppData CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppData>();
        optionsBuilder.UseSqlite("Data Source=data.db");

        return new AppData(optionsBuilder.Options);
    }
}

public class AppData : DbContext
{
    public DbSet<LogEntry> LogEntries { get; set; }

    public AppData(DbContextOptions<AppData> options) : base(options)
    {
        
    }
}
*/
public class AppLogger : ILogger
{
    private readonly string FullName;
    private readonly string Name;
    private readonly AppLogConfiguration Settings;

    public AppLogger(
        string name,
        AppLogConfiguration settings,
        LogService log
        //IRepository<LogEntry> repository
    )
    {
        FullName = name;
        Name = name.Split('.')[^1];
        Settings = settings;
        Logs = log;
        //LogEntries = repository;
    }

    //IRepository<LogEntry> LogEntries { get; }
    LogService Logs { get; }
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => default!;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= Settings.LogLevel &&
           (FullName.StartsWith("Microsoft.AspNetCore") is false);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel)) return;

        if (eventId.Name is not null) 
            return;

        Logs.Insert(
                new LogEntry(
                    Guid.NewGuid(),
                    DateTime.Now,
                    logLevel.ToString(),
                    Name,
                    formatter(state, exception)
                )
            );

    }
}

