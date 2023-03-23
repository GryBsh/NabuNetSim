using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI;

public class LogCleanupJob : Job
{
    protected IRepository Repository { get; }
    public LogCleanupJob(IConsole<LogCleanupJob> logger, IRepository repository, Settings settings) : base(logger, settings)
    {
        Repository = repository;
    }

    public override void Start()
    {
        Observable.Interval(
            TimeSpan.FromHours(Settings.LogCleanupIntervalHours)
        ).Subscribe(_ =>
        {

            var cutoff = DateTime.Now.AddDays(-Settings.MaxLogEntryAgeDays);
            //Repository.Collection<LogEntry>().EnsureIndex(e => e.Timestamp);
            var pendingDelete = Repository.Collection<LogEntry>().Find(e => e.Timestamp < cutoff);
            if (pendingDelete.Any())
            {
                Logger.Write($"Deleting {pendingDelete.Count()} log entries older than {cutoff}");
                Repository.Collection<LogEntry>().DeleteMany(e => e.Timestamp < cutoff);
            }
            else
            {
                Logger.Write($"No log entries older than {cutoff} to delete");
            }
            GC.Collect();
        });
    }
}