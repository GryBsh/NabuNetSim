using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI;

public class LogCleanupJob : Job
{
    protected IRepository<LogEntry> Repository { get; }
    public LogCleanupJob(IConsole<LogCleanupJob> logger, IRepository<LogEntry> repository, Settings settings) : base(logger, settings)
    {
        Repository = repository;
    }

    void Cleanup()
    {
        var cutoff = DateTime.Now.AddHours(-Settings.MaxLogEntryAgeHours);
        //Repository.Collection<LogEntry>().EnsureIndex(e => e.Timestamp);
        var pendingDelete = Repository.Select(e => e.Timestamp < cutoff);
        if (pendingDelete.Any())
        {
            Logger.Write($"Deleting {pendingDelete.Count()} log entries older than {cutoff}");
            Repository.Delete(e => e.Timestamp < cutoff);
        }
        else
        {
            Logger.Write($"No log entries older than {cutoff} to delete");
        }
    }

    public override void Start()
    {
        Cleanup();
        Observable.Interval(
            TimeSpan.FromMinutes(Settings.LogCleanupIntervalMinutes)
        ).Subscribe(_ =>
        {
            Cleanup();
        });
    }
}

/*
public class GCJob : Job
{
    public GCJob(IConsole<GCJob> logger, Settings settings) : base(logger, settings)
    {
    }

    public override void Start()
    {
        Observable.Interval(TimeSpan.FromMinutes(15)).Subscribe(_ => GC.Collect());
    }
}
*/